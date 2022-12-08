using System;
using System.IO;
using Wasmtime;

namespace Wasmtime.Tests
{
    public abstract class StoreFixture : IDisposable
    {
        protected StoreFixture()
        {
            Engine = new Engine();
            Store = new Store(Engine);
        }

        public void Dispose()
        {
            Store?.Dispose();
            Store = null;

            Engine?.Dispose();
            Engine = null;
        }

        public Engine Engine { get; set; }
        public Store Store { get; set; }
    }
}
