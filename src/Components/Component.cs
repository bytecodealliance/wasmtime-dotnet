using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime.Components
{
    /// <summary>
    /// Represents a compiled WebAssembly component.
    /// </summary>
    public class Component : IDisposable
    {
        /// <summary>
        /// Creates a <see cref="Component"/> from a span of bytes.
        /// </summary>
        /// <param name="engine">The engine to use for the component.</param>
        /// <param name="bytes">The bytes of the component.</param>
        /// <returns>Returns a new <see cref="Component"/>.</returns>
        public static Component FromBytes(Engine engine, ReadOnlySpan<byte> bytes)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var error = Native.wasmtime_component_new(engine.NativeHandle, ptr, (UIntPtr)bytes.Length, out var handle);
                    if (error != IntPtr.Zero)
                    {
                        throw new WasmtimeException($"WebAssembly component is not valid: {WasmtimeException.FromOwnedError(error).Message}");
                    }

                    return new Component(handle);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Component"/> from a file path.
        /// </summary>
        /// <param name="engine">The engine to use for the component.</param>
        /// <param name="path">The path to the WebAssembly component file.</param>
        /// <returns>Returns a new <see cref="Component"/>.</returns>
        public static Component FromFile(Engine engine, string path)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            return FromBytes(engine, File.ReadAllBytes(path));
        }

        /// <summary>
        /// Serializes the component to an array of bytes.
        /// </summary>
        /// <returns>Returns the serialized component as an array of bytes.</returns>
        public byte[] Serialize()
        {
            var error = Native.wasmtime_component_serialize(NativeHandle, out var array);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            using (array)
            {
                var len = checked((int)array.size);
                var bytes = new byte[len];
                unsafe
                {
                    Marshal.Copy((IntPtr)array.data, bytes, 0, len);
                }
                return bytes;
            }
        }

        /// <summary>
        /// Deserializes a previously serialized component from a span of bytes.
        /// </summary>
        /// <param name="engine">The engine to use to deserialize the component.</param>
        /// <param name="bytes">The previously serialized component bytes.</param>
        /// <returns>Returns the <see cref="Component"/> that was previously serialized.</returns>
        /// <remarks>The passed bytes must come from a previous call to <see cref="Serialize"/>.</remarks>
        public static Component Deserialize(Engine engine, ReadOnlySpan<byte> bytes)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var error = Native.wasmtime_component_deserialize(engine.NativeHandle, ptr, (UIntPtr)bytes.Length, out var handle);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    return new Component(handle);
                }
            }
        }

        /// <summary>
        /// Deserializes a previously serialized component from a file.
        /// </summary>
        /// <param name="engine">The engine to deserialize the component with.</param>
        /// <param name="path">The path to the previously serialized component.</param>
        /// <returns>Returns the <see cref="Component"/> that was previously serialized.</returns>
        /// <remarks>The file's contents must come from a previous call to <see cref="Serialize"/>.</remarks>
        public static Component DeserializeFile(Engine engine, string path)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            var error = Native.wasmtime_component_deserialize_file(engine.NativeHandle, path, out var handle);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return new Component(handle);
        }

        /// <summary>
        /// Looks up an export by name on this component.
        /// </summary>
        /// <param name="name">The name of the export to look up.</param>
        /// <returns>The export index if found; otherwise <see langword="null"/>.</returns>
        public ComponentExport? GetExport(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var index = Native.wasmtime_component_get_export_index(NativeHandle, IntPtr.Zero, name, (nuint)name.Length);
            if (index == IntPtr.Zero)
            {
                return null;
            }

            return new ComponentExport(index);
        }

        /// <summary>
        /// Looks up an export by name within a nested instance export.
        /// </summary>
        /// <param name="name">The name of the export to look up.</param>
        /// <param name="instanceExportIndex">The export index of the parent instance to search within.</param>
        /// <returns>The export index if found; otherwise <see langword="null"/>.</returns>
        public ComponentExport? GetExport(string name, ComponentExport instanceExportIndex)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (instanceExportIndex is null)
            {
                throw new ArgumentNullException(nameof(instanceExportIndex));
            }

            var index = Native.wasmtime_component_get_export_index(NativeHandle, instanceExportIndex.NativeHandle.DangerousGetHandle(), name, (nuint)name.Length);
            if (index == IntPtr.Zero)
            {
                return null;
            }

            return new ComponentExport(index);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        internal Component(IntPtr handle)
        {
            this.handle = new Handle(handle);
        }

        internal Handle NativeHandle
        {
            get
            {
                if (handle.IsInvalid || handle.IsClosed)
                {
                    throw new ObjectDisposedException(typeof(Component).FullName);
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
                Native.wasmtime_component_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_new(Engine.Handle engine, byte* bytes, UIntPtr size, out IntPtr handle);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_component_delete(IntPtr component);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_component_serialize(Handle component, out ByteArray bytes);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_component_deserialize(Engine.Handle engine, byte* bytes, UIntPtr size, out IntPtr handle);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_component_deserialize_file(Engine.Handle engine, [MarshalAs(Extensions.LPUTF8Str)] string path, out IntPtr handle);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_component_get_export_index(Handle component, IntPtr instanceExportIndex, [MarshalAs(Extensions.LPUTF8Str)] string name, nuint nameLength);
        }

        private readonly Handle handle;
    }
}
