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
            using var host = new Host(engine);

            using var function = host.DefineFunction(
                "",
                "log",
                (Caller caller, int address, int length) =>
                {
                    var message = caller.GetMemory("mem").ReadString(address, length);
                    Console.WriteLine($"Message from WebAssembly: {message}");
                }
            );

            using dynamic instance = host.Instantiate(module);
            instance.run();
        }
    }
}
