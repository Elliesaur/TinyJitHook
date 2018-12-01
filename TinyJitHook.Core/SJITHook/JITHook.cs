﻿/*The MIT License (MIT)

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

namespace TinyJitHook.Core.SJITHook
{
    /// <summary>
    /// A 32 bit JIT hook that works on a <see cref="VTableAddrProvider"/> to redirect the pointer to compileMethod.
    /// </summary>
    /// <typeparam name="T">A vtable address provider.</typeparam>
    public unsafe class JITHook<T> : IJitHook where T : VTableAddrProvider
    {

        private readonly byte[] _delegateTrampolineCode = {
            0x68, 0x00, 0x00, 0x00, 0x00,
            0xc3
        };

        private readonly T _addrProvider;

        /// <summary>
        /// The original compile method.
        /// </summary>
        public Data.CompileMethodDel OriginalCompileMethod32 { [MethodImpl(MethodImplOptions.NoInlining)]get; private set; }
        /// <summary>
        /// Completely unused, will return null. Do not use.
        /// </summary>
        public Data.CompileMethodDel64 OriginalCompileMethod64 { [MethodImpl(MethodImplOptions.NoInlining)]get; private set; }

        /// <summary>
        /// The callback to use instead of the original compile method.
        /// </summary>
        public Data.CompileMethodDel HookedCompileMethod { [MethodImpl(MethodImplOptions.NoInlining)]get; set; }


        private IntPtr pVTable;
        private IntPtr pCompileMethod;
        private uint old;
        
        /// <summary>
        /// Create a new 32 bit JIT hook (does not hook). Will change memory flags to PAGE_EXECUTE_READWRITE for the compileMethod region (4 or 8 bytes).
        /// </summary>
        /// <param name="hookedCompileMethod">The callback to replace the original.</param>
        public JITHook(Data.CompileMethodDel hookedCompileMethod)
        {
            _addrProvider = Activator.CreateInstance<T>();
            HookedCompileMethod = hookedCompileMethod;

            pVTable = _addrProvider.VTableAddr;
            pCompileMethod = Marshal.ReadIntPtr(pVTable);

            if (
                !Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                    Data.Protection.PAGE_EXECUTE_READWRITE, out old))
                throw new Exception("Cannot change memory protection flags.");

            OriginalCompileMethod32 =
                (Data.CompileMethodDel)
                    Marshal.GetDelegateForFunctionPointer(Marshal.ReadIntPtr(pCompileMethod), typeof(Data.CompileMethodDel));
        }

        /// <summary>
        /// Hook the compileMethod function and redirect to the supplied callback.
        /// </summary>
        /// <returns>Whether it was successfully hooked.</returns>
        public bool Hook()
        {
            // We don't want any infinite loops :-)
            RuntimeHelpers.PrepareDelegate(HookedCompileMethod);
            RuntimeHelpers.PrepareDelegate(OriginalCompileMethod32);
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("get_OriginalCompileMethod64").MethodHandle, new[] { typeof(T).TypeHandle });
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("get_OriginalCompileMethod32").MethodHandle, new[] { typeof(T).TypeHandle });
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("get_HookedCompileMethod").MethodHandle, new[] { typeof(T).TypeHandle });
            RuntimeHelpers.PrepareMethod(GetType().GetMethod("UnHook").MethodHandle, new[] {typeof (T).TypeHandle});

            IntPtr fPtr = Marshal.GetFunctionPointerForDelegate(HookedCompileMethod);
            PreLoad(fPtr);

            Marshal.WriteIntPtr(pCompileMethod, fPtr);

            return Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                (Data.Protection)old, out old);
        }

        /// <summary>
        /// Unhook the compileMethod function and stop redirection to the callback.
        /// </summary>
        /// <returns>Whether it was successfully removed.</returns>
        public bool UnHook()
        {
            if (
                !Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                    Data.Protection.PAGE_EXECUTE_READWRITE, out old))
                return false;

            Marshal.WriteIntPtr(pCompileMethod, Marshal.GetFunctionPointerForDelegate(OriginalCompileMethod32));

            return Data.VirtualProtect(pCompileMethod, (uint)IntPtr.Size,
                (Data.Protection) old, out old);
        }


        private void PreLoad(IntPtr fPtr)
        {
            // Trampoline to reverse pinvoke to test...
            var tPtr = AllocateTrampoline(fPtr);
            var t = (Data.CompileMethodDel64)Marshal.GetDelegateForFunctionPointer(
                tPtr, typeof(Data.CompileMethodDel64));

            t(IntPtr.Zero, IntPtr.Zero, (Data.CorMethodInfo64*)IntPtr.Zero.ToPointer(), default(Data.CorJitFlag),
              IntPtr.Zero, IntPtr.Zero);
            // Free it.
            Marshal.FreeHGlobal(tPtr);
        }

        private IntPtr AllocateTrampoline(IntPtr ptr)
        {
            var jmpNative = Marshal.AllocHGlobal(_delegateTrampolineCode.Length);
            if (!Data.VirtualProtect(jmpNative, (uint)_delegateTrampolineCode.Length,
                                     Data.Protection.PAGE_EXECUTE_READWRITE, out old))
                return IntPtr.Zero;
            Marshal.Copy(_delegateTrampolineCode, 0, jmpNative, _delegateTrampolineCode.Length);
            Marshal.WriteIntPtr(jmpNative, 1, ptr);
            return jmpNative;
        }
    }
}
