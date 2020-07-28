using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new EngineBuilder()
                .WithReferenceTypes(true)
                .Build();

            using var module = Module.FromTextFile(engine, "externref.wat");

            using var host = new Host(engine);

            using var function = host.DefineFunction(
                "",
                "concat",
                (string a, string b) => $"{a} {b}"
            );

            using dynamic instance = host.Instantiate(module);
            Console.WriteLine(instance.run("Hello", "world!"));
        }
    }
}
