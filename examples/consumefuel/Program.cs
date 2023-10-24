using System;
using Wasmtime;

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
        var remaining = Math.Max(caller.GetFuel(), 1000UL) - 1000UL;
        caller.SetFuel(remaining);
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

store.SetFuel(5000UL);
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
catch (WasmtimeException ex)
{
    Console.WriteLine("Exception caught with the following message:");
    Console.WriteLine(ex.Message);
}
