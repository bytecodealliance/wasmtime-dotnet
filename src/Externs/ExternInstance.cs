using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Wasmtime.Exports;

namespace Wasmtime.Externs
{
    /// <summary>
    /// Represents an external WebAssembly module instance.
    /// </summary>
    public class ExternInstance : DynamicObject, IDisposable, IImportable
    {
        internal ExternInstance(InstanceExport export, IntPtr instance)
        {
            _export = export;
            _instance = instance;
            _externs = new Externs(_export.Exports, _instance);
            _functions = Functions.ToDictionary(f => f.Name);
            _globals = Globals.ToDictionary(g => g.Name);
        }

        /// <summary>
        /// The name of the WebAssembly instance.
        /// </summary>
        public string Name => _export.Name;

        /// <summary>
        /// The exported functions of the instance.
        /// </summary>
        public IReadOnlyList<ExternFunction> Functions => _externs.Functions;

        /// <summary>
        /// The exported globals of the instance.
        /// </summary>
        public IReadOnlyList<ExternGlobal> Globals => _externs.Globals;

        /// <summary>
        /// The exported tables of the instance.
        /// </summary>
        public IReadOnlyList<ExternTable> Tables => _externs.Tables;

        /// <summary>
        /// The exported memories of the instance.
        /// </summary>
        public IReadOnlyList<ExternMemory> Memories => _externs.Memories;

        /// <summary>
        /// The exported instances of the instance.
        /// </summary>
        public IReadOnlyList<ExternInstance> Instances => _externs.Instances;

        /// <summary>
        /// The exported modules of the instance.
        /// </summary>
        public IReadOnlyList<ExternModule> Modules => _externs.Modules;

        /// <inheritdoc/>
        public override bool TryGetMember(GetMemberBinder binder, out object? result)
        {
            if (_globals.TryGetValue(binder.Name, out var global))
            {
                result = global.Value;
                return true;
            }
            result = null;
            return false;
        }

        /// <inheritdoc/>
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (_globals.TryGetValue(binder.Name, out var global))
            {
                global.Value = value;
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public override bool TryInvokeMember(InvokeMemberBinder binder, object?[] args, out object? result)
        {
            if (!_functions.TryGetValue(binder.Name, out var func))
            {
                result = null;
                return false;
            }

            result = func.Invoke(args);
            return true;
        }

        /// <inheritdoc/>
        public unsafe void Dispose()
        {
            foreach (var instance in Instances)
            {
                instance.Dispose();
            }

            if (!(_externs is null))
            {
                _externs.Dispose();
            }
        }

        IntPtr IImportable.GetHandle()
        {
            return Interop.wasm_instance_as_extern(_instance);
        }

        private InstanceExport _export;
        private IntPtr _instance;
        private Externs _externs;
        private Dictionary<string, ExternFunction> _functions;
        private Dictionary<string, ExternGlobal> _globals;
    }
}
