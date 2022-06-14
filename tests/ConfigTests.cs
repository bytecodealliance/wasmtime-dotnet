using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ConfigTests
    {
        [Fact]
        public void ItSetsCompilerStrategy()
        {
            var config = new Config();

            config.WithCompilerStrategy(CompilerStrategy.Cranelift);

            using var engine = new Engine(config);
        }

        [Fact]
        public void ItSetsProfilingStrategy()
        {
            var config = new Config();

            config.WithProfilingStrategy(ProfilingStrategy.None);

            using var engine = new Engine(config);
        }

        [Fact]
        public void ItSetsOptimizationLevel()
        {
            var config = new Config();

            config.WithOptimizationLevel(OptimizationLevel.Speed);

            using var engine = new Engine(config);
        }

        [Fact]
        public void ItSetsNanCanonicalization()
        {
            var config = new Config();

            config.WithCraneliftNaNCanonicalization(true);

            using var engine = new Engine(config);
        }

        [Fact]
        public void ItSetsEpochInterruption()
        {
            var config = new Config();

            config.WithEpochInterruption(true);

            using var engine = new Engine(config);
        }
    }
}
