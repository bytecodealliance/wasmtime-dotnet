using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new EngineBuilder().WithModuleLinking(true).Build();
            using var name = Module.FromTextFile(engine, "name.wat");
            using var program = Module.FromTextFile(engine, "program.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            var context = store.Context;

            var memory = new Memory(context, 1);
            linker.Define("", "mem", memory);
            linker.Define("", "inst", linker.Instantiate(context, name));
            linker.Define("", "print", Function.FromCallback(context, (Caller caller, int addr, int len) =>
            {
                Console.WriteLine(caller.GetMemory("mem").ReadString(caller.Context, addr, len));
            }));

            var instance = linker.Instantiate(context, program);

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
