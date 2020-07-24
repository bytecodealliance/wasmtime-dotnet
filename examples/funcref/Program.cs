using System;
using Wasmtime;

namespace HelloExample
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new EngineBuilder()
                .WithReferenceTypes(true)
                .Build();

            using var module = Module.FromTextFile(engine, "funcref.wat");
            using var store = new Store(engine);
            using var host = new Host(store);

            using var g = host.DefineFunction("", "g", (Function h) => { h.Invoke(); });
            using var i = host.DefineFunction("", "i", () => Console.WriteLine("Called via a function reference!"));

            using var func1 = Function.FromCallback(store, (string s) => Console.WriteLine($"First callback: {s}"));
            using var func2 = Function.FromCallback(store, (string s) => Console.WriteLine($"Second callback: {s}"));

            using dynamic instance = host.Instantiate(module);
            instance.call(func1, "Hello");
            instance.call(func2, "world!");
            instance.f();
        }
    }
}
