using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine(new Config().WithModuleLinking(true));
            using var name = Module.FromTextFile(engine, "name.wat");
            using var program = Module.FromTextFile(engine, "program.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);

            var memory = new Memory(store, 1);
            linker.Define("", "mem", memory);
            linker.Define("", "inst", linker.Instantiate(store, name));
            linker.Define("", "print", Function.FromCallback(store, (Caller caller, int addr, int len) =>
            {
                Console.WriteLine(caller.GetMemory("mem").ReadString(caller, addr, len));
            }));

            var instance = linker.Instantiate(store, program);

            var run = instance.GetFunction(store, "run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }

            run.Invoke(store);
        }
    }
}
