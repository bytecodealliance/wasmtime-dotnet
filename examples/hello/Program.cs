using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "hello.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            var context = store.Context;

            linker.Define(
                "",
                "hello",
                Function.FromCallback(context, () => Console.WriteLine("Hello from C#, WebAssembly!"))
            );

            var instance = linker.Instantiate(context, module);

            var run = instance.GetFunction(context, "run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }

            run.Invoke(context);
        }
    }
}
