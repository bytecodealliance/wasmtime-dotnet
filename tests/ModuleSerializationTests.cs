using System;
using System.IO;
using FluentAssertions;
using Xunit;

namespace Wasmtime.Tests
{
    public class ModuleSerializationTests
    {
        [Fact]
        public void ItSerializesAndDeserializesAModule()
        {
            using var engine = new Engine();

            using var original = Module.FromText(engine, "test", "(module)");

            var bytes = original.Serialize();
            bytes.Should().NotBeNull();
            bytes.Length.Should().NotBe(0);

            using var deserialized = Module.Deserialize(engine, "test", bytes);
            deserialized.Should().NotBeNull();
        }

        [Fact]
        public void ItDeserializesFromAFile()
        {
            using var engine = new Engine();

            using var original = Module.FromText(engine, "test", "(module)");

            var bytes = original.Serialize();
            bytes.Should().NotBeNull();
            bytes.Length.Should().NotBe(0);

            var path = Path.GetTempFileName();

            File.WriteAllBytes(path, bytes);

            try
            {
                using var deserialized = Module.DeserializeFile(engine, "test", path);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
