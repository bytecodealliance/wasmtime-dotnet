using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace Wasmtime
{
    /// <summary>
    /// Represents a Wasmtime function.
    /// </summary>
    public partial class Function : IExternal
    {
        #region FromCallback
        /// <summary>
        /// Creates a function given a callback.
        /// </summary>
        /// <param name="store">The store to create the function in.</param>
        /// <param name="callback">The callback for when the function is invoked.</param>
        public static Function FromCallback(IStore store, Delegate callback)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (callback is null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

            var parameterKinds = new List<ValueKind>();
            var resultKinds = new List<ValueKind>();

            using var funcType = GetFunctionType(callback.GetType(), parameterKinds, resultKinds, allowCaller: true, allowTuple: true, out var hasCaller, out var returnsTuple)!;
            var callbackInvokeMethod = callback.GetType().GetMethod(nameof(Action.Invoke))!;

            unsafe
            {
                Native.WasmtimeFuncCallback? func = (env, callerPtr, args, nargs, results, nresults) =>
                {
                    using var caller = new Caller(callerPtr);
                    return InvokeCallback(callback, callbackInvokeMethod, caller, hasCaller, args, (int)nargs, results, (int)nresults, resultKinds, returnsTuple);
                };

                Native.wasmtime_func_new(
                    store.Context.handle,
                    funcType,
                    func,
                    GCHandle.ToIntPtr(GCHandle.Alloc(func)),
                    Finalizer,
                    out var externFunc
                );

                return new Function(store, externFunc, parameterKinds, resultKinds);
            }
        }
        #endregion

        /// <summary>
        /// The parameters of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Parameters => parameters;

        /// <summary>
        /// The results of the WebAssembly function.
        /// </summary>
        public IReadOnlyList<ValueKind> Results => results;

        /// <summary>
        /// Determines if the underlying function reference is null.
        /// </summary>
        public bool IsNull => func.index == UIntPtr.Zero && func.store == 0;

        /// <summary>
        /// Represents a null function reference.
        /// </summary>
        public static Function Null => _null;

        /// <summary>
        /// Check if this function has the given type signature
        /// </summary>
        /// <param name="returnType">Return type (use a tuple for multiple return types)</param>
        /// <param name="parameters">The parameters of the function</param>
        /// <returns>Returns true if the type signature of the function is valid or false if not.</returns>
        public bool CheckTypeSignature(Type? returnType = null, params Type[] parameters)
        {
            // Check if the return type is a recognised result type (i.e. implements IActionResult or IFunctionResult)
            if (returnType != null && returnType.IsResultType())
            {
                // Try to get the type the result wraps (may be null if it's one of the non-generic result types)
                var wrappedReturnType = returnType.GetResultInnerType();

                // Check that the result does not attempt to wrap another result (e.g. Result<Result<int>>)
                if (wrappedReturnType != null && wrappedReturnType.IsResultType())
                {
                    return false;
                }

                // Type check with the wrapped value instead of the result
                return CheckTypeSignature(wrappedReturnType, parameters);
            }

            // Check if the func returns no values if that's expected
            if (Results.Count == 0 && returnType != null)
            {
                return false;
            }

            // Check if the func does return a value if that's expected
            if (Results.Count != 0 && returnType == null)
            {
                return false;
            }

            // Validate the return type(s)
            if (returnType != null)
            {
                // Multiple return types are represented by a tuple.
                if (typeof(ITuple).IsAssignableFrom(returnType))
                {
                    // Get the types from the tuple
                    var returnTypes = returnType.GetGenericArguments();

                    // Tuples with more than seven items are not. This is because under the hood only tuples
                    // up to 8 items are supported, longer tuples are faked by having a tuple with seven items
                    // and then the last field is a tuple of the remaining items. To avoid having to deal with this,
                    // simply don't support tuple that long.
                    if (returnTypes.Length >= 8)
                    {
                        return false;
                    }

                    // If the list lengths are different that's an instant fail
                    if (returnTypes.Length != Results.Count)
                    {
                        return false;
                    }

                    // Validate the types one by one
                    for (int i = 0; i < returnTypes.Length; i++)
                    {
                        if (!Results[i].IsAssignableFrom(returnTypes[i]))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // Return type is not a tuple, so if there are multiple results this is not valid.
                    if (Results.Count != 1)
                    {
                        return false;
                    }

                    // If the return type is not compatible then this is not valid.
                    if (!Results[0].IsAssignableFrom(returnType))
                    {
                        return false;
                    }
                }
            }

            // Check if the parameter lists are the same length
            if (parameters.Length != Parameters.Count)
            {
                return false;
            }

            // Validate the parameter types one by one
            for (int i = 0; i < parameters.Length; i++)
            {
                if (!Parameters[i].IsAssignableFrom(parameters[i]))
                {
                    return false;
                }
            }

            // All ok!
            return true;
        }

        #region wrap
        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action? WrapAction()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature())
            {
                return null;
            }

            return () =>
            {
                Span<Value> args = stackalloc Value[0];
                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA>? WrapAction<TA>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();

            return (a) =>
            {
                Span<Value> args = stackalloc Value[1];
                args[0] = Value.FromValueBox(ca.Box(a));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB>? WrapAction<TA, TB>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();

            return (a, b) =>
            {
                Span<Value> args = stackalloc Value[2];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC>? WrapAction<TA, TB, TC>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();

            return (a, b, c) =>
            {
                Span<Value> args = stackalloc Value[3];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD>? WrapAction<TA, TB, TC, TD>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();

            return (a, b, c, d) =>
            {
                Span<Value> args = stackalloc Value[4];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE>? WrapAction<TA, TB, TC, TD, TE>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();

            return (a, b, c, d, e) =>
            {
                Span<Value> args = stackalloc Value[5];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF>? WrapAction<TA, TB, TC, TD, TE, TF>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();

            return (a, b, c, d, e, f) =>
            {
                Span<Value> args = stackalloc Value[6];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF, TG>? WrapAction<TA, TB, TC, TD, TE, TF, TG>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF), typeof(TG)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();

            return (a, b, c, d, e, f, g) =>
            {
                Span<Value> args = stackalloc Value[7];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF, TG, TH>? WrapAction<TA, TB, TC, TD, TE, TF, TG, TH>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF), typeof(TG), typeof(TH)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();

            return (a, b, c, d, e, f, g, h) =>
            {
                Span<Value> args = stackalloc Value[8];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as an Action. Wrapped action is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <returns>An action to invoke this function. Null if the type signature is incompatible</returns>
        public Action<TA, TB, TC, TD, TE, TF, TG, TH, TI>? WrapAction<TA, TB, TC, TD, TE, TF, TG, TH, TI>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(null, typeof(TA), typeof(TB), typeof(TC), typeof(TD),
                                          typeof(TE), typeof(TF), typeof(TG), typeof(TH),
                                          typeof(TI)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();

            return (a, b, c, d, e, f, g, h, i) =>
            {
                Span<Value> args = stackalloc Value[9];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));

                InvokeWithoutReturn(args);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TR>? WrapFunc<TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR)))
            {
                return null;
            }

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return () =>
            {
                return InvokeWithReturn(stackalloc Value[0], factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TR>? WrapFunc<TA, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            if (!CheckTypeSignature(typeof(TR), typeof(TA)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a) =>
            {
                Span<Value> args = stackalloc Value[1];
                args[0] = Value.FromValueBox(ca.Box(a));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">First parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TR>? WrapFunc<TA, TB, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b) =>
            {
                Span<Value> args = stackalloc Value[2];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TR>? WrapFunc<TA, TB, TC, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c) =>
            {
                Span<Value> args = stackalloc Value[3];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TR>? WrapFunc<TA, TB, TC, TD, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d) =>
            {
                Span<Value> args = stackalloc Value[4];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TR>? WrapFunc<TA, TB, TC, TD, TE, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e) =>
            {
                Span<Value> args = stackalloc Value[5];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f) =>
            {
                Span<Value> args = stackalloc Value[6];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g) =>
            {
                Span<Value> args = stackalloc Value[7];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h) =>
            {
                Span<Value> args = stackalloc Value[8];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC), typeof(TD), typeof(TE), typeof(TF), typeof(TG), typeof(TH), typeof(TI)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i) =>
            {
                Span<Value> args = stackalloc Value[9];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j) =>
            {
                Span<Value> args = stackalloc Value[10];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k) =>
            {
                Span<Value> args = stackalloc Value[11];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l) =>
            {
                Span<Value> args = stackalloc Value[12];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m) =>
            {
                Span<Value> args = stackalloc Value[13];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <typeparam name="TN">Fourteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM), typeof(TN)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();
            var cn = ValueBox.Converter<TN>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m, n) =>
            {
                Span<Value> args = stackalloc Value[14];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));
                args[13] = Value.FromValueBox(cn.Box(n));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <typeparam name="TN">Fourteenth parameter</typeparam>
        /// <typeparam name="TO">Fifteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM), typeof(TN), typeof(TO)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();
            var cn = ValueBox.Converter<TN>();
            var co = ValueBox.Converter<TO>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o) =>
            {
                Span<Value> args = stackalloc Value[15];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));
                args[13] = Value.FromValueBox(cn.Box(n));
                args[14] = Value.FromValueBox(co.Box(o));

                return InvokeWithReturn(args, factory);
            };
        }

        /// <summary>
        /// Attempt to wrap this function as a Func. Wrapped func is faster than a normal Invoke call.
        /// </summary>
        /// <typeparam name="TR">Return type</typeparam>
        /// <typeparam name="TA">First parameter</typeparam>
        /// <typeparam name="TB">Second parameter</typeparam>
        /// <typeparam name="TC">Third parameter</typeparam>
        /// <typeparam name="TD">Fourth parameter</typeparam>
        /// <typeparam name="TE">Fifth parameter</typeparam>
        /// <typeparam name="TF">Sixth parameter</typeparam>
        /// <typeparam name="TG">Seventh parameter</typeparam>
        /// <typeparam name="TH">Eighth parameter</typeparam>
        /// <typeparam name="TI">Ninth parameter</typeparam>
        /// <typeparam name="TJ">Tenth parameter</typeparam>
        /// <typeparam name="TK">Eleventh parameter</typeparam>
        /// <typeparam name="TL">Twelfth parameter</typeparam>
        /// <typeparam name="TM">Thirteenth parameter</typeparam>
        /// <typeparam name="TN">Fourteenth parameter</typeparam>
        /// <typeparam name="TO">Fifteenth parameter</typeparam>
        /// <typeparam name="TP">Sixteenth parameter</typeparam>
        /// <returns>A Func to invoke this function. Null if the type signature is incompatible</returns>
        public Func<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>? WrapFunc<TA, TB, TC, TD, TE, TF, TG, TH, TI, TJ, TK, TL, TM, TN, TO, TP, TR>()
        {
            if (store is null)
            {
                throw new InvalidOperationException("Cannot Wrap null function");
            }

            // Check that the requested type signature is compatible
            if (!CheckTypeSignature(typeof(TR), typeof(TA), typeof(TB), typeof(TC),
                                    typeof(TD), typeof(TE), typeof(TF), typeof(TG),
                                    typeof(TH), typeof(TI), typeof(TJ), typeof(TK),
                                    typeof(TL), typeof(TM), typeof(TN), typeof(TO), typeof(TP)))
            {
                return null;
            }

            // Fetch a converter for each parameter type to box it
            var ca = ValueBox.Converter<TA>();
            var cb = ValueBox.Converter<TB>();
            var cc = ValueBox.Converter<TC>();
            var cd = ValueBox.Converter<TD>();
            var ce = ValueBox.Converter<TE>();
            var cf = ValueBox.Converter<TF>();
            var cg = ValueBox.Converter<TG>();
            var ch = ValueBox.Converter<TH>();
            var ci = ValueBox.Converter<TI>();
            var cj = ValueBox.Converter<TJ>();
            var ck = ValueBox.Converter<TK>();
            var cl = ValueBox.Converter<TL>();
            var cm = ValueBox.Converter<TM>();
            var cn = ValueBox.Converter<TN>();
            var co = ValueBox.Converter<TO>();
            var cp = ValueBox.Converter<TP>();

            // Create a factory for the return type
            var factory = IReturnTypeFactory<TR>.Create();

            return (a, b, c, d, e, f, g, h, i, j, k, l, m, n, o, p) =>
            {
                Span<Value> args = stackalloc Value[16];
                args[0] = Value.FromValueBox(ca.Box(a));
                args[1] = Value.FromValueBox(cb.Box(b));
                args[2] = Value.FromValueBox(cc.Box(c));
                args[3] = Value.FromValueBox(cd.Box(d));
                args[4] = Value.FromValueBox(ce.Box(e));
                args[5] = Value.FromValueBox(cf.Box(f));
                args[6] = Value.FromValueBox(cg.Box(g));
                args[7] = Value.FromValueBox(ch.Box(h));
                args[8] = Value.FromValueBox(ci.Box(i));
                args[9] = Value.FromValueBox(cj.Box(j));
                args[10] = Value.FromValueBox(ck.Box(k));
                args[11] = Value.FromValueBox(cl.Box(l));
                args[12] = Value.FromValueBox(cm.Box(m));
                args[13] = Value.FromValueBox(cn.Box(n));
                args[14] = Value.FromValueBox(co.Box(o));
                args[15] = Value.FromValueBox(cp.Box(p));

                return InvokeWithReturn(args, factory);
            };
        }
        #endregion

        /// <summary>
        /// Invokes the wasmtime function and processes the results through a return type factory.
        /// Assumes arguments are the correct type. Disposes the arguments.
        /// </summary>
        /// <typeparam name="TR"></typeparam>
        /// <param name="arguments">Span of arguments, will be diposed after use.</param>
        /// <param name="factory">Factory to use to construct the return item</param>
        /// <returns>The return value from the function</returns>
        private unsafe TR InvokeWithReturn<TR>(ReadOnlySpan<Value> arguments, IReturnTypeFactory<TR> factory)
        {
            Span<Value> output = stackalloc Value[Results.Count];

            try
            {
                var trap = Invoke(arguments, output);

                // Note: null suppression is safe because `Invoke` checks that `store` is not null
                return factory.Create(store!, trap, output);
            }
            finally
            {
                for (int i = 0; i < output.Length; i++)
                {
                    output[i].Dispose();
                }

                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    argument.Dispose();
                }
            }
        }

        /// <summary>
        /// Invokes the wasmtime function
        /// Assumes arguments are the correct type. Disposes the arguments.
        /// </summary>
        /// <param name="arguments">Span of arguments, will be diposed after use.</param>
        /// <returns></returns>
        private unsafe void InvokeWithoutReturn(ReadOnlySpan<Value> arguments)
        {
            try
            {
                var trap = Invoke(arguments, stackalloc Value[0]);
                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }
            }
            finally
            {
                for (int i = 0; i < arguments.Length; i++)
                {
                    var argument = arguments[i];
                    argument.Dispose();
                }
            }
        }

        /// <summary>
        /// Invokes the Wasmtime function with no arguments.
        /// </summary>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        public object? Invoke()
        {
            return Invoke(new ReadOnlySpan<ValueBox>());
        }

        /// <summary>
        /// Invokes the Wasmtime function.
        /// </summary>
        /// <param name="arguments">The array of arguments to pass to the function.</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        // TODO: remove overload when https://github.com/dotnet/csharplang/issues/1757 is resolved
        public object? Invoke(params ValueBox[] arguments)
        {
            return Invoke((ReadOnlySpan<ValueBox>)arguments);
        }

        /// <summary>
        /// Invokes the Wasmtime function.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the function, wrapped in `ValueBox`</param>
        /// <returns>
        ///   Returns null if the function has no return value.
        ///   Returns the value if the function returns a single value.
        ///   Returns an array of values if the function returns more than one value.
        /// </returns>
        public object? Invoke(ReadOnlySpan<ValueBox> arguments)
        {
            if (IsNull)
            {
                throw new InvalidOperationException("Cannot invoke a null function reference.");
            }

            if (arguments.Length != Parameters.Count)
            {
                throw new WasmtimeException($"Argument mismatch when invoking function: requires {Parameters.Count} but was given {arguments.Length}.");
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            // Convert arguments (ValueBox) into a form wasm can consume (Value)
            Span<Value> args = stackalloc Value[Parameters.Count];
            for (var i = 0; i < arguments.Length; ++i)
            {
                args[i] = arguments[i].ToValue(Parameters[i]);
            }

            // Make some space to store the return results
            Span<Value> resultsSpan = stackalloc Value[Results.Count];

            try
            {
                var trap = Invoke(args, resultsSpan);
                if (trap != IntPtr.Zero)
                {
                    throw TrapException.FromOwnedTrap(trap);
                }

                if (Results.Count == 0)
                {
                    return null;
                }

                try
                {
                    if (Results.Count == 1)
                    {
                        return resultsSpan[0].ToObject(store);
                    }

                    var ret = new object?[Results.Count];
                    for (int i = 0; i < Results.Count; ++i)
                    {
                        ret[i] = resultsSpan[i].ToObject(store);
                    }

                    return ret;
                }
                finally
                {
                    for (int i = 0; i < Results.Count; ++i)
                    {
                        resultsSpan[i].Dispose();
                    }
                }
            }
            finally
            {
                for (int i = 0; i < arguments.Length; ++i)
                {
                    args[i].Dispose();
                }
            }

        }

        /// <summary>
        /// Invokes the Wasmtime function. Assumes arguments are the correct type and return span is the correct size.
        /// </summary>
        /// <param name="arguments">The arguments to pass to the function, wrapped as `Value`</param>
        /// <param name="resultsOut">Output span to store the results in, must be the correct length</param>
        /// <returns>
        ///   Returns the trap ptr or zero
        /// </returns>
        private unsafe IntPtr Invoke(ReadOnlySpan<Value> arguments, Span<Value> resultsOut)
        {
            if (IsNull)
            {
                throw new InvalidOperationException("Cannot invoke a null function reference.");
            }

            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            var context = store.Context;

            IntPtr error;
            IntPtr trap;
            fixed (Value* argsPtr = arguments)
            fixed (Value* resultsPtr = resultsOut)
            {
                error = Native.wasmtime_func_call(context.handle, func, argsPtr, (UIntPtr)Parameters.Count, resultsPtr, (UIntPtr)Results.Count, out trap);
            }

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            return trap;
        }

        Extern IExternal.AsExtern()
        {
            return new Extern
            {
                kind = ExternKind.Func,
                of = new ExternUnion { func = this.func }
            };
        }

        internal Function()
        {
            this.store = null;
            this.func.store = 0;
            this.func.index = (UIntPtr)0;
        }

        internal Function(IStore store, ExternFunc func)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;

            this.func = func;

            if (!this.IsNull)
            {
                using var type = new TypeHandle(Native.wasmtime_func_type(store.Context.handle, this.func));

                unsafe
                {
                    parameters = (*Native.wasm_functype_params(type.DangerousGetHandle())).ToList();
                    results = (*Native.wasm_functype_results(type.DangerousGetHandle())).ToList();
                }
            }
        }
        private Function(IStore store, ExternFunc func, List<ValueKind> parameters, List<ValueKind> results)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }
            this.store = store;

            this.func = func;
            this.parameters = parameters;
            this.results = results;
        }

        private static IEnumerable<Type> EnumerateReturnTypes(Type? returnType, out bool isTuple)
        {
            isTuple = false;

            if (returnType is null)
            {
                return Array.Empty<Type>();
            }

            if (IsTuple(returnType))
            {
                isTuple = true;
                return EnumerateTupleTypes(returnType);

                static IEnumerable<Type> EnumerateTupleTypes(Type tupleType)
                {
                    foreach (var (typeArgument, idx) in tupleType.GenericTypeArguments.Select((e, idx) => (e, idx)))
                    {
                        if (idx is 7 && IsTuple(typeArgument))
                        {
                            // Recursively enumerate the nested tuple's type arguments.
                            foreach (var type in EnumerateTupleTypes(typeArgument))
                            {
                                yield return type;
                            }
                        }
                        else
                        {
                            yield return typeArgument;
                        }
                    }
                }
            }
            else
            {
                return new Type[] { returnType };
            }
        }

        private static bool IsTuple(Type type)
        {
            if (!type.IsConstructedGenericType)
            {
                return false;
            }

            var definition = type.GetGenericTypeDefinition();

            return definition == typeof(ValueTuple) ||
                   definition == typeof(ValueTuple<>) ||
                   definition == typeof(ValueTuple<,>) ||
                   definition == typeof(ValueTuple<,,>) ||
                   definition == typeof(ValueTuple<,,,>) ||
                   definition == typeof(ValueTuple<,,,,>) ||
                   definition == typeof(ValueTuple<,,,,,>) ||
                   definition == typeof(ValueTuple<,,,,,,>) ||
                   definition == typeof(ValueTuple<,,,,,,,>);
        }

        internal static TypeHandle? GetFunctionType(Type type, List<ValueKind> parameters, List<ValueKind> results, bool allowCaller, bool allowTuple, out bool hasCaller, out bool returnsTuple)
        {
            if (!typeof(Delegate).IsAssignableFrom(type))
                throw new ArgumentException("The specified type must be a Delegate type.");

            var invokeMethod = type.GetMethod(nameof(Action.Invoke))!;

            Span<Type> parameterTypes = invokeMethod.GetParameters().Select(e => e.ParameterType).ToArray();
            Type? returnType = invokeMethod.ReturnType == typeof(void) ? null : invokeMethod.ReturnType;

            return GetFunctionType(parameterTypes, returnType, parameters, results, allowCaller, allowTuple, out hasCaller, out returnsTuple);
        }

        internal static TypeHandle? GetFunctionType(ReadOnlySpan<Type> parameterTypes, Type? returnType, List<ValueKind> parameters, List<ValueKind> results, bool allowCaller, bool allowTuple, out bool hasCaller, out bool returnsTuple)
        {            
            hasCaller = parameterTypes.Length > 0 && parameterTypes[0] == typeof(Caller);

            if (hasCaller)
            {
                parameterTypes = parameterTypes[1..];
            }

            for (int i = 0; i < parameterTypes.Length; ++i)
            {
                if (parameterTypes[i] == typeof(Caller))
                {
                    throw new WasmtimeException($"A 'Caller' parameter must be the first parameter of the function.");
                }

                if (!Value.TryGetKind(parameterTypes[i], out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with parameter of type '{parameterTypes[i].ToString()}'.");
                }

                parameters.Add(kind);
            }

            results.AddRange(EnumerateReturnTypes(returnType, out returnsTuple).Select(t =>
            {
                if (!Value.TryGetKind(t, out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with a return type of type '{t.ToString()}'.");
                }
                return kind;
            }));

            if (hasCaller && !allowCaller || returnsTuple && !allowTuple)
            {
                // Return null to indicate that the parameter/result type combination
                // is not allowed.
                hasCaller = default;
                returnsTuple = default;
                return null;
            }

            return new Function.TypeHandle(Function.Native.wasm_functype_new(new ValueTypeArray(parameters), new ValueTypeArray(results)));
        }

        internal unsafe static IntPtr InvokeCallback(Delegate callback, MethodInfo callbackInvokeMethod, Caller caller, bool passCaller, Value* args, int nargs, Value* results, int nresults, IReadOnlyList<ValueKind> resultKinds, bool returnsTuple)
        {
            try
            {
                var offset = passCaller ? 1 : 0;
                var invokeArgs = new object?[nargs + offset];

                if (passCaller)
                {
                    invokeArgs[0] = caller;
                }

                var invokeArgsSpan = new Span<object?>(invokeArgs, offset, nargs);
                for (int i = 0; i < invokeArgsSpan.Length; ++i)
                {
                    invokeArgsSpan[i] = args[i].ToObject(caller);
                }

                // NOTE: reflection is extremely slow for invoking methods. in the future, perhaps this could be replaced with
                // source generators, system.linq.expressions, or generate IL with DynamicMethods or something
                var result = callbackInvokeMethod.Invoke(callback, BindingFlags.DoNotWrapExceptions, null, invokeArgs, null);

                if (returnsTuple)
                {
                    var tuple = (ITuple)result!;

                    for (int i = 0; i < tuple.Length; ++i)
                    {
                        results[i] = Value.FromObject(tuple[i], resultKinds[i]);
                    }
                }
                else if (resultKinds.Count == 1)
                {
                    results[0] = Value.FromObject(result, resultKinds[0]);
                }
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                return HandleCallbackException(ex);
            }
        }

        internal static unsafe IntPtr HandleCallbackException(Exception ex)
        {
            // Store the exception as error cause, so that we can use it as the WasmtimeException's
            // InnerException when the error bubbles up to the next host-to-wasm transition.
            // If the exception is already a WasmtimeException, we use that one's InnerException,
            // even if it's null.
            // Note: This code currently requires that on every host-to-wasm transition where a
            // error can occur, WasmtimeException.FromOwnedError() is called when an error actually
            // occured, which will then clear this field.
            CallbackErrorCause = ex is WasmtimeException wasmtimeException ? wasmtimeException.InnerException : ex;
            
            var bytes = Encoding.UTF8.GetBytes(ex.Message);

            fixed (byte* ptr = bytes)
            {
                return Native.wasmtime_trap_new(ptr, (nuint)bytes.Length);
            }
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
                Native.wasm_functype_delete(handle);
                return true;
            }
        }

        internal static class Native
        {
            public delegate void Finalizer(IntPtr data);

            public unsafe delegate IntPtr WasmtimeFuncCallback(IntPtr env, IntPtr caller, Value* args, UIntPtr nargs, Value* results, UIntPtr nresults);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_func_new(IntPtr context, TypeHandle type, WasmtimeFuncCallback callback, IntPtr env, Finalizer? finalizer, out ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_func_call(IntPtr context, in ExternFunc func, Value* args, UIntPtr nargs, Value* results, UIntPtr nresults, out IntPtr trap);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_func_type(IntPtr context, in ExternFunc func);

            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasm_functype_new(in ValueTypeArray parameters, in ValueTypeArray results);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ValueTypeArray* wasm_functype_params(IntPtr type);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe ValueTypeArray* wasm_functype_results(IntPtr type);


            [DllImport(Engine.LibraryName)]
            public static extern void wasm_functype_delete(IntPtr functype);

            [DllImport(Engine.LibraryName)]
            public static unsafe extern IntPtr wasmtime_trap_new(byte* bytes, nuint len);
        }

        private readonly IStore? store;
        internal readonly ExternFunc func;
        internal readonly List<ValueKind> parameters = new List<ValueKind>();
        internal readonly List<ValueKind> results = new List<ValueKind>();
        internal static readonly Native.Finalizer Finalizer = (p) => GCHandle.FromIntPtr(p).Free();

        /// <summary>
        /// Contains the cause for a error returned by invoking a wasm function, in case
        /// the error was caused by the host. 
        /// </summary>
        /// <remarks>
        /// This thread-local field will be set when catching a .NET exception at the
        /// wasm-to-host transition. When the error bubbles up to the next host-to-wasm
        /// transition, the field needs to be cleared, and its value can be used to set
        /// the inner exception of the created <see cref="WasmtimeException"/>.
        /// </remarks>
        [ThreadStatic]
        internal static Exception? CallbackErrorCause;

        private static readonly Function _null = new Function();
        private static readonly object?[] NullParams = new object?[1];
    }
}
