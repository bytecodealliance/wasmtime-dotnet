#if NETSTANDARD2_0 || NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    // Polyfill required by C# 9+ records / init-only setters when targeting frameworks
    // earlier than .NET 5. Marked internal so it is per-assembly and does not collide
    // with the runtime-provided definition on net5.0+.
    internal static class IsExternalInit
    {
    }
}
#endif
