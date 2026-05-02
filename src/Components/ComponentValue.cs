using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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
    /// This struct mirrors <c>wasmtime_component_val_t</c> for blittable interop. Currently only
    /// primitive values (bool, integers, floats, char) are supported via the <c>From*</c> /
    /// <c>As*</c> helpers. Strings, lists, records, tuples, variants, enums, options, results,
    /// and flags will be wired up in subsequent commits as part of the marshalling phase.
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    public struct ComponentValue
    {
        // Verify the struct matches the C layout: 1 byte kind + 7 bytes padding + 24 byte union = 32 bytes total.
        static ComponentValue() => Debug.Assert(Marshal.SizeOf(typeof(ComponentValue)) == 32);

        private byte kind;
        private byte _pad0;
        private byte _pad1;
        private byte _pad2;
        private byte _pad3;
        private byte _pad4;
        private byte _pad5;
        private byte _pad6;

        private WasmtimeComponentValUnion of;

        /// <summary>
        /// The discriminant indicating which alternative this value holds.
        /// </summary>
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

        private void ExpectKind(ComponentValueKind expected)
        {
            if (Kind != expected)
            {
                throw new InvalidOperationException($"ComponentValue is of kind '{Kind}', not '{expected}'.");
            }
        }
    }

    /// <summary>
    /// Mirror of <c>wasmtime_component_valunion_t</c>. Largest case (<c>variant</c>) determines the size: 24 bytes.
    /// </summary>
    /// <remarks>
    /// Composite cases (string, list, record, tuple, variant, enum, option, result, flags) have their
    /// fields reserved by the explicit size of 24 bytes but C# accessors will be added in Phase 2 alongside
    /// the marshalling support.
    /// </remarks>
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
    }
}
