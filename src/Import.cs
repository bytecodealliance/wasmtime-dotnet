using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wasmtime
{
    // NOTE: this differs from `Wasmtime.ExternKind` for now, but this will likely be fixed
    // in the Wasmtime API soon. The difference is the order of `Module` and `Instance`.
    internal enum WasmExternKind : byte
    {
        Func = 0,
        Global = 1,
        Table = 2,
        Memory = 3,
        Tag = 4,
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
            if (size == UIntPtr.Zero)
            {
                return Array.Empty<Import>();
            }

            var imports = new Import[(int)this.size];
            for (var i = 0; i < (int)this.size; ++i)
            {
                var importType = this.data[i];
                var externType = Native.wasm_importtype_type(importType);

                var kind = (WasmExternKind)ExportTypeArray.Native.wasm_externtype_kind(externType);
                imports[i] = kind switch
                {
                    WasmExternKind.Func   => new FunctionImport(importType, externType),
                    WasmExternKind.Global => new GlobalImport(importType, externType),
                    WasmExternKind.Table  => new TableImport(importType, externType),
                    WasmExternKind.Memory => new MemoryImport(importType, externType),
                    WasmExternKind.Tag    => new TagImport(importType, externType),
                    _ => throw new NotSupportedException($"Unsupported import extern type: {kind}.")
                };
            }
            return imports;
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasm_importtype_vec_delete(in ImportTypeArray vec);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_importtype_type(IntPtr importType);
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
                ModuleName = moduleName->size == 0
                           ? string.Empty
                           : Extensions.PtrToStringUTF8((IntPtr)moduleName->data, checked((int)moduleName->size));

                var name = Native.wasm_importtype_name(importType);
                Name = name is null || name->size == 0
                     ? string.Empty
                     : Extensions.PtrToStringUTF8((IntPtr)name->data, checked((int)name->size));
            }
        }

        /// <summary>
        /// The module name of the import.
        /// </summary>
        public string ModuleName { get; }

        /// <summary>
        /// The name of the import.
        /// </summary>
        public string Name { get; }

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

                Parameters = (*Function.Native.wasm_functype_params(type)).ToArray();
                Results = (*Function.Native.wasm_functype_results(type)).ToArray();
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

    /// <summary>
    /// Represents a tag imported to a WebAssembly module or instance.
    /// </summary>
    public class TagImport
        : Import
    {
        /// <summary>
        /// Parameter types of this tag
        /// </summary>
        public ValueKind[] Parameters { get; set; }

        internal TagImport(IntPtr exportType, IntPtr externType)
            : base(exportType)
        {
            var tagType = TagExport.Native.wasm_externtype_as_tagtype_const(externType);
            if (tagType == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            var funcType = TagExport.Native.wasm_tagtype_functype(tagType);
            if (funcType == IntPtr.Zero)
            {
                throw new InvalidOperationException();
            }

            unsafe
            {
                Parameters = (*Function.Native.wasm_functype_params(funcType)).ToArray();
            }
        }
    }
}
