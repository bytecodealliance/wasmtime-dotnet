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
            // First, check if the value is one of the 4 result wrappers
            if (typeof(TReturn).IsResult())
            {
                if (typeof(TReturn).IsGenericType)
                {
                    var wrapperType = typeof(TReturn).GetGenericTypeDefinition();
                    var wrappedType = typeof(TReturn).GetGenericArguments()[0];

                    var factoryType = wrapperType == typeof(Result<>)
                        ? typeof(ResultTFactory<>)
                        : typeof(ResultWithBacktraceTFactory<>);

                    return (IReturnTypeFactory<TReturn>)Activator.CreateInstance(factoryType.MakeGenericType(wrappedType))!;
                }
                else
                {
                    var wrapperType = typeof(TReturn);

                    var factoryType = wrapperType == typeof(Result)
                        ? typeof(ResultFactory)
                        : typeof(ResultWithBacktraceFactory);

                    return (IReturnTypeFactory<TReturn>)Activator.CreateInstance(factoryType)!;
                }
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

    internal class ResultFactory
        : IReturnTypeFactory<Result>
    {
        public Result Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                return new Result(trap);
            }
            else
            {
                return new Result();
            }
        }
    }

    internal class ResultTFactory<TReturn>
        : IReturnTypeFactory<Result<TReturn>>
    {
        private readonly IReturnTypeFactory<TReturn> _factory;

        public ResultTFactory()
        {
            _factory = IReturnTypeFactory<TReturn>.Create();
        }

        public Result<TReturn> Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                return new Result<TReturn>(trap);
            }
            else
            {
                return new Result<TReturn>(_factory.Create(store, trap, values));
            }
        }
    }

    internal class ResultWithBacktraceFactory
        : IReturnTypeFactory<ResultWithBacktrace>
    {
        public ResultWithBacktrace Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                return new ResultWithBacktrace(trap);
            }
            else
            {
                return new ResultWithBacktrace();
            }
        }
    }

    internal class ResultWithBacktraceTFactory<TReturn>
        : IReturnTypeFactory<ResultWithBacktrace<TReturn>>
    {
        private readonly IReturnTypeFactory<TReturn> _factory;

        public ResultWithBacktraceTFactory()
        {
            _factory = IReturnTypeFactory<TReturn>.Create();
        }

        public ResultWithBacktrace<TReturn> Create(IStore store, IntPtr trap, Span<Value> values)
        {
            if (trap != IntPtr.Zero)
            {
                return new ResultWithBacktrace<TReturn>(trap);
            }
            else
            {
                return new ResultWithBacktrace<TReturn>(_factory.Create(store, trap, values));
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
