using System;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Wasmtime;

namespace Simple
{
    [Config(typeof(Config))]
    public class Benchmark
    {
        private class Config : ManualConfig
        {
            public Config()
            {
                AddJob(Job.MediumRun
                    .WithLaunchCount(1)
                    .WithToolchain(InProcessEmitToolchain.Instance)
                    .WithId("InProcess"));
            }
        }

        public Benchmark()
        {
            _engine = new Engine();
            _store = new Store(_engine);
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
            using var host = new Host(_engine);

            host.DefineFunction("", "hello", () => { });

            // Define a memory to add memory pressure if not disposed in the benchmark test
            using var memory = host.DefineMemory("", "memory", 3);

            using dynamic instance = host.Instantiate(_module);
            instance.run();
        }

        private Engine _engine;
        private Store _store;
        private Module _module;
    }

    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Benchmark>();

            var report = summary[summary.BenchmarksCases.Where(c => c.Descriptor.Type == typeof(Benchmark)).Single()];
            if (!(report is null))
            {
                if (report.ExecuteResults.All(r => r.ExitCode == 0))
                {
                    return;
                }
            }

            Console.Error.WriteLine("Benchmark failed.");
            Environment.Exit(1);
        }
    }
}
