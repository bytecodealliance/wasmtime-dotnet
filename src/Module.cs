using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ByteArray : IDisposable
    {
        public UIntPtr size;
        public byte* data;

        public void Dispose()
        {
            Native.wasm_byte_vec_delete(this);
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasm_byte_vec_delete(in ByteArray vec);
        }
    }

    internal interface IExternal
    {
        Extern AsExtern();
    }

    /// <summary>
    /// Represents a WebAssembly module.
    /// </summary>
    public class Module : IDisposable
    {
        /// <summary>
        /// The name of the module.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The imports of the module.
        /// </summary>
        public IReadOnlyList<Import> Imports => imports;

        /// <summary>
        /// The exports of the module.
        /// </summary>
        public IReadOnlyList<Export> Exports => exports;

        /// <summary>
        /// Validates the given WebAssembly module.
        /// </summary>
        /// <param name="engine">The engine to use for validation.</param>
        /// <param name="bytes">The bytes of the WebAssembly module to validate.</param>
        /// <returns>Returns null if the module is valid or an error message if the module is invalid.</returns>
        public static string? Validate(Engine engine, ReadOnlySpan<byte> bytes)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var error = Native.wasmtime_module_validate(engine.NativeHandle, ptr, (UIntPtr)bytes.Length);
                    if (error != IntPtr.Zero)
                    {
                        return WasmtimeException.FromOwnedError(error).Message;
                    }

                    return null;
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Module"/> given the module name and bytes.
        /// </summary>
        /// <param name="engine">The engine to use for the module.</param>
        /// <param name="name">The name of the module.</param>
        /// <param name="bytes">The bytes of the module.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public static Module FromBytes(Engine engine, string name, ReadOnlySpan<byte> bytes)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var error = Native.wasmtime_module_new(engine.NativeHandle, ptr, (UIntPtr)bytes.Length, out var handle);
                    if (error != IntPtr.Zero)
                    {
                        throw new WasmtimeException($"WebAssembly module '{name}' is not valid: {WasmtimeException.FromOwnedError(error).Message}");
                    }

                    return new Module(handle, name);
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Module"/> given the path to the WebAssembly file.
        /// </summary>
        /// <param name="engine">The engine to use for the module.</param>
        /// <param name="path">The path to the WebAssembly file.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public static Module FromFile(Engine engine, string path)
        {
            return FromBytes(engine, Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(path));
        }

        /// <summary>
        /// Creates a <see cref="Module"/> from a stream.
        /// </summary>
        /// <param name="engine">The engine to use for the module.</param>
        /// <param name="name">The name of the module.</param>
        /// <param name="stream">The stream of the module data.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public static Module FromStream(Engine engine, string name, Stream stream)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return FromBytes(engine, name, ms.ToArray());
        }

        /// <summary>
        /// Creates a <see cref="Module"/> based on a WebAssembly text format representation.
        /// </summary>
        /// <param name="engine">The engine to use for the module.</param>
        /// <param name="name">The name of the module.</param>
        /// <param name="text">The WebAssembly text format representation of the module.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public static Module FromText(Engine engine, string name, string text)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (text is null)
            {
                throw new ArgumentNullException(nameof(text));
            }

            unsafe
            {
                var textBytes = Encoding.UTF8.GetBytes(text);
                fixed (byte* ptr = textBytes)
                {
                    var error = Native.wasmtime_wat2wasm(ptr, (UIntPtr)textBytes.Length, out var moduleBytes);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    using (var array = moduleBytes)
                    {
                        return FromBytes(engine, name, new ReadOnlySpan<byte>(array.data, checked((int)array.size)));
                    }
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Module"/> based on the path to a WebAssembly text format file.
        /// </summary>
        /// <param name="engine">The engine to use for the module.</param>
        /// <param name="path">The path to the WebAssembly text format file.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public static Module FromTextFile(Engine engine, string path)
        {
            return FromText(engine, Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
        }

        /// <summary>
        /// Creates a <see cref="Module"/> from a WebAssembly text format stream.
        /// </summary>
        /// <param name="engine">The engine to use for the module.</param>
        /// <param name="name">The name of the module.</param>
        /// <param name="stream">The stream of the module data.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public static Module FromTextStream(Engine engine, string name, Stream stream)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Create a `StreamReader` to read a text from the supplied stream. The minimum buffer
            // size and other parameters are hard-coded based on the default values used by the
            // `StreamReader(Stream)` constructor. Make sure to leave `stream` open by specifying
            // `leaveOpen`.
            using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
            return FromText(engine, name, reader.ReadToEnd());
        }

        /// <summary>
        /// Serializes the module to an array of bytes.
        /// </summary>
        /// <returns>Returns the serialized module as an array of bytes.</returns>
        public byte[] Serialize()
        {
            var error = Native.wasmtime_module_serialize(this.handle, out var array);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            using (array)
            {
                var len = Convert.ToInt32(array.size.ToUInt32());
                var bytes = new byte[len];
                unsafe
                {
                    Marshal.Copy((IntPtr)array.data, bytes, 0, len);
                }
                return bytes;
            }
        }

        /// <summary>
        /// Deserializes a previously serialized module from a span of bytes.
        /// </summary>
        /// <param name="engine">The engine to use to deserialize the module.</param>
        /// <param name="name">The name of the module being deserialized.</param>
        /// <param name="bytes">The previously serialized module bytes.</param>
        /// <returns>Returns the <see cref="Module" /> that was previously serialized.</returns>
        /// <remarks>The passed bytes must come from a previous call to <see cref="Module.Serialize" />.</remarks>
        public static Module Deserialize(Engine engine, string name, ReadOnlySpan<byte> bytes)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    var error = Native.wasmtime_module_deserialize(engine.NativeHandle, ptr, (UIntPtr)bytes.Length, out var handle);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    return new Module(handle, name);
                }
            }
        }

        /// <summary>
        /// Deserializes a previously serialized module from a file.
        /// </summary>
        /// <param name="engine">The engine to deserialize the module with.</param>
        /// <param name="name">The name of the deserialized module.</param>
        /// <param name="path">The path to the previously serialized module.</param>
        /// <returns>Returns the <see cref="Module" /> that was previously serialized.</returns>
        /// <remarks>The file's contents must come from a previous call to <see cref="Module.Serialize" />.</remarks>
        public static Module DeserializeFile(Engine engine, string name, string path)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            var error = Native.wasmtime_module_deserialize_file(engine.NativeHandle, path, out var handle);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return new Module(handle, name);
        }

        /// <summary>
        /// Convert WAT (Web Assembly Text) into WASM bytes
        /// </summary>
        /// <param name="wat">A string containing WAT (Web Assembly Text)</param>
        /// <returns>Returns a byte array containing the WebAssembly module represented by the given text.</returns>
        /// <exception cref="ArgumentNullException">Thrown if text is null</exception>
        /// <exception cref="WasmtimeException">Thrown if text is not valid WAT</exception>
        public static byte[] ConvertText(string wat)
        {
            if (wat is null)
            {
                throw new ArgumentNullException(nameof(wat));
            }

            unsafe
            {
                var textBytes = Encoding.UTF8.GetBytes(wat);
                fixed (byte* ptr = textBytes)
                {
                    var error = Native.wasmtime_wat2wasm(ptr, (UIntPtr)textBytes.Length, out var moduleBytes);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    using (moduleBytes)
                    {
                        var arr = new byte[(int)moduleBytes.size];

                        var src = new Span<byte>(moduleBytes.data, (int)moduleBytes.size);
                        src.CopyTo(arr);

                        return arr;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }


        internal Module(IntPtr handle, string name)
        {
            this.Name = name;
            this.handle = new Handle(handle);

            Native.wasmtime_module_imports(handle, out var imports);

            using (var _ = imports)
            {
                this.imports = imports.ToImportArray();
            }

            Native.wasmtime_module_exports(handle, out var exports);

            using (var _ = exports)
            {
                this.exports = exports.ToExportArray();
            }
        }

        internal Handle NativeHandle
        {
            get
            {
                if (handle.IsInvalid)
                {
                    throw new ObjectDisposedException(typeof(Module).FullName);
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
                Native.wasmtime_module_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_module_new(Engine.Handle engine, byte* bytes, UIntPtr size, out IntPtr handle);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_module_delete(IntPtr module);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_module_imports(IntPtr module, out ImportTypeArray imports);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_module_exports(IntPtr module, out ExportTypeArray exports);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_wat2wasm(byte* text, UIntPtr len, out ByteArray bytes);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_module_validate(Engine.Handle engine, byte* bytes, UIntPtr size);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_module_serialize(Handle module, out ByteArray bytes);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_module_deserialize(Engine.Handle engine, byte* bytes, UIntPtr size, out IntPtr handle);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_module_deserialize_file(Engine.Handle engine, [MarshalAs(UnmanagedType.LPUTF8Str)] string path, out IntPtr handle);
        }

        private readonly Handle handle;
        private readonly Import[] imports;
        private readonly Export[] exports;
    }
}
