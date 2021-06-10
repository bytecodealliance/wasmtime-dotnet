using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "global.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            var context = store.Context;

            var global = new Global(context, ValueKind.Int32, 1, Mutability.Mutable);

            linker.Define("", "global", global);

            linker.Define(
                "",
                "print_global",
                Function.FromCallback(context, (Caller caller) =>
                {
                    Console.WriteLine($"The value of the global is: {global.GetValue(caller.Context)}.");
                }
            ));

            var instance = linker.Instantiate(context, module);

            var run = instance.GetFunction(context, "run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }

            run.Invoke(context, 20);
        }
    }
}
