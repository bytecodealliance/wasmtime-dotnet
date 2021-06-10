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
            _module = Module.FromText(
                _engine,
                "hello",
                @"
(module
  (type $t0 (func))
  (import """" ""hello"" (func $.hello (type $t0)))
  (func $run
    call $.hello
  )
  (export ""run"" (func $run))
)"
            );
        }

        [Benchmark]
        public void SayHello()
        {
            using var linker = new Linker(_engine);
            using var store = new Store(_engine);

            var context = store.Context;
            linker.Define("", "hello", Function.FromCallback(context ,() => { }));
            linker.Define("", "memory", new Memory(context, 3));

            var instance = linker.Instantiate(context, _module);
            var run = instance.GetFunction(context, "run");
            if (run == null)
            {
                throw new InvalidOperationException();
            }
            
            run.Invoke(context);
        }

        private Engine _engine;
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
