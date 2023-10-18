using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Wasmtime
{
    public partial class AsyncFunction
        : IExternal
    {
        private readonly Store store;
        private readonly ExternFunc func;

        /// <summary>
        /// The types of the parameters of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Parameters { get; }

        /// <summary>
        /// The types of the results of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Results { get; }

        /// <summary>
        /// Determines if the underlying function reference is null.
        /// </summary>
        public bool IsNull => func.index == UIntPtr.Zero && func.store == 0;

        internal AsyncFunction(Store store, ExternFunc func)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;
            this.func = func;

            if (!IsNull)
            {
                var type = Function.Native.wasmtime_func_type(store.Context.handle, this.func);
                try
                {
                    unsafe
                    {
                        Parameters = (*Function.Native.wasm_functype_params(type)).ToArray();
                        Results = (*Function.Native.wasm_functype_results(type)).ToArray();
                    }
                }
                finally
                {
                    Function.Native.wasm_functype_delete(type);
                }

                GC.KeepAlive(store);
            }
            else
            {
                Parameters = Array.Empty<ValueKind>();
                Results = Array.Empty<ValueKind>();
            }
        }

        public bool CheckTypeSignature(Type? returnType = null, params Type[] parameters)
        {
            return Function.CheckTypeSignature(returnType, parameters, Results, Parameters);
        }

        private async Task InvokeWithoutReturn(Value[] argsAndResults)
        {
            var trap = await Invoke(argsAndResults);

            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }
        }

        private async Task<TResult?> InvokeWithReturn<TResult>(Value[] argsAndResults, IReturnTypeFactory<TResult> factory)
        {
            if (Results.Count == 0)
            {
                throw new InvalidOperationException($"Cannot call `InvokeWithReturn` on a method with 0 results");
            }

            var trap = await Invoke(argsAndResults.AsMemory());

            return factory.Create(store.Context, store, trap, argsAndResults.AsSpan(Parameters.Count));
        }

        private async Task<IntPtr> Invoke(Memory<Value> argsAndResults)
        {
            using var pin = argsAndResults.Pin();

            IntPtr trap;
            IntPtr error;
            nint futurePtr;
            unsafe
            {
                var argsPtr = (Value*)pin.Pointer;
                var resultsPtr = argsPtr + Parameters.Count;

                if (Parameters.Count == 0)
                {
                    argsPtr = (Value*)0;
                }

                if (Results.Count == 0)
                {
                    resultsPtr = (Value*)0;
                }

                futurePtr = Native.wasmtime_func_call_async(store.Context.handle, func, argsPtr, (nuint)Parameters.Count, resultsPtr, (nuint)Results.Count, out trap, out error);

                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }

                if (trap != IntPtr.Zero)
                {
                    return trap;
                }
            }

            try
            {
                while (!CallFuture.Native.wasmtime_call_future_poll(futurePtr))
                {
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    if (trap != IntPtr.Zero)
                    {
                        return trap;
                    }

                    await Task.Yield();
                }

                return trap;
            }
            finally
            {
                CallFuture.Native.wasmtime_call_future_delete(futurePtr);
                GC.KeepAlive(store);
            }
        }

        #region IExternal
        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Func,
                of = new ExternUnion { func = this.func }
            };
        }

        Store? IExternal.Store => store;
        #endregion

        internal static class Native
        {
            /// <summary>
            /// wasmtime_func_async_callback_t
            /// </summary>
            public unsafe delegate void WasmtimeFuncAsyncCallback(
                IntPtr env,
                IntPtr caller,
                Value* args, nuint nargs,
                Value* results, nuint nresults,
                out IntPtr trap_ret,
                out WasmtimeAsyncContinuation continuation_ret
            );

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_func_call_async(IntPtr context, in ExternFunc func, Value* args, nuint nargs, Value* results, nuint nresults, out IntPtr trap_ret, out IntPtr err_ret);
        }
    }
}