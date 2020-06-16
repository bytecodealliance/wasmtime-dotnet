using System;
using Wasmtime.Exports;

namespace Wasmtime.Externs
{
    /// <summary>
    /// Represents an external (instantiated) WebAssembly table.
    /// </summary>
    public class ExternTable
    {
        internal ExternTable(TableExport export, IntPtr table)
        {
            _export = export;
            _table = table;
        }

        /// <summary>
        /// The name of the WebAssembly memory.
        /// </summary>
        public string Name => _export.Name;

        /// <summary>
        /// The value kind of the table.
        /// </summary>
        public ValueKind Kind => _export.Kind;

        /// <summary>
        /// The minimum number of elements in the table.
        /// </summary>
        public uint Minimum => _export.Minimum;

        /// <summary>
        /// The maximum number of elements in the table.
        /// </summary>
        public uint Maximum => _export.Maximum;

        private TableExport _export;
        private IntPtr _table;
    }
}
