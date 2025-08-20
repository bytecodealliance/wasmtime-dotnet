namespace Wasmtime.Components;

// todo: everything here: https://docs.wasmtime.dev/c-api/component_2val_8h.html
/*
 wasmtime_component_vallist
 A vec of a struct wasmtime_component_val

 wasmtime_component_valrecord
 A vec of a struct wasmtime_component_valrecord_entry

 wasmtime_component_valtuple
 A vec of a struct wasmtime_component_val

 wasmtime_component_valflags
 A vec of a wasm_name_t

 wasmtime_component_valvariant_t
 Represents a variant type

 wasmtime_component_valresult_t
 Represents a result type

 wasmtime_component_valunion_t
 Represents possible runtime values which a component function can either consume or produce

 wasmtime_component_val
 Represents possible runtime values which a component function can either consume or produce

 wasmtime_component_valrecord_entry
 A pair of a name and a value that represents one entry in a value with kind WASMTIME_COMPONENT_RECORD
*/

internal enum ComponentValueKind
{
    Bool = 0,
    S8 = 1,
    U8 = 2,
    S16 = 3,
    U16 = 4,
    S32 = 5,
    U32 = 6,
    S64 = 7,
    U64 = 8,
    F32 = 9,
    F64 = 10,
    Char = 11,
    String = 12,
    List = 13,
    Record = 14,
    Tuple = 15,
    Variant = 16,
    Enum = 17,
    Option = 18,
    Result = 19,
    Flags = 20,
}

internal static class ComponentValueNative
{
    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_vallist_new(wasmtime_component_vallist_t*out, size_t size, struct wasmtime_component_val * ptr)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_vallist_new_empty (wasmtime_component_vallist_t*out)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_vallist_new_uninit (wasmtime_component_vallist_t*out, size_t size)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_vallist_copy (wasmtime_component_vallist_t* dst, const wasmtime_component_vallist_t* src)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_vallist_delete (wasmtime_component_vallist_t* value)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valrecord_new (wasmtime_component_valrecord_t*out, size_t size, struct wasmtime_component_valrecord_entry * ptr)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valrecord_new_empty (wasmtime_component_valrecord_t*out)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valrecord_new_uninit (wasmtime_component_valrecord_t*out, size_t size)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valrecord_copy (wasmtime_component_valrecord_t* dst, const wasmtime_component_valrecord_t* src)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valrecord_delete (wasmtime_component_valrecord_t* value)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valtuple_new (wasmtime_component_valtuple_t*out, size_t size, struct wasmtime_component_val * ptr)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valtuple_new_empty (wasmtime_component_valtuple_t*out)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valtuple_new_uninit (wasmtime_component_valtuple_t*out, size_t size)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valtuple_copy (wasmtime_component_valtuple_t* dst, const wasmtime_component_valtuple_t* src)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valtuple_delete (wasmtime_component_valtuple_t* value)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valflags_new (wasmtime_component_valflags_t*out, size_t size, wasm_name_t* ptr)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valflags_new_empty (wasmtime_component_valflags_t*out)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valflags_new_uninit (wasmtime_component_valflags_t*out, size_t size)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valflags_copy (wasmtime_component_valflags_t* dst, const wasmtime_component_valflags_t* src)

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_valflags_delete (wasmtime_component_valflags_t* value)

    //[DllImport(Engine.LibraryName)]
    //public static extern wasmtime_component_val_t * 	wasmtime_component_val_new ()

    //[DllImport(Engine.LibraryName)]
    //public static extern void wasmtime_component_val_delete(wasmtime_component_val_t* value)
}