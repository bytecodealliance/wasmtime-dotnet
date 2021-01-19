using System;
using Wasmtime.Exports;

namespace Wasmtime.Externs
{
    /// <summary>
    /// Represents an external WebAssembly module.
    /// </summary>
    public class ExternModule : IImportable
    {
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
        /// Instantiates the module given a set of imports.
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

                var error = Interop.wasmtime_instance_new(store.Handle, _module, ref importsVec, out var instance, out var trap);

                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }
                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }

                return new Instance(instance, _module);
            }
        }

        internal ExternModule(ModuleExport export, IntPtr module)
        {
            _export = export;
            _module = module;

            Interop.wasm_importtype_vec_t imports;
            Interop.wasm_module_imports(_module, out imports);

            try
            {
                Imports = new Wasmtime.Imports.Imports(imports);
            }
            finally
            {
                Interop.wasm_importtype_vec_delete(ref imports);
            }

            Interop.wasm_exporttype_vec_t exports;
            Interop.wasm_module_exports(_module, out exports);

            try
            {
                Exports = new Wasmtime.Exports.Exports(exports);
            }
            finally
            {
                Interop.wasm_exporttype_vec_delete(ref exports);
            }
        }

        IntPtr IImportable.GetHandle()
        {
            return Interop.wasm_module_as_extern(_module);
        }

        /// <summary>
        /// The name of the WebAssembly module.
        /// </summary>
        public string Name => _export.Name;

        private ModuleExport _export;
        private IntPtr _module;
    }
}
