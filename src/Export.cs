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
            var exports = new Export[(int)this.size];
            for (int i = 0; i < (int)this.size; ++i)
            {
                unsafe
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

                        case WasmExternKind.Instance:
                            exports[i] = new InstanceExport(exportType, externType);
                            break;

                        case WasmExternKind.Module:
                            exports[i] = new ModuleExport(exportType, externType);
                            break;

                        default:
                            throw new NotSupportedException("Unsupported export extern type.");
                    }
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
                if (name->size == UIntPtr.Zero)
                {
                    Name = String.Empty;
                }
                else
                {
                    Name = Marshal.PtrToStringUTF8((IntPtr)name->data, (int)name->size);
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

            Mutability = (Mutability)Global.Native.wasm_globaltype_mutability(type);
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

            unsafe
            {
                var limits = Memory.Native.wasm_memorytype_limits(type);
                Minimum = limits->min;
                Maximum = limits->max;
            }
        }

        /// <summary>
        /// The minimum memory size (in WebAssembly page units).
        /// </summary>
        public uint Minimum { get; private set; }

        /// <summary>
        /// The maximum memory size (in WebAssembly page units).
        /// </summary>
        public uint Maximum { get; private set; }

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

    /// <summary>
    /// Represents an instance exported from a WebAssembly module or instance.
    /// </summary>
    public class InstanceExport : Export
    {
        internal InstanceExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            var type = Native.wasmtime_externtype_as_instancetype(externType);
            if (type == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            Native.wasmtime_instancetype_exports(type, out var exports);

            using (var _ = exports)
            {
                this.exports = exports.ToExportArray();
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_externtype_as_instancetype(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_instancetype_exports(IntPtr type, out ExportTypeArray exports);
        }

        /// <summary>
        /// The exports of the instance.
        /// </summary>
        public IReadOnlyList<Export> Exports => exports;

        private readonly Export[] exports;
    }

    /// <summary>
    /// Represents a module exported from a WebAssembly module or instance.
    /// </summary>
    public class ModuleExport : Export
    {
        internal ModuleExport(IntPtr exportType, IntPtr externType) : base(exportType)
        {
            var type = Native.wasmtime_externtype_as_moduletype(externType);
            if (type == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            Module.Native.wasmtime_moduletype_imports(type, out var imports);

            using (var _ = imports)
            {
                this.imports = imports.ToImportArray();
            }

            Module.Native.wasmtime_moduletype_exports(type, out var exports);

            using (var _ = exports)
            {
                this.exports = exports.ToExportArray();
            }
        }

        /// <summary>
        /// The imports of the module.
        /// </summary>
        public IReadOnlyList<Import> Imports => imports;

        /// <summary>
        /// The exports of the module.
        /// </summary>
        public IReadOnlyList<Export> Exports => exports;

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_externtype_as_moduletype(IntPtr type);
        }

        private readonly Import[] imports;
        private readonly Export[] exports;
    }
}
