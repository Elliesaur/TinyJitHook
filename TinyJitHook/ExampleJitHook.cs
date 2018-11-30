using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TinyJitHook.Extensions;
using TinyJitHook.HookHelpers;
using TinyJitHook.Models;
using TinyJitHook.SJITHook;

namespace TinyJitHook
{
    public unsafe class ExampleJitHook
    {
        private static ExampleJitHook _instance;
        private readonly IHookHelper _hookHelper;
        public readonly bool Is64Bit;
        private readonly Dictionary<IntPtr, Assembly> _scopeMap;

        public delegate bool ActionDelegate(RawArguments args, ref byte[] ilBytes, uint methodToken, Assembly relatedAssembly);
        public Dictionary<uint, ActionDelegate> Actions;

        public class RawArguments
        {
            public IntPtr ThisPtr;
            public IntPtr CorJitInfo;
            public IntPtr MethodInfo;
            public Data.CorJitFlag Flags;

            public IntPtr NativeEntry;
            public IntPtr NativeSizeOfCode;
        }

        public ExampleJitHook(Assembly asm, bool is64Bit)
        {
            Is64Bit = is64Bit;
            Actions = new Dictionary<uint, ActionDelegate>();
            _instance = this;

            if (is64Bit)
            {
                _hookHelper = new HookHelper64(asm, HookedCompileMethod64);
            }
            else
            {
                _hookHelper = new HookHelper32(asm, HookedCompileMethod32);
            }

            _hookHelper.Hook.PrepareOriginalCompileGetter(Is64Bit);


            AppDomain.CurrentDomain.PrepareAssemblies();

            _scopeMap = AppDomain.CurrentDomain.GetScopeMap();
            var tmp = _scopeMap.ContainsKey(IntPtr.Zero);

        }
        public void Hook()
        {
            if (!Actions.ContainsKey(0))
            {
                throw new Exception("No default action! Add an action for key 0.");
            }

            if (!_hookHelper.Apply())
            {
                throw new Exception("Hook could not be applied.");
            }
        }

