using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using TinyJitHook.Extensions;
using TinyJitHook.HookHelpers;
using TinyJitHook.Models;
using TinyJitHook.SJITHook;

namespace TinyJitHook
{
    public unsafe class MainJitHook
    {
        private static MainJitHook _instance;
        private readonly IHookHelper _hookHelper;

        public readonly bool Is64Bit;
        public int EntryCount;

        private Dictionary<IntPtr, Assembly> _scopeMap;

        public delegate void ActionDelegate(RawArguments args, Assembly relatedAssembly,
                                            uint methodToken, ref byte[] ilBytes, ref byte[] ehBytes);

        #region Events

        private readonly AutoResetEvent _compileMethodResetEvent;
        private static object _jitCompileMethodLock = new object();
        public event ActionDelegate OnCompileMethod;

        #endregion

        public class RawArguments
        {
            public IntPtr ThisPtr;
            public IntPtr CorJitInfo;
            public IntPtr MethodInfo;
            public Data.CorJitFlag Flags;

            public IntPtr NativeEntry;
            public IntPtr NativeSizeOfCode;
        }

        public MainJitHook(Assembly asm, bool is64Bit)
        {
            Is64Bit = is64Bit;
            EntryCount = 0;

            _instance = this;
            _compileMethodResetEvent = new AutoResetEvent(false);

            if (is64Bit)
                _hookHelper = new HookHelper64(asm, HookedCompileMethod64);
            else
                _hookHelper = new HookHelper32(asm, HookedCompileMethod32);

            _hookHelper.Hook.PrepareOriginalCompileGetter(Is64Bit);
            //Assembly.GetExecutingAssembly().PrepareMethods();

            //AppDomain.CurrentDomain.PrepareAssemblies();

            _scopeMap = AppDomain.CurrentDomain.GetScopeMap();

        }

        public void Hook()
        {
            if (!_hookHelper.Apply()) throw new Exception("Hook could not be applied.");
        }

