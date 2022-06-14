using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents context about a <see cref="Store"/>.
    /// </summary>
    public readonly ref struct StoreContext
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
        }

        internal readonly IntPtr handle;
    }

    /// <summary>
    /// Represents a Wasmtime interrupt handle.
    /// </summary>
    public class InterruptHandle : IDisposable
    {
        /// <summary>
        /// Creates a new interrupt handle from the given store.
        /// </summary>
        /// <param name="store">The store to create the interrupt handle for.</param>
        public InterruptHandle(IStore store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            handle = new Handle(Native.wasmtime_interrupt_handle_new(store.Context.handle));
        }

        /// <summary>
        /// Interrupt any executing WebAssembly code in the associated store.
        /// </summary> 
        public void Interrupt()
        {
            Native.wasmtime_interrupt_handle_interrupt(NativeHandle);
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
                Native.wasmtime_interrupt_handle_delete(handle);
                return true;
            }
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_interrupt_handle_new(IntPtr context);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_interrupt_handle_delete(IntPtr handle);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_interrupt_handle_interrupt(Handle handle);
        }

        private readonly Handle handle;
    }

    /// <summary>
    /// An interface implemented on types that behave like stores.
    /// </summary>
    public interface IStore
    {
        /// <summary>
        /// Gets the context of the store.
        /// </summary>
        StoreContext Context { get; }
    }

    /// <summary>
    /// Represents a Wasmtime store.
    /// </summary>
    /// <remarks>
    /// A Wasmtime store may be sent between threads but cannot be used from more than one thread
    /// simultaneously.
    /// </remarks>
    public class Store : IStore, IDisposable
    {
        /// <summary>
        /// Constructs a new store.
        /// </summary>
        /// <param name="engine">The engine to use for the store.</param>
        public Store(Engine engine)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            handle = new Handle(Native.wasmtime_store_new(engine.NativeHandle, IntPtr.Zero, null));
        }

        /// <summary>
        /// Perform garbage collection within the given store.
        /// </summary>
        public void GC() => ((IStore)this).Context.GC();

        /// <summary>
        /// Adds fuel to this store for WebAssembly code to consume while executing.
        /// </summary>
        /// <param name="fuel">The fuel to add to the store.</param>
        public void AddFuel(ulong fuel) => ((IStore)this).Context.AddFuel(fuel);

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
        public ulong ConsumeFuel(ulong fuel) => ((IStore)this).Context.ConsumeFuel(fuel);

        /// <summary>
        /// Gets the fuel consumed by the executing WebAssembly code.
        /// </summary>
        /// <returns>Returns the fuel consumed by the executing WebAssembly code or 0 if fuel consumption was not enabled.</returns>
        public ulong GetConsumedFuel() => ((IStore)this).Context.GetConsumedFuel();

        /// <summary>
        /// Configres WASI within the store.
        /// </summary>
        /// <param name="config">The WASI configuration to use.</param>
        public void SetWasiConfiguration(WasiConfiguration config) => ((IStore)this).Context.SetWasiConfiguration(config);

        /// <summary>
        /// Configures the relative deadline at which point WebAssembly code will trap.
        /// </summary>
        /// <param name="ticksBeyondCurrent"></param>
        public void SetEpochDeadline(ulong ticksBeyondCurrent) => ((IStore)this).Context.SetEpochDeadline(ticksBeyondCurrent);

        StoreContext IStore.Context => new StoreContext(Native.wasmtime_store_context(NativeHandle));

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
    }
}
