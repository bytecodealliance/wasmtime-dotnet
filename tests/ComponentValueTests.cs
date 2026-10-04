using System;
using System.Collections.Generic;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests
{
    /// <summary>
    /// Round-trip tests for the component value marshaller.
    /// </summary>
    /// <remarks>
    /// These verify that the writer and the reader agree on the native encoding. They do not
    /// prove that the encoding matches Wasmtime's, which is fixed by the struct sizes and
    /// offsets taken from <c>wasmtime/component/val.h</c> and is exercised end to end once a
    /// component function is actually called.
    /// </remarks>
    public sealed class ComponentValueTests
    {
        private static ComponentValue RoundTrip(ComponentValue value)
        {
            using var scope = new ComponentValueMarshaller.AllocationScope();
            var pointer = scope.Allocate(ComponentValueMarshaller.ValueSize);
            ComponentValueMarshaller.Write(value, pointer, scope);
            return ComponentValueMarshaller.Read(pointer);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ItRoundTripsBool(bool value)
        {
            var result = RoundTrip(ComponentValue.Bool(value));

            result.Kind.Should().Be(ComponentValueKind.Bool);
            result.AsBool().Should().Be(value);
        }

        [Theory]
        [InlineData(sbyte.MinValue)]
        [InlineData((sbyte)-1)]
        [InlineData((sbyte)0)]
        [InlineData(sbyte.MaxValue)]
        public void ItRoundTripsS8(sbyte value)
        {
            RoundTrip(ComponentValue.S8(value)).AsS8().Should().Be(value);
        }

        [Theory]
        [InlineData(byte.MinValue)]
        [InlineData((byte)1)]
        [InlineData(byte.MaxValue)]
        public void ItRoundTripsU8(byte value)
        {
            RoundTrip(ComponentValue.U8(value)).AsU8().Should().Be(value);
        }

        [Theory]
        [InlineData(short.MinValue)]
        [InlineData((short)-1)]
        [InlineData(short.MaxValue)]
        public void ItRoundTripsS16(short value)
        {
            RoundTrip(ComponentValue.S16(value)).AsS16().Should().Be(value);
        }

        [Theory]
        [InlineData(ushort.MinValue)]
        [InlineData(ushort.MaxValue)]
        public void ItRoundTripsU16(ushort value)
        {
            RoundTrip(ComponentValue.U16(value)).AsU16().Should().Be(value);
        }

        [Theory]
        [InlineData(int.MinValue)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(int.MaxValue)]
        public void ItRoundTripsS32(int value)
        {
            RoundTrip(ComponentValue.S32(value)).AsS32().Should().Be(value);
        }

        [Theory]
        [InlineData(uint.MinValue)]
        [InlineData(1u)]
        [InlineData(uint.MaxValue)]
        public void ItRoundTripsU32(uint value)
        {
            RoundTrip(ComponentValue.U32(value)).AsU32().Should().Be(value);
        }

        [Theory]
        [InlineData(long.MinValue)]
        [InlineData(-1L)]
        [InlineData(long.MaxValue)]
        public void ItRoundTripsS64(long value)
        {
            RoundTrip(ComponentValue.S64(value)).AsS64().Should().Be(value);
        }

        [Theory]
        [InlineData(ulong.MinValue)]
        [InlineData(1ul)]
        [InlineData(ulong.MaxValue)]
        public void ItRoundTripsU64(ulong value)
        {
            RoundTrip(ComponentValue.U64(value)).AsU64().Should().Be(value);
        }

        [Theory]
        [InlineData(0.0f)]
        [InlineData(3.14159f)]
        [InlineData(float.MinValue)]
        [InlineData(float.MaxValue)]
        [InlineData(float.Epsilon)]
        [InlineData(float.NaN)]
        [InlineData(float.PositiveInfinity)]
        [InlineData(float.NegativeInfinity)]
        public void ItRoundTripsF32(float value)
        {
            RoundTrip(ComponentValue.F32(value)).AsF32().Should().Be(value);
        }

        /// <summary>
        /// <c>-0.0 == 0.0</c>, so the sign of zero is only observable through the bits.
        /// </summary>
        [Fact]
        public void ItPreservesTheSignOfZero()
        {
            var f32 = RoundTrip(ComponentValue.F32(-0.0f)).AsF32();
            BitConverter.SingleToInt32Bits(f32).Should().Be(BitConverter.SingleToInt32Bits(-0.0f));

            var f64 = RoundTrip(ComponentValue.F64(-0.0)).AsF64();
            BitConverter.DoubleToInt64Bits(f64).Should().Be(BitConverter.DoubleToInt64Bits(-0.0));
        }

        [Theory]
        [InlineData(0.0)]
        [InlineData(-2.718281828459045)]
        [InlineData(double.MinValue)]
        [InlineData(double.MaxValue)]
        [InlineData(double.Epsilon)]
        [InlineData(double.NaN)]
        [InlineData(double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity)]
        public void ItRoundTripsF64(double value)
        {
            RoundTrip(ComponentValue.F64(value)).AsF64().Should().Be(value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("hello")]
        [InlineData("h\u00e9llo \u4e16\u754c \ud83c\udf89")]
        [InlineData("embedded\0null")]
        public void ItRoundTripsString(string value)
        {
            var result = RoundTrip(ComponentValue.String(value));

            result.Kind.Should().Be(ComponentValueKind.String);
            result.AsString().Should().Be(value);
        }

        [Fact]
        public void ItRoundTripsEnum()
        {
            var result = RoundTrip(ComponentValue.Enum("warning"));

            result.Kind.Should().Be(ComponentValueKind.Enum);
            result.AsEnum().Should().Be("warning");
        }

        [Fact]
        public void ItRoundTripsEmptyList()
        {
            RoundTrip(ComponentValue.List([])).AsList().Should().BeEmpty();
        }

        [Fact]
        public void ItRoundTripsList()
        {
            var value = ComponentValue.List(new[]
            {
                ComponentValue.S32(1),
                ComponentValue.S32(-2),
                ComponentValue.S32(int.MaxValue),
            });

            var result = RoundTrip(value).AsList();

            result.Should().HaveCount(3);
            result[0].AsS32().Should().Be(1);
            result[1].AsS32().Should().Be(-2);
            result[2].AsS32().Should().Be(int.MaxValue);
        }

        [Fact]
        public void ItRoundTripsNestedLists()
        {
            var value = ComponentValue.List(new[]
            {
                ComponentValue.List(new[] { ComponentValue.String("a"), ComponentValue.String("b") }),
                ComponentValue.List([]),
                ComponentValue.List(new[] { ComponentValue.String("c") }),
            });

            var result = RoundTrip(value).AsList();

            result.Should().HaveCount(3);
            result[0].AsList().Should().HaveCount(2);
            result[0].AsList()[1].AsString().Should().Be("b");
            result[1].AsList().Should().BeEmpty();
            result[2].AsList()[0].AsString().Should().Be("c");
        }

        [Fact]
        public void ItRoundTripsTupleOfMixedKinds()
        {
            var value = ComponentValue.Tuple(new[]
            {
                ComponentValue.Bool(true),
                ComponentValue.String("two"),
                ComponentValue.F64(3.5),
            });

            var result = RoundTrip(value);

            result.Kind.Should().Be(ComponentValueKind.Tuple);
            result.AsTuple()[0].AsBool().Should().BeTrue();
            result.AsTuple()[1].AsString().Should().Be("two");
            result.AsTuple()[2].AsF64().Should().Be(3.5);
        }

        [Fact]
        public void ItRoundTripsRecordPreservingFieldOrder()
        {
            var value = ComponentValue.Record(new[]
            {
                new KeyValuePair<string, ComponentValue>("value", ComponentValue.F64(36.6)),
                new KeyValuePair<string, ComponentValue>("timestamp", ComponentValue.U64(1234567890123)),
            });

            var result = RoundTrip(value);

            result.Kind.Should().Be(ComponentValueKind.Record);
            result.AsRecord().Should().HaveCount(2);
            result.AsRecord()[0].Key.Should().Be("value");
            result.AsRecord()[1].Key.Should().Be("timestamp");
            result.Field("value").AsF64().Should().Be(36.6);
            result.Field("timestamp").AsU64().Should().Be(1234567890123);
        }

        [Fact]
        public void ItRoundTripsEmptyRecord()
        {
            RoundTrip(ComponentValue.Record([]))
                .AsRecord().Should().BeEmpty();
        }

        [Fact]
        public void ItRoundTripsSomeAndNone()
        {
            var some = RoundTrip(ComponentValue.Some(ComponentValue.S32(42)));
            some.Kind.Should().Be(ComponentValueKind.Option);
            some.IsSome.Should().BeTrue();
            some.Payload!.AsS32().Should().Be(42);

            var none = RoundTrip(ComponentValue.None());
            none.Kind.Should().Be(ComponentValueKind.Option);
            none.IsSome.Should().BeFalse();
            none.Payload.Should().BeNull();
        }

        [Fact]
        public void ItRoundTripsNestedOption()
        {
            var result = RoundTrip(ComponentValue.Some(ComponentValue.Some(ComponentValue.String("x"))));

            result.IsSome.Should().BeTrue();
            result.Payload!.IsSome.Should().BeTrue();
            result.Payload!.Payload!.AsString().Should().Be("x");
        }

        [Fact]
        public void ItRoundTripsResultWithPayload()
        {
            var ok = RoundTrip(ComponentValue.Ok(ComponentValue.S32(7)));
            ok.Kind.Should().Be(ComponentValueKind.Result);
            ok.IsOk.Should().BeTrue();
            ok.Payload!.AsS32().Should().Be(7);

            var err = RoundTrip(ComponentValue.Err(ComponentValue.String("boom")));
            err.IsOk.Should().BeFalse();
            err.Payload!.AsString().Should().Be("boom");
        }

        [Fact]
        public void ItRoundTripsResultWithoutPayload()
        {
            var ok = RoundTrip(ComponentValue.Ok());
            ok.IsOk.Should().BeTrue();
            ok.Payload.Should().BeNull();

            var err = RoundTrip(ComponentValue.Err());
            err.IsOk.Should().BeFalse();
            err.Payload.Should().BeNull();
        }

        [Fact]
        public void ItRoundTripsDeeplyNestedValues()
        {
            var value = ComponentValue.Record(new[]
            {
                new KeyValuePair<string, ComponentValue>("readings", ComponentValue.List(new[]
                {
                    ComponentValue.Record(new[]
                    {
                        new KeyValuePair<string, ComponentValue>("value", ComponentValue.F64(1.5)),
                        new KeyValuePair<string, ComponentValue>("note", ComponentValue.Some(ComponentValue.String("ok"))),
                    }),
                    ComponentValue.Record(new[]
                    {
                        new KeyValuePair<string, ComponentValue>("value", ComponentValue.F64(2.5)),
                        new KeyValuePair<string, ComponentValue>("note", ComponentValue.None()),
                    }),
                })),
                new KeyValuePair<string, ComponentValue>("level", ComponentValue.Enum("warning")),
            });

            var result = RoundTrip(value);
            var readings = result.Field("readings").AsList();

            readings.Should().HaveCount(2);
            readings[0].Field("value").AsF64().Should().Be(1.5);
            readings[0].Field("note").Payload!.AsString().Should().Be("ok");
            readings[1].Field("value").AsF64().Should().Be(2.5);
            readings[1].Field("note").IsSome.Should().BeFalse();
            result.Field("level").AsEnum().Should().Be("warning");
        }

        [Fact]
        public void ItThrowsReadingAnUnsupportedKind()
        {
            using var scope = new ComponentValueMarshaller.AllocationScope();
            var pointer = scope.Allocate(ComponentValueMarshaller.ValueSize);
            System.Runtime.InteropServices.Marshal.WriteByte(pointer, (byte)ComponentValueKind.Resource);

            var act = () => ComponentValueMarshaller.Read(pointer);

            act.Should().Throw<NotSupportedException>().WithMessage("*Resource*");
        }

        /// <summary>
        /// A native length is a <c>size_t</c>, so it can exceed what a managed length can hold.
        /// Truncating it would produce a negative or far-too-small length and read out of bounds,
        /// so it has to be rejected instead.
        /// </summary>
        [Theory]
        [InlineData(ComponentValueKind.String)]
        [InlineData(ComponentValueKind.List)]
        [InlineData(ComponentValueKind.Record)]
        public void ItRejectsALengthTooLargeToRepresent(ComponentValueKind kind)
        {
            using var scope = new ComponentValueMarshaller.AllocationScope();
            var pointer = scope.Allocate(ComponentValueMarshaller.ValueSize);

            System.Runtime.InteropServices.Marshal.WriteByte(pointer, (byte)kind);

            // All bits set reads back as a huge unsigned size on both 32- and 64-bit.
            System.Runtime.InteropServices.Marshal.WriteIntPtr(
                pointer + ComponentValueMarshaller.ValuePayloadOffset, new IntPtr(-1));

            // Non-null so the guard, rather than a null check, is what rejects this.
            System.Runtime.InteropServices.Marshal.WriteIntPtr(
                pointer + ComponentValueMarshaller.ValuePayloadOffset + 8, pointer);

            var act = () => ComponentValueMarshaller.Read(pointer);

            act.Should().Throw<NotSupportedException>()
                .WithMessage("*exceeds the maximum supported length*");
        }

        [Fact]
        public void ItThrowsAccessingTheWrongKind()
        {
            var value = ComponentValue.S32(1);

            value.Invoking(v => v.AsString()).Should().Throw<InvalidOperationException>();
            value.Invoking(v => v.AsRecord()).Should().Throw<InvalidOperationException>();
            value.Invoking(v => v.IsOk).Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ItCopiesTheCallersCollections()
        {
            var items = new List<ComponentValue> { ComponentValue.S32(1) };
            var fields = new List<KeyValuePair<string, ComponentValue>> { new("a", ComponentValue.S32(1)) };

            var list = ComponentValue.List(items);
            var tuple = ComponentValue.Tuple(items);
            var record = ComponentValue.Record(fields);

            items.Add(ComponentValue.S32(2));
            fields.Add(new("b", ComponentValue.S32(2)));

            list.AsList().Should().ContainSingle();
            tuple.AsTuple().Should().ContainSingle();
            record.AsRecord().Should().ContainSingle();
        }

        [Fact]
        public void ItThrowsForAMissingRecordField()
        {
            var value = ComponentValue.Record(new[]
            {
                new KeyValuePair<string, ComponentValue>("present", ComponentValue.S32(1)),
            });

            value.TryGetField("present", out var found).Should().BeTrue();
            found!.AsS32().Should().Be(1);

            value.TryGetField("absent", out var missing).Should().BeFalse();
            missing.Should().BeNull();

            value.Invoking(v => v.Field("absent"))
                .Should().Throw<InvalidOperationException>()
                .WithMessage("*absent*");
        }

        [Fact]
        public void ItRejectsNullArguments()
        {
            FluentActions.Invoking(() => ComponentValue.String(null!)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => ComponentValue.Enum(null!)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => ComponentValue.List(null!)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => ComponentValue.Tuple(null!)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => ComponentValue.Record(null!)).Should().Throw<ArgumentNullException>();
            FluentActions.Invoking(() => ComponentValue.Some(null!)).Should().Throw<ArgumentNullException>();
        }
    }
}
