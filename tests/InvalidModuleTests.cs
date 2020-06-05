using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class InvalidModuleTests
    {
        [Fact]
        public void ItThrowsWithErrorMessageForInvalidModules()
        {
            var host = new Host();

            Action action = () => host.LoadModule("invalid", new byte[] {});

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("WebAssembly module 'invalid' is not valid: Unexpected EOF (at offset 0)");
        }
    }
}
