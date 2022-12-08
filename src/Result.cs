using System;

namespace Wasmtime
{
    /// <summary>
    /// Indicates what type of result this is
    /// </summary>
    public enum ResultType
    {
        /// <summary>
        /// Excecution succeeded
        /// </summary>
        Ok = 0,

        /// <summary>
        /// Result contains a trap
        /// </summary>
        Trap = 1,
    }


    /// <summary>
    /// A result from a function call which does not produce a value, represents either a successful call or a trap
    /// </summary>
    public readonly struct ActionResult
        : IActionResult<ActionResult, ActionResultBuilder>
    {
        /// <summary>
        /// Indicates what type of result this contains
        /// </summary>
        public ResultType Type { get; }

        private readonly TrapException? _trap;

        internal ActionResult(TrapException trap)
        {
            Type = ResultType.Trap;
            _trap = trap;
        }

        /// <summary>
        /// Get the trap associated with this result
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this Type != Types.Trap</exception>
        public TrapException Trap
        {
            get
            {
                if (Type != ResultType.Trap)
                {
                    throw new InvalidOperationException($"Cannot get 'Trap' from '{Type}' type result");
                }

                return _trap!;
            }
        }
    }

    internal readonly struct ActionResultBuilder
        : IActionResultBuilder<ActionResult>
    {
        public ActionResult Create()
        {
            return new ActionResult();
        }

        public ActionResult Create(TrapAccessor accessor)
        {
            return new ActionResult(accessor.GetException());
        }
    }

    /// <summary>
    /// Indicates that the type implementing this interface is a "Result" type for actions (i.e. functions without a return value).
    /// This means that the function WASM call will use the TBuilder type to create an instance of the TResult type and return that.
    /// This can be used by advanced users to extract the necessary information from a trap result.
    /// </summary>
    /// <typeparam name="TResult">Type of this result type.</typeparam>
    /// <typeparam name="TBuilder">Type of the builder used to construct TResult</typeparam>
    public interface IActionResult<TResult, TBuilder>
        where TResult : struct, IActionResult<TResult, TBuilder>
        where TBuilder : struct, IActionResultBuilder<TResult>
    {
    }

    /// <summary>
    /// A factory which constructs instances of result types which do not contain a return value.
    /// </summary>
    /// <typeparam name="TResult">Type being constructed</typeparam>
    public interface IActionResultBuilder<out TResult>
        where TResult : struct
    {
        /// <summary>
        /// Create an new instance indicating a successful call.
        /// </summary>
        /// <returns>A new TResult instance representing a successful function call</returns>
        TResult Create();

        /// <summary>
        /// Create a new instance indicating a trap result.
        /// </summary>
        /// <param name="accessor"></param>
        /// <returns>A new TResult instance representing a trap result</returns>
        TResult Create(TrapAccessor accessor);
    }


