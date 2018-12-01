using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TinyJitHook.Core.Extensions
{
    public static class ExceptionHandlerByteHelper
    {
        public static List<ExceptionHandler> GetExceptionHandlers(
            this byte[] data, List<Instruction> relatedMethodBody)
        {
            var ret = new List<ExceptionHandler>();

            var ehReader = new BinaryReader(new MemoryStream(data));
            
            byte b = ehReader.ReadByte();
            if ((b & 0x3F) != 1)
                return new List<ExceptionHandler>(); // Not exception handler clauses

            ret.AddRange((b & 0x40) != 0
                             ? ReadBigExceptionHandlers(ehReader, relatedMethodBody)
                             : ReadSmallExceptionHandlers(ehReader, relatedMethodBody));

            return ret;
        }

        public static byte[] GetExceptionHandlerBytes(this List<ExceptionHandler> exceptionHandlers,
                                                      List<Instruction> relatedMethodBody)
        {
            // Recalculate offsets
            byte[] tmpBody = relatedMethodBody.GetInstructionBytes();
            List<Instruction> rBody = tmpBody.GetInstructions().ToList();

            byte[] extraSections;
            if (NeedBigExceptionClauses(exceptionHandlers, relatedMethodBody, rBody))
                extraSections = WriteBigExceptionClauses(exceptionHandlers, relatedMethodBody, rBody);
            else
                extraSections = WriteSmallExceptionClauses(exceptionHandlers, relatedMethodBody, rBody);
            return extraSections;

        }

        private static byte[] WriteBigExceptionClauses(List<ExceptionHandler> exceptionHandlers,
                                                       List<Instruction> relatedMethodBody, List<Instruction> rBody)
        {
            const int maxExceptionHandlers = (0x00FFFFFF - 4) / 24;
            int numExceptionHandlers = exceptionHandlers.Count;
            if (numExceptionHandlers > maxExceptionHandlers) throw new Exception("Too many exception handlers");

            byte[] data = new byte[numExceptionHandlers * 24 + 4];
            using (var ms = new MemoryStream(data))
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((((uint) numExceptionHandlers * 24 + 4) << 8) | 0x41);
                for (var i = 0; i < numExceptionHandlers; i++)
                {
                    var eh = exceptionHandlers[i];
                    eh.WriteBig(writer, relatedMethodBody, rBody);
                }

                if (writer.BaseStream.Position != data.Length)
                    throw new InvalidOperationException();
            }

            return data;
        }

        private static byte[] WriteSmallExceptionClauses(List<ExceptionHandler> exceptionHandlers,
                                                         List<Instruction> relatedMethodBody, List<Instruction> rBody)
        {
            const int maxExceptionHandlers = (0xFF - 4) / 12;
            int numExceptionHandlers = exceptionHandlers.Count;
            if (numExceptionHandlers > maxExceptionHandlers) throw new Exception("Too many exception handlers");

            byte[] data = new byte[numExceptionHandlers * 12 + 4];
            using (var ms = new MemoryStream(data))
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write((((uint) numExceptionHandlers * 12 + 4) << 8) | 1);
                for (var i = 0; i < numExceptionHandlers; i++)
                {
                    var eh = exceptionHandlers[i];
                    eh.WriteSmall(writer, relatedMethodBody, rBody);
                }

                if (writer.BaseStream.Position != data.Length)
                    throw new InvalidOperationException();
            }

            return data;
        }

        private static IEnumerable<ExceptionHandler> ReadBigExceptionHandlers(
            BinaryReader ehReader, List<Instruction> body)
        {
            ehReader.BaseStream.Position--;
            int num = (ushort) ((ehReader.ReadUInt32() >> 8) / 24);
            for (var i = 0; i < num; i++)
            {
                var eh = new ExceptionHandler();
                eh.ReadBig(ehReader, body);
                yield return eh;
            }
        }

        private static IEnumerable<ExceptionHandler> ReadSmallExceptionHandlers(
            BinaryReader ehReader, List<Instruction> body)
        {
            int num = (ushort) ((uint) ehReader.ReadByte() / 12);
            ehReader.BaseStream.Position += 2;
            for (var i = 0; i < num; i++)
            {
                var eh = new ExceptionHandler();
                eh.ReadSmall(ehReader, body);
                yield return eh;
            }
        }

        private static bool NeedBigExceptionClauses(List<ExceptionHandler> exceptionHandlers, List<Instruction> body,
                                                    List<Instruction> rBody)
        {
            // Size must fit in a byte, and since one small exception record is 12 bytes
            // and header is 4 bytes: x*12+4 <= 255 ==> x <= 20
            if (exceptionHandlers.Count > 20)
                return true;

            foreach (var eh in exceptionHandlers)
            {
                if (!FitsInSmallExceptionClause(eh.TryStart, eh.TryEnd, body, rBody))
                    return true;
                if (!FitsInSmallExceptionClause(eh.HandlerStart, eh.HandlerEnd, body, rBody))
                    return true;
            }

            return false;
        }

        private static bool FitsInSmallExceptionClause(Instruction start, Instruction end, List<Instruction> body,
                                                       List<Instruction> rBody)
        {
            uint offs1 = ExceptionHandler.GetOffset(start, body, rBody);
            uint offs2 = ExceptionHandler.GetOffset(end, body, rBody);
            if (offs2 < offs1)
                return false;
            return offs1 <= ushort.MaxValue && offs2 - offs1 <= byte.MaxValue;
        }
    }
}