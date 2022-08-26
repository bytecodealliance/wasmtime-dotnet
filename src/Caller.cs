using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents caller information for a function.
    /// </summary>
    public class Caller : IStore, IDisposable
    {
        internal Caller(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            this.handle = handle;
        }

        /// <summary>
        /// Gets an exported memory of the caller by the given name.
        /// </summary>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the exported memory if found or null if a memory of the requested name is not exported.</returns>
        public Memory? GetMemory(string name)
        {
            unsafe
            {
                var bytes = Encoding.UTF8.GetBytes(name);

                fixed (byte* ptr = bytes)
                {
                    if (!Native.wasmtime_caller_export_get(NativeHandle, ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Memory)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Memory(this, item.of.memory);
                }
            }
        }

        /// <summary>
        /// Gets an exported function of the caller by the given name.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>Returns the exported function if found or null if a function of the requested name is not exported.</returns>
        public Function? GetFunction(string name)
        {
            unsafe
            {
                var bytes = Encoding.UTF8.GetBytes(name);

                fixed (byte* ptr = bytes)
                {
                    if (!Native.wasmtime_caller_export_get(NativeHandle, ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Func)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Function(this, item.of.func);
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.handle = IntPtr.Zero;
        }

        private IntPtr NativeHandle
        {
            get
            {
                if (this.handle == IntPtr.Zero)
                {
                    throw new ObjectDisposedException(typeof(Caller).FullName);
                }

                return this.handle;
            }
        }

        StoreContext IStore.Context => new StoreContext(Native.wasmtime_caller_context(NativeHandle));

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

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static unsafe extern bool wasmtime_caller_export_get(IntPtr caller, byte* name, UIntPtr len, out Extern item);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_caller_context(IntPtr caller);
        }

        private IntPtr handle;
    }
}