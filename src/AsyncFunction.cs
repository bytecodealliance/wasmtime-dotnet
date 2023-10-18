using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Wasmtime
{
    public partial class AsyncFunction
        : IExternal
    {
        internal static readonly Function.Native.Finalizer Finalizer = (p) => GCHandle.FromIntPtr(p).Free();

        public delegate void UntypedCallbackDelegate(Caller caller, ReadOnlySpan<ValueBox> arguments, Span<ValueBox> results);

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
            using var pin = argsAndResults.AsMemory().Pin();

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

                futurePtr = Native.wasmtime_func_call_async(store.Context.handle, func, argsPtr, (nuint)Parameters.Count, resultsPtr, (nuint)Results.Count, out var trap, out var error);

                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }

                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }
            }

            try
            {
                //todo: why is this crashing?!
                // - NullReferenceException, but from where
                while (!CallFuture.Native.wasmtime_call_future_poll(futurePtr))
                    await Task.Yield();

                //todo: how do I get traps and errors from the future?
                //todo: results are in `resultsPtr`
            }
            finally
            {
                CallFuture.Native.wasmtime_call_future_delete(futurePtr);
            }

            GC.KeepAlive(store);
        }

        private Task<TResult?> InvokeWithReturn<TResult>(Value[] argsAndResults, IReturnTypeFactory<TResult> factory)
        {
            throw new NotImplementedException();
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
                out wasmtime_async_continuation_t continuation_ret
            );

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_func_call_async(IntPtr context, in ExternFunc func, Value* args, nuint nargs, Value* results, nuint nresults, out IntPtr trap_ret, out IntPtr err_ret);
        }
    }
}