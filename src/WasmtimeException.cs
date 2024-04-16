using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// The base type for Wasmtime exceptions.
    /// </summary>
    [Serializable]
    public class WasmtimeException : Exception
    {
        /// <inheritdoc/>
        public WasmtimeException() { }

        /// <inheritdoc/>
        public WasmtimeException(string message) : base(message) { }

        /// <inheritdoc/>
        public WasmtimeException(string message, Exception? inner) : base(message, inner) { }

        /// <summary>
        /// Gets the error's frames.
        /// </summary>
        public IReadOnlyList<TrapFrame>? Frames { get; private protected set; }

        /// <summary>
        /// Gets the exit code when the error results from executing the WASI <c>proc_exit</c> function.
        ///
        /// The value is <c>null</c> if the error was not an exit error.
        /// </summary>
        public int? ExitCode { get; private set; }

        internal static WasmtimeException FromOwnedError(IntPtr error)
        {
            try
            {
                // Get the cause of the error if available (in case the error was caused by a
                // .NET exception thrown in a callback).
                var callbackErrorCause = Function.CallbackErrorCause;

                if (callbackErrorCause is not null)
                {
                    // Clear the field as we consumed the value.
                    Function.CallbackErrorCause = null;
                }

                int? exitStatus = null;
                if (Native.wasmtime_error_exit_status(error, out int localExitStatus))
                {
                    exitStatus = localExitStatus;
                }

                Native.wasmtime_error_message(error, out var bytes);

                using (bytes)
                {
                    unsafe
                    {
                        var byteSpan = new ReadOnlySpan<byte>(bytes.data, checked((int)bytes.size));

                        Native.wasmtime_error_wasm_trace(error, out var frames);

                        using (frames)
                        {
                            return new WasmtimeException(Encoding.UTF8.GetString(byteSpan), callbackErrorCause)
                            {
                                ExitCode = exitStatus,
                                Frames = TrapException.GetFrames(frames)
                            };
                        }
                    }
                }
            }
            finally
            {
                Native.wasmtime_error_delete(error);
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_error_message(IntPtr error, out ByteArray message);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_error_wasm_trace(IntPtr error, out TrapException.Native.FrameArray frames);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_error_delete(IntPtr error);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_error_exit_status(IntPtr error, out int exitStatus);
        }
    }
}
