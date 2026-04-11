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
        /// <summary>The trap was the result of running out of the configured fuel amount.</summary>
        OutOfFuel = 11,
        /// <summary>
        /// The trap was the result of atomic wait operations on non-shared memory.
        /// </summary>
        AtomicWaitNonSharedMemory = 12,
        /// <summary>
        /// The trap was the result of a call to a null reference.
        /// </summary>
        NullReference = 13,
        /// <summary>
        /// The trap was the result of an attempt to access beyond the bounds of an array.
        /// </summary>
        ArrayOutOfBounds = 14,
        /// <summary>
        /// The trap was the result of an allocation that was too large to succeed.
        /// </summary>
        AllocationTooLarge = 15,
        /// <summary>
        /// The trap was the result of an attempt to cast a reference to a type that it is not an instance of.
        /// </summary>
        CastFailure = 16,
        /// <summary>
        /// The trap was the result of a component calling another component that would have violated the reentrance rules.
        /// </summary>
        CannotEnterComponent = 17,
        /// <summary>
        /// The trap was the result of an async-lifted export failing to return a valid async result.
        /// </summary>
        /// <remarks>
        /// An async-lifted export failed to produce a result by calling `task.return` before returning `STATUS_DONE`
        /// and/or after all host tasks completed.
        /// </remarks>
        NoAsyncResult = 18,
        /// <summary>
        /// The trap was the result of suspending to a tag for which there is no active handler.
        /// </summary>
        UnhandledTag = 19,
        /// <summary>
        /// The trap was the result of an attempt to resume a continuation twice.
        /// </summary>
        ContinuationAlreadyConsumed = 20,
        /// <summary>
        /// The trap was the result of a Pulley opcode executed at runtime when the opcode was disabled at compile time.
        /// </summary>
        DisabledOpCode = 21,
        /// <summary>
        /// The trap was the result of an async event loop deadlocking; i.e. it cannot make further
        /// progress given that all host tasks have completed and any/all host-owned stream/future
        /// handles have been dropped.
        /// </summary>
        AsyncDeadlock = 22,
        /// <summary>
        /// When the `component-model` feature is enabled this trap was the result of a scenario
        /// where a component instance tried to call an import or intrinsic when it wasn't allowed
        /// to, e.g. from a post-return function.
        /// </summary>
        CannotLeaveComponent = 23,
        /// <summary>
        /// The trap was the result of a synchronous task attempted to make a potentially blocking
        /// call prior to returning.
        /// </summary>
        CannotBlockSyncTask = 24,
        /// <summary>
        /// The trap was the result of a component trying to lift a `char` with an invalid bit pattern.
        /// </summary>
        InvalidChar = 25,
        /// <summary>
        /// The trap was the result of a debug assertion generated for a fused adapter regarding the
        /// expected completion of a string encoding operation.
        /// </summary>
        DebugAssertStringEncodingFinished = 26,
        /// <summary>
        /// The trap was the result of a debug assertion generated for a fused adapter regarding a
        /// string encoding operation.
        /// </summary>
        DebugAssertEqualCodeUnits = 27,
        /// <summary>
        /// The trap was the result of a debug assertion generated for a fused adapter regarding the
        /// alignment of a pointer.
        /// </summary>
        DebugAssertPointerAligned = 28,
        /// <summary>
        /// The trap was the result of a debug assertion generated for a fused adapter regarding the
        /// upper bits of a 64-bit value.
        /// </summary>
        DebugAssertUpperBitsUnset = 29,
        /// <summary>
        /// The trap was the result of a component trying to lift or lower a string past the end of its memory.
        /// </summary>
        StringOutOfBounds = 30,
        /// <summary>
        /// The trap was the result of a component tryping to lift or lower a list past the end of its memory.
        /// </summary>
        ListOutOfBounds = 31,
        /// <summary>
        /// The trap was the result of a component using an invalid discriminant when lowering a variant value.
        /// </summary>
        InvalidDiscriminant = 32,
        /// <summary>
        /// The trap was the result of a component passing an unaligned pointer when lifting or
        /// lowering a value.
        /// </summary>
        UnalignedPointer = 33,
        /// <summary>
        /// The trap was the result of <c>task.cancel</c> being invoked in an invalid way.
        /// </summary>
        TaskCancelNotCancelled = 34,
        /// <summary>
        /// The trap was the result of <c>task.cancel</c> or <c>task.return</c> being called too many times
        /// </summary>
        TaskCancelOrReturnTwice = 35,
        /// <summary>
        /// The trap was the result of <c>subtask.cancel</c> being invoked after it already finished.
        /// </summary>
        SubtaskCancelAfterTerminal = 36,
        /// <summary>
        /// The trap was the result of <c>task.return</c> being invoked with an invalid type.
        /// </summary>
        TaskReturnInvalid = 37,
        /// <summary>
        /// The trap was the result of <c>waitable-set.drop</c> being invoked on a waitable set with waiters.
        /// </summary>
        WaitableSetDropHasWaiters = 38,
        /// <summary>
        /// The trap was the result of <c>subtask.drop</c> being invoked on a subtask that hasn't resolved yet.
        /// </summary>
        SubtaskDropNotResolved = 39,
        /// <summary>
        /// The trap was the result of <c>thread.new-indirect</c> being invoked with a function that
        /// has an invalid type.
        /// </summary>
        ThreadNewIndirectInvalidType = 40,
        /// <summary>
        /// The trap was the result of <c>thread.new-indirect</c> being invoked with an
        /// uninitialized function reference.
        /// </summary>
        ThreadNewIndirectUninitialized = 41,
        /// <summary>
        /// The trap was the result of backpressure-related intrinsics overflowing the built-in counter.
        /// </summary>
        BackpressureOverflow = 42,
        /// <summary>
        /// The trap was the result of an invalid code being returned from the callback of an
        /// async-lifted function.
        /// </summary>
        UnsupportedCallbackCode = 43,
        /// <summary>
        /// The trap was the result of trying to resume a thread which is not suspended.
        /// </summary>
        CannotResumeThread = 44,
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
            if (bytes != null && checked((int)bytes->size) > 0)
            {
                FunctionName = Encoding.UTF8.GetString(bytes->data, (int)bytes->size);
            }

            bytes = Native.wasmtime_frame_module_name(frame);
            if (bytes != null && checked((int)bytes->size) > 0)
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
            public static extern nuint wasm_frame_func_offset(IntPtr frame);

            [DllImport(Engine.LibraryName)]
            public static extern nuint wasm_frame_module_offset(IntPtr frame);

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
                    return (TrapCode)code;
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

        internal TrapException(string message, IReadOnlyList<TrapFrame>? frames, TrapCode type, Exception? innerException = null)
            : base(message, innerException)
        {
            Type = type;
            Frames = frames;
        }

        internal static TrapException FromOwnedTrap(IntPtr trap, bool delete = true)
        {
            var accessor = new TrapAccessor(trap);
            try
            {
                var trappedException = new TrapException(accessor.Message, accessor.GetFrames(), accessor.TrapCode);
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
                public nuint size;
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
            internal static extern bool wasmtime_trap_code(IntPtr trap, out byte exitCode);
        }
    }
}
