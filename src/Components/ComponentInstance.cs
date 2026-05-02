using System;

namespace Wasmtime.Components
{
    /// <summary>
    /// Represents an instantiated <see cref="Component"/> within a <see cref="Wasmtime.Store"/>.
    /// </summary>
    /// <remarks>
    /// A <see cref="ComponentInstance"/> has the same lifetime as the <see cref="Wasmtime.Store"/>
    /// it was created in: it is automatically reclaimed when the store is disposed and does not
    /// require explicit cleanup.
    /// </remarks>
    public class ComponentInstance
    {
        internal ComponentInstance(Store store, WasmtimeComponentInstance instance)
        {
            Store = store;
            this.instance = instance;
        }

        /// <summary>
        /// The store this instance lives in.
        /// </summary>
        public Store Store { get; }

        internal WasmtimeComponentInstance Raw => instance;

        private readonly WasmtimeComponentInstance instance;
    }
}
