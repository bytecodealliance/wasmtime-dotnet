using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents the code associated with a trap.
    /// </summary>
    public enum TrapCode
    {
        /// <summary>
        /// The trap has no associated trap code.
        /// </summary>
        Undefined = -1,
        /// <summary>The trap was the result of exhausting the available stack space.</summary>
        StackOverflow = 0,
        /// <summary>The trap was the result of an out-of-bounds memory access.</summary>
        MemoryOutOfBounds = 1,
        /// <summary>The trap was the result of a wasm atomic operation that was presented with a misaligned linear-memory address.</summary>
        HeapMisaligned = 2,
        /// <summary>The trap was the result of an out-of-bounds access to a table.</summary>
        TableOutOfBounds = 3,
        /// <summary>The trap was the result of an indirect call to a null table entry.</summary>
        IndirectCallToNull = 4,
        /// <summary>The trap was the result of a signature mismatch on indirect call.</summary>
        BadSignature = 5,
        /// <summary>The trap was the result of an integer arithmetic operation that overflowed.</summary>
        IntegerOverflow = 6,
        /// <summary>The trap was the result of an integer division by zero.</summary>
        IntegerDivisionByZero = 7,
        /// <summary>The trap was the result of a failed float-to-int conversion.</summary>
        BadConversionToInteger = 8,
        /// <summary>The trap was the result of executing the `unreachable` instruction.</summary>
        Unreachable = 9,
        /// <summary>The trap was the result of interrupting execution.</summary>
        Interrupt = 10,
    }

    /// <summary>
    /// Represents a WebAssembly trap frame.
    /// </summary>
    [Serializable]
    public class TrapFrame
    {
        unsafe internal TrapFrame(IntPtr frame)
        {
            FunctionOffset = Native.wasm_frame_func_offset(frame);
            FunctionName = null;
            ModuleOffset = Native.wasm_frame_module_offset(frame);
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
        public nuint FunctionOffset { get; private set; }

        /// <summary>
        /// Gets the frame's function name.
        /// </summary>
        public string? FunctionName { get; private set; }

        /// <summary>
        /// Gets the frame's module offset from the start of the module.
        /// </summary>
        public nuint ModuleOffset { get; private set; }

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
    /// Provides access to a WebAssembly trap result
    /// </summary>
    public readonly ref struct TrapAccessor
    {
        private readonly IntPtr _trap;

        internal TrapAccessor(IntPtr trap)
        {
            _trap = trap;
        }

        internal void Dispose()
        {
            TrapException.Native.wasm_trap_delete(_trap);
        }

        /// <summary>
        /// Get the TrapCode
        /// </summary>
        public TrapCode TrapCode
        {
            get
            {
                if (TrapException.Native.wasmtime_trap_code(_trap, out var code))
                {
                    return code;
                }
                return TrapCode.Undefined;
            }
        }

        /// <summary>
        /// Get the message string
        /// </summary>
        public string Message
        {
            get
            {
                TrapException.Native.wasm_trap_message(_trap, out var bytes);
                using (bytes)
                {
                    unsafe
                    {
                        var byteSpan = new ReadOnlySpan<byte>(bytes.data, checked((int)bytes.size));

                        var indexOfNull = byteSpan.LastIndexOf((byte)0);
                        if (indexOfNull != -1)
                        {
                            byteSpan = byteSpan[..indexOfNull];
                        }

                        var message = Encoding.UTF8.GetString(byteSpan);
                        return message;
                    }
                }
            }
        }

        /// <summary>
        /// Copy the bytes of the message into a span
        /// </summary>
        /// <param name="output">Destination span to write bytes to</param>
        /// <param name="offset">Offset in the source span to begin copying from</param>
        /// <returns>The total length of the source span</returns>
        public int GetMessageBytes(Span<byte> output, int offset = 0)
        {
            TrapException.Native.wasm_trap_message(_trap, out var bytes);
            using (bytes)
            {
                unsafe
                {
                    var byteSpan = new ReadOnlySpan<byte>(bytes.data, checked((int)bytes.size));

                    var indexOfNull = byteSpan.LastIndexOf((byte)0);
                    if (indexOfNull != -1)
                    {
                        byteSpan = byteSpan[..indexOfNull];
                    }

                    var totalBytes = byteSpan.Length;
                    byteSpan = byteSpan[offset..];

                    if (byteSpan.Length > output.Length)
                    {
                        byteSpan[..output.Length].CopyTo(output);
                    }
                    else
                    {
                        byteSpan.CopyTo(output);
                    }

                    return totalBytes;
                }
            }
        }

        /// <summary>
        /// Get the trap frames
        /// </summary>
        /// <returns>A list of stack frames indicating where the trap occured, the first item in the list represents the innermost stack frame</returns>
        public List<TrapFrame> GetFrames()
        {
            TrapException.Native.wasm_trap_trace(_trap, out var frames);
            using (frames)
            {
                return TrapException.GetFrames(frames);
            }
        }

        /// <summary>
        /// Get a TrapException which contains all information about this trap
        /// </summary>
        /// <returns>A TrapException object which contains all the information about this trap in one convenient wrapper</returns>
        public TrapException GetException()
        {
            return TrapException.FromOwnedTrap(_trap, delete: false);
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
        public TrapException(string message, Exception? inner) : base(message, inner) { }

        /// <summary>
        /// Indentifies which type of trap this is.
        /// </summary>
        public TrapCode Type { get; private set; }

        /// <inheritdoc/>
        protected TrapException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        internal TrapException(string message, IReadOnlyList<TrapFrame>? frames, TrapCode type, Exception? innerException = null) 
            : base(message, innerException)
        {
            Type = type;
            Frames = frames;
        }

        internal static TrapException FromOwnedTrap(IntPtr trap, bool delete = true)
        {
            // Get the cause of the trap if available (in case the trap was caused by a
            // .NET exception thrown in a callback).
            var callbackTrapCause = Function.CallbackTrapCause;

            if (callbackTrapCause is not null)
            {
                // Clear the field as we consumed the value.
                Function.CallbackTrapCause = null;
            }

            var accessor = new TrapAccessor(trap);
            try
            {
                var trappedException = new TrapException(accessor.Message, accessor.GetFrames(), accessor.TrapCode, callbackTrapCause);
                return trappedException;
            }
            finally
            {
                if (delete)
                    accessor.Dispose();
            }
        }

        internal static List<TrapFrame> GetFrames(Native.FrameArray frames)
        {
            int framesSize = checked((int)frames.size);
            var trapFrames = new List<TrapFrame>(framesSize);
            for (var i = 0; i < framesSize; ++i)
            {
                unsafe
                {
                    trapFrames.Add(new TrapFrame(frames.data[i]));
                }
            }

            return trapFrames;
        }

        internal new static class Native
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
            public static extern void wasm_trap_trace(IntPtr trap, out FrameArray frames);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_trap_delete(IntPtr trap);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_frame_vec_delete(in FrameArray vec);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            internal static extern bool wasmtime_trap_code(IntPtr trap, out TrapCode exitCode);
        }
    }
}
