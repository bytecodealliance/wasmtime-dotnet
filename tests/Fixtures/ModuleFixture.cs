using System;
using System.IO;
using Wasmtime;

namespace Wasmtime.Tests
{
    public abstract class ModuleFixture : IDisposable
    {
        public ModuleFixture()
        {
            Host = new HostBuilder()
                .WithMultiValue(true)
                .WithReferenceTypes(true)
                .Build();

            var modulePath = Path.Combine("Modules", ModuleFileName);
            Module = Host.LoadModuleText(modulePath);
            EmbeddedModule = Host.LoadEmbeddedModuleText(modulePath);
        }

        public void Dispose()
        {
            if (!(Module is null))
            {
                Module.Dispose();
                Module = null;
            }

            if (!(EmbeddedModule is null))
            {
                EmbeddedModule.Dispose();
                EmbeddedModule = null;
            }
            
            if (!(Host is null))
            {
                Host.Dispose();
                Host = null;
            }
        }

        public Host Host { get; set; }
        public Module Module { get; set; }
        public Module EmbeddedModule { get; set; }

        protected abstract string ModuleFileName { get; }
    }
}
