using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    // NOTE: this differs from `Wasmtime.ExternKind` for now, but this will likely be fixed
    // in the Wasmtime API soon. The difference is the order of `Module` and `Instance`.
    internal enum WasmExternKind : byte
    {
        Func,
        Global,
        Table,
        Memory,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ImportTypeArray : IDisposable
    {
        public UIntPtr size;
        public IntPtr* data;

        public void Dispose()
        {
            if (data != null)
            {
                Native.wasm_importtype_vec_delete(this);
            }
        }

        public Import[] ToImportArray()
        {
            var imports = new Import[(int)this.size];
            for (int i = 0; i < (int)this.size; ++i)
            {
                unsafe
                {
                    var importType = this.data[i];
                    var externType = Native.wasm_importtype_type(importType);

                    switch ((WasmExternKind)ExportTypeArray.Native.wasm_externtype_kind(externType))
                    {
                        case WasmExternKind.Func:
                            imports[i] = new FunctionImport(importType, externType);
                            break;

                        case WasmExternKind.Global:
                            imports[i] = new GlobalImport(importType, externType);
                            break;

                        case WasmExternKind.Table:
                            imports[i] = new TableImport(importType, externType);
                            break;

                        case WasmExternKind.Memory:
                            imports[i] = new MemoryImport(importType, externType);
                            break;

                        default:
                            throw new NotSupportedException("Unsupported import extern type.");
                    }
                }
            }
            return imports;
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasm_importtype_vec_delete(in ImportTypeArray vec);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasm_importtype_type(IntPtr importType);
        }
    }
    /// <summary>
    /// The base class for import types.
    /// </summary>
    public abstract class Import
    {
        internal Import(IntPtr importType)
        {
            unsafe
            {
                var moduleName = Native.wasm_importtype_module(importType);
                if (moduleName->size == UIntPtr.Zero)
                {
                    ModuleName = String.Empty;
                }
                else
                {
                    ModuleName = Marshal.PtrToStringUTF8((IntPtr)moduleName->data, (int)moduleName->size);
                }

                var name = Native.wasm_importtype_name(importType);
                if (name is null || name->size == UIntPtr.Zero)
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
        /// The module name of the import.
        /// </summary>
        public string ModuleName { get; private set; }

        /// <summary>
        /// The name of the import.
        /// </summary>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{ModuleName}{(string.IsNullOrEmpty(ModuleName) ? "" : ".")}{Name}";
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static unsafe extern ByteArray* wasm_importtype_module(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern ByteArray* wasm_importtype_name(IntPtr type);
        }
    }

    /// <summary>
    /// Represents a function imported to a WebAssembly module.
    /// </summary>
    public class FunctionImport : Import
    {
        internal FunctionImport(IntPtr importType, IntPtr externType) : base(importType)
        {
            unsafe
            {
                var type = FunctionExport.Native.wasm_externtype_as_functype_const(externType);
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
    }

    /// <summary>
    /// Represents a global variable imported to a WebAssembly module.
    /// </summary>
    public class GlobalImport : Import
    {
        internal GlobalImport(IntPtr importType, IntPtr externType) : base(importType)
        {
            var type = GlobalExport.Native.wasm_externtype_as_globaltype_const(externType);
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
    }

    /// <summary>
    /// Represents a memory imported to a WebAssembly module.
    /// </summary>
    public class MemoryImport : Import
    {
        internal MemoryImport(IntPtr importType, IntPtr externType) : base(importType)
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
    /// Represents a table imported to a WebAssembly module.
    /// </summary>
    public class TableImport : Import
    {
        internal TableImport(IntPtr importType, IntPtr externType) : base(importType)
        {
            var type = TableExport.Native.wasm_externtype_as_tabletype_const(externType);
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
    }
}
