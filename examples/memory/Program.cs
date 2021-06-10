using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "memory.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            var context = store.Context;

            linker.Define(
                "",
                "log",
                Function.FromCallback(context, (Caller caller, int address, int length) =>
                {
                    var message = caller.GetMemory("mem").ReadString(caller.Context, address, length);
                    Console.WriteLine($"Message from WebAssembly: {message}");
                }
            ));

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
