using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        static void Main(string[] args)
        {
            using var engine = new Engine(new Config()
                .WithFuelConsumption(true));
            using var module = Module.FromTextFile(engine, "consumefuel.wat");
            using var linker = new Linker(engine);
            using var store = new Store(engine);

            linker.Define(
                "",
                "expensive",
                Function.FromCallback(store, (Caller caller) =>
                {
                    var remaining = caller.ConsumeFuel(1000UL);
                    Console.WriteLine($"Called an expensive function which consumed 1000 fuel. {remaining} units of fuel remaining.");
                }
            ));

            var instance = linker.Instantiate(store, module);

            var expensive = instance.GetAction("expensive");
            if (expensive is null)
            {
                Console.WriteLine("error: expensive export is missing");
                return;
            }

            store.AddFuel(5000UL);
            Console.WriteLine("Added 5000 units of fuel");

            for (var i = 0; i < 4; i++)
            {
                expensive();
            }

            Console.WriteLine("Calling the expensive function one more time, which will throw an exception.");
            try
            {
                expensive();
            }
            catch (TrapException ex)
            {
                Console.WriteLine("Exception caught with the following message:");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
