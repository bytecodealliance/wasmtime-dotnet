using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents caller information for a function.
    /// </summary>
    public readonly ref struct Caller
    {
        internal Caller(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            this.handle = handle;
            this.context = new StoreContext(Native.wasmtime_caller_context(handle));
            this.store = context.Store;
        }

        /// <summary>
        /// Gets a Span from an exported memory of the caller by the given name.
        /// </summary>
        /// <param name="name">The name of the exported memory.</param>
        /// <param name="address">The zero-based address of the start of the span.</param>
        /// <param name="length">The length of the span.</param>
        /// <param name="result">The Span of memory (if the function returns true)</param>
        /// <returns>Returns true if the exported memory is found or false if a memory of the requested name is not exported.</returns>
        /// <remarks>
        /// <para>
        /// The span may become invalid if the memory grows.
        ///
        /// This may happen if the memory is explicitly requested to grow or
        /// grows as a result of WebAssembly execution.
        /// </para>
        /// <para>
        /// Therefore, the returned span should not be used after calling the grow method or
        /// after calling into WebAssembly code.
        /// </para>
        /// <para>
        /// Note that WebAssembly always uses little endian as byte order. On platforms 
        /// that use big endian, you will need to convert numeric values accordingly.
        /// </para>
        /// </remarks>
        public bool TryGetMemorySpan<T>(string name, long address, int length, out Span<T> result)
            where T : unmanaged
        {
            using var nameBytes = name.ToUTF8(stackalloc byte[Math.Min(64, name.Length * 2)]);

            unsafe
            {
                fixed (byte* ptr = nameBytes.Span)
                {
                    if (!Native.wasmtime_caller_export_get(handle, ptr, (UIntPtr)nameBytes.Length, out var item))
                    {
                        result = default;
                        return false;
                    }

                    if (item.kind != ExternKind.Memory)
                    {
                        item.Dispose();
                        result = default;
                        return false;
                    }

                    result = Memory.GetSpan<T>(context, item.of.memory, address, length);
                    return true;
                }
            }
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
                    if (!Native.wasmtime_caller_export_get(handle, ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Memory)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Memory(store, item.of.memory);
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
                    if (!Native.wasmtime_caller_export_get(handle, ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Func)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Function(store, item.of.func);
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="Store"/> associated with this caller.
        /// </summary>
        public Store Store => store;

        /// <summary>
        /// Adds fuel to this store for WebAssembly code to consume while executing.
        /// </summary>
        /// <param name="fuel">The fuel to add to the store.</param>
        public void AddFuel(ulong fuel) => context.AddFuel(fuel);

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
        public ulong ConsumeFuel(ulong fuel) => context.ConsumeFuel(fuel);

        /// <summary>
        /// Gets the fuel consumed by the executing WebAssembly code.
        /// </summary>
        /// <returns>Returns the fuel consumed by the executing WebAssembly code or 0 if fuel consumption was not enabled.</returns>
        public ulong GetConsumedFuel() => context.GetConsumedFuel();

        /// <summary>
        /// Gets the user-defined data from the Store. 
        /// </summary>
        /// <returns>An object represeting the user defined data from this Store</returns>
        public object? GetData() => store.GetData();

        /// <summary>
        /// Replaces the user-defined data in the Store.
        /// </summary>
        public void SetData(object? data) => store.SetData(data);

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern unsafe bool wasmtime_caller_export_get(IntPtr caller, byte* name, UIntPtr len, out Extern item);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_caller_context(IntPtr caller);
        }

        private readonly IntPtr handle;
        internal readonly Store store;
        internal readonly StoreContext context;
    }
}