using System;
using Wasmtime;

namespace Example
{
    class Program
    {
        private class StoreData
        {
            public int Id { get; set; }

            public string Message { get; set; }

            public StoreData(string message, int id = 0)
            {
                Message = message;
                Id = id;
            }

            public override string ToString()
            {
                return $"ID {Id}: {Message}";
            }
        }

        static void Main(string[] args)
        {
            using var engine = new Engine(new Config().WithReferenceTypes(true));
            using var module = Module.FromTextFile(engine, "storedata.wat");
            using var linker = new Linker(engine);

            var storeData = new StoreData("Hello, WASM", 0);
            using var store = new Store(engine, storeData);

            linker.DefineFunction("", "store_data", (Caller caller) =>
            {
                var data = caller.GetData() as StoreData;

                Console.WriteLine($"ID {data.Id}: {data.Message}"); // 'ID 0: Hello, WASM' 

                // Fully replace store data
                var newStoreData = new StoreData("Store data replaced", 1);
                caller.SetData(newStoreData);
                data = caller.GetData() as StoreData;

                Console.WriteLine($"ID {data.Id}: {data.Message}"); // 'ID 1: Store data replaced' 

                // Change properties normally
                data.Message = "Properties changed";
                data.Id = 2;
            });

            var instance = linker.Instantiate(store, module);

            var run = instance.GetAction("run");
            if (run is null)
            {
                Console.WriteLine("error: run export is missing");
                return;
            }
            run();

            // Retrieve final data from store directly
            var data = store.GetData() as StoreData;
            Console.WriteLine($"ID {data.Id}: {data.Message}"); // 'ID 2: Properties changed' 
        }
    }
}
