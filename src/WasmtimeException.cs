using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// The base type for Wasmtime exceptions.
    /// </summary>
    [System.Serializable]
    public class WasmtimeException : Exception
    {
        /// <inheritdoc/>
        public WasmtimeException() { }

        /// <inheritdoc/>
        public WasmtimeException(string message) : base(message) { }

        /// <inheritdoc/>
        public WasmtimeException(string message, Exception? inner) : base(message, inner) { }

        /// <summary>
        /// Gets the exit code when the trap results from executing the WASI <c>proc_exit</c> function.
        ///
        /// The value is <c>null</c> if the trap was not an exit trap.
        /// </summary>
        public int? ExitCode { get; private set; }

        /// <inheritdoc/>
        protected WasmtimeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        internal static WasmtimeException FromOwnedError(IntPtr error)
        {
            try
            {
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

                        return new WasmtimeException(Encoding.UTF8.GetString(byteSpan))
                        {
                            ExitCode = exitStatus
                        };
                    }
                }
            }
            finally
            {
                Native.wasmtime_error_delete(error);
            }
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_error_message(IntPtr error, out ByteArray message);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_error_delete(IntPtr error);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_error_exit_status(IntPtr error, out int exitStatus);
        }
    }
}
