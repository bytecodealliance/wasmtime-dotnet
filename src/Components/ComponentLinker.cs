using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Wasmtime.Components;

public class ComponentLinker
    : IDisposable
{
    private readonly Handle handle;

    internal Handle NativeHandle
    {
        get
        {
            if (handle.IsInvalid || handle.IsClosed)
            {
                throw new ObjectDisposedException(typeof(Module).FullName);
            }

            return handle;
        }
    }

    internal ComponentLinker(IntPtr handle)
    {
        this.handle = new Handle(handle);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        handle.Dispose();
    }

    internal class Handle
        : SafeHandleZeroOrMinusOneIsInvalid
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
        // [DllImport(Engine.LibraryName)]
        //todo: wasmtime_component_linker_t * 	wasmtime_component_linker_new (const wasm_engine_t *engine)

        // [DllImport(Engine.LibraryName)]
        //todo: wasmtime_component_linker_instance_t * 	wasmtime_component_linker_root (wasmtime_component_linker_t *linker)

        // [DllImport(Engine.LibraryName)]
        //todo: wasmtime_error_t * 	wasmtime_component_linker_instantiate (const wasmtime_component_linker_t *linker, wasmtime_context_t *context, const wasmtime_component_t *component, wasmtime_component_instance_t *instance_out)

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_component_linker_delete(IntPtr /* wasmtime_component_linker_t* */ linker);

        //todo: wasmtime_error_t * 	wasmtime_component_linker_instance_add_instance (wasmtime_component_linker_instance_t *linker_instance, const char *name, size_t name_len, wasmtime_component_linker_instance_t **linker_instance_out)
        //todo: wasmtime_error_t* wasmtime_component_linker_instance_add_module(wasmtime_component_linker_instance_t* linker_instance, const char* name, size_t name_len, const wasmtime_module_t* module)
        //todo: wasmtime_error_t * 	wasmtime_component_linker_instance_add_func (wasmtime_component_linker_instance_t *linker_instance, const char *name, size_t name_len, wasmtime_component_func_callback_t callback, void *data, void(*finalizer)(void *))
        //todo: wasmtime_error_t * 	wasmtime_component_linker_add_wasip2 (wasmtime_component_linker_t *linker)

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_component_linker_instance_delete(IntPtr /* wasmtime_component_linker_instance_t* */ linker_instance);

    }
}