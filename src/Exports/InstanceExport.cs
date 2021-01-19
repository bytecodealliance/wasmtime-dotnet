using System;
using System.Diagnostics;

namespace Wasmtime.Exports
{
    /// <summary>
    /// Represents an instance exported from a WebAssembly module or instance.
    /// </summary>
    public class InstanceExport : Export
    {
        internal InstanceExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            Debug.Assert(Interop.wasm_externtype_kind(externType) == Interop.wasm_externkind_t.WASM_EXTERN_INSTANCE);

            var instanceType = Interop.wasm_externtype_as_instancetype_const(externType);

            Interop.wasm_exporttype_vec_t exports;
            Interop.wasm_instancetype_exports(instanceType, out exports);

            try
            {
                Exports = new Exports(exports);
            }
            finally
            {
                Interop.wasm_exporttype_vec_delete(ref exports);
            }
        }

        /// <summary>
        /// The exports of the instance.
        /// </summary>
        public Exports Exports { get; private set; }
    }
}
