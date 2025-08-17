namespace Wasmtime.Components;

public class ComponentInstance
{
    //todo: everything!


    internal static class Native
    {
        //[DllImport(Engine.LibraryName)]
        //public static extern IntPtr /* wasmtime_component_export_index_t* */ wasmtime_component_instance_get_export_index (wasmtime_component_instance_t *instance, wasmtime_context_t *context, ComponentExport.Handle instance_export_index, string name, nuint name_len)

        // [DllImport(Engine.LibraryName)]
        //todo: bool 	wasmtime_component_instance_get_func (const wasmtime_component_instance_t *instance, wasmtime_context_t *context, const wasmtime_component_export_index_t *export_index, wasmtime_component_func_t *func_out)
    }
}