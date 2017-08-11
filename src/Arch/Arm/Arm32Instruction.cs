﻿#region License
/* 
 * Copyright (C) 1999-2017 John Källén.
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2, or (at your option)
 * any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; see the file COPYING.  If not, write to
 * the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA.
 */
#endregion

using Gee.External.Capstone.Arm;
using Reko.Core;
using Reko.Core.Machine;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CapstoneArmInstruction = Gee.External.Capstone.Instruction<Gee.External.Capstone.Arm.ArmInstruction, Gee.External.Capstone.Arm.ArmRegister, Gee.External.Capstone.Arm.ArmInstructionGroup, Gee.External.Capstone.Arm.ArmInstructionDetail>;
using Opcode = Gee.External.Capstone.Arm.ArmInstruction;

namespace Reko.Arch.Arm
{
    public class Arm32Instruction : MachineInstruction 
    {
        private static Dictionary<ArmInstruction, InstructionClass> classOf;

        // If this instruction is NULL, then the instruction is invalid.
        // Callers need to be aware of this.
        internal CapstoneArmInstruction instruction;
        
        public Arm32Instruction(CapstoneArmInstruction instruction)
        {
            if (instruction == null)
                throw new ArgumentNullException("instruction");
            this.instruction = instruction;
            this.Address = Address.Ptr32((uint)instruction.Address);
            this.Length = instruction.Bytes.Length;
        }

        private Arm32Instruction(Address addr)
        {
            this.instruction = null;
            this.Address = addr;
            this.Length = 2;
        }

        /// <summary>
        /// Gets the inner, Capstone-originated instruction. If the instruction
        /// is null, it means it is invalid (ie. a garbage opcode).
        /// </summary>
        /// <param name="instr"></param>
        /// <returns>True if the instruction is valid, false otherwise.</returns>
        internal bool TryGetInternal(out CapstoneArmInstruction instr)
        {
            instr = instruction;
            return instr != null;
        }

        public override InstructionClass InstructionClass
        {
            get
            {
                if (instruction == null)
                {
                    return InstructionClass.Invalid;
                }
                InstructionClass ct;
                if (!classOf.TryGetValue(instruction.Id, out ct))
                {
                    ct = InstructionClass.Linear;
                }
                if (instruction.ArchitectureDetail.CodeCondition != ArmCodeCondition.AL)
                {
                    //$TODO: test conditional moves etc. to make sure Capstone works
                    ct |= InstructionClass.Conditional;
                }
                return ct;
            }
        }

        public override bool IsValid { get { return instruction.Id != Opcode.Invalid; } }

        public override int OpcodeAsInteger {
            get { return (int) instruction.Id; }
        }

        public override MachineOperand GetOperand(int i)
        {
            return null;
        }

        public override void Render(MachineInstructionWriter writer, MachineInstructionWriterOptions options)
        {
            if (instruction == null)
            {
                writer.Write("Invalid");
                return;
            }
            writer.WriteOpcode(instruction.Mnemonic);
            var ops = instruction.ArchitectureDetail.Operands;
            if (ops.Length < 1)
                return;
            writer.Tab();
            if (WriteRegisterSetInstruction(writer))
                return;
            Write(ops[0], writer, options);
            if (ops.Length < 2)
                return;
            writer.Write(",");
            Write(ops[1], writer, options);
            if (ops.Length < 3)
                return;
            writer.Write(",");
            Write(ops[2], writer, options);
            if (ops.Length < 4)
                return;
            writer.Write(",");
            Write(ops[3], writer, options);
        }

        private bool WriteRegisterSetInstruction(MachineInstructionWriter writer)
        {
            IEnumerable<ArmInstructionOperand> ops = 
                instruction.ArchitectureDetail.Operands;
            switch (instruction.Id)
            {
            case Opcode.POP:
            case Opcode.PUSH:
                break;
            case Opcode.LDM:
            case Opcode.STM:
            case Opcode.STMDB:
                Write(ops.First(), writer, MachineInstructionWriterOptions.None);
                if (instruction.ArchitectureDetail.WriteBack)
                    writer.Write("!");
                ops = ops.Skip(1);
                writer.Write(",");
                break;
            default:
                return false;
            }

            writer.Write("{");
            var sep = "";
            RegisterStorage regPrev = null;
            RegisterStorage reg = null;
            foreach (var op in ops)
            {
                reg = A32Registers.RegisterByCapstoneID[op.RegisterValue.Value];
                if (regPrev == null)
                {
                    writer.Write(sep);
                    writer.Write(reg.Name);
                    sep = ",";
                }
                else if (regPrev.Number + 1 < reg.Number)
                {
                    if (sep == "-")
                    {
                        writer.Write(sep);
                        writer.Write(regPrev.Name);
                        sep = ",";
                    }
                    writer.Write(sep);
                    writer.Write(reg.Name);
                    sep = ",";
                }
                else
                {
                    sep = "-";
                }
                regPrev = reg;
            }
            if (sep == "-")
            {
                writer.Write("-");
                writer.Write(reg.Name);
            }
            writer.Write("}");
            return true;
        }

