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

            linker.Define(
                "",
                "log",
                Function.FromCallback(store, (Caller caller, int address, int length) =>
                {
                    var message = caller.GetMemory("mem").ReadString(address, length);
                    Console.WriteLine($"Message from WebAssembly: {message}");
                }
            ));

            var instance = linker.Instantiate(store, module);

            var run = instance.GetAction("run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }

            run();
        }
    }
}
