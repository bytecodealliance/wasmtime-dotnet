using System;
using System.Diagnostics;

namespace Wasmtime.Exports
{
    /// <summary>
    /// Represents a module exported from a WebAssembly module or instance.
    /// </summary>
    public class ModuleExport : Export
    {
        internal ModuleExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            Debug.Assert(Interop.wasm_externtype_kind(externType) == Interop.wasm_externkind_t.WASM_EXTERN_MODULE);

            var moduleType = Interop.wasm_externtype_as_moduletype_const(externType);

            Interop.wasm_importtype_vec_t imports;
            Interop.wasm_moduletype_imports(moduleType, out imports);

            try
            {
                Imports = new Imports.Imports(imports);
            }
            finally
            {
                Interop.wasm_importtype_vec_delete(ref imports);
            }

            Interop.wasm_exporttype_vec_t exports;
            Interop.wasm_moduletype_exports(moduleType, out exports);

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
        /// The imports of the module.
        /// </summary>
        public Imports.Imports Imports { get; private set; }

        /// <summary>
        /// The exports of the module.
        /// </summary>
        public Exports Exports { get; private set; }
    }
}
