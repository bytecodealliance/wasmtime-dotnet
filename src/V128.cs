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

        private unsafe fixed byte Value[16];

        /// <summary>
        /// Construct a new V128 from a 16 element byte span
        /// </summary>
        /// <param name="bytes"></param>
        /// <exception cref="ArgumentException"></exception>
        public V128(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length != 16)
                throw new ArgumentException("Must supply exactly 16 bytes to construct V128");

            unsafe
            {
                fixed (byte* value = Value)
                    bytes.CopyTo(new Span<byte>(value, 16));
            }
        }

        /// <summary>
        /// Construct a new V128 from 16 bytes
        /// </summary>
        /// <param name="e0"></param>
        /// <param name="e1"></param>
        /// <param name="e2"></param>
        /// <param name="e3"></param>
        /// <param name="e4"></param>
        /// <param name="e5"></param>
        /// <param name="e6"></param>
        /// <param name="e7"></param>
        /// <param name="e8"></param>
        /// <param name="e9"></param>
        /// <param name="e10"></param>
        /// <param name="e11"></param>
        /// <param name="e12"></param>
        /// <param name="e13"></param>
        /// <param name="e14"></param>
        /// <param name="e15"></param>
        public V128(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
        {
            unsafe
            {
                Value[0] = e0;
                Value[1] = e1;
                Value[2] = e2;
                Value[3] = e3;
                Value[4] = e4;
                Value[5] = e5;
                Value[6] = e6;
                Value[7] = e7;
                Value[8] = e8;
                Value[9] = e9;
                Value[10] = e10;
                Value[11] = e11;
                Value[12] = e12;
                Value[13] = e13;
                Value[14] = e14;
                Value[15] = e15;
            }
        }

        internal unsafe V128(byte* src)
        {
            fixed (byte* value = Value)
            {
                Unsafe.CopyBlock(value, src, 16);
            }
        }

        internal unsafe void CopyTo(byte* dest)
        {
            for (int i = 0; i < 16; i++)
            {
                dest[i] = Value[i];
            }
        }
    }
}
