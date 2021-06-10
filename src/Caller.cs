using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents an exported memory of a function's caller.
    /// </summary>
    public class CallerMemory : MemoryBase
    {
        internal CallerMemory(ExternMemory memory)
        {
            this.memory = memory;
        }

        internal override ExternMemory Extern => memory;

        private readonly ExternMemory memory;
    }

    /// <summary>
    /// Represents caller information for a function.
    /// </summary>
    public class Caller
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
        public CallerMemory? GetMemory(string name)
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

                    return new CallerMemory(item.of.memory);
                }
            }
        }

        /// <summary>
        /// Gets the store context of the caller.
        /// </summary>
        public StoreContext Context => new StoreContext(Native.wasmtime_caller_context(handle));

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static unsafe extern bool wasmtime_caller_export_get(IntPtr caller, byte* name, UIntPtr len, out Extern item);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_caller_context(IntPtr caller);
        }

        private readonly IntPtr handle;
    }
}