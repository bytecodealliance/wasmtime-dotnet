using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new EngineBuilder().WithModuleLinking(true).Build();
            using var module = Module.FromTextFile(engine, "name.wat");
            using var program = Module.FromTextFile(engine, "program.wat");

            using var store = new Store(engine);
            using var host = new Host(store);

            using var memory = host.DefineMemory("", "mem");

            host.DefineInstance("", "inst", module.Instantiate(store, memory));

            host.DefineFunction("", "print", (Caller caller, int addr, int len) =>
            {
                Console.WriteLine(caller.GetMemory("mem").ReadString(addr, len));
            });

            using dynamic instance = host.Instantiate(program);

            instance.run();
        }
    }
}
