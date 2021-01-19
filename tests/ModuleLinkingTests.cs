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

            module.Imports.Modules.Count.Should().Be(1);
            module.Imports.Modules[0].ModuleName.Should().Be("a");
            module.Imports.Modules[0].Name.Should().Be("b");
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
            module.Imports.Instances[0].ModuleName.Should().Be("a");
            module.Imports.Instances[0].Name.Should().Be("b");
            module.Imports.Instances[0].Exports.Memories.Count.Should().Be(1);
            module.Imports.Instances[0].Exports.Memories[0].Name.Should().Be("c");
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
            using var importModule = Module.FromText(Engine, "other", @"(module (import ""a"" (func)) (export ""b"" (func 0)) (memory 1))");
            using var module = Module.FromText(Engine, "test", @"(module (import ""a"" (module (import ""b"" (func)))) (export ""c"" (module 0)))");

            var called = false;
            using var instance = module.Instantiate(Store, importModule);
            instance.Modules.Count.Should().Be(1);
            using var other = instance.Modules[0].Instantiate(Store, Function.FromCallback(Store, () => { called = true; }));
            other.Functions.Count.Should().Be(1);
            other.Functions[0].Invoke();
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
