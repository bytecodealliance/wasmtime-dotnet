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
            using var host = new Host(engine);

            using var function = host.DefineFunction(
                "",
                "hello",
                () => Console.WriteLine("Hello from C#, WebAssembly!")
            );

            using dynamic instance = host.Instantiate(module);
            instance.run();
        }
    }
}
