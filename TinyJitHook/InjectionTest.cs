using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TinyJitHook.Extensions;
using TinyJitHook.SJITHook;

namespace TinyJitHook
{
    public static class InjectionTest
    {
        // Required to invoke static constructor.
        public static string bytes;
        public static string bytes2;

        static InjectionTest()
        {
            bytes = "test";
            bytes2 = "test2";
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
            try
            {
                var methodBase = relatedAssembly.ManifestModule.ResolveMethod((int) methodToken);

                if (methodBase == relatedAssembly.EntryPoint)
                {
                    Console.WriteLine("REPLACING ENTRYPOINT");

                    ilBytes = Convert.FromBase64String(bytes);
                    ehBytes = Convert.FromBase64String(bytes2);

                    //var mi = (Data.CorMethodInfo*) args.MethodInfo;
                    //mi->EHCount = 1; //(uint)ehBytes.GetExceptionHandlers(ilBytes.GetInstructions()).Count;
                }
            }
            catch (Exception)
            {

            }
        }
    }
}
