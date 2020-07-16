using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class InvalidModuleTests
    {
        [Fact]
        public void ItThrowsWithErrorMessageForInvalidModules()
        {
            using var engine = new Engine();

            Action action = () => Module.FromBytes(engine, "invalid", new byte[] { });

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("WebAssembly module 'invalid' is not valid: Unexpected EOF (at offset 0)");
        }
    }
}
