using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class WasiPreview2StubTests
    {
        [Fact]
        public void DefineWasiPreview1AdapterStubs_SatisfiesAdapterImports()
        {
            using var engine = new Engine();
            using var store = new Store(engine);
            using var linker = new Linker(engine);
            linker.DefineWasiPreview1AdapterStubs();

            const string wat = "(module\n" +
                               "  (import \"wasi_snapshot_preview1\" \"adapter_close_badfd\" (func $f (param i32) (result i32)))\n" +
                               "  (func (export \"call\") (param i32) (result i32)\n" +
                               "    (call $f (local.get 0))))";

            using var module = Module.FromText(engine, "adapter", wat);
            var instance = linker.Instantiate(store, module);

            var call = instance.GetFunction<int, int>("call");
            call.Should().NotBeNull();
            call!(123).Should().Be(8);
        }

        [Fact]
        public void DefineWasiPreview2ResourceDropStubs_SatisfiesResourceDropImports()
        {
            using var engine = new Engine();
            using var store = new Store(engine);
            using var linker = new Linker(engine);
            linker.DefineWasiPreview2ResourceDropStubs();

            const string wat = "(module\n" +
                               "  (import \"wasi:io/poll@0.2.0\" \"[resource-drop]pollable\" (func $drop (param i32)))\n" +
                               "  (func (export \"drop\") (param i32)\n" +
                               "    (call $drop (local.get 0))))";

            using var module = Module.FromText(engine, "resource_drop", wat);
            var instance = linker.Instantiate(store, module);

            var drop = instance.GetAction<int>("drop");
            drop.Should().NotBeNull();
            drop!(0);
        }

        [Fact]
        public void DefineWasiPreview2Stubs_SatisfiesAdapterAndResourceDropImports()
        {
            using var engine = new Engine();
            using var store = new Store(engine);
            using var linker = new Linker(engine);
            linker.DefineWasiPreview2Stubs();

            const string wat = "(module\n" +
                               "  (import \"wasi_snapshot_preview1\" \"adapter_open_badfd\" (func $open (param i32) (result i32)))\n" +
                               "  (import \"wasi:sockets/tcp@0.2.0\" \"[resource-drop]tcp-socket\" (func $drop (param i32)))\n" +
                               "  (func (export \"call\") (param i32) (result i32)\n" +
                               "    (call $drop (local.get 0))\n" +
                               "    (call $open (local.get 0))))";

            using var module = Module.FromText(engine, "combined", wat);
            var instance = linker.Instantiate(store, module);

            var call = instance.GetFunction<int, int>("call");
            call.Should().NotBeNull();
            call!(9).Should().Be(8);
        }
    }
}
