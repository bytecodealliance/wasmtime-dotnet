using System;
using Wasmtime.Exports;

namespace Wasmtime.Externs
{
    /// <summary>
    /// Represents an external WebAssembly memory.
    /// </summary>
    public class ExternMemory : MemoryBase, IImportable
    {
        internal ExternMemory(MemoryExport export, IntPtr memory)
        {
            _export = export;
            _memory = memory;
        }

        IntPtr IImportable.GetHandle()
        {
            return Interop.wasm_memory_as_extern(_memory);
        }

        /// <summary>
        /// The name of the WebAssembly memory.
        /// </summary>
        public string Name => _export.Name;

        /// <summary>
        /// The minimum memory size (in WebAssembly page units).
        /// </summary>
        public uint Minimum => _export.Minimum;

        /// <summary>
        /// The maximum memory size (in WebAssembly page units).
        /// </summary>
        public uint Maximum => _export.Maximum;

        protected override IntPtr MemoryHandle => _memory;

        private MemoryExport _export;
        private IntPtr _memory;
    }
}
