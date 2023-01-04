using System;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ValueBoxTests
        : StoreFixture
    {
        /// <summary>
        /// Convert a box to a given kind and check that the unboxed value is as expected
        /// </summary>
        private void Convert<T>(ValueBox box, T expected)
        {
            // Get kind to convert to based on type parameter
            Value.TryGetKind(typeof(T), out var convertToKind).Should().BeTrue();

            // Convert to a value with a different type
            var value = box.ToValue(convertToKind);

            // Check that the value is as expected
            var fromValue = (T)value.ToObject(Store);
            fromValue.Should().Be(expected);

            // Convert back into a box
            var box2 = value.ToValueBox();

            // Check that the new box has the right value
            ValueBox.Converter<T>().Unbox(Store, box2).Should().Be(expected);
        }

        /// <summary>
        /// Convert a box to a given kind and check that the conversion fails
        /// </summary>
        private static void FailConvert(ValueBox box, ValueKind kind)
        {
            var act = () =>
            {
                box.ToValue(kind);
            };

            act.Should().Throw<InvalidCastException>();
        }

        [Fact]
        public void ItConvertsInt()
        {
            ValueBox box = 7;

            box.Kind.Should().Be(ValueKind.Int32);

            Convert(box, 7);
            Convert(box, 7L);
            Convert(box, 7f);
            Convert(box, 7d);

            FailConvert(box, ValueKind.ExternRef);
            FailConvert(box, ValueKind.FuncRef);
            FailConvert(box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsLong()
        {
            ValueBox box = 7L;

            box.Kind.Should().Be(ValueKind.Int64);

            Convert(box, 7);
            Convert(box, 7L);
            Convert(box, 7f);
            Convert(box, 7d);

            FailConvert(box, ValueKind.ExternRef);
            FailConvert(box, ValueKind.FuncRef);
            FailConvert(box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsFloat()
        {
            ValueBox box = 7f;

            box.Kind.Should().Be(ValueKind.Float32);

            Convert(box, 7);
            Convert(box, 7L);
            Convert(box, 7f);
            Convert(box, 7d);

            FailConvert(box, ValueKind.ExternRef);
            FailConvert(box, ValueKind.FuncRef);
            FailConvert(box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsDouble()
        {
            ValueBox box = 7d;

            box.Kind.Should().Be(ValueKind.Float64);

            Convert(box, 7);
            Convert(box, 7L);
            Convert(box, 7f);
            Convert(box, 7d);

            FailConvert(box, ValueKind.ExternRef);
            FailConvert(box, ValueKind.FuncRef);
            FailConvert(box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsV128()
        {
            var v = new V128(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            ValueBox box = v;

            box.Kind.Should().Be(ValueKind.V128);

            FailConvert(box, ValueKind.Int32);
            FailConvert(box, ValueKind.Int64);
            FailConvert(box, ValueKind.Float32);
            FailConvert(box, ValueKind.Float64);
            FailConvert(box, ValueKind.ExternRef);
            FailConvert(box, ValueKind.FuncRef);

            Convert(box, v);
        }

        [Fact]
        public void ItConvertsByteArrayToV128()
        {
            var b = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var box = (ValueBox)b;
            Convert(box, new V128(b));
        }

        [Fact]
        public void ItConvertsByteSpanToV128()
        {
            ReadOnlySpan<byte> b = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var box = (ValueBox)b;
            Convert(box, new V128(b));
        }

        [Fact]
        public void ItFailsToConvertLongByteArrayToV128()
        {
            var b = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
            var act = () => (ValueBox)b;
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ItFailsToConvertLongByteSpanToV128()
        {
            var b = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 };
            var act = () => new V128(b.AsSpan());
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void ItConvertsExternRef()
        {
            var o = new object();
            var box = ValueBox.AsBox(o);

            box.Kind.Should().Be(ValueKind.ExternRef);

            FailConvert(box, ValueKind.Int32);
            FailConvert(box, ValueKind.Int64);
            FailConvert(box, ValueKind.Float32);
            FailConvert(box, ValueKind.Float64);

            Convert(box, o);

            FailConvert(box, ValueKind.FuncRef);
            FailConvert(box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsFuncRef()
        {
            var func = Function.FromCallback(Store, ItConvertsFuncRef);
            ValueBox box = func;

            box.Kind.Should().Be(ValueKind.FuncRef);

            FailConvert(box, ValueKind.Int32);
            FailConvert(box, ValueKind.Int64);
            FailConvert(box, ValueKind.Float32);
            FailConvert(box, ValueKind.Float64);
            FailConvert(box, ValueKind.ExternRef);
            FailConvert(box, ValueKind.V128);

            // Checking a func for equality is different to all the other types so the normal "Convert" method cannot be used here

            var converted = box.ToValue(ValueKind.FuncRef).ToValueBox();
            converted.Kind.Should().Be(ValueKind.FuncRef);
            var unboxed = ValueBox.Converter<Function>().Unbox(Store, converted);
            unboxed.func.index.Should().Be(func.func.index);
            unboxed.func.store.Should().Be(func.func.store);

            var value = box.ToValue(ValueKind.FuncRef);
            var obj = (Function)value.ToObject(Store)!;
            obj.func.index.Should().Be(func.func.index);
            obj.func.store.Should().Be(func.func.store);
        }

        private struct Unsupported
        {
        }

        [Fact]
        public void ItFailsWithInvalidTypeConversion()
        {
            var act = () => { ValueBox.Converter<Unsupported>(); };
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ItFailsWithInvalidObjectRef()
        {
            var act = () => new ValueBox(ValueKind.ExternRef, new ValueUnion());
            act.Should().Throw<InvalidOperationException>();
        }
    }
}
