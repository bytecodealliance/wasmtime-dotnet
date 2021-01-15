using System;
using System.Collections.Generic;
using Wasmtime.Exports;

namespace Wasmtime.Externs
{
    /// <summary>
    /// Represents external WebAssembly functions, globals, tables, and memories.
    /// </summary>
    internal class Externs : IDisposable
    {
        internal Externs(Wasmtime.Exports.Exports exports, IntPtr instanceHandle)
        {
            var functions = new List<ExternFunction>();
            var globals = new List<ExternGlobal>();
            var tables = new List<ExternTable>();
            var memories = new List<ExternMemory>();
            var instances = new List<ExternInstance>();
            var modules = new List<ExternModule>();

            Interop.wasm_instance_exports(instanceHandle, out _externs);

            for (int i = 0; i < (int)_externs.size; ++i)
            {
                unsafe
                {
                    var ext = _externs.data[i];

                    switch (Interop.wasm_extern_kind(ext))
                    {
                        case Interop.wasm_externkind_t.WASM_EXTERN_FUNC:
                            var function = new ExternFunction((FunctionExport)exports.All[i], Interop.wasm_extern_as_func(ext));
                            functions.Add(function);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_GLOBAL:
                            var global = new ExternGlobal((GlobalExport)exports.All[i], Interop.wasm_extern_as_global(ext));
                            globals.Add(global);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_TABLE:
                            var table = new ExternTable((TableExport)exports.All[i], Interop.wasm_extern_as_table(ext));
                            tables.Add(table);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_MEMORY:
                            var memory = new ExternMemory((MemoryExport)exports.All[i], Interop.wasm_extern_as_memory(ext));
                            memories.Add(memory);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_INSTANCE:
                            var instance = new ExternInstance((InstanceExport)exports.All[i], Interop.wasm_extern_as_instance(ext));
                            instances.Add(instance);
                            break;

                        case Interop.wasm_externkind_t.WASM_EXTERN_MODULE:
                            var module = new ExternModule((ModuleExport)exports.All[i], Interop.wasm_extern_as_module(ext));
                            modules.Add(module);
                            break;

                        default:
                            throw new NotSupportedException("Unsupported extern type.");
                    }
                }
            }

            Functions = functions;
            Globals = globals;
            Tables = tables;
            Memories = memories;
            Instances = instances;
            Modules = modules;
        }

        /// <summary>
        /// The extern functions from an instantiated WebAssembly module.
        /// </summary>
        public IReadOnlyList<ExternFunction> Functions { get; private set; }

        /// <summary>
        /// The extern globals from an instantiated WebAssembly module.
        /// </summary>
        public IReadOnlyList<ExternGlobal> Globals { get; private set; }

        /// <summary>
        /// The extern tables from an instantiated WebAssembly module.
        /// </summary>
        public IReadOnlyList<ExternTable> Tables { get; private set; }

        /// <summary>
        /// The extern memories from an instantiated WebAssembly module.
        /// </summary>
        public IReadOnlyList<ExternMemory> Memories { get; private set; }

        /// <summary>
        /// The extern instances from an instantiated WebAssembly module.
        /// </summary>
        public IReadOnlyList<ExternInstance> Instances { get; private set; }

        /// <summary>
        /// The extern modules from an instantiated WebAssembly module.
        /// </summary>
        public IReadOnlyList<ExternModule> Modules { get; private set; }

        /// <inheritdoc/>
        public unsafe void Dispose()
        {
            foreach (var instance in Instances)
            {
                instance.Dispose();
            }

            if (!(_externs.data is null))
            {
                Interop.wasm_extern_vec_delete(ref _externs);
                _externs.data = null;
            }
        }

        private Interop.wasm_extern_vec_t _externs;
    }
}
