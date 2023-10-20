using System;
using System.IO;
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
        public void ItFailsSettingNonexistantCompilerStrategy()
        {
            var config = new Config();

            var act = () => { config.WithCompilerStrategy((CompilerStrategy)123); };
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ItSetsProfilingStrategy()
        {
            var config = new Config();

            config.WithProfilingStrategy(ProfilingStrategy.None);

            using var engine = new Engine(config);
        }

        [Fact]
        public void ItFailsSettingNonexistantProfilingStrategy()
        {
            var config = new Config();

            var act = () => { config.WithProfilingStrategy((ProfilingStrategy)123); };
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [Fact]
        public void ItSetsOptimizationLevel()
        {
            var config = new Config();

            config.WithOptimizationLevel(OptimizationLevel.Speed);

            using var engine = new Engine(config);
        }

        [Fact]
        public void ItFailsSettingNonexistantOptimizationLevel()
        {
            var config = new Config();

            var act = () => { config.WithOptimizationLevel((OptimizationLevel)123); };
            act.Should().Throw<ArgumentOutOfRangeException>();
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

        [Fact]
        public void ItSetsDebugInfo()
        {
            var config = new Config();

            config.WithDebugInfo(true);

            using var engine = new Engine(config);
        }

        [Fact]
        public void ItSetsThreads()
        {
            var config = new Config();
            config.WithWasmThreads(true);

            using var engine = new Engine(config);
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", "SharedMemory.wat"));
        }

        [Fact]
        public void ItSetsSIMD()
        {
            var config = new Config();
            config.WithSIMD(false);
            config.WithRelaxedSIMD(false, false);

            Action act = () =>
            {
                using var engine = new Engine(config);
                using var module = Module.FromTextFile(engine, Path.Combine("Modules", "SIMD.wat"));
            };

            act.Should().Throw<WasmtimeException>();
        }

        [Fact]
        public void ItSetsRelaxedSIMD()
        {
            var config = new Config();
            config.WithRelaxedSIMD(false, false);

            Action act = () =>
            {
                using var engine = new Engine(config);
                using var module = Module.FromTextFile(engine, Path.Combine("Modules", "RelaxedSIMD.wat"));
            };

            act.Should().Throw<WasmtimeException>();
        }

        [Fact]
        public void ItSetsBulkMemory()
        {
            var config = new Config();
            config.WithBulkMemory(false);
            config.WithWasmThreads(false);
            config.WithReferenceTypes(false);

            Action act = () =>
            {
                using var engine = new Engine(config);
                using var module = Module.FromTextFile(engine, Path.Combine("Modules", "BulkMemory.wat"));
            };

            act.Should().Throw<WasmtimeException>();
        }

        [Fact]
        public void ItSetsMultiValue()
        {
            var config = new Config();
            config.WithMultiValue(false);

            Action act = () =>
            {
                using var engine = new Engine(config);
                using var module = Module.FromTextFile(engine, Path.Combine("Modules", "MultiValue.wat"));
            };

            act.Should().Throw<WasmtimeException>();
        }
    }
}
