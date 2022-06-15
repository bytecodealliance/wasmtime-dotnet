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
        /// <typeparam name="TA">Paramerter type</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Action<TA>? GetAction<TA>(IStore store, string name)
        {
            var func = GetFunction(store, name, null, typeof(TA));
            return func?.WrapAction<TA>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TR>? GetFunction<TR>(IStore store, string name)
        {
            var func = GetFunction(store, name, typeof(TR));
            return func?.WrapFunc<TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TR>? GetFunction<TA, TR>(IStore store, string name)
        {
            var func = GetFunction(store, name, typeof(TR), typeof(TA));
            return func?.WrapFunc<TA, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TR>? GetFunction<TA, TB, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB));
            return func?.WrapFunc<TA, TB, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TR>? GetFunction<TA, TB, TC, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC));
            return func?.WrapFunc<TA, TB, TC, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TR>? GetFunction<TA, TB, TC, TD, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD));
            return func?.WrapFunc<TA, TB, TC, TD, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TR>? GetFunction<TA, TB, TC, TD, TE, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE));
            return func?.WrapFunc<TA, TB, TC, TD, TE, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF));
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG));
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <typeparam name="TA">First parameter type</typeparam>
        /// <typeparam name="TB">Second parameter type</typeparam>
        /// <typeparam name="TC">Third parameter type</typeparam>
        /// <typeparam name="TD">Fourth parameter type</typeparam>
        /// <typeparam name="TE">Fifth parameter type</typeparam>
        /// <typeparam name="TF">Sixth parameter type</typeparam>
        /// <typeparam name="TG">Seventh parameter type</typeparam>
        /// <typeparam name="TH">Eigth parameter type</typeparam>
        /// <typeparam name="TR">Return type. Use a tuple for multiple return values</typeparam>
        /// <returns>Returns the function if a function of that name and type was exported or null if not.</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH));
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH), typeof(TI));
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH), typeof(TI), typeof(TJ));
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH), typeof(TI), typeof(TJ), typeof(TK));
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR),
                typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                typeof(TE), typeof(TF), typeof(TG), typeof(TH),
                typeof(TI), typeof(TJ), typeof(TK), typeof(TL)
            );
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR),
                typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                typeof(TE), typeof(TF), typeof(TG), typeof(TH),
                typeof(TI), typeof(TJ), typeof(TK), typeof(TL),
                typeof(TM)
            );
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR),
                typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                typeof(TE), typeof(TF), typeof(TG), typeof(TH),
                typeof(TI), typeof(TJ), typeof(TK), typeof(TL),
                typeof(TM), typeof(TN)
            );
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR),
                typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                typeof(TE), typeof(TF), typeof(TG), typeof(TH),
                typeof(TI), typeof(TJ), typeof(TK), typeof(TL),
                typeof(TM), typeof(TN), typeof(TO)
            );
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
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
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>? GetFunction<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>(IStore store, string name)
        {
            Function? func = GetFunction(store, name, typeof(TR),
                typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                typeof(TE), typeof(TF), typeof(TG), typeof(TH),
                typeof(TI), typeof(TJ), typeof(TK), typeof(TL),
                typeof(TM), typeof(TN), typeof(TO), typeof(TP)
            );
            return func?.WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>(store);
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="store">The store that owns the instance.</param>
        /// <param name="name">The name of the exported function.</param>
        /// <param name="returnType">The return type of the function. Null if no return type. Tuple of types is multiple returns expected.</param>
        /// <param name="parameterTypes">The expected parameters to the function</param>
        /// <returns>Returns the function if a function of that name and type signature was exported or null if not.</returns>
        public Function? GetFunction(IStore store, string name, Type? returnType, params Type[] parameterTypes)
        {
            var func = GetFunction(store, name);
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
