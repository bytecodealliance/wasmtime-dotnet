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
        /// <param name="maximum">The maximum number of WebAssembly pages, or <c>null</c> to not specify a maximum.</param>
        /// <param name="is64Bit"><c>true</c> when memory type represents a 64-bit memory, <c>false</c> when it represents a 32-bit memory.</param>
        public Memory(Store store, long minimum = 0, long? maximum = null, bool is64Bit = false)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (minimum < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum));
            }

            if (maximum < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maximum));
            }

            if (maximum < minimum)
            {
                throw new ArgumentException("The maximum cannot be less than the minimum.", nameof(maximum));
            }

            this.store = store;
            Minimum = minimum;
            Maximum = maximum;
            Is64Bit = is64Bit;

            using var type = new TypeHandle(Native.wasmtime_memorytype_new((ulong)minimum, maximum is not null, (ulong)(maximum ?? 0), is64Bit));

            var error = Native.wasmtime_memory_new(store.Context.handle, type, out this.memory);
            GC.KeepAlive(store);

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// The size, in bytes, of a WebAssembly memory page.
        /// </summary>
        public const int PageSize = 65536;

        /// <summary>
        /// Gets the minimum memory size (in WebAssembly page units).
        /// </summary>
        /// <value>The minimum memory size (in WebAssembly page units).</value>
        public long Minimum { get; }

        /// <summary>
        /// Gets the maximum memory size (in WebAssembly page units).
        /// </summary>
        /// <value>The maximum memory size (in WebAssembly page units), or <c>null</c> if no maximum is specified.</value>
        public long? Maximum { get; }

        /// <summary>
        /// Gets a value that indicates whether this type of memory represents a 64-bit memory.
        /// </summary>
        /// <value><c>true</c> if this type of memory represents a 64-bit memory, <c>false</c> otherwise.</value>
        public bool Is64Bit { get; }

        /// <summary>
        /// Gets the current size of the memory, in WebAssembly page units.
        /// </summary>
        /// <returns>Returns the current size of the memory, in WebAssembly page units.</returns>
        public long GetSize()
        {
            var size = (long)Native.wasmtime_memory_size(store.Context.handle, this.memory);
            GC.KeepAlive(store);
            return size;
        }

        /// <summary>
        /// Gets the current length of the memory, in bytes.
        /// </summary>
        /// <returns>Returns the current length of the memory, in bytes.</returns>
        public long GetLength()
        {
            var length = checked((long)Native.wasmtime_memory_data_size(store.Context.handle, this.memory));
            GC.KeepAlive(store);
            return length;
        }

        /// <summary>
        /// Returns a pointer to the start of the memory. The length for which the pointer
        /// is valid can be retrieved with <see cref="GetLength"/>.
        /// </summary>
        /// <returns>Returns a pointer to the start of the memory.</returns>
        /// <remarks>
        /// <para>
        /// The pointer may become invalid if the memory grows.
        ///
        /// This may happen if the memory is explicitly requested to grow or
        /// grows as a result of WebAssembly execution.
        /// </para>
        /// <para>
        /// Therefore, the returned pointer should not be used after calling the grow method or
        /// after calling into WebAssembly code.
        /// </para>
        /// </remarks>
        public unsafe IntPtr GetPointer()
        {
            var data = Native.wasmtime_memory_data(store.Context.handle, this.memory);
            GC.KeepAlive(store);
            return (nint)data;
        }

        /// <summary>
        /// Gets the span of the memory.
        /// </summary>
        /// <returns>Returns the span of the memory.</returns>
        /// <exception cref="OverflowException">The memory has more than 32767 pages.</exception>
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
        /// </remarks>
        [Obsolete("This method will throw an OverflowException if the memory has more than 32767 pages. " +
            "Use the " + nameof(GetSpan) + " overload taking an address and a length.")]
        public Span<byte> GetSpan()
        {
            return GetSpan(0, checked((int)GetLength()));
        }

        /// <summary>
        /// Gets a span of a section of the memory.
        /// </summary>
        /// <returns>Returns the span of a section of the memory.</returns>
        /// <param name="address">The zero-based address of the start of the span.</param>
        /// <param name="length">The length of the span.</param>
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
        /// </remarks>
        public Span<byte> GetSpan(long address, int length)
        {
            return GetSpan<byte>(address, length);
        }

        /// <summary>
        /// Gets the span of the memory viewed as a specific type, starting at a given address.
        /// </summary>
        /// <param name="address">The zero-based address of the start of the span.</param>
        /// <returns>Returns the span of the memory.</returns>
        /// <exception cref="OverflowException">The memory exceeds the byte length that can be 
        /// represented by a <see cref="Span{T}"/>.</exception>
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
        public unsafe Span<T> GetSpan<T>(int address)
            where T : unmanaged
        {
            return GetSpan<T>(address, checked((int)((GetLength() - address) / sizeof(T))));
        }

        /// <summary>
        /// Gets a span of a section of the memory.
        /// </summary>
        /// <returns>Returns the span of a section of the memory.</returns>
        /// <param name="address">The zero-based address of the start of the span.</param>
        /// <param name="length">The length of the span.</param>
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
        public unsafe Span<T> GetSpan<T>(long address, int length)
            where T : unmanaged
        {
            if (address < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(address));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length));
            }

            var context = store.Context;
            var data = Native.wasmtime_memory_data(context.handle, this.memory);
            GC.KeepAlive(store);

            var memoryLength = this.GetLength();

            // Note: A Span<T> can span more than 2 GiB bytes if sizeof(T) > 1.
            long byteLength = (long)length * sizeof(T);

            if (address > memoryLength - byteLength)
            {
                throw new ArgumentException("The specified address and length exceed the Memory's bounds.");
            }

            return new Span<T>((T*)(data + address), length);
        }

        /// <summary>
        /// Read a struct from memory.
        /// </summary>
        /// <typeparam name="T">Type of the struct to read. Ensure layout in C# is identical to layout in WASM.</typeparam>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the struct read from memory.</returns>
        /// <remarks>
        /// <para>
        /// Note that WebAssembly always uses little endian as byte order. On platforms 
        /// that use big endian, you will need to convert numeric values accordingly.
        /// </para>
        /// </remarks>
        public T Read<T>(long address)
            where T : unmanaged
        {
            return GetSpan<T>(address, 1)[0];
        }

        /// <summary>
        /// Write a struct to memory.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="address">The zero-based address to read from.</param>
        /// <param name="value">The struct to write.</param>
        /// <remarks>
        /// <para>
        /// Note that WebAssembly always uses little endian as byte order. On platforms 
        /// that use big endian, you will need to convert numeric values accordingly.
        /// </para>
        /// </remarks>
        public void Write<T>(long address, T value)
            where T : unmanaged
        {
            GetSpan<T>(address, 1)[0] = value;
        }

        /// <summary>
        /// Reads a string from memory with the specified encoding.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <param name="length">The length of bytes to read.</param>
        /// <param name="encoding">The encoding to use when reading the string; if null, UTF-8 encoding is used.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadString(long address, int length, Encoding? encoding = null)
        {
            if (encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetString(GetSpan(address, length));
        }

        /// <summary>
        /// Reads a null-terminated UTF-8 string from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the string read from memory.</returns>
        public string ReadNullTerminatedString(long address)
        {
            if (address < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(address));
            }

            // We can only read a maximum of 2 GiB.
            var slice = GetSpan(address, (int)Math.Min(int.MaxValue, GetLength() - address));
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
        public int WriteString(long address, string value, Encoding? encoding = null)
        {
            if (address < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(address));
            }

            if (encoding is null)
            {
                encoding = Encoding.UTF8;
            }

            return encoding.GetBytes(value, GetSpan(address, (int)Math.Min(int.MaxValue, GetLength() - address)));
        }

        /// <summary>
        /// Reads a byte from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the byte read from memory.</returns>
        public byte ReadByte(long address)
        {
            return GetSpan(address, sizeof(byte))[0];
        }

        /// <summary>
        /// Writes a byte to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The byte to write.</param>
        public void WriteByte(long address, byte value)
        {
            GetSpan(address, sizeof(byte))[0] = value;
        }

        /// <summary>
        /// Reads a short from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the short read from memory.</returns>
        public short ReadInt16(long address)
        {
            return BinaryPrimitives.ReadInt16LittleEndian(GetSpan(address, sizeof(short)));
        }

        /// <summary>
        /// Writes a short to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The short to write.</param>
        public void WriteInt16(long address, short value)
        {
            BinaryPrimitives.WriteInt16LittleEndian(GetSpan(address, sizeof(short)), value);
        }

        /// <summary>
        /// Reads an int from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the int read from memory.</returns>
        public int ReadInt32(long address)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(GetSpan(address, sizeof(int)));
        }

        /// <summary>
        /// Writes an int to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The int to write.</param>
        public void WriteInt32(long address, int value)
        {
            BinaryPrimitives.WriteInt32LittleEndian(GetSpan(address, sizeof(int)), value);
        }

        /// <summary>
        /// Reads a long from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the long read from memory.</returns>
        public long ReadInt64(long address)
        {
            return BinaryPrimitives.ReadInt64LittleEndian(GetSpan(address, sizeof(long)));
        }

        /// <summary>
        /// Writes a long to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The long to write.</param>
        public void WriteInt64(long address, long value)
        {
            BinaryPrimitives.WriteInt64LittleEndian(GetSpan(address, sizeof(long)), value);
        }

        /// <summary>
        /// Reads an IntPtr from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the IntPtr read from memory.</returns>
        public IntPtr ReadIntPtr(long address)
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
        public void WriteIntPtr(long address, IntPtr value)
        {
            if (IntPtr.Size == 4)
            {
                WriteInt32(address, (int)value);
            }
            else
            {
                WriteInt64(address, (long)value);
            }
        }

        /// <summary>
        /// Reads a single from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the single read from memory.</returns>
        public float ReadSingle(long address)
        {
            return BitConverter.Int32BitsToSingle(ReadInt32(address));
        }

        /// <summary>
        /// Writes a single to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The single to write.</param>
        public void WriteSingle(long address, float value)
        {
            WriteInt32(address, BitConverter.SingleToInt32Bits(value));
        }

        /// <summary>
        /// Reads a double from memory.
        /// </summary>
        /// <param name="address">The zero-based address to read from.</param>
        /// <returns>Returns the double read from memory.</returns>
        public double ReadDouble(long address)
        {
            return BitConverter.Int64BitsToDouble(ReadInt64(address));
        }

        /// <summary>
        /// Writes a double to memory.
        /// </summary>
        /// <param name="address">The zero-based address to write to.</param>
        /// <param name="value">The double to write.</param>
        public void WriteDouble(long address, double value)
        {
            WriteInt64(address, BitConverter.DoubleToInt64Bits(value));
        }

        /// <summary>
        /// Grows the memory by the specified number of pages.
        /// </summary>
        /// <param name="delta">The number of WebAssembly pages to grow the memory by.</param>
        /// <returns>Returns the previous size of the Webassembly memory, in pages.</returns>
        /// <remarks>This method will invalidate previously returned values from `GetSpan`.</remarks>
        public long Grow(long delta)
        {
            if (delta < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(delta));
            }

            var error = Native.wasmtime_memory_grow(store.Context.handle, this.memory, (ulong)delta, out var prev);
            GC.KeepAlive(store);

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return (long)prev;
        }

        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Memory,
                of = new ExternUnion { memory = this.memory }
            };
        }
        internal Memory(Store store, ExternMemory memory)
        {
            this.memory = memory;
            this.store = store;

            using var type = new TypeHandle(Native.wasmtime_memory_type(store.Context.handle, this.memory));
            GC.KeepAlive(store);

            Minimum = (long)Native.wasmtime_memorytype_minimum(type.DangerousGetHandle());

            if (Native.wasmtime_memorytype_maximum(type.DangerousGetHandle(), out ulong max))
            {
                Maximum = (long)max;
            }

            Is64Bit = Native.wasmtime_memorytype_is64(type.DangerousGetHandle());
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
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memory_new(IntPtr context, TypeHandle type, out ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern byte* wasmtime_memory_data(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern nuint wasmtime_memory_data_size(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern ulong wasmtime_memory_size(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memory_grow(IntPtr context, in ExternMemory memory, ulong delta, out ulong prev);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memory_type(IntPtr context, in ExternMemory memory);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_memorytype_new(ulong min, [MarshalAs(UnmanagedType.I1)] bool max_present, ulong max, [MarshalAs(UnmanagedType.I1)] bool is_64);

            [DllImport(Engine.LibraryName)]
            public static extern ulong wasmtime_memorytype_minimum(IntPtr type);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_memorytype_maximum(IntPtr type, out ulong max);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_memorytype_is64(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_memorytype_delete(IntPtr handle);
        }

        private readonly Store store;
        private readonly ExternMemory memory;
    }
}
