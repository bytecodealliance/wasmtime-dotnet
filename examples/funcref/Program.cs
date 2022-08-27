using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine(new Config().WithReferenceTypes(true));
            using var module = Module.FromTextFile(engine, "funcref.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);

            linker.Define(
                "",
                "g",
                Function.FromCallback(store, (Caller caller, Function h) => { h.Invoke(); })
            );

            linker.Define(
                "",
                "i",
                Function.FromCallback(store, () => Console.WriteLine("Called via a function reference!"))
            );

            var func1 = Function.FromCallback(store, (string s) => Console.WriteLine($"First callback: {s}"));
            var func2 = Function.FromCallback(store, (string s) => Console.WriteLine($"Second callback: {s}"));

            var instance = linker.Instantiate(store, module);

            var call = instance.GetAction<Function, string>("call");
            if (call is null)
            {
                Console.WriteLine("error: `call` export is missing");
                return;
            }

            var f = instance.GetAction("f");
            if (f is null)
            {
                Console.WriteLine("error: `f` export is missing");
                return;
            }

            call(func1, "Hello");
            call(func2, "Hello");
            f();
        }
    }
}
