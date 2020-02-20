﻿using System;
using System.Runtime.CompilerServices;

namespace TinyJitHook.Core.SJITHook
{
    /// <summary>
    /// JIT hook interface to abstract it out a bit.
    /// </summary>
    public interface IJitHook
    {
        /// <summary>
        /// The VTable Address that the address provider retrieved.
        /// </summary>
        IntPtr VTableAddress { get; }

        /// <summary>
        /// The original compile method (x86) if the JIT hook is for 32 bit (otherwise null).
        /// </summary>
        Data.CompileMethodDel OriginalCompileMethod32 { [MethodImpl(MethodImplOptions.NoInlining)] get; }
        /// <summary>
        /// The original compile method (x64) if the JIT hook is for 64 bit (otherwise null)
        /// </summary>
        Data.CompileMethodDel64 OriginalCompileMethod64 { [MethodImpl(MethodImplOptions.NoInlining)] get; }

        /// <summary>
        /// Hook the compileMethod function and redirect to the supplied callback.
        /// </summary>
        /// <returns>Whether it was successfully hooked.</returns>
        bool Hook();
        /// <summary>
        /// Unhook the compileMethod function and stop redirection to the callback.
        /// </summary>
        /// <returns>Whether it was successfully removed.</returns>
        bool UnHook();

        /// <summary>
        /// Prepare internal methods so that we do not run into loops.
        /// </summary>
        void PrepareInternalMethods();
    }
}
