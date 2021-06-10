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
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            var context = store.Context;

            linker.Define(
                "",
                "concat",
                Function.FromCallback(context, (string a, string b) => $"{a} {b}")
            );

            var instance = linker.Instantiate(context, module);

            var run = instance.GetFunction(context, "run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }

            Console.WriteLine(run.Invoke(context, "hello", "world!"));
        }
    }
}
