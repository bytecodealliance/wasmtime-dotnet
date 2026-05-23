using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

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
        Cranelift
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
        VTune,
        /// <summary>
        /// Linux "perfmap" profiling.
        /// </summary>
        PerfMap
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
        /// Sets whether or not to the WebAssembly tail call proposal is enabled. 
        /// </summary>
        /// <param name="enable">True to enable tails calls or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithTailCalls(bool enable)
        {
            Native.wasmtime_config_wasm_tail_call_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether the WebAssembly typed function reference types proposal is enabled. 
        /// </summary>
        /// <param name="enable">True to enable function references or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithFunctionReferences(bool enable)
        {
            Native.wasmtime_config_wasm_function_references_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether the WebAssembly GC proposal is enabled. 
        /// </summary>
        /// <param name="enable">True to enable GC or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithGc(bool enable)
        {
            Native.wasmtime_config_wasm_gc_set(handle, enable);
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
        /// Sets whether or not to enable WebAssembly Relaxed SIMD support. New SIMD instructions that may be non-deterministic across different hosts unless deterministic mode is enabled.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly Relaxed SIMD support or false to disable.</param>
        /// <param name="deterministic">True to enable deterministic mode for WebAssembly Relaxed SIMD or false to allow non-deterministic execution.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithRelaxedSIMD(bool enable, bool deterministic)
        {
            Native.wasmtime_config_wasm_relaxed_simd_set(handle, enable);
            Native.wasmtime_config_wasm_relaxed_simd_deterministic_set(handle, deterministic);
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
        /// Sets whether or not enable WebAssembly memory64 support.
        /// </summary>
        /// <param name="enable">True to enable WebAssembly memory64 support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMemory64(bool enable)
        {
            Native.wasmtime_config_wasm_memory64_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether the WebAssembly wide-arithmetic proposal is enabled. 
        /// </summary>
        /// <param name="enable">True to enable WebAssembly wide arithmetic support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithWideArithmetic(bool enable)
        {
            Native.wasmtime_config_wasm_wide_arithmetic_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether the WebAssembly stack switching proposal is enabled. 
        /// </summary>
        /// <param name="enable">True to enable WebAssembly stack switching support or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        private Config WithStackSwitching(bool enable)
        {
            // todo: unlikely to be compatible with wasmtime-dotnet on Windows due to threads

            Native.wasmtime_config_wasm_stack_switching_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets whether wasmtime should compile a module using multiple threads.
        /// </summary>
        /// <param name="enable">True to enable parallel compilation or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithParallelCompilation(bool enable)
        {
            Native.wasmtime_config_parallel_compilation_set(handle, enable);
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
        /// Configures whether memory_reservation is the maximal size of linear memory. <see cref="WithStaticMemoryMaximumSize"/>
        /// </summary>
        /// <param name="enable">True to allow memory base pointer to move or false to disable</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMemoryMayMove(bool enable)
        {
            Native.wasmtime_config_memory_may_move_set(handle, enable);
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
        /// Configures the size, in bytes, of initial memory reservation size for linear memories. If <see cref="WithMemoryMayMove"/> if not set, this is the maximum size of memory.
        /// </summary>
        /// <param name="size">The initial size of static WebAssembly linear memories, in bytes.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithStaticMemoryMaximumSize(ulong size)
        {
            Native.wasmtime_config_memory_reservation_set(handle, size);
            return this;
        }

        /// <summary>
        /// Configures the size, in bytes, of the extra virtual memory space reserved for memories to grow into after being relocated.
        /// </summary>
        /// <param name="size">The extra reserved bytes of virtual memory reserved for future growth.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMemoryReservationForGrowth(ulong size)
        {
            Native.wasmtime_config_memory_reservation_for_growth_set(handle, size);
            return this;
        }

        /// <summary>
        /// Configures whether copy-on-write memory-mapped data is used to initialize a linear memory.
        /// Initializing linear memory via a copy-on-write mapping can drastically improve instantiation costs of a
        /// WebAssembly module because copying memory is deferred.Additionally if a page of memory is only ever read
        /// from WebAssembly and never written too then the same underlying page of data will be reused between
        /// all instantiations of a module meaning that if a module is instantiated many times this can lower the
        /// overall memory required needed to run that module.
        /// </summary>
        /// <param name="enabled">True to enable copy on write for memory initialisation or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMemoryInitCopyOnWrite(bool enabled)
        {
            Native.wasmtime_config_memory_init_cow_set(handle, enabled);
            return this;
        }

        /// <summary>
        /// Sets the size of the guard region used at the end of a linear memory’s address space reservation
        /// </summary>
        /// <param name="size">The size, in bytes, of the guard region used at the end of a linear memory’s address space reservation</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMemoryGuardSize(ulong size)
        {
            Native.wasmtime_config_memory_guard_size_set(handle, size);
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

        /// <summary>
        /// Configures whether, when on macOS, Mach ports are used for exception handling
        /// instead of traditional Unix-based signal handling.
        /// 
        /// This option defaults to true, using Mach ports by default.
        /// </summary>
        /// <param name="enable">True to enable Mach ports or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithMacosMachPorts(bool enable)
        {
            Native.wasmtime_config_macos_use_mach_ports(handle, enable);
            return this;
        }

        /// <summary>
        /// Configures whether to generate native unwind information (e.g. .eh_frame on Linux).
        /// 
        /// For more information see the Rust documentation at <a href="https://docs.wasmtime.dev/api/wasmtime/struct.Config.html#method.native_unwind_info">https://docs.wasmtime.dev/api/wasmtime/struct.Config.html#method.native_unwind_info</a>
        /// </summary>
        /// <param name="enable">True to enable unwind info or false to disable.</param>
        /// <returns>Returns the current config.</returns>
        public Config WithUnwindInfo(bool enable)
        {
            Native.wasmtime_config_native_unwind_info_set(handle, enable);
            return this;
        }

        /// <summary>
        /// Sets the Wasmtime allocation strategy to use the pooling allocator. For more
        /// information see the Rust documentation at
        /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.Config.html#method.allocation_strategy">https://docs.wasmtime.dev/api/wasmtime/struct.Config.html#method.allocation_strategy</a>
        /// </summary>
        /// <param name="strategy">The pooling strategy config</param>
        /// <returns></returns>
        public Config WithPoolingAllocationStrategy(PoolingAllocationConfig strategy)
        {
            PoolingAllocationConfig.Native.wasmtime_pooling_allocation_strategy_set(NativeHandle, strategy.NativeHandle);
            return this;
        }

        /// <summary>
        /// Configures whether the WebAssembly component-model proposal will be enabled for compilation. For more
        /// information see the Rust documentation at
        /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.Config.html#method.wasm_component_model">https://docs.wasmtime.dev/api/wasmtime/struct.Config.html#method.wasm_component_model</a>
        /// </summary>
        /// <param name="enabled">True to enable component model or false to disable.</param>
        /// <returns></returns>
        public Config WithComponentModel(bool enabled)
        {
            Native.wasmtime_config_wasm_component_model_set(NativeHandle, enabled);
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
                if (handle.IsInvalid || handle.IsClosed)
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
            public static extern void wasmtime_config_debug_info_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_epoch_interruption_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_consume_fuel_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_tail_call_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_function_references_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_gc_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_max_wasm_stack_set(Handle config, nuint size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_threads_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_reference_types_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_simd_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_relaxed_simd_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_relaxed_simd_deterministic_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_bulk_memory_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_multi_value_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_multi_memory_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_memory64_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);
            
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_wide_arithmetic_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);
            
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_stack_switching_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);
            
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_parallel_compilation_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_strategy_set(Handle config, byte strategy);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_debug_verifier_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_nan_canonicalization_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);
            
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_memory_may_move_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_native_unwind_info_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_memory_init_cow_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);
            
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_memory_reservation_for_growth_set(Handle config, ulong bytes);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_cranelift_opt_level_set(Handle config, byte level);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_profiler_set(Handle config, byte strategy);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_memory_reservation_set(Handle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_memory_guard_size_set(Handle config, ulong size);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_config_cache_config_load(Handle config, [MarshalAs(Extensions.LPUTF8Str)] string? path);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_macos_use_mach_ports(Handle config, [MarshalAs(UnmanagedType.I1)] bool enable);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_config_wasm_component_model_set(Handle config, [MarshalAs(UnmanagedType.I1)] bool value);

            // todo: void wasmtime_config_host_memory_creator_set(wasm_config_t *, wasmtime_memory_creator_t *)
        }

        private readonly Handle handle;
    }
}
