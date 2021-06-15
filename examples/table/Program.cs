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

            var table = new Table(store, ValueKind.FuncRef, null, 4);

            table.SetElement(store, 0, Function.FromCallback(store, (int a, int b) => a + b));
            table.SetElement(store, 1, Function.FromCallback(store, (int a, int b) => a - b));
            table.SetElement(store, 2, Function.FromCallback(store, (int a, int b) => a * b));
            table.SetElement(store, 3, Function.FromCallback(store, (int a, int b) => a / b));

            linker.Define("", "table", table);

            var instance = linker.Instantiate(store, module);

            var call_indirect = instance.GetFunction(store, "call_indirect");
            if (call_indirect is null)
            {
                Console.WriteLine("error: `call_indirect` export is missing");
                return;
            }

            Console.WriteLine($"100 + 25 = {call_indirect.Invoke(store, 0, 100, 25)}");
            Console.WriteLine($"100 - 25 = {call_indirect.Invoke(store, 1, 100, 25)}");
            Console.WriteLine($"100 * 25 = {call_indirect.Invoke(store, 2, 100, 25)}");
            Console.WriteLine($"100 / 25 = {call_indirect.Invoke(store, 3, 100, 25)}");
        }
    }
}
