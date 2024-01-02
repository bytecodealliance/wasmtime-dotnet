using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

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
        public Instance(Store store, Module module, params object[] imports)
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

            _store = store;

            unsafe
            {
                var externs = stackalloc Extern[imports.Length];
                for (int i = 0; i < imports.Length; ++i)
                {
                    var external = imports[i] as IExternal;
                    if (external is null)
                    {
                        throw new ArgumentException($"Objects of type `{imports[i].GetType()}` cannot be imported.");
                    }
                    externs[i] = external.AsExtern();
                }

                var error = Native.wasmtime_instance_new(store.Context.handle, module.NativeHandle, externs, (UIntPtr)imports.Length, out this.instance, out var trap);
                GC.KeepAlive(store);

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

        #region typed wrappers
        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Action? GetAction(string name)
        {
            return GetFunction(name)
                 ?.WrapAction();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">Parameter type</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Action<TA>? GetAction<TA>(string name)
        {
            return GetFunction(name)
                 ?.WrapAction<TA>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Action<TA, TB>? GetAction<TA, TB>(string name)
        {
            return GetFunction(name)
                 ?.WrapAction<TA, TB>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Action<TA, TB, TC>? GetAction<TA, TB, TC>(string name)
        {
            return GetFunction(name)
                 ?.WrapAction<TA, TB, TC>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Action<TA, TB, TC, TD>? GetAction<TA, TB, TC, TD>(string name)
        {
            return GetFunction(name)
                 ?.WrapAction<TA, TB, TC, TD>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Action<TA, TB, TC, TD, TE>? GetAction<TA, TB, TC, TD, TE>(string name)
        {
            return GetFunction(name)
                 ?.WrapAction<TA, TB, TC, TD, TE>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TR?>? GetFunction<TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TR?>? GetFunction<TA, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TR?>? GetFunction<TA, TB, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TR?>? GetFunction<TA, TB, TC, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TR?>? GetFunction<TA, TB, TC, TD, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TR?>? GetFunction<TA, TB, TC, TD, TE, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TJ">Tenth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TJ">Tenth parameter type</typeparam>
        /// <typeparam name="TK">Eleventh parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TJ">Tenth parameter type</typeparam>
        /// <typeparam name="TK">Eleventh parameter type</typeparam>
        /// <typeparam name="TL">Twelfth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TJ">Tenth parameter type</typeparam>
        /// <typeparam name="TK">Eleventh parameter type</typeparam>
        /// <typeparam name="TL">Twelfth parameter type</typeparam>
        /// <typeparam name="TM">Thirteenth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TJ">Tenth parameter type</typeparam>
        /// <typeparam name="TK">Eleventh parameter type</typeparam>
        /// <typeparam name="TL">Twelfth parameter type</typeparam>
        /// <typeparam name="TM">Thirteenth parameter type</typeparam>
        /// <typeparam name="TN">Fourteenth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TJ">Tenth parameter type</typeparam>
        /// <typeparam name="TK">Eleventh parameter type</typeparam>
        /// <typeparam name="TL">Twelfth parameter type</typeparam>
        /// <typeparam name="TM">Thirteenth parameter type</typeparam>
        /// <typeparam name="TN">Fourteenth parameter type</typeparam>
        /// <typeparam name="TO">Fifteenth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>();
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eighth parameter type</typeparam>
        /// <typeparam name="TI">Ninth parameter type</typeparam>
        /// <typeparam name="TJ">Tenth parameter type</typeparam>
        /// <typeparam name="TK">Eleventh parameter type</typeparam>
        /// <typeparam name="TL">Twelfth parameter type</typeparam>
        /// <typeparam name="TM">Thirteenth parameter type</typeparam>
        /// <typeparam name="TN">Fourteenth parameter type</typeparam>
        /// <typeparam name="TO">Fifteenth parameter type</typeparam>
        /// <typeparam name="TP">Sixteenth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR?>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>(string name)
        {
            return GetFunction(name)
                 ?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>();
        }
        #endregion

        /// <summary>
        /// Gets an exported function from the instance and check the type signature.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <param name="returnType">The return type of the function. Null if no return type. Tuple of types is multiple returns expected.</param>
        /// <param name="parameterTypes">The expected parameters to the function</param>
        /// <returns>Returns the function if a function of that name and type signature was exported or null if not.</returns>
        public Function? GetFunction(string name, Type? returnType, params Type[] parameterTypes)
        {
            var func = GetFunction(name);
            if (func is null)
            {
                return null;
            }

            if (!func.CheckTypeSignature(returnType, parameterTypes))
            {
                return null;
            }

            return func;
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>Returns the function if a function of that name was exported or null if not.</returns>
        public Function? GetFunction(string name)
        {
            var context = _store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Func)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return _store.GetCachedExtern(ext.of.func);
        }

        /// <summary>
        /// Gets an exported table from the instance.
        /// </summary>
        /// <param name="name">The name of the exported table.</param>
        /// <returns>Returns the table if a table of that name was exported or null if not.</returns>
        public Table? GetTable(string name)
        {
            var context = _store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Table)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return new Table(_store, ext.of.table);
        }

        /// <summary>
        /// Gets an exported memory from the instance.
        /// </summary>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the memory if a memory of that name was exported or null if not.</returns>
        public Memory? GetMemory(string name)
        {
            if (!TryGetExtern(_store.Context, name, out var ext) || ext.kind != ExternKind.Memory)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return _store.GetCachedExtern(ext.of.memory);
        }

        /// <summary>
        /// Gets an exported global from the instance.
        /// </summary>
        /// <param name="name">The name of the exported global.</param>
        /// <returns>Returns the global if a global of that name was exported or null if not.</returns>
        public Global? GetGlobal(string name)
        {
            var context = _store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Global)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return _store.GetCachedExtern(ext.of.global);
        }

        /// <summary>
        /// Get all exported functions
        /// </summary>
        /// <returns>An enumerable of functions exported from this instance</returns>
        public IEnumerable<(string Name, Function Function)> GetFunctions()
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (TryGetExtern(i, ExternKind.Func) is not var (name, @extern))
                {
                    break;
                }

                yield return (name, _store.GetCachedExtern(@extern.of.func));
            }

            GC.KeepAlive(_store);
        }

        /// <summary>
        /// Get all exported tables
        /// </summary>
        /// <returns>An enumerable of tables exported from this instance</returns>
        public IEnumerable<(string Name, Table Table)> GetTables()
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (TryGetExtern(i, ExternKind.Table) is not var (name, @extern))
                {
                    break;
                }

                yield return (name, new Table(_store, @extern.of.table));
            }

            GC.KeepAlive(_store);
        }

        /// <summary>
        /// Get all exported memories
        /// </summary>
        /// <returns>An enumerable of memories exported from this instance</returns>
        public IEnumerable<(string Name, Memory Memory)> GetMemories()
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (TryGetExtern(i, ExternKind.Memory) is not var (name, @extern))
                {
                    break;
                }

                yield return (name, _store.GetCachedExtern(@extern.of.memory));
            }

            GC.KeepAlive(_store);
        }

        /// <summary>
        /// Get all exported globals
        /// </summary>
        /// <returns>An enumerable of globals exported from this instance</returns>
        public IEnumerable<(string Name, Global Global)> GetGlobals()
        {
            for (var i = 0; i < int.MaxValue; i++)
            {
                if (TryGetExtern(i, ExternKind.Global) is not var (name, @extern))
                {
                    break;
                }

                yield return (name, _store.GetCachedExtern(@extern.of.global));
            }

            GC.KeepAlive(_store);
        }

        private (string name, Extern @extern)? TryGetExtern(int index, ExternKind? type = null)
        {
            unsafe
            {
                if (!Native.wasmtime_instance_export_nth(_store.Context.handle, instance, (UIntPtr)index, out var namePtr, out var nameLen, out var @extern))
                {
                    return null;
                }

                if (type != null && type.Value != @extern.kind)
                {
                    return  null;
                }

                var name = Encoding.UTF8.GetString(namePtr, checked((int)nameLen));
                return (name, @extern);
            }
        }

        private bool TryGetExtern(StoreContext context, string name, out Extern ext)
        {
            using var nameBytes = name.ToUTF8(stackalloc byte[Math.Min(64, name.Length * 2)]);

            unsafe
            {
                fixed (byte* ptr = nameBytes.Span)
                {
                    return Native.wasmtime_instance_export_get(context.handle, this.instance, ptr, (UIntPtr)nameBytes.Length, out ext);
                }
            }
        }

        internal Instance(Store store, ExternInstance instance)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            this._store = store;
            this.instance = instance;
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
        }

        private readonly Store _store;
        internal readonly ExternInstance instance;
    }
}
