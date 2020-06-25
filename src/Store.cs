using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly store.
    /// </summary>
    /// <remarks>
    /// A store is used for loading WebAssembly modules.
    /// </remarks>
    public class Store : IDisposable
    {
        /// <summary>
        /// Constructs a new store.
        /// </summary>
        public Store()
        {
            Initialize(Interop.wasm_engine_new());
        }

        /// <summary>
        /// Loads a <see cref="Module"/> given the module name and bytes.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="bytes">The bytes of the module.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public Module LoadModule(string name, byte[] bytes)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            return new Module(Handle, name, bytes);
        }

        /// <summary>
        /// Loads a <see cref="Module"/> given the path to the WebAssembly file.
        /// </summary>
        /// <param name="path">The path to the WebAssembly file.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public Module LoadModule(string path)
        {
            return LoadModule(Path.GetFileNameWithoutExtension(path), File.ReadAllBytes(path));
        }

        /// <summary>
        /// Loads a <see cref="Module"/> given a stream.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="stream">The stream of the module data.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public Module LoadModule(string name, Stream stream)
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
            return LoadModule(name, ms.ToArray());
        }

        /// <summary>
        /// Loads a <see cref="Module"/> based on a WebAssembly text format representation.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="text">The WebAssembly text format representation of the module.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public Module LoadModuleText(string name, string text)
        {
            CheckDisposed();

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
                fixed (byte *ptr = textBytes)
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
                    return LoadModule(name, moduleBytes);
                }
            }
        }

        /// <summary>
        /// Loads a <see cref="Module"/> based on the path to a WebAssembly text format file.
        /// </summary>
        /// <param name="path">The path to the WebAssembly text format file.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public Module LoadModuleText(string path)
        {
            return LoadModuleText(Path.GetFileNameWithoutExtension(path), File.ReadAllText(path));
        }

        /// <summary>
        /// Loads a <see cref="Module"/> given stream as WebAssembly text format stream.
        /// </summary>
        /// <param name="name">The name of the module.</param>
        /// <param name="stream">The stream of the module data.</param>
        /// <returns>Returns a new <see cref="Module"/>.</returns>
        public Module LoadModuleText(string name, Stream stream)
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
            return LoadModuleText(name, reader.ReadToEnd());
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!Handle.IsInvalid)
            {
                Handle.Dispose();
                Handle.SetHandleAsInvalid();
            }

            if (!_engine.IsInvalid)
            {
                _engine.Dispose();
                _engine.SetHandleAsInvalid();
            }
        }

        internal Store(Interop.WasmConfigHandle config)
        {
            var engine = Interop.wasm_engine_new_with_config(config);
            config.SetHandleAsInvalid();

            Initialize(engine);
        }

        private void Initialize(Interop.EngineHandle engine)
        {
            if (engine.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime engine.");
            }

            var store = Interop.wasm_store_new(engine);
            if (store.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime store.");
            }

            _engine = engine;
            _handle = store;
        }

        internal Interop.StoreHandle Handle
        {
            get
            {
                CheckDisposed();
                return _handle;
            }
        }

        private void CheckDisposed()
        {
            if (_handle.IsInvalid)
            {
                throw new ObjectDisposedException(typeof(Store).FullName);
            }
        }

        private Interop.EngineHandle _engine;
        private Interop.StoreHandle _handle;
    }
}
