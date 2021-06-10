using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents the Wasmtime engine.
    /// </summary>
    public class Engine : IDisposable
    {
        internal const string LibraryName = "wasmtime";

        /// <summary>
        /// Constructs a new default engine.
        /// </summary>
        public Engine()
        {
            handle = new Handle(Native.wasm_engine_new());
        }

        internal Engine(IntPtr config)
        {
            handle = new Handle(Native.wasm_engine_new_with_config(config));
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        internal Handle NativeHandle
        {
            get
            {
                if (handle.IsInvalid)
                {
                    throw new ObjectDisposedException(typeof(Engine).FullName);
                }

                return handle;
            }
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
                Native.wasm_engine_delete(handle);
                return true;
            }
        }

        private static class Native
        {
            [DllImport(LibraryName)]
            public static extern IntPtr wasm_engine_new();

            [DllImport(LibraryName)]
            public static extern IntPtr wasm_engine_new_with_config(IntPtr config);

            [DllImport(LibraryName)]
            public static extern void wasm_engine_delete(IntPtr engine);
        }

        private readonly Handle handle;
    }
}
