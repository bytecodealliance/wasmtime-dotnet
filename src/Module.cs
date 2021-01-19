using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Implemented by types that can be imported by WebAssembly modules.
    /// </summary>
    public interface IImportable
    {
        /// <summary>
        /// Gets the extern handle for the importable.
        /// </summary>
        /// <remarks>
        /// This interface is internal to Wasmtime and not intended to be implemented by users.
        /// </remarks>
        /// <returns>Returns the extern handle for the importable.</returns>
        IntPtr GetHandle();
    }

    /// <summary>
    /// Represents a WebAssembly module.
    /// </summary>
    public class Module : IDisposable, IImportable
    {
        /// <summary>
        /// The name of the module.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The imports of the module.
        /// </summary>
        public Wasmtime.Imports.Imports Imports { get; private set; }

        /// <summary>
        /// The exports of the module.
        /// </summary>
        /// <value></value>
        public Wasmtime.Exports.Exports Exports { get; private set; }

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

            return new Module(engine.Handle, name, bytes);
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

            var textBytes = Encoding.UTF8.GetBytes(text);
            unsafe
            {
                fixed (byte* ptr = textBytes)
                {
                    Interop.wasm_byte_vec_t textVec;
                    textVec.size = (UIntPtr)textBytes.Length;
                    textVec.data = ptr;

                    var error = Interop.wasmtime_wat2wasm(ref textVec, out var bytes);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    var byteSpan = new ReadOnlySpan<byte>(bytes.data, checked((int)bytes.size));
                    var moduleBytes = byteSpan.ToArray();
                    Interop.wasm_byte_vec_delete(ref bytes);
                    return FromBytes(engine, name, moduleBytes);
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
        /// Instantiates a <see cref="Module"/> given a set of imports.
        /// </summary>
        /// <param name="store">The store to associate with the instance.</param>
        /// <param name="imports">The imports to use for the instantiations.</param>
        /// <returns>Returns a new <see cref="Instance"/>.</returns>
        public Instance Instantiate(Store store, params IImportable[] imports)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (imports is null)
            {
                throw new ArgumentNullException(nameof(imports));
            }

            unsafe
            {
                IntPtr* handles = stackalloc IntPtr[imports.Length];

                for (int i = 0; i < imports.Length; ++i)
                {
                    unsafe
                    {
                        handles[i] = imports[i].GetHandle();
                    }
                }

                Interop.wasm_extern_vec_t importsVec = new Interop.wasm_extern_vec_t() { size = (UIntPtr)imports.Length, data = handles };

                var error = Interop.wasmtime_instance_new(store.Handle, Handle.DangerousGetHandle(), ref importsVec, out var instance, out var trap);

                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }
                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }

                return new Instance(instance, Handle.DangerousGetHandle());
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!Handle.IsInvalid)
            {
                Handle.Dispose();
                Handle.SetHandleAsInvalid();
            }
        }

        IntPtr IImportable.GetHandle()
        {
            return Interop.wasm_module_as_extern(Handle.DangerousGetHandle());
        }

        internal Module(Interop.EngineHandle engine, string name, ReadOnlySpan<byte> bytes)
        {
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    Interop.wasm_byte_vec_t vec;
                    vec.size = (UIntPtr)bytes.Length;
                    vec.data = ptr;

                    var error = Interop.wasmtime_module_new(engine, ref vec, out var handle);
                    if (error != IntPtr.Zero)
                    {
                        throw new WasmtimeException($"WebAssembly module '{name}' is not valid: {WasmtimeException.FromOwnedError(error).Message}");
                    }

                    Handle = handle;
                }
            }

            Name = name;

            Interop.wasm_importtype_vec_t imports;
            Interop.wasm_module_imports(Handle.DangerousGetHandle(), out imports);

            try
            {
                Imports = new Wasmtime.Imports.Imports(imports);
            }
            finally
            {
                Interop.wasm_importtype_vec_delete(ref imports);
            }

            Interop.wasm_exporttype_vec_t exports;
            Interop.wasm_module_exports(Handle.DangerousGetHandle(), out exports);

            try
            {
                Exports = new Wasmtime.Exports.Exports(exports);
            }
            finally
            {
                Interop.wasm_exporttype_vec_delete(ref exports);
            }
        }

        internal Interop.ModuleHandle Handle { get; private set; }
    }
}
