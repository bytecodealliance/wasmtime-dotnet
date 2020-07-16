using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly store.
    /// </summary>
    public class Store : IDisposable
    {
        /// <summary>
        /// Constructs a new store.
        /// </summary>
        /// <param name="engine">The engine to use for the store.</param>
        public Store(Engine engine)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            var store = Interop.wasm_store_new(engine.Handle);
            if (store.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime store.");
            }

            _engine = engine;
            _handle = store;
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

        private Engine _engine;
        private Interop.StoreHandle _handle;
    }
}
