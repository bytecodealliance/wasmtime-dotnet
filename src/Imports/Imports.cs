using System;
using System.Collections.Generic;

namespace Wasmtime.Imports
{
    /// <summary>
    /// Represents imported functions, globals, tables, and memories to a WebAssembly module.
    /// </summary>
    public class Imports
    {
        internal Imports(Interop.wasm_importtype_vec_t imports)
        {
            var all = new List<Import>((int)imports.size);
            var functions = new List<FunctionImport>();
            var globals = new List<GlobalImport>();
            var tables = new List<TableImport>();
            var memories = new List<MemoryImport>();
            var instances = new List<InstanceImport>();
            var modules = new List<ModuleImport>();

            for (int i = 0; i < (int)imports.size; ++i)
            {
                unsafe
                {
                    var importType = imports.data[i];
                    var externType = Interop.wasm_importtype_type(importType);

                    switch (Interop.wasm_externtype_kind(externType))
                    {
                        case Interop.wasm_externkind_t.WASM_EXTERN_FUNC:
                            var function = new FunctionImport(importType, externType);
                            functions.Add(function);
                            all.Add(function);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_GLOBAL:
                            var global = new GlobalImport(importType, externType);
                            globals.Add(global);
                            all.Add(global);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_TABLE:
                            var table = new TableImport(importType, externType);
                            tables.Add(table);
                            all.Add(table);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_MEMORY:
                            var memory = new MemoryImport(importType, externType);
                            memories.Add(memory);
                            all.Add(memory);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_INSTANCE:
                            var instance = new InstanceImport(importType, externType);
                            instances.Add(instance);
                            all.Add(instance);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_MODULE:
                            var module = new ModuleImport(importType, externType);
                            modules.Add(module);
                            all.Add(module);
                            break;

                        default:
                            throw new NotSupportedException("Unsupported import extern type.");
                    }
                }
            }

            Functions = functions;
            Globals = globals;
            Tables = tables;
            Memories = memories;
            Instances = instances;
            Modules = modules;
            All = all;
        }

        /// <summary>
        /// The imported functions required by a WebAssembly module.
        /// </summary>
        public IReadOnlyList<FunctionImport> Functions { get; private set; }

        /// <summary>
        /// The imported globals required by a WebAssembly module.
        /// </summary>
        public IReadOnlyList<GlobalImport> Globals { get; private set; }

        /// <summary>
        /// The imported tables required by a WebAssembly module.
        /// </summary>
        public IReadOnlyList<TableImport> Tables { get; private set; }

        /// <summary>
        /// The imported memories required by a WebAssembly module.
        /// </summary>
        public IReadOnlyList<MemoryImport> Memories { get; private set; }

        /// <summary>
        /// The imported instances required by a WebAssembly module.
        /// </summary>
        public IReadOnlyList<InstanceImport> Instances { get; private set; }

        /// <summary>
        /// The imported modules required by a WebAssembly module.
        /// </summary>
        public IReadOnlyList<ModuleImport> Modules { get; private set; }

        internal IReadOnlyList<Import> All { get; private set; }
    }
}
