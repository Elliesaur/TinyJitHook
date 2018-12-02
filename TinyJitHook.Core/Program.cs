using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TinyJitHook.Core.Extensions;
using TinyJitHook.Core.SJITHook;

namespace TinyJitHook.Core
{
    public unsafe class Program
    {
        public static void Main(string[] args)
        {
            Assembly asm = Assembly.LoadFrom(@"TinyJitHook.Core.TestApp_x64.dll");
            // True indicates that it is x64.
            MainJitHook hook = new MainJitHook(asm, IntPtr.Size == 8);

            hook.OnCompileMethod += ChangeExample;
            //hook.OnCompileMethod += NoChangeExample;

            hook.Hook();

            asm.EntryPoint.Invoke(null, new object[] { new[] { "" } });

            hook.Unhook();

            Console.WriteLine("DONE");
            Console.ReadKey();
        }

        private static unsafe void ChangeExample(MainJitHook.RawArguments args, Assembly relatedAssembly, uint methodToken, ref byte[] ilBytes, ref byte[] ehBytes)
        {
            var methodBase = relatedAssembly.ManifestModule.ResolveMethod((int)methodToken);
            Data.CorMethodInfo* rawMethodInfo = (Data.CorMethodInfo*)args.MethodInfo.ToPointer();
            var insts = ilBytes.GetInstructions().ToList();

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

            //// Get the instructions in a nice mini-format.
            //var insts = ilBytes.GetInstructions().ToList();
            //foreach (var inst in insts)
            //{
            //    inst.OpCode = OpCodes.Nop;
            //}
            //insts.Add(Instruction.Create(OpCodes.Ret));
            //ilBytes = insts.GetBytes();
        }

        private static unsafe void NoChangeExample(MainJitHook.RawArguments args, Assembly relatedAssembly, uint methodToken, ref byte[] ilBytes, ref byte[] ehBytes)
        {
            // Changes to the il byte array in the previous delegate will be reflected here.

        }
    }
}
