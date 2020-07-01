using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Wasmtime;

namespace Simple
{
    public class Benchmark
    {
        public Benchmark()
        {
            _store = new Store();
            _module = _store.LoadModuleText("hello", @"
(module
  (type $t0 (func))
  (import """" ""hello"" (func $.hello (type $t0)))
  (func $run
    call $.hello
  )
  (export ""run"" (func $run))
)");
        }

        [Benchmark]
        public void SayHello()
        {
            using var host = new Host(_store);

            host.DefineFunction("", "hello", () => {});

            // Define a memory to add memory pressure if not disposed in the benchmark test
            using var memory = host.DefineMemory("", "memory", 3);

            using dynamic instance = host.Instantiate(_module);
            instance.run();
        }

        private Store _store;
        private Module _module;
    }

    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<Benchmark>();
        }
    }
}
