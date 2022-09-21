using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wasmtime
{
    interface IReturnTypeFactory<out TReturn>
    {
        TReturn Create(IStore store, IntPtr trap, Span<Value> values);

        static IReturnTypeFactory<TReturn> Create()
        {
            // First, check if the value is a result builder
            var resultInterface = typeof(TReturn).TryGetResultInterface();
            if (resultInterface != null)
            {
                if (resultInterface.GetGenericTypeDefinition() == typeof(IActionResult<,>))
                {
                    var genericArgs = resultInterface.GetGenericArguments();
                    var builderType = genericArgs[1];
                    var returnType = genericArgs[0];

                    return (IReturnTypeFactory<TReturn>)Activator.CreateInstance(typeof(ActionResultFactory<,>).MakeGenericType(returnType, builderType))!;
                }

                if (resultInterface.GetGenericTypeDefinition() == typeof(IFunctionResult<,,>))
                {
                    var genericArgs = resultInterface.GetGenericArguments();
                    var resultType = genericArgs[0];
                    var valueType = genericArgs[1];
                    var builderType = genericArgs[2];

                    return (IReturnTypeFactory<TReturn>)Activator.CreateInstance(typeof(FunctionResultFactory<,,>).MakeGenericType(resultType, valueType, builderType))!;
                }

                // If this happens checks that this method and `TryGetResultInterface` both handle the same set of interfaces!
                throw new InvalidOperationException("Unknown Result type");
            }
            else
            {
                var types = GetTupleTypes().ToList();

                if (types.Count == 1)
                {
                    return new NonTupleTypeFactory<TReturn>();
                }

                // All of the factories take parameters: <TupleType, Item1Type, Item2Type... etc>
                // Add TupleType to the start of the list
                types.Insert(0, typeof(TReturn));

                Type factoryType = GetTupleFactoryType(types.Count - 1);
                return (IReturnTypeFactory<TReturn>)Activator.CreateInstance(factoryType.MakeGenericType(types.ToArray()))!;
            }
        }

        private static Type GetTupleFactoryType(int arity)
        {
            return arity switch
            {
                2 => typeof(TupleFactory2<,,>),
                3 => typeof(TupleFactory3<,,,>),
                4 => typeof(TupleFactory4<,,,,>),
                5 => typeof(TupleFactory5<,,,,,>),
                6 => typeof(TupleFactory6<,,,,,,>),
                7 => typeof(TupleFactory7<,,,,,,,>),
                _ => throw new InvalidOperationException("Too many return types in tuple"),
            };
        }

        private static IReadOnlyList<Type> GetTupleTypes()
        {
            if (typeof(ITuple).IsAssignableFrom(typeof(TReturn)))
            {
                return typeof(TReturn).GetGenericArguments();
            }
            else
            {
                return new[] { typeof(TReturn) };
            }
        }
    }

    internal class ActionResultFactory<TResult, TBuilder>
        : IReturnTypeFactory<TResult>
        where TBuilder : struct, IActionResultBuilder<TResult>
        where TResult : struct, IActionResult<TResult, TBuilder>
    {
        public TResult Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap == IntPtr.Zero)
            {
                return default(TBuilder).Create();
            }
            else
            {
                using var accessor = new TrapAccessor(trap);
                return default(TBuilder).Create(accessor);
            }
        }
    }

    internal class FunctionResultFactory<TResult, TValue, TBuilder>
        : IReturnTypeFactory<TResult>
        where TBuilder : struct, IFunctionResultBuilder<TResult, TValue>
        where TResult : struct
    {
        private readonly IReturnTypeFactory<TValue> _valueFactory;

        public FunctionResultFactory()
        {
            _valueFactory = IReturnTypeFactory<TValue>.Create();
        }

        public TResult Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap == IntPtr.Zero)
            {
                var result = _valueFactory.Create(store, trap, values);
                return default(TBuilder).Create(result);
            }
            else
            {
                using var accessor = new TrapAccessor(trap);
                return default(TBuilder).Create(accessor);
            }
        }
    }

    internal class NonTupleTypeFactory<TReturn>
        : IReturnTypeFactory<TReturn>
    {
        private readonly IValueBoxConverter<TReturn> converter;

        public NonTupleTypeFactory()
        {
            converter = ValueBox.Converter<TReturn>();
        }

        public TReturn Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return converter.Unbox(store, values[0].ToValueBox());
        }
    }

    internal abstract class BaseTupleFactory<TReturn, TFunc>
        : IReturnTypeFactory<TReturn>
        where TFunc : MulticastDelegate
    {
        protected TFunc Factory { get; }

        protected BaseTupleFactory()
        {
            // Get all the generic arguments of TFunc. All of the Parameters, followed by the return type
            var args = typeof(TFunc).GetGenericArguments();

            Factory = (TFunc)GetCreateMethodInfo(args.Length - 1)
                .MakeGenericMethod(args[..^1])
                .CreateDelegate(typeof(TFunc));
        }

        protected static MethodInfo GetCreateMethodInfo(int arity)
        {
            return typeof(ValueTuple)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(a => a.Name == "Create")
                .Where(a => a.ContainsGenericParameters && a.IsGenericMethod)
                .First(a => a.GetGenericArguments().Length == arity);
        }

        public abstract TReturn Create(IStore store, IntPtr trap, Span<Value> values);
    }

    internal class TupleFactory2<TReturn, TA, TB>
        : BaseTupleFactory<TReturn, Func<TA, TB, TReturn>>
    {
        private readonly IValueBoxConverter<TA> converterA;
        private readonly IValueBoxConverter<TB> converterB;

        public TupleFactory2()
        {
            converterA = ValueBox.Converter<TA>();
            converterB = ValueBox.Converter<TB>();
        }

        public override TReturn Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(store, values[0].ToValueBox()),
                converterB.Unbox(store, values[1].ToValueBox())
            );
        }
    }

    internal class TupleFactory3<TReturn, TA, TB, TC>
        : BaseTupleFactory<TReturn, Func<TA, TB, TC, TReturn>>
    {
        private readonly IValueBoxConverter<TA> converterA;
        private readonly IValueBoxConverter<TB> converterB;
        private readonly IValueBoxConverter<TC> converterC;

        public TupleFactory3()
        {
            converterA = ValueBox.Converter<TA>();
            converterB = ValueBox.Converter<TB>();
            converterC = ValueBox.Converter<TC>();
        }

        public override TReturn Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(store, values[0].ToValueBox()),
                converterB.Unbox(store, values[1].ToValueBox()),
                converterC.Unbox(store, values[2].ToValueBox())
            );
        }
    }

    internal class TupleFactory4<TReturn, TA, TB, TC, TD>
        : BaseTupleFactory<TReturn, Func<TA, TB, TC, TD, TReturn>>
    {
        private readonly IValueBoxConverter<TA> converterA;
        private readonly IValueBoxConverter<TB> converterB;
        private readonly IValueBoxConverter<TC> converterC;
        private readonly IValueBoxConverter<TD> converterD;

        public TupleFactory4()
        {
            converterA = ValueBox.Converter<TA>();
            converterB = ValueBox.Converter<TB>();
            converterC = ValueBox.Converter<TC>();
            converterD = ValueBox.Converter<TD>();
        }

        public override TReturn Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(store, values[0].ToValueBox()),
                converterB.Unbox(store, values[1].ToValueBox()),
                converterC.Unbox(store, values[2].ToValueBox()),
                converterD.Unbox(store, values[3].ToValueBox())
            );
        }
    }

    internal class TupleFactory5<TReturn, TA, TB, TC, TD, TE>
        : BaseTupleFactory<TReturn, Func<TA, TB, TC, TD, TE, TReturn>>
    {
        private readonly IValueBoxConverter<TA> converterA;
        private readonly IValueBoxConverter<TB> converterB;
        private readonly IValueBoxConverter<TC> converterC;
        private readonly IValueBoxConverter<TD> converterD;
        private readonly IValueBoxConverter<TE> converterE;

        public TupleFactory5()
        {
            converterA = ValueBox.Converter<TA>();
            converterB = ValueBox.Converter<TB>();
            converterC = ValueBox.Converter<TC>();
            converterD = ValueBox.Converter<TD>();
            converterE = ValueBox.Converter<TE>();
        }

        public override TReturn Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(store, values[0].ToValueBox()),
                converterB.Unbox(store, values[1].ToValueBox()),
                converterC.Unbox(store, values[2].ToValueBox()),
                converterD.Unbox(store, values[3].ToValueBox()),
                converterE.Unbox(store, values[4].ToValueBox())
            );
        }
    }

    internal class TupleFactory6<TReturn, TA, TB, TC, TD, TE, TF>
        : BaseTupleFactory<TReturn, Func<TA, TB, TC, TD, TE, TF, TReturn>>
    {
        private readonly IValueBoxConverter<TA> converterA;
        private readonly IValueBoxConverter<TB> converterB;
        private readonly IValueBoxConverter<TC> converterC;
        private readonly IValueBoxConverter<TD> converterD;
        private readonly IValueBoxConverter<TE> converterE;
        private readonly IValueBoxConverter<TF> converterF;

        public TupleFactory6()
        {
            converterA = ValueBox.Converter<TA>();
            converterB = ValueBox.Converter<TB>();
            converterC = ValueBox.Converter<TC>();
            converterD = ValueBox.Converter<TD>();
            converterE = ValueBox.Converter<TE>();
            converterF = ValueBox.Converter<TF>();
        }

        public override TReturn Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(store, values[0].ToValueBox()),
                converterB.Unbox(store, values[1].ToValueBox()),
                converterC.Unbox(store, values[2].ToValueBox()),
                converterD.Unbox(store, values[3].ToValueBox()),
                converterE.Unbox(store, values[4].ToValueBox()),
                converterF.Unbox(store, values[5].ToValueBox())
            );
        }
    }

    internal class TupleFactory7<TReturn, TA, TB, TC, TD, TE, TF, TG>
        : BaseTupleFactory<TReturn, Func<TA, TB, TC, TD, TE, TF, TG, TReturn>>
    {
        private readonly IValueBoxConverter<TA> converterA;
        private readonly IValueBoxConverter<TB> converterB;
        private readonly IValueBoxConverter<TC> converterC;
        private readonly IValueBoxConverter<TD> converterD;
        private readonly IValueBoxConverter<TE> converterE;
        private readonly IValueBoxConverter<TF> converterF;
        private readonly IValueBoxConverter<TG> converterG;

        public TupleFactory7()
        {
            converterA = ValueBox.Converter<TA>();
            converterB = ValueBox.Converter<TB>();
            converterC = ValueBox.Converter<TC>();
            converterD = ValueBox.Converter<TD>();
            converterE = ValueBox.Converter<TE>();
            converterF = ValueBox.Converter<TF>();
            converterG = ValueBox.Converter<TG>();
        }

        public override TReturn Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(store, values[0].ToValueBox()),
                converterB.Unbox(store, values[1].ToValueBox()),
                converterC.Unbox(store, values[2].ToValueBox()),
                converterD.Unbox(store, values[3].ToValueBox()),
                converterE.Unbox(store, values[4].ToValueBox()),
                converterF.Unbox(store, values[5].ToValueBox()),
                converterG.Unbox(store, values[6].ToValueBox())
            );
        }
    }
}
