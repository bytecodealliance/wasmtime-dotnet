using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime.Components
{
    /// <summary>
    /// Represents a cached lookup index for a component export.
    /// </summary>
    public class ComponentExport : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        internal ComponentExport(IntPtr handle)
        {
            this.handle = new Handle(handle);
        }

        internal Handle NativeHandle
        {
            get
            {
                if (handle.IsInvalid || handle.IsClosed)
                {
                    throw new ObjectDisposedException(typeof(ComponentExport).FullName);
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
                Native.wasmtime_component_export_index_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_component_export_index_delete(IntPtr exportIndex);
        }

        private readonly Handle handle;
    }
}
