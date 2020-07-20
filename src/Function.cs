using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    /// <summary>
    /// Represents a host function.
    /// </summary>
    public class Function : IDisposable
    {
        /// <inheritdoc/>
        public void Dispose()
        {
            if (!Handle.IsInvalid)
            {
                Handle.Dispose();
                Handle.SetHandleAsInvalid();
            }
        }

        internal Function(Interop.StoreHandle store, Delegate func, bool hasReturn)
        {
            if (func is null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            var type = func.GetType();
            Span<Type> parameterTypes = null;
            Type returnType = null;

            if (hasReturn)
            {
                parameterTypes = type.GenericTypeArguments[0..^1];
                returnType = type.GenericTypeArguments[^1];
            }
            else
            {
                parameterTypes = type.GenericTypeArguments;
                returnType = null;
            }

            bool hasCaller = parameterTypes.Length > 0 && parameterTypes[0] == typeof(Caller);

            if (hasCaller)
            {
                parameterTypes = parameterTypes[1..];
            }

            var parameterKinds = GetParameterKinds(parameterTypes);
            var resultKinds = GetReturnTypeKinds(EnumerateReturnTypes(returnType));

            var parameters = CreateValueTypeVec(parameterKinds);
            var results = CreateValueTypeVec(resultKinds);
            using var funcType = Interop.wasm_functype_new(ref parameters, ref results);

            if (hasCaller)
            {
                var callback = CreateWasmtimeCallback(store, func, parameterTypes.Length, resultKinds);
                Handle = Interop.wasmtime_func_new_with_env(
                    store,
                    funcType,
                    callback,
                    GCHandle.ToIntPtr(GCHandle.Alloc(callback)),
                    Interop.GCHandleFinalizer
                );
            }
            else
            {
                var callback = CreateCallback(store, func, parameterTypes.Length, resultKinds);
                Handle = Interop.wasm_func_new_with_env(
                    store,
                    funcType,
                    callback,
                    GCHandle.ToIntPtr(GCHandle.Alloc(callback)),
                    Interop.GCHandleFinalizer
                );
            }

            if (Handle.IsInvalid)
            {
                throw new WasmtimeException("Failed to create Wasmtime function.");
            }
        }

        private static ValueKind[] GetParameterKinds(Span<Type> parameters)
        {
            var kinds = new ValueKind[parameters.Length];
            for (int i = 0; i < parameters.Length; ++i)
            {
                if (parameters[i] == typeof(Caller))
                {
                    throw new WasmtimeException($"A 'Caller' parameter must be the first parameter of the function.");
                }

                if (!Interop.TryGetValueKind(parameters[i], out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with parameter of type '{parameters[i].ToString()}'.");
                }

                kinds[i] = kind;
            }
            return kinds;
        }

        private static ValueKind[] GetReturnTypeKinds(IEnumerable<Type> returnTypes)
        {
            return returnTypes.Select(t =>
            {
                if (!Interop.TryGetValueKind(t, out var kind))
                {
                    throw new WasmtimeException($"Unable to create a function with a return type of type '{t.ToString()}'.");
                }
                return kind;
            }).ToArray();
        }

        private static IEnumerable<Type> EnumerateReturnTypes(Type returnType)
        {
            if (returnType is null)
            {
                yield break;
            }

            if (IsTuple(returnType))
            {
                foreach (var type in returnType
                    .GetGenericArguments()
                    .SelectMany(type =>
                        {
                            if (type.IsConstructedGenericType)
                            {
                                return type.GenericTypeArguments;
                            }
                            return Enumerable.Repeat(type, 1);
                        }))
                {
                    yield return type;
                }
            }
            else
            {
                yield return returnType;
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

        private static unsafe Interop.WasmFuncCallbackWithEnv CreateCallback(Interop.StoreHandle store, Delegate callback, int parameterCount, ValueKind[] resultKinds)
        {
            // NOTE: this capture is not thread-safe.
            var args = new object[parameterCount];

            return (env, arguments, results) =>
                InvokeCallback(store, callback, args, resultKinds, IntPtr.Zero, arguments, results);
        }

        private static unsafe Interop.WasmtimeFuncCallbackWithEnv CreateWasmtimeCallback(Interop.StoreHandle store, Delegate callback, int parameterCount, ValueKind[] resultKinds)
        {
            // NOTE: this capture is not thread-safe.
            var args = new object[parameterCount + 1];
            args[0] = new Caller();

            return (caller, env, arguments, results) =>
                InvokeCallback(store, callback, args, resultKinds, caller, arguments, results);
        }

        private static Interop.wasm_valtype_vec_t CreateValueTypeVec(IList<ValueKind> kinds)
        {
            Interop.wasm_valtype_vec_t vec;
            Interop.wasm_valtype_vec_new_uninitialized(out vec, (UIntPtr)kinds.Count);

            for (int i = 0; i < kinds.Count; ++i)
            {
                var valType = Interop.wasm_valtype_new((Interop.wasm_valkind_t)kinds[i]);
                unsafe
                {
                    vec.data[i] = valType.DangerousGetHandle();
                }
                valType.SetHandleAsInvalid();
            }

            return vec;
        }

        private unsafe static IntPtr InvokeCallback(
            Interop.StoreHandle store,
            Delegate callback,
            object[] args,
            ValueKind[] resultKinds,
            IntPtr caller,
            Interop.wasm_val_t* arguments,
            Interop.wasm_val_t* results)
        {
            try
            {
                int offset = 0;
                if (caller != IntPtr.Zero)
                {
                    offset = 1;
                    ((Caller)args[0]).Handle = caller;
                }

                for (int i = 0; i < args.Length - offset; ++i)
                {
                    args[i + offset] = Interop.ToObject(&arguments[i]);
                }

                var result = callback.Method.Invoke(callback.Target, BindingFlags.DoNotWrapExceptions, null, args, null);

                for (int i = 0; i < args.Length - offset; ++i)
                {
                    args[i + offset] = null;
                }

                if (caller != IntPtr.Zero)
                {
                    ((Caller)args[0]).Handle = IntPtr.Zero;
                }

                if (resultKinds.Length > 0)
                {
                    var tuple = result as ITuple;
                    if (tuple is null)
                    {
                        results[0] = Interop.ToValue(result, resultKinds[0]);
                    }
                    else
                    {
                        for (int i = 0; i < tuple.Length; ++i)
                        {
                            results[i] = Interop.ToValue(tuple[i], resultKinds[i]);
                        }
                    }
                }
                return IntPtr.Zero;
            }
            catch (Exception ex)
            {
                var bytes = Encoding.UTF8.GetBytes(ex.Message + "\0" /* exception messages need a null */);

                fixed (byte* ptr = bytes)
                {
                    Interop.wasm_byte_vec_t message = new Interop.wasm_byte_vec_t();
                    message.size = (UIntPtr)bytes.Length;
                    message.data = ptr;

                    return Interop.wasm_trap_new(store, ref message);
                }
            }
        }

        internal Interop.FunctionHandle Handle { get; private set; }
    }
}
