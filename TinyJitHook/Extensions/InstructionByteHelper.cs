using System.Collections.Generic;
using System.IO;

namespace TinyJitHook.Extensions
{
    public static class InstructionByteHelper
    {
        public static List<Instruction> GetInstructions(this byte[] bytes)
        {
            var ret = new List<Instruction>();
            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader r = new BinaryReader(ms))
            {
                while (r.BaseStream.Position < bytes.Length)
                {
                    Instruction instruction = new Instruction { Offset = (int)r.BaseStream.Position };

                    short code = r.ReadByte();
                    if (code == 0xfe)
                    {
                        code = (short)(r.ReadByte() | 0xfe00);
                    }

                    instruction.OpCode = code.GetOpCode();
                    instruction.Read(r);

                    ret.Add(instruction);
                }
            }

            return ret;
        }

        public static byte[] GetInstructionBytes(this List<Instruction> instructions)
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter w = new BinaryWriter(ms))
            {
                foreach (var inst in instructions)
                {
                    if (inst.OpCode.Size == 1)
                    {
                        w.Write((byte)inst.OpCode.Value);
                    }
                    else
                    {
                        w.Write(inst.OpCode.Value);
                    }

                    inst.Write(w);
                }

                return ms.ToArray();
            }
        }
    }
}
