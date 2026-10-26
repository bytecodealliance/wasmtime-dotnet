using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

namespace Wasmtime;

/// <summary>
/// Represents a WASI2 configuration.
/// </summary>
public class Wasi2Configuration
{
    private bool _inheritStandardInput;
    private bool _inheritStandardOutput;
    private bool _inheritStandardError;

    /// <summary>
    /// Sets the configuration to inherit stdin.
    /// </summary>
    /// <returns>Returns the current configuration.</returns>
    public Wasi2Configuration WithInheritedStandardInput()
    {
        _inheritStandardInput = true;
        return this;
    }

    /// <summary>
    /// Sets the configuration to inherit stdout.
    /// </summary>
    /// <returns>Returns the current configuration.</returns>
    public Wasi2Configuration WithInheritedStandardOutput()
    {
        _inheritStandardOutput = true;
        return this;
    }

    /// <summary>
    /// Sets the configuration to inherit stderr.
    /// </summary>
    /// <returns>Returns the current configuration.</returns>
    public Wasi2Configuration WithInheritedStandardError()
    {
        _inheritStandardError = true;
        return this;
    }

    internal Handle Build()
    {
        var config = new Handle(Native.wasmtime_wasip2_config_new());

        SetStandardIn(config);
        SetStandardOut(config);
        SetStandardError(config);

        return config;
    }

    private void SetStandardIn(Handle config)
    {
        if (_inheritStandardInput)
            Native.wasmtime_wasip2_config_inherit_stdin(config);
    }

    private void SetStandardOut(Handle config)
    {
        if (_inheritStandardOutput)
            Native.wasmtime_wasip2_config_inherit_stdout(config);
    }

    private void SetStandardError(Handle config)
    {
        if (_inheritStandardError)
            Native.wasmtime_wasip2_config_inherit_stderr(config);
    }

    internal class Handle : SafeHandleZeroOrMinusOneIsInvalid
    {
        public Handle(IntPtr handle)
            : base(true)
        {
            SetHandle(handle);
        }

        protected override bool ReleaseHandle()
        {
            Native.wasmtime_wasip2_config_delete(handle);
            return true;
        }
    }

    private static class Native
    {
        [DllImport(Engine.LibraryName)]
        public static extern IntPtr wasmtime_wasip2_config_new();

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_wasip2_config_delete(IntPtr config);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_wasip2_config_inherit_stdin(Handle config);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_wasip2_config_inherit_stdout(Handle config);

        [DllImport(Engine.LibraryName)]
        public static extern void wasmtime_wasip2_config_inherit_stderr(Handle config);

        [DllImport(Engine.LibraryName)]
        public static extern unsafe void wasmtime_wasip2_config_arg(Handle config, char* arg, nuint arg_len);
    }
}