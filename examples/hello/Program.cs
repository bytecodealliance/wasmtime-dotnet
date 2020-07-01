using System;
using Wasmtime;

namespace HelloExample
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine();
            using var store = new Store(engine);
            using var module = store.LoadModuleText("hello.wat");
            using var host = new Host(engine);

            host.DefineFunction(
                "",
                "hello",
                () => Console.WriteLine("Hello from C#, WebAssembly!")
            );

            using dynamic instance = host.Instantiate(module);
            instance.run();
        }
    }
}
