using System;
using System.Text;

namespace Wasmtime
{
    public class AsyncInstance
    {
        /// <summary>
        /// Gets an exported function from the instance and check the type signature.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <param name="returnType">The return type of the function. Null if no return type. Tuple of types is multiple returns expected.</param>
        /// <param name="parameterTypes">The expected parameters to the function</param>
        /// <returns>Returns the function if a function of that name and type signature was exported or null if not.</returns>
        public AsyncFunction? GetFunction(string name, Type? returnType, params Type[] parameterTypes)
        {
            var func = GetFunction(name);
            if (func is null)
            {
                return null;
            }

            if (!func.CheckTypeSignature(returnType, parameterTypes))
            {
                return null;
            }

            return func;
        }

        /// <summary>
        /// Gets an exported function from the instance.
        /// </summary>
        /// <param name="name">The name of the exported function.</param>
        /// <returns>Returns the function if a function of that name was exported or null if not.</returns>
        public AsyncFunction? GetFunction(string name)
        {
            var context = _store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Func)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return _store.GetCachedExternAsync(ext.of.func);
        }

        /// <summary>
        /// Gets an exported table from the instance.
        /// </summary>
        /// <param name="name">The name of the exported table.</param>
        /// <returns>Returns the table if a table of that name was exported or null if not.</returns>
        public Table? GetTable(string name)
        {
            var context = _store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Table)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return new Table(_store, ext.of.table);
        }

        /// <summary>
        /// Gets an exported memory from the instance.
        /// </summary>
        /// <param name="name">The name of the exported memory.</param>
        /// <returns>Returns the memory if a memory of that name was exported or null if not.</returns>
        public Memory? GetMemory(string name)
        {
            if (!TryGetExtern(_store.Context, name, out var ext) || ext.kind != ExternKind.Memory)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return _store.GetCachedExtern(ext.of.memory);
        }

        /// <summary>
        /// Gets an exported global from the instance.
        /// </summary>
        /// <param name="name">The name of the exported global.</param>
        /// <returns>Returns the global if a global of that name was exported or null if not.</returns>
        public Global? GetGlobal(string name)
        {
            var context = _store.Context;
            if (!TryGetExtern(context, name, out var ext) || ext.kind != ExternKind.Global)
            {
                return null;
            }

            GC.KeepAlive(_store);

            return _store.GetCachedExtern(ext.of.global);
        }

        private bool TryGetExtern(StoreContext context, string name, out Extern ext)
        {
            unsafe
            {
                var nameBytes = Encoding.UTF8.GetBytes(name);
                fixed (byte* ptr = nameBytes)
                {
                    return Instance.Native.wasmtime_instance_export_get(context.handle, _instance, ptr, (UIntPtr)nameBytes.Length, out ext);
                }
            }
        }

        internal AsyncInstance(Store store, ExternInstance instance)
        {
            if (store is null)
            {
                throw new ArgumentNullException(nameof(store));
            }

            _store = store;
            _instance = instance;
        }

        private readonly Store _store;
        internal readonly ExternInstance _instance;
    }
}
