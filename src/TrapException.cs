using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly trap frame.
    /// </summary>
    [Serializable]
    public class TrapFrame
    {
        unsafe internal TrapFrame(IntPtr frame)
        {
            FunctionOffset = (int)Native.wasm_frame_func_offset(frame);
            FunctionName = null;
            ModuleOffset = (int)Native.wasm_frame_module_offset(frame);
            ModuleName = null;

            var bytes = Native.wasmtime_frame_func_name(frame);
            if (bytes != null && (int)bytes->size > 0)
            {
                FunctionName = Encoding.UTF8.GetString(bytes->data, (int)bytes->size);
            }

            bytes = Native.wasmtime_frame_module_name(frame);
            if (bytes != null && (int)bytes->size > 0)
            {
                ModuleName = Encoding.UTF8.GetString(bytes->data, (int)bytes->size);
            }
        }

        /// <summary>
        /// Gets the frame's byte offset from the start of the function.
        /// </summary>
        public int FunctionOffset { get; private set; }

        /// <summary>
        /// Gets the frame's function name.
        /// </summary>
        public string? FunctionName { get; private set; }

        /// <summary>
        /// Gets the frame's module offset from the start of the module.
        /// </summary>
        public int ModuleOffset { get; private set; }

        /// <summary>
        /// Gets the frame's module name.
        /// </summary>
        public string? ModuleName { get; private set; }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern UIntPtr wasm_frame_func_offset(IntPtr frame);

            [DllImport(Engine.LibraryName)]
            public static extern UIntPtr wasm_frame_module_offset(IntPtr frame);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ByteArray* wasmtime_frame_func_name(IntPtr frame);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ByteArray* wasmtime_frame_module_name(IntPtr frame);
        }
    }

    /// <summary>
    /// The exception for WebAssembly traps.
    /// </summary>
    [Serializable]
    public class TrapException : WasmtimeException
    {
        /// <inheritdoc/>
        public TrapException() { }

        /// <inheritdoc/>
        public TrapException(string message) : base(message) { }

        /// <inheritdoc/>
        public TrapException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// Gets the trap's frames.
        /// </summary>
        public IReadOnlyList<TrapFrame>? Frames { get; private set; }

        /// <inheritdoc/>
        protected TrapException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        private TrapException(string message, IReadOnlyList<TrapFrame> frames) : base(message)
        {
            Frames = frames;
        }

        internal static TrapException FromOwnedTrap(IntPtr trap)
        {
            unsafe
            {
                Native.wasm_trap_message(trap, out var bytes);
                var byteSpan = new ReadOnlySpan<byte>(bytes.data, checked((int)bytes.size));

                int indexOfNull = byteSpan.LastIndexOf((byte)0);
                if (indexOfNull != -1)
                {
                    byteSpan = byteSpan.Slice(0, indexOfNull);
                }

                var message = Encoding.UTF8.GetString(byteSpan);
                bytes.Dispose();

                Native.wasm_trap_trace(trap, out var frames);

                var trapFrames = new List<TrapFrame>((int)frames.size);
                for (int i = 0; i < (int)frames.size; ++i)
                {
                    trapFrames.Add(new TrapFrame(frames.data[i]));
                }
                frames.Dispose();

                Native.wasm_trap_delete(trap);

                return new TrapException(message, trapFrames);
            }
        }

        private static class Native
        {
            [StructLayout(LayoutKind.Sequential)]
            public unsafe struct FrameArray : IDisposable
            {
                public UIntPtr size;
                public IntPtr* data;

                public void Dispose()
                {
                    Native.wasm_frame_vec_delete(this);
                }
            }

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_trap_message(IntPtr trap, out ByteArray message);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_trap_trace(IntPtr trap, out FrameArray message);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_trap_delete(IntPtr trap);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_frame_vec_delete(in FrameArray vec);
        }
    }
}
