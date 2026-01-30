using System;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    /// <summary>
    /// Extracts the main core module from a WASI 0.2 component for core-module execution.
    /// </summary>
    public static class ComponentCoreExtractor
    {
        private const string ShimLibraryName = "wasmtime_preview2_shim";

        public static void ExtractMainModule(string componentPath, string outputPath)
        {
            if (componentPath is null)
            {
                throw new ArgumentNullException(nameof(componentPath));
            }

            if (outputPath is null)
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            var result = Native.wasmtime_preview2_extract_core_module(componentPath, outputPath, out var errorPtr);
            if (result != 0)
            {
                var message = errorPtr == IntPtr.Zero
                    ? "Failed to extract core module from component."
                    : PtrToStringUtf8NullTerminated(errorPtr);

                if (errorPtr != IntPtr.Zero)
                {
                    Native.wasmtime_preview2_free_error(errorPtr);
                }

                throw new WasmtimeException(message);
            }
        }

        private static class Native
        {
            [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int wasmtime_preview2_extract_core_module(
                [MarshalAs(Extensions.LPUTF8Str)] string componentPath,
                [MarshalAs(Extensions.LPUTF8Str)] string outputPath,
                out IntPtr errorMessage);

            [DllImport(ShimLibraryName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void wasmtime_preview2_free_error(IntPtr error);
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
    }
}