        private static readonly char[] nosuffixRequired = new[] { '.', 'E', 'e' };

        public void Write(ArmInstructionOperand op, MachineInstructionWriter writer, MachineInstructionWriterOptions options)
        {
            switch (op.Type)
            {
            case ArmInstructionOperandType.Immediate:
                if (instruction.Id == Opcode.B ||
                    instruction.Id == Opcode.BL ||
                    instruction.Id == Opcode.BLX)
                {
                    writer.Write("$");
                    writer.WriteAddress(
                        string.Format("{0:X8}", op.ImmediateValue.Value),
                        Address.Ptr32((uint)op.ImmediateValue.Value));
                    break;
                }
                writer.Write("#");
                WriteImmediateValue(op.ImmediateValue.Value, writer);
                break;
            case ArmInstructionOperandType.CImmediate:
                writer.Write("c{0}", op.ImmediateValue);
                break;
            case ArmInstructionOperandType.PImmediate:
                writer.Write("p{0}", op.ImmediateValue);
                break;
            case ArmInstructionOperandType.Register:
                if (op.IsSubtracted)
                    writer.Write('-');
                writer.Write(A32Registers.RegisterByCapstoneID[op.RegisterValue.Value].Name);
                WriteShift(op, writer);
                break;
            case ArmInstructionOperandType.SysRegister:
                writer.Write(A32Registers.SysRegisterByCapstoneID[op.SysRegisterValue.Value].Name);
                break;
            case ArmInstructionOperandType.Memory:
                if (op.MemoryValue.BaseRegister == ArmRegister.PC)
                {
                    var uAddr = (uint)((int)this.Address.ToUInt32() + op.MemoryValue.Displacement) + 8u;
                    var addr = Address.Ptr32(uAddr);
                    if ((options & MachineInstructionWriterOptions.ResolvePcRelativeAddress) != 0)
                    {
                        writer.Write('[');
                        writer.WriteAddress(addr.ToString(), addr);
                        writer.Write(']');
                        var sr = new StringRenderer();
                        WriteMemoryOperand(op, sr);
                        writer.AddAnnotation(sr.ToString());
                    }
                    else
                    {
                        WriteMemoryOperand(op, writer);
                        writer.AddAnnotation(addr.ToString());
                    }
                    return;
                }
                WriteMemoryOperand(op, writer);
                break;
            case ArmInstructionOperandType.SetEnd:
                writer.Write(op.SetEndValue.ToString().ToLowerInvariant());
                break;
            case ArmInstructionOperandType.FloatingPoint:
                var f = op.FloatingPointValue.Value.ToString("g", CultureInfo.InvariantCulture);
                if (f.IndexOfAny(nosuffixRequired) < 0)
                    f += ".0";
                writer.Write("#{0}", f);
                break;
            default:
                throw new NotImplementedException(string.Format(
                    "Can't disassemble {0} {1}. Unknown operand type: {2}",
                    instruction.Mnemonic,
                    instruction.Operand,
                    op.Type));
            }
        }

        private void WriteShift(ArmInstructionOperand op, MachineInstructionWriter writer)
        {
            switch (op.Shifter.Type)
            {
            case ArmShifterType.ASR: WriteImmShift("asr", op.Shifter.Value, writer); break;
            case ArmShifterType.LSL: WriteImmShift("lsl", op.Shifter.Value, writer); break;
            case ArmShifterType.LSR: WriteImmShift("lsr", op.Shifter.Value, writer); break;
            case ArmShifterType.ROR: WriteImmShift("ror", op.Shifter.Value, writer); break;
            case ArmShifterType.RRX: writer.Write(",rrx"); break;
            case ArmShifterType.ASR_REG: WriteRegShift("asr", op.Shifter.Value, writer); break;
            case ArmShifterType.LSL_REG: WriteRegShift("lsl", op.Shifter.Value, writer); break;
            case ArmShifterType.LSR_REG: WriteRegShift("lsr", op.Shifter.Value, writer); break;
            case ArmShifterType.ROR_REG: WriteRegShift("ror", op.Shifter.Value, writer); break;
            case ArmShifterType.RRX_REG: WriteRegShift("rrx", op.Shifter.Value, writer); break;
            case ArmShifterType.Invalid: break;
            }
        }

