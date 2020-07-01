using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly engine.
    /// </summary>
    public class Engine : IDisposable
    {
        /// <summary>
        /// Constructs a new default engine.
        /// </summary>
        public Engine()
        {
            _handle = Interop.wasm_engine_new();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_handle.IsInvalid)
            {
                _handle.Dispose();
                _handle.SetHandleAsInvalid();
            }
        }

        internal Engine(Interop.WasmConfigHandle config)
        {
            if (config.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime engine.");
            }

            _handle = Interop.wasm_engine_new_with_config(config);
            config.SetHandleAsInvalid();
        }

        internal Interop.EngineHandle Handle
        {
            get
            {
                if (_handle.IsInvalid)
                {
                    throw new ObjectDisposedException(typeof(Engine).FullName);
                }

                return _handle;
            }
        }

        private Interop.EngineHandle _handle;
    }
}