        public void Unhook()
        {
            if (!_hookHelper.Remove())
            {
                throw new Exception("Hook could not be removed.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int HookedCompileMethod32(IntPtr thisPtr, [In] IntPtr corJitInfo,
                                                        [In] Data.CorMethodInfo* methodInfo, Data.CorJitFlag flags,
                                                        [Out] IntPtr nativeEntry, [Out] IntPtr nativeSizeOfCode)
        {
            // THIS IS THE 32 BIT COMPILE METHOD.
            IntPtr safeMethodInfo = new IntPtr((int*)methodInfo);

            uint token = (uint)(0x06000000 | *(ushort*)methodInfo->ftn);

            _instance._hookHelper.Remove();

            Assembly relatedAssembly = null;
            if (_instance._scopeMap.ContainsKey(methodInfo->scope))
            {
                relatedAssembly = _instance._scopeMap[methodInfo->scope];
            }

            RawArguments ra = new RawArguments()
            {
                ThisPtr = thisPtr,
                CorJitInfo = corJitInfo,
                MethodInfo = safeMethodInfo,
                Flags = flags,
                NativeEntry = nativeEntry,
                NativeSizeOfCode = nativeSizeOfCode
            };

            byte[] il = new byte[methodInfo->ilCodeSize];
            Marshal.Copy((IntPtr)methodInfo->ilCode, il, 0, il.Length);

            bool changed = _instance.Actions.ContainsKey(token)
                ? _instance.Actions[token](ra, ref il, token, relatedAssembly)
                : _instance.Actions[0](ra, ref il, token, relatedAssembly);

            if (changed)
            {
                thisPtr = ra.ThisPtr;
                corJitInfo = ra.CorJitInfo;
                methodInfo = (Data.CorMethodInfo*)ra.MethodInfo.ToPointer();
                flags = ra.Flags;
                nativeEntry = ra.NativeEntry;
                nativeSizeOfCode = ra.NativeSizeOfCode;

                IntPtr ilCodeHandle = Marshal.AllocHGlobal(il.Length);
                Marshal.Copy(il, 0, ilCodeHandle, il.Length);

                // This isn't exactly safe, what happens to the original pointers?
                // Is the size correct?
                Data.VirtualProtect((IntPtr)methodInfo->ilCode, methodInfo->ilCodeSize, Data.Protection.PAGE_READWRITE,
                                    out uint prevProt);

                methodInfo->ilCode = (byte*)ilCodeHandle.ToPointer();
                methodInfo->ilCodeSize = (uint)il.Length;

                // Cannot reprotect the marshal allocated memory.
                //Data.VirtualProtect((IntPtr)methodInfo->ilCode, methodInfo->ilCodeSize, (Data.Protection)prevProt,
                //                    out prevProt);
            }


            _instance._hookHelper.Apply();
            return _instance._hookHelper.Hook.OriginalCompileMethod32(thisPtr, corJitInfo, methodInfo, flags, nativeEntry, nativeSizeOfCode);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int HookedCompileMethod64(IntPtr thisPtr, [In] IntPtr corJitInfo,
                                                 [In] Data.CorMethodInfo64* methodInfo, Data.CorJitFlag flags,
                                                 [Out] IntPtr nativeEntry, [Out] IntPtr nativeSizeOfCode)
        {
            // THIS IS THE 64 BIT COMPILE METHOD.
            IntPtr safeMethodInfo = new IntPtr((int*)methodInfo);

            uint token = (uint)(0x06000000 | *(ushort*)methodInfo->ftn);

            _instance._hookHelper.Remove();

            Assembly relatedAssembly = null;
            if (_instance._scopeMap.ContainsKey(methodInfo->scope))
            {
                relatedAssembly = _instance._scopeMap[methodInfo->scope];
            }

            RawArguments ra = new RawArguments()
            {
                ThisPtr = thisPtr,
                CorJitInfo = corJitInfo,
                MethodInfo = safeMethodInfo,
                Flags = flags,
                NativeEntry = nativeEntry,
                NativeSizeOfCode = nativeSizeOfCode
            };

            byte[] il = new byte[methodInfo->ilCodeSize];
            Marshal.Copy((IntPtr)methodInfo->ilCode, il, 0, il.Length);

            bool changed = _instance.Actions.ContainsKey(token)
                ? _instance.Actions[token](ra, ref il, token, relatedAssembly)
                : _instance.Actions[0](ra, ref il, token, relatedAssembly);

            if (changed)
            {
                thisPtr = ra.ThisPtr;
                corJitInfo = ra.CorJitInfo;
                methodInfo = (Data.CorMethodInfo64*)ra.MethodInfo.ToPointer();
                flags = ra.Flags;
                nativeEntry = ra.NativeEntry;
                nativeSizeOfCode = ra.NativeSizeOfCode;

                IntPtr ilCodeHandle = Marshal.AllocHGlobal(il.Length);
                Marshal.Copy(il, 0, ilCodeHandle, il.Length);

                // This isn't exactly safe, what happens to the original pointers?
                // Is the size correct?
                Data.VirtualProtect((IntPtr)methodInfo->ilCode, methodInfo->ilCodeSize, Data.Protection.PAGE_READWRITE,
                                    out uint prevProt);

                methodInfo->ilCode = (byte*)ilCodeHandle.ToPointer();
                methodInfo->ilCodeSize = (uint)il.Length;

                // Cannot reprotect the marshal allocated memory.
                //Data.VirtualProtect((IntPtr)methodInfo->ilCode, methodInfo->ilCodeSize, (Data.Protection)prevProt,
                //                    out prevProt);
            }

            _instance._hookHelper.Apply();
            return _instance._hookHelper.Hook.OriginalCompileMethod64(thisPtr, corJitInfo, methodInfo, flags,
                                                                      nativeEntry, nativeSizeOfCode);
        }
    }
}
