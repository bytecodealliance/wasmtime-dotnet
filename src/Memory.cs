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

            this.store = store;
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
        /// <returns>Returns the current size of the memory, in WebAssembly page units.</returns>
        public uint GetSize()
        {
            return Native.wasmtime_memory_size(store.Context.handle, this.memory);
        }

        /// <summary>
        /// Gets the span of the memory.
        /// </summary>
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
        public unsafe Span<byte> GetSpan()
        {
            var context = store.Context;
            var data = Native.wasmtime_memory_data(context.handle, this.memory);
            var size = Convert.ToInt32(Native.wasmtime_memory_data_size(context.handle, this.memory).ToUInt32());
            return new Span<byte>(data, size);
        }

        /// <summary>
        /// Gets the span of the memory viewed as a specific type, starting at a given address.
        /// </summary>
        /// <param name="address">The zero-based address of the start of the span.</param>
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
        public unsafe Span<T> GetSpan<T>(int address)
            where T : unmanaged
        {
            return MemoryMarshal.Cast<byte, T>(GetSpan()[address..]);
        }

        /// <summary>
        /// Read a struct from memory.
        /// </summary>
        /// <typeparam name="T">Type of the struct to read. Ensure layout in C# is identical to layout in WASM.</typeparam>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the struct read from memory.</returns>
        public T Read<T>(int address)
            where T : unmanaged
        {
            return GetSpan<T>(address)[0];
        }

        /// <summary>
        /// Write a struct to memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address">The zero-based address to read from.</param>
        /// <param name="value">The struct to write.</param>
        public void Write<T>(int address, T value)
            where T : unmanaged
        {
            GetSpan<T>(address)[0] = value;
        }

        /// <summary>
        /// Reads a string from memory with the specified encoding.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <param name="length">The length of bytes to read.</param>
        /// <param name="encoding">The encoding to use when reading the string; if null, UTF-8 encoding is used.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadString(int address, int length, Encoding? encoding = null)
        {
            if (encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetString(GetSpan().Slice(address, length));
        }

        /// <summary>
        /// Reads a null-terminated UTF-8 string from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadNullTerminatedString(int address)
        {
            var slice = GetSpan().Slice(address);
            var terminator = slice.IndexOf((byte)0);
            if (terminator == -1)
            {
                throw new InvalidOperationException("string is not null terminated");
            }

            return Encoding.UTF8.GetString(slice.Slice(0, terminator));
        }

        /// <summary>
        /// Writes a string at the given address with the given encoding.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The string to write.</param>
        /// <param name="encoding">The encoding to use when writing the string; if null, UTF-8 encoding is used.</param>
        /// <return>Returns the number of bytes written.</return>
        public int WriteString(int address, string value, Encoding? encoding = null)
        {
            if (encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetBytes(value, GetSpan().Slice(address));
        }

        /// <summary>
        /// Reads a byte from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the byte read from memory.</returns>
        public byte ReadByte(int address)
        {
            return GetSpan()[address];
        }

        /// <summary>
        /// Writes a byte to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The byte to write.</param>
        public void WriteByte(int address, byte value)
        {
            GetSpan()[address] = value;
        }

        /// <summary>
        /// Reads a short from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the short read from memory.</returns>
        public short ReadInt16(int address)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(GetSpan().Slice(address, 2));
        }

        /// <summary>
        /// Writes a short to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The short to write.</param>
        public void WriteInt16(int address, short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(GetSpan().Slice(address, 2), value);
        }

        /// <summary>
        /// Reads an int from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the int read from memory.</returns>
        public int ReadInt32(int address)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(GetSpan().Slice(address, 4));
        }

        /// <summary>
        /// Writes an int to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The int to write.</param>
        public void WriteInt32(int address, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(GetSpan().Slice(address, 4), value);
        }

        /// <summary>
        /// Reads a long from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the long read from memory.</returns>
        public long ReadInt64(int address)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(GetSpan().Slice(address, 8));
        }

        /// <summary>
        /// Writes a long to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The long to write.</param>
        public void WriteInt64(int address, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(GetSpan().Slice(address, 8), value);
        }

        /// <summary>
        /// Reads an IntPtr from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the IntPtr read from memory.</returns>
        public IntPtr ReadIntPtr(int address)
        {
            if (IntPtr.Size == 4)
            {
                return (IntPtr)ReadInt32(address);
            }
            return (IntPtr)ReadInt64(address);
        }

        /// <summary>
        /// Writes an IntPtr to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The IntPtr to write.</param>
        public void WriteIntPtr(int address, IntPtr value)
        {
            if (IntPtr.Size == 4)
            {
                WriteInt32(address, value.ToInt32());
            }
            else
            {
                WriteInt64(address, value.ToInt64());
            }
        }

        /// <summary>
        /// Reads a single from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the single read from memory.</returns>
        public float ReadSingle(int address)
        {
            unsafe
            {
                var i = ReadInt32(address);
                return *((float*)&i);
            }
        }

        /// <summary>
        /// Writes a single to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The single to write.</param>
        public void WriteSingle(int address, float value)
        {
            unsafe
            {
                WriteInt32(address, *(int*)&value);
            }
        }

        /// <summary>
        /// Reads a double from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the double read from memory.</returns>
        public double ReadDouble(int address)
        {
            unsafe
            {
                var i = ReadInt64(address);
                return *((double*)&i);
            }
        }

        /// <summary>
        /// Writes a double to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The double to write.</param>
        public void WriteDouble(int address, double value)
        {
            unsafe
            {
                WriteInt64(address, *(long*)&value);
            }
        }

        /// <summary>
        /// Grows the memory by the specified number of pages.
        /// </summary>
        /// <param name="delta">The number of WebAssembly pages to grow the memory by.</param>
        /// <returns>Returns the previous size of the Webassembly memory, in pages.</returns>
        /// <remarks>This method will invalidate previously returned values from `GetSpan`.</remarks>
        public uint Grow(uint delta)
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
        internal Memory(IStore store, ExternMemory memory)
        {
            this.memory = memory;
            this.store = store;

            using var type = new TypeHandle(Native.wasmtime_memory_type(store.Context.handle, this.memory));

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

        private readonly IStore store;
        private readonly ExternMemory memory;
    }
}
