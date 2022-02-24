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
    }

    internal static class ValueType
    {
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

        public List<ValueKind> ToList()
        {
            var list = new List<ValueKind>((int)size);

            for (int i = 0; i < (int)size; ++i)
            {
                list.Add(ValueType.ToKind(data[i]));
            }

            return list;
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

    [StructLayout(LayoutKind.Sequential)]
    internal struct Value : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            Native.wasmtime_val_delete(this);
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

            if (type == typeof(byte[]))
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

            kind = default(ValueKind);
            return false;
        }

        public static Value FromObject(object? o, ValueKind kind)
        {
            var value = new Value();
            value.kind = kind;

            try
            {
                switch (kind)
                {
                    case ValueKind.Int32:
                        if (o is null)
                            throw new WasmtimeException($"A `null` value is not valid for WebAssembly type {kind}.");
                        value.of.i32 = (int)Convert.ChangeType(o, TypeCode.Int32);
                        break;

                    case ValueKind.Int64:
                        if (o is null)
                            throw new WasmtimeException($"A `null` value is not valid for WebAssembly type {kind}.");
                        value.of.i64 = (long)Convert.ChangeType(o, TypeCode.Int64);
                        break;

                    case ValueKind.Float32:
                        if (o is null)
                            throw new WasmtimeException($"A `null` value is not valid for WebAssembly type {kind}.");
                        value.of.f32 = (float)Convert.ChangeType(o, TypeCode.Single);
                        break;

                    case ValueKind.Float64:
                        if (o is null)
                            throw new WasmtimeException($"A `null` value is not valid for WebAssembly type {kind}.");
                        value.of.f64 = (double)Convert.ChangeType(o, TypeCode.Double);
                        break;

                    case ValueKind.V128:
                        var bytes = o as byte[];
                        if ((bytes is null) || bytes.Length != 16)
                        {
                            throw new ArgumentException("expected a 16 byte array for a v128 value", nameof(o));
                        }

                        unsafe
                        {
                            for (int i = 0; i < 16; ++i)
                            {
                                value.of.v128[i] = bytes[i];
                            }
                        }
                        break;

                    case ValueKind.ExternRef:
                        value.of.externref = IntPtr.Zero;

                        if (!(o is null))
                        {
                            value.of.externref = Native.wasmtime_externref_new(
                                GCHandle.ToIntPtr(GCHandle.Alloc(o)),
                                Value.Finalizer
                            );
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

        public object? ToObject(StoreContext context)
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
                    var bytes = new byte[16];
                    unsafe
                    {
                        for (int i = 0; i < 16; ++i)
                        {
                            bytes[i] = of.v128[i];
                        }
                    }
                    return bytes;

                case ValueKind.ExternRef:
                    if (of.externref == IntPtr.Zero)
                    {
                        return null;
                    }
                    var data = Native.wasmtime_externref_data(of.externref);
                    if (data == IntPtr.Zero)
                    {
                        return null;
                    }
                    return GCHandle.FromIntPtr(data).Target;

                case ValueKind.FuncRef:
                    return new Function(context, of.funcref);

                default:
                    throw new NotSupportedException("Unsupported value kind.");
            }
        }

        private static class Native
        {
            public delegate void Finalizer(IntPtr data);

            [StructLayout(LayoutKind.Explicit)]
            public unsafe struct ValueUnion
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
                public ExternFunc funcref;

                [FieldOffset(0)]
                public IntPtr externref;

                [FieldOffset(0)]
                public fixed byte v128[16];
            }

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_val_delete(in Value val);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_externref_new(IntPtr data, Finalizer? finalizer);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_externref_data(IntPtr externref);

        }

        private static readonly Native.Finalizer Finalizer = (p) => GCHandle.FromIntPtr(p).Free();

        private ValueKind kind;
        private Native.ValueUnion of;
    }
}
