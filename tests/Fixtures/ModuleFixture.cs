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
            using (FileStream fs = File.OpenRead(modulePath))
            {
                StreamModule = Host.LoadModuleText(modulePath, fs);
            }
        }

        public void Dispose()
        {
            if (!(Module is null))
            {
                Module.Dispose();
                Module = null;
            }

            if (!(StreamModule is null))
            {
                StreamModule.Dispose();
                StreamModule = null;
            }
            
            if (!(Host is null))
            {
                Host.Dispose();
                Host = null;
            }
        }

        public Host Host { get; set; }
        public Module Module { get; set; }
        public Module StreamModule { get; set; }

        protected abstract string ModuleFileName { get; }
    }
}
