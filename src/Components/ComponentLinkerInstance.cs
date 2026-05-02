using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime.Components
{
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
        }

        private readonly Handle handle;
    }
}
