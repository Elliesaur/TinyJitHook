using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using TinyJitHook.Extensions;

namespace TinyJitHook
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Assembly asm = Assembly.LoadFrom(@"Amelia_EXAMPLE.exe");
            ExampleJitHook hook = new ExampleJitHook(asm, true);
            // Default action is given a token of 0.
            hook.Actions.Add(0, DefaultAction);

            hook.Hook();

            asm.EntryPoint.Invoke(null, new object[] {});

            hook.Unhook();

            Console.WriteLine("DONE");
            Console.ReadKey();
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static unsafe bool DefaultAction(ExampleJitHook.RawArguments args, ref byte[] ilBytes, uint methodToken, Assembly relatedAssembly)
        {
            var methodBase = relatedAssembly.ManifestModule.ResolveMethod((int)methodToken);
            SJITHook.Data.CorMethodInfo64* rawMethodInfo = (SJITHook.Data.CorMethodInfo64*)args.MethodInfo.ToPointer();

            // Get the instructions in a nice mini-format.
            var insts = ilBytes.GetInstructions().ToList();
            foreach (var inst in insts)
            {
                inst.OpCode = OpCodes.Nop;
            }
            insts.Add(Instruction.Create(OpCodes.Ret));

            // Replace the instructions
            ilBytes = insts.GetBytes();

            // True indicates the method has been changed
            // Do not return true if things have not changed
            return true;
        }
    }
}
