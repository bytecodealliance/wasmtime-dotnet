using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime.Components
{
    /// <summary>
    /// Discriminant for the variants of <see cref="ComponentValue"/>.
    /// </summary>
    /// <remarks>
    /// Mirrors the <c>WASMTIME_COMPONENT_*</c> constants in <c>wasmtime/component/val.h</c>.
    /// </remarks>
    public enum ComponentValueKind : byte
    {
        /// <summary>The value is a <see cref="bool"/>.</summary>
        Bool = 0,
        /// <summary>The value is a signed 8-bit integer.</summary>
        S8 = 1,
        /// <summary>The value is an unsigned 8-bit integer.</summary>
        U8 = 2,
        /// <summary>The value is a signed 16-bit integer.</summary>
        S16 = 3,
        /// <summary>The value is an unsigned 16-bit integer.</summary>
        U16 = 4,
        /// <summary>The value is a signed 32-bit integer.</summary>
        S32 = 5,
        /// <summary>The value is an unsigned 32-bit integer.</summary>
        U32 = 6,
        /// <summary>The value is a signed 64-bit integer.</summary>
        S64 = 7,
        /// <summary>The value is an unsigned 64-bit integer.</summary>
        U64 = 8,
        /// <summary>The value is a 32-bit float.</summary>
        F32 = 9,
        /// <summary>The value is a 64-bit float.</summary>
        F64 = 10,
        /// <summary>The value is a Unicode scalar value.</summary>
        Char = 11,
        /// <summary>The value is a string.</summary>
        String = 12,
        /// <summary>The value is a list.</summary>
        List = 13,
        /// <summary>The value is a record.</summary>
        Record = 14,
        /// <summary>The value is a tuple.</summary>
        Tuple = 15,
        /// <summary>The value is a variant.</summary>
        Variant = 16,
        /// <summary>The value is an enum.</summary>
        Enum = 17,
        /// <summary>The value is an option.</summary>
        Option = 18,
        /// <summary>The value is a result.</summary>
        Result = 19,
        /// <summary>The value is a flags set.</summary>
        Flags = 20,
    }

    /// <summary>
    /// Represents a single value passed to or returned from a component function.
    /// </summary>
    /// <remarks>
    /// Mirrors <c>wasmtime_component_val_t</c> for blittable interop. Composite values
    /// (currently <see cref="ComponentValueKind.String"/>) own a heap-allocated buffer
    /// when constructed by <c>From*</c> factories — call <see cref="Free"/> after use,
    /// preferably from a <c>finally</c> block.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentValue
    {
        // Verify the struct matches the C layout: 1 byte kind + 1 byte allocation flag + 6 bytes padding + 24 byte union = 32 bytes total.
        static ComponentValue() => Debug.Assert(Marshal.SizeOf(typeof(ComponentValue)) == 32);

        private byte kind;
        private byte ownsHeap;
        private byte _pad0;
        private byte _pad1;
        private byte _pad2;
        private byte _pad3;
        private byte _pad4;
        private byte _pad5;

        private WasmtimeComponentValUnion of;

        /// <summary>The discriminant indicating which alternative this value holds.</summary>
        public ComponentValueKind Kind => (ComponentValueKind)kind;

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.Bool"/>.</summary>
        public static ComponentValue FromBool(bool value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.Bool };
            v.of.Boolean = value ? (byte)1 : (byte)0;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.S8"/>.</summary>
        public static ComponentValue FromS8(sbyte value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.S8 };
            v.of.S8 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.U8"/>.</summary>
        public static ComponentValue FromU8(byte value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.U8 };
            v.of.U8 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.S16"/>.</summary>
        public static ComponentValue FromS16(short value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.S16 };
            v.of.S16 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.U16"/>.</summary>
        public static ComponentValue FromU16(ushort value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.U16 };
            v.of.U16 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.S32"/>.</summary>
        public static ComponentValue FromS32(int value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.S32 };
            v.of.S32 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.U32"/>.</summary>
        public static ComponentValue FromU32(uint value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.U32 };
            v.of.U32 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.S64"/>.</summary>
        public static ComponentValue FromS64(long value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.S64 };
            v.of.S64 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.U64"/>.</summary>
        public static ComponentValue FromU64(ulong value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.U64 };
            v.of.U64 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.F32"/>.</summary>
        public static ComponentValue FromF32(float value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.F32 };
            v.of.F32 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.F64"/>.</summary>
        public static ComponentValue FromF64(double value)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.F64 };
            v.of.F64 = value;
            return v;
        }

        /// <summary>Creates a value of kind <see cref="ComponentValueKind.Char"/> from a Unicode scalar value.</summary>
        public static ComponentValue FromChar(uint scalarValue)
        {
            var v = new ComponentValue { kind = (byte)ComponentValueKind.Char };
            v.of.Character = scalarValue;
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.String"/> by encoding <paramref name="value"/> as UTF-8.
        /// </summary>
        /// <remarks>
        /// The returned value owns a heap-allocated UTF-8 buffer. Call <see cref="Free"/> after use to release it.
        /// </remarks>
        public static ComponentValue FromString(string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var byteCount = Encoding.UTF8.GetByteCount(value);
            var ptr = byteCount == 0 ? IntPtr.Zero : Marshal.AllocHGlobal(byteCount);
            if (byteCount > 0)
            {
                unsafe
                {
                    fixed (char* chars = value)
                    {
                        Encoding.UTF8.GetBytes(chars, value.Length, (byte*)ptr, byteCount);
                    }
                }
            }

            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.String,
                ownsHeap = 1,
            };
            v.of.String = new WasmName { Size = (UIntPtr)byteCount, Data = ptr };
            return v;
        }

        /// <summary>Reads the value as <see cref="bool"/>; throws if <see cref="Kind"/> is not <see cref="ComponentValueKind.Bool"/>.</summary>
        public bool AsBool() { ExpectKind(ComponentValueKind.Bool); return of.Boolean != 0; }

        /// <summary>Reads the value as <see cref="sbyte"/>.</summary>
        public sbyte AsS8() { ExpectKind(ComponentValueKind.S8); return of.S8; }

        /// <summary>Reads the value as <see cref="byte"/>.</summary>
        public byte AsU8() { ExpectKind(ComponentValueKind.U8); return of.U8; }

        /// <summary>Reads the value as <see cref="short"/>.</summary>
        public short AsS16() { ExpectKind(ComponentValueKind.S16); return of.S16; }

        /// <summary>Reads the value as <see cref="ushort"/>.</summary>
        public ushort AsU16() { ExpectKind(ComponentValueKind.U16); return of.U16; }

        /// <summary>Reads the value as <see cref="int"/>.</summary>
        public int AsS32() { ExpectKind(ComponentValueKind.S32); return of.S32; }

        /// <summary>Reads the value as <see cref="uint"/>.</summary>
        public uint AsU32() { ExpectKind(ComponentValueKind.U32); return of.U32; }

        /// <summary>Reads the value as <see cref="long"/>.</summary>
        public long AsS64() { ExpectKind(ComponentValueKind.S64); return of.S64; }

        /// <summary>Reads the value as <see cref="ulong"/>.</summary>
        public ulong AsU64() { ExpectKind(ComponentValueKind.U64); return of.U64; }

        /// <summary>Reads the value as <see cref="float"/>.</summary>
        public float AsF32() { ExpectKind(ComponentValueKind.F32); return of.F32; }

        /// <summary>Reads the value as <see cref="double"/>.</summary>
        public double AsF64() { ExpectKind(ComponentValueKind.F64); return of.F64; }

        /// <summary>Reads the value as a Unicode scalar value.</summary>
        public uint AsChar() { ExpectKind(ComponentValueKind.Char); return of.Character; }

        /// <summary>Reads the value as <see cref="string"/>; the underlying UTF-8 bytes are decoded into a managed string.</summary>
        public string AsString()
        {
            ExpectKind(ComponentValueKind.String);
            var size = checked((int)(uint)of.String.Size);
            if (size == 0)
            {
                return string.Empty;
            }

            unsafe
            {
                return Encoding.UTF8.GetString((byte*)of.String.Data, size);
            }
        }

        /// <summary>
        /// Releases any heap-allocated payload associated with this value (currently strings).
        /// </summary>
        /// <remarks>
        /// Safe to call multiple times. Has no effect on values of primitive kinds or values not allocated
        /// by the managed factories. After <see cref="Free"/> the value's payload is no longer accessible.
        /// </remarks>
        public void Free()
        {
            if (ownsHeap == 0)
            {
                return;
            }

            switch ((ComponentValueKind)kind)
            {
                case ComponentValueKind.String:
                    if (of.String.Data != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(of.String.Data);
                        of.String = default;
                    }
                    break;
            }

            ownsHeap = 0;
        }

        private void ExpectKind(ComponentValueKind expected)
        {
            if (Kind != expected)
            {
                throw new InvalidOperationException($"ComponentValue is of kind '{Kind}', not '{expected}'.");
            }
        }
    }

    /// <summary>
    /// Mirror of <c>wasm_byte_vec_t</c> / <c>wasm_name_t</c> — used for strings, enum case names, and flag names.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WasmName
    {
        public UIntPtr Size;
        public IntPtr Data;
    }

    /// <summary>
    /// Mirror of the vec types <c>wasmtime_component_vallist_t</c>, <c>wasmtime_component_valtuple_t</c>,
    /// <c>wasmtime_component_valrecord_t</c>, and <c>wasmtime_component_valflags_t</c>. They share the same
    /// <c>{ size, data* }</c> layout — the element type differs but is always referenced by an opaque pointer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentValVec
    {
        public UIntPtr Size;
        public IntPtr Data;
    }

    /// <summary>
    /// Mirror of <c>wasmtime_component_valvariant_t</c>: a name discriminant plus an optional payload pointer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentValVariant
    {
        public WasmName Discriminant;
        public IntPtr Val;
    }

    /// <summary>
    /// Mirror of <c>wasmtime_component_valresult_t</c>: an <c>ok</c> flag plus an optional payload pointer.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentValResult
    {
        public byte IsOk;
        // Trailing padding to 8-byte alignment is implicit; matches the C struct's layout exactly.
        public IntPtr Val;
    }

    /// <summary>
    /// Mirror of <c>wasmtime_component_valunion_t</c>. The largest case (<c>variant</c>) drives the size: 24 bytes.
    /// All cases overlap at offset 0 — at most one is valid at any time, indicated by <see cref="ComponentValue.Kind"/>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct WasmtimeComponentValUnion
    {
        [FieldOffset(0)] public byte Boolean;
        [FieldOffset(0)] public sbyte S8;
        [FieldOffset(0)] public byte U8;
        [FieldOffset(0)] public short S16;
        [FieldOffset(0)] public ushort U16;
        [FieldOffset(0)] public int S32;
        [FieldOffset(0)] public uint U32;
        [FieldOffset(0)] public long S64;
        [FieldOffset(0)] public ulong U64;
        [FieldOffset(0)] public float F32;
        [FieldOffset(0)] public double F64;
        [FieldOffset(0)] public uint Character;
        [FieldOffset(0)] public WasmName String;
        [FieldOffset(0)] public ComponentValVec List;
        [FieldOffset(0)] public ComponentValVec Record;
        [FieldOffset(0)] public ComponentValVec Tuple;
        [FieldOffset(0)] public ComponentValVariant Variant;
        [FieldOffset(0)] public WasmName Enumeration;
        [FieldOffset(0)] public IntPtr Option;
        [FieldOffset(0)] public ComponentValResult Result;
        [FieldOffset(0)] public ComponentValVec Flags;
    }
}
