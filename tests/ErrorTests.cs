using System;
using System.IO;
using System.Reflection;

using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ErrorFixture : ModuleFixture
    {
        protected override string ModuleFileName => "Error.wat";
    }

    public class ErrorTests : IClassFixture<ErrorFixture>, IDisposable
    {
        private ErrorFixture Fixture { get; set; }

        private Store Store { get; set; }

        private Linker Linker { get; set; }

        private Action ErrorFromHostExceptionCallback { get; set; }

        private Action HostCallback { get; set; }

        public ErrorTests(ErrorFixture fixture)
        {
            Fixture = fixture;
            Store = new Store(Fixture.Engine);
            Linker = new Linker(Fixture.Engine);

            Linker.DefineWasi();

            Linker.Define("", "error_from_host_exception", Function.FromCallback(
                Store,
                () => ErrorFromHostExceptionCallback?.Invoke()));

            Linker.Define("", "call_host_callback", Function.FromCallback(
                Store,
                () => HostCallback?.Invoke()));
        }

        [Fact]
        public void ItPassesCallbackErrorCauseAsInnerException()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var callError = instance.GetAction("error_from_host_exception");
            var errorInWasm = instance.GetAction("error_in_wasm");

            var exceptionToThrow = new IOException("My I/O exception.");

            ErrorFromHostExceptionCallback = () => throw exceptionToThrow;

            // Verify that the IOException thrown at the host callback is passed as
            // InnerException to the WasmtimeException thrown on the host-to-wasm transition.
            var action = callError;

            action
                .Should()
                .Throw<WasmtimeException>()
                .Where(e => e.InnerException == exceptionToThrow);

            // After that, ensure that when invoking another function that causes an error
            // by setting the WASI exit code (so it cannot have a .NET exception as cause),
            // the WasmtimeException's InnerException is now null.
            action = errorInWasm;
            action
                .Should()
                .Throw<WasmtimeException>()
                .Where(e => e.InnerException == null);
        }

        [Fact]
        public void ItPassesCallbackErrorCauseAsInnerExceptionOverTwoLevels()
        {
            var instance = Linker.Instantiate(Store, Fixture.Module);
            var callError = instance.GetAction("error_from_host_exception");
            var callHostCallback = instance.GetAction("call_host_callback");

            var exceptionToThrow = new IOException("My I/O exception.");

            ErrorFromHostExceptionCallback = () => throw exceptionToThrow;
            HostCallback = callError;

            // Verify that the IOException is passed as InnerException to the
            // WasmtimeException even after two levels of wasm-to-host transitions.
            var action = callHostCallback;

            action
                .Should()
                .Throw<WasmtimeException>()
                .Where(e => e.InnerException == exceptionToThrow);
        }

        [Fact]
        public void ItPassesCallbackErrorCauseAsInnerExceptionWhenInstantiating()
        {
            var exceptionToThrow = new IOException("My I/O exception.");
            HostCallback = () => throw exceptionToThrow;

            var action = () => Linker.Instantiate(Store, Fixture.Module);

            action
                .Should()
                .Throw<WasmtimeException>()
                .Where(e => e.InnerException == exceptionToThrow);
        }

        public void Dispose()
        {
            Store.Dispose();
            Linker.Dispose();
        }
    }
}
