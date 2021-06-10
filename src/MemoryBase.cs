using System;
using System.Text;
using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    /// <summary>
    /// The base class for memories.
    /// </summary>
    public abstract class MemoryBase
    {
        /// <summary>
        /// Gets the current size of the memory, in WebAssembly page units.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <returns>Returns the current size of the memory, in WebAssembly page units.</returns>
        public uint GetSize(StoreContext context)
        {
            return Native.wasmtime_memory_size(context.handle, Extern);
        }

        /// <summary>
        /// Gets the span of the memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
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
        public unsafe Span<byte> GetSpan(StoreContext context)
        {
            var data = Native.wasmtime_memory_data(context.handle, Extern);
            var size = Convert.ToInt32(Native.wasmtime_memory_data_size(context.handle, Extern).ToUInt32());
            return new Span<byte>(data, size);
        }

        /// <summary>
        /// Reads a UTF-8 string from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <param name="length">The length of bytes to read.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadString(StoreContext context, int address, int length)
        {
            return Encoding.UTF8.GetString(GetSpan(context).Slice(address, length));
        }

        /// <summary>
        /// Reads a null-terminated UTF-8 string from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadNullTerminatedString(StoreContext context, int address)
        {
            var slice = GetSpan(context).Slice(address);
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
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The string to write.</param>
        /// <return>Returns the number of bytes written.</return>
        public int WriteString(StoreContext context, int address, string value)
        {
            return Encoding.UTF8.GetBytes(value, GetSpan(context).Slice(address));
        }

        /// <summary>
        /// Reads a byte from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the byte read from memory.</returns>
        public byte ReadByte(StoreContext context, int address)
        {
            return GetSpan(context)[address];
        }

        /// <summary>
        /// Writes a byte to memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The byte to write.</param>
        public void WriteByte(StoreContext context, int address, byte value)
        {
            GetSpan(context)[address] = value;
        }

        /// <summary>
        /// Reads a short from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the short read from memory.</returns>
        public short ReadInt16(StoreContext context, int address)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(GetSpan(context).Slice(address, 2));
        }

        /// <summary>
        /// Writes a short to memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The short to write.</param>
        public void WriteInt16(StoreContext context, int address, short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(GetSpan(context).Slice(address, 2), value);
        }

        /// <summary>
        /// Reads an int from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the int read from memory.</returns>
        public int ReadInt32(StoreContext context, int address)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(GetSpan(context).Slice(address, 4));
        }

        /// <summary>
        /// Writes an int to memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The int to write.</param>
        public void WriteInt32(StoreContext context, int address, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(GetSpan(context).Slice(address, 4), value);
        }

        /// <summary>
        /// Reads a long from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the long read from memory.</returns>
        public long ReadInt64(StoreContext context, int address)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(GetSpan(context).Slice(address, 8));
        }

        /// <summary>
        /// Writes a long to memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The long to write.</param>
        public void WriteInt64(StoreContext context, int address, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(GetSpan(context).Slice(address, 8), value);
        }

        /// <summary>
        /// Reads an IntPtr from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the IntPtr read from memory.</returns>
        public IntPtr ReadIntPtr(StoreContext context, int address)
        {
            if (IntPtr.Size == 4)
            {
                return (IntPtr)ReadInt32(context, address);
            }
            return (IntPtr)ReadInt64(context, address);
        }

        /// <summary>
        /// Writes an IntPtr to memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The IntPtr to write.</param>
        public void WriteIntPtr(StoreContext context, int address, IntPtr value)
        {
            if (IntPtr.Size == 4)
            {
                WriteInt32(context, address, value.ToInt32());
            }
            else
            {
                WriteInt64(context, address, value.ToInt64());
            }
        }

        /// <summary>
        /// Reads a single from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the single read from memory.</returns>
        public float ReadSingle(StoreContext context, int address)
        {
            unsafe
            {
                var i = ReadInt32(context, address);
                return *((float*)&i);
            }
        }

        /// <summary>
        /// Writes a single to memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The single to write.</param>
        public void WriteSingle(StoreContext context, int address, float value)
        {
            unsafe
            {
                WriteInt32(context, address, *(int*)&value);
            }
        }

        /// <summary>
        /// Reads a double from memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the double read from memory.</returns>
        public double ReadDouble(StoreContext context, int address)
        {
            unsafe
            {
                var i = ReadInt64(context, address);
                return *((double*)&i);
            }
        }

        /// <summary>
        /// Writes a double to memory.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The double to write.</param>
        public void WriteDouble(StoreContext context, int address, double value)
        {
            unsafe
            {
                WriteInt64(context, address, *(long*)&value);
            }
        }

        /// <summary>
        /// Grows the memory by the specified number of pages.
        /// </summary>
        /// <param name="context">The store context for the memory.</param>
        /// <param name="delta">The number of WebAssembly pages to grow the memory by.</param>
        /// <returns>Returns the previous size of the Webassembly memory, in pages.</returns>
        public uint Grow(StoreContext context, uint delta)
        {
            var error = Native.wasmtime_memory_grow(context.handle, Extern, delta, out var prev);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
            return prev;
        }

        internal abstract ExternMemory Extern { get; }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static unsafe extern byte* wasmtime_memory_data(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern UIntPtr wasmtime_memory_data_size(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern uint wasmtime_memory_size(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memory_grow(IntPtr context, in ExternMemory memory, uint delta, out uint prev);
        }
    }
}
