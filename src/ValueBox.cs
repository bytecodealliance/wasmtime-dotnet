using System;

namespace Wasmtime
{
    /// <summary>
    /// Allocation free container for a single value
    /// </summary>
    public readonly struct ValueBox
    {
        internal readonly ValueKind Kind;
        internal readonly ValueUnion Union;
        internal readonly object? ExternRefObject;

        internal ValueBox(ValueKind kind, ValueUnion of)
        {
            if (kind == ValueKind.ExternRef)
            {
                throw new InvalidOperationException("Must pass in `object?` for an externref ValueBox`");
            }

            Kind = kind;
            Union = of;
            ExternRefObject = null;
        }

        internal ValueBox(object? externref)
        {
            Kind = ValueKind.ExternRef;
            Union = default;
            ExternRefObject = externref;
        }

        internal Value ToValue(ValueKind convertTo)
        {
            if (convertTo != Kind)
                return Value.FromValueBox(ConvertTo(convertTo));

            return Value.FromValueBox(this);
        }

        internal ValueBox ConvertTo(ValueKind convertTo)
        {
            return (Kind, convertTo) switch
            {
                (ValueKind.Int32, ValueKind.Int32) => this,
                (ValueKind.Int32, ValueKind.Int64) => Convert.ToInt64(Union.i32),
                (ValueKind.Int32, ValueKind.Float32) => Convert.ToSingle(Union.i32),
                (ValueKind.Int32, ValueKind.Float64) => Convert.ToDouble(Union.i32),

                (ValueKind.Int64, ValueKind.Int32) => Convert.ToInt32(Union.i64),
                (ValueKind.Int64, ValueKind.Int64) => Convert.ToInt64(Union.i64),
                (ValueKind.Int64, ValueKind.Float32) => Convert.ToSingle(Union.i64),
                (ValueKind.Int64, ValueKind.Float64) => Convert.ToDouble(Union.i64),

                (ValueKind.Float32, ValueKind.Int32) => Convert.ToInt32(Union.f32),
                (ValueKind.Float32, ValueKind.Int64) => Convert.ToInt64(Union.f32),
                (ValueKind.Float32, ValueKind.Float32) => Convert.ToSingle(Union.f32),
                (ValueKind.Float32, ValueKind.Float64) => Convert.ToDouble(Union.f32),

                (ValueKind.Float64, ValueKind.Int32) => Convert.ToInt32(Union.f64),
                (ValueKind.Float64, ValueKind.Int64) => Convert.ToInt64(Union.f64),
                (ValueKind.Float64, ValueKind.Float32) => Convert.ToSingle(Union.f64),
                (ValueKind.Float64, ValueKind.Float64) => Convert.ToDouble(Union.f64),

                _ => throw new InvalidCastException($"Cannot convert from `{Kind}` to `{convertTo}`")
            };
        }

        /// <summary>
        /// "Box" an int without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ValueBox(int value)
        {
            return new ValueBox(ValueKind.Int32, new ValueUnion { i32 = value });
        }

        /// <summary>
        /// "Box" a long without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ValueBox(long value)
        {
            return new ValueBox(ValueKind.Int64, new ValueUnion { i64 = value });
        }

        /// <summary>
        /// "Box" a float without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ValueBox(float value)
        {
            return new ValueBox(ValueKind.Float32, new ValueUnion { f32 = value });
        }

        /// <summary>
        /// "Box" a double without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ValueBox(double value)
        {
            return new ValueBox(ValueKind.Float64, new ValueUnion { f64 = value });
        }

        /// <summary>
        /// "Box" a 16 element vector of bytes without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ValueBox(V128 value)
        {
            var union = new ValueUnion();
            unsafe
            {
                value.CopyTo(union.v128);
            }

            return new ValueBox(ValueKind.V128, union);
        }

        /// <summary>
        /// "Box" a 16 element byte array without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator ValueBox(byte[] value)
        {
            return (ValueBox)(ReadOnlySpan<byte>)value;
        }

        /// <summary>
        /// "Box" a 16 element byte span without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static explicit operator ValueBox(ReadOnlySpan<byte> value)
        {
            if (value.Length != 16)
                throw new ArgumentException("expected a 16 byte array for a v128 value", nameof(value));

            var union = new ValueUnion();
            unsafe
            {
                value.CopyTo(new Span<byte>(union.v128, 16));
            }

            return new ValueBox(ValueKind.V128, union);
        }

        /// <summary>
        /// "Box" a function without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ValueBox(Function? value)
        {
            var func = value?.func ?? Function.Null.func;
            return new ValueBox(ValueKind.FuncRef, new ValueUnion { funcref = func });
        }

        /// <summary>
        /// "Box" a string without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        public static implicit operator ValueBox(string value)
        {
            return new ValueBox(value);
        }

        /// <summary>
        /// "Box" an arbitrary reference type without any heap allocations
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static ValueBox AsBox<T>(T? value)
            where T : class
        {
            return new ValueBox(value);
        }

        internal static IValueBoxConverter<T> Converter<T>()
        {
            if (typeof(T) == typeof(int))
            {
                return (IValueBoxConverter<T>)Int32ValueBoxConverter.Instance;
            }

            if (typeof(T) == typeof(long))
            {
                return (IValueBoxConverter<T>)Int64ValueBoxConverter.Instance;
            }

            if (typeof(T) == typeof(float))
            {
                return (IValueBoxConverter<T>)Float32ValueBoxConverter.Instance;
            }

            if (typeof(T) == typeof(double))
            {
                return (IValueBoxConverter<T>)Float64ValueBoxConverter.Instance;
            }

            if (typeof(T) == typeof(Function))
            {
                return (IValueBoxConverter<T>)FuncRefValueBoxConverter.Instance;
            }

            if (typeof(T) == typeof(V128))
            {
                return (IValueBoxConverter<T>)V128ValueBoxConverter.Instance;
            }

            if (typeof(T).IsClass)
            {
                return (IValueBoxConverter<T>)GenericValueBoxConverter<T>.Instance;
            }

            throw new InvalidOperationException($"Cannot convert type '{typeof(T).Name}' into a WASM parameter type");
        }
    }

