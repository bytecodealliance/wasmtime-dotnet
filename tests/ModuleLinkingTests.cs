using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;
using Wasmtime;

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
            Host = new Host(Store);
        }

        private Engine Engine { get; set; }

        private Store Store { get; set; }

        private Host Host { get; set; }

        [Fact]
        public void ItExposesModuleImports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (import ""a"" ""b"" (module)))");

            module.Imports.Instances.Count.Should().Be(1);

            var instanceImport = module.Imports.Instances[0];
            instanceImport.ModuleName.Should().Be("a");
            instanceImport.Name.Should().Be("");

            instanceImport.Exports.Modules.Count.Should().Be(1);

            var moduleExport = instanceImport.Exports.Modules[0];
            moduleExport.Name.Should().Be("b");
        }

        [Fact]
        public void ItExposesModuleExports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (module (export ""a"")))");

            module.Exports.Modules.Count.Should().Be(1);
            module.Exports.Modules[0].Name.Should().Be("a");
        }

        [Fact]
        public void ItExposesInstanceImports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (import ""a"" ""b"" (instance (export ""c"" (memory 1)))))");

            module.Imports.Instances.Count.Should().Be(1);

            var instanceImport = module.Imports.Instances[0];

            instanceImport.ModuleName.Should().Be("a");
            instanceImport.Name.Should().Be("");
            instanceImport.Exports.Instances.Count.Should().Be(1);

            var instanceExport = instanceImport.Exports.Instances[0];

            instanceExport.Name.Should().Be("b");
            instanceExport.Exports.Memories.Count.Should().Be(1);

            var memoryExport = instanceExport.Exports.Memories[0];

            memoryExport.Name.Should().Be("c");
        }

        [Fact]
        public void ItExposesInstanceExports()
        {
            using var module = Module.FromText(Engine, "test", @"(module (module (table (export ""b"") 1 10 funcref)) (instance (export ""a"") (instantiate 0)))");

            module.Exports.Instances.Count.Should().Be(1);
            module.Exports.Instances[0].Name.Should().Be("a");
            module.Exports.Instances[0].Exports.Tables.Count.Should().Be(1);
            module.Exports.Instances[0].Exports.Tables[0].Name.Should().Be("b");
        }

        [Fact]
        public void ItInstantiatesAModule()
        {
            using var a = Module.FromText(Engine, "other", @"(module (import """" ""a"" (func)) (export ""b"" (func 0)))");
            using var c = Module.FromText(Engine, "test", @"(module (import """" ""a"" (instance $i (export ""b"" (func)))) (export ""d"" (func $i ""b"")))");

            var called = false;

            Host.DefineFunction("", "a", () => { called = true; });

            using var instanceA = Host.Instantiate(a);

            Host.DefineInstance("", "a", instanceA);

            using var instanceC = Host.Instantiate(c);

            instanceC.Functions.Count.Should().Be(1);
            instanceC.Functions[0].Invoke();
            called.Should().BeTrue();
        }

        public void Dispose()
        {
            Engine.Dispose();
            Store.Dispose();
            Host.Dispose();
        }
    }
}
