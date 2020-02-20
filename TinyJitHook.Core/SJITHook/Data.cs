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
using System.Runtime.InteropServices;

namespace TinyJitHook.Core.SJITHook
{
    /// <summary>
    /// A class that holds structs and enums for JIT.
    /// </summary>
    public static class Data
    {
        /// <summary>
        /// Protection flags for memory regions.
        /// </summary>
        internal enum Protection
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool VirtualProtect(IntPtr lpAddress, uint dwSize,
           Protection flNewProtect, out uint lpflOldProtect);

        #region Standard

        #region Enums

        public enum CorInfoEhClauseFlags
        {
            CORINFO_EH_CLAUSE_NONE = 0,
            CORINFO_EH_CLAUSE_FILTER = 0x0001,    // If this bit is on, then this EH entry is for a filter
            CORINFO_EH_CLAUSE_FINALLY = 0x0002,   // This clause is a finally clause
            CORINFO_EH_CLAUSE_FAULT = 0x0004,     // This clause is a fault clause
            CORINFO_EH_CLAUSE_DUPLICATE = 0x0008, // Duplicated clause. This clause was duplicated to a funclet which was pulled out of line
            CORINFO_EH_CLAUSE_SAMETRY = 0x0010,   // This clause covers same try block as the previous one. (Used by CoreRT ABI.)
        };

        // Linked with CorInfoMethodInfo->options
        // Changes size depending on .net version, ushort for .net 4.0, uint for .net 3.5
        /// <summary>
        /// An enum that describes the options of a method.
        /// </summary>
#if NET4
        public enum CorInfoOptions : ushort
#else
        public enum CorInfoOptions : uint
#endif
        {
            /// <summary>
            /// Zero initialize all variables.
            /// </summary>
            CORINFO_OPT_INIT_LOCALS = 0x00000010,

            /// <summary>
            /// Is this shared generic code that access the generic context from the this pointer? 
            /// If so, then if the method has SEH then the 'this' pointer must always be reported and kept alive.
            /// </summary>
            CORINFO_GENERICS_CTXT_FROM_THIS = 0x00000020,

            /// <summary>
            /// Is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodDesc)?
            /// If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as <see cref="CorInfoCallConv.PARAMTYPE"/>. 
            /// </summary>
            CORINFO_GENERICS_CTXT_FROM_METHODDESC = 0x00000040,

            /// <summary>
            /// Is this shared generic code that access the generic context from the ParamTypeArg(that is a MethodTable)? 
            /// If so, then if the method has SEH then the 'ParamTypeArg' must always be reported and kept alive. Same as <see cref="CorInfoCallConv.PARAMTYPE"/>.
            /// </summary>
            CORINFO_GENERICS_CTXT_FROM_METHODTABLE = 0x00000080,


            CORINFO_GENERICS_CTXT_MASK = CORINFO_GENERICS_CTXT_FROM_THIS |
                                                       CORINFO_GENERICS_CTXT_FROM_METHODDESC |
                                                       CORINFO_GENERICS_CTXT_FROM_METHODTABLE,
            /// <summary>
            /// Keep the generics context alive throughout the method even if there is no explicit use, and report its location to the CLR
            /// </summary>
            CORINFO_GENERICS_CTXT_KEEP_ALIVE = 0x00000100
        }

        /// <summary>
        /// An enum that provides information on the region the method is in.
        /// </summary>
        public enum CorInfoRegionKind
        {
            CORINFO_REGION_NONE,
            CORINFO_REGION_HOT,
            CORINFO_REGION_COLD,
            CORINFO_REGION_JIT,
        }

        /// <summary>
        /// The JIT flags for the current compile method call.
        /// </summary>
        public enum CorJitFlag
        {
            CORJIT_FLG_SPEED_OPT = 0x00000001,
            CORJIT_FLG_SIZE_OPT = 0x00000002,

