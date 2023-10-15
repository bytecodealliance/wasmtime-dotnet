using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    internal class CallFuture
        : IDisposable
    {
        private readonly Handle handle;

        internal CallFuture(Handle handle)
        {
            this.handle = handle;
        }

        public void Dispose()
        {
            if (!handle.IsClosed)
                handle.Dispose();
        }

        /// <summary>
        /// Executes WebAssembly in the function.
        /// </summary>
        /// <remarks>
        /// Returns true if the function call has completed. After this function returns true, it should <b>not</b> be 
        /// called again for a given future.
        /// <br /><br />
        /// For more see the information at <a href="https://docs.wasmtime.dev/api/wasmtime/struct.Config.html#asynchronous-wasm" />
        /// </remarks>
        /// <returns>
        /// This function returns false if execution has yielded either due to being out of fuel
        /// (see <see cref="Store.OutOfFuelYieldAsync"/>), or the epoch has been incremented enough
        /// (see <see cref="Store.EpochDeadlineAsyncYieldAndUpdate"/>). The function may also return false if 
        /// asynchronous host functions have been called, which then calling this  function will call the 
        /// continuation from the async host function.
        /// </returns>
        public bool Poll()
        {
            if (handle.IsClosed)
                throw new InvalidOperationException("Cannot call `Poll` after future has been disposed");

            var result = Native.wasmtime_call_future_poll(this);
            if (result)
                Dispose();

            return result;
        }

        internal class Handle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public Handle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                Native.wasmtime_call_future_delete(handle);
                return true;
            }
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_call_future_delete(IntPtr future);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_call_future_poll(CallFuture future);
        }
    }
}
