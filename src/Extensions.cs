using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime
{
    internal static class Extensions
    {
        public const UnmanagedType LPUTF8Str =
#if NETSTANDARD2_0
            (UnmanagedType)48;
#else
            UnmanagedType.LPUTF8Str;
#endif

#if NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe string GetString(this Encoding encoding, Span<byte> bytes)
        {
            fixed (byte* bytesPtr = bytes)
            {
                return encoding.GetString(bytesPtr, bytes.Length);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe string GetString(this Encoding encoding, ReadOnlySpan<byte> bytes)
        {
            fixed (byte* bytesPtr = bytes)
            {
                return encoding.GetString(bytesPtr, bytes.Length);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetBytes(this Encoding encoding, Span<char> chars, Span<byte> bytes)
        {
            fixed (char* charsPtr = chars)
            fixed (byte* bytesPtr = bytes)
            {
                return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetBytes(this Encoding encoding, ReadOnlySpan<char> chars, Span<byte> bytes)
        {
            fixed (char* charsPtr = chars)
            fixed (byte* bytesPtr = bytes)
            {
                return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetBytes(this Encoding encoding, string chars, Span<byte> bytes)
        {
            fixed (char* charsPtr = chars)
            fixed (byte* bytesPtr = bytes)
            {
                return encoding.GetBytes(charsPtr, chars.Length, bytesPtr, bytes.Length);
            }
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsTupleType(this Type type)
        {
#if NETSTANDARD2_0
            return type.FullName.StartsWith("System.ValueTuple`");
#else
            return typeof(ITuple).IsAssignableFrom(type);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Int32BitsToSingle(int value)
        {
#if NETSTANDARD2_0
            unsafe
            {
                return *(float*)&value;
            }
#else
            return BitConverter.Int32BitsToSingle(value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SingleToInt32Bits(float value)
        {
#if NETSTANDARD2_0
            unsafe
            {
                return *(int*)&value;
            }
#else
            return BitConverter.SingleToInt32Bits(value);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string PtrToStringUTF8(IntPtr ptr, int byteLen)
        {
#if NETSTANDARD2_0
            unsafe
            {
                return Encoding.UTF8.GetString((byte*)ptr, byteLen);
            }
#else
            return Marshal.PtrToStringUTF8(ptr, byteLen);
#endif
        }
    }
}