            /// <summary>
            /// Generate "debuggable" code (no code-mangling optimizations).
            /// </summary>
            CORJIT_FLG_DEBUG_CODE = 0x00000004,

            /// <summary>
            /// We are in Edit-n-Continue mode.
            /// </summary>
            CORJIT_FLG_DEBUG_EnC = 0x00000008,

            /// <summary>
            /// Generate line and local-var info.
            /// </summary>
            CORJIT_FLG_DEBUG_INFO = 0x00000010,

            /// <summary>
            /// Loose exception order.
            /// </summary>
            CORJIT_FLG_LOOSE_EXCEPT_ORDER = 0x00000020,

            CORJIT_FLG_TARGET_PENTIUM = 0x00000100,
            CORJIT_FLG_TARGET_PPRO = 0x00000200,
            CORJIT_FLG_TARGET_P4 = 0x00000400,
            CORJIT_FLG_TARGET_BANIAS = 0x00000800,

            /// <summary>
            /// Generated code may use fcomi(p) instruction.
            /// </summary>
            CORJIT_FLG_USE_FCOMI = 0x00001000,

            /// <summary>
            /// Generated code may use cmov instruction.
            /// </summary>
            CORJIT_FLG_USE_CMOV = 0x00002000,

            /// <summary>
            /// Generated code may use SSE-2 instructions.
            /// </summary>
            CORJIT_FLG_USE_SSE2 = 0x00004000,

            /// <summary>
            /// Wrap method calls with probes.
            /// </summary>
            CORJIT_FLG_PROF_CALLRET = 0x00010000,

            /// <summary>
            /// Instrument prologues/epilogues.
            /// </summary>
            CORJIT_FLG_PROF_ENTERLEAVE = 0x00020000,

            /// <summary>
            /// Inprocess debugging active requires different instrumentation.
            /// </summary>
            CORJIT_FLG_PROF_INPROC_ACTIVE_DEPRECATED = 0x00040000,

            /// <summary>
            /// Disables PInvoke inlining.
            /// </summary>
            CORJIT_FLG_PROF_NO_PINVOKE_INLINE = 0x00080000,

            /// <summary>
            /// (lazy) Skip verification - determined without doing a full resolve.
            /// </summary>
            CORJIT_FLG_SKIP_VERIFICATION = 0x00100000,

            /// <summary>
            /// JIT or preJIT is the execution engine.
            /// </summary>
            CORJIT_FLG_PREJIT = 0x00200000,

            /// <summary>
            /// Generate relocatable code.
            /// </summary>
            CORJIT_FLG_RELOC = 0x00400000,

            /// <summary>
            /// Only import the function.
            /// </summary>
            CORJIT_FLG_IMPORT_ONLY = 0x00800000,

            /// <summary>
            /// Method is an IL stub.
            /// </summary>
            CORJIT_FLG_IL_STUB = 0x01000000,

            /// <summary>
            /// JIT should separate code into hot and cold sections.
            /// </summary>
            CORJIT_FLG_PROCSPLIT = 0x02000000,

            /// <summary>
            /// Collect basic block profile information.
            /// </summary>
            CORJIT_FLG_BBINSTR = 0x04000000,

            /// <summary>
            /// Optimize method based on profile information.
            /// </summary>
            CORJIT_FLG_BBOPT = 0x08000000,

            /// <summary>
            /// All methods have an EBP frame.
            /// </summary>
            CORJIT_FLG_FRAMED = 0x10000000,

            /// <summary>
            /// Add NOPs before loops to align them at 16 byte boundaries.
            /// </summary>
            CORJIT_FLG_ALIGN_LOOPS = 0x20000000,

            /// <summary>
            /// JIT must place stub secret param into local 0.  (used by IL stubs).
            /// </summary>
            CORJIT_FLG_PUBLISH_SECRET_PARAM = 0x40000000,
        }

