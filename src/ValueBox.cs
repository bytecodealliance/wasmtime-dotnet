using System;
using System.Runtime.Intrinsics;

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
        public static implicit operator ValueBox(Vector128<byte> value)
        {
            var union = new ValueUnion();
            unsafe
            {
                for (int i = 0; i < 16; i++)
                {
                    union.v128[i] = value.GetElement(i);
                }
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
    }
}
