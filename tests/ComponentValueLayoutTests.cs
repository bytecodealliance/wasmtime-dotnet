using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FluentAssertions;
using Wasmtime.Components;
using Xunit;

namespace Wasmtime.Tests
{
    /// <summary>
    /// Validates the native encoding of component values against Wasmtime itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ComponentValueTests"/> only proves that our writer and reader agree with each
    /// other; it would still pass if every offset were wrong in the same way. These tests close
    /// that gap by routing each value through <c>wasmtime_component_val_clone</c>, which walks
    /// the structure using Wasmtime's own definition of the layout:
    /// </para>
    /// <list type="number">
    /// <item><description>we encode a value into <c>source</c>;</description></item>
    /// <item><description>Wasmtime deep-copies it, reading <c>source</c> with its layout;</description></item>
    /// <item><description>we decode <c>destination</c>, which Wasmtime wrote with its layout.</description></item>
    /// </list>
    /// <para>
    /// A mismatch in any size or offset therefore corrupts the value or crashes, rather than
    /// cancelling out. This needs no engine, store or component.
    /// </para>
    /// </remarks>
    public sealed class ComponentValueLayoutTests
    {
        private static ComponentValue Clone(ComponentValue value)
        {
            using var scope = new ComponentValueMarshaller.AllocationScope();
            var source = scope.Allocate(ComponentValueMarshaller.ValueSize);
            ComponentValueMarshaller.Write(value, source, scope);

            var destination = Marshal.AllocHGlobal(ComponentValueMarshaller.ValueSize);
            try
            {
                unsafe
                {
                    new Span<byte>((void*)destination, ComponentValueMarshaller.ValueSize).Clear();
                }

                ComponentValueNative.wasmtime_component_val_clone(source, destination);
                return ComponentValueMarshaller.Read(destination);
            }
            finally
            {
                ComponentValueNative.wasmtime_component_val_delete(destination);
                Marshal.FreeHGlobal(destination);
            }
        }

        /// <summary>
        /// Clones the value through Wasmtime and asserts the result is structurally identical.
        /// </summary>
        private static void Survives(ComponentValue value)
        {
            AssertEquivalent(value, Clone(value), "$");
        }

        private static void AssertEquivalent(ComponentValue expected, ComponentValue actual, string path)
        {
            actual.Kind.Should().Be(expected.Kind, "kind at {0}", path);

            switch (expected.Kind)
            {
                case ComponentValueKind.Bool:
                    actual.AsBool().Should().Be(expected.AsBool(), "value at {0}", path);
                    break;

                case ComponentValueKind.S8:
                case ComponentValueKind.U8:
                case ComponentValueKind.S16:
                case ComponentValueKind.U16:
                case ComponentValueKind.S32:
                case ComponentValueKind.U32:
                case ComponentValueKind.S64:
                case ComponentValueKind.U64:
                case ComponentValueKind.Char:
                    actual.Integer.Should().Be(expected.Integer, "value at {0}", path);
                    break;

                case ComponentValueKind.F32:
                case ComponentValueKind.F64:
                    actual.Real.Should().Be(expected.Real, "value at {0}", path);
                    break;

                case ComponentValueKind.String:
                case ComponentValueKind.Enum:
                    actual.Text.Should().Be(expected.Text, "text at {0}", path);
                    break;

                case ComponentValueKind.List:
                case ComponentValueKind.Tuple:
                    actual.Items.Should().HaveCount(expected.Items.Count, "length at {0}", path);
                    for (var i = 0; i < expected.Items.Count; i++)
                    {
                        AssertEquivalent(expected.Items[i], actual.Items[i], $"{path}[{i}]");
                    }

                    break;

                case ComponentValueKind.Record:
                    actual.Fields.Should().HaveCount(expected.Fields.Count, "field count at {0}", path);
                    for (var i = 0; i < expected.Fields.Count; i++)
                    {
                        actual.Fields[i].Key.Should().Be(expected.Fields[i].Key, "field name at {0}[{1}]", path, i);
                        AssertEquivalent(
                            expected.Fields[i].Value,
                            actual.Fields[i].Value,
                            $"{path}.{expected.Fields[i].Key}");
                    }

                    break;

                case ComponentValueKind.Option:
                case ComponentValueKind.Result:
                case ComponentValueKind.Variant:
                    actual.Text.Should().Be(expected.Text, "case name at {0}", path);
                    actual.Flag.Should().Be(expected.Flag, "discriminant at {0}", path);
                    if (expected.Payload is null)
                    {
                        actual.Payload.Should().BeNull("payload at {0}", path);
                    }
                    else
                    {
                        actual.Payload.Should().NotBeNull("payload at {0}", path);
                        AssertEquivalent(expected.Payload, actual.Payload!, $"{path}.payload");
                    }

                    break;

                case ComponentValueKind.Flags:
                    actual.AsFlags().Should().Equal(expected.AsFlags(), "flags at {0}", path);
                    break;

                default:
                    throw new NotSupportedException($"No comparison for kind {expected.Kind}.");
            }
        }

