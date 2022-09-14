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
        Trap = 2,
    }

    /// <summary>
    /// A result from a function call which may represent a Value or a Trap. If a trap happens the full backtrace is captured.
    /// </summary>
    public readonly struct ResultWithBacktrace
    {
        /// <summary>
        /// Indicates what type of result this contains
        /// </summary>
        public ResultType Type { get; }

        private readonly TrapException? _trap;

        internal ResultWithBacktrace(IntPtr trap)
        {
            Type = ResultType.Trap;
            _trap = TrapException.FromOwnedTrap(trap);
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
                    throw new InvalidOperationException($"Cannot get 'Trap' from '{Type}' type result");
                return _trap!;
            }
        }
    }

    /// <summary>
    /// A result from a function call which may represent a Value or a Trap
    /// </summary>
    public readonly struct Result
    {
        /// <summary>
        /// Indicates what type of result this contains
        /// </summary>
        public ResultType Type { get; }

        private readonly TrapCode _trap;

        internal Result(IntPtr trap)
        {
            Type = ResultType.Trap;
            _trap = TrapException.GetTrapCode(trap);
            TrapException.Native.wasm_trap_delete(trap);

        }

        /// <summary>
        /// Get the trap associated with this result
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this Type != Types.Trap</exception>
        public TrapCode Trap
        {
            get
            {
                if (Type != ResultType.Trap)
                    throw new InvalidOperationException($"Cannot get 'Trap' from '{Type}' type result");
                return _trap;
            }
        }
    }

    /// <summary>
    /// A result from a function call which may represent a Value or a Trap. If a trap happens the full backtrace is captured.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct ResultWithBacktrace<T>
    {
        /// <summary>
        /// Indicates what type of result this contains
        /// </summary>
        public ResultType Type { get; }

        private readonly T? _value;
        private readonly TrapException? _trap;

        internal ResultWithBacktrace(T value)
        {
            Type = ResultType.Ok;
            _value = value;
            _trap = null;
        }

        internal ResultWithBacktrace(IntPtr trap)
        {
            Type = ResultType.Trap;
            _value = default;
            _trap = TrapException.FromOwnedTrap(trap);
        }

        /// <summary>
        /// Convert this result into a value, throw if it is a Trap
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TrapException">Thrown if Type == Trap</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if Type property contains an unknown value</exception>
        public static explicit operator T?(ResultWithBacktrace<T> value)
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
                    throw new InvalidOperationException($"Cannot get 'Value' from '{Type}' type result");
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
                    throw new InvalidOperationException($"Cannot get 'Trap' from '{Type}' type result");
                return _trap!;
            }
        }
    }

    /// <summary>
    /// A result from a function call which may represent a Value or a Trap
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public readonly struct Result<T>
    {
        /// <summary>
        /// Indicates what type of result this contains
        /// </summary>
        public ResultType Type { get; }

        private readonly T? _value;
        private readonly TrapCode _trap;

        internal Result(T value)
        {
            Type = ResultType.Ok;

            _value = value;
            _trap = default;
        }

        internal Result(IntPtr trap)
        {
            Type = ResultType.Trap;

            _value = default;

            _trap = TrapException.GetTrapCode(trap);
            TrapException.Native.wasm_trap_delete(trap);

        }

        /// <summary>
        /// Convert this result into a value, throw if it is a Trap
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <exception cref="TrapException">Thrown if Type == Trap</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if Type property contains an unknoown value</exception>
        public static explicit operator T?(Result<T> value)
        {
            switch (value.Type)
            {
                case ResultType.Ok:
                    return value._value;

                case ResultType.Trap:
                    throw new TrapException($"{value._trap} trap", null, value._trap);

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
                    throw new InvalidOperationException($"Cannot get 'Value' from '{Type}' type result");
                return _value;
            }
        }

        /// <summary>
        /// Get the trap associated with this result
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if this Type != Types.Trap</exception>
        public TrapCode Trap
        {
            get
            {
                if (Type != ResultType.Trap)
                    throw new InvalidOperationException($"Cannot get 'Trap' from '{Type}' type result");
                return _trap;
            }
        }
    }

    internal static class TypeExtensions
    {
        internal static bool IsResult(this Type type)
        {
            if (type == typeof(Result) || type == typeof(ResultWithBacktrace))
            {
                return true;
            }

            if (!type.IsGenericType)
            {
                return false;
            }

            var gtd = type.GetGenericTypeDefinition();
            return typeof(Result<>) == gtd || typeof(ResultWithBacktrace<>) == gtd;
        }
    }
}