        private void WriteMemoryOperand(ArmInstructionOperand op, MachineInstructionWriter writer)
        {
            writer.Write('[');
            writer.Write(A32Registers.RegisterByCapstoneID[op.MemoryValue.BaseRegister].Name);
            int displacement = op.MemoryValue.Displacement;
            if (displacement != 0)
            {
                if (true) // preincInternal.ArchitectureDetail)
                {
                    writer.Write(",");
                    if (displacement < 0)
                    {
                        displacement = -displacement;
                        writer.Write("-");
                    }
                    writer.Write("#");
                    WriteImmediateValue(displacement, writer);
                    writer.Write("]");
                    if (instruction.ArchitectureDetail.WriteBack)
                        writer.Write("!");
                }
                else
                {
                    writer.Write("],");
                    if (displacement < 0)
                    {
                        displacement = -displacement;
                        writer.Write("-");
                    }
                    WriteImmediateValue(displacement, writer);
                }
            }
            else
            {
                if (op.MemoryValue.IndexRegister != ArmRegister.Invalid)
                {
                    writer.Write(",");
                    // NOTE: capstone.NET seems to reverse the sense of this scale parameter.
                    if (op.IsSubtracted)
                        writer.Write("-");
                    writer.Write(A32Registers.RegisterByCapstoneID[op.MemoryValue.IndexRegister].Name);
                }
                if (op.Shifter.Type != ArmShifterType.Invalid)
                {
                    WriteShift(op, writer);
                }
                writer.Write(']');
                if (instruction.ArchitectureDetail.WriteBack && IsLastOperand(op))
                    writer.Write("!");
            
            }
        }

        /// <summary>
        /// Returns true if <paramref name="op"/> is the last operand of the instruction.
        /// </summary>
        /// <param name="op"></param>
        /// <returns></returns>
        public bool IsLastOperand(ArmInstructionOperand op)
        {
            var ops = instruction.ArchitectureDetail.Operands;
            return op == ops[ops.Length-1];
        }

        private void WriteImmShift(string op, int value, MachineInstructionWriter writer)
        {
            writer.Write(",");
            writer.WriteOpcode(op);
            writer.Write(" #");
            WriteImmediateValue(value, writer);
        }

        private void WriteRegShift(string op, int value, MachineInstructionWriter writer)
        {
            writer.Write(",");
            writer.WriteOpcode(op);
            writer.Write(' ');
            writer.Write(A32Registers.RegisterByCapstoneID[(ArmRegister)value].Name);
        }

        private static void WriteImmediateValue(int imm8, MachineInstructionWriter writer)
        {
            if (imm8 > 256 && ((imm8 & (imm8 - 1)) == 0))
            {
                /* only one bit set, and that later than bit 8.
                 * Represent as 1<<... .
                 */
                writer.Write("1<<");
                {
                    uint n = 0;
                    while ((imm8 & 15) == 0)
                    {
                        n += 4; imm8 = imm8 >> 4;
                    }
                    // Now imm8 is 1, 2, 4 or 8. 
                    n += (uint)((0x30002010 >> (int)(4 * (imm8 - 1))) & 15);
                    writer.Write(n);
                }
            }
            else
            {
                var fmt = (-9 <= imm8 && imm8 <= 9) ? "{0}{1}" : "&{0}{1:X}";
                var sign = "";
                if (((int)imm8) < 0 && ((int)imm8) > -100)
                {
                    imm8 = -imm8;
                    sign = "-";
                }
                writer.Write(fmt, sign, imm8);
            }
        }

        internal static Arm32Instruction CreateInvalid(Address addr)
        {
            return new Arm32Instruction(addr);
        }

        static Arm32Instruction()
        {
            classOf = new Dictionary<Opcode, InstructionClass>
            {
                { Opcode.Invalid, InstructionClass.Invalid },

                { Opcode.BKPT,  InstructionClass.Transfer },
                { Opcode.BL,    InstructionClass.Transfer | InstructionClass.Call },
                { Opcode.BLX,   InstructionClass.Transfer | InstructionClass.Call },
                { Opcode.BX,    InstructionClass.Transfer },
                { Opcode.BXJ,   InstructionClass.Transfer },
                { Opcode.B,     InstructionClass.Transfer },
                { Opcode.HLT,   InstructionClass.Transfer },
                { Opcode.SVC,   InstructionClass.Transfer },
                { Opcode.TEQ,   InstructionClass.Transfer },
                { Opcode.TRAP,  InstructionClass.Transfer },
                { Opcode.YIELD, InstructionClass.Transfer },
            };
        }
    }
}
