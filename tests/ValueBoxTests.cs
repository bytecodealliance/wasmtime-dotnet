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
        private void Convert<T>(Store store, ValueBox box, T expected)
        {
            // Get kind to convert to based on type parameter
            Value.TryGetKind(typeof(T), out var convertToKind).Should().BeTrue();

            // Convert to a value with a different type
            var value = box.ToValue(store, convertToKind);

            // Check that the value is as expected
            var fromValue = (T)value.ToObject(Store);
            fromValue.Should().Be(expected);

            // Convert back into a box
            var box2 = value.ToValueBox(store);

            // Check that the new box has the right value
            ValueBox.Converter<T>().Unbox(Store, box2).Should().Be(expected);
        }

        /// <summary>
        /// Convert a box to a given kind and check that the conversion fails
        /// </summary>
        private static void FailConvert(Store store, ValueBox box, ValueKind kind)
        {
            var act = () =>
            {
                box.ToValue(store, kind);
            };

            act.Should().Throw<InvalidCastException>();
        }

        [Fact]
        public void ItConvertsInt()
        {
            ValueBox box = 7;

            box.Kind.Should().Be(ValueKind.Int32);

            Convert(Store, box, 7);
            Convert(Store, box, 7L);
            Convert(Store, box, 7f);
            Convert(Store, box, 7d);

            FailConvert(Store, box, ValueKind.ExternRef);
            FailConvert(Store, box, ValueKind.FuncRef);
            FailConvert(Store, box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsLong()
        {
            ValueBox box = 7L;

            box.Kind.Should().Be(ValueKind.Int64);

            Convert(Store, box, 7);
            Convert(Store, box, 7L);
            Convert(Store, box, 7f);
            Convert(Store, box, 7d);

            FailConvert(Store, box, ValueKind.ExternRef);
            FailConvert(Store, box, ValueKind.FuncRef);
            FailConvert(Store, box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsFloat()
        {
            ValueBox box = 7f;

            box.Kind.Should().Be(ValueKind.Float32);

            Convert(Store, box, 7);
            Convert(Store, box, 7L);
            Convert(Store, box, 7f);
            Convert(Store, box, 7d);

            FailConvert(Store, box, ValueKind.ExternRef);
            FailConvert(Store, box, ValueKind.FuncRef);
            FailConvert(Store, box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsDouble()
        {
            ValueBox box = 7d;

            box.Kind.Should().Be(ValueKind.Float64);

            Convert(Store, box, 7);
            Convert(Store, box, 7L);
            Convert(Store, box, 7f);
            Convert(Store, box, 7d);

            FailConvert(Store, box, ValueKind.ExternRef);
            FailConvert(Store, box, ValueKind.FuncRef);
            FailConvert(Store, box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsV128()
        {
            var v = new V128(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
            ValueBox box = v;

            box.Kind.Should().Be(ValueKind.V128);

            FailConvert(Store, box, ValueKind.Int32);
            FailConvert(Store, box, ValueKind.Int64);
            FailConvert(Store, box, ValueKind.Float32);
            FailConvert(Store, box, ValueKind.Float64);
            FailConvert(Store, box, ValueKind.ExternRef);
            FailConvert(Store, box, ValueKind.FuncRef);

            Convert(Store, box, v);
        }

        [Fact]
        public void ItConvertsByteArrayToV128()
        {
            var b = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var box = (ValueBox)b;
            Convert(Store, box, new V128(b));
        }

        [Fact]
        public void ItConvertsByteSpanToV128()
        {
            ReadOnlySpan<byte> b = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 };
            var box = (ValueBox)b;
            Convert(Store, box, new V128(b));
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

            FailConvert(Store, box, ValueKind.Int32);
            FailConvert(Store, box, ValueKind.Int64);
            FailConvert(Store, box, ValueKind.Float32);
            FailConvert(Store, box, ValueKind.Float64);

            Convert(Store, box, o);

            FailConvert(Store, box, ValueKind.FuncRef);
            FailConvert(Store, box, ValueKind.V128);
        }

        [Fact]
        public void ItConvertsFuncRef()
        {
            var func = Function.FromCallback(Store, ItConvertsFuncRef);
            ValueBox box = func;

            box.Kind.Should().Be(ValueKind.FuncRef);

            FailConvert(Store, box, ValueKind.Int32);
            FailConvert(Store, box, ValueKind.Int64);
            FailConvert(Store, box, ValueKind.Float32);
            FailConvert(Store, box, ValueKind.Float64);
            FailConvert(Store, box, ValueKind.ExternRef);
            FailConvert(Store, box, ValueKind.V128);

            // Checking a func for equality is different to all the other types so the normal "Convert" method cannot be used here

            var converted = box.ToValue(Store, ValueKind.FuncRef).ToValueBox(Store);
            converted.Kind.Should().Be(ValueKind.FuncRef);
            var unboxed = ValueBox.Converter<Function>().Unbox(Store, converted);
            unboxed.func.__private.Should().Be(func.func.__private);
            unboxed.func.store.Should().Be(func.func.store);

            var value = box.ToValue(Store, ValueKind.FuncRef);
            var obj = (Function)value.ToObject(Store)!;
            obj.func.__private.Should().Be(func.func.__private);
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
