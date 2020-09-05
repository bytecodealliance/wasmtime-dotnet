using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAsssembly function.
    /// </summary>
    public class Function : IDisposable
    {
        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback(Store store, Action callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T>(Store store, Action<T> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2>(Store store, Action<T1, T2> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3>(Store store, Action<T1, T2, T3> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4>(Store store, Action<T1, T2, T3, T4> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5>(Store store, Action<T1, T2, T3, T4, T5> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6>(Store store, Action<T1, T2, T3, T4, T5, T6> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7>(Store store, Action<T1, T2, T3, T4, T5, T6, T7> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(Store store, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, false);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<TResult>(Store store, Func<TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T, TResult>(Store store, Func<T, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, TResult>(Store store, Func<T1, T2, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, TResult>(Store store, Func<T1, T2, T3, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, TResult>(Store store, Func<T1, T2, T3, T4, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, TResult>(Store store, Func<T1, T2, T3, T4, T5, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(Store store, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            return new Function(store.Handle, callback, true);
        }

        /// <summary>
        /// The parameters of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Parameters { get; private set; }

        /// <summary>
        /// The results of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Results { get; private set; }

        /// <summary>
        /// Determines if the underlying function reference is null.
        /// </summary>
        public bool IsNull { get; private set; }

        /// <summary>
        /// Represents a null function reference.
        /// </summary>
        public static Function Null => _null;

        /// <summary>
        /// Invokes the WebAssembly function.
        /// </summary>
        /// <param name="arguments">The array of arguments to pass to the function.</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        public object? Invoke(params object[] arguments)
        {
            if (IsNull)
            {
                throw new InvalidOperationException("Cannot invoke a null function reference.");
            }

            CheckDisposed();
            return Invoke(Handle.DangerousGetHandle(), Parameters, Results, arguments);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!Handle.IsInvalid)
            {
                Handle.Dispose();
                Handle.SetHandleAsInvalid();
            }
        }

        internal static object? Invoke(IntPtr func, IReadOnlyList<ValueKind> funcParameters, IReadOnlyList<ValueKind> funcResults, object?[] arguments)
        {
            if (arguments.Length != funcParameters.Count)
            {
                throw new WasmtimeException($"Argument mismatch when invoking function: requires {funcParameters.Count} but was given {arguments.Length}.");
            }

            unsafe
            {
                Interop.wasm_val_t* args = stackalloc Interop.wasm_val_t[funcParameters.Count];
                Interop.wasm_val_t* results = stackalloc Interop.wasm_val_t[funcResults.Count];

                for (int i = 0; i < arguments.Length; ++i)
                {
                    args[i] = Interop.ToValue(arguments[i], funcParameters[i]);
                }

                var trap = Interop.wasm_func_call(func, args, results);

                for (int i = 0; i < arguments.Length; ++i)
                {
                    Interop.DeleteValue(&args[i]);
                }

                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }

                if (funcResults.Count == 0)
                {
                    return null;
                }

                if (funcResults.Count == 1)
                {
                    var result = Interop.ToObject(&results[0]);
                    Interop.DeleteValue(&results[0]);
                    return result;
                }

                var ret = new object?[funcResults.Count];
                for (int i = 0; i < funcResults.Count; ++i)
                {
                    ret[i] = Interop.ToObject(&results[i]);
                    Interop.DeleteValue(&results[i]);
                }
                return ret;
            }
        }

        internal Function(Interop.StoreHandle store, Delegate callback, bool hasReturn)
        {
            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var type = callback.GetType();
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

            bool hasCaller = parameterTypes.Length > 0 && parameterTypes[0] == typeof(Caller);

            if (hasCaller)
            {
                parameterTypes = parameterTypes[1..];
            }

            Parameters = GetParameterKinds(parameterTypes);
            Results = GetReturnTypeKinds(EnumerateReturnTypes(returnType));

            var parameters = CreateValueTypeVec(Parameters);
            var results = CreateValueTypeVec(Results);
            using var funcType = Interop.wasm_functype_new(ref parameters, ref results);

            if (hasCaller)
            {
                var func = CreateWasmtimeCallback(store, callback, parameterTypes.Length, Results);
                Handle = Interop.wasmtime_func_new_with_env(
                    store,
                    funcType,
                    func,
                    GCHandle.ToIntPtr(GCHandle.Alloc(func)),
                    Interop.GCHandleFinalizer
                );
            }
            else
            {
                var func = CreateCallback(store, callback, parameterTypes.Length, Results);
                Handle = Interop.wasm_func_new_with_env(
                    store,
                    funcType,
                    func,
                    GCHandle.ToIntPtr(GCHandle.Alloc(func)),
                    Interop.GCHandleFinalizer
                );
            }

            if (Handle.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime function.");
            }
        }

        internal Function(Interop.FunctionHandle handle)
        {
            Handle = handle;

            if (Handle.IsInvalid)
            {
                IsNull = true;
                Parameters = Array.Empty<ValueKind>();
                Results = Array.Empty<ValueKind>();
            }
            else
            {
                using var funcType = Interop.wasm_func_type(Handle);

                unsafe
                {
                    Parameters = Interop.ToValueKindList(Interop.wasm_functype_params(funcType.DangerousGetHandle()));
                    Results = Interop.ToValueKindList(Interop.wasm_functype_results(funcType.DangerousGetHandle()));
                }
            }
        }

        private static ValueKind[] GetParameterKinds(Span<Type> parameters)
        {
            var kinds = new ValueKind[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i] == typeof(Caller))
                {
                    throw new WasmtimeException($"A 'Caller' parameter must be the first parameter of the function.");
                }

                if (!Interop.TryGetValueKind(parameters[i], out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with parameter of type '{parameters[i].ToString()}'.");
                }

                kinds[i] = kind;
            }
            return kinds;
        }

        private static ValueKind[] GetReturnTypeKinds(IEnumerable<Type> returnTypes)
        {
            return returnTypes.Select(t =>
            {
                if (!Interop.TryGetValueKind(t, out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with a return type of type '{t.ToString()}'.");
                }
                return kind;
            }).ToArray();
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

        private static unsafe Interop.WasmFuncCallbackWithEnv CreateCallback(Interop.StoreHandle store, Delegate callback, int parameterCount, IReadOnlyList<ValueKind> resultKinds)
        {
            // NOTE: this capture is not thread-safe.
            var args = new object[parameterCount];

            return (env, arguments, results) =>
                InvokeCallback(store, callback, args, resultKinds, IntPtr.Zero, arguments, results);
        }

        private static unsafe Interop.WasmtimeFuncCallbackWithEnv CreateWasmtimeCallback(Interop.StoreHandle store, Delegate callback, int parameterCount, IReadOnlyList<ValueKind> resultKinds)
        {
            // NOTE: this capture is not thread-safe.
            var args = new object[parameterCount + 1];
            args[0] = new Caller();

            return (caller, env, arguments, results) =>
                InvokeCallback(store, callback, args, resultKinds, caller, arguments, results);
        }

        private static Interop.wasm_valtype_vec_t CreateValueTypeVec(IReadOnlyList<ValueKind> kinds)
        {
            Interop.wasm_valtype_vec_t vec;
            Interop.wasm_valtype_vec_new_uninitialized(out vec, (UIntPtr)kinds.Count);

            for (int i = 0; i < kinds.Count; ++i)
            {
                var valType = Interop.wasm_valtype_new((Interop.wasm_valkind_t)kinds[i]);
                unsafe
                {
                    vec.data[i] = valType.DangerousGetHandle();
                }
                valType.SetHandleAsInvalid();
            }

            return vec;
        }

        private unsafe static IntPtr InvokeCallback(
            Interop.StoreHandle store,
            Delegate callback,
            object?[] args,
            IReadOnlyList<ValueKind> resultKinds,
            IntPtr caller,
            Interop.wasm_val_t* arguments,
            Interop.wasm_val_t* results)
        {
            try
            {
                int offset = 0;
                if (caller != IntPtr.Zero)
                {
                    offset = 1;
                    ((Caller)args[0]!).Handle = caller;
                }

                for (int i = 0; i < args.Length - offset; ++i)
                {
                    args[i + offset] = Interop.ToObject(&arguments[i]);
                }

                var result = callback.Method.Invoke(callback.Target, BindingFlags.DoNotWrapExceptions, null, args, null);

                for (int i = 0; i < args.Length - offset; ++i)
                {
                    args[i + offset] = null;
                }

                if (caller != IntPtr.Zero)
                {
                    ((Caller)args[0]!).Handle = IntPtr.Zero;
                }

                if (resultKinds.Count > 0)
                {
                    var tuple = result as ITuple;
                    if (tuple is null)
                    {
                        results[0] = Interop.ToValue(result, resultKinds[0]);
                    }
                    else
                    {
                        for (int i = 0; i < tuple.Length; ++i)
                        {
                            results[i] = Interop.ToValue(tuple[i], resultKinds[i]);
                        }
                    }
                }
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                var bytes = Encoding.UTF8.GetBytes(ex.Message + "\0" /* exception messages need a null */);

                fixed (byte* ptr = bytes)
                {
                    Interop.wasm_byte_vec_t message = new Interop.wasm_byte_vec_t();
                    message.size = (UIntPtr)bytes.Length;
                    message.data = ptr;

                    return Interop.wasm_trap_new(store, ref message);
                }
            }
        }

        private void CheckDisposed()
        {
            if (Handle.IsInvalid)
            {
                throw new ObjectDisposedException(typeof(Function).FullName);
            }
        }

        internal Interop.FunctionHandle Handle { get; private set; }
        private static Function _null = new Function(new Interop.FunctionHandle());
    }
}
