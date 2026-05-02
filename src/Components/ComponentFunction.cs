using System;
using System.Runtime.InteropServices;

namespace Wasmtime.Components
{
    /// <summary>
    /// Represents a callable function exported by a <see cref="ComponentInstance"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="ComponentFunction"/> is bound to its originating <see cref="Wasmtime.Store"/> and
    /// becomes invalid once that store is disposed. Like a core wasm function, it does not need
    /// explicit cleanup.
    /// </remarks>
    public class ComponentFunction
    {
        /// <summary>
        /// Invokes the function with the given arguments and writes results into <paramref name="results"/>.
        /// </summary>
        /// <param name="arguments">The arguments to pass; their kinds must match the function signature.</param>
        /// <param name="results">A span sized to the number of results the function produces.</param>
        /// <remarks>
        /// After a successful call, <see cref="PostReturn"/> must be invoked before the next call on this
        /// function. The call helper invokes it automatically; only call it manually if you handle the
        /// raw P/Invoke.
        /// </remarks>
        public void Call(ReadOnlySpan<ComponentValue> arguments, Span<ComponentValue> results)
        {
            var store = Store;

            unsafe
            {
                fixed (ComponentValue* argsPtr = arguments)
                fixed (ComponentValue* resultsPtr = results)
                fixed (WasmtimeComponentFunc* funcPtr = &func)
                {
                    var error = Native.wasmtime_component_func_call(
                        funcPtr,
                        store.Context.handle,
                        argsPtr,
                        (UIntPtr)arguments.Length,
                        resultsPtr,
                        (UIntPtr)results.Length);

                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    var postReturnError = Native.wasmtime_component_func_post_return(funcPtr, store.Context.handle);
                    if (postReturnError != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(postReturnError);
                    }
                }
            }

            GC.KeepAlive(store);
        }

        /// <summary>
        /// Invokes the post-return canonical ABI option for this function.
        /// </summary>
        /// <remarks>
        /// Required after each <see cref="Call(System.ReadOnlySpan{Wasmtime.Components.ComponentValue},System.Span{Wasmtime.Components.ComponentValue})"/>
        /// to release any temporary allocations the guest produced for the result buffer. Most callers
        /// do not need to invoke this directly because <see cref="Call"/> performs it automatically.
        /// </remarks>
        public void PostReturn()
        {
            var store = Store;

            unsafe
            {
                fixed (WasmtimeComponentFunc* funcPtr = &func)
                {
                    var error = Native.wasmtime_component_func_post_return(funcPtr, store.Context.handle);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }
                }
            }

            GC.KeepAlive(store);
        }

        internal ComponentFunction(Store store, WasmtimeComponentFunc func)
        {
            Store = store;
            this.func = func;
        }

        /// <summary>
        /// The store this function lives in.
        /// </summary>
        public Store Store { get; }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_func_call(
                WasmtimeComponentFunc* func,
                IntPtr context,
                ComponentValue* args,
                UIntPtr argsSize,
                ComponentValue* results,
                UIntPtr resultsSize);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_func_post_return(
                WasmtimeComponentFunc* func,
                IntPtr context);
        }

        private WasmtimeComponentFunc func;
    }

    /// <summary>
    /// Mirror of <c>wasmtime_component_func_t</c>. The C header declares an anonymous nested
    /// struct, which carries trailing padding to satisfy 8-byte alignment, so the actual size
    /// is 24 bytes (not the 16 a flat reading suggests). The Rust side enforces this layout via
    /// a const assertion in <c>crates/wasmtime/src/runtime/component/func.rs</c>.
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = 24)]
    internal struct WasmtimeComponentFunc
    {
        [FieldOffset(0)] public ulong StoreId;
        [FieldOffset(8)] public uint Private1;
        [FieldOffset(16)] public uint Private2;
    }
}
