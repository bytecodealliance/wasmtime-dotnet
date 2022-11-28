using System;
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

        /// <summary>
        /// Defines an item in the linker.
        /// </summary>
        /// <param name="module">The module name of the item.</param>
        /// <param name="name">The name of the item.</param>
        /// <param name="item">The item being defined (e.g. function, global, table, etc.).</param>
        public void Define(string module, string name, object item)
        {
            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var external = item as IExternal;
            if (external is null)
            {
                throw new ArgumentException($"Objects of type `{item.GetType().ToString()}` cannot be defined in a linker.");
            }

            var ext = external.AsExtern();

            unsafe
            {
                var moduleBytes = Encoding.UTF8.GetBytes(module);
                var nameBytes = Encoding.UTF8.GetBytes(name);
                fixed (byte* modulePtr = moduleBytes, namePtr = nameBytes)
                {
                    var error = Native.wasmtime_linker_define(handle, modulePtr, (UIntPtr)moduleBytes.Length, namePtr, (UIntPtr)nameBytes.Length, ext);
                    if (error != IntPtr.Zero)
                    {
                        throw WasmtimeException.FromOwnedError(error);
                    }
                }
            }
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
        public void DefineInstance(IStore store, string name, Instance instance)
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
        public Instance Instantiate(IStore store, Module module)
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
        public void DefineModule(IStore store, Module module)
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
        public Function GetDefaultFunction(IStore store, string name)
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
        public Function? GetFunction(IStore store, string module, string name)
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

            return new Function(store, ext.of.func);
        }

        /// <summary>
        /// Gets an exported table from the linker.
        /// </summary>
        /// <param name="store">The store of the table.</param>
        /// <param name="module">The module of the exported table.</param>
        /// <param name="name">The name of the exported table.</param>
        /// <returns>Returns the table if a table of that name was exported or null if not.</returns>
        public Table? GetTable(IStore store, string module, string name)
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

            return new Table(store, ext.of.table);
        }

        /// <summary>
        /// Gets an exported memory from the linker.
        /// </summary>
        /// <param name="store">The store of the memory.</param>
        /// <param name="module">The module of the exported memory.</param>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the memory if a memory of that name was exported or null if not.</returns>
        public Memory? GetMemory(IStore store, string module, string name)
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

            return new Memory(store, ext.of.memory);
        }

        /// <summary>
        /// Gets an exported global from the linker.
        /// </summary>
        /// <param name="store">The store of the global.</param>
        /// <param name="module">The module of the exported global.</param>
        /// <param name="name">The name of the exported global.</param>
        /// <returns>Returns the global if a global of that name was exported or null if not.</returns>
        public Global? GetGlobal(IStore store, string module, string name)
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

            return new Global(store, ext.of.global);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            handle.Dispose();
        }

        /// <summary>
        /// Defines a function in the linker.
        /// </summary>
        /// <remarks>Functions defined with this method are store-independent.</remarks>
        /// <param name="module">The module name of the function.</param>
        /// <param name="name">The name of the function.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public void DefineFunction(string module, string name, Delegate callback)
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

            var parameterKinds = new List<ValueKind>();
            var resultKinds = new List<ValueKind>();

            using var funcType = Function.GetFunctionType(callback.GetType(), parameterKinds, resultKinds, allowCaller: true, allowTuple: true, out var hasCaller, out var returnsTuple)!;
            var callbackInvokeMethod = callback.GetType().GetMethod(nameof(Action.Invoke))!;

            unsafe
            {
                Function.Native.WasmtimeFuncCallback func = (env, callerPtr, args, nargs, results, nresults) =>
                {
                    using var caller = new Caller(callerPtr);
                    return Function.InvokeCallback(callback, callbackInvokeMethod, caller, hasCaller, args, (int)nargs, results, (int)nresults, resultKinds, returnsTuple);
                };

                var moduleBytes = Encoding.UTF8.GetBytes(module);
                var nameBytes = Encoding.UTF8.GetBytes(name);
                fixed (byte* modulePtr = moduleBytes, namePtr = nameBytes)
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
        }

        private bool TryGetExtern(StoreContext context, string module, string name, out Extern ext)
        {
            unsafe
            {
                var moduleBytes = Encoding.UTF8.GetBytes(name);
                var nameBytes = Encoding.UTF8.GetBytes(name);
                fixed (byte* modulePtr = moduleBytes, namePtr = nameBytes)
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
            public static unsafe extern IntPtr wasmtime_linker_new(Engine.Handle engine);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_linker_delete(IntPtr linker);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_linker_allow_shadowing(Handle linker, bool allow);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define(Handle linker, byte* module, nuint moduleLen, byte* name, nuint nameLen, in Extern item);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_linker_define_wasi(Handle linker);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define_instance(Handle linker, IntPtr context, byte* name, nuint len, in ExternInstance instance);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define_func(Handle linker, byte* module, nuint moduleLen, byte* name, nuint nameLen, Function.TypeHandle type, Function.Native.WasmtimeFuncCallback callback, IntPtr data, Function.Native.Finalizer? finalizer);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_linker_define_func_unchecked(Handle linker, byte* module, nuint moduleLen, byte* name, nuint nameLen, Function.TypeHandle type, Function.Native.WasmtimeFuncUncheckedCallback callback, IntPtr data, Function.Native.Finalizer? finalizer);

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