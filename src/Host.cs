using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly host environment.
    /// </summary>
    /// <remarks>
    /// A host is used to configure the environment for WebAssembly modules to execute in.
    /// </remarks>
    public class Host : IDisposable
    {
        /// <summary>
        /// Constructs a new host.
        /// </summary>
        /// <param name="engine">The engine to use for the host.</param>
        public Host(Engine engine)
        {
            if (engine is null)
            {
                throw new ArgumentNullException(nameof(engine));
            }

            // Create a separate store for the host
            _store = Interop.wasm_store_new(engine.Handle);

            var linker = Interop.wasmtime_linker_new(_store);
            if (linker.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime linker.");
            }

            Interop.wasmtime_linker_allow_shadowing(linker, allowShadowing: true);

            _linker = linker;
        }

        /// <summary>
        /// Constructs a new host with the given store.
        /// </summary>
        /// <param name="store">The store to use for the host.</param>
        public Host(Store store)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            _store = store.Handle;
            _disposeStore = false;

            var linker = Interop.wasmtime_linker_new(_store);
            if (linker.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime linker.");
            }

            Interop.wasmtime_linker_allow_shadowing(linker, allowShadowing: true);

            _linker = linker;
        }

        /// <summary>
        /// Defines a WASI implementation in the host.
        /// </summary>
        /// <param name="name">The name of the WASI module to define.</param>
        /// <param name="config">The <see cref="WasiConfiguration"/> to configure the WASI implementation with.</param>
        public void DefineWasi(string name, WasiConfiguration config = null)
        {
            CheckDisposed();

            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException("Name cannot be null or empty.", nameof(name));
            }

            if (config is null)
            {
                config = new WasiConfiguration();
            }

            using var wasi = config.CreateWasi(_store, name);

            var error = Interop.wasmtime_linker_define_wasi(_linker, wasi);
            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction(string moduleName, string name, Action func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T>(string moduleName, string name, Action<T> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2>(string moduleName, string name, Action<T1, T2> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3>(string moduleName, string name, Action<T1, T2, T3> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4>(string moduleName, string name, Action<T1, T2, T3, T4> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5>(string moduleName, string name, Action<T1, T2, T3, T4, T5> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string moduleName, string name, Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16> func)
        {
            return DefineFunction(moduleName, name, func, false);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<TResult>(string moduleName, string name, Func<TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T, TResult>(string moduleName, string name, Func<T, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, TResult>(string moduleName, string name, Func<T1, T2, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, TResult>(string moduleName, string name, Func<T1, T2, T3, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a host function.
        /// </summary>
        /// <param name="moduleName">The module name of the host function.</param>
        /// <param name="name">The name of the host function.</param>
        /// <param name="func">The callback for when the host function is invoked.</param>
        /// <returns>Returns a <see cref="Function"/> representing the host function.</returns>
        public Function DefineFunction<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult>(string moduleName, string name, Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> func)
        {
            return DefineFunction(moduleName, name, func, true);
        }

        /// <summary>
        /// Defines a new host global variable.
        /// </summary>
        /// <param name="moduleName">The module name of the host variable.</param>
        /// <param name="name">The name of the host variable.</param>
        /// <param name="initialValue">The initial value of the host variable.</param>
        /// <typeparam name="T">The type of the host variable.</typeparam>
        /// <returns>Returns a new <see cref="Global{T}"/> representing the defined global variable.</returns>
        public Global<T> DefineGlobal<T>(string moduleName, string name, T initialValue)
        {
            CheckDisposed();

            if (moduleName is null)
            {
                throw new ArgumentNullException(nameof(moduleName));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var global = new Global<T>(_store, initialValue);
            var ex = Define(moduleName, name, Interop.wasm_global_as_extern(global.Handle));

            if (ex != null)
            {
                global.Dispose();
                throw new WasmtimeException($"Failed to define global '{name}' in module '{moduleName}': {ex.Message}");
            }

            return global;
        }

        /// <summary>
        /// Defines a new host mutable global variable.
        /// </summary>
        /// <param name="moduleName">The module name of the host variable.</param>
        /// <param name="name">The name of the host variable.</param>
        /// <param name="initialValue">The initial value of the host variable.</param>
        /// <typeparam name="T">The type of the host variable.</typeparam>
        /// <returns>Returns a new <see cref="MutableGlobal{T}"/> representing the defined mutable global variable.</returns>
        public MutableGlobal<T> DefineMutableGlobal<T>(string moduleName, string name, T initialValue)
        {
            CheckDisposed();

            if (moduleName is null)
            {
                throw new ArgumentNullException(nameof(moduleName));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var global = new MutableGlobal<T>(_store, initialValue);
            var ex = Define(moduleName, name, Interop.wasm_global_as_extern(global.Handle));

            if (ex != null)
            {
                global.Dispose();
                throw new WasmtimeException($"Failed to define global '{name}' in module '{moduleName}': {ex.Message}");
            }

            return global;
        }

        /// <summary>
        /// Defines a new host memory.
        /// </summary>
        /// <param name="moduleName">The module name of the host memory.</param>
        /// <param name="name">The name of the host memory.</param>
        /// <param name="minimum">The minimum number of pages for the host memory.</param>
        /// <param name="maximum">The maximum number of pages for the host memory.</param>
        /// <returns>Returns a new <see cref="Memory"/> representing the defined memory.</returns>
        public Memory DefineMemory(string moduleName, string name, uint minimum = 1, uint maximum = uint.MaxValue)
        {
            CheckDisposed();

            if (moduleName is null)
            {
                throw new ArgumentNullException(nameof(moduleName));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var memory = new Memory(_store, minimum, maximum);
            var ex = Define(moduleName, name, Interop.wasm_memory_as_extern(memory.Handle));

            if (ex != null)
            {
                memory.Dispose();
                throw new WasmtimeException($"Failed to define memory '{name}' in module '{moduleName}': {ex.Message}");
            }

            return memory;
        }

        /// <summary>
        /// Instantiates a WebAssembly module.
        /// </summary>
        /// <param name="module">The module to instantiate.</param>
        /// <returns>Returns a new <see cref="Instance" />.</returns>
        public Instance Instantiate(Module module)
        {
            CheckDisposed();

            if (module is null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            return new Instance(_linker, module);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (_disposeStore && !_store.IsInvalid)
            {
                _store.Dispose();
                _store.SetHandleAsInvalid();
            }

            if (!_linker.IsInvalid)
            {
                _linker.Dispose();
                _linker.SetHandleAsInvalid();
            }
        }

        private void CheckDisposed()
        {
            if (_linker.IsInvalid)
            {
                throw new ObjectDisposedException(typeof(Host).FullName);
            }
        }

        private Function DefineFunction(string moduleName, string name, Delegate func, bool hasReturn)
        {
            if (moduleName is null)
            {
                throw new ArgumentNullException(nameof(moduleName));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var function = new Function(_store, func, hasReturn);
            var ex = Define(moduleName, name, Interop.wasm_func_as_extern(function.Handle));

            if (ex != null)
            {
                function.Dispose();
                throw new WasmtimeException($"Failed to define function '{name}' in module '{moduleName}': {ex.Message}");
            }

            return function;
        }

        private WasmtimeException Define(string moduleName, string name, IntPtr ext)
        {
            var moduleNameBytes = Encoding.UTF8.GetBytes(moduleName);
            var nameBytes = Encoding.UTF8.GetBytes(name);

            unsafe
            {
                fixed (byte* moduleNamePtr = moduleNameBytes)
                fixed (byte* namePtr = nameBytes)
                {
                    Interop.wasm_byte_vec_t moduleNameVec = new Interop.wasm_byte_vec_t();
                    moduleNameVec.size = (UIntPtr)moduleNameBytes.Length;
                    moduleNameVec.data = moduleNamePtr;

                    Interop.wasm_byte_vec_t nameVec = new Interop.wasm_byte_vec_t();
                    nameVec.size = (UIntPtr)nameBytes.Length;
                    nameVec.data = namePtr;

                    var error = Interop.wasmtime_linker_define(_linker, ref moduleNameVec, ref nameVec, ext);
                    if (error != IntPtr.Zero)
                    {
                        return WasmtimeException.FromOwnedError(error);
                    }

                    return null;
                }
            }
        }

        private Interop.StoreHandle _store;
        private Interop.LinkerHandle _linker;
        private bool _disposeStore = true;
    }
}
