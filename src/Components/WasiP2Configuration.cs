using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Wasmtime.Components
{
    /// <summary>
    /// Builds the WASI 0.2 (preview 2) context attached to a <see cref="Store"/> when
    /// instantiating a component that imports WASI interfaces.
    /// </summary>
    /// <remarks>
    /// Required whenever <see cref="ComponentLinker.AddWasiPreview2"/> is called: the linker
    /// registers the WASI host functions, but each invocation looks up the WASI context on
    /// the store. Without one wasmtime traps in <c>WasiView::ctx()</c>.
    /// </remarks>
    public sealed class WasiP2Configuration
    {
        /// <summary>Inherits the host process's stdin stream.</summary>
        public bool InheritStandardInput { get; set; }

        /// <summary>Inherits the host process's stdout stream.</summary>
        public bool InheritStandardOutput { get; set; }

        /// <summary>Inherits the host process's stderr stream.</summary>
        public bool InheritStandardError { get; set; }

        /// <summary>Arguments forwarded to <c>wasi:cli/environment.get-arguments</c>.</summary>
        public IList<string> Arguments { get; } = new List<string>();

        internal IntPtr Build()
        {
            var cfg = Native.wasmtime_wasip2_config_new();
            if (cfg == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to allocate wasmtime_wasip2_config_t.");
            }

            try
            {
                if (InheritStandardInput)
                {
                    Native.wasmtime_wasip2_config_inherit_stdin(cfg);
                }

                if (InheritStandardOutput)
                {
                    Native.wasmtime_wasip2_config_inherit_stdout(cfg);
                }

                if (InheritStandardError)
                {
                    Native.wasmtime_wasip2_config_inherit_stderr(cfg);
                }

                foreach (var arg in Arguments)
                {
                    if (arg is null)
                    {
                        throw new ArgumentException("Argument values must not be null.", nameof(Arguments));
                    }

                    var bytes = Encoding.UTF8.GetBytes(arg);
                    unsafe
                    {
                        fixed (byte* ptr = bytes)
                        {
                            Native.wasmtime_wasip2_config_arg(cfg, ptr, (UIntPtr)bytes.Length);
                        }
                    }
                }

                return cfg;
            }
            catch
            {
                Native.wasmtime_wasip2_config_delete(cfg);
                throw;
            }
        }

        internal static class Native
        {
            [DllImport(Engine.LibraryName)]
            public static extern IntPtr wasmtime_wasip2_config_new();

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_wasip2_config_inherit_stdin(IntPtr config);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_wasip2_config_inherit_stdout(IntPtr config);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_wasip2_config_inherit_stderr(IntPtr config);

            [DllImport(Engine.LibraryName)]
            public static extern unsafe void wasmtime_wasip2_config_arg(IntPtr config, byte* arg, UIntPtr argLen);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_wasip2_config_delete(IntPtr config);

            [DllImport(Engine.LibraryName)]
            public static extern void wasmtime_context_set_wasip2(IntPtr context, IntPtr config);
        }
    }

    /// <summary>
    /// Component-model extensions for <see cref="Store"/>.
    /// </summary>
    public static class StoreComponentExtensions
    {
        /// <summary>
        /// Attaches a WASI 0.2 context to <paramref name="store"/>, satisfying the lookups that
        /// <see cref="ComponentLinker.AddWasiPreview2"/>'s host functions perform at call time.
        /// </summary>
        /// <param name="store">The store to attach the context to.</param>
        /// <param name="config">The configuration describing stdio inheritance and arguments.</param>
        public static void SetWasiP2Configuration(this Store store, WasiP2Configuration config)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            if (config is null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var cfg = config.Build();
            WasiP2Configuration.Native.wasmtime_context_set_wasip2(store.Context.handle, cfg);
            GC.KeepAlive(store);
        }
    }
}