        public void Unhook()
        {
            if (!_hookHelper.Remove()) throw new Exception("Hook could not be removed.");
        }



        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void OnCompileEventResetMethod(RawArguments args, Assembly assembly, uint methodToken,
                                                      ref byte[] bytes,
                                                      ref byte[] ehBytes)
        {
            _instance._compileMethodResetEvent.Set();
        }



        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int HookedCompileMethod32(IntPtr thisPtr, [In] IntPtr corJitInfo,
                                                 [In] Data.CorMethodInfo* methodInfo, Data.CorJitFlag flags,
                                                 [Out] IntPtr nativeEntry, [Out] IntPtr nativeSizeOfCode)
        {
            // THIS IS THE 32 BIT COMPILE METHOD.

            if (thisPtr == IntPtr.Zero) return 0;

            var o = _instance._hookHelper.Hook.OriginalCompileMethod32;

            _instance.EntryCount++;
            if (_instance.EntryCount > 1)
            {
                _instance.EntryCount--;
                return o(thisPtr, corJitInfo, methodInfo, flags,
                         nativeEntry, nativeSizeOfCode);
            }


            var safeMethodInfo = new IntPtr((int*)methodInfo);
            EHInfoHook exceptionHandlerHook = null;

            var token = (uint)(0x06000000 | *(ushort*)methodInfo->ftn);

            lock (_jitCompileMethodLock)
            {

                Assembly relatedAssembly = null;
                if (!_instance._scopeMap.ContainsKey(methodInfo->scope))
                    _instance._scopeMap = AppDomain.CurrentDomain.GetScopeMap();
                if (_instance._scopeMap.ContainsKey(methodInfo->scope))
                {
                    relatedAssembly = _instance._scopeMap[methodInfo->scope];
                }
                else
                {
                    _instance.EntryCount--;
                    return o(thisPtr, corJitInfo, methodInfo, flags,
                             nativeEntry, nativeSizeOfCode);
                }

                var ra = new RawArguments
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

                // Extra sections contains the exception handlers.
                byte[] extraSections = new byte[0];
                if (methodInfo->EHCount > 0)
                {
                    byte* extraSectionsPtr = methodInfo->ilCode + methodInfo->ilCodeSize;
                    extraSections = TryReadExtraSections(extraSectionsPtr);
                }

                _instance.OnCompileMethod -= OnCompileEventResetMethod;
                _instance.OnCompileMethod += OnCompileEventResetMethod;
                _instance.OnCompileMethod(ra, relatedAssembly, token, ref il, ref extraSections);
                _instance._compileMethodResetEvent.WaitOne();

                // Assume something has changed.
                thisPtr = ra.ThisPtr;
                corJitInfo = ra.CorJitInfo;
                methodInfo = (Data.CorMethodInfo*)ra.MethodInfo.ToPointer();
                flags = ra.Flags;
                nativeEntry = ra.NativeEntry;
                nativeSizeOfCode = ra.NativeSizeOfCode;

                // IL code and extra sections
                //var ilCodeHandle = Marshal.AllocHGlobal(il.Length + extraSections.Length);
                //Marshal.Copy(il, 0, ilCodeHandle, il.Length);
                //Data.VirtualProtect((IntPtr)methodInfo->ilCode, (uint)methodInfo->ilCodeSize, Data.Protection.PAGE_EXECUTE_READWRITE,
                //                    out uint prevProt);
                ////methodInfo->ilCode = (byte*)ilCodeHandle.ToPointer();
                ////methodInfo->ilCodeSize = (uint)il.Length;
                //Marshal.Copy(il, 0, (IntPtr)methodInfo->ilCode, (int)methodInfo->ilCodeSize);
                //Marshal.Copy(extraSections, 0, (IntPtr)Align(methodInfo->ilCode + methodInfo->ilCodeSize, 4),
                //             extraSections.Length);
                var ilCodeHandle = Marshal.AllocHGlobal(il.Length + (extraSections.Length * 2));
                Marshal.Copy(il, 0, ilCodeHandle, il.Length);
                Data.VirtualProtect((IntPtr)methodInfo->ilCode, (uint)IntPtr.Size, Data.Protection.PAGE_READWRITE,
                                    out uint prevProt);
                methodInfo->ilCode = (byte*)ilCodeHandle.ToPointer();
                methodInfo->ilCodeSize = (uint)il.Length;
                Marshal.Copy(extraSections, 0, (IntPtr)(methodInfo->ilCode + methodInfo->ilCodeSize),
                             extraSections.Length);

                exceptionHandlerHook = new EHInfoHook(corJitInfo, methodInfo->ftn);

            }



            
            var res = o(thisPtr, corJitInfo, methodInfo, flags,
                     nativeEntry, nativeSizeOfCode);
            if (exceptionHandlerHook != null)
            {
                exceptionHandlerHook.Dispose();
            }

            _instance.EntryCount--;
            return res;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int HookedCompileMethod64(IntPtr thisPtr, [In] IntPtr corJitInfo,
                                                 [In] Data.CorMethodInfo64* methodInfo, Data.CorJitFlag flags,
                                                 [Out] IntPtr nativeEntry, [Out] IntPtr nativeSizeOfCode)
        {
            // THIS IS THE 64 BIT COMPILE METHOD.

            if (thisPtr == IntPtr.Zero) return 0;

            var o = _instance._hookHelper.Hook.OriginalCompileMethod64;

            _instance.EntryCount++;
            if (_instance.EntryCount > 1)
            {
                _instance.EntryCount--;
                return o(thisPtr, corJitInfo, methodInfo, flags,
                         nativeEntry, nativeSizeOfCode);
            }

            var safeMethodInfo = new IntPtr((int*)methodInfo);
            var token = (uint)(0x06000000 | *(ushort*)methodInfo->ftn);

            lock (_jitCompileMethodLock)
            {
                Assembly relatedAssembly = null;
                if (!_instance._scopeMap.ContainsKey(methodInfo->scope))
                    _instance._scopeMap = AppDomain.CurrentDomain.GetScopeMap();
                if (_instance._scopeMap.ContainsKey(methodInfo->scope))
                {
                    relatedAssembly = _instance._scopeMap[methodInfo->scope];
                }
                else
                {
                    _instance.EntryCount--;
                    return o(thisPtr, corJitInfo, methodInfo, flags,
                             nativeEntry, nativeSizeOfCode);
                }

                var ra = new RawArguments
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

                // Extra sections contains the exception handlers.
                byte[] extraSections = new byte[0];
                if (methodInfo->EHCount > 0)
                {
                    byte* extraSectionsPtr = methodInfo->ilCode + methodInfo->ilCodeSize;
                    extraSections = TryReadExtraSections(extraSectionsPtr);
                }

                _instance.OnCompileMethod -= OnCompileEventResetMethod;
                _instance.OnCompileMethod += OnCompileEventResetMethod;
                _instance.OnCompileMethod(ra, relatedAssembly, token, ref il, ref extraSections);
                _instance._compileMethodResetEvent.WaitOne();

                // Assume something has changed.
                thisPtr = ra.ThisPtr;
                corJitInfo = ra.CorJitInfo;
                methodInfo = (Data.CorMethodInfo64*)ra.MethodInfo.ToPointer();
                flags = ra.Flags;
                nativeEntry = ra.NativeEntry;
                nativeSizeOfCode = ra.NativeSizeOfCode;

                // IL code and extra sections
                var ilCodeHandle = Marshal.AllocHGlobal(il.Length + extraSections.Length);
                Marshal.Copy(il, 0, ilCodeHandle, il.Length);
                Data.VirtualProtect((IntPtr)methodInfo->ilCode, 1, Data.Protection.PAGE_READWRITE,
                                    out uint prevProt);
                methodInfo->ilCode = (byte*)ilCodeHandle.ToPointer();
                methodInfo->ilCodeSize = (uint)il.Length;
                Marshal.Copy(extraSections, 0, (IntPtr)Align(methodInfo->ilCode + methodInfo->ilCodeSize, 4),
                             extraSections.Length);
            }

            _instance.EntryCount--;
            return o(thisPtr, corJitInfo, methodInfo, flags,
                     nativeEntry, nativeSizeOfCode);
        }

        #region Extra Section Reader

        // Credits to 0xd4d
        // https://github.com/0xd4d/de4dot/blob/master/de4dot.mdecrypt/DynamicMethodsDecrypter.cs
        private static byte[] TryReadExtraSections(byte* p)
        {
            try
            {
                p = Align(p, 4);
                byte* startPos = p;
                p = ParseSection(p);
                var size = (int)(p - startPos);
                byte[] sections = new byte[size];
                Marshal.Copy((IntPtr)startPos, sections, 0, sections.Length);
                return sections;
            }
            catch (Exception ex)
            {
                return new byte[0];
            }
        }

        private static byte* Align(byte* p, int alignment)
        {
            return (byte*)new IntPtr((long)((ulong)(p + alignment - 1) & ~(ulong)(alignment - 1)));
        }

        private static byte* ParseSection(byte* p)
        {
            byte flags;
            do
            {
                p = Align(p, 4);

                flags = *p++;
                if ((flags & 1) == 0)
                    throw new ApplicationException("Not an exception section");
                if ((flags & 0x3E) != 0)
                    throw new ApplicationException("Invalid bits set");

                if ((flags & 0x40) != 0)
                {
                    p--;
                    int num = (int)(*(uint*)p >> 8) / 24;
                    p += 4 + num * 24;
                }
                else
                {
                    int num = *p++ / 12;
                    p += 2 + num * 12;
                }
            } while ((flags & 0x80) != 0);

            return p;
        }

        #endregion

        private class EHInfoHook
        {
            private readonly byte[] _delegateTrampolineCode = {
                0x68, 0x00, 0x00, 0x00, 0x00,
                0xc3
            };

            public readonly IntPtr CorJitInfoPtr;
            public readonly IntPtr* NewVfTable;
            public readonly IntPtr* OldVfTable;

            public readonly Data.GetEHInfoDel OriginalMethod;
            public readonly Data.GetEHInfoDel NewMethod;
            public readonly IntPtr MethodFtn;

            [MethodImpl(MethodImplOptions.NoInlining)]
            public EHInfoHook(IntPtr corJitInfo, IntPtr ftn)

            {
                MethodFtn = ftn;
                // Find clauses from somewhere, lookup table for ftn --> list<excpetionhandler>?, need to provide them easily.
                CorJitInfoPtr = corJitInfo;

                OldVfTable = (IntPtr*)Marshal.ReadIntPtr(corJitInfo);

                // Slot number, the amount of function pointers in the vftable.
                // Original from ConfuserEx: 158
                const int slotNum = 166;
                NewVfTable = (IntPtr*) Marshal.AllocHGlobal(slotNum * IntPtr.Size);
                for (int i = 0; i < slotNum; i++)
                {
                    NewVfTable[i] = OldVfTable[i];
                }

                NewMethod = Hook;
                OriginalMethod =
                    (Data.GetEHInfoDel) Marshal.GetDelegateForFunctionPointer(OldVfTable[8], typeof(Data.GetEHInfoDel));

                NewVfTable[8] = Marshal.GetFunctionPointerForDelegate(NewMethod);

                Marshal.WriteIntPtr(corJitInfo, 0, (IntPtr)NewVfTable);
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            public void Dispose()
            {
                Marshal.FreeHGlobal((IntPtr)NewVfTable);
                Marshal.WriteIntPtr(CorJitInfoPtr, 0, (IntPtr)OldVfTable);
            }
            [MethodImpl(MethodImplOptions.NoInlining)]
            // The new getehinfo method.
            private void Hook(IntPtr ptr, IntPtr ftn, uint ehNumber, Data.CorInfoEhClause* clause)
            {
                OriginalMethod(ptr, ftn, ehNumber, clause);
            }

            ////public void Hook2()
            ////{
            ////    Data.VirtualProtect(_getEhInfoPtr, (uint)IntPtr.Size,
            ////                        Data.Protection.PAGE_EXECUTE_READWRITE, out uint oldProt);

            ////    var fPtr = Marshal.GetFunctionPointerForDelegate(_newGetEhInfoDel);
            ////    PreLoad<Data.GetEHInfoDel>(fPtr, d =>
            ////    {
            ////        var casted = (Data.GetEHInfoDel)d;
            ////        casted(IntPtr.Zero, IntPtr.Zero, 0, default(Data.CorInfoEhClause));
            ////    });

            ////    Marshal.WriteIntPtr(_getEhInfoPtr, fPtr);
            ////    Data.VirtualProtect(_getEhInfoPtr, (uint)IntPtr.Size,
            ////                        (Data.Protection)oldProt, out oldProt);
            ////}
            //private void PreLoad<T>(IntPtr fPtr, Action<Delegate> testAction)
            //{
            //    // Trampoline to reverse pinvoke
            //    var tPtr = AllocateTrampoline(fPtr);
            //    var t = Marshal.GetDelegateForFunctionPointer(
            //        tPtr, typeof(T));

            //    testAction(t);

            //    // Free it.
            //    Marshal.FreeHGlobal(tPtr);
            //}

            //private IntPtr AllocateTrampoline(IntPtr ptr)
            //{
            //    var jmpNative = Marshal.AllocHGlobal(_delegateTrampolineCode.Length);
            //    if (!Data.VirtualProtect(jmpNative, (uint)_delegateTrampolineCode.Length,
            //                             Data.Protection.PAGE_EXECUTE_READWRITE, out uint old))
            //        return IntPtr.Zero;
            //    Marshal.Copy(_delegateTrampolineCode, 0, jmpNative, _delegateTrampolineCode.Length);
            //    Marshal.WriteIntPtr(jmpNative, 1, ptr);
            //    return jmpNative;
            //}
        }
    }
}