using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents a Wasmtime function.
    /// </summary>
    public class Function : IExternal
    {
        #region FromCallback
        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback(IStore store, Action callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T>(IStore store, Action<T> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2>(IStore store, Action<T1, T2> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3>(IStore store, Action<T1, T2, T3> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4>(IStore store, Action<T1, T2, T3, T4> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5>(IStore store, Action<T1, T2, T3, T4, T5> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6>(IStore store, Action<T1, T2, T3, T4, T5, T6> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(IStore store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<TResult>(IStore store, Func<TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T, TResult>(IStore store, Func<T, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, TResult>(IStore store, Func<T1, T2, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, TResult>(IStore store, Func<T1, T2, T3, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, TResult>(IStore store, Func<T1, T2, T3, T4, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, TResult>(IStore store, Func<T1, T2, T3, T4, T5, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(IStore store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Context, callback, true);
        }
        #endregion

        /// <summary>
        /// The parameters of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Parameters => parameters;

        /// <summary>
        /// The results of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Results => results;

        /// <summary>
        /// Determines if the underlying function reference is null.
        /// </summary>
        public bool IsNull => func.index == UIntPtr.Zero && func.store == 0;

        /// <summary>
        /// Represents a null function reference.
        /// </summary>
        public static Function Null => _null;

        /// <summary>
        /// Check if this function has the given type signature
        /// </summary>
        /// <param name="returnType">Return type (use a tuple for multiple return types)</param>
        /// <param name="parameters">The parameters of the function</param>
        /// <returns></returns>
        public bool CheckTypeSignature(Type? returnType = null, params Type[] parameters)
        {
            // Check if the func returns no values if that's expected
            if (Results.Count == 0 && returnType != null)
                return false;
            if (Results.Count != 0 && returnType == null)
                return false;

            // Validate the return type(s)
            if (returnType != null)
            {
                // Multiple return types are represented by a tuple.
                if (typeof(ITuple).IsAssignableFrom(returnType))
                {
                    // Get the types from the tuple
                    var returnTypes = returnType.GetGenericArguments();

                    // If the list lengths are different that's an instant fail
                    if (returnTypes.Length != Results.Count)
                        return false;

                    // Validate the types one by one
                    for (int i = 0; i < returnTypes.Length; i++)
                    {
                        if (!Results[i].IsAssignableFrom(returnTypes[i]))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // Return type is not a tuple, so if there are multiple results this is not valid.
                    if (Results.Count != 1)
                        return false;

                    // If the return type is not compatible then this is not valid.
                    if (!Results[0].IsAssignableFrom(returnType))
                        return false;
                }
            }

            // Check if the parameter lists are the same length
            if (parameters.Length != Parameters.Count)
                return false;

            // Validate the parameter types one by one
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!Parameters[i].IsAssignableFrom(parameters[i]))
                {
                    return false;
                }
            }

            // All ok!
            return true;
        }

        #region wrap
        internal Action<TA> WrapAction<TA>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();

            return (a) =>
            {
                Span<Value> args = stackalloc Value[1];
                args[0] = Value.FromValueBox(ca.Box(a));

                InvokeWithoutReturn(store, args);
            };
        }

        internal Action<TA, TB> WrapAction<TA, TB>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();

            return (a, b) =>
            {
                Span<Value> args = stackalloc Value[2];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));

                InvokeWithoutReturn(store, args);
            };
        }

        internal Action<TA, TB, TC> WrapAction<TA, TB, TC>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();

            return (a, b, c) =>
            {
                Span<Value> args = stackalloc Value[3];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));

                InvokeWithoutReturn(store, args);
            };
        }

        internal Func<TR> WrapFunc<TR>(IStore store)
        {
            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return () =>
            {
                return InvokeWithReturn(store, stackalloc Value[0], factory);
            };
        }

        internal Func<TA, TR> WrapFunc<TA, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a) =>
            {
                Span<Value> args = stackalloc Value[1];
                args[0] = Value.FromValueBox(ca.Box(a));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TR> WrapFunc<TA, TB, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b) =>
            {
                Span<Value> args = stackalloc Value[2];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TR> WrapFunc<TA, TB, TC, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c) =>
            {
                Span<Value> args = stackalloc Value[3];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TR> WrapFunc<TA, TB, TC, TD, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d) =>
            {
                Span<Value> args = stackalloc Value[4];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TR> WrapFunc<TA, TB, TC, TD, TE, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e) =>
            {
                Span<Value> args = stackalloc Value[5];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f) =>
            {
                Span<Value> args = stackalloc Value[6];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g) =>
            {
                Span<Value> args = stackalloc Value[7];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h) =>
            {
                Span<Value> args = stackalloc Value[8];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i) =>
            {
                Span<Value> args = stackalloc Value[9];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j) =>
            {
                Span<Value> args = stackalloc Value[10];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k) =>
            {
                Span<Value> args = stackalloc Value[11];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l) =>
            {
                Span<Value> args = stackalloc Value[12];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m) =>
            {
                Span<Value> args = stackalloc Value[13];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();
            var cn = ValueBox.Converter<TN>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m, n) =>
            {
                Span<Value> args = stackalloc Value[14];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));
                args[13] = Value.FromValueBox(cn.Box(n));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();
            var cn = ValueBox.Converter<TN>();
            var co = ValueBox.Converter<TO>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) =>
            {
                Span<Value> args = stackalloc Value[15];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));
                args[13] = Value.FromValueBox(cn.Box(n));
                args[14] = Value.FromValueBox(co.Box(o));

                return InvokeWithReturn(store, args, factory);
            };
        }

        internal Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR> WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>(IStore store)
        {
            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();
            var cn = ValueBox.Converter<TN>();
            var co = ValueBox.Converter<TO>();
            var cp = ValueBox.Converter<TP>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
            {
                Span<Value> args = stackalloc Value[16];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));
                args[13] = Value.FromValueBox(cn.Box(n));
                args[14] = Value.FromValueBox(co.Box(o));
                args[15] = Value.FromValueBox(cp.Box(p));

                return InvokeWithReturn(store, args, factory);
            };
        }
        #endregion

        /// <summary>
        /// Invokes the Wasmtime function with no arguments.
        /// </summary>
        /// <param name="store">The store that owns this function.</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        public object? Invoke(IStore store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return Invoke(store, new ReadOnlySpan<ValueBox>());
        }

        /// <summary>
        /// Invokes the Wasmtime function.
        /// </summary>
        /// <param name="store">The store that owns this function.</param>
        /// <param name="arguments">The array of arguments to pass to the function.</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        // TODO: remove overload when https://github.com/dotnet/csharplang/issues/1757 is resolved
        public object? Invoke(IStore store, params ValueBox[] arguments)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return Invoke(store, (ReadOnlySpan<ValueBox>)arguments);
        }

        /// <summary>
        /// Invokes the Wasmtime function.
        /// </summary>
        /// <param name="store">The store that owns this function.</param>
        /// <param name="arguments">The arguments to pass to the function, wrapped in `ValueBox`</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        public object? Invoke(IStore store, ReadOnlySpan<ValueBox> arguments)
        {
            if (IsNull)
            {
                throw new InvalidOperationException("Cannot invoke a null function reference.");
            }

            if (arguments.Length != Parameters.Count)
            {
                throw new WasmtimeException($"Argument mismatch when invoking function: requires {Parameters.Count} but was given {arguments.Length}.");
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            unsafe
            {
                Value* args = stackalloc Value[Parameters.Count];
                Value* results = stackalloc Value[Results.Count];

                for (int i = 0; i < arguments.Length; ++i)
                    args[i] = arguments[i].ToValue(Parameters[i]);

                var error = Native.wasmtime_func_call(context.handle, func, args, (UIntPtr)Parameters.Count, results, (UIntPtr)Results.Count, out var trap);
                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }

                for (int i = 0; i < arguments.Length; ++i)
                {
                    args[i].Dispose();
                }

                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }

                if (Results.Count == 0)
                {
                    return null;
                }

                try
                {
                    if (Results.Count == 1)
                    {
                        return results[0].ToObject(context);
                    }

                    var ret = new object?[Results.Count];
                    for (int i = 0; i < Results.Count; ++i)
                    {
                        ret[i] = results[i].ToObject(context);
                    }

                    return ret;
                }
                finally
                {
                    for (int i = 0; i < Results.Count; ++i)
                    {
                        results[i].Dispose();
                    }
                }
            }
        }

        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Func,
                of = new ExternUnion { func = this.func }
            };
        }

        internal Function(StoreContext context, Delegate callback, bool hasReturn)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            using var funcType = GetFunctionType(callback.GetType(), hasReturn, this.parameters, this.results, out var hasCaller);

            unsafe
            {
                Native.WasmtimeFuncCallback? func = null;
                if (hasCaller)
                {
                    func = (env, callerPtr, args, nargs, results, nresults) =>
                    {
                        using var caller = new Caller(callerPtr);
                        return InvokeCallback(callback, caller, true, args, (int)nargs, results, (int)nresults, Results);
                    };
                }
                else
                {
                    func = (env, callerPtr, args, nargs, results, nresults) =>
                    {
                        using var caller = new Caller(callerPtr);
                        return InvokeCallback(callback, caller, false, args, (int)nargs, results, (int)nresults, Results);
                    };
                }

                Native.wasmtime_func_new(
                    context.handle,
                    funcType,
                    func,
                    GCHandle.ToIntPtr(GCHandle.Alloc(func)),
                    Finalizer,
                    out this.func
                );
            }
        }

        internal Function()
        {
            this.func.store = 0;
            this.func.index = (UIntPtr)0;
        }

        internal Function(StoreContext context, ExternFunc func)
        {
            this.func = func;

            if (!this.IsNull)
            {
                using var type = new TypeHandle(Native.wasmtime_func_type(context.handle, this.func));

                unsafe
                {
                    parameters = (*Native.wasm_functype_params(type.DangerousGetHandle())).ToList();
                    results = (*Native.wasm_functype_results(type.DangerousGetHandle())).ToList();
                }
            }
        }

        private static IEnumerable<Type> EnumerateReturnTypes(Type? returnType)
        {
            if (returnType is null)
            {
                yield break;
            }

            if (IsTuple(returnType))
            {
                foreach (var type in returnType
                    .GetGenericArguments()
                    .SelectMany(type =>
                        {
                            if (type.IsConstructedGenericType)
                            {
                                return type.GenericTypeArguments;
                            }
                            return Enumerable.Repeat(type, 1);
                        }))
                {
                    yield return type;
                }
            }
            else
            {
                yield return returnType;
            }
        }

        private static bool IsTuple(Type type)
        {
            if (!type.IsConstructedGenericType)
            {
                return false;
            }

            var definition = type.GetGenericTypeDefinition();

            return definition == typeof(ValueTuple) ||
                   definition == typeof(ValueTuple<>) ||
                   definition == typeof(ValueTuple<,>) ||
                   definition == typeof(ValueTuple<,,>) ||
                   definition == typeof(ValueTuple<,,,>) ||
                   definition == typeof(ValueTuple<,,,,>) ||
                   definition == typeof(ValueTuple<,,,,,>) ||
                   definition == typeof(ValueTuple<,,,,,,>) ||
                   definition == typeof(ValueTuple<,,,,,,,>);
        }

        internal static TypeHandle GetFunctionType(Type type, bool hasReturn, List<ValueKind> parameters, List<ValueKind> results, out bool hasCaller)
        {
            Span<Type> parameterTypes = null;
            Type? returnType = null;

            if (hasReturn)
            {
                parameterTypes = type.GenericTypeArguments[0..^1];
                returnType = type.GenericTypeArguments[^1];
            }
            else
            {
                parameterTypes = type.GenericTypeArguments;
                returnType = null;
            }

            hasCaller = parameterTypes.Length > 0 && parameterTypes[0] == typeof(Caller);

            if (hasCaller)
            {
                parameterTypes = parameterTypes[1..];
            }

            for (int i = 0; i < parameterTypes.Length; ++i)
            {
                if (parameterTypes[i] == typeof(Caller))
                {
                    throw new WasmtimeException($"A 'Caller' parameter must be the first parameter of the function.");
                }

                if (!Value.TryGetKind(parameterTypes[i], out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with parameter of type '{parameterTypes[i].ToString()}'.");
                }

                parameters.Add(kind);
            }

            results.AddRange(EnumerateReturnTypes(returnType).Select(t =>
            {
                if (!Value.TryGetKind(t, out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with a return type of type '{t.ToString()}'.");
                }
                return kind;
            }));

            return new Function.TypeHandle(Function.Native.wasm_functype_new(new ValueTypeArray(parameters), new ValueTypeArray(results)));
        }

        internal unsafe static IntPtr InvokeCallback(Delegate callback, Caller caller, bool passCaller, Value* args, int nargs, Value* results, int nresults, IReadOnlyList<ValueKind> resultKinds)
        {
            try
            {
                var offset = passCaller ? 1 : 0;
                var invokeArgs = new object?[nargs + offset];

                if (passCaller)
                {
                    invokeArgs[0] = caller;
                }

                var invokeArgsSpan = new Span<object?>(invokeArgs, offset, nargs);
                var context = ((IStore)caller).Context;
                for (int i = 0; i < invokeArgsSpan.Length; ++i)
                {
                    invokeArgsSpan[i] = args[i].ToObject(context);
                }

                // NOTE: reflection is extremely slow for invoking methods. in the future, perhaps this could be replaced with
                // source generators, system.linq.expressions, or generate IL with DynamicMethods or something
                var result = callback.Method.Invoke(callback.Target, BindingFlags.DoNotWrapExceptions, null, invokeArgs, null);

                if (resultKinds.Count > 0)
                {
                    var tuple = result as ITuple;
                    if (tuple is null)
                    {
                        results[0] = Value.FromObject(result, resultKinds[0]);
                    }
                    else
                    {
                        for (int i = 0; i < tuple.Length; ++i)
                        {
                            results[i] = Value.FromObject(tuple[i], resultKinds[i]);
                        }
                    }
                }
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                var bytes = Encoding.UTF8.GetBytes(ex.Message);

                fixed (byte* ptr = bytes)
                {
                    return Native.wasmtime_trap_new(ptr, (UIntPtr)bytes.Length);
                }
            }
        }

        internal class TypeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public TypeHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                Native.wasm_functype_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            public delegate void Finalizer(IntPtr data);

            public unsafe delegate IntPtr WasmtimeFuncCallback(IntPtr env, IntPtr caller, Value* args, UIntPtr nargs, Value* results, UIntPtr nresults);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_func_new(IntPtr context, TypeHandle type, WasmtimeFuncCallback callback, IntPtr env, Finalizer? finalizer, out ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_func_call(IntPtr context, in ExternFunc func, Value* args, UIntPtr nargs, Value* results, UIntPtr nresults, out IntPtr trap);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_func_type(IntPtr context, in ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_functype_new(in ValueTypeArray parameters, in ValueTypeArray results);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ValueTypeArray* wasm_functype_params(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ValueTypeArray* wasm_functype_results(IntPtr type);


            [DllImport(Engine.LibraryName)]
            public static extern void wasm_functype_delete(IntPtr functype);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_trap_new(byte* bytes, UIntPtr len);
        }

        internal readonly ExternFunc func;
        internal readonly List<ValueKind> parameters = new List<ValueKind>();
        internal readonly List<ValueKind> results = new List<ValueKind>();
        internal static readonly Native.Finalizer Finalizer = (p) => GCHandle.FromIntPtr(p).Free();

        private static readonly Function _null = new Function();
        private static readonly object?[] NullParams = new object?[1];
    }
}
