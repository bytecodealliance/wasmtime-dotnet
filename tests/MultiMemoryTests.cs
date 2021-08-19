using System;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class MultiMemoryTests
    {
        [Fact]
        public void ItFailsWithoutMultiMemoryEnabled()
        {
            using var engine = new Engine();
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
        public void ItSucceedsWithMultiMemoryEnabled()
        {
            using var engine = new Engine(new Config().WithMultiMemory(true));
            using var module = Module.FromText(engine, "test", @"(module (memory 0 1) (memory 0 1))");
        }
    }
}
