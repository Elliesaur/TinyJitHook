using System;
using System.Reflection;
using TinyJitHook.Core.Extensions;
using TinyJitHook.Core.Models;
using TinyJitHook.Core.SJITHook;

namespace TinyJitHook.Core.HookHelpers
{
    /// <summary>
    /// A 64 bit hook helper that wraps around SJITHook's implementation.
    /// </summary>
    public class HookHelper64 : IHookHelper
    {
        /// <summary>
        /// The original CompileMethod.
        /// </summary>
        public Data.CompileMethodDel64 Original;

        /// <summary>
        /// The loaded assembly.
        /// </summary>
        public Assembly LoadedAssembly { get; private set; }

        /// <summary>
        /// The underlying JIT hook used.
        /// </summary>
        public IJitHook Hook { get; private set; }

        /// <summary>
        /// The loaded module's scope.
        /// </summary>
        public IntPtr ModuleScope { get; private set; }

        /// <summary>
        /// Initialize a new 64 bit hook for JIT on the supplied assembly with the callback CompileMethod.
        /// Will determine the runtime version (and JIT hook to use).
        /// </summary>
        /// <param name="asm">The assembly to wrap.</param>
        /// <param name="hookedCompileMethod64">The CompileMethod to call instead of the original.</param>
        public HookHelper64(Assembly asm, Data.CompileMethodDel64 hookedCompileMethod64)
        {
            LoadedAssembly = asm;
            ModuleScope = LoadedAssembly.ManifestModule.GetScope();

            // .NET 4.0+
            Hook = new JITHook64<ClrjitAddrProvider>(hookedCompileMethod64);
            Original = Hook.OriginalCompileMethod64;
        }

        /// <summary>
        /// Apply the JIT hook and load the assembly (does invoke the module static constructor!).
        /// </summary>
        /// <returns>Whether or not the JIT hook was applied.</returns>
        public bool Apply()
        {
            return Hook.Hook();
        }

        /// <summary>
        /// Remove the JIT hook and restore the original.
        /// </summary>
        /// <returns>Whether or not the JIT hook was successfully unhooked.</returns>
        public bool Remove()
        {
            return Hook.UnHook();
        }
    }
}
