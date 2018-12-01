using System;
using System.IO;
using System.Reflection.Emit;

namespace TinyJitHook.Core
{
    public sealed class Instruction
    {
        public int Offset { get; set; }


        public OpCode OpCode { get; set; }

        public object Data { get; set; }

        public static Instruction Create(OpCode opcode, object data = null)
        {
            var inst = new Instruction
            {
                OpCode = opcode,
                Data = data
            };
            return inst;
        }

        public void Read(BinaryReader r)
        {
            switch (OpCode.OperandType)
            {
                case OperandType.InlineBrTarget:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineField:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineI:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineI8:
                    Data = r.ReadInt64();
                    break;

                case OperandType.InlineMethod:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineNone:
                    break;

                case OperandType.InlineR:
                    Data = r.ReadDouble();
                    break;

                case OperandType.InlineSig:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineString:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineSwitch:
                    int count = r.ReadInt32() + 1;
                    Data = r.ReadBytes(4 * count);
                    break;

                case OperandType.InlineTok:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineType:
                    Data = r.ReadInt32();
                    break;

                case OperandType.InlineVar:
                    Data = r.ReadBytes(2);
                    break;

                case OperandType.ShortInlineBrTarget:
                    Data = r.ReadByte();
                    break;

                case OperandType.ShortInlineI:
                    Data = r.ReadByte();
                    break;

                case OperandType.ShortInlineR:
                    Data = r.ReadSingle();
                    break;

                case OperandType.ShortInlineVar:
                    Data = r.ReadByte();
                    break;

                default:
                    throw new NotImplementedException();
            }

        }

        public void Write(BinaryWriter w)
        {
            switch (OpCode.OperandType)
            {
                case OperandType.InlineBrTarget:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineField:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineI:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineI8:
                    w.Write((long) Data);
                    break;

                case OperandType.InlineMethod:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineNone:
                    break;

                case OperandType.InlineR:
                    w.Write((double) Data);
                    break;

                case OperandType.InlineSig:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineString:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineSwitch:
                    w.Write(((byte[]) Data).Length / 4 - 1);
                    w.Write((byte[]) Data);
                    break;

                case OperandType.InlineTok:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineType:
                    w.Write((int) Data);
                    break;

                case OperandType.InlineVar:
                    w.Write((byte[]) Data);
                    break;

                case OperandType.ShortInlineBrTarget:
                    w.Write((byte) Data);
                    break;

                case OperandType.ShortInlineI:
                    w.Write((sbyte) Data);
                    break;

                case OperandType.ShortInlineR:
                    w.Write((float) Data);
                    break;

                case OperandType.ShortInlineVar:
                    w.Write((byte) Data);
                    break;

                default:
                    throw new NotImplementedException();
            }

        }

        public override string ToString()
        {
            return $"{Offset:X4}: {OpCode} - {Data}";
        }
    }
}