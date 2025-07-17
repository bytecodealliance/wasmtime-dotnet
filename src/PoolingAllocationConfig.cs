using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Wasmtime;

/// <summary>
/// A type containing configuration options for the pooling allocator. For more information
/// see the Rust documentation at <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html</a>
/// </summary>
public class PoolingAllocationConfig
    : IDisposable
{
    private readonly Handle handle;

    /// <summary>
    /// Creates a new configuration.
    /// </summary>
    public PoolingAllocationConfig()
    {
        handle = new Handle(Native.wasmtime_pooling_allocation_config_new());
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

    /// <summary>
    /// Configures the maximum number of “unused warm slots” to retain in the pooling allocator. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_unused_warm_slots">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_unused_warm_slots</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxUnusedWarmSlots(uint count)
    {
        Native.wasmtime_pooling_allocation_config_max_unused_warm_slots_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The target number of decommits to do per batch. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.decommit_batch_size">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.decommit_batch_size</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithDecommitBatchSize(nuint count)
    {
        Native.wasmtime_pooling_allocation_config_decommit_batch_size_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// How much memory, in bytes, to keep resident for async stacks allocated with the pooling allocator. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.async_stack_keep_resident">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.async_stack_keep_resident</a>
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    private PoolingAllocationConfig WithAsyncStackKeepResidentBytes(nuint bytes)
    {
        // todo: Wasmtime-dotnet does not support async! Expose this if support is added.

        Native.wasmtime_pooling_allocation_config_async_stack_keep_resident_set(NativeHandle, bytes);
        return this;
    }

    /// <summary>
    /// How much memory, in bytes, to keep resident for each linear memory after deallocation. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.linear_memory_keep_resident">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.linear_memory_keep_resident</a>
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithLinearMemoryKeepResidentBytes(nuint bytes)
    {
        Native.wasmtime_pooling_allocation_config_linear_memory_keep_resident_set(NativeHandle, bytes);
        return this;
    }

    /// <summary>
    /// How much memory, in bytes, to keep resident for each table after deallocation. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.table_keep_resident">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.table_keep_resident</a>
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithTableKeepResidentBytes(nuint bytes)
    {
        Native.wasmtime_pooling_allocation_config_table_keep_resident_set(NativeHandle, bytes);
        return this;
    }

    /// <summary>
    /// The maximum number of concurrent component instances supported (default is 1000). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_component_instances">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_component_instances</a> 
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithTotalComponentInstances(uint count)
    {
        Native.wasmtime_pooling_allocation_config_total_component_instances_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum size, in bytes, allocated for a component instance’s VMComponentContext metadata. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_component_instance_size">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_component_instance_size</a> 
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxComponentInstanceSize(nuint bytes)
    {
        Native.wasmtime_pooling_allocation_config_max_component_instance_size_set(NativeHandle, bytes);
        return this;
    }

    /// <summary>
    /// The maximum size, in bytes, allocated for a core instance’s VMContext metadata. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_core_instance_size">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_core_instance_size</a> 
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxCoreInstanceSize(nuint bytes)
    {
        Native.wasmtime_pooling_allocation_config_max_core_instance_size_set(NativeHandle, bytes);
        return this;
    }

    /// <summary>
    /// The maximum number of core instances a single component may contain (default is unlimited). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_core_instances_per_component">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_core_instances_per_component</a> 
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxCoreInstancesPerComponent(uint count)
    {
        Native.wasmtime_pooling_allocation_config_max_core_instances_per_component_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of Wasm linear memories that a single component may transitively contain (default is unlimited). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_memories_per_component">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_memories_per_component</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxMemoriesPerComponent(uint count)
    {
        Native.wasmtime_pooling_allocation_config_max_memories_per_component_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of tables that a single component may transitively contain (default is unlimited).  For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_tables_per_component">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_tables_per_component</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxTablesPerComponent(uint count)
    {
        Native.wasmtime_pooling_allocation_config_max_tables_per_component_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of concurrent Wasm linear memories supported (default is 1000). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_memories">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_memories</a> 
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxMemories(uint count)
    {
        Native.wasmtime_pooling_allocation_config_total_memories_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of execution stacks allowed for asynchronous execution, when enabled (default is 1000).
    /// For more information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_stacks">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_stacks</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    private PoolingAllocationConfig WithMaxStacks(uint count)
    {
        // todo: Wasmtime-dotnet does not support async! Expose this if support is added.

        Native.wasmtime_pooling_allocation_config_total_stacks_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of concurrent tables supported (default is 1000). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_tables">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_tables</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxTables(uint count)
    {
        Native.wasmtime_pooling_allocation_config_total_tables_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of concurrent core instances supported (default is 1000). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_core_instances">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_core_instances</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxCoreInstances(uint count)
    {
        Native.wasmtime_pooling_allocation_config_total_core_instances_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of defined tables for a core module (default is 1). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_tables_per_module">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_tables_per_module</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxTablesPerModule(uint count)
    {
        Native.wasmtime_pooling_allocation_config_max_tables_per_module_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum table elements for any table defined in a module (default is 20000). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.table_elements">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.table_elements</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxTableElements(nuint count)
    {
        Native.wasmtime_pooling_allocation_config_table_elements_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum number of defined linear memories for a module (default is 1). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_memories_per_module">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_memories_per_module</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxMemoriesPerModule(uint count)
    {
        Native.wasmtime_pooling_allocation_config_max_memories_per_module_set(NativeHandle, count);
        return this;
    }

    /// <summary>
    /// The maximum byte size that any WebAssembly linear memory may grow to. For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_memory_size">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.max_memory_size</a>
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxMemorySize(nuint bytes)
    {
        Native.wasmtime_pooling_allocation_config_max_memory_size_set(NativeHandle, bytes);
        return this;
    }

    /// <summary>
    /// The maximum number of concurrent GC heaps supported (default is 1000). For more
    /// information see the Rust documentation at
    /// <a href="https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_gc_heaps">https://docs.wasmtime.dev/api/wasmtime/struct.PoolingAllocationConfig.html#method.total_gc_heaps</a>
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public PoolingAllocationConfig WithMaxGcHeaps(uint count)
    {
        Native.wasmtime_pooling_allocation_config_total_gc_heaps_set(NativeHandle, count);
        return this;
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
            Native.wasmtime_pooling_allocation_config_delete(handle);
            return true;
        }
    }

    internal static class Native
    {
        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_pooling_allocation_config_new();

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_delete(IntPtr intPtr);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_unused_warm_slots_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_decommit_batch_size_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_async_stack_keep_resident_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_linear_memory_keep_resident_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_table_keep_resident_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_total_component_instances_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_component_instance_size_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_core_instances_per_component_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_memories_per_component_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_tables_per_component_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_total_memories_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_total_tables_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_total_stacks_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_total_core_instances_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_core_instance_size_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_tables_per_module_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_table_elements_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_memories_per_module_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_max_memory_size_set(Handle handle, nuint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_config_total_gc_heaps_set(Handle handle, uint value);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_pooling_allocation_strategy_set(Config.Handle configHandle, Handle handle);
    }
}