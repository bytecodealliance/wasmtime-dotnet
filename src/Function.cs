using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents a Wasmtime function.
    /// </summary>
    public partial class Function : IExternal
    {
        /// <summary>
        /// Encapsulates an untyped callback that receives arguments and can set results via a span of <see cref="ValueBox"/>.
        /// </summary>
        /// <param name="caller">The caller.</param>
        /// <param name="arguments">The function arguments.</param>
        /// <param name="results">The function results. These must be set (using the correct type) before returning, except when the method throws (in which case they are ignored).</param>
        public delegate void UntypedCallbackDelegate(Caller caller, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> results);

        #region FromCallback
        /// <summary>
        /// Creates an function given an untyped callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        /// <param name="parameterKinds">The function parameter kinds.</param>
        /// <param name="resultKinds">The function result kinds.</param>
        public static Function FromCallback(Store store, UntypedCallbackDelegate callback, IReadOnlyList<ValueKind> parameterKinds, IReadOnlyList<ValueKind> resultKinds)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            unsafe
            {
                // Copy the lists to ensure they are not modified.
                parameterKinds = parameterKinds.ToArray();
                resultKinds = resultKinds.ToArray();
                Native.WasmtimeFuncCallback func = (env, callerPtr, args, nargs, results, nresults) =>
                {
                    return InvokeUntypedCallback(callback, callerPtr, args, (int)nargs, results, (int)nresults, resultKinds);
                };

                var funcType = CreateFunctionType(parameterKinds, resultKinds);
                ExternFunc externFunc;
                try
                {
                    Native.wasmtime_func_new(
                        store.Context.handle,
                        funcType,
                        func,
                        GCHandle.ToIntPtr(GCHandle.Alloc(func)),
                        Finalizer,
                        out externFunc
                    );
                }
                finally
                {
                    Native.wasm_functype_delete(funcType);
                }

                GC.KeepAlive(store);
                return new Function(store, externFunc, parameterKinds, resultKinds);
            }
        }
        #endregion

        /// <summary>
        /// The types of the parameters of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Parameters { get; private set; }

        /// <summary>
        /// The types of the results of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Results { get; private set; }

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
                if (returnType.IsTupleType())
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

        /// <summary>
        /// Invokes the wasmtime function and processes the results through a return type factory.
        /// Assumes arguments are the correct type, and the span is large enough to also hold the results.
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="argsAndResults">Span of arguments and results.</param>
        /// <param name="factory">Factory to use to construct the return item</param>
        /// <param name="storeContext">The <see cref="StoreContext"/> from the <see cref="store"/>.</param>
        /// <returns>The return value from the function</returns>
        private unsafe TR? InvokeWithReturn<TR>(Span<ValueRaw> argsAndResults, IReturnTypeFactory<TR> factory, StoreContext storeContext)
        {
            // The caller has to ensure that the span is large enough to hold both
            // the arguments and results.
            var trap = Invoke(argsAndResults, storeContext);

            // Note: null suppression is safe because `Invoke` checks that `store` is not null
            // (by checking that we are not a null function reference).
            var results = argsAndResults[..Results.Count];
            return factory.Create(storeContext, store!, trap, results);
        }

        /// <summary>
        /// Invokes the wasmtime function.
        /// Assumes arguments are the correct type.
        /// </summary>
        /// <param name="arguments">Span of arguments.</param>
        /// /// <param name="storeContext">The <see cref="StoreContext"/> from the <see cref="store"/>.</param>
        /// <returns></returns>
        private unsafe void InvokeWithoutReturn(Span<ValueRaw> arguments, StoreContext storeContext)
        {
            var trap = Invoke(arguments, storeContext);

            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
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
                error = Native.wasmtime_func_call(context.handle, func, argsPtr, (nuint)Parameters.Count, resultsPtr, (nuint)Results.Count, out trap);

                // We need to keep the `Store` alive here for two reasons:
                // 1) The `Store` references the `Store.Handle` instance which must be kept alive
                //    during native calls where a `wasmtime_context_t*` is passed, to prevent the GC
                //    from prematurely running its finalizer (that would delete the handle) if the
                //    `Store` is no longer reachable.
                //    This is done in every place where the `StoreContext` is retrieved from the
                //    `Store`.
                // 2) The `Store` itself must be kept alive while calling a wasm function, because
                //    we need to be able to retrieve the `Store` from a `Caller` using a weak
                //    `GCHandle`.
                GC.KeepAlive(store);
            }

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return trap;
        }

        /// <summary>
        /// Invokes the Wasmtime function. Assumes arguments are the correct type and the span has the correct size.
        /// </summary>
        /// <param name="argumentsAndResults">The span where the function arguments are read from, and the results are written to. The span must have the correct length.</param>
        /// <param name="storeContext">The <see cref="StoreContext"/> for the <see cref="store"/>.</param>
        /// <returns>
        ///   Returns the trap ptr or zero
        /// </returns>
        private unsafe IntPtr Invoke(Span<ValueRaw> argumentsAndResults, StoreContext storeContext)
        {
            if (IsNull)
            {
                throw new InvalidOperationException("Cannot invoke a null function reference.");
            }

            IntPtr error;
            IntPtr trap;
            fixed (ValueRaw* argsAndResultsPtr = argumentsAndResults)
            {
                error = Native.wasmtime_func_call_unchecked(storeContext.handle, func, argsAndResultsPtr, (nuint)argumentsAndResults.Length, out trap);

                // See comments above for the two reasons why the `Store` must be kept alive here.
                GC.KeepAlive(store);
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

        Store? IExternal.Store => store;

        internal Function()
        {
            this.store = null;
            this.func.store = 0;
            this.func.index = (UIntPtr)0;
            this.Parameters = this.Results = Array.Empty<ValueKind>();
        }

        internal Function(Store store, ExternFunc func)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;
            this.func = func;

            if (!this.IsNull)
            {
                var type = Native.wasmtime_func_type(store.Context.handle, this.func);

                try
                {
                    unsafe
                    {
                        this.Parameters = (*Native.wasm_functype_params(type)).ToArray();
                        this.Results = (*Native.wasm_functype_results(type)).ToArray();
                    }
                }
                finally
                {
                    Native.wasm_functype_delete(type);
                }

                GC.KeepAlive(store);
            }
            else
            {
                this.Parameters = this.Results = Array.Empty<ValueKind>();
            }
        }

        private Function(Store store, ExternFunc func, IReadOnlyList<ValueKind> parameters, IReadOnlyList<ValueKind> results)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            this.store = store;
            this.func = func;
            this.Parameters = parameters;
            this.Results = results;
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
                    var gta = tupleType.GenericTypeArguments;
                    for (var idx = 0; idx < gta.Length; idx++)
                    {
                        var typeArgument = gta[idx];

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

        internal static (IReadOnlyList<ValueKind> parameterKinds, IReadOnlyList<ValueKind> resultKinds)
            GetFunctionType(ReadOnlySpan<Type> parameterTypes, Type? returnType, bool allowCaller, bool allowTuple)
        {
            var hasCaller = parameterTypes.Length > 0 && parameterTypes[0] == typeof(Caller);

            if (hasCaller)
            {
                parameterTypes = parameterTypes[1..];
            }

            var parameterKinds = new ValueKind[parameterTypes.Length];

            for (int i = 0; i < parameterTypes.Length; ++i)
            {
                if (parameterTypes[i] == typeof(Caller))
                {
                    throw new WasmtimeException($"A 'Caller' parameter must be the first parameter of the callback.");
                }

                if (!Value.TryGetKind(parameterTypes[i], out var kind))
                {
                    throw new WasmtimeException($"Unable to create a callback with parameter of type '{parameterTypes[i]}'.");
                }

                parameterKinds[i] = kind;
            }

            var resultKinds = EnumerateReturnTypes(returnType, out var returnsTuple).Select(t =>
            {
                if (!Value.TryGetKind(t, out var kind))
                {
                    throw new WasmtimeException($"Unable to create a callback with a return type of type '{t}'.");
                }
                return kind;
            }).ToArray();

            if (hasCaller && !allowCaller)
            {
                throw new InvalidOperationException($"A 'Caller' parameter is not allowed for this callback.");
            }

            if (returnsTuple && !allowTuple)
            {
                throw new InvalidOperationException($"Returning a ValueTuple is not allowed for this callback; use an overload that implicitly returns ValueTuple or an untyped callback for returning more than 4 values.");
            }

            return (parameterKinds, resultKinds);
        }

        internal static IntPtr CreateFunctionType(IReadOnlyList<ValueKind> parameterKinds, IReadOnlyList<ValueKind> resultKinds)
        {
            return Native.wasm_functype_new(new ValueTypeArray(parameterKinds), new ValueTypeArray(resultKinds));
        }

        internal static unsafe IntPtr InvokeUntypedCallback(UntypedCallbackDelegate callback, IntPtr callerPtr, Value* args, int nargs, Value* results, int nresults, IReadOnlyList<ValueKind> resultKinds)
        {
            try
            {
                var caller = new Caller(callerPtr);

                // Rent ValueBox arrays from the array pool (as it's not possible to
                // stackalloc a managed type).
                var argumentsBuffer = ArrayPool<ValueBox>.Shared.Rent(nargs);
                var resultsBuffer = ArrayPool<ValueBox>.Shared.Rent(nresults);

                try
                {
                    var argumentsSpan = argumentsBuffer.AsSpan()[..nargs];
                    var resultsSpan = resultsBuffer.AsSpan()[..nresults];

                    // Initialize the results with ValueBoxes using the expected
                    // ValueKind but with a default value. Otherwise (when just using
                    // resultsSpan.Clear()), they would all be initialized with
                    // ValueKind.Int32.
                    for (int i = 0; i < resultsSpan.Length; i++)
                    {
                        resultsSpan[i] = resultKinds[i] is ValueKind.ExternRef ?
                            new ValueBox(null) :
                            new ValueBox(resultKinds[i], default);
                    }

                    for (int i = 0; i < argumentsSpan.Length; i++)
                    {
                        argumentsSpan[i] = args[i].ToValueBox();
                    }

                    callback(caller, argumentsSpan, resultsSpan);

                    for (int i = 0; i < resultsSpan.Length; i++)
                    {
                        results[i] = resultsSpan[i].ToValue(resultKinds[i]);
                    }
                }
                finally
                {
                    ArrayPool<ValueBox>.Shared.Return(argumentsBuffer);
                    ArrayPool<ValueBox>.Shared.Return(resultsBuffer);
                }

                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                return HandleCallbackException(ex);
            }
        }

        internal static unsafe IntPtr HandleCallbackException(Exception ex)
        {
            try
            {
                // Store the exception as error cause, so that we can use it as the WasmtimeException's
                // InnerException when the error bubbles up to the next host-to-wasm transition.
                // If the exception is already a WasmtimeException, we use that one's InnerException,
                // even if it's null.
                // Note: This code currently requires that on every host-to-wasm transition where a
                // error can occur, WasmtimeException.FromOwnedError() is called when an error actually
                // occured, which will then clear this field.
                CallbackErrorCause = ex is WasmtimeException wasmtimeException ? wasmtimeException.InnerException : ex;

                var bytes = Encoding.UTF8.GetBytes(ex.Message);

                fixed (byte* ptr = bytes)
                {
                    return Native.wasmtime_trap_new(ptr, (nuint)bytes.Length);
                }
            }
            catch (Exception separateException)
            {
                // We never must let .NET exceptions bubble through the native-to-managed transition,
                // as otherwise (at least on Windows) the .NET runtime would unwind the stack up to the
                // next .NET exception handler even when there are native frames in between, and that
                // would cause undefined behavior with Wasmtime. For example, this can happen if the
                // system is low on memory when allocating the UTF-8 byte array. See:
                // https://github.com/bytecodealliance/wasmtime-dotnet/issues/187
                Environment.FailFast(separateException.Message, separateException);

                // Satisfy the control-flow analyzer; this line is never reached.
                throw;
            }
        }

        internal static class Native
        {
            public delegate void Finalizer(IntPtr data);

            public unsafe delegate IntPtr WasmtimeFuncCallback(IntPtr env, IntPtr caller, Value* args, nuint nargs, Value* results, nuint nresults);

            public unsafe delegate IntPtr WasmtimeFuncUncheckedCallback(IntPtr env, IntPtr caller, ValueRaw* args_and_results, nuint num_args_and_results);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_func_new(IntPtr context, IntPtr type, WasmtimeFuncCallback callback, IntPtr env, Finalizer? finalizer, out ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_func_new_unchecked(IntPtr context, IntPtr type, WasmtimeFuncUncheckedCallback callback, IntPtr env, Finalizer? finalizer, out ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_func_call(IntPtr context, in ExternFunc func, Value* args, nuint nargs, Value* results, nuint nresults, out IntPtr trap);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_func_call_unchecked(IntPtr context, in ExternFunc func, ValueRaw* args_and_results, nuint args_and_results_len, out IntPtr trap);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_func_type(IntPtr context, in ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern void wasmtime_func_from_raw(IntPtr context, IntPtr raw, out ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_func_to_raw(IntPtr context, in ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_functype_new(in ValueTypeArray parameters, in ValueTypeArray results);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ValueTypeArray* wasm_functype_params(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ValueTypeArray* wasm_functype_results(IntPtr type);


            [DllImport(Engine.LibraryName)]
            public static extern void wasm_functype_delete(IntPtr functype);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_trap_new(byte* bytes, nuint len);
        }

        internal readonly Store? store;
        internal readonly ExternFunc func;
        internal static readonly Native.Finalizer Finalizer = (p) => GCHandle.FromIntPtr(p).Free();

        /// <summary>
        /// Contains the cause for a error returned by invoking a wasm function, in case
        /// the error was caused by the host. 
        /// </summary>
        /// <remarks>
        /// This thread-local field will be set when catching a .NET exception at the
        /// wasm-to-host transition. When the error bubbles up to the next host-to-wasm
        /// transition, the field needs to be cleared, and its value can be used to set
        /// the inner exception of the created <see cref="WasmtimeException"/>.
        /// </remarks>
        [ThreadStatic]
        internal static Exception? CallbackErrorCause;

        private static readonly Function _null = new Function();
    }
}
