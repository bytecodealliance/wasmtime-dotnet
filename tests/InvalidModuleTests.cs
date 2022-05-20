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
                .WithMessage("WebAssembly module 'invalid' is not valid: failed to parse WebAssembly module*");
        }

        [Fact]
        public void ItReturnsAnErrorWhenValidatingAnInvalidModule()
        {
            using var engine = new Engine();
            Module.Validate(engine, Array.Empty<byte>()).Should().Be("unexpected end-of-file (at offset 0x0)");
        }
    }
}
