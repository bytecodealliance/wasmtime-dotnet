using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents the Wasmtime linker that can be used to define imports
    /// and instantiate WebAssembly modules.
    /// </summary>
    public partial class Linker : IDisposable
    {
        private const int StackallocThreshold = 256;

        /// <summary>
        /// Constructs a new linker from the given engine.
        /// </summary>
        /// <param name="engine">The Wasmtime engine to use for the linker.</param>
        public Linker(Engine engine)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            handle = new Handle(Native.wasmtime_linker_new(engine.NativeHandle));
        }

        /// <summary>
        /// Configures whether or not the linker allows later definitions to shadow previous definitions.
        /// </summary>
        public bool AllowShadowing
        {
            set
            {
                Native.wasmtime_linker_allow_shadowing(handle, value);
            }
        }

        private void Define<T>(string module, string name, T item)
            where T : IExternal
        {
            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var store = item.Store;
            if (store is null)
            {
                throw new ArgumentException($"The item is not associated with a store.");
            }

            var ext = item.AsExtern();
            
            using var nameBytes = name.ToUTF8(stackalloc byte[Math.Min(64, name.Length * 2)]);
            using var moduleBytes = module.ToUTF8(stackalloc byte[Math.Min(64, module.Length * 2)]);

            unsafe
            {
                fixed (byte* modulePtr = moduleBytes.Span, namePtr = nameBytes.Span)
                {
                    var error = Native.wasmtime_linker_define(handle, store.Context.handle, modulePtr, (UIntPtr)moduleBytes.Length, namePtr, (UIntPtr)nameBytes.Length, ext);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }
                }
            }

            GC.KeepAlive(store);
        }

        /// <summary>
        /// Defines an item in the linker.
        /// </summary>
        /// <param name="module">The module name of the item.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="function">The item being defined</param>
        public void Define(string module, string name, Function function)
        {
            Define<Function>(module, name, function);
        }

        /// <summary>
        /// Defines an item in the linker.
        /// </summary>
        /// <param name="module">The module name of the item.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="global">The item being defined</param>
        public void Define(string module, string name, Global global)
        {
            Define<Global>(module, name, global);
        }

        /// <summary>
        /// Defines an item in the linker.
        /// </summary>
        /// <param name="module">The module name of the item.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="global">The item being defined</param>
        public void Define<T>(string module, string name, Global.Accessor<T> global)
        {
            Define<Global.Accessor<T>>(module, name, global);
        }

        /// <summary>
        /// Defines an item in the linker.
        /// </summary>
        /// <param name="module">The module name of the item.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="memory">The item being defined</param>
        public void Define(string module, string name, Memory memory)
        {
            Define<Memory>(module, name, memory);
        }

        /// <summary>
        /// Defines an item in the linker.
        /// </summary>
        /// <param name="module">The module name of the item.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="table">The item being defined</param>
        public void Define(string module, string name, Table table)
        {
            Define<Table>(module, name, table);
        }

        /// <summary>
        /// Defines WASI functions in the linker.
        /// </summary>
        /// <remarks>
        /// When WASI functions are defined in the linker, a store must be configured with a WASI
        /// configuration.
        /// </remarks>
        public void DefineWasi()
        {
            var error = Native.wasmtime_linker_define_wasi(handle);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// Defines an instance with the specified name in the linker.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the instance to define.</param>
        /// <param name="instance">The instance to define.</param>
        public void DefineInstance(Store store, string name, Instance instance)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (instance is null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            unsafe
            {
                var nameBytes = Encoding.UTF8.GetBytes(name);
                fixed (byte* namePtr = nameBytes)
                {
                    var error = Native.wasmtime_linker_define_instance(handle, store.Context.handle, namePtr, (UIntPtr)nameBytes.Length, instance.instance);
                    GC.KeepAlive(store);

                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }
                }
            }
        }

        /// <summary>
        /// Instantiates a module with imports from items defined in the linker.
        /// </summary>
        /// <param name="store">The store to instantiate in.</param>
        /// <param name="module">The module to instantiate.</param>
        /// <returns>Returns the new instance.</returns>
        public Instance Instantiate(Store store, Module module)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            var error = Native.wasmtime_linker_instantiate(handle, store.Context.handle, module.NativeHandle, out var instance, out var trap);
            GC.KeepAlive(store);

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            if (trap != IntPtr.Zero)
            {
                throw TrapException.FromOwnedTrap(trap);
            }

            return new Instance(store, instance);
        }

        /// <summary>
        /// Defines automatic instantiations of a module in this linker.
        /// </summary>
        /// <param name="store">The store to instantiate in.</param>
        /// <param name="module">The module to automatically instantiate.</param>
        public void DefineModule(Store store, Module module)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            unsafe
            {
                var nameBytes = Encoding.UTF8.GetBytes(module.Name);
                fixed (byte* namePtr = nameBytes)
                {
                    var error = Native.wasmtime_linker_module(handle, store.Context.handle, namePtr, (UIntPtr)nameBytes.Length, module.NativeHandle);
                    GC.KeepAlive(store);

                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the "default" function export for a module with the given name defined in the linker.
        /// </summary>
        /// <param name="store">The store for the function.</param>
        /// <param name="name">Tha name of the module to get the default function export.</param>
        /// <returns></returns>
        public Function GetDefaultFunction(Store store, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var context = store.Context;

            unsafe
            {
                var nameBytes = Encoding.UTF8.GetBytes(name);
                fixed (byte* namePtr = nameBytes)
                {
                    var error = Native.wasmtime_linker_get_default(handle, context.handle, namePtr, (UIntPtr)nameBytes.Length, out var func);
                    GC.KeepAlive(store);

                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }

                    return new Function(store, func);
                }
            }
        }

        /// <summary>
        /// Gets an exported function from the linker.
        /// </summary>
        /// <param name="store">The store of the function.</param>
        /// <param name="module">The module of the exported function.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>Returns the function if a function of that name was exported or null if not.</returns>
        public Function? GetFunction(Store store, string module, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, module, name, out var ext) || ext.kind != ExternKind.Func)
            {
                return null;
            }

            GC.KeepAlive(store);

            return new Function(store, ext.of.func);
        }

        /// <summary>
        /// Gets an exported table from the linker.
        /// </summary>
        /// <param name="store">The store of the table.</param>
        /// <param name="module">The module of the exported table.</param>
        /// <param name="name">The name of the exported table.</param>
        /// <returns>Returns the table if a table of that name was exported or null if not.</returns>
        public Table? GetTable(Store store, string module, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, module, name, out var ext) || ext.kind != ExternKind.Table)
            {
                return null;
            }

            GC.KeepAlive(store);

            return new Table(store, ext.of.table);
        }

        /// <summary>
        /// Gets an exported memory from the linker.
        /// </summary>
        /// <param name="store">The store of the memory.</param>
        /// <param name="module">The module of the exported memory.</param>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the memory if a memory of that name was exported or null if not.</returns>
        public Memory? GetMemory(Store store, string module, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, module, name, out var ext) || ext.kind != ExternKind.Memory)
            {
                return null;
            }

            return store.GetCachedExtern(ext.of.memory);
        }

        /// <summary>
        /// Gets an exported global from the linker.
        /// </summary>
        /// <param name="store">The store of the global.</param>
        /// <param name="module">The module of the exported global.</param>
        /// <param name="name">The name of the exported global.</param>
        /// <returns>Returns the global if a global of that name was exported or null if not.</returns>
        public Global? GetGlobal(Store store, string module, string name)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;
            if (!TryGetExtern(context, module, name, out var ext) || ext.kind != ExternKind.Global)
            {
                return null;
            }

            GC.KeepAlive(store);

            return new Global(store, ext.of.global);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        /// <summary>
        /// Defines an function in the linker given an untyped callback.
        /// </summary>
        /// <remarks>Functions defined with this method are store-independent.</remarks>
        /// <param name="module">The module name of the function.</param>
        /// <param name="name">The name of the function.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        /// <param name="parameterKinds">The function parameter kinds.</param>
        /// <param name="resultKinds">The function result kinds.</param>
        public void DefineFunction(string module, string name, Function.UntypedCallbackDelegate callback, IReadOnlyList<ValueKind> parameterKinds, IReadOnlyList<ValueKind> resultKinds)
        {
            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            unsafe
            {
                // Copy the lists to ensure they are not modified.
                parameterKinds = parameterKinds.ToArray();
                resultKinds = resultKinds.ToArray();
                Function.Native.WasmtimeFuncCallback func = (env, callerPtr, args, nargs, results, nresults) =>
                {
                    return Function.InvokeUntypedCallback(callback, callerPtr, args, (int)nargs, results, (int)nresults, resultKinds);
                };

                using var nameBytes = name.ToUTF8(stackalloc byte[Math.Min(64, name.Length * 2)]);
                using var moduleBytes = module.ToUTF8(stackalloc byte[Math.Min(64, module.Length * 2)]);

                var funcType = Function.CreateFunctionType(parameterKinds, resultKinds);
                try
                {
                    fixed (byte* modulePtr = moduleBytes.Span, namePtr = nameBytes.Span)
                    {
                        var error = Native.wasmtime_linker_define_func(
                            handle,
                            modulePtr,
                            (nuint)moduleBytes.Length,
                            namePtr,
                            (nuint)nameBytes.Length,
                            funcType,
                            func,
                            GCHandle.ToIntPtr(GCHandle.Alloc(func)),
                            Function.Finalizer
                        );

                        if (error != IntPtr.Zero)
                        {
                            throw WasmtimeException.FromOwnedError(error);
                        }
                    }
                }
                finally
                {
                    Function.Native.wasm_functype_delete(funcType);
                }
            }
        }

        private bool TryGetExtern(StoreContext context, string module, string name, out Extern ext)
        {
            unsafe
            {
                using var moduleBytes = module.ToUTF8(stackalloc byte[Math.Min(64, module.Length * 2)]);
                using var nameBytes = name.ToUTF8(stackalloc byte[Math.Min(64, name.Length * 2)]);

                fixed (byte* modulePtr = moduleBytes.Span, namePtr = nameBytes.Span)
                {
                    return Native.wasmtime_linker_get(handle, context.handle, modulePtr, (UIntPtr)moduleBytes.Length, namePtr, (UIntPtr)nameBytes.Length, out ext);
                }
            }
        }

        internal class Handle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public Handle(IntPtr handle)
                : base(true)
            {
                SetHandle(handle);
            }

            protected override bool ReleaseHandle()
            {
                Native.wasmtime_linker_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_linker_new(Engine.Handle engine);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_linker_delete(IntPtr linker);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_linker_allow_shadowing(Handle linker, [MarshalAs(UnmanagedType.I1)] bool allow);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define(Handle linker, IntPtr context, byte* module, nuint moduleLen, byte* name, nuint nameLen, in Extern item);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_linker_define_wasi(Handle linker);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define_instance(Handle linker, IntPtr context, byte* name, nuint len, in ExternInstance instance);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define_func(Handle linker, byte* module, nuint moduleLen, byte* name, nuint nameLen, IntPtr type, Function.Native.WasmtimeFuncCallback callback, IntPtr data, Function.Native.Finalizer? finalizer);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define_func_unchecked(Handle linker, byte* module, nuint moduleLen, byte* name, nuint nameLen, IntPtr type, Function.Native.WasmtimeFuncUncheckedCallback callback, IntPtr data, Function.Native.Finalizer? finalizer);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_linker_instantiate(Handle linker, IntPtr context, Module.Handle module, out ExternInstance instance, out IntPtr trap);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_module(Handle linker, IntPtr context, byte* name, nuint len, Module.Handle module);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_get_default(Handle linker, IntPtr context, byte* name, nuint len, out ExternFunc func);

            [DllImport(Engine.LibraryName)]
            [return: MarshalAs(UnmanagedType.I1)]
            public static unsafe extern bool wasmtime_linker_get(Handle linker, IntPtr context, byte* module, nuint moduleLen, byte* name, nuint nameLen, out Extern func);
        }

        private readonly Handle handle;
    }
}