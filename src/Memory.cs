using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly memory.
    /// </summary>
    public class Memory : MemoryBase, IExternal
    {
        /// <summary>
        /// Creates a new WebAssembly memory.
        /// </summary>
        /// <param name="context">The store context to create the memory in.</param>
        /// <param name="minimum">The minimum number of WebAssembly pages.</param>
        /// <param name="maximum">The maximum number of WebAssembly pages.</param>
        public Memory(StoreContext context, uint minimum = 0, uint maximum = uint.MaxValue)
        {
            if (maximum < minimum)
            {
                throw new ArgumentException("The maximum cannot be less than the minimum.", nameof(maximum));
            }

            Minimum = minimum;
            Maximum = maximum;

            unsafe
            {
                var limits = new Native.Limits();
                limits.min = minimum;
                limits.max = maximum;

                using var type = new TypeHandle(Native.wasm_memorytype_new(limits));
                var error = Native.wasmtime_memory_new(context.handle, type, out this.memory);
                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }
            }
        }

        /// <summary>
        /// The size, in bytes, of a WebAssembly memory page.
        /// </summary>
        public const int PageSize = 65536;

        /// <summary>
        /// The minimum memory size (in WebAssembly page units).
        /// </summary>
        public uint Minimum { get; private set; }

        /// <summary>
        /// The maximum memory size (in WebAssembly page units).
        /// </summary>
        public uint Maximum { get; private set; }

        internal override ExternMemory Extern => memory;

        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Memory,
                of = new ExternUnion { memory = this.memory }
            };
        }
        internal Memory(StoreContext context, ExternMemory memory)
        {
            this.memory = memory;

            using var type = new TypeHandle(Native.wasmtime_memory_type(context.handle, this.memory));

            unsafe
            {
                var limits = Native.wasm_memorytype_limits(type.DangerousGetHandle());
                Minimum = limits->min;
                Maximum = limits->max;
            }
        }

        internal class TypeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public TypeHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                Native.wasm_memorytype_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct Limits
            {
                public uint min;

                public uint max;
            }

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memory_new(IntPtr context, TypeHandle type, out ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memory_type(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_memorytype_new(in Limits limits);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern Limits* wasm_memorytype_limits(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_memorytype_delete(IntPtr handle);
        }

        private readonly ExternMemory memory;
    }
}
