using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly memory.
    /// </summary>
    public class Memory : IExternal
    {
        /// <summary>
        /// Creates a new WebAssembly memory.
        /// </summary>
        /// <param name="store">The store to create the memory in.</param>
        /// <param name="minimum">The minimum number of WebAssembly pages.</param>
        /// <param name="maximum">The maximum number of WebAssembly pages.</param>
        public Memory(IStore store, uint minimum = 0, uint maximum = uint.MaxValue)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

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
                var error = Native.wasmtime_memory_new(store.Context.handle, type, out this.memory);
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

        /// <summary>
        /// Gets the current size of the memory, in WebAssembly page units.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <returns>Returns the current size of the memory, in WebAssembly page units.</returns>
        public uint GetSize(IStore store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return Native.wasmtime_memory_size(store.Context.handle, this.memory);
        }

        /// <summary>
        /// Gets the span of the memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <returns>Returns the span of the memory.</returns>
        /// <remarks>
        /// The span may become invalid if the memory grows.
        ///
        /// This may happen if the memory is explicitly requested to grow or
        /// grows as a result of WebAssembly execution.
        ///
        /// Therefore, the returned span should not be used after calling the grow method or
        /// after calling into WebAssembly code.
        /// </remarks>
        public unsafe Span<byte> GetSpan(IStore store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            var data = Native.wasmtime_memory_data(context.handle, this.memory);
            var size = Convert.ToInt32(Native.wasmtime_memory_data_size(context.handle, this.memory).ToUInt32());
            return new Span<byte>(data, size);
        }

        /// <summary>
        /// Reads a UTF-8 string from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <param name="length">The length of bytes to read.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadString(IStore store, int address, int length)
        {
            return Encoding.UTF8.GetString(GetSpan(store).Slice(address, length));
        }

        /// <summary>
        /// Reads a null-terminated UTF-8 string from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadNullTerminatedString(IStore store, int address)
        {
            var slice = GetSpan(store).Slice(address);
            var terminator = slice.IndexOf((byte)0);
            if (terminator == -1)
            {
                throw new InvalidOperationException("string is not null terminated");
            }

            return Encoding.UTF8.GetString(slice.Slice(0, terminator));
        }

        /// <summary>
        /// Writes a UTF-8 string at the given address.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The string to write.</param>
        /// <return>Returns the number of bytes written.</return>
        public int WriteString(IStore store, int address, string value)
        {
            return Encoding.UTF8.GetBytes(value, GetSpan(store).Slice(address));
        }

        /// <summary>
        /// Reads a byte from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the byte read from memory.</returns>
        public byte ReadByte(IStore store, int address)
        {
            return GetSpan(store)[address];
        }

        /// <summary>
        /// Writes a byte to memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The byte to write.</param>
        public void WriteByte(IStore store, int address, byte value)
        {
            GetSpan(store)[address] = value;
        }

        /// <summary>
        /// Reads a short from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the short read from memory.</returns>
        public short ReadInt16(IStore store, int address)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(GetSpan(store).Slice(address, 2));
        }

        /// <summary>
        /// Writes a short to memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The short to write.</param>
        public void WriteInt16(IStore store, int address, short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(GetSpan(store).Slice(address, 2), value);
        }

        /// <summary>
        /// Reads an int from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the int read from memory.</returns>
        public int ReadInt32(IStore store, int address)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(GetSpan(store).Slice(address, 4));
        }

        /// <summary>
        /// Writes an int to memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The int to write.</param>
        public void WriteInt32(IStore store, int address, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(GetSpan(store).Slice(address, 4), value);
        }

        /// <summary>
        /// Reads a long from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the long read from memory.</returns>
        public long ReadInt64(IStore store, int address)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(GetSpan(store).Slice(address, 8));
        }

        /// <summary>
        /// Writes a long to memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The long to write.</param>
        public void WriteInt64(IStore store, int address, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(GetSpan(store).Slice(address, 8), value);
        }

        /// <summary>
        /// Reads an IntPtr from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the IntPtr read from memory.</returns>
        public IntPtr ReadIntPtr(IStore store, int address)
        {
            if (IntPtr.Size == 4)
            {
                return (IntPtr)ReadInt32(store, address);
            }
            return (IntPtr)ReadInt64(store, address);
        }

        /// <summary>
        /// Writes an IntPtr to memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The IntPtr to write.</param>
        public void WriteIntPtr(IStore store, int address, IntPtr value)
        {
            if (IntPtr.Size == 4)
            {
                WriteInt32(store, address, value.ToInt32());
            }
            else
            {
                WriteInt64(store, address, value.ToInt64());
            }
        }

        /// <summary>
        /// Reads a single from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the single read from memory.</returns>
        public float ReadSingle(IStore store, int address)
        {
            unsafe
            {
                var i = ReadInt32(store, address);
                return *((float*)&i);
            }
        }

        /// <summary>
        /// Writes a single to memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The single to write.</param>
        public void WriteSingle(IStore store, int address, float value)
        {
            unsafe
            {
                WriteInt32(store, address, *(int*)&value);
            }
        }

        /// <summary>
        /// Reads a double from memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the double read from memory.</returns>
        public double ReadDouble(IStore store, int address)
        {
            unsafe
            {
                var i = ReadInt64(store, address);
                return *((double*)&i);
            }
        }

        /// <summary>
        /// Writes a double to memory.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The double to write.</param>
        public void WriteDouble(IStore store, int address, double value)
        {
            unsafe
            {
                WriteInt64(store, address, *(long*)&value);
            }
        }

        /// <summary>
        /// Grows the memory by the specified number of pages.
        /// </summary>
        /// <param name="store">The store that owns the memory.</param>
        /// <param name="delta">The number of WebAssembly pages to grow the memory by.</param>
        /// <returns>Returns the previous size of the Webassembly memory, in pages.</returns>
        /// <remarks>This method will invalidate previously returned values from `GetSpan`.</remarks>
        public uint Grow(IStore store, uint delta)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var error = Native.wasmtime_memory_grow(store.Context.handle, this.memory, delta, out var prev);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return prev;
        }

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
            public static unsafe extern byte* wasmtime_memory_data(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern UIntPtr wasmtime_memory_data_size(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern uint wasmtime_memory_size(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memory_grow(IntPtr context, in ExternMemory memory, uint delta, out uint prev);

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