        [Fact]
        public void ExtendedValuesSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.Char(0));
            Survives(ComponentValue.Char(0x1F600));
            Survives(ComponentValue.Char(0x10FFFF));
            Survives(ComponentValue.Flags(Array.Empty<string>()));
            Survives(ComponentValue.Flags(new[] { "read", "write", "f69" }));
            Survives(ComponentValue.Variant("empty"));
            Survives(ComponentValue.Variant("nested", ComponentValue.Some(ComponentValue.List(new[]
            {
                ComponentValue.Variant("scalar", ComponentValue.Char(0x1F600)),
                ComponentValue.Variant("text", ComponentValue.String("payload")),
                ComponentValue.Variant("flags", ComponentValue.Flags(new[] { "read", "write" })),
            }))));
        }

        [Fact]
        public void ScalarsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.Bool(true));
            Survives(ComponentValue.Bool(false));
            Survives(ComponentValue.S8(sbyte.MinValue));
            Survives(ComponentValue.S8(-1));
            Survives(ComponentValue.U8(byte.MaxValue));
            Survives(ComponentValue.S16(short.MinValue));
            Survives(ComponentValue.U16(ushort.MaxValue));
            Survives(ComponentValue.S32(int.MinValue));
            Survives(ComponentValue.S32(-1));
            Survives(ComponentValue.U32(uint.MaxValue));
            Survives(ComponentValue.S64(long.MinValue));
            Survives(ComponentValue.U64(ulong.MaxValue));
            Survives(ComponentValue.F32(float.MinValue));
            Survives(ComponentValue.F32(float.NaN));
            Survives(ComponentValue.F64(-2.718281828459045));
            Survives(ComponentValue.F64(double.PositiveInfinity));
        }

        [Fact]
        public void StringsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.String(string.Empty));
            Survives(ComponentValue.String("hello"));
            Survives(ComponentValue.String("h\u00e9llo \u4e16\u754c \ud83c\udf89"));
            Survives(ComponentValue.String(new string('x', 4096)));
        }

        [Fact]
        public void EnumsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.Enum("warning"));
            Survives(ComponentValue.Enum("a"));
        }

        [Fact]
        public void ListsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.List([]));
            Survives(ComponentValue.List(new[] { ComponentValue.S32(1) }));
            Survives(ComponentValue.List(new[]
            {
                ComponentValue.S32(1),
                ComponentValue.S32(-2),
                ComponentValue.S32(int.MaxValue),
            }));
            Survives(ComponentValue.List(new[]
            {
                ComponentValue.String("first"),
                ComponentValue.String(string.Empty),
                ComponentValue.String("third"),
            }));
        }

        [Fact]
        public void NestedListsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.List(new[]
            {
                ComponentValue.List(new[] { ComponentValue.String("a"), ComponentValue.String("b") }),
                ComponentValue.List([]),
                ComponentValue.List(new[] { ComponentValue.String("c") }),
            }));
        }

        [Fact]
        public void TuplesSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.Tuple(new[]
            {
                ComponentValue.Bool(true),
                ComponentValue.String("two"),
                ComponentValue.F64(3.5),
            }));
        }

        [Fact]
        public void RecordsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.Record([]));
            Survives(ComponentValue.Record(new[]
            {
                new KeyValuePair<string, ComponentValue>("value", ComponentValue.F64(36.6)),
                new KeyValuePair<string, ComponentValue>("timestamp", ComponentValue.U64(1234567890123)),
            }));
        }

        [Fact]
        public void OptionsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.None());
            Survives(ComponentValue.Some(ComponentValue.S32(42)));
            Survives(ComponentValue.Some(ComponentValue.String("boxed")));
            Survives(ComponentValue.Some(ComponentValue.Some(ComponentValue.String("x"))));
        }

        [Fact]
        public void ResultsSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.Ok());
            Survives(ComponentValue.Err());
            Survives(ComponentValue.Ok(ComponentValue.S32(7)));
            Survives(ComponentValue.Err(ComponentValue.String("boom")));
        }

        [Fact]
        public void DeeplyNestedValuesSurviveACloneThroughWasmtime()
        {
            Survives(ComponentValue.Record(new[]
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
                new KeyValuePair<string, ComponentValue>("outcome", ComponentValue.Ok(ComponentValue.Tuple(new[]
                {
                    ComponentValue.U16(200),
                    ComponentValue.String("done"),
                }))),
            }));
        }

        /// <summary>
        /// Host function results are written with <see cref="ComponentValueMarshaller.WriteOwned"/>
        /// and then freed by Wasmtime, so every heap payload must come from Wasmtime's allocator.
        /// Freeing memory allocated by the wrong allocator corrupts the heap, so this deletes the
        /// value after reading it back.
        /// </summary>
        [Fact]
        public void OwnedWritesUseWasmtimesAllocator()
        {
            var values = new[]
            {
                ComponentValue.S32(7),
                ComponentValue.String("a host-allocated string"),
                ComponentValue.String(string.Empty),
                ComponentValue.Enum("warning"),
                ComponentValue.List([]),
                ComponentValue.List(new[] { ComponentValue.String("a"), ComponentValue.String("b") }),
                ComponentValue.Tuple(new[] { ComponentValue.S32(3), ComponentValue.String("tuple") }),
                ComponentValue.Record(new[]
                {
                    new KeyValuePair<string, ComponentValue>("name", ComponentValue.String("x")),
                    new KeyValuePair<string, ComponentValue>("value", ComponentValue.F64(1.5)),
                }),
                ComponentValue.Record(new[]
                {
                    new KeyValuePair<string, ComponentValue>("items", ComponentValue.List(new[]
                    {
                        ComponentValue.Some(ComponentValue.String("nested")),
                        ComponentValue.None(),
                    })),
                    new KeyValuePair<string, ComponentValue>("result", ComponentValue.Ok(ComponentValue.Tuple(new[]
                    {
                        ComponentValue.U16(12),
                        ComponentValue.String("done"),
                    }))),
                }),
                ComponentValue.Some(ComponentValue.String("boxed")),
                ComponentValue.None(),
                ComponentValue.Ok(ComponentValue.S32(1)),
                ComponentValue.Err(ComponentValue.String("boom")),
            };

            foreach (var value in values)
            {
                var destination = Marshal.AllocHGlobal(ComponentValueMarshaller.ValueSize);
                try
                {
                    unsafe
                    {
                        new Span<byte>((void*)destination, ComponentValueMarshaller.ValueSize).Clear();
                    }

                    ComponentValueMarshaller.WriteOwned(value, destination);
                    AssertEquivalent(value, ComponentValueMarshaller.Read(destination), "$");
                }
                finally
                {
                    ComponentValueNative.wasmtime_component_val_delete(destination);
                    Marshal.FreeHGlobal(destination);
                }
            }
        }

        /// <summary>
        /// A large list exercises the element stride, which a single-element list cannot: an
        /// over-estimated <see cref="ComponentValueMarshaller.ValueSize"/> still round-trips one
        /// element correctly but misaligns every element after the first.
        /// </summary>
        [Fact]
        public void ElementStrideSurvivesACloneThroughWasmtime()
        {
            var items = new ComponentValue[64];
            for (var i = 0; i < items.Length; i++)
            {
                items[i] = ComponentValue.Record(new[]
                {
                    new KeyValuePair<string, ComponentValue>("index", ComponentValue.S32(i)),
                    new KeyValuePair<string, ComponentValue>("name", ComponentValue.String($"item-{i}")),
                });
            }

            var result = Clone(ComponentValue.List(items)).AsList();

            result.Should().HaveCount(items.Length);
            for (var i = 0; i < items.Length; i++)
            {
                result[i].Field("index").AsS32().Should().Be(i);
                result[i].Field("name").AsString().Should().Be($"item-{i}");
            }
        }
    }
}
