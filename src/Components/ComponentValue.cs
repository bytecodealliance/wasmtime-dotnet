using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Wasmtime.Components;

/// <summary>
/// The type discriminant of a <see cref="ComponentValue"/>, mirroring
/// <c>wasmtime_component_valkind_t</c>.
/// </summary>
public enum ComponentValueKind : byte
{
    /// <summary>A <c>bool</c>.</summary>
    Bool = 0,
    /// <summary>A signed 8-bit integer.</summary>
    S8 = 1,
    /// <summary>An unsigned 8-bit integer.</summary>
    U8 = 2,
    /// <summary>A signed 16-bit integer.</summary>
    S16 = 3,
    /// <summary>An unsigned 16-bit integer.</summary>
    U16 = 4,
    /// <summary>A signed 32-bit integer.</summary>
    S32 = 5,
    /// <summary>An unsigned 32-bit integer.</summary>
    U32 = 6,
    /// <summary>A signed 64-bit integer.</summary>
    S64 = 7,
    /// <summary>An unsigned 64-bit integer.</summary>
    U64 = 8,
    /// <summary>A 32-bit IEEE-754 float.</summary>
    F32 = 9,
    /// <summary>A 64-bit IEEE-754 float.</summary>
    F64 = 10,
    /// <summary>A Unicode scalar value.</summary>
    Char = 11,
    /// <summary>A UTF-8 string.</summary>
    String = 12,
    /// <summary>A homogeneous list.</summary>
    List = 13,
    /// <summary>A record of named fields.</summary>
    Record = 14,
    /// <summary>A tuple of positional values.</summary>
    Tuple = 15,
    /// <summary>A named variant case with an optional payload.</summary>
    Variant = 16,
    /// <summary>An enumeration, identified by case name.</summary>
    Enum = 17,
    /// <summary>An optional value.</summary>
    Option = 18,
    /// <summary>A result value.</summary>
    Result = 19,
    /// <summary>A set of flag names.</summary>
    Flags = 20,
    /// <summary>A resource handle. Not yet supported by this binding.</summary>
    Resource = 21,
    /// <summary>A map. Not yet supported by this binding.</summary>
    Map = 22,
}

/// <summary>
/// A value which a component function can consume or produce, mirroring
/// <c>wasmtime_component_val_t</c>.
/// </summary>
/// <remarks>
/// Create values with the static factory methods and read them back with the typed accessors,
/// each of which throws if the value has a different <see cref="Kind"/>. The
/// <see cref="ComponentValueKind.Resource"/> and
/// <see cref="ComponentValueKind.Map"/> kinds are not yet supported.
/// </remarks>
public sealed class ComponentValue
{
    private ComponentValue(ComponentValueKind kind)
    {
        Kind = kind;
    }

    /// <summary>
    /// Gets the type discriminant of this value.
    /// </summary>
    public ComponentValueKind Kind { get; }

    /// <summary>Backing store for the integral kinds and for <see cref="ComponentValueKind.Bool"/>.</summary>
    internal long Integer { get; private set; }

    /// <summary>Backing store for the floating-point kinds.</summary>
    internal double Real { get; private set; }

    /// <summary>String contents, or the case name of an enum.</summary>
    internal string? Text { get; private set; }

    /// <summary>Elements of a list or tuple.</summary>
    internal IReadOnlyList<ComponentValue> Items { get; private set; } = Array.Empty<ComponentValue>();

    internal IReadOnlyList<string> Names { get; private set; } = Array.Empty<string>();

    /// <summary>Fields of a record, in declaration order.</summary>
    internal IReadOnlyList<KeyValuePair<string, ComponentValue>> Fields { get; private set; } =
        Array.Empty<KeyValuePair<string, ComponentValue>>();

    /// <summary>True for <c>some</c> options and <c>ok</c> results.</summary>
    internal bool Flag { get; private set; }

    /// <summary>
    /// Gets the payload of an <see cref="ComponentValueKind.Option"/> or
    /// <see cref="ComponentValueKind.Result"/> or <see cref="ComponentValueKind.Variant"/>,
    /// or <c>null</c> when there is none.
    /// </summary>
    public ComponentValue? Payload { get; private set; }

