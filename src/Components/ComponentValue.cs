using System;
using System.Collections.Generic;
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

            var name = AllocateName(value);
            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.String,
                ownsHeap = 1,
            };
            v.of.String = name;
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Enum"/> with the given case name.
        /// </summary>
        /// <remarks>
        /// The returned value owns a heap-allocated UTF-8 buffer for the case name. Call <see cref="Free"/> after use.
        /// </remarks>
        public static ComponentValue FromEnum(string caseName)
        {
            if (caseName is null)
            {
                throw new ArgumentNullException(nameof(caseName));
            }

            var name = AllocateName(caseName);
            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Enum,
                ownsHeap = 1,
            };
            v.of.Enumeration = name;
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Flags"/> with the given set of flag names.
        /// </summary>
        /// <remarks>
        /// The returned value owns a heap-allocated array plus one buffer per flag name. Call <see cref="Free"/> after use.
        /// </remarks>
        public static ComponentValue FromFlags(IReadOnlyList<string> names)
        {
            if (names is null)
            {
                throw new ArgumentNullException(nameof(names));
            }

            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Flags,
                ownsHeap = 1,
            };
            v.of.Flags = AllocateNameArray(names);
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.List"/> from a sequence of elements.
        /// </summary>
        /// <remarks>
        /// Takes ownership of <paramref name="elements"/>: callers must not call <see cref="Free"/> on the
        /// individual elements afterwards. <see cref="Free"/> on the returned value releases the array and
        /// recursively frees each element.
        /// </remarks>
        public static ComponentValue FromList(IReadOnlyList<ComponentValue> elements)
        {
            if (elements is null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.List,
                ownsHeap = 1,
            };
            v.of.List = AllocateValueArray(elements);
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Tuple"/> from a sequence of elements.
        /// </summary>
        /// <remarks>
        /// Same ownership semantics as <see cref="FromList(System.Collections.Generic.IReadOnlyList{Wasmtime.Components.ComponentValue})"/>.
        /// </remarks>
        public static ComponentValue FromTuple(IReadOnlyList<ComponentValue> elements)
        {
            if (elements is null)
            {
                throw new ArgumentNullException(nameof(elements));
            }

            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Tuple,
                ownsHeap = 1,
            };
            v.of.Tuple = AllocateValueArray(elements);
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Record"/> from a sequence of named fields.
        /// </summary>
        /// <remarks>
        /// Takes ownership of the field values: callers must not call <see cref="Free"/> on
        /// <see cref="RecordField.Value"/> afterwards. <see cref="Free"/> on the returned value releases
        /// every name buffer and recursively frees every value.
        /// </remarks>
        public static ComponentValue FromRecord(IReadOnlyList<RecordField> fields)
        {
            if (fields is null)
            {
                throw new ArgumentNullException(nameof(fields));
            }

            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Record,
                ownsHeap = 1,
            };
            v.of.Record = AllocateRecordEntries(fields);
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Variant"/> with a case discriminant and an optional payload.
        /// </summary>
        /// <remarks>
        /// Takes ownership of <paramref name="payload"/> when supplied; do not call <see cref="Free"/> on it afterwards.
        /// </remarks>
        public static ComponentValue FromVariant(string discriminant, ComponentValue? payload = null)
        {
            if (discriminant is null)
            {
                throw new ArgumentNullException(nameof(discriminant));
            }

            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Variant,
                ownsHeap = 1,
            };
            v.of.Variant = new ComponentValVariant
            {
                Discriminant = AllocateName(discriminant),
                Val = AllocateValuePtr(payload),
            };
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Option"/>: <see langword="null"/> for <c>none</c>, otherwise <c>some(value)</c>.
        /// </summary>
        /// <remarks>Takes ownership of <paramref name="value"/> when supplied; do not call <see cref="Free"/> on it afterwards.</remarks>
        public static ComponentValue FromOption(ComponentValue? value)
        {
            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Option,
                ownsHeap = 1,
            };
            v.of.Option = AllocateValuePtr(value);
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Result"/> in the <c>ok</c> case, optionally carrying a payload.
        /// </summary>
        /// <remarks>Takes ownership of <paramref name="value"/> when supplied; do not call <see cref="Free"/> on it afterwards.</remarks>
        public static ComponentValue FromOk(ComponentValue? value = null)
        {
            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Result,
                ownsHeap = 1,
            };
            v.of.Result = new ComponentValResult
            {
                IsOk = 1,
                Val = AllocateValuePtr(value),
            };
            return v;
        }

        /// <summary>
        /// Creates a value of kind <see cref="ComponentValueKind.Result"/> in the <c>err</c> case, optionally carrying a payload.
        /// </summary>
        /// <remarks>Takes ownership of <paramref name="value"/> when supplied; do not call <see cref="Free"/> on it afterwards.</remarks>
        public static ComponentValue FromErr(ComponentValue? value = null)
        {
            var v = new ComponentValue
            {
                kind = (byte)ComponentValueKind.Result,
                ownsHeap = 1,
            };
            v.of.Result = new ComponentValResult
            {
                IsOk = 0,
                Val = AllocateValuePtr(value),
            };
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
            return DecodeName(of.String);
        }

        /// <summary>Reads an enum case name as a managed string.</summary>
        public string AsEnum()
        {
            ExpectKind(ComponentValueKind.Enum);
            return DecodeName(of.Enumeration);
        }

        /// <summary>Reads the set of flag names from a <see cref="ComponentValueKind.Flags"/> value.</summary>
        public IReadOnlyList<string> AsFlags()
        {
            ExpectKind(ComponentValueKind.Flags);
            var count = checked((int)(uint)of.Flags.Size);
            if (count == 0)
            {
                return System.Array.Empty<string>();
            }

            var result = new string[count];
            unsafe
            {
                var array = (WasmName*)of.Flags.Data;
                for (var i = 0; i < count; i++)
                {
                    result[i] = DecodeName(array[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Reads the elements of a <see cref="ComponentValueKind.List"/> value.
        /// </summary>
        /// <remarks>
        /// The returned values are shallow copies pointing at the same underlying buffers; do not call
        /// <see cref="Free"/> on them — call it on the parent list value instead.
        /// </remarks>
        public IReadOnlyList<ComponentValue> AsList()
        {
            ExpectKind(ComponentValueKind.List);
            return DecodeValueArray(of.List);
        }

        /// <summary>Reads the elements of a <see cref="ComponentValueKind.Tuple"/> value.</summary>
        /// <remarks>Shares ownership rules with <see cref="AsList"/>.</remarks>
        public IReadOnlyList<ComponentValue> AsTuple()
        {
            ExpectKind(ComponentValueKind.Tuple);
            return DecodeValueArray(of.Tuple);
        }

        /// <summary>Reads the discriminant of a <see cref="ComponentValueKind.Variant"/> value.</summary>
        public string AsVariantDiscriminant()
        {
            ExpectKind(ComponentValueKind.Variant);
            return DecodeName(of.Variant.Discriminant);
        }

        /// <summary>Reads the optional payload of a <see cref="ComponentValueKind.Variant"/> value, or <see langword="null"/> if the case has no payload.</summary>
        public ComponentValue? AsVariantPayload()
        {
            ExpectKind(ComponentValueKind.Variant);
            return DecodeValuePtr(of.Variant.Val);
        }

        /// <summary>Indicates whether an <see cref="ComponentValueKind.Option"/> value carries a <c>some</c> payload.</summary>
        public bool HasOption()
        {
            ExpectKind(ComponentValueKind.Option);
            return of.Option != IntPtr.Zero;
        }

        /// <summary>Reads the optional payload of an <see cref="ComponentValueKind.Option"/> value; <see langword="null"/> for <c>none</c>.</summary>
        public ComponentValue? AsOption()
        {
            ExpectKind(ComponentValueKind.Option);
            return DecodeValuePtr(of.Option);
        }

        /// <summary>Indicates whether a <see cref="ComponentValueKind.Result"/> value is in the <c>ok</c> case.</summary>
        public bool IsOk()
        {
            ExpectKind(ComponentValueKind.Result);
            return of.Result.IsOk != 0;
        }

        /// <summary>Reads the optional payload of a <see cref="ComponentValueKind.Result"/> value; <see langword="null"/> if the case has no payload.</summary>
        public ComponentValue? AsResultValue()
        {
            ExpectKind(ComponentValueKind.Result);
            return DecodeValuePtr(of.Result.Val);
        }

        /// <summary>Reads the named fields of a <see cref="ComponentValueKind.Record"/> value.</summary>
        /// <remarks>The returned values share ownership with the parent — do not Free them individually.</remarks>
        public IReadOnlyList<RecordField> AsRecord()
        {
            ExpectKind(ComponentValueKind.Record);
            var n = checked((int)(uint)of.Record.Size);
            if (n == 0)
            {
                return System.Array.Empty<RecordField>();
            }

            var result = new RecordField[n];
            unsafe
            {
                var entries = (ComponentValRecordEntry*)of.Record.Data;
                for (var i = 0; i < n; i++)
                {
                    result[i] = new RecordField(DecodeName(entries[i].Name), entries[i].Val);
                }
            }
            return result;
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
                    FreeName(ref of.String);
                    break;
                case ComponentValueKind.Enum:
                    FreeName(ref of.Enumeration);
                    break;
                case ComponentValueKind.Flags:
                    FreeNameArray(ref of.Flags);
                    break;
                case ComponentValueKind.List:
                    FreeValueArray(ref of.List);
                    break;
                case ComponentValueKind.Tuple:
                    FreeValueArray(ref of.Tuple);
                    break;
                case ComponentValueKind.Record:
                    FreeRecordEntries(ref of.Record);
                    break;
                case ComponentValueKind.Variant:
                    FreeName(ref of.Variant.Discriminant);
                    FreeValuePtr(of.Variant.Val);
                    of.Variant.Val = IntPtr.Zero;
                    break;
                case ComponentValueKind.Option:
                    FreeValuePtr(of.Option);
                    of.Option = IntPtr.Zero;
                    break;
                case ComponentValueKind.Result:
                    FreeValuePtr(of.Result.Val);
                    of.Result = default;
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

        private static WasmName AllocateName(string value)
        {
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

            return new WasmName { Size = (UIntPtr)byteCount, Data = ptr };
        }

        private static string DecodeName(WasmName name)
        {
            var size = checked((int)(uint)name.Size);
            if (size == 0)
            {
                return string.Empty;
            }

            unsafe
            {
                return Encoding.UTF8.GetString((byte*)name.Data, size);
            }
        }

        private static void FreeName(ref WasmName name)
        {
            if (name.Data != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(name.Data);
                name = default;
            }
        }

        private static unsafe ComponentValVec AllocateNameArray(IReadOnlyList<string> names)
        {
            var n = names.Count;
            if (n == 0)
            {
                return new ComponentValVec { Size = UIntPtr.Zero, Data = IntPtr.Zero };
            }

            var elementSize = sizeof(WasmName);
            var arrayPtr = Marshal.AllocHGlobal(n * elementSize);
            var array = (WasmName*)arrayPtr;
            for (var i = 0; i < n; i++)
            {
                if (names[i] is null)
                {
                    // Roll back already-allocated entries.
                    for (var j = 0; j < i; j++)
                    {
                        if (array[j].Data != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(array[j].Data);
                        }
                    }
                    Marshal.FreeHGlobal(arrayPtr);
                    throw new ArgumentException("Flag names must not be null.", nameof(names));
                }

                array[i] = AllocateName(names[i]);
            }

            return new ComponentValVec { Size = (UIntPtr)n, Data = arrayPtr };
        }

        private static unsafe void FreeNameArray(ref ComponentValVec vec)
        {
            if (vec.Data == IntPtr.Zero)
            {
                vec = default;
                return;
            }

            var n = checked((int)(uint)vec.Size);
            var array = (WasmName*)vec.Data;
            for (var i = 0; i < n; i++)
            {
                if (array[i].Data != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(array[i].Data);
                }
            }

            Marshal.FreeHGlobal(vec.Data);
            vec = default;
        }

        private static unsafe ComponentValVec AllocateValueArray(IReadOnlyList<ComponentValue> elements)
        {
            var n = elements.Count;
            if (n == 0)
            {
                return new ComponentValVec { Size = UIntPtr.Zero, Data = IntPtr.Zero };
            }

            var elementSize = sizeof(ComponentValue);
            var arrayPtr = Marshal.AllocHGlobal(n * elementSize);
            var array = (ComponentValue*)arrayPtr;
            for (var i = 0; i < n; i++)
            {
                array[i] = elements[i];
            }

            return new ComponentValVec { Size = (UIntPtr)n, Data = arrayPtr };
        }

        private static unsafe ComponentValue[] DecodeValueArray(ComponentValVec vec)
        {
            var n = checked((int)(uint)vec.Size);
            if (n == 0)
            {
                return System.Array.Empty<ComponentValue>();
            }

            var result = new ComponentValue[n];
            var array = (ComponentValue*)vec.Data;
            for (var i = 0; i < n; i++)
            {
                result[i] = array[i];
            }
            return result;
        }

        private static unsafe void FreeValueArray(ref ComponentValVec vec)
        {
            if (vec.Data == IntPtr.Zero)
            {
                vec = default;
                return;
            }

            var n = checked((int)(uint)vec.Size);
            var array = (ComponentValue*)vec.Data;
            for (var i = 0; i < n; i++)
            {
                array[i].Free();
            }

            Marshal.FreeHGlobal(vec.Data);
            vec = default;
        }

        private static unsafe ComponentValVec AllocateRecordEntries(IReadOnlyList<RecordField> fields)
        {
            var n = fields.Count;
            if (n == 0)
            {
                return new ComponentValVec { Size = UIntPtr.Zero, Data = IntPtr.Zero };
            }

            var entrySize = sizeof(ComponentValRecordEntry);
            var arrayPtr = Marshal.AllocHGlobal(n * entrySize);
            var entries = (ComponentValRecordEntry*)arrayPtr;
            for (var i = 0; i < n; i++)
            {
                if (fields[i].Name is null)
                {
                    for (var j = 0; j < i; j++)
                    {
                        FreeName(ref entries[j].Name);
                        entries[j].Val.Free();
                    }
                    Marshal.FreeHGlobal(arrayPtr);
                    throw new ArgumentException("Record field name must not be null.", nameof(fields));
                }

                entries[i].Name = AllocateName(fields[i].Name);
                entries[i].Val = fields[i].Value;
            }

            return new ComponentValVec { Size = (UIntPtr)n, Data = arrayPtr };
        }

        private static unsafe void FreeRecordEntries(ref ComponentValVec vec)
        {
            if (vec.Data == IntPtr.Zero)
            {
                vec = default;
                return;
            }

            var n = checked((int)(uint)vec.Size);
            var entries = (ComponentValRecordEntry*)vec.Data;
            for (var i = 0; i < n; i++)
            {
                FreeName(ref entries[i].Name);
                entries[i].Val.Free();
            }

            Marshal.FreeHGlobal(vec.Data);
            vec = default;
        }

        private static unsafe IntPtr AllocateValuePtr(ComponentValue? value)
        {
            if (value is null)
            {
                return IntPtr.Zero;
            }

            var ptr = Marshal.AllocHGlobal(sizeof(ComponentValue));
            *(ComponentValue*)ptr = value.Value;
            return ptr;
        }

        private static unsafe ComponentValue? DecodeValuePtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return null;
            }

            return *(ComponentValue*)ptr;
        }

        private static unsafe void FreeValuePtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return;
            }

            ((ComponentValue*)ptr)->Free();
            Marshal.FreeHGlobal(ptr);
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
    /// Mirror of <c>wasmtime_component_valrecord_entry_t</c>: a name and the value associated with it.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ComponentValRecordEntry
    {
        public WasmName Name;
        public ComponentValue Val;
    }

    /// <summary>
    /// A single named field within a record value.
    /// </summary>
    public readonly record struct RecordField(string Name, ComponentValue Value);

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
