using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime.Components
{
    /// <summary>
    /// Resolves imports for a <see cref="Component"/> and instantiates it within a <see cref="Store"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="ComponentLinker"/> describes the imports a component requires.
    /// Use <see cref="Root"/> or <see cref="ComponentLinkerInstance.Instance(string)"/>
    /// to define functions, modules, or nested instances, then call
    /// <see cref="Instantiate(Store, Component)"/> to create a runnable
    /// <see cref="ComponentInstance"/>.
    /// </remarks>
    public class ComponentLinker : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="ComponentLinker"/> for the specified engine.
        /// </summary>
        /// <param name="engine">The engine the linker belongs to.</param>
        public ComponentLinker(Engine engine)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            handle = new Handle(Native.wasmtime_component_linker_new(engine.NativeHandle));
        }

        /// <summary>
        /// Returns the root <see cref="ComponentLinkerInstance"/>, used to define names in the root namespace.
        /// </summary>
        /// <remarks>
        /// While the returned instance is alive, the linker must not be used directly. Dispose the instance
        /// before invoking other linker operations.
        /// </remarks>
        public ComponentLinkerInstance Root()
        {
            return new ComponentLinkerInstance(Native.wasmtime_component_linker_root(NativeHandle));
        }

        /// <summary>
        /// Adds all WASI 0.2 (preview 2) interfaces to this linker.
        /// </summary>
        public void AddWasiPreview2()
        {
            var error = Native.wasmtime_component_linker_add_wasip2(NativeHandle);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// Instantiates the given <paramref name="component"/> within <paramref name="store"/>, satisfying
        /// its imports from this linker.
        /// </summary>
        /// <param name="store">The store the instance lives in.</param>
        /// <param name="component">The component to instantiate.</param>
        /// <returns>A <see cref="ComponentInstance"/> usable until <paramref name="store"/> is disposed.</returns>
        public ComponentInstance Instantiate(Store store, Component component)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (component is null)
            {
                throw new ArgumentNullException(nameof(component));
            }

            var error = Native.wasmtime_component_linker_instantiate(
                NativeHandle,
                store.Context.handle,
                component.NativeHandle,
                out var raw);

            GC.KeepAlive(store);
            GC.KeepAlive(component);

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return new ComponentInstance(store, raw);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        internal Handle NativeHandle
        {
            get
            {
                if (handle.IsInvalid || handle.IsClosed)
                {
                    throw new ObjectDisposedException(typeof(ComponentLinker).FullName);
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
                Native.wasmtime_component_linker_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_component_linker_new(Engine.Handle engine);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_component_linker_delete(IntPtr linker);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_component_linker_root(Handle linker);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_component_linker_add_wasip2(Handle linker);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_component_linker_instantiate(
                Handle linker,
                IntPtr context,
                Component.Handle component,
                out WasmtimeComponentInstance instanceOut);
        }

        private readonly Handle handle;
    }

    /// <summary>
    /// Mirror of `wasmtime_component_instance_t` — the value-typed handle that wasmtime fills in
    /// when a component is instantiated.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct WasmtimeComponentInstance
    {
        public ulong StoreId;
        public uint Private;
    }
}