    /// <summary>
    /// Gets whether this <see cref="ComponentValueKind.Option"/> carries a value.
    /// </summary>
    public bool IsSome => Expect(ComponentValueKind.Option, Flag);

    /// <summary>
    /// Gets whether this <see cref="ComponentValueKind.Result"/> is the <c>ok</c> case.
    /// </summary>
    public bool IsOk => Expect(ComponentValueKind.Result, Flag);

    /// <summary>Creates a <c>bool</c> value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue Bool(bool value) =>
        new ComponentValue(ComponentValueKind.Bool) { Integer = value ? 1 : 0 };

    /// <summary>Creates a signed 8-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue S8(sbyte value) =>
        new ComponentValue(ComponentValueKind.S8) { Integer = value };

    /// <summary>Creates an unsigned 8-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue U8(byte value) =>
        new ComponentValue(ComponentValueKind.U8) { Integer = value };

    /// <summary>Creates a signed 16-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue S16(short value) =>
        new ComponentValue(ComponentValueKind.S16) { Integer = value };

    /// <summary>Creates an unsigned 16-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue U16(ushort value) =>
        new ComponentValue(ComponentValueKind.U16) { Integer = value };

    /// <summary>Creates a signed 32-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue S32(int value) =>
        new ComponentValue(ComponentValueKind.S32) { Integer = value };

    /// <summary>Creates an unsigned 32-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue U32(uint value) =>
        new ComponentValue(ComponentValueKind.U32) { Integer = value };

    /// <summary>Creates a signed 64-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue S64(long value) =>
        new ComponentValue(ComponentValueKind.S64) { Integer = value };

    /// <summary>Creates an unsigned 64-bit integer value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue U64(ulong value) =>
        new ComponentValue(ComponentValueKind.U64) { Integer = unchecked((long)value) };

    /// <summary>Creates a 32-bit float value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue F32(float value) =>
        new ComponentValue(ComponentValueKind.F32) { Real = value };

    /// <summary>Creates a 64-bit float value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue F64(double value) =>
        new ComponentValue(ComponentValueKind.F64) { Real = value };

    /// <summary>Creates a Unicode scalar value, not a UTF-16 code unit.</summary>
    /// <param name="value">A value from 0 to 0x10FFFF, excluding surrogates.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for an invalid scalar.</exception>
    public static ComponentValue Char(uint value)
    {
        if (value > 0x10FFFF || (value >= 0xD800 && value <= 0xDFFF))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "Expected a Unicode scalar value.");
        }

        return new ComponentValue(ComponentValueKind.Char) { Integer = value };
    }

    /// <summary>Creates a Unicode scalar value from a UTF-16 code unit.</summary>
    /// <param name="value">A UTF-16 code unit that is not a surrogate.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for a surrogate code unit.</exception>
    public static ComponentValue Char(char value) => Char((uint)value);

#if NETCOREAPP3_0_OR_GREATER
    /// <summary>Creates a Unicode scalar value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue Char(System.Text.Rune value) =>
        new ComponentValue(ComponentValueKind.Char) { Integer = value.Value };
