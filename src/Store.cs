using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents context about a <see cref="Wasmtime.Store"/>.
    /// </summary>
    internal readonly ref struct StoreContext
    {
        internal StoreContext(IntPtr handle)
        {
            this.handle = handle;
        }

        internal void GC()
        {
            Native.wasmtime_context_gc(handle);
        }

        internal void AddFuel(ulong fuel)
        {
            var error = Native.wasmtime_context_add_fuel(handle, fuel);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        internal Store Store
        {
            get
            {
                var data = Native.wasmtime_context_get_data(handle);

                // Since this is a weak handle, it could be `null` if the target object (`Store`)
                // was already collected. However, this would be an error in wasmtime-dotnet
                // itself because the `Store` must be kept alive when this is called, and
                // therefore this should never happen (otherwise, when the `Store` was already
                // GCed, its `Handle` might also be GCed and have run its finalizer, which
                // would already have freed the `GCHandle` (from the Finalize callback) and thus
                // it would already be undefined behavior to try to get the `GCHandle` from the
                // `IntPtr` value).
                var targetStore = (Store?)GCHandle.FromIntPtr(data).Target!;

                return targetStore;
            }
        }

        internal ulong ConsumeFuel(ulong fuel)
        {
            var error = Native.wasmtime_context_consume_fuel(handle, fuel, out var remaining);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return remaining;
        }

        internal ulong GetConsumedFuel()
        {
            if (!Native.wasmtime_context_fuel_consumed(handle, out var fuel))
            {
                return 0;
            }

            return fuel;
        }

        internal void SetWasiConfiguration(WasiConfiguration config)
        {
            var wasi = config.Build();
            var error = Native.wasmtime_context_set_wasi(handle, wasi.DangerousGetHandle());
            wasi.SetHandleAsInvalid();

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// Configures the relative deadline at which point WebAssembly code will trap.
        /// </summary>
        /// <param name="deadline"></param>
        public void SetEpochDeadline(ulong deadline)
        {
            Native.wasmtime_context_set_epoch_deadline(handle, deadline);
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_context_gc(IntPtr handle);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_context_add_fuel(IntPtr handle, ulong fuel);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_context_consume_fuel(IntPtr handle, ulong fuel, out ulong remaining);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_context_fuel_consumed(IntPtr handle, out ulong fuel);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_context_set_wasi(IntPtr handle, IntPtr config);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_context_set_epoch_deadline(IntPtr handle, ulong ticksBeyondCurrent);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_context_get_data(IntPtr handle);
        }

        internal readonly IntPtr handle;
    }

    /// <summary>
    /// Represents a Wasmtime store.
    /// </summary>
    /// <remarks>
    /// A Wasmtime store may be sent between threads but cannot be used from more than one thread
    /// simultaneously.
    /// </remarks>
    public class Store : IDisposable
    {
        /// <summary>
        /// Constructs a new store.
        /// </summary>
        /// <param name="engine">The engine to use for the store.</param>
        public Store(Engine engine) : this(engine, null) { }

        /// <summary>
        /// Constructs a new store with the given context data.
        /// </summary>
        /// <param name="engine">The engine to use for the store.</param>
        /// <param name="data">The data to initialize the store with; this can later be accessed with the GetData function.</param>
        public Store(Engine engine, object? data)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            this.data = data;

            // Allocate a weak GCHandle, so that it does not participate in keeping the Store alive.
            // Otherwise, the circular reference would prevent the Store from being finalized even
            // if it's no longer referenced by user code.
            // The weak handle will be used to get the originating Store object from a Caller's
            // context in host callbacks.
            var storeHandle = GCHandle.Alloc(this, GCHandleType.Weak);

            handle = new Handle(Native.wasmtime_store_new(engine.NativeHandle, (IntPtr)storeHandle, Finalizer));
        }

        /// <summary>
        /// Perform garbage collection within the given store.
        /// </summary>
        public void GC()
        {
            _funcCache.Clear();
            _memCache.Clear();

            Context.GC();
            System.GC.KeepAlive(this);
        }

        /// <summary>
        /// Adds fuel to this store for WebAssembly code to consume while executing.
        /// </summary>
        /// <param name="fuel">The fuel to add to the store.</param>
        public void AddFuel(ulong fuel)
        {
            Context.AddFuel(fuel);
            System.GC.KeepAlive(this);
        }

        /// <summary>
        /// Synthetically consumes fuel from this store.
        /// 
        /// For this method to work fuel consumption must be enabled via <see cref="Config.WithFuelConsumption(bool)"/>.
        /// 
        /// WebAssembly execution will automatically consume fuel but if so desired the embedder can also consume fuel manually
        /// to account for relative costs of host functions, for example.
        /// 
        /// This method will attempt to consume <paramref name="fuel"/> units of fuel from within this store. If the remaining
        /// amount of fuel allows this then the amount of remaining fuel is returned. Otherwise, a <see cref="WasmtimeException"/>
        /// is thrown and no fuel is consumed.
        /// </summary>
        /// <param name="fuel">The fuel to consume from the store.</param>
        /// <returns>Returns the remaining amount of fuel.</returns>
        /// <exception cref="WasmtimeException">Thrown if more fuel is consumed than the store currently has.</exception>
        public ulong ConsumeFuel(ulong fuel)
        {
            var result = Context.ConsumeFuel(fuel);
            System.GC.KeepAlive(this);
            return result;
        }

        /// <summary>
        /// Gets the fuel consumed by the executing WebAssembly code.
        /// </summary>
        /// <returns>Returns the fuel consumed by the executing WebAssembly code or 0 if fuel consumption was not enabled.</returns>
        public ulong GetConsumedFuel()
        {
            var result = Context.GetConsumedFuel();
            System.GC.KeepAlive(this);
            return result;
        }

        /// <summary>
        /// Configures WASI within the store.
        /// </summary>
        /// <param name="config">The WASI configuration to use.</param>
        public void SetWasiConfiguration(WasiConfiguration config)
        {
            Context.SetWasiConfiguration(config);
            System.GC.KeepAlive(this);
        }

        /// <summary>
        /// Configures the relative deadline at which point WebAssembly code will trap.
        /// </summary>
        /// <param name="ticksBeyondCurrent"></param>
        public void SetEpochDeadline(ulong ticksBeyondCurrent)
        {
            Context.SetEpochDeadline(ticksBeyondCurrent);
            System.GC.KeepAlive(this);
        }

        /// <summary>
        /// Retrieves the data stored in the Store context
        /// </summary>
        public object? GetData() => data;

        /// <summary>
        /// Replaces the data stored in the Store context 
        /// </summary>
        public void SetData(object? data) => this.data = data;

        /// <summary>
        /// Gets the context of the store.
        /// </summary>
        /// <remarks>
        /// Note: Generally, you must keep the <see cref="Store"/> alive (by using
        /// <see cref="GC.KeepAlive(object)"/>) until the <see cref="StoreContext"/> is no longer
        /// used, to prevent the the <see cref="Handle"/> finalizer from prematurely deleting the
        /// store handle in the GC finalizer thread while the <see cref="StoreContext"/> is still
        /// in use.
        /// </remarks>
        internal StoreContext Context => new StoreContext(Native.wasmtime_store_context(NativeHandle));

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
                    throw new ObjectDisposedException(typeof(Store).FullName);
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
                Native.wasmtime_store_delete(handle);
                return true;
            }
        }

        private static class Native
        {
            public delegate void Finalizer(IntPtr data);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_store_new(Engine.Handle engine, IntPtr data, Finalizer? finalizer);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_store_context(Handle store);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_store_delete(IntPtr store);
        }

        private readonly Handle handle;

        private object? data;

        private static readonly Native.Finalizer Finalizer = (p) => GCHandle.FromIntPtr(p).Free();

        private readonly ConcurrentDictionary<nuint, Function> _funcCache = new();
        internal Function GetCachedExtern(ExternFunc @extern)
        {
            if (!_funcCache.TryGetValue(@extern.index, out var func))
            {
                func = new Function(this, @extern);
                func = _funcCache.GetOrAdd(@extern.index, func);
            }

            return func;
        }

        private readonly ConcurrentDictionary<nuint, Memory> _memCache = new();
        internal Memory GetCachedExtern(ExternMemory @extern)
        {
            if (!_memCache.TryGetValue(@extern.index, out var mem))
            {
                mem = new Memory(this, @extern);
                mem = _memCache.GetOrAdd(@extern.index, mem);
            }

            return mem;
        }
    }
}
