using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Wasmtime.Components;

public class ComponentExport
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

    internal ComponentExport(IntPtr handle)
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
            Native.wasmtime_component_export_index_delete(handle);
            return true;
        }
    }

    internal static class Native
    {
        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_component_export_index_delete(IntPtr /* wasmtime_component_export_index_t* */ export_index);
    }
}