    internal interface IValueBoxConverter<T>
    {
        public ValueBox Box(T value);

        public T Unbox(IStore store, ValueBox value);
    }

    internal class Int32ValueBoxConverter
        : IValueBoxConverter<int>
    {
        public static readonly Int32ValueBoxConverter Instance = new Int32ValueBoxConverter();

        private Int32ValueBoxConverter()
        {
        }

        public ValueBox Box(int value)
        {
            return value;
        }

        public int Unbox(IStore store, ValueBox value)
        {
            return value.Union.i32;
        }
    }

    internal class Int64ValueBoxConverter
        : IValueBoxConverter<long>
    {
        public static readonly Int64ValueBoxConverter Instance = new Int64ValueBoxConverter();

        private Int64ValueBoxConverter()
        {
        }

        public ValueBox Box(long value)
        {
            return value;
        }

        public long Unbox(IStore store, ValueBox value)
        {
            return value.Union.i64;
        }
    }

    internal class Float32ValueBoxConverter
        : IValueBoxConverter<float>
    {
        public static readonly Float32ValueBoxConverter Instance = new Float32ValueBoxConverter();

        private Float32ValueBoxConverter()
        {
        }

        public ValueBox Box(float value)
        {
            return value;
        }

        public float Unbox(IStore store, ValueBox value)
        {
            return value.Union.f32;
        }
    }

    internal class Float64ValueBoxConverter
        : IValueBoxConverter<double>
    {
        public static readonly Float64ValueBoxConverter Instance = new Float64ValueBoxConverter();

        private Float64ValueBoxConverter()
        {
        }

        public ValueBox Box(double value)
        {
            return value;
        }

        public double Unbox(IStore store, ValueBox value)
        {
            return value.Union.f64;
        }
    }

    internal class FuncRefValueBoxConverter
        : IValueBoxConverter<Function>
    {
        public static readonly FuncRefValueBoxConverter Instance = new FuncRefValueBoxConverter();

        private FuncRefValueBoxConverter()
        {
        }

        public ValueBox Box(Function value)
        {
            return value;
        }

        public Function Unbox(IStore store, ValueBox value)
        {
            return new Function(store, value.Union.funcref);
        }
    }

    internal class V128ValueBoxConverter
        : IValueBoxConverter<V128>
    {
        public static readonly V128ValueBoxConverter Instance = new V128ValueBoxConverter();

        private V128ValueBoxConverter()
        {
        }

        public ValueBox Box(V128 value)
        {
            return value;
        }

        public V128 Unbox(IStore store, ValueBox value)
        {
            unsafe
            {
                return new V128(value.Union.v128);
            }
        }
    }

    internal class GenericValueBoxConverter<T>
        : IValueBoxConverter<T?>
    {
        public static readonly GenericValueBoxConverter<T> Instance = new GenericValueBoxConverter<T>();

        private GenericValueBoxConverter()
        {
        }

        public ValueBox Box(T? value)
        {
            return ValueBox.AsBox((object?)value);
        }

        public T? Unbox(IStore store, ValueBox value)
        {
            return (T?)value.ExternRefObject;
        }
    }
}
