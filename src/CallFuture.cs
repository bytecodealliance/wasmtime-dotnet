using System;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    internal class CallFuture
    {
        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_call_future_delete(IntPtr future);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_call_future_poll(nint futurePtr);
        }
    }
}