    /// <summary>
    /// A result from a function call which may represent a Value or a Trap
    /// </summary>
    /// <typeparam name="T">Type of the return value contained in this result</typeparam>
    public readonly struct FunctionResult<T>
        : IFunctionResult<FunctionResult<T>, T, FunctionResultBuilder<T>>
    {
        /// <summary>
        /// Indicates what type of result this contains
        /// </summary>
        public ResultType Type { get; }

        private readonly T? _value;
        private readonly TrapException? _trap;

        internal FunctionResult(T? value)
        {
            Type = ResultType.Ok;
            _value = value;
            _trap = null;
        }

        internal FunctionResult(TrapException trap)
        {
            Type = ResultType.Trap;
            _value = default;
            _trap = trap;
        }

        /// <summary>
        /// Convert this result into a value, throw if it is a Trap
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TrapException">Thrown if Type == Trap</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if Type property contains an unknown value</exception>
        public static explicit operator T?(FunctionResult<T> value)
        {
            switch (value.Type)
            {
                case ResultType.Ok:
                    return value._value;

                case ResultType.Trap:
                    throw value._trap!;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value), $"Unknown Result Type property value '{value.Type}'");
            }
        }

        /// <summary>
        /// Get the value associated with this result
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this Type != Types.Value</exception>
        public T? Value
        {
            get
            {
                if (Type != ResultType.Ok)
                {
                    throw new InvalidOperationException($"Cannot get 'Value' from '{Type}' type result");
                }

                return _value;
            }
        }

        /// <summary>
        /// Get the trap associated with this result
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this Type != Types.Trap</exception>
        public TrapException Trap
        {
            get
            {
                if (Type != ResultType.Trap)
                {
                    throw new InvalidOperationException($"Cannot get 'Trap' from '{Type}' type result");
                }

                return _trap!;
            }
        }
    }

    internal readonly struct FunctionResultBuilder<TOk>
        : IFunctionResultBuilder<FunctionResult<TOk>, TOk>
    {
        public FunctionResult<TOk> Create(TOk? value)
        {
            return new FunctionResult<TOk>(value);
        }

        public FunctionResult<TOk> Create(TrapAccessor accessor)
        {
            return new FunctionResult<TOk>(accessor.GetException());
        }
    }

    /// <summary>
    /// Indicates that the type implementing this interface is a "Result" type for function.
    /// This means that the function WASM call will use the TBuilder type to create an instance of the TSelf type and return that.
    /// This can be used by advanced users to extract the necessary information from a trap result.
    /// </summary>
    /// <typeparam name="TResult">Type of this result type.</typeparam>
    /// <typeparam name="TBuilder">Type of the builder used to construct TResult</typeparam>
    /// <typeparam name="TOk">Type contained in the result in the success case</typeparam>
    public interface IFunctionResult<TResult, TOk, TBuilder>
        where TResult : struct, IFunctionResult<TResult, TOk, TBuilder>
        where TBuilder : struct, IFunctionResultBuilder<TResult, TOk>
    {
    }

    /// <summary>
    /// A factory which constructs instances of result types which contain a return value.
    /// </summary>
    /// <typeparam name="TResult">Type being constructed</typeparam>
    /// <typeparam name="TOk">Type contained in the result in the success case</typeparam>
    public interface IFunctionResultBuilder<out TResult, in TOk>
        where TResult : struct
    {
        /// <summary>
        /// Create an new instance indicating a successful call.
        /// </summary>
        /// <param name="value">The return value of the function call</param>
        /// <returns>A new TResult instance representing a successful function call</returns>
        TResult Create(TOk? value);

        /// <summary>
        /// Create a new instance indicating a trap result.
        /// </summary>
        /// <param name="accessor">Provides access to query information about a trap</param>
        /// <returns>A new TResult instance representing a trap result</returns>
        TResult Create(TrapAccessor accessor);
    }


    internal static class TypeExtensions
    {
        public static Type? TryGetResultInterface(this Type type)
        {
            foreach (var @interface in type.GetInterfaces())
            {
                if (!@interface.IsGenericType)
                {
                    continue;
                }

                var gtd = @interface.GetGenericTypeDefinition();
                if (gtd == typeof(IActionResult<,>) || gtd == typeof(IFunctionResult<,,>))
                {
                    return @interface;
                }
            }

            return null;
        }

        public static bool IsResultType(this Type type)
        {
            return type.TryGetResultInterface() != null;
        }

        public static Type? GetResultInnerType(this Type type)
        {
            var result = type.TryGetResultInterface();
            if (result == null)
            {
                throw new ArgumentException("Type must implement IActionResult<,> or IFunctionResult<,,>", nameof(type));
            }

            if (result.GetGenericTypeDefinition() == typeof(IActionResult<,>))
            {
                return null;
            }

            if (result.GetGenericTypeDefinition() == typeof(IFunctionResult<,,>))
            {
                var types = result.GetGenericArguments();
                return types[1];
            }

            throw new ArgumentException("Type is not a recognised Result type");
        }
    }
}
