using System;

namespace Wasmtime.Components
{
    /// <summary>
    /// Marks a partial class as the entry point for source-generated component bindings.
    /// </summary>
    /// <remarks>
    /// Applied to a <see langword="partial"/> class declared in user code. The
    /// <c>Wasmtime.Component.SourceGenerators</c> Roslyn generator reads the WIT file at
    /// <see cref="WitPath"/>, optionally selects a world named <see cref="World"/>, and emits
    /// strongly-typed C# bindings into the same partial class.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ComponentBindingsAttribute : Attribute
    {
        /// <summary>
        /// Creates a new attribute referencing a WIT file by relative path.
        /// </summary>
        /// <param name="witPath">Path to a <c>.wit</c> file declared as <c>&lt;AdditionalFiles&gt;</c> in the project.</param>
        /// <param name="world">Optional world name; required if the WIT file declares multiple worlds.</param>
        public ComponentBindingsAttribute(string witPath, string? world = null)
        {
            WitPath = witPath ?? throw new ArgumentNullException(nameof(witPath));
            World = world;
        }

        /// <summary>The path to the WIT file the bindings are derived from.</summary>
        public string WitPath { get; }

        /// <summary>The selected world name, or <see langword="null"/> if the WIT file declares only one world.</summary>
        public string? World { get; }
    }
}
