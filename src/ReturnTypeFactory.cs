using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Wasmtime
{
    interface IReturnTypeFactory<out TReturn>
    {
        TReturn? Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values);

        TReturn? Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values);
    }

    internal static class ReturnTypeFactory<TReturn>
    {
        public static IReturnTypeFactory<TReturn> Create()
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
                var types = GetTupleTypes();

                if (types == null)
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

        /// <summary>
        /// If `TReturn` is a tuple get a list of types it contains, otherwise return null
        /// </summary>
        /// <returns></returns>
        private static List<Type>? GetTupleTypes()
        {
            if (typeof(TReturn).IsTupleType())
            {
                return typeof(TReturn).GetGenericArguments().ToList();
            }
            else
            {
                return null;
            }
        }
    }

    internal class ActionResultFactory<TResult, TBuilder>
        : IReturnTypeFactory<TResult>
        where TBuilder : struct, IActionResultBuilder<TResult>
        where TResult : struct, IActionResult<TResult, TBuilder>
    {
        public TResult Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public TResult Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
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
        private readonly IReturnTypeFactory<TValue> _valueFactory = ReturnTypeFactory<TValue>.Create();

        public TResult Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public TResult Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
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
        private readonly IValueRawConverter<TReturn> converter = ValueRaw.Converter<TReturn>();
        private readonly IValueBoxConverter<TReturn> boxConverter = ValueBox.Converter<TReturn>();

        public TReturn? Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return converter.Unbox(storeContext, store, values[0]);
        }

        public TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return boxConverter.Unbox(store, values[0]);
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

            Array.Resize(ref args, args.Length - 1);

            Factory = (TFunc)GetCreateMethodInfo(args.Length)
                .MakeGenericMethod(args)
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

        public abstract TReturn? Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values);

        public abstract TReturn? Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values);
    }

    internal class TupleFactory2<TReturn, TA, TB>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA = ValueRaw.Converter<TA>();
        private readonly IValueRawConverter<TB> converterB = ValueRaw.Converter<TB>();
        private readonly IValueBoxConverter<TA> boxConverterA = ValueBox.Converter<TA>();
        private readonly IValueBoxConverter<TB> boxConverterB = ValueBox.Converter<TB>();

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                boxConverterA.Unbox(store, values[0]),
                boxConverterB.Unbox(store, values[1])
            );
        }
    }

    internal class TupleFactory3<TReturn, TA, TB, TC>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA = ValueRaw.Converter<TA>();
        private readonly IValueRawConverter<TB> converterB = ValueRaw.Converter<TB>();
        private readonly IValueRawConverter<TC> converterC = ValueRaw.Converter<TC>();
        private readonly IValueBoxConverter<TA> boxConverterA = ValueBox.Converter<TA>();
        private readonly IValueBoxConverter<TB> boxConverterB = ValueBox.Converter<TB>();
        private readonly IValueBoxConverter<TC> boxConverterC = ValueBox.Converter<TC>();

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                boxConverterA.Unbox(store, values[0]),
                boxConverterB.Unbox(store, values[1]),
                boxConverterC.Unbox(store, values[2])
            );
        }
    }

    internal class TupleFactory4<TReturn, TA, TB, TC, TD>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA = ValueRaw.Converter<TA>();
        private readonly IValueRawConverter<TB> converterB = ValueRaw.Converter<TB>();
        private readonly IValueRawConverter<TC> converterC = ValueRaw.Converter<TC>();
        private readonly IValueRawConverter<TD> converterD = ValueRaw.Converter<TD>();
        private readonly IValueBoxConverter<TA> boxConverterA = ValueBox.Converter<TA>();
        private readonly IValueBoxConverter<TB> boxConverterB = ValueBox.Converter<TB>();
        private readonly IValueBoxConverter<TC> boxConverterC = ValueBox.Converter<TC>();
        private readonly IValueBoxConverter<TD> boxConverterD = ValueBox.Converter<TD>();

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                boxConverterA.Unbox(store, values[0]),
                boxConverterB.Unbox(store, values[1]),
                boxConverterC.Unbox(store, values[2]),
                boxConverterD.Unbox(store, values[3])
            );
        }
    }

    internal class TupleFactory5<TReturn, TA, TB, TC, TD, TE>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TE?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA = ValueRaw.Converter<TA>();
        private readonly IValueRawConverter<TB> converterB = ValueRaw.Converter<TB>();
        private readonly IValueRawConverter<TC> converterC = ValueRaw.Converter<TC>();
        private readonly IValueRawConverter<TD> converterD = ValueRaw.Converter<TD>();
        private readonly IValueRawConverter<TE> converterE = ValueRaw.Converter<TE>();
        private readonly IValueBoxConverter<TA> boxConverterA = ValueBox.Converter<TA>();
        private readonly IValueBoxConverter<TB> boxConverterB = ValueBox.Converter<TB>();
        private readonly IValueBoxConverter<TC> boxConverterC = ValueBox.Converter<TC>();
        private readonly IValueBoxConverter<TD> boxConverterD = ValueBox.Converter<TD>();
        private readonly IValueBoxConverter<TE> boxConverterE = ValueBox.Converter<TE>();

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                boxConverterA.Unbox(store, values[0]),
                boxConverterB.Unbox(store, values[1]),
                boxConverterC.Unbox(store, values[2]),
                boxConverterD.Unbox(store, values[3]),
                boxConverterE.Unbox(store, values[4])
            );
        }
    }

    internal class TupleFactory6<TReturn, TA, TB, TC, TD, TE, TF>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TE?, TF?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA = ValueRaw.Converter<TA>();
        private readonly IValueRawConverter<TB> converterB = ValueRaw.Converter<TB>();
        private readonly IValueRawConverter<TC> converterC = ValueRaw.Converter<TC>();
        private readonly IValueRawConverter<TD> converterD = ValueRaw.Converter<TD>();
        private readonly IValueRawConverter<TE> converterE = ValueRaw.Converter<TE>();
        private readonly IValueRawConverter<TF> converterF = ValueRaw.Converter<TF>();
        private readonly IValueBoxConverter<TA> boxConverterA = ValueBox.Converter<TA>();
        private readonly IValueBoxConverter<TB> boxConverterB = ValueBox.Converter<TB>();
        private readonly IValueBoxConverter<TC> boxConverterC = ValueBox.Converter<TC>();
        private readonly IValueBoxConverter<TD> boxConverterD = ValueBox.Converter<TD>();
        private readonly IValueBoxConverter<TE> boxConverterE = ValueBox.Converter<TE>();
        private readonly IValueBoxConverter<TF> boxConverterF = ValueBox.Converter<TF>();

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                boxConverterA.Unbox(store, values[0]),
                boxConverterB.Unbox(store, values[1]),
                boxConverterC.Unbox(store, values[2]),
                boxConverterD.Unbox(store, values[3]),
                boxConverterE.Unbox(store, values[4]),
                boxConverterF.Unbox(store, values[5])
            );
        }
    }

    internal class TupleFactory7<TReturn, TA, TB, TC, TD, TE, TF, TG>
        : BaseTupleFactory<TReturn, Func<TA?, TB?, TC?, TD?, TE?, TF?, TG?, TReturn>>
    {
        private readonly IValueRawConverter<TA> converterA = ValueRaw.Converter<TA>();
        private readonly IValueRawConverter<TB> converterB = ValueRaw.Converter<TB>();
        private readonly IValueRawConverter<TC> converterC = ValueRaw.Converter<TC>();
        private readonly IValueRawConverter<TD> converterD = ValueRaw.Converter<TD>();
        private readonly IValueRawConverter<TE> converterE = ValueRaw.Converter<TE>();
        private readonly IValueRawConverter<TF> converterF = ValueRaw.Converter<TF>();
        private readonly IValueRawConverter<TG> converterG = ValueRaw.Converter<TG>();
        private readonly IValueBoxConverter<TA> boxConverterA = ValueBox.Converter<TA>();
        private readonly IValueBoxConverter<TB> boxConverterB = ValueBox.Converter<TB>();
        private readonly IValueBoxConverter<TC> boxConverterC = ValueBox.Converter<TC>();
        private readonly IValueBoxConverter<TD> boxConverterD = ValueBox.Converter<TD>();
        private readonly IValueBoxConverter<TE> boxConverterE = ValueBox.Converter<TE>();
        private readonly IValueBoxConverter<TF> boxConverterF = ValueBox.Converter<TF>();
        private readonly IValueBoxConverter<TG> boxConverterG = ValueBox.Converter<TG>();

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<ValueRaw> values)
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

        public override TReturn Create(StoreContext storeContext, Store store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return Factory(
                boxConverterA.Unbox(store, values[0]),
                boxConverterB.Unbox(store, values[1]),
                boxConverterC.Unbox(store, values[2]),
                boxConverterD.Unbox(store, values[3]),
                boxConverterE.Unbox(store, values[4]),
                boxConverterF.Unbox(store, values[5]),
                boxConverterG.Unbox(store, values[6])
            );
        }
    }
}
