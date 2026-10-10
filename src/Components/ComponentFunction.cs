using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Wasmtime.Components;

/// <summary>
/// An exported function of a <see cref="ComponentInstance"/>.
/// </summary>
public class ComponentFunction
{
    /// <summary>Size of <c>wasmtime_component_valtype_t</c>: a 1-byte kind padded to 8, plus an 8-byte union.</summary>
    private const int ValueTypeSize = 16;

    private readonly Store store;
    private readonly Native.Func func;

    internal ComponentFunction(Store store, Native.Func func)
    {
        this.store = store;
        this.func = func;

        var type = Native.wasmtime_component_func_type(in func, store.Context.handle);
        GC.KeepAlive(store);

        try
        {
            ParameterCount = (int)Native.wasmtime_component_func_type_param_count(type);
            HasResult = ReadHasResult(type);
        }
        finally
        {
            Native.wasmtime_component_func_type_delete(type);
        }
    }

    /// <summary>
    /// Gets the number of parameters this function takes.
    /// </summary>
    public int ParameterCount { get; }

    /// <summary>
    /// Gets whether this function returns a value.
    /// </summary>
    /// <remarks>
    /// A component function has at most one result. Note that a WIT function declared to return
    /// <c>result&lt;_, E&gt;</c> still has one result here, even though bindings generators
    /// usually surface it as returning nothing.
    /// </remarks>
    public bool HasResult { get; }

    /// <summary>
    /// Invokes the function.
    /// </summary>
    /// <param name="arguments">The arguments, which must match <see cref="ParameterCount"/>.</param>
    /// <returns>The result, or null if the function does not return one.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="arguments"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the wrong number of arguments is given.</exception>
    /// <exception cref="WasmtimeException">Thrown if the function traps or fails.</exception>
    public ComponentValue? Call(params ComponentValue[] arguments)
    {
        return Call((IReadOnlyList<ComponentValue>)arguments);
    }

    /// <summary>
    /// Invokes the function.
    /// </summary>
    /// <param name="arguments">The arguments, which must match <see cref="ParameterCount"/>.</param>
    /// <returns>The result, or null if the function does not return one.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="arguments"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the wrong number of arguments is given.</exception>
    /// <exception cref="WasmtimeException">Thrown if the function traps or fails.</exception>
    /// <remarks>
    /// A trap leaves the whole <see cref="Store"/> unusable for further component calls, which
    /// then fail with "cannot enter component instance". This applies to every instance in the
    /// store, not just this one, so a store cannot be reused after a trap.
    /// </remarks>
    public ComponentValue? Call(IReadOnlyList<ComponentValue> arguments)
    {
        if (arguments is null)
        {
            throw new ArgumentNullException(nameof(arguments));
        }

        var argumentCount = arguments.Count;
        if (argumentCount != ParameterCount)
        {
            throw new ArgumentException(
                $"The function takes {ParameterCount} argument(s) but {argumentCount} were given.",
                nameof(arguments));
        }

        var resultCount = HasResult ? 1 : 0;

        using (var scope = new ComponentValueMarshaller.AllocationScope())
        {
            var argumentBuffer = IntPtr.Zero;
            if (argumentCount > 0)
            {
                argumentBuffer = scope.Allocate(argumentCount * ComponentValueMarshaller.ValueSize);
                for (var i = 0; i < argumentCount; i++)
                {
                    if (arguments[i] is null)
                    {
                        throw new ArgumentException($"Argument {i} is null.", nameof(arguments));
                    }

                    ComponentValueMarshaller.Write(
                        arguments[i],
                        argumentBuffer + (i * ComponentValueMarshaller.ValueSize),
                        scope);
                }
            }

            // Wasmtime allocates the contents of the results, so the buffer must start zeroed and
            // must only be deleted if the call actually wrote to it.
            var resultBuffer = resultCount == 0
                ? IntPtr.Zero
                : scope.Allocate(resultCount * ComponentValueMarshaller.ValueSize);

            var error = Native.wasmtime_component_func_call(
                in func,
                store.Context.handle,
                argumentBuffer,
                (nuint)argumentCount,
                resultBuffer,
                (nuint)resultCount);

            GC.KeepAlive(store);

            if (error != IntPtr.Zero)
            {
                throw WasmtimeException.FromOwnedError(error);
            }

            if (resultCount == 0)
            {
                return null;
            }

            try
            {
                return ComponentValueMarshaller.Read(resultBuffer);
            }
            finally
            {
                ComponentValueNative.wasmtime_component_val_delete(resultBuffer);
            }
        }
    }

    private static bool ReadHasResult(IntPtr type)
    {
        Span<byte> buffer = stackalloc byte[ValueTypeSize];
        buffer.Clear();

        unsafe
        {
            fixed (void* bufferPtr = buffer)
            {
                if (!Native.wasmtime_component_func_type_result(type, (IntPtr)bufferPtr))
                {
                    return false;
                }

                Native.wasmtime_component_valtype_delete((IntPtr)bufferPtr);
                return true;
            }
        }
    }

    internal static class Native
    {
        /// <summary>
        /// Mirrors <c>wasmtime_component_func_t</c>. The first two fields sit in an anonymous
        /// struct, so the trailing field lands at offset 16 and the whole thing is 24 bytes.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 24)]
        internal struct Func
        {
            [FieldOffset(0)]
            public ulong StoreId;

            [FieldOffset(8)]
            public uint Private1;

            [FieldOffset(16)]
            public uint Private2;
        }

        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_component_func_call(
            in Func func,
            IntPtr context,
            IntPtr args,
            nuint args_size,
            IntPtr results,
            nuint results_size);

        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_component_func_type(in Func func, IntPtr context);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_component_func_type_delete(IntPtr ty);

        [DllImport(Engine.LibraryName)]
        public static extern nuint wasmtime_component_func_type_param_count(IntPtr ty);

        [DllImport(Engine.LibraryName)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool wasmtime_component_func_type_result(IntPtr ty, IntPtr type_ret);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_component_valtype_delete(IntPtr ptr);
    }
}