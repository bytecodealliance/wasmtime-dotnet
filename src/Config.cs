using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents the Wasmtime compiler strategy.
    /// </summary>
    public enum CompilerStrategy : byte
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
    public enum OptimizationLevel : byte
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
    public enum ProfilingStrategy : byte
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
    /// Represents a configuration used to create <see cref="Engine"/> instances.
    /// </summary>
    public class Config : IDisposable
    {
        /// <summary>
        /// Creates a new configuration.
        /// </summary>
        public Config()
        {
            handle = new Handle(Native.wasm_config_new());
        }

        /// <summary>
        /// Sets whether or not to enable debug information.
        /// </summary>
        /// <param name="enable">True to enable debug information or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithDebugInfo(bool enable)
        {
            Native.wasmtime_config_debug_info_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether or not to enable epoch-based interruption of WebAssembly code.
        /// </summary>
        /// <param name="enable">True to enable epoch-based interruption or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithEpochInterruption(bool enable)
        {
            Native.wasmtime_config_epoch_interruption_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether or not to enable fuel consumption for WebAssembly code.
        /// </summary>
        /// <param name="enable">True to enable fuel consumption or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithFuelConsumption(bool enable)
        {
            Native.wasmtime_config_consume_fuel_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets the maximum WebAssembly stack size.
        /// </summary>
        /// <param name="size">The maximum WebAssembly stack size, in bytes.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMaximumStackSize(int size)
        {
            if (size < 0)
            {
                throw new ArgumentException("Stack size cannot be negative.", nameof(size));
            }

            Native.wasmtime_config_max_wasm_stack_set(handle, (UIntPtr)size);
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly threads support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly threads support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithWasmThreads(bool enable)
        {
            Native.wasmtime_config_wasm_threads_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly reference types support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly reference types support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithReferenceTypes(bool enable)
        {
            Native.wasmtime_config_wasm_reference_types_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly SIMD support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly SIMD support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithSIMD(bool enable)
        {
            Native.wasmtime_config_wasm_simd_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly bulk memory support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly bulk memory support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithBulkMemory(bool enable)
        {
            Native.wasmtime_config_wasm_bulk_memory_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly multi-value support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly multi-value support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMultiValue(bool enable)
        {
            Native.wasmtime_config_wasm_multi_value_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether or not enable WebAssembly multi-memory support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly multi-memory support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMultiMemory(bool enable)
        {
            Native.wasmtime_config_wasm_multi_memory_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets the compiler strategy to use.
        /// </summary>
        /// <param name="strategy">The compiler strategy to use.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithCompilerStrategy(CompilerStrategy strategy)
        {
            if (!Enum.IsDefined(typeof(CompilerStrategy), (byte)strategy))
            {
                throw new ArgumentOutOfRangeException(nameof(strategy));
            }

            Native.wasmtime_config_strategy_set(handle, (byte)strategy);
            return this;
        }

        /// <summary>
        /// Sets whether or not enable the Cranelift debug verifier.
        /// </summary>
        /// <param name="enable">True to enable the Cranelift debug verifier or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithCraneliftDebugVerifier(bool enable)
        {
            Native.wasmtime_config_cranelift_debug_verifier_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Configures whether Cranelift should perform a NaN-canonicalization pass.
        /// 
        /// When Cranelift is used as a code generation backend this will configure
        /// it to replace NaNs with a single canonical value.This is useful for users
        /// requiring entirely deterministic WebAssembly computation.
        /// 
        /// This is not required by the WebAssembly spec, so it is not enabled by default.
        /// 
        /// The default value for this is `false`
        /// </summary>
        /// <param name="enable">True to enable the Cranelift nan canonicalization or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithCraneliftNaNCanonicalization(bool enable)
        {
            Native.wasmtime_config_cranelift_nan_canonicalization_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets the optimization level to use.
        /// </summary>
        /// <param name="level">The optimization level to use.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithOptimizationLevel(OptimizationLevel level)
        {
            if (!Enum.IsDefined(typeof(OptimizationLevel), (byte)level))
            {
                throw new ArgumentOutOfRangeException(nameof(level));
            }

            Native.wasmtime_config_cranelift_opt_level_set(handle, (byte)level);
            return this;
        }

        /// <summary>
        /// Sets the profiling strategy to use.
        /// </summary>
        /// <param name="strategy">The profiling strategy to use.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithProfilingStrategy(ProfilingStrategy strategy)
        {
            if (!Enum.IsDefined(typeof(ProfilingStrategy), (byte)strategy))
            {
                throw new ArgumentOutOfRangeException(nameof(strategy));
            }

            Native.wasmtime_config_profiler_set(handle, (byte)strategy);
            return this;
        }

        /// <summary>
        /// Sets the maximum size of static WebAssembly linear memories.
        /// </summary>
        /// <param name="size">The maximum size of static WebAssembly linear memories, in bytes.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithStaticMemoryMaximumSize(ulong size)
        {
            Native.wasmtime_config_static_memory_maximum_size_set(handle, size);
            return this;
        }

        /// <summary>
        /// Sets the maximum size of the guard region for static WebAssembly linear memories.
        /// </summary>
        /// <param name="size">The maximum guard region size for static WebAssembly linear memories, in bytes.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithStaticMemoryGuardSize(ulong size)
        {
            Native.wasmtime_config_static_memory_guard_size_set(handle, size);
            return this;
        }

        /// <summary>
        /// Sets the maximum size of the guard region for dynamic WebAssembly linear memories.
        /// </summary>
        /// <param name="size">The maximum guard region size for dynamic WebAssembly linear memories, in bytes.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithDynamicMemoryGuardSize(ulong size)
        {
            Native.wasmtime_config_dynamic_memory_guard_size_set(handle, size);
            return this;
        }

        /// <summary>
        /// Sets the path to the Wasmtime cache configuration to use.
        /// </summary>
        /// <remarks>
        /// If the path is null, the default Wasmtime cache configuration will be used.
        /// </remarks>
        /// <param name="path">The path to the cache configuration file to use or null to load the default configuration.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithCacheConfig(string? path)
        {
            var error = Native.wasmtime_config_cache_config_load(handle, path);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return this;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        internal Handle NativeHandle
        {
            get
            {
                if (handle.IsInvalid)
                {
                    throw new ObjectDisposedException(typeof(Config).FullName);
                }

                return handle;
            }
        }

        internal class Handle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public Handle(IntPtr handle)
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

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_config_new();

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_config_delete(IntPtr config);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_debug_info_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_epoch_interruption_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_consume_fuel_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_max_wasm_stack_set(Handle config, UIntPtr size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_threads_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_reference_types_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_simd_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_bulk_memory_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_multi_value_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_multi_memory_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_strategy_set(Handle config, byte strategy);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_debug_verifier_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_nan_canonicalization_set(Handle config, bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_opt_level_set(Handle config, byte level);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_profiler_set(Handle config, byte strategy);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_static_memory_maximum_size_set(Handle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_static_memory_guard_size_set(Handle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_dynamic_memory_guard_size_set(Handle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_config_cache_config_load(Handle config, [MarshalAs(UnmanagedType.LPUTF8Str)] string? path);
        }

        private readonly Handle handle;
    }
}
