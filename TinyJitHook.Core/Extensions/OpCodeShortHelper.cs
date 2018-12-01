using System.Collections.Generic;
using System.Reflection.Emit;

namespace TinyJitHook.Core.Extensions
{
    public static class OpCodeShortHelper
    {
        private static readonly Dictionary<short, OpCode> _opCodes = new Dictionary<short, OpCode>();

        static OpCodeShortHelper()
        {
            Initialize();
        }


        public static OpCode GetOpCode(this short value)
        {
            return _opCodes[value];
        }


        private static void Initialize()
        {
            foreach (var fieldInfo in typeof(OpCodes).GetFields())
            {
                var opCode = (OpCode) fieldInfo.GetValue(null);

                _opCodes.Add(opCode.Value, opCode);
            }
        }
    }
}