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
        public Func<TR>? GetFunction<TR>(string name)
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
        public Func<TA, TR>? GetFunction<TA, TR>(string name)
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
        public Func<TA, TB, TR>? GetFunction<TA, TB, TR>(string name)
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
        public Func<TA, TB, TC, TR>? GetFunction<TA, TB, TC, TR>(string name)
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
        public Func<TA, TB, TC, TD, TR>? GetFunction<TA, TB, TC, TD, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TR>? GetFunction<TA, TB, TC, TD, TE, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>(string name)
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>(string name)
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

            return new Function(_store, ext.of.func);
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

            return new Memory(_store, ext.of.memory);
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

            return new Global(_store, ext.of.global);
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

        internal Instance(IStore store, ExternInstance instance)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            this._store = store;
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

        private readonly IStore _store;
        internal readonly ExternInstance instance;
    }
}
