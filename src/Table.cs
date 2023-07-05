using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents the possible kinds of WebAssembly values stored in a table
    /// </summary>
    public enum TableKind
    {
        /// <summary>
        /// The value is a function reference.
        /// </summary>
        FuncRef = ValueKind.FuncRef,

        /// <summary>
        /// The value is an external reference.
        /// </summary>
        ExternRef = ValueKind.ExternRef,
    }

    /// <summary>
    /// Represents a WebAssembly table.
    /// </summary>
    public class Table : IExternal
    {
        /// <summary>
        /// Creates a new WebAssembly table.
        /// </summary>
        /// <param name="store">The store to create the table in.</param>
        /// <param name="kind">The value kind for the elements in the table.</param>
        /// <param name="initialValue">The initial value for elements in the table.</param>
        /// <param name="initial">The number of initial elements in the table.</param>
        /// <param name="maximum">The maximum number of elements in the table.</param>
        [Obsolete("Replace ValueKind parameter with TableKind")]
        public Table(Store store, ValueKind kind, object? initialValue, uint initial, uint maximum = uint.MaxValue)
            : this(store, (TableKind)kind, initialValue, initial, maximum)
        {
        }

        /// <summary>
        /// Creates a new WebAssembly table.
        /// </summary>
        /// <param name="store">The store to create the table in.</param>
        /// <param name="kind">The value kind for the elements in the table.</param>
        /// <param name="initialValue">The initial value for elements in the table.</param>
        /// <param name="initial">The number of initial elements in the table.</param>
        /// <param name="maximum">The maximum number of elements in the table.</param>
        public Table(Store store, TableKind kind, object? initialValue, uint initial, uint maximum = uint.MaxValue)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (kind != TableKind.ExternRef && kind != TableKind.FuncRef)
            {
                throw new WasmtimeException($"Table elements must be externref or funcref.");
            }

            if (maximum < initial)
            {
                throw new ArgumentException("The maximum number of elements cannot be less than the minimum.", nameof(maximum));
            }

            this.store = store;
            Kind = kind;
            Minimum = initial;
            Maximum = maximum;

            var limits = new Native.Limits();
            limits.min = initial;
            limits.max = maximum;

            using var tableType = new TypeHandle(Native.wasm_tabletype_new(
                ValueType.FromKind(kind),
                limits
            ));

            var value = Value.FromObject(initialValue, Kind);
            var error = Native.wasmtime_table_new(store.Context.handle, tableType, in value, out this.table);
            GC.KeepAlive(store);
            value.Dispose();

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// Gets the value kind of the table.
        /// </summary>
        /// <value></value>
        public TableKind Kind { get; private set; }

        /// <summary>
        /// The minimum table element size.
        /// </summary>
        public uint Minimum { get; private set; }

        /// <summary>
        /// The maximum table element size.
        /// </summary>
        public uint Maximum { get; private set; }

        /// <summary>
        /// Gets an element from the table.
        /// </summary>
        /// <param name="index">The index in the table to get the element of.</param>
        /// <returns>Returns the table element.</returns>
        public object? GetElement(uint index)
        {
            var context = store.Context;
            if (!Native.wasmtime_table_get(context.handle, this.table, index, out var v))
            {
                throw new IndexOutOfRangeException();
            }

            GC.KeepAlive(store);

            var val = v.ToObject(store);
            v.Dispose();
            return val;
        }

        /// <summary>
        /// Sets an element in the table.
        /// </summary>
        /// <param name="index">The index in the table to set the element of.</param>
        /// <param name="value">The value to set.</param>
        public void SetElement(uint index, object? value)
        {
            var v = Value.FromObject(value, Kind);
            var error = Native.wasmtime_table_set(store.Context.handle, this.table, index, v);
            GC.KeepAlive(store);
            v.Dispose();

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// Gets the current size of the table.
        /// </summary>
        /// <value>Returns the current size of the table.</value>
        public uint GetSize()
        {
            var result = Native.wasmtime_table_size(store.Context.handle, this.table);
            GC.KeepAlive(store);
            return result;
        }

        /// <summary>
        /// Grows the table by the given number of elements.
        /// </summary>
        /// <param name="delta">The number of elements to grow the table.</param>
        /// <param name="initialValue">The initial value for the new elements.</param>
        /// <returns>Returns the previous number of elements in the table.</returns>
        public uint Grow(uint delta, object? initialValue)
        {
            var v = Value.FromObject(initialValue, Kind);

            var error = Native.wasmtime_table_grow(store.Context.handle, this.table, delta, v, out var prev);
            GC.KeepAlive(store);
            v.Dispose();

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return prev;
        }

        internal Table(Store store, ExternTable table)
        {
            this.store = store;
            this.table = table;

            using var type = new TypeHandle(Native.wasmtime_table_type(store.Context.handle, this.table));
            GC.KeepAlive(store);

            this.Kind = (TableKind)ValueType.ToKind(Native.wasm_tabletype_element(type.DangerousGetHandle()));

            unsafe
            {
                var limits = Native.wasm_tabletype_limits(type.DangerousGetHandle());
                Minimum = limits->min;
                Maximum = limits->max;
            }
        }

        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Table,
                of = new ExternUnion { table = this.table }
            };
        }

        Store? IExternal.Store => store;

        internal class TypeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public TypeHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                Native.wasm_tabletype_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [StructLayout(LayoutKind.Sequential)]
            internal struct Limits
            {
                public uint min;

                public uint max;
            }

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_table_new(IntPtr context, TypeHandle type, in Value val, out ExternTable table);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern bool wasmtime_table_get(IntPtr context, in ExternTable table, uint index, out Value val);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_table_set(IntPtr context, in ExternTable table, uint index, in Value val);

            [DllImport(Engine.LibraryName)]
            public static extern uint wasmtime_table_size(IntPtr context, in ExternTable table);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_table_grow(IntPtr context, in ExternTable table, uint delta, in Value value, out uint prev);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_table_type(IntPtr context, in ExternTable table);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_tabletype_new(IntPtr valueType, in Limits limits);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_tabletype_element(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern Limits* wasm_tabletype_limits(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_tabletype_delete(IntPtr handle);
        }

        private readonly Store store;
        private readonly ExternTable table;
    }
}
