using System;
using System.Runtime.CompilerServices;

namespace Wasmtime
{
    /// <summary>
    /// A 128 bit value
    /// </summary>
    public struct V128
    {
        /// <summary>
        /// Get a V128 with all bits set to 1
        /// </summary>
        public static readonly V128 AllBitsSet = new V128(
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
            byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue
        );

        private unsafe fixed byte bytes[16];

        /// <summary>
        /// Construct a new V128 from a 16 element byte span
        /// </summary>
        /// <param name="bytes"></param>
        /// <exception cref="ArgumentException"></exception>
        public V128(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 16)
            {
                throw new ArgumentException("Must supply exactly 16 bytes to construct V128");
            }

            unsafe
            {
                fixed (byte* bytesPtr = this.bytes)
                {
                    bytes.CopyTo(new Span<byte>(bytesPtr, 16));
                }
            }
        }

        /// <summary>
        /// Construct a new V128 from 16 bytes
        /// </summary>
        /// <param name="b0">First byte.</param>
        /// <param name="b1">Second byte.</param>
        /// <param name="b2">Third byte.</param>
        /// <param name="b3">Fourth byte.</param>
        /// <param name="b4">Fifth byte.</param>
        /// <param name="b5">Sixth byte.</param>
        /// <param name="b6">Seventh byte.</param>
        /// <param name="b7">Eighth byte.</param>
        /// <param name="b8">Ninth byte.</param>
        /// <param name="b9">Tenth byte.</param>
        /// <param name="b10">Eleventh byte.</param>
        /// <param name="b11">Twelfth byte.</param>
        /// <param name="b12">Thirteenth byte.</param>
        /// <param name="b13">Fourteenth byte.</param>
        /// <param name="b14">Fifteenth byte.</param>
        /// <param name="b15">Sixteenth byte.</param>
        public V128(byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8, byte b9, byte b10, byte b11, byte b12, byte b13, byte b14, byte b15)
        {
            unsafe
            {
                bytes[0] = b0;
                bytes[1] = b1;
                bytes[2] = b2;
                bytes[3] = b3;
                bytes[4] = b4;
                bytes[5] = b5;
                bytes[6] = b6;
                bytes[7] = b7;
                bytes[8] = b8;
                bytes[9] = b9;
                bytes[10] = b10;
                bytes[11] = b11;
                bytes[12] = b12;
                bytes[13] = b13;
                bytes[14] = b14;
                bytes[15] = b15;
            }
        }

        internal unsafe V128(byte* src)
        {
            fixed (byte* dest = bytes)
            {
                Unsafe.CopyBlock(dest, src, 16);
            }
        }

        internal unsafe void CopyTo(byte* dest)
        {
            var dst = new Span<byte>(dest, 16);
            CopyTo(dst);
        }

        internal unsafe void CopyTo(Span<byte> dest)
        {
            fixed (byte* bytesPtr = bytes)
            {
                var src = new Span<byte>(bytesPtr, 16);
                src.CopyTo(dest);
            }
        }
    }
}
