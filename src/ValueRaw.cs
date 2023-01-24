using System;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct ValueRaw
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
        public V128 v128;

        [FieldOffset(0)]
        public nuint funcref;

        [FieldOffset(0)]
        public nuint externref;

        public static IValueRawConverter<T> Converter<T>()
        {
            // Ensure we are on a little endian system. For big endian, we would need
            // separate converters since the value layout of the native wasmtime_val_raw_t
            // is always little endian, regardless of the host architecture.
            if (!BitConverter.IsLittleEndian)
            {
                throw new PlatformNotSupportedException("Big endian systems are currently not supported for raw values.");
            }

            if (typeof(T) == typeof(int))
            {
                return (IValueRawConverter<T>)Int32ValueRawConverter.Instance;
            }

            if (typeof(T) == typeof(long))
            {
                return (IValueRawConverter<T>)Int64ValueRawConverter.Instance;
            }

            if (typeof(T) == typeof(float))
            {
                return (IValueRawConverter<T>)Float32ValueRawConverter.Instance;
            }

            if (typeof(T) == typeof(double))
            {
                return (IValueRawConverter<T>)Float64ValueRawConverter.Instance;
            }

            if (typeof(T) == typeof(Function))
            {
                return (IValueRawConverter<T>)FuncRefValueRawConverter.Instance;
            }

            if (typeof(T) == typeof(V128))
            {
                return (IValueRawConverter<T>)V128ValueRawConverter.Instance;
            }

            if (!typeof(T).IsValueType)
            {
                return GenericValueRawConverter<T>.Instance;
            }

            throw new InvalidOperationException($"Cannot convert type '{typeof(T).Name}' into a WASM parameter type");
        }
    }

    internal interface IValueRawConverter<T>
    {
        T? Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw);

        void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, T value);
    }

    internal class Int32ValueRawConverter : IValueRawConverter<int>
    {
        public static readonly Int32ValueRawConverter Instance = new();

        private Int32ValueRawConverter()
        {
        }

        public int Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw)
        {
            return valueRaw.i32;
        }

        public void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, int value)
        {
            // Note: We set the i64 instead of the i32 field here using a zero-extended
            // version of `value`, to ensure the first 64 bits are always initialized.
            // This ensures we match the behavior of the Rust 'ValRaw' constructor
            // that does the same.
            // See: https://github.com/bytecodealliance/wasmtime/blob/7a6fbe08987a7cb43c7e2a78ba5482bec8ab3c2a/crates/runtime/src/vmcontext.rs#L982-L993
            valueRaw.i64 = unchecked((uint)value);
        }
    }

    internal class Int64ValueRawConverter : IValueRawConverter<long>
    {
        public static readonly Int64ValueRawConverter Instance = new();

        private Int64ValueRawConverter()
        {
        }

        public long Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw)
        {
            return valueRaw.i64;
        }

        public void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, long value)
        {
            valueRaw.i64 = value;
        }
    }

    internal class Float32ValueRawConverter : IValueRawConverter<float>
    {
        public static readonly Float32ValueRawConverter Instance = new();

        private Float32ValueRawConverter()
        {
        }

        public float Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw)
        {
            return valueRaw.f32;
        }

        public void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, float value)
        {
            // See comments in Int32ValueRawConverter for why we set the i64 field.
            valueRaw.i64 = unchecked((uint)BitConverter.SingleToInt32Bits(value));
        }
    }

    internal class Float64ValueRawConverter : IValueRawConverter<double>
    {
        public static readonly Float64ValueRawConverter Instance = new();

        private Float64ValueRawConverter()
        {
        }

        public double Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw)
        {
            return valueRaw.f64;
        }

        public void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, double value)
        {
            valueRaw.f64 = value;
        }
    }

    internal class V128ValueRawConverter : IValueRawConverter<V128>
    {
        public static readonly V128ValueRawConverter Instance = new();

        private V128ValueRawConverter()
        {
        }

        public unsafe V128 Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw)
        {
            return valueRaw.v128;
        }

        public unsafe void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, V128 value)
        {
            valueRaw.v128 = value;
        }
    }

    internal class FuncRefValueRawConverter : IValueRawConverter<Function>
    {
        public static readonly FuncRefValueRawConverter Instance = new();

        private FuncRefValueRawConverter()
        {
        }

        public Function Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw)
        {
            var funcref = default(ExternFunc);

            if (valueRaw.funcref != 0)
            {
                Function.Native.wasmtime_func_from_raw(storeContext.handle, valueRaw.funcref, out funcref);
            }

            return new Function(store!, funcref);
        }

        public void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, Function? value)
        {
            nuint funcrefInt = 0;

            if (value?.IsNull is false)
            {
                // It is only allowed to return functions whose store context is the same as
                // the one we are being called from, so we need to verify this.
                var valueStoreContext = value.store!.Context;

                if (valueStoreContext.handle != storeContext.handle)
                {
                    throw new InvalidOperationException("Returning a Function is only allowed when it belongs to the current store.");
                }

                funcrefInt = Function.Native.wasmtime_func_to_raw(storeContext.handle, value.func);
            }

            valueRaw.funcref = funcrefInt;
        }
    }

    internal class GenericValueRawConverter<T> : IValueRawConverter<T>
    {
        public static readonly GenericValueRawConverter<T> Instance = new();

        private GenericValueRawConverter()
        {
        }

        public T? Unbox(StoreContext storeContext, Store store, in ValueRaw valueRaw)
        {
            object? o = null;

            if (valueRaw.externref != 0)
            {
                // The externref is an owned value, so we must delete it afterwards.
                var externref = Value.Native.wasmtime_externref_from_raw(storeContext.handle, valueRaw.externref);

                try
                {
                    var data = Value.Native.wasmtime_externref_data(externref);
                    if (data != IntPtr.Zero)
                    {
                        o = GCHandle.FromIntPtr(data).Target!;
                    }
                }
                finally
                {
                    Value.Native.wasmtime_externref_delete(externref);
                }
            }

            return (T?)o;
        }

        public void Box(StoreContext storeContext, Store store, ref ValueRaw valueRaw, T value)
        {
            nuint externrefInt = 0;

            if (value is not null)
            {
                var externref = Value.Native.wasmtime_externref_new(
                    GCHandle.ToIntPtr(GCHandle.Alloc(value)),
                    Value.Finalizer);

                try
                {
                    // Convert the externref into a raw value.
                    // Note: The externref data isn't tracked by wasmtime's GC until
                    // it enters WebAssembly, so Store.GC() mustn't be called between
                    // converting the value and passing it to WebAssembly.
                    externrefInt = Value.Native.wasmtime_externref_to_raw(storeContext.handle, externref);
                }
                finally
                {
                    // We still must delete the old externref afterwards because
                    // wasmtime_externref_to_raw doesn't transfer ownership.
                    Value.Native.wasmtime_externref_delete(externref);
                }
            }

            valueRaw.externref = externrefInt;
        }
    }
}
