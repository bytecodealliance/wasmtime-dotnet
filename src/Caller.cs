using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents caller information for a function.
    /// </summary>
    public readonly struct Caller
        : IDisposable
    {
        internal Caller(IntPtr handle)
        {
            _context = CallerContext.Get(handle);
            _epoch = _context.Epoch;
        }

        /// <summary>
        /// Dispose this Caller. All other calls after this will throw ObjectDisposedException.
        /// </summary>
        void IDisposable.Dispose()
        {
            if (_context?.Epoch == _epoch)
            {
                _context.Recycle();
            }
        }

        internal IStore AsStore()
        {
            return _context.GetStore(_epoch);
        }

        private StoreContext StoreContext => new(Native.wasmtime_caller_context(_context.GetHandle(_epoch)));

        /// <summary>
        /// Gets an exported memory of the caller by the given name.
        /// </summary>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the exported memory if found or null if a memory of the requested name is not exported.</returns>
        public Memory? GetMemory(string name)
        {
            unsafe
            {
                //todo:try to avoid this allocation if possible (e.g. stackalloc/ArrayPool)
                var bytes = Encoding.UTF8.GetBytes(name);

                fixed (byte* ptr = bytes)
                {
                    if (!Native.wasmtime_caller_export_get(_context.GetHandle(_epoch), ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Memory)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Memory(AsStore(), item.of.memory);
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
                //todo:try to avoid this allocation if possible (e.g. stackalloc/ArrayPool)
                var bytes = Encoding.UTF8.GetBytes(name);

                fixed (byte* ptr = bytes)
                {
                    if (!Native.wasmtime_caller_export_get(_context.GetHandle(_epoch), ptr, (UIntPtr)bytes.Length, out var item))
                    {
                        return null;
                    }

                    if (item.kind != ExternKind.Func)
                    {
                        item.Dispose();
                        return null;
                    }

                    return new Function(AsStore(), item.of.func);
                }
            }
        }

        /// <summary>
        /// Adds fuel to this store for WebAssembly code to consume while executing.
        /// </summary>
        /// <param name="fuel">The fuel to add to the store.</param>
        public void AddFuel(ulong fuel) => StoreContext.AddFuel(fuel);

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
        public ulong ConsumeFuel(ulong fuel) => StoreContext.ConsumeFuel(fuel);

        /// <summary>
        /// Gets the fuel consumed by the executing WebAssembly code.
        /// </summary>
        /// <returns>Returns the fuel consumed by the executing WebAssembly code or 0 if fuel consumption was not enabled.</returns>
        public ulong GetConsumedFuel() => StoreContext.GetConsumedFuel();

        /// <summary>
        /// Gets the user-defined data from the Store's Context. 
        /// </summary>
        /// <returns>An object represeting the user defined data from this Store</returns>
        public object? GetData() => StoreContext.GetData();

        /// <summary>
        /// Replaces the user-defined data in the Store's Context 
        /// </summary>
        public void SetData(object? data) => StoreContext.SetData(data);

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern unsafe bool wasmtime_caller_export_get(IntPtr caller, byte* name, UIntPtr len, out Extern item);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_caller_context(IntPtr caller);
        }

        private readonly uint _epoch;
        private readonly CallerContext _context;
    }

    /// <summary>
    /// An impementation of IStore with a lifetime tied to a CallerContext
    /// </summary>
    internal class CallerStore
        : IStore
    {
        private readonly uint _epoch;
        private readonly CallerContext _context;

        public CallerStore(CallerContext context, uint epoch)
        {
            _context = context;
            _epoch = epoch;
        }

        public StoreContext Context => new(Caller.Native.wasmtime_caller_context(_context.GetHandle(_epoch)));
    }

    /// <summary>
    /// Internal representation of caller information. Public wrappers compare the "epoch" to check if they have been disposed.
    /// </summary>
    internal class CallerContext
    {
        public static CallerContext Get(IntPtr handle)
        {
            if (handle == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            if (!_pool.TryTake(out var ctx))
                ctx = new CallerContext();

            ctx.Epoch++;
            ctx._handle = handle;

            return ctx;
        }

        public void Recycle()
        {
            Epoch++;
            _handle = IntPtr.Zero;
            _store = null;

            // Do not recycle if epoch is getting near max limit
            if (Epoch > uint.MaxValue - 10)
            {
                return;
            }

            if (_pool.Count < PoolMaxSize)
            {
                _pool.Add(this);
            }
        }

        internal IntPtr GetHandle(uint epoch)
        {
            if (epoch != Epoch)
            {
                throw new ObjectDisposedException(typeof(Caller).FullName);
            }

            return _handle;
        }

        internal CallerStore GetStore(uint epoch)
        {
            if (epoch != Epoch)
            {
                throw new ObjectDisposedException(typeof(Caller).FullName);
            }

            if (_store == null)
            {
                _store = new CallerStore(this, epoch);
            }

            return _store;
        }

        internal uint Epoch { get; private set; }
        private IntPtr _handle;
        private CallerStore? _store;

        private const int PoolMaxSize = 64;
        private static readonly ConcurrentBag<CallerContext> _pool = new();
    }
}