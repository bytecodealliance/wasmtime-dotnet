using System;
using System.Collections.Generic;

namespace Wasmtime.Exports
{
    /// <summary>
    /// Represents the exports of a WebAssembly module or instance.
    /// </summary>
    public class Exports
    {
        internal Exports(Interop.wasm_exporttype_vec_t exports)
        {
            var all = new List<Export>((int)exports.size);
            var functions = new List<FunctionExport>();
            var globals = new List<GlobalExport>();
            var tables = new List<TableExport>();
            var memories = new List<MemoryExport>();
            var instances = new List<InstanceExport>();
            var modules = new List<ModuleExport>();

            for (int i = 0; i < (int)exports.size; ++i)
            {
                unsafe
                {
                    var exportType = exports.data[i];
                    var externType = Interop.wasm_exporttype_type(exportType);

                    switch (Interop.wasm_externtype_kind(externType))
                    {
                        case Interop.wasm_externkind_t.WASM_EXTERN_FUNC:
                            var function = new FunctionExport(exportType, externType);
                            functions.Add(function);
                            all.Add(function);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_GLOBAL:
                            var global = new GlobalExport(exportType, externType);
                            globals.Add(global);
                            all.Add(global);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_TABLE:
                            var table = new TableExport(exportType, externType);
                            tables.Add(table);
                            all.Add(table);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_MEMORY:
                            var memory = new MemoryExport(exportType, externType);
                            memories.Add(memory);
                            all.Add(memory);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_INSTANCE:
                            var instance = new InstanceExport(exportType, externType);
                            instances.Add(instance);
                            all.Add(instance);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_MODULE:
                            var module = new ModuleExport(exportType, externType);
                            modules.Add(module);
                            all.Add(module);
                            break;

                        default:
                            throw new NotSupportedException("Unsupported export extern type.");
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
        /// The exported functions of a WebAssembly module or instance.
        /// </summary>
        public IReadOnlyList<FunctionExport> Functions { get; private set; }

        /// <summary>
        /// The exported globals of a WebAssembly module or instance.
        /// </summary>
        public IReadOnlyList<GlobalExport> Globals { get; private set; }

        /// <summary>
        /// The exported tables of a WebAssembly module or instance.
        /// </summary>
        public IReadOnlyList<TableExport> Tables { get; private set; }

        /// <summary>
        /// The exported memories of a WebAssembly module or instance.
        /// </summary>
        public IReadOnlyList<MemoryExport> Memories { get; private set; }

        /// <summary>
        /// The exported instances of a WebAssembly module or instance.
        /// </summary>
        public IReadOnlyList<InstanceExport> Instances { get; private set; }

        /// <summary>
        /// The exported modules of a WebAssembly module or instance.
        /// </summary>
        public IReadOnlyList<ModuleExport> Modules { get; private set; }

        internal List<Export> All { get; private set; }
    }
}
