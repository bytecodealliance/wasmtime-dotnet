using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "table.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);
            var context = store.Context;

            var table = new Table(context, ValueKind.FuncRef, null, 4);

            table.SetElement(context, 0, Function.FromCallback(context, (int a, int b) => a + b));
            table.SetElement(context, 1, Function.FromCallback(context, (int a, int b) => a - b));
            table.SetElement(context, 2, Function.FromCallback(context, (int a, int b) => a * b));
            table.SetElement(context, 3, Function.FromCallback(context, (int a, int b) => a / b));

            linker.Define("", "table", table);

            var instance = linker.Instantiate(context, module);

            var call_indirect = instance.GetFunction(context, "call_indirect");
            if (call_indirect is null)
            {
                Console.WriteLine("error: `call_indirect` export is missing");
                return;
            }

            Console.WriteLine($"100 + 25 = {call_indirect.Invoke(context, 0, 100, 25)}");
            Console.WriteLine($"100 - 25 = {call_indirect.Invoke(context, 1, 100, 25)}");
            Console.WriteLine($"100 * 25 = {call_indirect.Invoke(context, 2, 100, 25)}");
            Console.WriteLine($"100 / 25 = {call_indirect.Invoke(context, 3, 100, 25)}");
        }
    }
}
