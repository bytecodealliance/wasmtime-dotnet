using System;
using System.Reflection;
using FluentAssertions;
using Wasmtime;
using Xunit;

namespace Wasmtime.Tests
{
    public class ModuleLoadTests
    {
        [Fact]
        public void ItLoadsModuleFromEmbeddedResource()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("hello.wasm");
            stream.Should().NotBeNull();

            using var host = new Host();
            host.LoadModule("hello.wasm", stream).Should().NotBeNull();
        }

        [Fact]
        public void ItLoadsModuleTextFromEmbeddedResource()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("hello.wat");
            stream.Should().NotBeNull();

            using var host = new Host();
            host.LoadModuleText("hello.wat", stream).Should().NotBeNull();
        }
    }
}
