using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TinyJitHook.Core.Extensions;
using TinyJitHook.Core.SJITHook;

namespace TinyJitHook.Core
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Assembly asm = Assembly.LoadFrom(@"TinyJitHook.Core.TestApp_x64.dll");
            MainJitHook hook = new MainJitHook(asm, IntPtr.Size == 8);

            //hook.OnCompileMethod += ChangeExample;

            hook.Hook();

            asm.EntryPoint.Invoke(null, new object[] { new[] { "" } });

            hook.Unhook();

            Console.WriteLine("DONE");
            Console.ReadKey();
        }

        private static unsafe void ChangeExample(MainJitHook.RawArguments args, Assembly relatedAssembly, uint methodToken, ref byte[] ilBytes, ref byte[] ehBytes)
        {
            try
            {
                var methodBase = relatedAssembly.ManifestModule.ResolveMethod((int)methodToken);
                Data.CorMethodInfo* rawMethodInfo = (Data.CorMethodInfo*)args.MethodInfo.ToPointer();
                
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
            catch (Exception ex)
            {
                // RIP
            }
        }
        // Token: 0x0400000B RID: 11
        public static string bytes = "AChNAAAKAAAWKE4AAAoAcwsAAAYoTwAACgAA3hEKAHK5AgBwBihQAAAKAADeACo="; //"AChNAAAKAAAWKE4AAAoAcwsAAAYoTwAACgAA3hEKAHK5AgBwBihQAAAKAADeACo=";

        // Token: 0x0400000C RID: 12
        public static string bytes2 = "ARAAAAAABwAWHQARHwAAAQ==";  // "ARAAAAAABwAWHQARHwAAAQ==";
    }
}