        /// <summary>
        /// The calling convention of the method.
        /// </summary>
        public enum CorInfoCallConv
        {
            C = 1,
            DEFAULT = 0,
            EXPLICITTHIS = 64,
            FASTCALL = 4,
            FIELD = 6,
            GENERIC = 16,
            HASTHIS = 32,
            LOCAL_SIG = 7,
            MASK = 15,
            NATIVEVARARG = 11,
            PARAMTYPE = 128,
            PROPERTY = 8,
            STDCALL = 2,
            THISCALL = 3,
            VARARG = 5
        }

        /// <summary>
        /// The data type
        /// </summary>
        public enum CorInfoType : byte
        {
            BOOL = 2,
            BYREF = 18,
            BYTE = 4,
            CHAR = 3,
            CLASS = 20,
            COUNT = 23,
            DOUBLE = 15,
            FLOAT = 14,
            INT = 8,
            LONG = 10,
            NATIVEINT = 12,
            NATIVEUINT = 13,
            PTR = 17,
            REFANY = 21,
            SHORT = 6,
            STRING = 16,
            UBYTE = 5,
            UINT = 9,
            ULONG = 11,
            UNDEF = 0,
            USHORT = 7,
            VALUECLASS = 19,
            VAR = 22,
            VOID = 1
        }
        #endregion

        #region Structs

        #region Sig Inst

        /// <summary>
        /// Information on how type variables are being instantiated in generic code.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CorinfoSigInst
        {
            public uint classInstCount;
            /// <summary>
            /// (representative, not exact) instantiation for class type variables in signature
            /// </summary>
            public unsafe IntPtr* classInst;

            public uint methInstCount;
            /// <summary>
            /// (representative, not exact) instantiation for method type variables in signature
            /// </summary>
            public unsafe IntPtr* methInst;
        }
        /// <summary>
        /// Information on how type variables are being instantiated in generic code.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct CorinfoSigInst64
        {
            public uint classInstCount;
            uint dummy;

            /// <summary>
            /// (representative, not exact) instantiation for class type variables in signature
            /// </summary>
            public IntPtr* classInst;
            public uint methInstCount;
            uint dummy2;

            /// <summary>
            /// (representative, not exact) instantiation for method type variables in signature
            /// </summary>
            public IntPtr* methInst;
        }
        #endregion


        #region Sig Info
        /// <summary>
        /// The main information on the signature of a <see cref="CorMethodInfo"/>.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CorinfoSigInfo
        {
            /// <summary>
            /// The calling convention of the method.
            /// </summary>
            public CorInfoCallConv callConv;
            /// <summary>
            /// If the return type is a value class, this is its handle (enums are normalized).
            /// </summary>
            public IntPtr retTypeClass;
            /// <summary>
            /// Returns the value class as it is in the sig (enums are not converted to primitives).
            /// </summary>
            public IntPtr retTypeSigClass;
            /// <summary>
            /// The return type of the method.
            /// </summary>
            public CorInfoType retType;
            /// <summary>
            /// Used by IL stubs code.
            /// </summary>
            public byte flags;
            /// <summary>
            /// Number of arguments.
            /// </summary>
            public ushort numArgs;
            /// <summary>
            /// Information about how type variables are being instantiated in generic code.
            /// </summary>
            public CorinfoSigInst sigInst;
            /// <summary>
            /// Pointer to the args list handle.
            /// </summary>
            public IntPtr args;
            /// <summary>
            /// The method signature token?
            /// </summary>
            public uint token;
            /// <summary>
            /// The method signature.
            /// </summary>
            public IntPtr sig;
            /// <summary>
            /// Module scope handle.
            /// </summary>
            public IntPtr scope;
        }
        /// <summary>
        /// <para>The main information on the signature of a <see cref="CorMethodInfo"/>.</para>
        /// 
        /// <para>Currently used only on .NET 2.0 with x64.</para>
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct CorinfoSigInfo64
        {

            public uint callConv;
            uint pad1;
            public IntPtr retTypeClass;
            public IntPtr retTypeSigClass;
            public byte retType;
            public byte flags;
            public ushort numArgs;
            uint pad2;
            public CorinfoSigInst64 sigInst;
            public IntPtr args;
            public IntPtr sig;
            public IntPtr scope;
            public uint token;
            uint pad3;
        }

