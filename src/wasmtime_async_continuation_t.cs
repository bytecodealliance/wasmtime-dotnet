﻿using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Wasmtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct wasmtime_async_continuation_t
    {
        private static readonly Native.wasmtime_func_async_continuation_callback_t ReturnTrue = _ => true;
        private static readonly Native.Finalizer DoNothingFinalizer = _ => { };
        public static readonly wasmtime_async_continuation_t ImmediateCompletion = new()
        {
            Env = IntPtr.Zero,
            Callback = ReturnTrue,
            Finalizer = DoNothingFinalizer
        };

        private static readonly Native.wasmtime_func_async_continuation_callback_t AwaitTask = env => ((Task?)GCHandle.FromIntPtr(env).Target)?.IsCompleted ?? true;
        private static readonly Native.Finalizer FreeGCHandleFinalizer = env => GCHandle.FromIntPtr(env).Free();

        public Native.wasmtime_func_async_continuation_callback_t Callback;
        public IntPtr Env;
        public Native.Finalizer Finalizer;

        public static wasmtime_async_continuation_t FromTask<T>(Task<T> task)
        {
            return FromTask((Task)task);
        }

        public static wasmtime_async_continuation_t FromTask(Task task)
        {
            return new()
            {
                Env = (IntPtr)GCHandle.Alloc(task),
                Callback = AwaitTask,
                Finalizer = FreeGCHandleFinalizer
            };
        }

        internal static class Native
        {
            public delegate void Finalizer(IntPtr env);

            /// <summary>
            /// wasmtime_func_async_continuation_callback_t
            /// </summary>
            [return: MarshalAs(UnmanagedType.I1)]
            public delegate bool wasmtime_func_async_continuation_callback_t(IntPtr env);
        }
    }
}
