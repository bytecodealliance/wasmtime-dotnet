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

            var global = new Global(store, ValueKind.Int32, 1, Mutability.Mutable);

            linker.Define("", "global", global);

            linker.Define(
                "",
                "print_global",
                Function.FromCallback(store, (Caller caller) =>
                {
                    Console.WriteLine($"The value of the global is: {global.GetValue()}.");
                }
            ));

            var instance = linker.Instantiate(store, module);

            var run = instance.GetAction<int>("run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }

            run(20);
        }
    }
}
