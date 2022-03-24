using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents an instantiated WebAssembly module.
    /// </summary>
    public class Instance
    {
        /// <summary>
        /// Creates a new WebAssembly instance.
        /// </summary>
        /// <param name="store">The store to create the instance in.</param>
        /// <param name="module">The module to create the instance for.</param>
        /// <param name="imports">The imports for the instance.</param>
        public Instance(IStore store, Module module, params object[] imports)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (imports is null)
            {
                throw new ArgumentNullException(nameof(imports));
            }

            unsafe
            {
                var externs = stackalloc Extern[imports.Length];
                for (int i = 0; i < imports.Length; ++i)
                {
                    var external = imports[i] as IExternal;
                    if (external is null)
                    {
                        throw new ArgumentException($"Objects of type `{imports[i].GetType().ToString()}` cannot be imported.");
                    }
                    externs[i] = external.AsExtern();
                }

                var error = Native.wasmtime_instance_new(store.Context.handle, module.NativeHandle, externs, (UIntPtr)imports.Length, out this.instance, out var trap);

                if (error != IntPtr.Zero)
                {
                    throw WasmtimeException.FromOwnedError(error);
                }

                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }
            }
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>Returns the function if a function of that name was exported or null if not.</returns>
        public Function? GetFunction(IStore store, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Func)
            {
                return null;
            }

            return new Function(context, ext.of.func);
        }

        /// <summary>
        /// Gets an exported table from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported table.</param>
        /// <returns>Returns the table if a table of that name was exported or null if not.</returns>
        public Table? GetTable(IStore store, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Table)
            {
                return null;
            }

            return new Table(context, ext.of.table);
        }

        /// <summary>
        /// Gets an exported memory from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the memory if a memory of that name was exported or null if not.</returns>
        public Memory? GetMemory(IStore store, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Memory)
            {
                return null;
            }

            return new Memory(context, ext.of.memory);
        }

        /// <summary>
        /// Gets an exported global from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported global.</param>
        /// <returns>Returns the global if a global of that name was exported or null if not.</returns>
        public Global? GetGlobal(IStore store, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Global)
            {
                return null;
            }

            return new Global(context, ext.of.global);
        }

        private bool TryGetExtern(StoreContext context, string name, out Extern ext)
        {
            unsafe
            {
                var nameBytes = Encoding.UTF8.GetBytes(name);
                fixed (byte* ptr = nameBytes)
                {
                    return Native.wasmtime_instance_export_get(context.handle, this.instance, ptr, (UIntPtr)nameBytes.Length, out ext);
                }
            }
        }

        internal Instance(ExternInstance instance)
        {
            this.instance = instance;
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
                Native.wasmtime_instancetype_delete(handle);
                return true;
            }
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern unsafe IntPtr wasmtime_instance_new(IntPtr context, Module.Handle module, Extern* imports, UIntPtr nimports, out ExternInstance instance, out IntPtr trap);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern unsafe bool wasmtime_instance_export_get(IntPtr context, in ExternInstance instance, byte* name, UIntPtr len, out Extern ext);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern unsafe bool wasmtime_instance_export_nth(IntPtr context, in ExternInstance instance, UIntPtr index, out byte* name, out UIntPtr len, out Extern ext);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_instancetype_delete(IntPtr handle);
        }

        internal readonly ExternInstance instance;
    }
}
