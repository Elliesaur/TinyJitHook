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
            var methodBase = relatedAssembly.ManifestModule.ResolveMethod((int)methodToken);
            Data.CorMethodInfo* rawMethodInfo = (Data.CorMethodInfo*)args.MethodInfo.ToPointer();

            if (methodBase == relatedAssembly.EntryPoint)
            {
                Console.WriteLine("REPLACING ENTRYPOINT");

                ilBytes = Convert.FromBase64String(bytes); 
                ehBytes = Convert.FromBase64String(bytes2);
                rawMethodInfo->EHCount = 1;
            }
           
                var insts = ilBytes.GetInstructions();

                Logger.LogInfo(typeof(Program), $"---------------------------------------");
                Logger.LogSuccess(typeof(Program), $"{methodBase.DeclaringType?.FullName}.{methodBase.Name}");
                Logger.LogSuccess(typeof(Program), $"Inst Count: {insts.Count}");
                Logger.LogSuccess(typeof(Program), $"Exception Handler Count: {rawMethodInfo->EHCount}");

                if (rawMethodInfo->EHCount > 0)
                {
                    var ehs = ehBytes.GetExceptionHandlers(insts);
                    for (var i = 0; i < ehs.Count; i++)
                    {
                        var eh = ehs[i];
                        Logger.LogWarn(typeof(Program), $"Exception Handler {i + 1}:");
                        Logger.LogWarn(typeof(Program), $" Type: {eh.HandlerType}");
                        Logger.LogWarn(typeof(Program), $" TryStart: {eh.TryStart}");
                        Logger.LogWarn(typeof(Program), $" TryEnd: {eh.TryEnd}");
                        Logger.LogWarn(typeof(Program), $" CatchTypeToken: {eh.CatchTypeToken}");
                    }
                }
                foreach (var inst in insts)
                {
                    Logger.Log(typeof(Program), $"{inst}");
                }
            
        }
    }
}
