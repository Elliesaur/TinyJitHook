using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TinyJitHook.Extensions;

namespace TinyJitHook
{
    // Credits: https://github.com/0xd4d/dnlib
    //  License: https://github.com/0xd4d/dnlib/blob/master/LICENSE.txt

    /// <summary>
    /// A CIL method exception handler
    /// </summary>
    public sealed class ExceptionHandler
    {
        /// <summary>
        /// First instruction of try block
        /// </summary>
        public Instruction TryStart;

        /// <summary>
        /// One instruction past the end of try block or <c>null</c> if it ends at the end
        /// of the method.
        /// </summary>
        public Instruction TryEnd;

        /// <summary>
        /// Start of filter handler or <c>null</c> if none. The end of filter handler is
        /// always <see cref="HandlerStart"/>.
        /// </summary>
        public Instruction FilterStart;

        /// <summary>
        /// First instruction of try handler block
        /// </summary>
        public Instruction HandlerStart;

        /// <summary>
        /// One instruction past the end of try handler block or <c>null</c> if it ends at the end
        /// of the method.
        /// </summary>
        public Instruction HandlerEnd;

        /// <summary>
        /// Type of exception handler clause
        /// </summary>
        public ExceptionHandlerType HandlerType;

        public uint CatchTypeToken;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ExceptionHandler()
        {
        }

        public static Instruction GetInstruction(uint off, List<Instruction> body)
        {
            return body.FirstOrDefault(inst => inst.Offset == off);
        }

        public static uint GetOffset(Instruction inst, List<Instruction> body, List<Instruction> recalcBody)
        {
            // TODO: Index of does not work with the recalculated body, the instruction is different
            // Custom comparator!?
            int index1 = body.IndexOf(inst);
            if (index1 < 0)
            {
                return uint.MaxValue;
            }

            int index2 = recalcBody.IndexOf(inst);
            if (index2 < 0)
            {
                // Hack to fix this, rely on the first index.
                index2 = index1;
            }

            return body[index1].Offset == 0 ? (uint) recalcBody[index2].Offset : (uint) body[index1].Offset;
        }

        public void ReadBig(BinaryReader r, List<Instruction> body)
        {
            HandlerType = (ExceptionHandlerType)r.ReadUInt32();
            uint offset = r.ReadUInt32();
            TryStart = GetInstruction(offset, body);
            TryEnd = GetInstruction(offset + r.ReadUInt32(), body);

            offset = r.ReadUInt32();
            HandlerStart = GetInstruction(offset, body);
            HandlerEnd = GetInstruction(offset + r.ReadUInt32(), body);
            if (HandlerType == ExceptionHandlerType.Catch)
            {
                CatchTypeToken = r.ReadUInt32();
            }
            else if (HandlerType == ExceptionHandlerType.Filter)
            {
                FilterStart = GetInstruction(r.ReadUInt32(), body);
            }
            else
            {
                r.ReadUInt32();
            }
        }

        public void ReadSmall(BinaryReader r, List<Instruction> body)
        {
            HandlerType = (ExceptionHandlerType) r.ReadUInt16();
            uint offset = r.ReadUInt16();
            TryStart = GetInstruction(offset, body);
            TryEnd = GetInstruction(offset + r.ReadByte(), body);

            offset = r.ReadUInt16();
            HandlerStart = GetInstruction(offset, body);
            HandlerEnd = GetInstruction(offset + r.ReadByte(), body);
            if (HandlerType == ExceptionHandlerType.Catch)
            {
                CatchTypeToken = r.ReadUInt32();
            }
            else if (HandlerType == ExceptionHandlerType.Filter)
            {
                FilterStart = GetInstruction(r.ReadUInt32(), body);
            }
            else
            {
                r.ReadUInt32();
            }
        }

        public void WriteBig(BinaryWriter w, List<Instruction> body, List<Instruction> rBody)
        {
            w.Write((uint)HandlerType);

            uint offs1 = GetOffset(TryStart, body, rBody);
            uint offs2 = GetOffset(TryEnd, body, rBody);
            if (offs2 <= offs1)
                throw new Exception("Exception handler: TryEnd <= TryStart");
            w.Write(offs1);
            w.Write(offs2 - offs1);

            offs1 = GetOffset(HandlerStart, body, rBody);
            offs2 = GetOffset(HandlerEnd, body, rBody);
            if (offs2 <= offs1)
                throw new Exception("Exception handler: HandlerEnd <= HandlerStart");
            w.Write(offs1);
            w.Write(offs2 - offs1);

            if (HandlerType == ExceptionHandlerType.Catch)
                w.Write(CatchTypeToken);
            else if (HandlerType == ExceptionHandlerType.Filter)
                w.Write(GetOffset(FilterStart, body, rBody));
            else
                w.Write(0);
        }
        public void WriteSmall(BinaryWriter w, List<Instruction> body, List<Instruction> rBody)
        {
            w.Write((ushort)HandlerType);

            uint offs1 = GetOffset(TryStart, body, rBody);
            uint offs2 = GetOffset(TryEnd, body, rBody);
            if (offs2 <= offs1)
                throw new Exception("Exception handler: TryEnd <= TryStart");
            w.Write((ushort)offs1);
            w.Write((byte)(offs2 - offs1));

            offs1 = GetOffset(HandlerStart, body, rBody);
            offs2 = GetOffset(HandlerEnd, body, rBody);
            if (offs2 <= offs1)
                throw new Exception("Exception handler: HandlerEnd <= HandlerStart");
            w.Write((ushort)offs1);
            w.Write((byte)(offs2 - offs1));

            if (HandlerType == ExceptionHandlerType.Catch)
                w.Write(CatchTypeToken);
            else if (HandlerType == ExceptionHandlerType.Filter)
                w.Write(GetOffset(FilterStart, body, rBody));
            else
                w.Write(0);
        }
    }

    /// <summary>
    /// Type of exception handler. See CorHdr.h/CorExceptionFlag
    /// </summary>
    [Flags]
    public enum ExceptionHandlerType
    {
        /// <summary/>
        Catch = 0x0000,
        /// <summary/>
        Filter = 0x0001,
        /// <summary/>
        Finally = 0x0002,
        /// <summary/>
        Fault = 0x0004,
        /// <summary/>
        Duplicated = 0x0008,
    }
}
