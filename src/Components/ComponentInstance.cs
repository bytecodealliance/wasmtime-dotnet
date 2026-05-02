using System;
using System.Runtime.InteropServices;

namespace Wasmtime.Components
{
    /// <summary>
    /// Represents an instantiated <see cref="Component"/> within a <see cref="Wasmtime.Store"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="ComponentInstance"/> has the same lifetime as the <see cref="Wasmtime.Store"/>
    /// it was created in: it is automatically reclaimed when the store is disposed and does not
    /// require explicit cleanup.
    /// </remarks>
    public class ComponentInstance
    {
        /// <summary>
        /// Looks up an export of this instance by name.
        /// </summary>
        /// <param name="name">The name of the export.</param>
        /// <returns>An export index if found; otherwise <see langword="null"/>.</returns>
        public ComponentExport? GetExport(string name)
        {
            return GetExport(name, null);
        }

        /// <summary>
        /// Looks up an export within a nested instance export of this instance.
        /// </summary>
        /// <param name="name">The name of the export.</param>
        /// <param name="parent">The parent instance export, or <see langword="null"/> for top-level.</param>
        /// <returns>An export index if found; otherwise <see langword="null"/>.</returns>
        public ComponentExport? GetExport(string name, ComponentExport? parent)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var parentHandle = parent is null ? IntPtr.Zero : parent.NativeHandle.DangerousGetHandle();

            IntPtr index;
            unsafe
            {
                fixed (WasmtimeComponentInstance* instancePtr = &instance)
                {
                    index = Native.wasmtime_component_instance_get_export_index(
                        instancePtr,
                        Store.Context.handle,
                        parentHandle,
                        name,
                        (UIntPtr)name.Length);
                }
            }

            GC.KeepAlive(Store);
            GC.KeepAlive(parent);

            if (index == IntPtr.Zero)
            {
                return null;
            }

            return new ComponentExport(index);
        }

        /// <summary>
        /// Looks up an exported function by name.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>A <see cref="ComponentFunction"/> if a function with that name was exported; otherwise <see langword="null"/>.</returns>
        public ComponentFunction? GetFunction(string name)
        {
            using var export = GetExport(name);
            if (export is null)
            {
                return null;
            }

            return GetFunction(export);
        }

        /// <summary>
        /// Looks up an exported function from a previously-resolved <see cref="ComponentExport"/>.
        /// </summary>
        /// <param name="export">The export index obtained via <see cref="GetExport(string)"/> or <see cref="Component.GetExport(string)"/>.</param>
        /// <returns>A <see cref="ComponentFunction"/> if the export refers to a function; otherwise <see langword="null"/>.</returns>
        public ComponentFunction? GetFunction(ComponentExport export)
        {
            if (export is null)
            {
                throw new ArgumentNullException(nameof(export));
            }

            bool found;
            WasmtimeComponentFunc func;
            unsafe
            {
                fixed (WasmtimeComponentInstance* instancePtr = &instance)
                {
                    found = Native.wasmtime_component_instance_get_func(
                        instancePtr,
                        Store.Context.handle,
                        export.NativeHandle,
                        out func);
                }
            }

            GC.KeepAlive(Store);
            GC.KeepAlive(export);

            if (!found)
            {
                return null;
            }

            return new ComponentFunction(Store, func);
        }

        internal ComponentInstance(Store store, WasmtimeComponentInstance instance)
        {
            Store = store;
            this.instance = instance;
        }

        /// <summary>
        /// The store this instance lives in.
        /// </summary>
        public Store Store { get; }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_instance_get_export_index(
                WasmtimeComponentInstance* instance,
                IntPtr context,
                IntPtr parentExportIndex,
                [MarshalAs(Extensions.LPUTF8Str)] string name,
                UIntPtr nameLength);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe bool wasmtime_component_instance_get_func(
                WasmtimeComponentInstance* instance,
                IntPtr context,
                ComponentExport.Handle exportIndex,
                out WasmtimeComponentFunc funcOut);
        }

        private WasmtimeComponentInstance instance;
    }
}
