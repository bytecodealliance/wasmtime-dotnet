using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ExportTypeArray : IDisposable
    {
        public UIntPtr size;
        public IntPtr* data;

        public void Dispose()
        {
            Native.wasm_exporttype_vec_delete(this);
        }

        public Export[] ToExportArray()
        {
            if (size == UIntPtr.Zero)
            {
                return Array.Empty<Export>();
            }

            var exports = new Export[(int)this.size];
            for (int i = 0; i < (int)this.size; ++i)
            {
                var exportType = this.data[i];
                var externType = Native.wasm_exporttype_type(exportType);

                switch ((WasmExternKind)Native.wasm_externtype_kind(externType))
                {
                    case WasmExternKind.Func:
                        exports[i] = new FunctionExport(exportType, externType);
                        break;

                    case WasmExternKind.Global:
                        exports[i] = new GlobalExport(exportType, externType);
                        break;

                    case WasmExternKind.Table:
                        exports[i] = new TableExport(exportType, externType);
                        break;

                    case WasmExternKind.Memory:
                        exports[i] = new MemoryExport(exportType, externType);
                        break;

                    default:
                        throw new NotSupportedException("Unsupported export extern type.");
                }
            }

            return exports;
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasm_exporttype_vec_delete(in ExportTypeArray vec);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern byte wasm_externtype_kind(IntPtr valueType);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_exporttype_type(IntPtr exportType);
        }
    }

    /// <summary>
    /// Represents an export of a WebAssembly module or instance.
    /// </summary>
    public abstract class Export
    {
        internal Export(IntPtr exportType)
        {
            unsafe
            {
                var name = Native.wasm_exporttype_name(exportType);
                if (name->size == 0)
                {
                    Name = String.Empty;
                }
                else
                {
                    Name = Marshal.PtrToStringUTF8((IntPtr)name->data, checked((int)name->size));
                }
            }
        }

        /// <summary>
        /// The name of the export.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return Name;
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static unsafe extern ByteArray* wasm_exporttype_name(IntPtr type);
        }
    }

    /// <summary>
    /// Represents a function exported from a WebAssembly module or instance.
    /// </summary>
    public class FunctionExport : Export
    {
        internal FunctionExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            unsafe
            {
                var type = Native.wasm_externtype_as_functype_const(externType);
                if (type == IntPtr.Zero)
                {
                    throw new InvalidOperationException();
                }

                Parameters = (*Function.Native.wasm_functype_params(type)).ToList();
                Results = (*Function.Native.wasm_functype_results(type)).ToList();
            }
        }

        /// <summary>
        /// The parameter of the exported WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Parameters { get; private set; }

        /// <summary>
        /// The results of the exported WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Results { get; private set; }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_externtype_as_functype_const(IntPtr type);
        }
    }

    /// <summary>
    /// Represents a global variable exported from a WebAssembly module or instance.
    /// </summary>
    public class GlobalExport : Export
    {
        internal GlobalExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            var type = Native.wasm_externtype_as_globaltype_const(externType);
            if (type == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            Kind = ValueType.ToKind(Global.Native.wasm_globaltype_content(type));

            Mutability = new Mutability(Global.Native.wasm_globaltype_mutability(type));
        }

        /// <summary>
        /// The kind of value for the global variable.
        /// </summary>
        public ValueKind Kind { get; private set; }

        /// <summary>
        /// Gets the mutability of the global.
        /// </summary>
        public Mutability Mutability { get; private set; }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_externtype_as_globaltype_const(IntPtr type);
        }
    }

    /// <summary>
    /// Represents a memory exported from a WebAssembly module or instance.
    /// </summary>
    public class MemoryExport : Export
    {
        internal MemoryExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            var type = Native.wasm_externtype_as_memorytype_const(externType);
            if (type == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            Minimum = (long)Memory.Native.wasmtime_memorytype_minimum(type);

            if (Memory.Native.wasmtime_memorytype_maximum(type, out ulong max))
            {
                Maximum = (long)max;
            }

            Is64Bit = Memory.Native.wasmtime_memorytype_is64(type);
        }

        /// <summary>
        /// Gets the minimum memory size (in WebAssembly page units).
        /// </summary>
        /// <value>The minimum memory size (in WebAssembly page units).</value>
        public long Minimum { get; }

        /// <summary>
        /// Gets the maximum memory size (in WebAssembly page units).
        /// </summary>
        /// <value>The maximum memory size (in WebAssembly page units), or <c>null</c> if no maximum is specified.</value>
        public long? Maximum { get; }

        /// <summary>
        /// Gets a value that indicates whether this type of memory represents a 64-bit memory.
        /// </summary>
        /// <value><c>true</c> if this type of memory represents a 64-bit memory, <c>false</c> if it represents a 32-bit memory.</value>
        public bool Is64Bit { get; }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_externtype_as_memorytype_const(IntPtr type);
        }
    }

    /// <summary>
    /// Represents a table exported from a WebAssembly module or instance.
    /// </summary>
    public class TableExport : Export
    {
        internal TableExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            var type = Native.wasm_externtype_as_tabletype_const(externType);
            if (type == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            Kind = ValueType.ToKind(Table.Native.wasm_tabletype_element(type));

            unsafe
            {
                var limits = Table.Native.wasm_tabletype_limits(type);
                Minimum = limits->min;
                Maximum = limits->max;
            }
        }

        /// <summary>
        /// The value kind of the table.
        /// </summary>
        public ValueKind Kind { get; private set; }

        /// <summary>
        /// The minimum number of elements in the table.
        /// </summary>
        public uint Minimum { get; private set; }

        /// <summary>
        /// The maximum number of elements in the table.
        /// </summary>
        public uint Maximum { get; private set; }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_externtype_as_tabletype_const(IntPtr type);
        }
    }
}
