using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using TinyJitHook.Extensions;
using TinyJitHook.SJITHook;

namespace TinyJitHook
{
    public static class InjectionTest
    {
        // Required to invoke static constructor.
        public static BinaryReader r;

        static InjectionTest()
        {
            using (Stream resFilestream = typeof(InjectionTest).Assembly.GetManifestResourceStream("Name"))
            {
                byte[] ba = new byte[resFilestream.Length];
                resFilestream.Read(ba, 0, ba.Length);
                r = new BinaryReader(new MemoryStream(ba));
            }
            Start();
        }

        private static void Start()
        {
            MainJitHook hook = new MainJitHook(typeof(InjectionTest).Assembly, IntPtr.Size == 8);
            hook.OnCompileMethod += CompileMethod;
            hook.Hook();
        }

        private static unsafe void CompileMethod(MainJitHook.RawArguments args, Assembly relatedAssembly, uint methodToken, ref byte[] ilBytes, ref byte[] ehBytes)
        {
           
            //if (relatedAssembly != null)
            //{
            //    Console.WriteLine($"0x{methodToken:x8} - {relatedAssembly.FullName}");
            //}
            //else
            //{
            //    Console.WriteLine($"0x{methodToken:x8}");
            //}
            if (relatedAssembly != typeof(InjectionTest).Assembly)
            {
                return;
            }

            try
            {
                var methodBase = relatedAssembly.ManifestModule.ResolveMethod((int) methodToken);
                //var nameOfMethod = methodBase.Name;
                //if (nameOfMethod == "GetHash")
                //{
                //    nameOfMethod += "test";
                //}
                var insts = ilBytes.GetInstructions();
                int index = -1;

                if (insts[0].OpCode == OpCodes.Ldc_I4)
                    index = (int) insts[0].Data;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_S)
                    index = (int) (byte) insts[0].Data;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_0)
                    index = 0;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_1)
                    index = 1;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_2)
                    index = 2;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_3)
                    index = 3;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_4)
                    index = 4;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_5)
                    index = 5;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_6)
                    index = 6;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_7)
                    index = 7;
                else if (insts[0].OpCode == OpCodes.Ldc_I4_8)
                    index = 8;

                if (index == -1)
                    return;
                r.BaseStream.Position = index;

                int ilByteCount = r.ReadInt32();
                byte[] newIL = r.ReadBytes(ilByteCount);
                int extraCount = r.ReadInt32();
                byte[] newEH = r.ReadBytes(extraCount);

                ilBytes = newIL;
                ehBytes = newEH;
            }
            catch (Exception)
            {

            }
        }
    }
}
