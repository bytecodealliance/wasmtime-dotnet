using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace Wasmtime
{
    [StructLayout(LayoutKind.Sequential)]
    internal record struct ExternFunc
    {
        static ExternFunc() => Debug.Assert(Unsafe.SizeOf<ExternFunc>() == 16);

        public ulong store;
        public IntPtr __private;
    }

    [StructLayout(LayoutKind.Explicit)]
    internal record struct ExternTable
    {
        static ExternTable() => Debug.Assert(Unsafe.SizeOf<ExternTable>() == 24);

        // Use explicit offsets because the struct in the C api has extra padding
        // due to field alignments. The total struct size is 24 bytes.

        [FieldOffset(0)]
        public ulong store;
        [FieldOffset(8)]
        public uint __private1;
        [FieldOffset(16)]
        public uint __private2;
    }


    [StructLayout(LayoutKind.Explicit)]
    internal record struct ExternMemory
    {
        static ExternMemory() => Debug.Assert(Unsafe.SizeOf<ExternMemory>() == 24);

        // Use explicit offsets because the struct in the C api has extra padding
        // due to field alignments. The total struct size is 24 bytes.

        [FieldOffset(0)]
        public ulong store;
        [FieldOffset(8)]
        public uint __private1;
        [FieldOffset(16)]
        public uint __private2;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal record struct ExternInstance
    {
        static ExternInstance() => Debug.Assert(Unsafe.SizeOf<ExternInstance>() == 16);

        public ulong store;
        public nuint __private;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal record struct ExternGlobal
    {
        static ExternGlobal() => Debug.Assert(Unsafe.SizeOf<ExternMemory>() == 24);

        public ulong store;
        public uint __private1;
        public uint __private2;
        public uint __private3;
    }

    internal enum ExternKind : byte
    {
        Func,
        Global,
        Table,
        Memory,
        SharedMemory,
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct ExternUnion
    {
        static ExternUnion() => Debug.Assert(Unsafe.SizeOf<ExternUnion>() == 24);

        [FieldOffset(0)]
        public ExternFunc func;

        [FieldOffset(0)]
        public ExternGlobal global;

        [FieldOffset(0)]
        public ExternTable table;

        [FieldOffset(0)]
        public ExternMemory memory;

        [FieldOffset(0)]
        public IntPtr sharedmemory;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Extern : IDisposable
    {
        static Extern() => Debug.Assert(Unsafe.SizeOf<Extern>() == 32);

        public ExternKind kind;
        public ExternUnion of;

        public void Dispose()
        {
            Native.wasmtime_extern_delete(this);
        }

        private static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_extern_delete(in Extern self);
        }
    }
}
