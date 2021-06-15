using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents the mutability of a WebAssembly global value.
    /// </summary>
    public enum Mutability : byte
    {
        /// <summary>
        /// The global value is immutable (i.e. constant).
        /// </summary>
        Immutable,
        /// <summary>
        /// The global value is mutable.
        /// </summary>
        Mutable,
    }

    /// <summary>
    /// Represents a WebAssembly global value.
    /// </summary>
    public class Global : IExternal
    {
        /// <summary>
        /// Creates a new WebAssembly global value.
        /// </summary>
        /// <param name="store">The store to create the global in.</param>
        /// <param name="kind">The kind of value stored in the global.</param>
        /// <param name="initialValue">The global's initial value.</param>
        /// <param name="mutability">The mutability of the global being created.</param>
        public Global(IStore store, ValueKind kind, object? initialValue, Mutability mutability)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            Kind = kind;
            Mutability = mutability;

            using var globalType = new TypeHandle(Native.wasm_globaltype_new(
                ValueType.FromKind(kind),
                mutability
            ));

            var value = Wasmtime.Value.FromObject(initialValue, Kind);
            var error = Native.wasmtime_global_new(store.Context.handle, globalType, in value, out this.global);
            value.Dispose();

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// Gets the value of the global.
        /// </summary>
        /// <param name="store">The store that owns the global.</param>
        /// <returns>Returns the global's value.</returns>
        public object? GetValue(IStore store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            Native.wasmtime_global_get(context.handle, this.global, out var v);
            var val = v.ToObject(context);
            v.Dispose();
            return val;
        }

        /// <summary>
        /// Sets the value of the global.
        /// </summary>
        /// <param name="store">The store that owns the global.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue(IStore store, object? value)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (Mutability != Mutability.Mutable)
            {
                throw new InvalidOperationException("The global is immutable and cannot be changed.");
            }

            var v = Value.FromObject(value, Kind);
            Native.wasmtime_global_set(store.Context.handle, this.global, in v);
            v.Dispose();
        }

        /// <summary>
        /// Gets the value kind of the global.
        /// </summary>
        public ValueKind Kind { get; private set; }

        /// <summary>
        /// Gets the mutability of the global.
        /// </summary>
        public Mutability Mutability { get; private set; }

        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Global,
                of = new ExternUnion { global = this.global }
            };
        }

        internal Global(StoreContext context, ExternGlobal global)
        {
            this.global = global;

            using var type = new TypeHandle(Native.wasmtime_global_type(context.handle, this.global));

            this.Kind = ValueType.ToKind(Native.wasm_globaltype_content(type.DangerousGetHandle()));
            this.Mutability = (Mutability)Native.wasm_globaltype_mutability(type.DangerousGetHandle());
        }

        internal class TypeHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public TypeHandle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                Native.wasm_globaltype_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_global_new(IntPtr context, TypeHandle type, in Value val, out ExternGlobal global);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_global_get(IntPtr context, in ExternGlobal global, out Value val);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_global_set(IntPtr context, in ExternGlobal global, in Value val);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_global_type(IntPtr context, in ExternGlobal global);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_globaltype_new(IntPtr valueType, Mutability mutability);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_globaltype_content(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern byte wasm_globaltype_mutability(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern void wasm_globaltype_delete(IntPtr type);
        }

        private readonly ExternGlobal global;
    }
}
