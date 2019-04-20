/*The MIT License (MIT)

Copyright (c) 2014 UbbeLoL

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.*/

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TinyJitHook.SJITHook
{
    /// <summary>
    /// A 64 bit JIT hook that works on a <see cref="VTableAddrProvider"/> to redirect the pointer to compileMethod.
    /// </summary>
    /// <typeparam name="T">A vtable address provider.</typeparam>
    public unsafe class JITHook64<T> : IJitHook where T : VTableAddrProvider
    {
        /// <inheritdoc />
        public IntPtr VTableAddress
        {

            [MethodImpl(MethodImplOptions.NoInlining)]
            get => pVTable;
        }

        private readonly T _addrProvider;
        /// <summary>
        /// The original compile method.
        /// </summary>
        
        public Data.CompileMethodDel64 OriginalCompileMethod64 { [MethodImpl(MethodImplOptions.NoInlining)] get; private set; }

        /// <summary>
        /// Completely unused, will return null. Do not use.
        /// </summary>
        public Data.CompileMethodDel OriginalCompileMethod32 { [MethodImpl(MethodImplOptions.NoInlining)] get; private set; }

        /// <summary>
        /// The callback to use instead of the original compile method.
        /// </summary>
        public Data.CompileMethodDel64 HookedCompileMethod { [MethodImpl(MethodImplOptions.NoInlining)] get; set; }

        private IntPtr pVTable;
        private IntPtr pCompileMethod;
        private uint old;

        /// <summary>
        /// Create a new 64 bit JIT hook (does not hook). Will change memory flags to PAGE_EXECUTE_READWRITE for the compileMethod region (4 or 8 bytes).
        /// </summary>
        /// <param name="hookedCompileMethod">The callback to replace the original.</param>
        public JITHook64(Data.CompileMethodDel64 hookedCompileMethod)
        {
            _addrProvider = Activator.CreateInstance<T>();
            HookedCompileMethod = hookedCompileMethod;

            pVTable = _addrProvider.VTableAddr;
            pCompileMethod = Marshal.ReadIntPtr(pVTable);

            if (
                !Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                    Data.Protection.PAGE_EXECUTE_READWRITE, out old))
                throw new Exception("Cannot change memory protection flags.");

            OriginalCompileMethod64 =
                (Data.CompileMethodDel64)
                    Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(pCompileMethod), typeof(Data.CompileMethodDel64));
            Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                                (Data.Protection)old, out old);
        }

        /// <summary>
        /// Hook the compileMethod function and redirect to the supplied callback.
        /// </summary>
        /// <returns>Whether it was successfully hooked.</returns>
        public bool Hook()
        {
           
            // We don't want any infinite loops :-)
            PrepareInternalMethods();

            if (
                !Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                                     Data.Protection.PAGE_EXECUTE_READWRITE, out old))
                throw new Exception("Cannot change memory protection flags.");

            Marshal.WriteIntPtr(pCompileMethod, Marshal.GetFunctionPointerForDelegate(HookedCompileMethod));

            return Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                (Data.Protection)old, out old);
        }

        /// <summary>
        /// Unhook the compileMethod function and stop redirection to the callback.
        /// </summary>
        /// <returns>Whether it was successfully removed.</returns>
        public bool UnHook()
        {
            IntPtr pVTable = _addrProvider.VTableAddr;
            IntPtr pCompileMethod = Marshal.ReadIntPtr(pVTable);
            uint old;

            if (
                !Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                    Data.Protection.PAGE_EXECUTE_READWRITE, out old))
                return false;

            Marshal.WriteIntPtr(pCompileMethod, Marshal.GetFunctionPointerForDelegate(OriginalCompileMethod64));

            return Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                (Data.Protection) old, out old);
        }

        /// <inheritdoc />
        public void PrepareInternalMethods()
        {
            RuntimeHelpers.PrepareDelegate(HookedCompileMethod);
            RuntimeHelpers.PrepareDelegate(OriginalCompileMethod64);
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("get_OriginalCompileMethod64").MethodHandle, new[] { typeof(T).TypeHandle });
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("get_OriginalCompileMethod32").MethodHandle, new[] { typeof(T).TypeHandle });
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("get_HookedCompileMethod").MethodHandle, new[] { typeof(T).TypeHandle });
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("get_VTableAddress").MethodHandle, new[] { typeof(T).TypeHandle });
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("UnHook").MethodHandle, new[] { typeof(T).TypeHandle });
        }
    }
}
