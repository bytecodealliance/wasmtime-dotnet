using System;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ModuleLoadTests
    {
        [Fact]
        public void ItLoadsModuleFromEmbeddedResource()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("hello.wasm")!;
            stream.Should().NotBeNull();

            using var engine = new Engine();
            Module.FromStream(engine, "hello.wasm", stream).Should().NotBeNull();

            // `LoadModule` is not supposed to close the supplied stream,
            // so the following statement should complete without throwing
            // `ObjectDisposedException`
            stream.ReadExactly(Array.Empty<byte>(), 0, 0);
        }

        [Fact]
        public void ItValidatesModuleFromEmbeddedResource()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("hello.wasm")!;
            stream.Should().NotBeNull();

            byte[] buffer = new byte[stream.Length];

            stream.ReadExactly(buffer, 0, buffer.Length);

            using var engine = new Engine();
            Module.Validate(engine, buffer).Should().BeNull();
        }

        [Fact]
        public void ItLoadsModuleTextFromEmbeddedResource()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("hello.wat")!;
            stream.Should().NotBeNull();

            using var engine = new Engine();
            Module.FromTextStream(engine, "hello.wat", stream).Should().NotBeNull();

            // `LoadModuleText` is not supposed to close the supplied stream,
            // so the following statement should complete without throwing
            // `ObjectDisposedException`
            stream.ReadExactly(new byte[0], 0, 0);
        }

        [Fact]
        public void ItCannotBeAccessedOnceDisposed()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("hello.wasm")!;
            stream.Should().NotBeNull();

            using var engine = new Engine();
            var module = Module.FromStream(engine, "hello.wasm", stream);

            module.Dispose();

            Assert.Throws<ObjectDisposedException>(() => module.NativeHandle);
            Assert.Throws<ObjectDisposedException>(() => module.Serialize());
        }
    }
}
