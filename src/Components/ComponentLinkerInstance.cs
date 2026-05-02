using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime.Components
{
    /// <summary>
    /// Callback signature for host-defined component functions.
    /// </summary>
    /// <param name="arguments">The arguments passed by the component.</param>
    /// <param name="results">A span sized to the number of results expected by the function.
    /// The callback must populate every element.</param>
    /// <remarks>
    /// Throwing from a callback surfaces as a wasmtime trap; the message is taken from
    /// <see cref="Exception.Message"/>.
    /// </remarks>
    public delegate void ComponentFuncCallback(
        ReadOnlySpan<ComponentValue> arguments,
        Span<ComponentValue> results);

    /// <summary>
    /// Represents an instance scope within a <see cref="ComponentLinker"/> in which functions,
    /// modules, and nested instances can be defined.
    /// </summary>
    /// <remarks>
    /// Obtained via <see cref="ComponentLinker.Root"/> or <see cref="Instance(string)"/>.
    /// While alive, holds an exclusive lock on its parent linker.
    /// </remarks>
    public class ComponentLinkerInstance : IDisposable
    {
        /// <summary>
        /// Defines a nested instance within this instance.
        /// </summary>
        /// <param name="name">The name of the nested instance.</param>
        /// <returns>The newly created nested <see cref="ComponentLinkerInstance"/>.</returns>
        public ComponentLinkerInstance Instance(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            unsafe
            {
                fixed (byte* ptr = nameBytes)
                {
                    var error = Native.wasmtime_component_linker_instance_add_instance(
                        NativeHandle,
                        ptr,
                        (UIntPtr)nameBytes.Length,
                        out var nestedHandle);

                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    return new ComponentLinkerInstance(nestedHandle);
                }
            }
        }

        /// <summary>
        /// Defines a host function that components can import under the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name to expose the function under.</param>
        /// <param name="callback">The C# implementation invoked when the component calls the function.</param>
        /// <remarks>
        /// The <paramref name="callback"/> is rooted via a managed handle for the lifetime of the
        /// linker; when the linker is disposed the handle is released. Inside the callback you can
        /// read <see cref="ComponentValue"/> arguments and write results — both spans share the
        /// underlying buffers wasmtime owns, so do not hold them past the call.
        /// </remarks>
        public void DefineFunc(string name, ComponentFuncCallback callback)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var entry = new HostCallback(callback);
            var handle = GCHandle.Alloc(entry);
            var data = GCHandle.ToIntPtr(handle);

            var nameBytes = Encoding.UTF8.GetBytes(name);
            unsafe
            {
                fixed (byte* ptr = nameBytes)
                {
                    var error = Native.wasmtime_component_linker_instance_add_func(
                        NativeHandle,
                        ptr,
                        (UIntPtr)nameBytes.Length,
                        HostCallback.NativeTrampoline,
                        data,
                        HostCallback.NativeFinalizer);

                    if (error != IntPtr.Zero)
                    {
                        // Drop the GCHandle since wasmtime won't call the finalizer on failure.
                        handle.Free();
                        throw WasmtimeException.FromOwnedError(error);
                    }
                }
            }
        }

        /// <summary>
        /// Defines a core <see cref="Module"/> within this instance, providing it as an import to a component.
        /// </summary>
        /// <param name="name">The name to bind the module to.</param>
        /// <param name="module">The module to expose.</param>
        public void AddModule(string name, Module module)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            var nameBytes = System.Text.Encoding.UTF8.GetBytes(name);
            unsafe
            {
                fixed (byte* ptr = nameBytes)
                {
                    var error = Native.wasmtime_component_linker_instance_add_module(
                        NativeHandle,
                        ptr,
                        (UIntPtr)nameBytes.Length,
                        module.NativeHandle);

                    GC.KeepAlive(module);

                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        internal ComponentLinkerInstance(IntPtr handle)
        {
            this.handle = new Handle(handle);
        }

        internal Handle NativeHandle
        {
            get
            {
                if (handle.IsInvalid || handle.IsClosed)
                {
                    throw new ObjectDisposedException(typeof(ComponentLinkerInstance).FullName);
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
                Native.wasmtime_component_linker_instance_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_component_linker_instance_delete(IntPtr linkerInstance);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_linker_instance_add_instance(
                Handle linkerInstance,
                byte* name,
                UIntPtr nameLength,
                out IntPtr nestedOut);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_linker_instance_add_module(
                Handle linkerInstance,
                byte* name,
                UIntPtr nameLength,
                Module.Handle module);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_linker_instance_add_func(
                Handle linkerInstance,
                byte* name,
                UIntPtr nameLength,
                HostCallback.NativeCallbackDelegate callback,
                IntPtr data,
                HostCallback.NativeFinalizerDelegate finalizer);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_error_new([MarshalAs(Extensions.LPUTF8Str)] string message);
        }

        private readonly Handle handle;

        internal sealed class HostCallback
        {
            internal delegate IntPtr NativeCallbackDelegate(
                IntPtr data,
                IntPtr context,
                IntPtr args,
                UIntPtr argsLength,
                IntPtr results,
                UIntPtr resultsLength);

            internal delegate void NativeFinalizerDelegate(IntPtr data);

            internal static readonly NativeCallbackDelegate NativeTrampoline = TrampolineImpl;
            internal static readonly NativeFinalizerDelegate NativeFinalizer = FinalizerImpl;

            private readonly ComponentFuncCallback callback;

            internal HostCallback(ComponentFuncCallback callback)
            {
                this.callback = callback;
            }

            private static IntPtr TrampolineImpl(
                IntPtr data,
                IntPtr context,
                IntPtr args,
                UIntPtr argsLength,
                IntPtr results,
                UIntPtr resultsLength)
            {
                try
                {
                    var handle = GCHandle.FromIntPtr(data);
                    var entry = (HostCallback)handle.Target!;

                    unsafe
                    {
                        var argSpan = new ReadOnlySpan<ComponentValue>(
                            (ComponentValue*)args,
                            checked((int)(uint)argsLength));
                        var resultSpan = new Span<ComponentValue>(
                            (ComponentValue*)results,
                            checked((int)(uint)resultsLength));

                        entry.callback(argSpan, resultSpan);
                    }

                    return IntPtr.Zero;
                }
                catch (Exception ex)
                {
                    return Native.wasmtime_error_new(ex.Message);
                }
            }

            private static void FinalizerImpl(IntPtr data)
            {
                if (data == IntPtr.Zero)
                {
                    return;
                }

                var handle = GCHandle.FromIntPtr(data);
                if (handle.IsAllocated)
                {
                    handle.Free();
                }
            }
        }
    }
}