        #endregion


        #region Method Info

        public unsafe interface ICorMethodInfo
        {

        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct CorMethodInfo64 : ICorMethodInfo
        {
#if NET4
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte* ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public UInt32 ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public UInt32 maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public UInt32 EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;

            // .NET 4.0 - https://github.com/dotnet/coreclr/blob/master/src/inc/corinfo.h#L1211 (coreclr)
            /// <summary>
            /// The <see cref="CorInfoRegionKind"/> of the method. 
            /// </summary>
            public int regionKind;

            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo args;
            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo locals;
#else
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte* ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public uint ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public ushort maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public ushort EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;
            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo64 args;
            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo64 locals;
#endif
        }

        [StructLayout(LayoutKind.Sequential)]
        public unsafe struct CorMethodInfo : ICorMethodInfo
        {
#if NET4
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte* ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public UInt32 ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public UInt32 maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public UInt32 EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;

            // .NET 4.0 - https://github.com/dotnet/coreclr/blob/master/src/inc/corinfo.h#L1211 (coreclr)
            /// <summary>
            /// The <see cref="CorInfoRegionKind"/> of the method. 
            /// </summary>
            public Int32 regionKind;

            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo args;

            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo locals;
#else
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte* ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public uint ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public ushort maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public ushort EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;
            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo args;
            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo locals;
#endif
        }
        #endregion

        #region Exception Handler Clause

        [StructLayout(LayoutKind.Sequential, Size = 24)]
        public unsafe struct CorInfoEhClause
        {
            public uint Flags;
            public uint TryOffset;
            public uint TryLength;
            public uint HandlerOffset;
            public uint HandlerLength;
            public uint ClassTokenOrFilterOffset;   // use for type-based exception handlers
        };

        #endregion

        #endregion

        #endregion

        #region Safe CorMethodInfos


        public class SafeCorMethodInfo64
        {
#if NET4
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte[] ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public UInt32 ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public UInt32 maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public UInt32 EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;

            // .NET 4.0 - https://github.com/dotnet/coreclr/blob/master/src/inc/corinfo.h#L1211 (coreclr)
            /// <summary>
            /// The <see cref="CorInfoRegionKind"/> of the method. 
            /// </summary>
            public int regionKind;

            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo args;
            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo locals;
#else
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte[] ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public uint ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public ushort maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public ushort EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;
            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo64 args;
            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo64 locals;
#endif
            public unsafe SafeCorMethodInfo64(CorMethodInfo64* other)
            {
                args = other->args;
                EHCount = other->EHCount;
                ftn = other->ftn;
                ilCode = new byte[other->ilCodeSize];
                Marshal.Copy((IntPtr)other->ilCode, ilCode, 0, ilCode.Length);
                ilCodeSize = other->ilCodeSize;
                locals = other->locals;
                maxStack = other->maxStack;
                options = other->options;
                scope = other->scope;

            }

        }

        public class SafeCorMethodInfo
        {
#if NET4
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte[] ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public UInt32 ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public UInt32 maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public UInt32 EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;

