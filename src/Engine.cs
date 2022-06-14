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

        /// <summary>
        /// Constructs a new engine using the given configuration.
        /// </summary>
        /// <param name="config">The configuration to use for the engine.</param>
        /// <remarks>This method will dispose the given configuration.</remarks>
        public Engine(Config config)
        {
            handle = new Handle(Native.wasm_engine_new_with_config(config.NativeHandle));
            config.NativeHandle.SetHandleAsInvalid();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        /// <summary>
        /// Increments the epoch for epoch-based interruption
        /// </summary>
        public void IncrementEpoch()
        {
            Native.wasmtime_engine_increment_epoch(handle.DangerousGetHandle());
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
            public static extern IntPtr wasm_engine_new_with_config(Config.Handle config);

            [DllImport(LibraryName)]
            public static extern void wasm_engine_delete(IntPtr engine);

            [DllImport(LibraryName)]
            public static extern IntPtr wasmtime_engine_increment_epoch(IntPtr engine);
        }

        private readonly Handle handle;
    }
}
