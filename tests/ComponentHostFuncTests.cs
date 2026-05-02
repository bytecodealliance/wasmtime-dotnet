using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests;

public class ComponentHostFuncTests
{
    private static byte[] LoadFixture(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
            ?? throw new FileNotFoundException($"Embedded fixture '{name}' not found.");
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    [Fact]
    public void HostAdd_IsInvokedAndResultLifted()
    {
        var bytes = LoadFixture("host-add.wasm");

        using var engine = new Engine();
        using var component = Component.FromBytes(engine, bytes);
        using var linker = new ComponentLinker(engine);
        using var store = new Store(engine);

        var hostInvocations = 0;
        using var root = linker.Root();
        root.DefineFunc("host-add", (args, results) =>
        {
            hostInvocations++;
            var a = args[0].AsU32();
            var b = args[1].AsU32();
            results[0] = ComponentValue.FromU32(a + b);
        });

        var instance = linker.Instantiate(store, component);
        var compute = instance.GetFunction("compute");
        compute.Should().NotBeNull();

        var argv = new[] { ComponentValue.FromU32(40), ComponentValue.FromU32(2) };
        var rets = new ComponentValue[1];

        compute!.Call(argv, rets);

        hostInvocations.Should().Be(1);
        rets[0].AsU32().Should().Be(42u);
    }

    [Fact]
    public void HostAdd_ExceptionPropagatesAsTrap()
    {
        var bytes = LoadFixture("host-add.wasm");

        using var engine = new Engine();
        using var component = Component.FromBytes(engine, bytes);
        using var linker = new ComponentLinker(engine);
        using var store = new Store(engine);

        using var root = linker.Root();
        root.DefineFunc("host-add", (args, results) =>
        {
            throw new InvalidOperationException("host failure");
        });

        var instance = linker.Instantiate(store, component);
        var compute = instance.GetFunction("compute");

        var argv = new[] { ComponentValue.FromU32(1), ComponentValue.FromU32(2) };
        var rets = new ComponentValue[1];

        Action act = () => compute!.Call(argv, rets);
        act.Should().Throw<WasmtimeException>().WithMessage("*host failure*");
    }
}
