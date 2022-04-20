using System;
using System.IO;
using Wasmtime;

namespace Wasmtime.Tests
{
    public abstract class ModuleFixture : IDisposable
    {
        public ModuleFixture()
        {
            Engine = new Engine(GetEngineConfig());

            Module = Wasmtime.Module.FromTextFile(Engine, Path.Combine("Modules", ModuleFileName));
        }

        public virtual Config GetEngineConfig()
        {
            return new Config()
                .WithReferenceTypes(true);
        }

        public void Dispose()
        {
            if (!(Module is null))
            {
                Module.Dispose();
                Module = null;
            }

            if (!(Engine is null))
            {
                Engine.Dispose();
                Engine = null;
            }
        }

        public Engine Engine { get; set; }
        public Module Module { get; set; }

        protected abstract string ModuleFileName { get; }
    }
}
