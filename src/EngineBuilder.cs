using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents the Wasmtime compiler strategy.
    /// </summary>
    public enum CompilerStrategy
    {
        /// <summary>
        /// Automatically pick the compiler strategy.
        /// </summary>
        Auto,
        /// <summary>
        /// Use the Cranelift compiler.
        /// </summary>
        Cranelift,
        /// <summary>
        /// Use the Lightbeam compiler.
        /// </summary>
        Lightbeam
    }

    /// <summary>
    /// Represents the Wasmtime optimization level.
    /// </summary>
    public enum OptimizationLevel
    {
        /// <summary>
        /// Disable optimizations.
        /// </summary>
        None,
        /// <summary>
        /// Optimize for speed.
        /// </summary>
        Speed,
        /// <summary>
        /// Optimize for speed and size.
        /// </summary>
        SpeedAndSize
    }

    /// <summary>
    /// Represents the Wasmtime code profiling strategy.
    /// </summary>
    public enum ProfilingStrategy
    {
        /// <summary>
        /// Disable code profiling.
        /// </summary>
        None,
        /// <summary>
        /// Linux "jitdump" profiling.
        /// </summary>
        JitDump,
        /// <summary>
        /// VTune code profiling.
        /// </summary>
        VTune
    }

    /// <summary>
    /// Represents a builder of <see cref="Engine"/> instances.
    /// </summary>
    public class EngineBuilder
    {
        /// <summary>
        /// Sets whether or not to enable debug information.
        /// </summary>
        /// <param name="enable">True to enable debug information or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithDebugInfo(bool enable)
        {
            debugInfo = enable;
            return this;
        }

        /// <summary>
        /// Sets whether or not to enable interruptability of WebAssembly code.
        /// </summary>
        /// <param name="enable">True to enable interruptability or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithInterruptability(bool enable)
        {
            interruptability = enable;
            return this;
        }

        /// <summary>
        /// Sets whether or not to enable fuel consumption for WebAssembly code.
        /// </summary>
        /// <param name="enable">True to enable fuel consumption or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithFuelConsumption(bool enable)
        {
            fuelConsumption = enable;
            return this;
        }

        /// <summary>
        /// Sets the maximum WebAssembly stack size.
        /// </summary>
        /// <param name="size">The maximum WebAssembly stack size, in bytes.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithMaximumStackSize(int size)
        {
            if (size < 0)
            {
                throw new ArgumentException("Stack size cannot be negative.", nameof(size));
            }

            maximumStackSize = size;
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly threads support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly threads support or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithWasmThreads(bool enable)
        {
            threads = enable;
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly reference types support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly reference types support or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithReferenceTypes(bool enable)
        {
            referenceTypes = enable;
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly SIMD support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly SIMD support or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithSIMD(bool enable)
        {
            simd = enable;
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly bulk memory support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly bulk memory support or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithBulkMemory(bool enable)
        {
            bulkMemory = enable;
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly multi-value support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly multi-value support or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithMultiValue(bool enable)
        {
            multiValue = enable;
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly module linking support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly module linking support or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithModuleLinking(bool enable)
        {
            moduleLinking = enable;
            return this;
        }

        /// <summary>
        /// Sets the compiler strategy to use.
        /// </summary>
        /// <param name="strategy">The compiler strategy to use.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithCompilerStrategy(CompilerStrategy strategy)
        {
            if (!Enum.IsDefined(typeof(CompilerStrategy), (byte)strategy))
            {
                throw new ArgumentOutOfRangeException(nameof(strategy));
            }

            compilerStrategy = (byte)strategy;
            return this;
        }

        /// <summary>
        /// Sets whether or not enable the Cranelift debug verifier.
        /// </summary>
        /// <param name="enable">True to enable the Cranelift debug verifier or false to disable.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithCraneliftDebugVerifier(bool enable)
        {
            craneliftDebugVerifier = enable;
            return this;
        }

        /// <summary>
        /// Sets the optimization level to use.
        /// </summary>
        /// <param name="level">The optimization level to use.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithOptimizationLevel(OptimizationLevel level)
        {
            if (!Enum.IsDefined(typeof(OptimizationLevel), (byte)level))
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            optLevel = (byte)level;
            return this;
        }

        /// <summary>
        /// Sets the profiling strategy to use.
        /// </summary>
        /// <param name="strategy">The profiling strategy to use.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithOptimizationLevel(ProfilingStrategy strategy)
        {
            if (!Enum.IsDefined(typeof(ProfilingStrategy), (byte)strategy))
            {
                throw new ArgumentOutOfRangeException(nameof(strategy));
            }

            profilingStrategy = (byte)strategy;
            return this;
        }

        /// <summary>
        /// Sets the maximum size of static WebAssembly linear memories.
        /// </summary>
        /// <param name="size">The maximum size of static WebAssembly linear memories, in bytes.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithStaticMemoryMaximumSize(ulong size)
        {
            staticMemoryMaximumSize = size;
            return this;
        }

        /// <summary>
        /// Sets the maximum size of the guard region for static WebAssembly linear memories.
        /// </summary>
        /// <param name="size">The maximum guard region size for static WebAssembly linear memories, in bytes.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithStaticMemoryGuardSize(ulong size)
        {
            staticMemoryGuardSize = size;
            return this;
        }

        /// <summary>
        /// Sets the maximum size of the guard region for dynamic WebAssembly linear memories.
        /// </summary>
        /// <param name="size">The maximum guard region size for dynamic WebAssembly linear memories, in bytes.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithDynamicMemoryGuardSize(ulong size)
        {
            dynamicMemoryGuardSize = size;
            return this;
        }

        /// <summary>
        /// Sets the path to the Wasmtime cache configuration to use.
        /// </summary>
        /// <remarks>
        /// If the path is null, the default Wasmtime cache configuration will be used.
        /// </remarks>
        /// <param name="path">The path to the cache configuration file to use or null to load the default configuration.</param>
        /// <returns>Returns the current builder.</returns>
        public EngineBuilder WithCacheConfig(string? path)
        {
            cacheConfig = path;
            return this;
        }

        /// <summary>
        /// Builds the <see cref="Engine" /> instance.
        /// </summary>
        /// <returns>Returns the new <see cref="Engine" /> instance.</returns>
        public Engine Build()
        {
            var config = new ConfigHandle(Native.wasm_config_new());

            if (debugInfo.HasValue)
            {
                Native.wasmtime_config_debug_info_set(config, debugInfo.Value);
            }

            if (interruptability.HasValue)
            {
                Native.wasmtime_config_interruptable_set(config, interruptability.Value);
            }

            if (fuelConsumption.HasValue)
            {
                Native.wasmtime_config_consume_fuel_set(config, fuelConsumption.Value);
            }

            if (maximumStackSize.HasValue)
            {
                Native.wasmtime_config_max_wasm_stack_set(config, (UIntPtr)maximumStackSize.Value);
            }

            if (threads.HasValue)
            {
                Native.wasmtime_config_wasm_threads_set(config, threads.Value);
            }

            if (referenceTypes.HasValue)
            {
                Native.wasmtime_config_wasm_reference_types_set(config, referenceTypes.Value);
            }

            if (simd.HasValue)
            {
                Native.wasmtime_config_wasm_simd_set(config, simd.Value);
            }

            if (bulkMemory.HasValue)
            {
                Native.wasmtime_config_wasm_bulk_memory_set(config, bulkMemory.Value);
            }

            if (multiValue.HasValue)
            {
                Native.wasmtime_config_wasm_multi_value_set(config, multiValue.Value);
            }

            if (moduleLinking.HasValue)
            {
                Native.wasmtime_config_wasm_module_linking_set(config, moduleLinking.Value);
            }

            if (compilerStrategy.HasValue)
            {
                var error = Native.wasmtime_config_strategy_set(config, compilerStrategy.Value);
                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }
            }

            if (craneliftDebugVerifier.HasValue)
            {
                Native.wasmtime_config_cranelift_debug_verifier_set(config, craneliftDebugVerifier.Value);
            }

            if (optLevel.HasValue)
            {
                Native.wasmtime_config_cranelift_opt_level_set(config, optLevel.Value);
            }

            if (profilingStrategy.HasValue)
            {
                var error = Native.wasmtime_config_profiler_set(config, profilingStrategy.Value);
                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }
            }

            if (staticMemoryMaximumSize.HasValue)
            {
                Native.wasmtime_config_static_memory_maximum_size_set(config, staticMemoryMaximumSize.Value);
            }

            if (staticMemoryGuardSize.HasValue)
            {
                Native.wasmtime_config_static_memory_guard_size_set(config, staticMemoryGuardSize.Value);
            }

            if (dynamicMemoryGuardSize.HasValue)
            {
                Native.wasmtime_config_dynamic_memory_guard_size_set(config, dynamicMemoryGuardSize.Value);
            }

            if (!(cacheConfig is null))
            {
                var error = Native.wasmtime_config_cache_config_load(config, cacheConfig);
                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }
            }

            var handle = config.DangerousGetHandle();
            config.SetHandleAsInvalid();
            return new Engine(handle);
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_config_new();

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_config_delete(IntPtr config);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_debug_info_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_interruptable_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_consume_fuel_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_max_wasm_stack_set(ConfigHandle config, UIntPtr size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_threads_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_reference_types_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_simd_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_bulk_memory_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_multi_value_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_module_linking_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_config_strategy_set(ConfigHandle config, byte strategy);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_debug_verifier_set(ConfigHandle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_opt_level_set(ConfigHandle config, byte level);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_config_profiler_set(ConfigHandle config, byte strategy);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_static_memory_maximum_size_set(ConfigHandle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_static_memory_guard_size_set(ConfigHandle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_dynamic_memory_guard_size_set(ConfigHandle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_config_cache_config_load(ConfigHandle config, [MarshalAs(UnmanagedType.LPUTF8Str)] string? path);
        }

        private class ConfigHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public ConfigHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                Native.wasm_config_delete(handle);
                return true;
            }
        }

        private bool? debugInfo;
        private bool? interruptability;
        private bool? fuelConsumption;
        private int? maximumStackSize;
        private bool? threads;
        private bool? referenceTypes;
        private bool? simd;
        private bool? bulkMemory;
        private bool? multiValue;
        private bool? moduleLinking;
        private byte? compilerStrategy;
        private bool? craneliftDebugVerifier;
        private byte? optLevel;
        private byte? profilingStrategy;
        private ulong? staticMemoryMaximumSize;
        private ulong? staticMemoryGuardSize;
        private ulong? dynamicMemoryGuardSize;
        private string? cacheConfig;
    }
}
