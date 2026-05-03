using System;

namespace Wasmtime.Components
{
    /// <summary>
    /// Represents an empty payload for the <c>ok</c> or <c>err</c> arm of a
    /// <see cref="Result{T,E}"/>; mirrors WIT's <c>_</c> payload syntax.
    /// </summary>
    public readonly record struct Unit;

    /// <summary>
    /// Discriminated optional value used when WIT <c>option</c> can't be flattened to
    /// <c>T?</c> — specifically <c>option&lt;option&lt;T&gt;&gt;</c>, where C# would otherwise
    /// require the invalid <c>T??</c>.
    /// </summary>
    /// <remarks>
    /// Generated bindings emit <c>Option&lt;T&gt;</c> instead of <c>T?</c> for any option whose
    /// element is itself an option. <c>T</c> may be a nullable reference type, a
    /// <see cref="Nullable{T}"/>, or another <see cref="Option{T}"/> in deeper nesting.
    /// </remarks>
    public readonly struct Option<T>
    {
        private readonly bool hasValue;
        private readonly T value;

        private Option(bool hasValue, T value)
        {
            this.hasValue = hasValue;
            this.value = value;
        }

        /// <summary>Indicates whether the option carries a value.</summary>
        public bool HasValue => hasValue;

        /// <summary>The carried value; throws when <see cref="HasValue"/> is <see langword="false"/>.</summary>
        public T Value => hasValue ? value : throw new InvalidOperationException("Option has no value.");

        /// <summary>Constructs an option carrying <paramref name="value"/>.</summary>
        public static Option<T> Some(T value) => new(true, value);

        /// <summary>The empty option.</summary>
        public static Option<T> None => default;
    }

    /// <summary>
    /// Discriminated union representing the value of a WIT <c>result&lt;T, E&gt;</c>.
    /// </summary>
    public readonly struct Result<T, E>
    {
        private readonly bool isOk;
        private readonly T okValue;
        private readonly E errValue;

        private Result(bool isOk, T okValue, E errValue)
        {
            this.isOk = isOk;
            this.okValue = okValue;
            this.errValue = errValue;
        }

        /// <summary>Indicates whether the result represents a successful value.</summary>
        public bool IsOk => isOk;

        /// <summary>Reads the successful value; throws if the result is an error.</summary>
        public T Value => isOk ? okValue : throw new InvalidOperationException("Result is in the err state.");

        /// <summary>Reads the error value; throws if the result is successful.</summary>
        public E Error => !isOk ? errValue : throw new InvalidOperationException("Result is in the ok state.");

        /// <summary>Constructs a successful result.</summary>
        public static Result<T, E> Ok(T value) => new(true, value, default!);

        /// <summary>Constructs an error result.</summary>
        public static Result<T, E> Err(E error) => new(false, default!, error);

        /// <summary>Pattern-matches the two cases.</summary>
        public TR Match<TR>(Func<T, TR> ok, Func<E, TR> err)
        {
            if (ok is null)
            {
                throw new ArgumentNullException(nameof(ok));
            }
            if (err is null)
            {
                throw new ArgumentNullException(nameof(err));
            }
            return isOk ? ok(okValue) : err(errValue);
        }
    }
}
