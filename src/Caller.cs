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

                    return new Memory(((IStore)this).Context, item.of.memory);
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