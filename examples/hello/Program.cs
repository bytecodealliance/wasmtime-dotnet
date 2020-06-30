using System;
using Wasmtime;

namespace HelloExample
{
    class Program
    {
        static void Main(string[] args)
        {
            using var store = new Store();
            using var module = store.LoadModuleText("hello.wat");
            using var host = new Host(store);

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
