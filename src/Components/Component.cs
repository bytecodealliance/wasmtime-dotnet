using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Wasmtime.Components;

/// <summary>
/// Representation of a component in the component model. 
/// </summary>
public class Component
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

    internal Component(IntPtr handle)
    {
        this.handle = new Handle(handle);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        handle.Dispose();
    }

    /// <summary>
    /// Creates a <see cref="Component"/> given bytes.
    /// </summary>
    /// <param name="engine">The engine to use for the Component.</param>
    /// <param name="bytes">The bytes of the Component.</param>
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
    /// This function serializes compiled component artifacts as blob data. 
    /// </summary>
    /// <returns>If the conversion is successful, the serialized compiled component.</returns>
    public byte[] Serialize()
    {
        var error = Native.wasmtime_component_serialize(NativeHandle, out var bytes);
        if (error != IntPtr.Zero)
        {
            throw WasmtimeException.FromOwnedError(error);
        }

        using (bytes)
            return bytes.ToArray();
    }

    /// <summary>
    /// Deserializes a previously serialized component from a span of bytes.
    /// </summary>
    /// <param name="engine">The engine to use to deserialize the component.</param>
    /// <param name="bytes">The previously serialized component bytes.</param>
    /// <returns>Returns the <see cref="Component" /> that was previously serialized.</returns>
    /// <remarks>The passed bytes must come from a previous call to <see cref="Component.Serialize" />.</remarks>
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
    /// <returns>Returns the <see cref="Component" /> that was previously serialized.</returns>
    /// <remarks>The file's contents must come from a previous call to <see cref="Component.Serialize" />.</remarks>
    public static Component DeserializeFile(Engine engine, string path)
    {
        if (engine is null)
        {
            throw new ArgumentNullException(nameof(engine));
        }

        var error = Native.wasmtime_component_deserialize_file(engine.NativeHandle, path, out var handle);
        if (error != IntPtr.Zero)
        {
            throw WasmtimeException.FromOwnedError(error);
        }

        return new Component(handle);
    }

    public ComponentExport? GetExport(string name)
    {
        var ret = Native.wasmtime_component_get_export_index(NativeHandle, null, name, (nuint)name.Length);
        if (ret == IntPtr.Zero)
            return null;

        return new ComponentExport(ret);
    }

    public ComponentExport? GetExport(string name, ComponentExport instance_export_index)
    {
        var ret = Native.wasmtime_component_get_export_index(NativeHandle, instance_export_index.NativeHandle, name, (nuint)name.Length);
        if (ret == IntPtr.Zero)
            return null;

        return new ComponentExport(ret);
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
            Native.wasmtime_component_delete(handle);
            return true;
        }
    }

    internal static class Native
    {
        [DllImport(Engine.LibraryName)]
        public static extern unsafe IntPtr wasmtime_component_new(Engine.Handle engine, byte* bytes, nuint size, out IntPtr handle);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_component_delete(IntPtr handle);

        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_component_serialize(Handle component, out ByteArray ret);

        [DllImport(Engine.LibraryName)]
        public static extern unsafe IntPtr wasmtime_component_deserialize(Engine.Handle engine, byte* bytes, nuint size, out IntPtr handle);

        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_component_deserialize_file(Engine.Handle engine, string path, out IntPtr handle);

        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_component_get_export_index(Handle component, ComponentExport.Handle? instance_export_index, string name, nuint name_len);
    }
}