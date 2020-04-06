using System;
using Wasmtime;

namespace HelloExample
{
    class Program
    {
        static void Main(string[] args)
        {
            using var host = new Host();

            host.DefineFunction(
                "",
                "log",
                (Caller caller, int address, int length) => {
                    var message = caller.GetMemory("mem").ReadString(address, length);
                    Console.WriteLine($"Message from WebAssembly: {message}");
                }
            );

            using var module = host.LoadModuleText("memory.wat");

            using dynamic instance = host.Instantiate(module);
            instance.run();
        }
    }
}
