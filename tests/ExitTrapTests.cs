using System.IO;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ExitTrapTests
    {
        [Theory]
        [InlineData("ExitTrap.wat", 0)]
        [InlineData("ExitTrap.wat", 1)]
        [InlineData("ExitTrap.wat", -1)]
        public void ItReturnsExitCode(string path, int exitCode)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(new WasiConfiguration());
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory("memory");
            memory.Should().NotBeNull();

            var exit = instance.GetAction<int>("exit")!;
            exit.Should().NotBeNull();

            try
            {
                exit(exitCode);
                Assert.False(bool.Parse(bool.TrueString));
            }
            catch (TrapException ex)
            {
                if (exitCode < 0)
                {
                    Assert.Null(ex.ExitCode);
                    Assert.StartsWith("exit with invalid exit status", ex.Message);
                }
                else
                {
                    Assert.NotNull(ex.ExitCode);
                    Assert.Equal(exitCode, ex.ExitCode);
                    Assert.StartsWith($"Exited with i32 exit status {exitCode}\n", ex.Message);
                }
            }
        }
    }
}
