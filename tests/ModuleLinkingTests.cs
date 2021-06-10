using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ModuleLinkingTests : IDisposable
    {
        public ModuleLinkingTests()
        {
            Engine = new EngineBuilder()
                .WithModuleLinking(true)
                .Build();
            Store = new Store(Engine);
            Linker = new Linker(Engine);
        }

        private Engine Engine { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        [Fact]
        public void ItExposesModuleImports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (import ""a"" ""b"" (module)))");

            module.Imports.Count(i => i is InstanceImport).Should().Be(1);

            var instanceImport = module.Imports[0] as InstanceImport;
            instanceImport.Should().NotBeNull();
            instanceImport.ModuleName.Should().Be("a");
            instanceImport.Name.Should().Be("");
            instanceImport.Exports.Count(e => e is ModuleExport).Should().Be(1);

            var moduleExport = instanceImport.Exports[0] as ModuleExport;
            moduleExport.Should().NotBeNull();
            moduleExport.Name.Should().Be("b");
        }

        [Fact]
        public void ItExposesModuleExports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (module (export ""a"")))");

            module.Exports.Count(e => e is ModuleExport).Should().Be(1);

            var moduleExport = module.Exports[0] as ModuleExport;
            moduleExport.Should().NotBeNull();
            moduleExport.Name.Should().Be("a");
        }

        [Fact]
        public void ItExposesInstanceImports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (import ""a"" ""b"" (instance (export ""c"" (memory 1)))))");

            module.Imports.Count(i => i is InstanceImport).Should().Be(1);

            var instanceImport = module.Imports[0] as InstanceImport;
            instanceImport.Should().NotBeNull();
            instanceImport.ModuleName.Should().Be("a");
            instanceImport.Name.Should().Be("");
            instanceImport.Exports.Count(e => e is InstanceExport).Should().Be(1);

            var instanceExport = instanceImport.Exports[0] as InstanceExport;
            instanceExport.Should().NotBeNull();
            instanceExport.Name.Should().Be("b");
            instanceExport.Exports.Count(e => e is MemoryExport).Should().Be(1);

            var memoryExport = instanceExport.Exports[0] as MemoryExport;
            memoryExport.Should().NotBeNull();
            memoryExport.Name.Should().Be("c");
        }

        [Fact]
        public void ItExposesInstanceExports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (module (table (export ""b"") 1 10 funcref)) (instance (export ""a"") (instantiate 0)))");

            module.Exports.Count(e => e is InstanceExport).Should().Be(1);

            var instanceExport = module.Exports[0] as InstanceExport;
            instanceExport.Should().NotBeNull();
            instanceExport.Name.Should().Be("a");
            instanceExport.Exports.Count(e => e is TableExport).Should().Be(1);

            var tableExport = instanceExport.Exports[0] as TableExport;
            tableExport.Should().NotBeNull();
            tableExport.Name.Should().Be("b");
        }

        [Fact]
        public void ItInstantiatesAModule()
        {
            using var a = Module.FromText(Engine, "other", @"(module (import """" ""a"" (func)) (export ""b"" (func 0)))");
            using var c = Module.FromText(Engine, "test", @"(module (import """" ""a"" (instance $i (export ""b"" (func)))) (export ""d"" (func $i ""b"")))");

            var called = false;

            var context = Store.Context;
            Linker.Define("", "a", Function.FromCallback(Store.Context, () => { called = true; }));

            var instanceA = Linker.Instantiate(context, a);

            Linker.AllowShadowing = true;
            Linker.Define("", "a", instanceA);

            var instanceC = Linker.Instantiate(context, c);

            var d = instanceC.GetFunction(context, "d");
            d.Invoke(context);
            called.Should().BeTrue();
        }

        public void Dispose()
        {
            Engine.Dispose();
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
