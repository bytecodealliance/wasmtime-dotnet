using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MultiMemoryTests
    {
        [Fact]
        public void ItFailsWithMultiMemoryDisabled()
        {
            using var engine = new Engine(new Config().WithMultiMemory(false));
            Action action = () =>
            {
                using var module = Module.FromText(engine, "test", @"(module (memory 0 1) (memory 0 1))");
            };

            action
                .Should()
                .Throw<WasmtimeException>()
                .WithMessage("WebAssembly module 'test' is not valid: failed to parse WebAssembly module*");
        }

        [Fact]
        public void ItSucceedsWithoutMultiMemoryDisabled()
        {
            using var engine = new Engine();
            using var module = Module.FromText(engine, "test", @"(module (memory 0 1) (memory 0 1))");
        }
    }
}
