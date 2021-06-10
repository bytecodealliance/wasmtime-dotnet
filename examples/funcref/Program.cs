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

            using var module = Module.FromTextFile(engine, "funcref.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            var context = store.Context;

            linker.Define(
                "",
                "g",
                Function.FromCallback(context, (Caller caller, Function h) => { h.Invoke(caller.Context); })
            );

            linker.Define(
                "",
                "i",
                Function.FromCallback(context, () => Console.WriteLine("Called via a function reference!"))
            );

            var func1 = Function.FromCallback(context, (string s) => Console.WriteLine($"First callback: {s}"));
            var func2 = Function.FromCallback(context, (string s) => Console.WriteLine($"Second callback: {s}"));

            var instance = linker.Instantiate(context, module);

            var call = instance.GetFunction(context, "call");
            if (call is null)
            {
                Console.WriteLine("error: `call` export is missing");
                return;
            }

            var f = instance.GetFunction(context, "f");
            if (f is null)
            {
                Console.WriteLine("error: `f` export is missing");
                return;
            }

            call.Invoke(context, func1, "Hello");
            call.Invoke(context, func2, "Hello");
            f.Invoke(context);
        }
    }
}
