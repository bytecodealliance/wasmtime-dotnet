using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class WasiTests
    {
        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItHasNoEnvironmentByDefault(string path)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(new WasiConfiguration());
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_environ_sizes_get = instance.GetFunction(store, "call_environ_sizes_get");
            call_environ_sizes_get.Should().NotBeNull();

            Assert.Equal(0, call_environ_sizes_get.Invoke(store, 0, 4));
            Assert.Equal(0, memory.ReadInt32(store, 0));
            Assert.Equal(0, memory.ReadInt32(store, 4));
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItHasSpecifiedEnvironment(string path)
        {
            var env = new Dictionary<string, string>() {
                {"FOO", "BAR"},
                {"WASM", "IS"},
                {"VERY", "COOL"},
            };

            var config = new WasiConfiguration()
                .WithEnvironmentVariables(env.Select(kvp => (kvp.Key, kvp.Value)));

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(config);
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_environ_sizes_get = instance.GetFunction(store, "call_environ_sizes_get");
            call_environ_sizes_get.Should().NotBeNull();
            var call_environ_get = instance.GetFunction(store, "call_environ_get");
            call_environ_sizes_get.Should().NotBeNull();

            Assert.Equal(0, call_environ_sizes_get.Invoke(store, 0, 4));
            Assert.Equal(env.Count, memory.ReadInt32(store, 0));
            Assert.Equal(env.Sum(kvp => kvp.Key.Length + kvp.Value.Length + 2), memory.ReadInt32(store, 4));
            Assert.Equal(0, call_environ_get.Invoke(store, 0, 4 * env.Count));

            for (int i = 0; i < env.Count; ++i)
            {
                var kvp = memory.ReadNullTerminatedString(store, memory.ReadInt32(store, i * 4)).Split("=");
                Assert.Equal(env[kvp[0]], kvp[1]);
            }
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItInheritsEnvironment(string path)
        {
            var config = new WasiConfiguration()
                .WithInheritedEnvironment();

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(config);
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_environ_sizes_get = instance.GetFunction(store, "call_environ_sizes_get");
            call_environ_sizes_get.Should().NotBeNull();

            Assert.Equal(0, call_environ_sizes_get.Invoke(store, 0, 4));
            Assert.Equal(Environment.GetEnvironmentVariables().Keys.Count, memory.ReadInt32(store, 0));
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItHasNoArgumentsByDefault(string path)
        {
            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(new WasiConfiguration());
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_args_sizes_get = instance.GetFunction(store, "call_args_sizes_get");
            call_args_sizes_get.Should().NotBeNull();

            Assert.Equal(0, call_args_sizes_get.Invoke(store, 0, 4));
            Assert.Equal(0, memory.ReadInt32(store, 0));
            Assert.Equal(0, memory.ReadInt32(store, 4));
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItHasSpecifiedArguments(string path)
        {
            var args = new List<string>() {
                "WASM",
                "IS",
                "VERY",
                "COOL"
            };

            var config = new WasiConfiguration()
                .WithArgs(args);

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(config);
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_args_sizes_get = instance.GetFunction(store, "call_args_sizes_get");
            call_args_sizes_get.Should().NotBeNull();
            var call_args_get = instance.GetFunction(store, "call_args_get");
            call_args_get.Should().NotBeNull();

            Assert.Equal(0, call_args_sizes_get.Invoke(store, 0, 4));
            Assert.Equal(args.Count, memory.ReadInt32(store, 0));
            Assert.Equal(args.Sum(a => a.Length + 1), memory.ReadInt32(store, 4));
            Assert.Equal(0, call_args_get.Invoke(store, 0, 4 * args.Count));

            for (int i = 0; i < args.Count; ++i)
            {
                var arg = memory.ReadNullTerminatedString(store, memory.ReadInt32(store, i * 4));
                Assert.Equal(args[i], arg);
            }
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItInheritsArguments(string path)
        {
            var config = new WasiConfiguration()
                .WithInheritedArgs();

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(config);
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_args_sizes_get = instance.GetFunction(store, "call_args_sizes_get");
            call_args_sizes_get.Should().NotBeNull();

            Assert.Equal(0, call_args_sizes_get.Invoke(store, 0, 4));
            Assert.Equal(Environment.GetCommandLineArgs().Length, memory.ReadInt32(store, 0));
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItSetsStdIn(string path)
        {
            const string MESSAGE = "WASM IS VERY COOL";

            using var file = new TempFile();
            File.WriteAllText(file.Path, MESSAGE);

            var config = new WasiConfiguration()
                .WithStandardInput(file.Path);

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(config);
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_fd_read = instance.GetFunction(store, "call_fd_read");
            call_fd_read.Should().NotBeNull();

            memory.WriteInt32(store, 0, 8);
            memory.WriteInt32(store, 4, MESSAGE.Length);

            Assert.Equal(0, call_fd_read.Invoke(store, 0, 0, 1, 32));
            Assert.Equal(MESSAGE.Length, memory.ReadInt32(store, 32));
            Assert.Equal(MESSAGE, memory.ReadString(store, 8, MESSAGE.Length));
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat", 1)]
        [InlineData("WasiSnapshot0.wat", 2)]
        [InlineData("Wasi.wat", 1)]
        [InlineData("Wasi.wat", 2)]
        public void ItSetsStdOutAndStdErr(string path, int fd)
        {
            const string MESSAGE = "WASM IS VERY COOL";

            using var file = new TempFile();

            var config = new WasiConfiguration();
            if (fd == 1)
            {
                config.WithStandardOutput(file.Path);
            }
            else if (fd == 2)
            {
                config.WithStandardError(file.Path);
            }

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(config);
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_fd_write = instance.GetFunction(store, "call_fd_write");
            call_fd_write.Should().NotBeNull();
            var call_fd_close = instance.GetFunction(store, "call_fd_close");
            call_fd_close.Should().NotBeNull();

            memory.WriteInt32(store, 0, 8);
            memory.WriteInt32(store, 4, MESSAGE.Length);
            memory.WriteString(store, 8, MESSAGE);

            Assert.Equal(0, call_fd_write.Invoke(store, fd, 0, 1, 32));
            Assert.Equal(MESSAGE.Length, memory.ReadInt32(store, 32));
            Assert.Equal(0, call_fd_close.Invoke(store, fd));
            Assert.Equal(MESSAGE, File.ReadAllText(file.Path));
        }

        [Theory]
        [InlineData("WasiSnapshot0.wat")]
        [InlineData("Wasi.wat")]
        public void ItSetsPreopenDirectories(string path)
        {
            const string MESSAGE = "WASM IS VERY COOL";

            using var file = new TempFile();

            var config = new WasiConfiguration()
                .WithPreopenedDirectory(Path.GetDirectoryName(file.Path), "/foo");

            using var engine = new Engine();
            using var module = Module.FromTextFile(engine, Path.Combine("Modules", path));
            using var store = new Store(engine);
            using var linker = new Linker(engine);

            linker.DefineWasi();

            store.SetWasiConfiguration(config);
            var instance = linker.Instantiate(store, module);

            var memory = instance.GetMemory(store, "memory");
            memory.Should().NotBeNull();
            var call_path_open = instance.GetFunction(store, "call_path_open");
            call_path_open.Should().NotBeNull();
            var call_fd_write = instance.GetFunction(store, "call_fd_write");
            call_fd_write.Should().NotBeNull();
            var call_fd_close = instance.GetFunction(store, "call_fd_close");
            call_fd_close.Should().NotBeNull();

            var fileName = Path.GetFileName(file.Path);
            memory.WriteString(store, 0, fileName);

            Assert.Equal(0, call_path_open.Invoke(
                    store,
                    3,
                    0,
                    0,
                    fileName.Length,
                    0,
                    0x40 /* RIGHTS_FD_WRITE */,
                    0,
                    0,
                    64
                )
            );

            var fileFd = (int)memory.ReadInt32(store, 64);
            Assert.True(fileFd > 3);

            memory.WriteInt32(store, 0, 8);
            memory.WriteInt32(store, 4, MESSAGE.Length);
            memory.WriteString(store, 8, MESSAGE);

            Assert.Equal(0, call_fd_write.Invoke(store, fileFd, 0, 1, 64));
            Assert.Equal(MESSAGE.Length, memory.ReadInt32(store, 64));
            Assert.Equal(0, call_fd_close.Invoke(store, fileFd));
            Assert.Equal(MESSAGE, File.ReadAllText(file.Path));
        }
    }
}
