using System;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

public class ComponentTests
{
    [Fact]
    public void FromBytes_RejectsCoreModule()
    {
        // Empty wasm core module: magic "\0asm" + version 1.
        var coreModule = new byte[] { 0x00, 0x61, 0x73, 0x6d, 0x01, 0x00, 0x00, 0x00 };

        using var engine = new Engine();
        Action act = () => Component.FromBytes(engine, coreModule);

        act.Should().Throw<WasmtimeException>();
    }

    [Fact]
    public void FromBytes_RejectsGarbage()
    {
        using var engine = new Engine();
        var bytes = new byte[] { 0xff, 0xff, 0xff, 0xff };

        Action act = () => Component.FromBytes(engine, bytes);
        act.Should().Throw<WasmtimeException>();
    }

    [Fact]
    public void Linker_CanBeCreatedAndDisposed()
    {
        using var engine = new Engine();
        using var linker = new ComponentLinker(engine);
        // Dispose via using; should not throw.
    }

    [Fact]
    public void Linker_AddWasiPreview2_Succeeds()
    {
        using var engine = new Engine();
        using var linker = new ComponentLinker(engine);

        Action act = () => linker.AddWasiPreview2();
        act.Should().NotThrow();
    }
}
