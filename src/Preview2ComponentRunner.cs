using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents a preopened directory mapping for WASI preview2.
    /// </summary>
    public readonly struct PreopenDirectory
    {
        public PreopenDirectory(string hostPath, string guestPath)
        {
            HostPath = hostPath ?? throw new ArgumentNullException(nameof(hostPath));
            GuestPath = guestPath ?? throw new ArgumentNullException(nameof(guestPath));
        }

        public string HostPath { get; }
        public string GuestPath { get; }
    }

    /// <summary>
    /// Runs WASI preview2 (WASI 0.2) components through the native preview2 shim.
    /// </summary>
    public static class Preview2ComponentRunner
    {
        private const string ShimLibraryName = "wasmtime_preview2_shim";

        public static int Run(
            string componentPath,
            IReadOnlyList<string>? args = null,
            IReadOnlyDictionary<string, string>? environment = null,
            IReadOnlyList<PreopenDirectory>? preopens = null,
            bool inheritStdio = true,
            bool inheritEnvironment = true,
            bool inheritNetwork = true)
        {
            if (componentPath is null)
            {
                throw new ArgumentNullException(nameof(componentPath));
            }

            var argList = new List<string>(args ?? Array.Empty<string>());
            var envList = new List<string>();
            if (environment is not null)
            {
                foreach (var kvp in environment)
                {
                    envList.Add($"{kvp.Key}={kvp.Value}");
                }
            }

            var preopenList = new List<string>();
            if (preopens is not null)
            {
                foreach (var preopen in preopens)
                {
                    if (string.IsNullOrEmpty(preopen.HostPath))
                    {
                        throw new ArgumentException("Preopen host path must not be empty.", nameof(preopens));
                    }
                    if (string.IsNullOrEmpty(preopen.GuestPath))
                    {
                        throw new ArgumentException("Preopen guest path must not be empty.", nameof(preopens));
                    }
                    preopenList.Add($"{preopen.HostPath}::{preopen.GuestPath}");
                }
            }

            return RunCore(
                componentPath,
                argList,
                envList,
                preopenList,
                inheritStdio,
                inheritEnvironment,
                inheritNetwork);
        }

        private static unsafe int RunCore(
            string componentPath,
            IReadOnlyList<string> args,
            IReadOnlyList<string> env,
            IReadOnlyList<string> preopens,
            bool inheritStdio,
            bool inheritEnvironment,
            bool inheritNetwork)
        {
            var (argPtrs, argHandles) = ToUtf8PtrArray(args);
            var (envPtrs, envHandles) = ToUtf8PtrArray(env);
            var (preopenPtrs, preopenHandles) = ToUtf8PtrArray(preopens);

            try
            {
                fixed (byte** argPtr = argPtrs)
                fixed (byte** envPtr = envPtrs)
                fixed (byte** preopenPtr = preopenPtrs)
                {
                    byte** argUse = argPtrs.Length == 0 ? null : argPtr;
                    byte** envUse = envPtrs.Length == 0 ? null : envPtr;
                    byte** preopenUse = preopenPtrs.Length == 0 ? null : preopenPtr;

                    var result = Native.wasmtime_preview2_run_component(
                        componentPath,
                        argUse,
                        (nuint)argPtrs.Length,
                        envUse,
                        (nuint)envPtrs.Length,
                        preopenUse,
                        (nuint)preopenPtrs.Length,
                        inheritStdio,
                        inheritEnvironment,
                        inheritNetwork,
                        out var exitCode,
                        out var errorPtr);

                    if (result != 0)
                    {
                        var message = errorPtr == IntPtr.Zero
                            ? "Failed to run WASI preview2 component."
                            : PtrToStringUtf8NullTerminated(errorPtr);

                        if (errorPtr != IntPtr.Zero)
                        {
                            Native.wasmtime_preview2_free_error(errorPtr);
                        }

                        throw new WasmtimeException(message);
                    }

                    return exitCode;
                }
            }
            finally
            {
                FreeHandles(argHandles);
                FreeHandles(envHandles);
                FreeHandles(preopenHandles);
            }
        }

        private static void FreeHandles(GCHandle[] handles)
        {
            for (int i = 0; i < handles.Length; i++)
            {
                if (handles[i].IsAllocated)
                {
                    handles[i].Free();
                }
            }
        }

        private static unsafe string PtrToStringUtf8NullTerminated(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
            {
                return string.Empty;
            }

            var length = 0;
            var bytes = (byte*)ptr;
            while (bytes[length] != 0)
            {
                length++;
            }

            return Extensions.PtrToStringUTF8(ptr, length);
        }

        private static unsafe (byte*[] ptrs, GCHandle[] handles) ToUtf8PtrArray(IReadOnlyList<string> strings)
        {
            if (strings.Count == 0)
            {
                return (new byte*[0], Array.Empty<GCHandle>());
            }

            var handles = new GCHandle[strings.Count];
            var ptrs = new byte*[strings.Count];
            for (int i = 0; i < strings.Count; ++i)
            {
                handles[i] = GCHandle.Alloc(
                    Encoding.UTF8.GetBytes(strings[i] + '\0'),
                    GCHandleType.Pinned
                );
                ptrs[i] = (byte*)handles[i].AddrOfPinnedObject();
            }

            return (ptrs, handles);
        }

        private static unsafe class Native
        {
            [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int wasmtime_preview2_run_component(
                [MarshalAs(Extensions.LPUTF8Str)] string componentPath,
                byte** argv,
                nuint argc,
                byte** env,
                nuint envc,
                byte** preopens,
                nuint preopenCount,
                [MarshalAs(UnmanagedType.I1)] bool inheritStdio,
                [MarshalAs(UnmanagedType.I1)] bool inheritEnvironment,
                [MarshalAs(UnmanagedType.I1)] bool inheritNetwork,
                out int exitCode,
                out IntPtr errorMessage);

            [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void wasmtime_preview2_free_error(IntPtr error);
        }
    }
}
