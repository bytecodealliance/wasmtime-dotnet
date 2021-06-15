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
        public WasmtimeException(string message, Exception inner) : base(message, inner) { }

        /// <inheritdoc/>
        protected WasmtimeException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        internal static WasmtimeException FromOwnedError(IntPtr error)
        {
            Native.wasmtime_error_message(error, out var bytes);
            Native.wasmtime_error_delete(error);

            unsafe
            {
                using (var message = bytes)
                {
                    return new WasmtimeException(Encoding.UTF8.GetString(message.data, (int)message.size));
                }
            }
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_error_message(IntPtr error, out ByteArray message);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_error_delete(IntPtr error);

        }
    }
}
