using System;
using Wasmtime;

namespace HelloExample
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, "table.wat");
            using var store = new Store(engine);
            using var host = new Host(store);

            using var table = host.DefineTable<Function>("", "table", null, 4);

            table[0] = Function.FromCallback(store, (int a, int b) => a + b);
            table[1] = Function.FromCallback(store, (int a, int b) => a - b);
            table[2] = Function.FromCallback(store, (int a, int b) => a * b);
            table[3] = Function.FromCallback(store, (int a, int b) => a / b);

            using dynamic instance = host.Instantiate(module);

            Console.WriteLine($"100 + 25 = {instance.call_indirect(0, 100, 25)}");
            Console.WriteLine($"100 - 25 = {instance.call_indirect(1, 100, 25)}");
            Console.WriteLine($"100 * 25 = {instance.call_indirect(2, 100, 25)}");
            Console.WriteLine($"100 / 25 = {instance.call_indirect(3, 100, 25)}");
        }
    }
}
