using System;
using System.Diagnostics;

namespace Wasmtime.Imports
{
    /// <summary>
    /// Represents an instance imported to a WebAssembly module.
    /// </summary>
    public class InstanceImport : Import
    {
        internal InstanceImport(IntPtr importType, IntPtr externType) : base(importType)
        {
            Debug.Assert(Interop.wasm_externtype_kind(externType) == Interop.wasm_externkind_t.WASM_EXTERN_INSTANCE);

            var instanceType = Interop.wasm_externtype_as_instancetype_const(externType);

            Interop.wasm_exporttype_vec_t exports;
            Interop.wasm_instancetype_exports(instanceType, out exports);

            try
            {
                Exports = new Exports.Exports(exports);
            }
            finally
            {
                Interop.wasm_exporttype_vec_delete(ref exports);
            }
        }

        /// <summary>
        /// The exports of the instance.
        /// </summary>
        public Exports.Exports Exports { get; private set; }
    }
}
