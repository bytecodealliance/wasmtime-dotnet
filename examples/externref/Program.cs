using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine(new Config().WithReferenceTypes(true));
            using var module = Module.FromTextFile(engine, "externref.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);

            linker.Define(
                "",
                "concat",
                Function.FromCallback(store, (string a, string b) => $"{a} {b}")
            );

            var instance = linker.Instantiate(store, module);

            var run = instance.GetFunction<string, string, string>("run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }

            Console.WriteLine(run("hello", "world!"));
        }
    }
}