#endif

    /// <summary>Creates a named variant case.</summary>
    /// <param name="caseName">The WIT case name.</param>
    /// <param name="value">The optional payload.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue Variant(string caseName, ComponentValue? value = null)
    {
        if (caseName is null)
        {
            throw new ArgumentNullException(nameof(caseName));
        }

        return new ComponentValue(ComponentValueKind.Variant) { Text = caseName, Payload = value };
    }

    /// <summary>Creates a set of flag names, copying the input.</summary>
    /// <param name="names">The enabled WIT flag names, with no nulls or duplicates.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentException">Thrown for a null or duplicate name.</exception>
    public static ComponentValue Flags(IReadOnlyList<string> names)
    {
        if (names is null)
        {
            throw new ArgumentNullException(nameof(names));
        }

        var copy = Copy(names);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var name in copy)
        {
            if (name is null || !seen.Add(name))
            {
                throw new ArgumentException("Flag names must not be null or duplicated.", nameof(names));
            }
        }

        return new ComponentValue(ComponentValueKind.Flags) { Names = Array.AsReadOnly(copy) };
    }

    /// <summary>Creates a string value.</summary>
    /// <param name="value">The value.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static ComponentValue String(string value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ComponentValue(ComponentValueKind.String) { Text = value };
    }

    /// <summary>Creates an enumeration value.</summary>
    /// <param name="caseName">The name of the enum case, as declared in WIT.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="caseName"/> is null.</exception>
    /// <remarks>Component model enums travel by case name rather than by ordinal.</remarks>
    public static ComponentValue Enum(string caseName)
    {
        if (caseName is null)
        {
            throw new ArgumentNullException(nameof(caseName));
        }

        return new ComponentValue(ComponentValueKind.Enum) { Text = caseName };
    }

    /// <summary>Creates a list value.</summary>
    /// <param name="items">The elements of the list, which are copied.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null.</exception>
    public static ComponentValue List(IReadOnlyList<ComponentValue> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        return new ComponentValue(ComponentValueKind.List) { Items = Copy(items) };
    }

    /// <summary>Creates a tuple value.</summary>
    /// <param name="items">The elements of the tuple, which are copied.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="items"/> is null.</exception>
    public static ComponentValue Tuple(IReadOnlyList<ComponentValue> items)
    {
        if (items is null)
        {
            throw new ArgumentNullException(nameof(items));
        }

        return new ComponentValue(ComponentValueKind.Tuple) { Items = Copy(items) };
    }

    /// <summary>Creates a record value.</summary>
    /// <param name="fields">The fields of the record in declaration order, which are copied.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="fields"/> is null.</exception>
    public static ComponentValue Record(IReadOnlyList<KeyValuePair<string, ComponentValue>> fields)
    {
        if (fields is null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        return new ComponentValue(ComponentValueKind.Record) { Fields = Copy(fields) };
    }

    // The Owned* factories take ownership instead of copying, for lists nothing else references.
    internal static ComponentValue OwnedList(List<ComponentValue> items) =>
        new ComponentValue(ComponentValueKind.List) { Items = items };

    internal static ComponentValue OwnedTuple(List<ComponentValue> items) =>
        new ComponentValue(ComponentValueKind.Tuple) { Items = items };

    internal static ComponentValue OwnedRecord(List<KeyValuePair<string, ComponentValue>> fields) =>
        new ComponentValue(ComponentValueKind.Record) { Fields = fields };

    private static T[] Copy<T>(IReadOnlyList<T> source)
    {
        if (source.Count == 0)
        {
            return Array.Empty<T>();
        }

        var copy = new T[source.Count];
        for (var i = 0; i < copy.Length; i++)
        {
            copy[i] = source[i];
        }

        return copy;
    }

    /// <summary>Creates an option value carrying a payload.</summary>
    /// <param name="value">The payload.</param>
    /// <returns>The component value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static ComponentValue Some(ComponentValue value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        return new ComponentValue(ComponentValueKind.Option) { Payload = value, Flag = true };
    }

    /// <summary>Creates an empty option value.</summary>
    /// <returns>The component value.</returns>
    public static ComponentValue None() =>
        new ComponentValue(ComponentValueKind.Option) { Flag = false };

    /// <summary>Creates the <c>ok</c> case of a result.</summary>
    /// <param name="value">The payload, or null for a result with no <c>ok</c> type.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue Ok(ComponentValue? value = null) =>
        new ComponentValue(ComponentValueKind.Result) { Payload = value, Flag = true };

    /// <summary>Creates the <c>err</c> case of a result.</summary>
    /// <param name="value">The payload, or null for a result with no <c>err</c> type.</param>
    /// <returns>The component value.</returns>
    public static ComponentValue Err(ComponentValue? value = null) =>
        new ComponentValue(ComponentValueKind.Result) { Payload = value, Flag = false };

    /// <summary>Gets the value as a <c>bool</c>.</summary>
    /// <returns>The value.</returns>
    public bool AsBool() => Expect(ComponentValueKind.Bool, Integer != 0);

    /// <summary>Gets the value as a signed 8-bit integer.</summary>
    /// <returns>The value.</returns>
    public sbyte AsS8() => Expect(ComponentValueKind.S8, unchecked((sbyte)Integer));

    /// <summary>Gets the value as an unsigned 8-bit integer.</summary>
    /// <returns>The value.</returns>
    public byte AsU8() => Expect(ComponentValueKind.U8, unchecked((byte)Integer));

    /// <summary>Gets the value as a signed 16-bit integer.</summary>
    /// <returns>The value.</returns>
    public short AsS16() => Expect(ComponentValueKind.S16, unchecked((short)Integer));

    /// <summary>Gets the value as an unsigned 16-bit integer.</summary>
    /// <returns>The value.</returns>
    public ushort AsU16() => Expect(ComponentValueKind.U16, unchecked((ushort)Integer));

    /// <summary>Gets the value as a signed 32-bit integer.</summary>
    /// <returns>The value.</returns>
    public int AsS32() => Expect(ComponentValueKind.S32, unchecked((int)Integer));

    /// <summary>Gets the value as an unsigned 32-bit integer.</summary>
    /// <returns>The value.</returns>
    public uint AsU32() => Expect(ComponentValueKind.U32, unchecked((uint)Integer));

    /// <summary>Gets the value as a signed 64-bit integer.</summary>
    /// <returns>The value.</returns>
    public long AsS64() => Expect(ComponentValueKind.S64, Integer);

    /// <summary>Gets the value as an unsigned 64-bit integer.</summary>
    /// <returns>The value.</returns>
    public ulong AsU64() => Expect(ComponentValueKind.U64, unchecked((ulong)Integer));

    /// <summary>Gets the value as a 32-bit float.</summary>
    /// <returns>The value.</returns>
    public float AsF32() => Expect(ComponentValueKind.F32, (float)Real);

    /// <summary>Gets the value as a 64-bit float.</summary>
    /// <returns>The value.</returns>
    public double AsF64() => Expect(ComponentValueKind.F64, Real);

    /// <summary>Gets the Unicode scalar value.</summary>
    /// <returns>The scalar, including supplementary-plane values.</returns>
    public uint AsChar() => Expect(ComponentValueKind.Char, (uint)Integer);

