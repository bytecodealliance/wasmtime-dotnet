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
            using var module = store.LoadModuleText("global.wat");
            using var host = new Host(engine);

            var global = host.DefineMutableGlobal("", "global", 1);

            host.DefineFunction(
                "",
                "print_global",
                () => {
                    Console.WriteLine($"The value of the global is: {global.Value}.");
                }
            );

            using dynamic instance = host.Instantiate(module);
            instance.run(20);
        }
    }
}
