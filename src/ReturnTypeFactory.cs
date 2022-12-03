using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Wasmtime
{
    interface IReturnTypeFactory<out TReturn>
    {
        TReturn? Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values);

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
        public TResult Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
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

        public TResult Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap == IntPtr.Zero)
            {
                var result = _valueFactory.Create(storeContext, store, trap, values);
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
        private readonly IValueRawConverter<TReturn> converter;

        public NonTupleTypeFactory()
        {
            converter = ValueRaw.Converter<TReturn>();
        }

        public TReturn? Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return converter.Unbox(storeContext, store, values[0]);
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

        public abstract TReturn? Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values);
    }

    internal class TupleFactory2<TReturn, TA, TB>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA;
        private readonly IValueRawConverter<TB> converterB;

        public TupleFactory2()
        {
            converterA = ValueRaw.Converter<TA>();
            converterB = ValueRaw.Converter<TB>();
        }

        public override TReturn Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(storeContext, store, values[0]),
                converterB.Unbox(storeContext, store, values[1])
            );
        }
    }

    internal class TupleFactory3<TReturn, TA, TB, TC>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA;
        private readonly IValueRawConverter<TB> converterB;
        private readonly IValueRawConverter<TC> converterC;

        public TupleFactory3()
        {
            converterA = ValueRaw.Converter<TA>();
            converterB = ValueRaw.Converter<TB>();
            converterC = ValueRaw.Converter<TC>();
        }

        public override TReturn Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(storeContext, store, values[0]),
                converterB.Unbox(storeContext, store, values[1]),
                converterC.Unbox(storeContext, store, values[2])
            );
        }
    }

    internal class TupleFactory4<TReturn, TA, TB, TC, TD>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA;
        private readonly IValueRawConverter<TB> converterB;
        private readonly IValueRawConverter<TC> converterC;
        private readonly IValueRawConverter<TD> converterD;

        public TupleFactory4()
        {
            converterA = ValueRaw.Converter<TA>();
            converterB = ValueRaw.Converter<TB>();
            converterC = ValueRaw.Converter<TC>();
            converterD = ValueRaw.Converter<TD>();
        }

        public override TReturn Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(storeContext, store, values[0]),
                converterB.Unbox(storeContext, store, values[1]),
                converterC.Unbox(storeContext, store, values[2]),
                converterD.Unbox(storeContext, store, values[3])
            );
        }
    }

    internal class TupleFactory5<TReturn, TA, TB, TC, TD, TE>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TE?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA;
        private readonly IValueRawConverter<TB> converterB;
        private readonly IValueRawConverter<TC> converterC;
        private readonly IValueRawConverter<TD> converterD;
        private readonly IValueRawConverter<TE> converterE;

        public TupleFactory5()
        {
            converterA = ValueRaw.Converter<TA>();
            converterB = ValueRaw.Converter<TB>();
            converterC = ValueRaw.Converter<TC>();
            converterD = ValueRaw.Converter<TD>();
            converterE = ValueRaw.Converter<TE>();
        }

        public override TReturn Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(storeContext, store, values[0]),
                converterB.Unbox(storeContext, store, values[1]),
                converterC.Unbox(storeContext, store, values[2]),
                converterD.Unbox(storeContext, store, values[3]),
                converterE.Unbox(storeContext, store, values[4])
            );
        }
    }

    internal class TupleFactory6<TReturn, TA, TB, TC, TD, TE, TF>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TE?, TF?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA;
        private readonly IValueRawConverter<TB> converterB;
        private readonly IValueRawConverter<TC> converterC;
        private readonly IValueRawConverter<TD> converterD;
        private readonly IValueRawConverter<TE> converterE;
        private readonly IValueRawConverter<TF> converterF;

        public TupleFactory6()
        {
            converterA = ValueRaw.Converter<TA>();
            converterB = ValueRaw.Converter<TB>();
            converterC = ValueRaw.Converter<TC>();
            converterD = ValueRaw.Converter<TD>();
            converterE = ValueRaw.Converter<TE>();
            converterF = ValueRaw.Converter<TF>();
        }

        public override TReturn Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(storeContext, store, values[0]),
                converterB.Unbox(storeContext, store, values[1]),
                converterC.Unbox(storeContext, store, values[2]),
                converterD.Unbox(storeContext, store, values[3]),
                converterE.Unbox(storeContext, store, values[4]),
                converterF.Unbox(storeContext, store, values[5])
            );
        }
    }

    internal class TupleFactory7<TReturn, TA, TB, TC, TD, TE, TF, TG>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TE?, TF?, TG?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA;
        private readonly IValueRawConverter<TB> converterB;
        private readonly IValueRawConverter<TC> converterC;
        private readonly IValueRawConverter<TD> converterD;
        private readonly IValueRawConverter<TE> converterE;
        private readonly IValueRawConverter<TF> converterF;
        private readonly IValueRawConverter<TG> converterG;

        public TupleFactory7()
        {
            converterA = ValueRaw.Converter<TA>();
            converterB = ValueRaw.Converter<TB>();
            converterC = ValueRaw.Converter<TC>();
            converterD = ValueRaw.Converter<TD>();
            converterE = ValueRaw.Converter<TE>();
            converterF = ValueRaw.Converter<TF>();
            converterG = ValueRaw.Converter<TG>();
        }

        public override TReturn Create(StoreContext storeContext, IStore store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                converterA.Unbox(storeContext, store, values[0]),
                converterB.Unbox(storeContext, store, values[1]),
                converterC.Unbox(storeContext, store, values[2]),
                converterD.Unbox(storeContext, store, values[3]),
                converterE.Unbox(storeContext, store, values[4]),
                converterF.Unbox(storeContext, store, values[5]),
                converterG.Unbox(storeContext, store, values[6])
            );
        }
    }
}