#if NETCOREAPP3_0_OR_GREATER
    /// <summary>Gets the Unicode scalar value.</summary>
    /// <returns>The scalar.</returns>
    public System.Text.Rune AsRune() => new System.Text.Rune(AsChar());
#endif

    /// <summary>Gets the WIT variant case name.</summary>
    /// <returns>The case name.</returns>
    public string AsVariant() => Expect(ComponentValueKind.Variant, Text!);

    /// <summary>Gets the enabled WIT flag names.</summary>
    /// <returns>The read-only set of names in input order.</returns>
    public IReadOnlyList<string> AsFlags() => Expect(ComponentValueKind.Flags, Names);

    /// <summary>Gets the value as a string.</summary>
    /// <returns>The value.</returns>
    public string AsString() => Expect(ComponentValueKind.String, Text ?? string.Empty);

    /// <summary>Gets the case name of an enumeration value.</summary>
    /// <returns>The case name.</returns>
    public string AsEnum() => Expect(ComponentValueKind.Enum, Text ?? string.Empty);

    /// <summary>Gets the elements of a list.</summary>
    /// <returns>The elements.</returns>
    public IReadOnlyList<ComponentValue> AsList() => Expect(ComponentValueKind.List, Items);

    /// <summary>Gets the elements of a tuple.</summary>
    /// <returns>The elements.</returns>
    public IReadOnlyList<ComponentValue> AsTuple() => Expect(ComponentValueKind.Tuple, Items);

    /// <summary>Gets the fields of a record.</summary>
    /// <returns>The fields, in declaration order.</returns>
    public IReadOnlyList<KeyValuePair<string, ComponentValue>> AsRecord() =>
        Expect(ComponentValueKind.Record, Fields);

    /// <summary>
    /// Gets the value of a record field by name.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <returns>The field's value.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if this is not a record, or if it has no such field.
    /// </exception>
    public ComponentValue Field(string name)
    {
        if (!TryGetField(name, out var value))
        {
            throw new InvalidOperationException($"Record has no field '{name}'.");
        }

        return value!;
    }

    /// <summary>
    /// Gets the value of a record field by name, if present.
    /// </summary>
    /// <param name="name">The field name.</param>
    /// <param name="value">The field's value, or null if there is no such field.</param>
    /// <returns>True if the field was found.</returns>
    /// <exception cref="InvalidOperationException">Thrown if this is not a record.</exception>
    public bool TryGetField(string name, out ComponentValue? value)
    {
        foreach (var field in AsRecord())
        {
            if (field.Key == name)
            {
                value = field.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private T Expect<T>(ComponentValueKind expected, T value)
    {
        if (Kind != expected)
        {
            throw new InvalidOperationException($"Expected component value kind {expected}, got {Kind}.");
        }

        return value;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        switch (Kind)
        {
            case ComponentValueKind.Bool:
                return (Integer != 0).ToString(CultureInfo.InvariantCulture);
            case ComponentValueKind.F32:
            case ComponentValueKind.F64:
                return Real.ToString(CultureInfo.InvariantCulture);
            case ComponentValueKind.U64:
                return unchecked((ulong)Integer).ToString(CultureInfo.InvariantCulture);
            case ComponentValueKind.String:
                return $"\"{Text}\"";
            case ComponentValueKind.Char:
                return char.ConvertFromUtf32((int)Integer);
            case ComponentValueKind.Variant:
                return Payload is null ? Text! : $"{Text}({Payload})";
            case ComponentValueKind.Flags:
                return $"{{{string.Join(", ", Names)}}}";
            case ComponentValueKind.Enum:
                return Text ?? string.Empty;
            case ComponentValueKind.List:
            case ComponentValueKind.Tuple:
                return $"[{string.Join(", ", Items)}]";
            case ComponentValueKind.Record:
                var fields = new string[Fields.Count];
                for (var i = 0; i < Fields.Count; i++)
                {
                    fields[i] = $"{Fields[i].Key}: {Fields[i].Value}";
                }

                return $"{{{string.Join(", ", fields)}}}";
            case ComponentValueKind.Option:
                return Flag ? $"some({Payload})" : "none";
            case ComponentValueKind.Result:
                return Flag ? $"ok({Payload})" : $"err({Payload})";
            default:
                return Integer.ToString(CultureInfo.InvariantCulture);
        }
    }
}

internal static class ComponentValueNative
{
    [DllImport(Engine.LibraryName)]
    public static extern void wasm_byte_vec_new_uninitialized(IntPtr value, nuint size);

    [DllImport(Engine.LibraryName)]
    public static extern void wasmtime_component_vallist_new_uninit(IntPtr value, nuint size);

    [DllImport(Engine.LibraryName)]
    public static extern void wasmtime_component_valrecord_new_uninit(IntPtr value, nuint size);

    [DllImport(Engine.LibraryName)]
    public static extern void wasmtime_component_valtuple_new_uninit(IntPtr value, nuint size);

    [DllImport(Engine.LibraryName)]
    public static extern void wasmtime_component_valflags_new_uninit(IntPtr value, nuint size);

    [DllImport(Engine.LibraryName)]
    public static extern IntPtr wasmtime_component_val_new(IntPtr value);

    /// <summary>
    /// Performs a deep copy of <paramref name="src"/> into <paramref name="dst"/>. The contents
    /// of <paramref name="dst"/> are owned by Wasmtime and must be released with
    /// <see cref="wasmtime_component_val_delete"/>.
    /// </summary>
    [DllImport(Engine.LibraryName)]
    public static extern void wasmtime_component_val_clone(
        IntPtr /* const wasmtime_component_val_t* */ src,
        IntPtr /* wasmtime_component_val_t* */ dst);

    /// <summary>
    /// Deallocates the memory owned by <paramref name="value"/>, but not the storage of
    /// <paramref name="value"/> itself. Only valid for embedder-owned storage.
    /// </summary>
    [DllImport(Engine.LibraryName)]
    public static extern void wasmtime_component_val_delete(IntPtr /* wasmtime_component_val_t* */ value);
}