            // .NET 4.0 - https://github.com/dotnet/coreclr/blob/master/src/inc/corinfo.h#L1211 (coreclr)
            /// <summary>
            /// The <see cref="CorInfoRegionKind"/> of the method. 
            /// </summary>
            public Int32 regionKind;

            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo args;

            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo locals;
#else
            /// <summary>
            /// The method handle (token included).
            /// </summary>
            public IntPtr ftn;
            /// <summary>
            /// The module handle.
            /// </summary>
            public IntPtr scope;
            /// <summary>
            /// The IL code of the method.
            /// </summary>
            public byte[] ilCode;
            /// <summary>
            /// The size of the IL code.
            /// </summary>
            public uint ilCodeSize;
            /// <summary>
            /// The max stack of the method.
            /// </summary>
            public ushort maxStack;
            /// <summary>
            /// The number of exception handlers in the method.
            /// </summary>
            public ushort EHCount;
            /// <summary>
            /// The method options/flags. 
            /// </summary>
            public CorInfoOptions options;
            /// <summary>
            /// Arguments.
            /// </summary>
            public CorinfoSigInfo args;
            /// <summary>
            /// Locals.
            /// </summary>
            public CorinfoSigInfo locals;
#endif
            public unsafe SafeCorMethodInfo(CorMethodInfo* other)
            {
                args = other->args;
                EHCount = other->EHCount;
                ftn = other->ftn;
                ilCode = new byte[other->ilCodeSize];
                Marshal.Copy((IntPtr)other->ilCode, ilCode, 0, ilCode.Length);
                ilCodeSize = other->ilCodeSize;
                locals = other->locals;
                maxStack = other->maxStack;
                options = other->options;
                scope = other->scope;
            }
        }

        #endregion

        #region CompileMethod Delegates

        /// <summary>
        /// The 32 bit JIT compileMethod callback.
        /// </summary>
        /// <param name="thisPtr">Pointer to the ICorJitCompiler.</param>
        /// <param name="corJitInfo">The pointer to the ICorJitInfo class instance.</param>
        /// <param name="methodInfo">The MethodInfo of the currently compiling method.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="nativeEntry">The native entrypoint for this method's code.</param>
        /// <param name="nativeSizeOfCode">The size of that code in native.</param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public unsafe delegate int CompileMethodDel(
            IntPtr thisPtr, [In] IntPtr corJitInfo, [In] CorMethodInfo* methodInfo, CorJitFlag flags,
            [Out] IntPtr nativeEntry, [Out] IntPtr nativeSizeOfCode);

        /// <summary>
        /// The 64 bit JIT compileMethod callback.
        /// </summary>
        /// <param name="thisPtr">Pointer to the ICorJitCompiler.</param>
        /// <param name="corJitInfo">The pointer to the ICorJitInfo class instance.</param>
        /// <param name="methodInfo">The MethodInfo of the currently compiling method.</param>
        /// <param name="flags">The flags.</param>
        /// <param name="nativeEntry">The native entrypoint for this method's code.</param>
        /// <param name="nativeSizeOfCode">The size of that code in native.</param>
        /// <returns></returns>
        [UnmanagedFunctionPointer(CallingConvention.StdCall, SetLastError = true)]
        public unsafe delegate int CompileMethodDel64(
            IntPtr thisPtr, [In] IntPtr corJitInfo, [In] CorMethodInfo64* methodInfo, CorJitFlag flags,
            [Out] IntPtr nativeEntry, [Out] IntPtr nativeSizeOfCode);

        #endregion

        #region Other Delegates

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, SetLastError = true)]
        public unsafe delegate void GetEHInfoDel(IntPtr thisPtr, [In] IntPtr ftn, [In] uint EHnumber,
                                                [Out] CorInfoEhClause* clause);
        //virtual void getEHinfo(
        //    CORINFO_METHOD_HANDLE ftn, /* IN  */
        //    unsigned EHnumber,         /* IN */
        //    CORINFO_EH_CLAUSE* clause  /* OUT */
        //)

        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate uint GetMethodDefFromMethodDel(IntPtr thisPtr, IntPtr ftn);

        /// <summary>
        /// Clear JIT Cache from the ICorJitCompiler
        /// </summary>
        /// <param name="thisPtr">The instance of the ICorJitCompiler.</param>
        // https://github.com/dotnet/coreclr/blob/release/2.1/src/inc/corjit.h#L277
        [UnmanagedFunctionPointer(CallingConvention.ThisCall)]
        public delegate void ClearCacheDel(IntPtr thisPtr);

        #endregion
    }
}
