using System;
using System.Buffers;
using System.Text;

namespace Wasmtime
{
    internal static class StringExtensions
    {
        public static TemporaryAllocation ToUTF8(this string value, Span<byte> bytes)
        {
            return TemporaryAllocation.FromString(value, bytes);
        }
    }

    internal readonly ref struct TemporaryAllocation
    {
        public readonly Span<byte> Span;
        private readonly byte[]? _rented;

        public int Length => Span.Length;

        private TemporaryAllocation(Span<byte> span, byte[]? rented)
        {
            Span = span;
            _rented = rented;
        }

        public static TemporaryAllocation FromString(string str, Span<byte> output)
        {
            var length = Encoding.UTF8.GetByteCount(str);

            if (length <= output.Length)
            {
                Encoding.UTF8.GetBytes(str, output);
                return new TemporaryAllocation(output[..length], null);
            }

            var rented = ArrayPool<byte>.Shared.Rent(length);
            Encoding.UTF8.GetBytes(str, rented);
            return new TemporaryAllocation(rented.AsSpan()[..length], rented);
        }

        /// <summary>
        /// Recycle rented memory
        /// </summary>
        public void Dispose()
        {
            if (_rented != null)
            {
                ArrayPool<byte>.Shared.Return(_rented);
            }
        }
    }
}
