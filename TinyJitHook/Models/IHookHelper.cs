using System;
using System.Reflection;
using TinyJitHook.SJITHook;

namespace TinyJitHook.Models
{
    /// <summary>
    /// A hook helper that provides wrapping to an underlying JIT hook.
    /// </summary>
    public interface IHookHelper
    {
        /// <summary>
        /// The underlying JIT hook.
        /// </summary>
        IJitHook Hook { get; }

        /// <summary>
        /// The loaded assembly.
        /// </summary>
        Assembly LoadedAssembly { get; }

        /// <summary>
        /// The module scope of the manifest module.
        /// </summary>
        IntPtr ModuleScope { get; }

        /// <summary>
        /// Apply the hook.
        /// </summary>
        /// <returns>Whether the hook was applied.</returns>
        bool Apply();

        /// <summary>
        /// Remove the hook.
        /// </summary>
        /// <returns>Whether the hook was removed.</returns>
        bool Remove();
    }
}
