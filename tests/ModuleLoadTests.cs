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

            // `LoadModule` is not supposed to close the supplied stream,
            // so the following statement should complete without throwing
            // `ObjectDisposedException`
            stream.Read(new byte[0], 0, 0);
        }

        [Fact]
        public void ItLoadsModuleTextFromEmbeddedResource()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("hello.wat");
            stream.Should().NotBeNull();

            using var host = new Host();
            host.LoadModuleText("hello.wat", stream).Should().NotBeNull();

            // `LoadModuleText` is not supposed to close the supplied stream,
            // so the following statement should complete without throwing
            // `ObjectDisposedException`
            stream.Read(new byte[0], 0, 0);
        }
    }
}
