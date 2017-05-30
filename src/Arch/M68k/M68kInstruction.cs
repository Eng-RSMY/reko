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

using Reko.Core;
using Reko.Core.Machine;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reko.Arch.M68k
{
    public class M68kInstruction : MachineInstruction
    {
        private const InstructionClass Linear = InstructionClass.Linear;
        private const InstructionClass Transfer = InstructionClass.Transfer;
        private const InstructionClass Cond = InstructionClass.Conditional;
        private const InstructionClass CallTransfer = InstructionClass.Call | InstructionClass.Transfer;
        private const InstructionClass CondTransfer = InstructionClass.Conditional | InstructionClass.Transfer;

        private static Dictionary<Opcode, InstructionClass> classOf;

        public Opcode code;
        public PrimitiveType dataWidth;
        public MachineOperand op1;
        public MachineOperand op2;
        public MachineOperand op3;

        public override bool IsValid { get { return code != Opcode.illegal; } }

        public override int OpcodeAsInteger { get { return (int)code; } }

        public override MachineOperand GetOperand(int i)
        {
            switch (i)
            {
            case 0: return op1;
            case 1: return op2;
            case 2: return op3;
            default: return null;
            }
        }

        public override void Render(MachineInstructionWriter writer, MachineInstructionWriterOptions options)
        {
            if (code == Opcode.illegal && op1 != null && writer.Platform != null)
            {
                var imm = op1 as M68kImmediateOperand;
                // MacOS uses invalid opcodes to invoke Macintosh Toolbox services. 
                // We may have to generalize the Platform API to allow specifying 
                // the opcode of the invoking instruction, to disambiguate from 
                // "legitimate" TRAP calls.
                var svc = writer.Platform.FindService((int)imm.Constant.ToUInt32(), null);
                if (svc != null)
                {
                    writer.Write(svc.Name);
                    return;
                }
            }
            if (dataWidth != null)
            {
                writer.WriteOpcode(string.Format("{0}{1}", code, DataSizeSuffix(dataWidth)));
            }
            else
            {
                writer.WriteOpcode(code.ToString());
            }
            writer.Tab();
            if (op1 != null)
            {
                op1.Write(writer, options);
                if (op2 != null)
                {
                    writer.Write(',');
                    op2.Write(writer, options);
                }
            }
        }

        private string DataSizeSuffix(PrimitiveType dataWidth)
        {
            if (dataWidth.Domain == Domain.Real)
            {
                switch (dataWidth.BitSize)
                {
                case 32: return ".s";
                case 64: return ".d";
                case 80: return ".x";   //$REVIEW: not quite true?
                case 96: return ".x";
                }
            }
            else
            {
                switch (dataWidth.BitSize)
                {
                case 8: return ".b";
                case 16: return ".w";
                case 32: return ".l";
                }
            }
            throw new InvalidOperationException(string.Format("Unsupported data width {0}.", dataWidth.BitSize));
        }

        public override InstructionClass InstructionClass
        {
            get
            {
                InstructionClass cl;
                if (!classOf.TryGetValue(code, out cl))
                    cl = InstructionClass.Linear;
                return cl;
            }
        }

        static M68kInstruction()
        {
            classOf = new Dictionary<Opcode, InstructionClass>
            {
                { Opcode.illegal, InstructionClass.Invalid },

                { Opcode.bcc,      CondTransfer },
                { Opcode.bcs,      CondTransfer },
                { Opcode.beq,      CondTransfer },
                { Opcode.bge,      CondTransfer },
                { Opcode.bgt,      CondTransfer },
                { Opcode.bhi,      CondTransfer },
                { Opcode.ble,      CondTransfer },
                { Opcode.blt,      CondTransfer },
                { Opcode.bmi,      CondTransfer },
                { Opcode.bne,      CondTransfer },
                { Opcode.bpl,      CondTransfer },
                { Opcode.bra,      Transfer },
                { Opcode.bsr,      CallTransfer },
                { Opcode.bvc,      CondTransfer },
                { Opcode.bvs,      CondTransfer },

                { Opcode.callm,    CallTransfer },
                { Opcode.jmp,      Transfer },
                { Opcode.jsr,      CallTransfer },
                { Opcode.reset,    Transfer },

                { Opcode.rtd,      Transfer },
                { Opcode.rte,      Transfer },
                { Opcode.rtm,      Transfer },
                { Opcode.rtr,      Transfer },
                { Opcode.rts,      Transfer },

                { Opcode.traphi,   CondTransfer },
                { Opcode.trapls,   CondTransfer },
                { Opcode.trapcc,   CondTransfer },
                { Opcode.trapcs,   CondTransfer },
                { Opcode.trapne,   CondTransfer },
                { Opcode.trapeq,   CondTransfer },
                { Opcode.trapvc,   CondTransfer },
                { Opcode.trapvs,   CondTransfer },
                { Opcode.trappl,   CondTransfer },
                { Opcode.trapmi,   CondTransfer },
                { Opcode.trapge,   CondTransfer },
                { Opcode.traplt,   CondTransfer },
                { Opcode.trapgt,   CondTransfer },
                { Opcode.traple,   CondTransfer },
                { Opcode.trapv,    CondTransfer },
            };
        }
    }
}
