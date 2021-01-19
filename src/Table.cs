using System;
using System.Diagnostics.CodeAnalysis;

namespace Wasmtime
{
    /// <summary>
    /// Represents a WebAssembly table.
    /// </summary>
    public class Table<T> : IDisposable, IImportable where T : class
    {
        /// <summary>
        /// Gets the value kind of the table.
        /// </summary>
        /// <value></value>
        public ValueKind Kind { get; private set; }

        /// <summary>
        /// The minimum table element size.
        /// </summary>
        public uint Minimum { get; private set; }

        /// <summary>
        /// The maximum table element size.
        /// </summary>
        public uint Maximum { get; private set; }

        /// <summary>
        /// Gets or sets a value in the table at the given index.
        /// </summary>
        /// <value>The value to set in the table.</value>
        public T? this[uint index]
        {
            get
            {
                CheckDisposed();

                unsafe
                {
                    using var reference = Interop.wasm_table_get(Handle.DangerousGetHandle(), index);
                    return (T?)Interop.ToObject(reference.DangerousGetHandle(), Kind);
                }
            }
            set
            {
                CheckDisposed();

                unsafe
                {
                    var val = Interop.ToValue(value, Kind);

                    var success = Interop.wasm_table_set(Handle.DangerousGetHandle(), index, val.of.reference);

                    Interop.DeleteValue(&val);

                    if (!success)
                    {
                        throw new IndexOutOfRangeException("The specified index is out of bounds.");
                    }
                }
            }
        }

        /// <summary>
        /// Gets the current size of the table.
        /// </summary>
        /// <value>Returns the current size of the table.</value>
        public uint Size
        {
            get
            {
                CheckDisposed();
                return Interop.wasm_table_size(Handle.DangerousGetHandle());
            }
        }

        /// <summary>
        /// Grows the table by the given number of elements.
        /// </summary>
        /// <param name="delta">The number of elements to grow the table.</param>
        /// <param name="initialValue">The initial value for the new elements.</param>
        /// <returns>Returns true if the table grew successfully or false if the table cannot grow to the requested size.</returns>
        public bool Grow(uint delta, T initialValue)
        {
            CheckDisposed();

            var value = Interop.ToValue((object)initialValue, Kind);

            var ret = Interop.wasm_table_grow(Handle.DangerousGetHandle(), delta, value.of.reference);

            unsafe { Interop.DeleteValue(&value); }

            return ret;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!Handle.IsInvalid)
            {
                Handle.Dispose();
                Handle.SetHandleAsInvalid();
            }
        }

        IntPtr IImportable.GetHandle()
        {
            return Interop.wasm_table_as_extern(Handle.DangerousGetHandle());
        }

        internal Table(Interop.StoreHandle store, T? initialValue, uint initial, uint maximum)
        {
            if (!Interop.TryGetValueKind(typeof(T), out var kind))
            {
                throw new WasmtimeException($"Table elements cannot be of type '{typeof(T).ToString()}'.");
            }

            if (kind != ValueKind.ExternRef && kind != ValueKind.FuncRef)
            {
                throw new WasmtimeException($"Table elements cannot be of type '{typeof(T).ToString()}'.");
            }

            if (initial == 0)
            {
                throw new ArgumentException("The initial number of elements cannot be zero.", nameof(initial));
            }

            if (maximum < initial)
            {
                throw new ArgumentException("The maximum number of elements cannot be less than the minimum.", nameof(maximum));
            }

            Kind = kind;
            Minimum = initial;
            Maximum = maximum;

            unsafe
            {
                var value = Interop.ToValue((object?)initialValue, Kind);

                var valueType = Interop.wasm_valtype_new(value.kind);
                var valueTypeHandle = valueType.DangerousGetHandle();
                valueType.SetHandleAsInvalid();

                Interop.wasm_limits_t limits = new Interop.wasm_limits_t();
                limits.min = initial;
                limits.max = maximum;

                using var tableType = Interop.wasm_tabletype_new(valueTypeHandle, &limits);
                Handle = Interop.wasm_table_new(store, tableType, value.of.reference);

                Interop.DeleteValue(&value);

                if (Handle.IsInvalid)
                {
                    throw new WasmtimeException("Failed to create Wasmtime table.");
                }
            }
        }

        private void CheckDisposed()
        {
            if (Handle.IsInvalid)
            {
                throw new ObjectDisposedException(typeof(Table<T>).FullName);
            }
        }

        internal Interop.TableHandle Handle { get; set; }
    }
}
