using System;
using System.IO;
using Wasmtime;

namespace Wasmtime.Tests
{
    public abstract class ModuleFixture : IDisposable
    {
        public ModuleFixture()
        {
            Store = new StoreBuilder()
                .WithMultiValue(true)
                .WithReferenceTypes(true)
                .Build();

            Module = Store.LoadModuleText(Path.Combine("Modules", ModuleFileName));
        }

        public void Dispose()
        {
            if (!(Module is null))
            {
                Module.Dispose();
                Module = null;
            }

            if (!(Store is null))
            {
                Store.Dispose();
                Store = null;
            }
        }

        public Store Store { get; set; }
        public Module Module { get; set; }

        protected abstract string ModuleFileName { get; }
    }
}
