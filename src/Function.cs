using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
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
        internal unsafe delegate void InvokeCallbackDelegate(Caller caller, Value* args, int nargs, Value* results, int nresults);

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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, false);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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

            return new Function(store, callback, true);
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
        /// <returns>Returns true if the type signature of the function is valid or false if not.</returns>
        public bool CheckTypeSignature(Type? returnType = null, params Type[] parameters)
        {
            // Check if the return type is a recognised result type (i.e. implements IActionResult or IFunctionResult)
            if (returnType != null && returnType.IsResultType())
            {
                // Try to get the type the result wraps (may be null if it's one of the non-generic result types)
                var wrappedReturnType = returnType.GetResultInnerType();

                // Check that the result does not attempt to wrap another result (e.g. Result<Result<int>>)
                if (wrappedReturnType != null && wrappedReturnType.IsResultType())
                {
                    return false;
                }

                // Type check with the wrapped value instead of the result
                return CheckTypeSignature(wrappedReturnType, parameters);
            }

            // Check if the func returns no values if that's expected
            if (Results.Count == 0 && returnType != null)
            {
                return false;
            }

            // Check if the func does return a value if that's expected
            if (Results.Count != 0 && returnType == null)
            {
                return false;
            }

            // Validate the return type(s)
            if (returnType != null)
            {
                // Multiple return types are represented by a tuple.
                if (typeof(ITuple).IsAssignableFrom(returnType))
                {
                    // Get the types from the tuple
                    var returnTypes = returnType.GetGenericArguments();

                    // Tuples with more than seven items are not. This is because under the hood only tuples
                    // up to 8 items are supported, longer tuples are faked by having a tuple with seven items
                    // and then the last field is a tuple of the remaining items. To avoid having to deal with this,
                    // simply don't support tuple that long.
                    if (returnTypes.Length >= 8)
                    {
                        return false;
                    }

                    // If the list lengths are different that's an instant fail
                    if (returnTypes.Length != Results.Count)
                    {
                        return false;
                    }

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
                    {
                        return false;
                    }

                    // If the return type is not compatible then this is not valid.
                    if (!Results[0].IsAssignableFrom(returnType))
                    {
                        return false;
                    }
                }
            }

            // Check if the parameter lists are the same length
            if (parameters.Length != Parameters.Count)
            {
                return false;
            }

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
        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action? WrapAction()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature())
            {
                return null;
            }

            return () =>
            {
                Span<Value> args = stackalloc Value[0];
                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA>? WrapAction<TA>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();

            return (a) =>
            {
                Span<Value> args = stackalloc Value[1];
                args[0] = Value.FromValueBox(ca.Box(a));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB>? WrapAction<TA, TB>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();

            return (a, b) =>
            {
                Span<Value> args = stackalloc Value[2];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC>? WrapAction<TA, TB, TC>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC)))
            {
                return null;
            }

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

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD>? WrapAction<TA, TB, TC, TD>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();

            return (a, b, c, d) =>
            {
                Span<Value> args = stackalloc Value[4];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE>? WrapAction<TA, TB, TC, TD, TE>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();

            return (a, b, c, d, e) =>
            {
                Span<Value> args = stackalloc Value[5];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF>? WrapAction<TA, TB, TC, TD, TE, TF>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();

            return (a, b, c, d, e, f) =>
            {
                Span<Value> args = stackalloc Value[6];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF, TG>? WrapAction<TA, TB, TC, TD, TE, TF, TG>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF), typeof(TG)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();

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

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF, TG, TH>? WrapAction<TA, TB, TC, TD, TE, TF, TG, TH>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF), typeof(TG), typeof(TH)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();

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

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF, TG, TH, TI>? WrapAction<TA, TB, TC, TD, TE, TF, TG, TH, TI>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF), typeof(TG), typeof(TH),
                                          typeof(TI)))
            {
                return null;
            }

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

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TR>? WrapFunc<TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR)))
            {
                return null;
            }

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return () =>
            {
                return InvokeWithReturn(stackalloc Value[0], factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TR>? WrapFunc<TA, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            if (!CheckTypeSignature(typeof(TR), typeof(TA)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a) =>
            {
                Span<Value> args = stackalloc Value[1];
                args[0] = Value.FromValueBox(ca.Box(a));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">First parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TR>? WrapFunc<TA, TB, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TR>? WrapFunc<TA, TB, TC, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TR>? WrapFunc<TA, TB, TC, TD, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TR>? WrapFunc<TA, TB, TC, TD, TE, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH), typeof(TI)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <typeparam name="TN">Fourteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM), typeof(TN)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <typeparam name="TN">Fourteenth parameter</typeparam>
        /// <typeparam name="TO">Fifteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM), typeof(TN), typeof(TO)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <typeparam name="TN">Fourteenth parameter</typeparam>
        /// <typeparam name="TO">Fifteenth parameter</typeparam>
        /// <typeparam name="TP">Sixteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM), typeof(TN), typeof(TO), typeof(TP)))
            {
                return null;
            }

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

                return InvokeWithReturn(args, factory);
            };
        }
        #endregion

        /// <summary>
        /// Invokes the wasmtime function and processes the results through a return type factory.
        /// Assumes arguments are the correct type. Disposes the arguments.
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="arguments">Span of arguments, will be diposed after use.</param>
        /// <param name="factory">Factory to use to construct the return item</param>
        /// <returns>The return value from the function</returns>
        private unsafe TR InvokeWithReturn<TR>(ReadOnlySpan<Value> arguments, IReturnTypeFactory<TR> factory)
        {
            Span<Value> output = stackalloc Value[Results.Count];

            try
            {
                var trap = Invoke(arguments, output);

                // Note: null suppression is safe because `Invoke` checks that `store` is not null
                return factory.Create(store!, trap, output);
            }
            finally
            {
                for (int i = 0; i < output.Length; i++)
                {
                    output[i].Dispose();
                }

                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    argument.Dispose();
                }
            }
        }

        /// <summary>
        /// Invokes the wasmtime function
        /// Assumes arguments are the correct type. Disposes the arguments.
        /// </summary>
        /// <param name="arguments">Span of arguments, will be diposed after use.</param>
        /// <returns></returns>
        private unsafe void InvokeWithoutReturn(ReadOnlySpan<Value> arguments)
        {
            try
            {
                var trap = Invoke(arguments, stackalloc Value[0]);
                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }
            }
            finally
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    argument.Dispose();
                }
            }
        }

        /// <summary>
        /// Invokes the Wasmtime function with no arguments.
        /// </summary>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        public object? Invoke()
        {
            return Invoke(new ReadOnlySpan<ValueBox>());
        }

        /// <summary>
        /// Invokes the Wasmtime function.
        /// </summary>
        /// <param name="arguments">The array of arguments to pass to the function.</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        // TODO: remove overload when https://github.com/dotnet/csharplang/issues/1757 is resolved
        public object? Invoke(params ValueBox[] arguments)
        {
            return Invoke((ReadOnlySpan<ValueBox>)arguments);
        }

        /// <summary>
        /// Invokes the Wasmtime function.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the function, wrapped in `ValueBox`</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        public object? Invoke(ReadOnlySpan<ValueBox> arguments)
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

            // Convert arguments (ValueBox) into a form wasm can consume (Value)
            Span<Value> args = stackalloc Value[Parameters.Count];
            for (var i = 0; i < arguments.Length; ++i)
            {
                args[i] = arguments[i].ToValue(Parameters[i]);
            }

            // Make some space to store the return results
            Span<Value> resultsSpan = stackalloc Value[Results.Count];

            try
            {
                var trap = Invoke(args, resultsSpan);
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
                        return resultsSpan[0].ToObject(store);
                    }

                    var ret = new object?[Results.Count];
                    for (int i = 0; i < Results.Count; ++i)
                    {
                        ret[i] = resultsSpan[i].ToObject(store);
                    }

                    return ret;
                }
                finally
                {
                    for (int i = 0; i < Results.Count; ++i)
                    {
                        resultsSpan[i].Dispose();
                    }
                }
            }
            finally
            {
                for (int i = 0; i < arguments.Length; ++i)
                {
                    args[i].Dispose();
                }
            }

        }

        /// <summary>
        /// Invokes the Wasmtime function. Assumes arguments are the correct type and return span is the correct size.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the function, wrapped as `Value`</param>
        /// <param name="resultsOut">Output span to store the results in, must be the correct length</param>
        /// <returns>
        ///   Returns the trap ptr or zero
        /// </returns>
        private unsafe IntPtr Invoke(ReadOnlySpan<Value> arguments, Span<Value> resultsOut)
        {
            if (IsNull)
            {
                throw new InvalidOperationException("Cannot invoke a null function reference.");
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;

            IntPtr error;
            IntPtr trap;
            fixed (Value* argsPtr = arguments)
            fixed (Value* resultsPtr = resultsOut)
            {
                error = Native.wasmtime_func_call(context.handle, func, argsPtr, (UIntPtr)Parameters.Count, resultsPtr, (UIntPtr)Results.Count, out trap);
            }

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return trap;
        }

        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Func,
                of = new ExternUnion { func = this.func }
            };
        }

        internal Function(IStore store, Delegate callback, bool hasReturn)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;

            using var funcType = GetFunctionType(callback.GetType(), hasReturn, this.parameters, this.results, out var hasCaller, out var returnsTuple);

            // Generate code for invoking the callback without reflection.
            var generatedDelegate = GenerateInvokeCallbackDelegate(callback, hasCaller, returnsTuple);

            unsafe
            {
                Native.WasmtimeFuncCallback? func = (env, callerPtr, args, nargs, results, nresults) =>
                {
                    using var caller = new Caller(callerPtr);
                    return InvokeCallback(generatedDelegate, caller, args, (int)nargs, results, (int)nresults);
                };

                Native.wasmtime_func_new(
                    store.Context.handle,
                    funcType,
                    func,
                    GCHandle.ToIntPtr(GCHandle.Alloc(func)),
                    &Finalize,
                    out this.func
                );
            }
        }

        internal Function()
        {
            this.store = null;
            this.func.store = 0;
            this.func.index = (UIntPtr)0;
        }

        internal Function(IStore store, ExternFunc func)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;

            this.func = func;

            if (!this.IsNull)
            {
                using var type = new TypeHandle(Native.wasmtime_func_type(store.Context.handle, this.func));

                unsafe
                {
                    parameters = (*Native.wasm_functype_params(type.DangerousGetHandle())).ToList();
                    results = (*Native.wasm_functype_results(type.DangerousGetHandle())).ToList();
                }
            }
        }

        private static IEnumerable<Type> EnumerateReturnTypes(Type? returnType, out bool isTuple)
        {
            isTuple = false;

            if (returnType is null)
            {
                return Array.Empty<Type>();
            }

            if (IsTuple(returnType))
            {
                isTuple = true;
                return EnumerateTupleTypes(returnType);

                static IEnumerable<Type> EnumerateTupleTypes(Type tupleType)
                {
                    foreach (var (typeArgument, idx) in tupleType.GenericTypeArguments.Select((e, idx) => (e, idx)))
                    {
                        if (idx is 7 && IsTuple(typeArgument))
                        {
                            // Recursively enumerate the nested tuple's type arguments.
                            foreach (var type in EnumerateTupleTypes(typeArgument))
                            {
                                yield return type;
                            }
                        }
                        else
                        {
                            yield return typeArgument;
                        }
                    }
                }
            }
            else
            {
                return new Type[] { returnType };
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

        internal static TypeHandle GetFunctionType(Type type, bool hasReturn, List<ValueKind> parameters, List<ValueKind> results, out bool hasCaller, out bool returnsTuple)
        {
            Span<Type> parameterTypes;
            Type? returnType;

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

            results.AddRange(EnumerateReturnTypes(returnType, out returnsTuple).Select(t =>
            {
                if (!Value.TryGetKind(t, out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with a return type of type '{t.ToString()}'.");
                }
                return kind;
            }));

            return new Function.TypeHandle(Function.Native.wasm_functype_new(new ValueTypeArray(parameters), new ValueTypeArray(results)));
        }

        internal static unsafe InvokeCallbackDelegate GenerateInvokeCallbackDelegate(Delegate callback, bool passCaller, bool returnsTuple)
        {
            // Generate IL code using DynamicMethod to call the delegate without reflection.
            // This will generate a Lightweight Function that can be collected by the GC
            // once it is no longer referenced.
            //
            // For example, when using a
            // Func<Caller, int, long, object, ValueTuple<int, float, double, long, object, Function, int, ValueTuple<int>>>,
            // the generated code will be equivalent to the following:
            // 
            // static unsafe void InvokeCallback(Delegate callback, Caller caller, Value* args, int nargs, Value* results, int nresults)
            // {
            //     var dele = (Func<Caller, int, long, object, ValueTuple<int, float, double, long, object, Function, int, ValueTuple<int>>>)callback;
            // 
            //     ValueTuple<int, float, double, long, object, Function, int, ValueTuple<int>> result = dele(
            //         caller,
            //         Int32ValueBoxConverter.Instance.Unbox(caller, args[0].ToValueBox()),
            //         Int64ValueBoxConverter.Instance.Unbox(caller, args[1].ToValueBox()),
            //         GenericValueBoxConverter<object>.Instance.Unbox(caller, args[2].ToValueBox()));
            // 
            //     results[0] = Value.FromValueBox(Int32ValueBoxConverter.Instance.Box(result.Item1));
            //     results[1] = Value.FromValueBox(Float32ValueBoxConverter.Instance.Box(result.Item2));
            //     results[2] = Value.FromValueBox(Float64ValueBoxConverter.Instance.Box(result.Item3));
            //     results[3] = Value.FromValueBox(Int64ValueBoxConverter.Instance.Box(result.Item4));
            //     results[4] = Value.FromValueBox(GenericValueBoxConverter<object>.Box(result.Item5));
            //     results[5] = Value.FromValueBox(FuncRefValueBoxConverter.Instance.Box(result.Item6));
            //     results[6] = Value.FromValueBox(Int32ValueBoxConverter.Instance.Box(result.Item7));
            //     results[7] = Value.FromValueBox(Int32ValueBoxConverter.Instance.Box(result.Rest.Item1));
            // }

            var callbackInvokeMethod = callback.GetType().GetMethod(nameof(Action.Invoke))!;

            var dynamicMethod = new DynamicMethod(
               name: "InvokeFunctionCallback",
               returnType: typeof(void),
               parameterTypes: InvokeCallbackDelegateParameterTypes,
               owner: typeof(Function),
               skipVisibility: true);

            var generator = dynamicMethod.GetILGenerator();

            // TOOD: Mabye generate a check/assert that the passed nargs and nresults matches our generated code.
            // This should always be the case since Wasmtime should pass the number of arguments we used to define
            // the callback.

            // Load the delegate and cast it.
            generator.Emit(OpCodes.Ldarg_0);
            generator.Emit(OpCodes.Castclass, callback.GetType());

            // Load the argument values.
            var methodParameters = callbackInvokeMethod.GetParameters().AsSpan();

            if (passCaller)
            {
                generator.Emit(OpCodes.Ldarg_1);
                methodParameters = methodParameters[1..];
            }

            var valueToValueBoxMethod = typeof(Value).GetMethod(
                nameof(Value.ToValueBox), 
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!;

            // Get the generic ValueBox.Converter method which we will use to get the actual converter type.
            var valueBoxConverterObjectFunc = ValueBox.Converter<object>;
            var valueBoxConverterGenericMethod = valueBoxConverterObjectFunc.Method.GetGenericMethodDefinition();

            for (int i = 0; i < methodParameters.Length; i++)
            {
                var parameterType = methodParameters[i].ParameterType;
                
                // Load the ValueConverter Instance required for this parameter.
                var valueConverterType = valueBoxConverterGenericMethod.MakeGenericMethod(parameterType)
                    .Invoke(null, null)!.GetType();
                var valueConverterInstanceField = valueConverterType.GetField(nameof(Int32ValueBoxConverter.Instance))!;
                generator.Emit(OpCodes.Ldsfld, valueConverterInstanceField);

                // Load caller.
                generator.Emit(OpCodes.Ldarg_1);

                // Load args pointer.
                generator.Emit(OpCodes.Ldarg_2);

                if (i > 0)
                {
                    // Increment the pointer.
                    generator.Emit(OpCodes.Ldc_I4, i * sizeof(Value));
                    generator.Emit(OpCodes.Conv_I);
                    generator.Emit(OpCodes.Add);
                }

                // (args + i)->ToValueBox()
                generator.Emit(OpCodes.Call, valueToValueBoxMethod);

                // valueConverter.Unbox()
                var valueConverterUnboxMethod = valueConverterType
                    .GetMethod(nameof(Int32ValueBoxConverter.Unbox))!;

                generator.Emit(OpCodes.Call, valueConverterUnboxMethod);
            }

            // callback.Invoke(caller, ...)
            generator.Emit(OpCodes.Callvirt, callbackInvokeMethod);

            var valueFromValueBoxMethod = typeof(Value).GetMethod(nameof(Value.FromValueBox))!;

            if (returnsTuple)
            {
                // Multuple return values returned as ValueTuple.
                var valueTupleVariable = generator.DeclareLocal(callbackInvokeMethod.ReturnType);
                generator.Emit(OpCodes.Stloc, valueTupleVariable);

                var currentReturnTypes = callbackInvokeMethod.ReturnType.GetGenericArguments();
                for (int i = 0; ; i++)
                {
                    var returnType = currentReturnTypes[i % 7];

                    // Load results pointer.
                    generator.Emit(OpCodes.Ldarg_S, (byte)4);

                    if (i > 0)
                    {
                        // Increment the pointer.
                        generator.Emit(OpCodes.Ldc_I4, i * sizeof(Value));
                        generator.Emit(OpCodes.Conv_I);
                        generator.Emit(OpCodes.Add);
                    }

                    // Load the ValueConverter Instance required for this parameter.
                    var valueConverterType = valueBoxConverterGenericMethod.MakeGenericMethod(returnType)
                        .Invoke(null, null)!.GetType();
                    var valueConverterInstanceField = valueConverterType.GetField(nameof(Int32ValueBoxConverter.Instance))!;
                    generator.Emit(OpCodes.Ldsfld, valueConverterInstanceField);

                    // Load the valueTupleVariable.
                    generator.Emit(OpCodes.Ldloc, valueTupleVariable);

                    // Load the ValueTuple<...>.ItemX field. For a ValueTuple with a Rest, we
                    // also need to dereference these nested ValueTuples.
                    int tupleNestingLevels = i / 7;
                    var currentTupleType = callbackInvokeMethod.ReturnType;

                    for (int tupleLevel = 0; tupleLevel < tupleNestingLevels; tupleLevel++)
                    {
                        var restField = currentTupleType.GetField("Rest")!;
                        generator.Emit(OpCodes.Ldfld, restField);
                        currentTupleType = restField.FieldType;
                    }

                    string tupleFieldName = "Item" + ((i % 7) + 1).ToString(CultureInfo.InvariantCulture);
                    var tupleField = currentTupleType.GetField(tupleFieldName)!;
                    generator.Emit(OpCodes.Ldfld, tupleField);

                    // value = Value.FromValueBox(valueConverter.Box(x))
                    var valueConverterBoxMethod = valueConverterType
                        .GetMethod(nameof(Int32ValueBoxConverter.Box))!;

                    generator.Emit(OpCodes.Call, valueConverterBoxMethod);
                    generator.Emit(OpCodes.Call, valueFromValueBoxMethod);

                    // *(results + i) = value
                    generator.Emit(OpCodes.Stobj, typeof(Value));

                    // Handle the next ValueTuple level.
                    if ((i % 7) + 1 >= currentReturnTypes.Length)
                        break;
                    else if (i % 7 is 6)
                        currentReturnTypes = currentReturnTypes[7].GetGenericArguments();
                }
            }
            else if (callbackInvokeMethod.ReturnType != typeof(void))
            {
                // Single return value.
                var returnType = callbackInvokeMethod.ReturnType;

                var returnValueVariable = generator.DeclareLocal(callbackInvokeMethod.ReturnType);
                generator.Emit(OpCodes.Stloc, returnValueVariable);

                // Load return value pointer.
                generator.Emit(OpCodes.Ldarg_S, (byte)4);

                // Load the ValueConverter Instance required for this parameter.
                var valueConverterType = valueBoxConverterGenericMethod.MakeGenericMethod(returnType)
                    .Invoke(null, null)!.GetType();
                var valueConverterInstanceField = valueConverterType.GetField(nameof(Int32ValueBoxConverter.Instance))!;
                generator.Emit(OpCodes.Ldsfld, valueConverterInstanceField);
                
                // Load the valueTupleVariable.
                generator.Emit(OpCodes.Ldloc, returnValueVariable);

                // value = Value.FromValueBox(valueConverter.Box(x))
                var valueConverterBoxMethod = valueConverterType
                    .GetMethod(nameof(Int32ValueBoxConverter.Box))!;

                generator.Emit(OpCodes.Call, valueConverterBoxMethod);
                generator.Emit(OpCodes.Call, valueFromValueBoxMethod);

                // *results = value
                generator.Emit(OpCodes.Stobj, typeof(Value));
            }

            generator.Emit(OpCodes.Ret);

            return dynamicMethod.CreateDelegate<InvokeCallbackDelegate>(callback);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe static IntPtr InvokeCallback(InvokeCallbackDelegate invokeCallbackDelegate, Caller caller, Value* args, int nargs, Value* results, int nresults)
        {
            try
            {
                invokeCallbackDelegate(caller, args, nargs, results, nresults);
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

        [UnmanagedCallersOnly]
        internal static void Finalize(IntPtr p)
        {
            GCHandle.FromIntPtr(p).Free();
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
            public unsafe delegate IntPtr WasmtimeFuncCallback(IntPtr env, IntPtr caller, Value* args, UIntPtr nargs, Value* results, UIntPtr nresults);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe void wasmtime_func_new(IntPtr context, TypeHandle type, WasmtimeFuncCallback callback, IntPtr env, delegate* unmanaged<IntPtr, void> finalizer, out ExternFunc func);

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

        private readonly IStore? store;
        internal readonly ExternFunc func;
        internal readonly List<ValueKind> parameters = new List<ValueKind>();
        internal readonly List<ValueKind> results = new List<ValueKind>();

        private static readonly Function _null = new Function();
        private static readonly object?[] NullParams = new object?[1];

        private static readonly Type[] InvokeCallbackDelegateParameterTypes =
        {
            typeof(Delegate),
            typeof(Caller),
            typeof(Value).MakePointerType(),
            typeof(int),
            typeof(Value).MakePointerType(),
            typeof(int)
        };
    }
}
