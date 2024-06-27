using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    /// <summary>
    /// Represents the possible kinds of WebAssembly values.
    /// </summary>
    public enum ValueKind : byte
    {
        /// <summary>
        /// The value is a 32-bit integer.
        /// </summary>
        Int32,
        /// <summary>
        /// The value is a 64-bit integer.
        /// </summary>
        Int64,
        /// <summary>
        /// The value is a 32-bit floating point number.
        /// </summary>
        Float32,
        /// <summary>
        /// The value is a 64-bit floating point number.
        /// </summary>
        Float64,
        /// <summary>
        /// The value is a 128-bit value representing the WebAssembly `v128` type.
        /// </summary>
        V128,
        /// <summary>
        /// The value is a function reference.
        /// </summary>
        FuncRef,
        /// <summary>
        /// The value is an external reference.
        /// </summary>
        ExternRef,
        /// <summary>
        /// The value is an `anyref`.
        /// </summary>
        AnyRef,
    }

    internal static class ValueKindExtensions
    {
        public static bool IsAssignableFrom(this ValueKind kind, Type type)
        {
            return (kind) switch
            {
                ValueKind.Int32 => type == typeof(int),
                ValueKind.Int64 => type == typeof(long),
                ValueKind.Float32 => type == typeof(float),
                ValueKind.Float64 => type == typeof(double),
                ValueKind.V128 => type == typeof(V128),
                ValueKind.FuncRef => type == typeof(Function),
                ValueKind.ExternRef => type.IsClass,
                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }
    }

    internal static class AnyRefExtensions
    {
        public static bool IsNull(this in AnyRef anyref)
        {
            // This code originates from inline function "wasmtime_anyref_is_null" in `val.h`.
            return anyref.store == 0;
        }
    }

    internal static class ExternRefExtensions
    {
        public static bool IsNull(this in ExternRef externref)
        {
            // This code originates from inline function "wasmtime_externref_is_null" in `val.h`.
            return externref.store == 0;
        }
    }

    internal static class ExternFuncExtensions
    {
        public static bool IsNull(this in ExternFunc externfunc)
        {
            // This code originates from inline function "wasmtime_funcref_is_null" in `val.h`.
            return externfunc.store == 0;
        }
    }

    internal static class ValueType
    {
        public static IntPtr FromKind(TableKind kind)
        {
            return FromKind((ValueKind)kind);
        }

        public static IntPtr FromKind(ValueKind kind)
        {
            switch (kind)
            {
                case ValueKind.Int32:
                case ValueKind.Int64:
                case ValueKind.Float32:
                case ValueKind.Float64:
                case ValueKind.V128:
                    return Native.wasm_valtype_new((byte)kind);

                case ValueKind.ExternRef:
                    return Native.wasm_valtype_new(128);

                case ValueKind.FuncRef:
                    return Native.wasm_valtype_new(129);

                default:
                    throw new ArgumentException("unsupported value kind");
            }
        }

        public static ValueKind ToKind(IntPtr type)
        {
            var kind = (ValueKind)Native.wasm_valtype_kind(type);
            switch (kind)
            {
                case ValueKind.Int32:
                case ValueKind.Int64:
                case ValueKind.Float32:
                case ValueKind.Float64:
                case ValueKind.V128:
                    return kind;

                case (ValueKind)128:
                    return ValueKind.ExternRef;

                case (ValueKind)129:
                    return ValueKind.FuncRef;

                default:
                    throw new ArgumentException("unsupported value kind");
            }
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_valtype_new(byte kind);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern byte wasm_valtype_kind(IntPtr valueType);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ValueTypeArray : IDisposable
    {
        public ValueTypeArray(IReadOnlyList<ValueKind> kinds)
        {
            Native.wasm_valtype_vec_new_uninitialized(out var vec, (UIntPtr)kinds.Count);
            this.size = vec.size;
            this.data = vec.data;

            for (int i = 0; i < kinds.Count; ++i)
            {
                this.data[i] = ValueType.FromKind(kinds[i]);
            }
        }

        public ValueKind[] ToArray()
        {
            var arr = new ValueKind[(int)size];

            for (int i = 0; i < (int)size; ++i)
            {
                arr[i] = ValueType.ToKind(data[i]);
            }

            return arr;
        }

        public readonly UIntPtr size;
        public readonly IntPtr* data;

        public void Dispose()
        {
            Native.wasm_valtype_vec_delete(this);
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasm_valtype_vec_delete(in ValueTypeArray vec);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_valtype_vec_new_uninitialized(out ValueTypeArray vec, UIntPtr len);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// <para>
    /// When owning the value and you are finished with using it, you must release/unroot
    /// it by calling the <see cref="Release(Store)"/> method. After that, the
    /// <see cref="Value"/> must no longer be used.
    /// </para>
    /// <para>
    /// Previously, this type implemented the <see cref="IDisposable"/> interface, but since
    /// Wasmtime v20.0.0, unrooting the value requires passing a store context, which is why
    /// the <see cref="Release(Store)"/> method needs to explicitly be called, passing a
    /// <see cref="Store"/>.
    /// </para>
    /// </remarks>
    [StructLayout(LayoutKind.Sequential)]
    internal struct Value
    {
        public void Release(Store store)
        {
            Native.wasmtime_val_unroot(store.Context.handle, this);
            GC.KeepAlive(store);
        }

        public static bool TryGetKind(Type type, out ValueKind kind)
        {
            if (type == typeof(int))
            {
                kind = ValueKind.Int32;
                return true;
            }

            if (type == typeof(long))
            {
                kind = ValueKind.Int64;
                return true;
            }

            if (type == typeof(float))
            {
                kind = ValueKind.Float32;
                return true;
            }

            if (type == typeof(double))
            {
                kind = ValueKind.Float64;
                return true;
            }

            if (type == typeof(V128))
            {
                kind = ValueKind.V128;
                return true;
            }

            if (type == typeof(Function))
            {
                kind = ValueKind.FuncRef;
                return true;
            }

            if (!type.IsValueType)
            {
                kind = ValueKind.ExternRef;
                return true;
            }

            kind = default;
            return false;
        }

        public static Value FromValueBox(Store store, ValueBox box)
        {
            var value = new Value();
            value.kind = box.Kind;
            value.of = box.Union;

            if (value.kind == ValueKind.ExternRef)
            {
                value.of.externref = default;

                if (box.ExternRefObject is not null)
                {
                    var gcHandle = GCHandle.Alloc(box.ExternRefObject);

                    try
                    {
                        if (!Native.wasmtime_externref_new(
                            store.Context.handle,
                            GCHandle.ToIntPtr(gcHandle),
                            Finalizer,
                            ref value.of.externref
                        ))
                        {
                            throw new WasmtimeException("The host wasn't able to create more GC values at this time.");
                        }
                    }
                    catch
                    {
                        gcHandle.Free();
                        throw;
                    }

                    GC.KeepAlive(store);
                }
            }

            return value;
        }

        public ValueBox ToValueBox(Store store)
        {
            if (kind != ValueKind.ExternRef)
            {
                return new ValueBox(kind, of);
            }
            else
            {
                return new ValueBox(ResolveExternRef(store));
            }
        }

        public static Value FromObject(Store store, object? o, TableKind kind)
        {
            return FromObject(store, o, (ValueKind)kind);
        }

        public static Value FromObject(Store store, object? o, ValueKind kind)
        {
            var value = new Value();
            value.kind = kind;

            try
            {
                switch (kind)
                {
                    case ValueKind.Int32:
                        if (o is null)
                            throw new WasmtimeException($"The value `null` is not valid for WebAssembly type {kind}.");
                        value.of.i32 = (int)Convert.ChangeType(o, TypeCode.Int32);
                        break;

                    case ValueKind.Int64:
                        if (o is null)
                            throw new WasmtimeException($"The value `null` is not valid for WebAssembly type {kind}.");
                        value.of.i64 = (long)Convert.ChangeType(o, TypeCode.Int64);
                        break;

                    case ValueKind.Float32:
                        if (o is null)
                            throw new WasmtimeException($"The value `null` is not valid for WebAssembly type {kind}.");
                        value.of.f32 = (float)Convert.ChangeType(o, TypeCode.Single);
                        break;

                    case ValueKind.Float64:
                        if (o is null)
                            throw new WasmtimeException($"The value `null` is not valid for WebAssembly type {kind}.");
                        value.of.f64 = (double)Convert.ChangeType(o, TypeCode.Double);
                        break;

                    case ValueKind.V128:
                        if (o is null)
                            throw new WasmtimeException($"The value `null` is not valid for WebAssembly type {kind}.");
                        value.of.v128 = (V128)o;
                        break;

                    case ValueKind.ExternRef:
                        value.of.externref = default;

                        if (o is not null)
                        {
                            var gcHandle = GCHandle.Alloc(o);

                            try
                            {
                                if (!Native.wasmtime_externref_new(
                                    store.Context.handle,
                                    GCHandle.ToIntPtr(gcHandle),
                                    Value.Finalizer,
                                    ref value.of.externref
                                ))
                                {
                                    throw new WasmtimeException("The host wasn't able to create more GC values at this time.");
                                }
                            }
                            catch
                            {
                                gcHandle.Free();
                                throw;
                            }

                            GC.KeepAlive(store);
                        }
                        break;

                    case ValueKind.FuncRef:
                        switch (o)
                        {
                            case null:
                                value.of.funcref = Function.Null.func;
                                break;

                            case Function f:
                                value.of.funcref = f.func;
                                break;

                            default:
                                throw new ArgumentException("expected a function value", nameof(o));
                        }
                        break;

                    default:
                        throw new NotSupportedException("Unsupported value type.");
                }
            }
            catch (InvalidCastException ex)
            {
                throw new WasmtimeException($"The value `{o ?? "null"}` is not valid for WebAssembly type {kind}.", ex);
            }

            return value;
        }

        public object? ToObject(Store store)
        {
            switch (kind)
            {
                case ValueKind.Int32:
                    return of.i32;

                case ValueKind.Int64:
                    return of.i64;

                case ValueKind.Float32:
                    return of.f32;

                case ValueKind.Float64:
                    return of.f64;

                case ValueKind.V128:
                    return of.v128;

                case ValueKind.ExternRef:
                    return ResolveExternRef(store);

                case ValueKind.FuncRef:
                    return store.GetCachedExtern(of.funcref);

                default:
                    throw new NotSupportedException("Unsupported value kind.");
            }
        }

        private object? ResolveExternRef(Store store)
        {
            if (of.externref.IsNull())
            {
                return null;
            }
            var data = Native.wasmtime_externref_data(store.Context.handle, of.externref);
            if (data == IntPtr.Zero)
            {
                return null;
            }
            return GCHandle.FromIntPtr(data).Target;
        }

        public static class Native
        {
            public delegate void Finalizer(IntPtr data);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_val_unroot(IntPtr context, in Value val);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_externref_new(IntPtr context, IntPtr data, Finalizer? finalizer, ref ExternRef @out);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_externref_data(IntPtr context, in ExternRef externref);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_externref_unroot(IntPtr context, in ExternRef externref);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_externref_from_raw(IntPtr context, uint raw, out ExternRef @out);

            [DllImport(Engine.LibraryName)]
            public static extern uint wasmtime_externref_to_raw(IntPtr context, in ExternRef externref);
        }

        public static readonly Native.Finalizer Finalizer = (p) => GCHandle.FromIntPtr(p).Free();

        private ValueKind kind;
        private ValueUnion of;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct ValueUnion
    {
        [FieldOffset(0)]
        public int i32;

        [FieldOffset(0)]
        public long i64;

        [FieldOffset(0)]
        public float f32;

        [FieldOffset(0)]
        public double f64;

        [FieldOffset(0)]
        public AnyRef anyref;

        [FieldOffset(0)]
        public ExternRef externref;

        [FieldOffset(0)]
        public ExternFunc funcref;

        [FieldOffset(0)]
        public V128 v128;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct AnyRef
    {
        public ulong store;

        private uint __private1;

        private uint __private2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ExternRef
    {
        public ulong store;

        private uint __private1;

        private uint __private2;
    }
}
