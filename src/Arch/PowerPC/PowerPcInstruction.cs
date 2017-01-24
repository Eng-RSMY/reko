#region License
/* 
 * Copyright (C) 1999-2017 John K�ll�n.
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

namespace Reko.Arch.PowerPC
{
    public class PowerPcInstruction : MachineInstruction
    {
        private const InstructionClass Transfer = InstructionClass.Transfer;
        private const InstructionClass CondTransfer = InstructionClass.Conditional | InstructionClass.Transfer;
        private const InstructionClass LinkTransfer = InstructionClass.Call | InstructionClass.Transfer;
        private const InstructionClass LinkCondTransfer = InstructionClass.Call | InstructionClass.Transfer | InstructionClass.Conditional;

        private static Dictionary<Opcode, InstructionClass> classOf;

        private Opcode opcode;
        public MachineOperand op1;
        public MachineOperand op2;
        public MachineOperand op3;
        public MachineOperand op4;
        public MachineOperand op5;
        public bool setsCR0;

        public PowerPcInstruction(Opcode opcode)
        {
            this.opcode = opcode;
        }

        public PowerPcInstruction(Opcode opcode, MachineOperand op1, MachineOperand op2, MachineOperand op3, bool setsCR0)
        {
            this.opcode = opcode;
            this.op1 = op1;
            this.op2 = op2;
            this.op3 = op3;
            this.setsCR0 = setsCR0;
        }

        public override bool IsValid { get { return opcode != Opcode.illegal; } }

        public override int OpcodeAsInteger { get { return (int)opcode; } }

        public override InstructionClass InstructionClass
        {
            get {
                InstructionClass cl;
                if (!classOf.TryGetValue(opcode, out cl))
                    cl = InstructionClass.Linear;
                return cl; 
            }
        }

        public int Operands
        {
            get
            {
                if (op1 == null)
                    return 0;
                if (op2 == null)
                    return 1;
                if (op3 == null)
                    return 2;
                if (op4 == null)
                    return 3;
                if (op5 == null)
                    return 4;
                return 5;
            }
        }
	
        public Opcode Opcode
        {
            get { return opcode; }
        }

        public override MachineOperand GetOperand(int i)
        {
            switch (i)
            {
            case 0: return op1;
            case 1: return op2;
            case 2: return op3;
            case 3: return op4;
            case 4: return op5;
            default: return null;
            }
        }

        public override void Render(MachineInstructionWriter writer)
        {
            var op = string.Format("{0}{1}", 
                opcode,
                setsCR0 ? "." : "");
            writer.WriteOpcode(op);
            if (op1 != null)
            {
                writer.Tab();
                op1.Write(true, writer);
                if (op2 != null)
                {
                    writer.Write(',');
                    op2.Write(true, writer);
                    if (op3 != null)
                    {
                        writer.Write(',');
                        op3.Write(true, writer);
                        if (op4 != null)
                        {
                            writer.Write(",");
                            op4.Write(true, writer);
                            if (op5 != null)
                            {
                                writer.Write(",");
                                op5.Write(true, writer);
                            }
                        }
                    }
                }
            }
        }

        public uint DefCc()
        {
            if (setsCR0)
                return 0xF;
            return 0;
        }

        static PowerPcInstruction()
        {
            classOf = new Dictionary<Opcode, InstructionClass>
            {
                { Opcode.illegal,   InstructionClass.Invalid },

                { Opcode.b,         Transfer },
                { Opcode.bc,        CondTransfer },
                { Opcode.bcl,       LinkCondTransfer },
                { Opcode.bclr,      Transfer },
                { Opcode.bclrl,     LinkTransfer },
                { Opcode.bcctr,     CondTransfer },
                { Opcode.bctrl,     LinkTransfer },
                { Opcode.bdnz,      CondTransfer },
                { Opcode.bdnzf,     CondTransfer },
                { Opcode.bdnzfl,    LinkCondTransfer },
                { Opcode.bdnzl,     LinkCondTransfer },
                { Opcode.bdnzt,     CondTransfer },
                { Opcode.bdnztl,    LinkCondTransfer },
                { Opcode.bdz,       CondTransfer },
                { Opcode.bdzf,      CondTransfer },
                { Opcode.bdzfl,     LinkCondTransfer },
                { Opcode.bdzl,      CondTransfer },
                { Opcode.bdzt,      CondTransfer },
                { Opcode.bdztl,     LinkCondTransfer },

                { Opcode.beq,       CondTransfer },
                { Opcode.beql,      LinkCondTransfer },
                { Opcode.beqlr,     CondTransfer },
                { Opcode.beqlrl,    LinkCondTransfer },
                { Opcode.bge,       CondTransfer },
                { Opcode.bgel,      LinkCondTransfer },
                { Opcode.bgelr,     CondTransfer },
                { Opcode.bgelrl,    LinkCondTransfer },
                { Opcode.bgt,       CondTransfer },
                { Opcode.bgtl,      LinkCondTransfer },
                { Opcode.bgtlr,     CondTransfer },
                { Opcode.bgtlrl,    LinkCondTransfer },
                { Opcode.bl,        LinkTransfer },
                { Opcode.ble,       CondTransfer },
                { Opcode.blel,      LinkCondTransfer },
                { Opcode.blelr,     CondTransfer },
                { Opcode.blelrl,    LinkCondTransfer },
                { Opcode.blr,       Transfer },
                { Opcode.blrl,      LinkTransfer },
                { Opcode.blt,       CondTransfer },
                { Opcode.bltl,      LinkCondTransfer },
                { Opcode.bltlr,     CondTransfer },
                { Opcode.bltlrl,    LinkCondTransfer },
                { Opcode.bne,       CondTransfer },
                { Opcode.bnel,      LinkCondTransfer },
                { Opcode.bnelr,     CondTransfer },
                { Opcode.bnelrl,    LinkCondTransfer },
                { Opcode.bns,       CondTransfer },
                { Opcode.bnsl,      LinkCondTransfer },
                { Opcode.bnslr,     CondTransfer },
                { Opcode.bnslrl,    LinkCondTransfer },
                { Opcode.bso,       CondTransfer },
                { Opcode.bsol,      LinkCondTransfer },
                { Opcode.bsolr,     CondTransfer },
                { Opcode.bsolrl,    LinkCondTransfer },
            };
        }
    }

    public class AddressOperand : MachineOperand
    {
        public Address Address;

        public AddressOperand(Address a)
            : base(PrimitiveType.Pointer32)	//$BUGBUG: 64-bit pointers?
        {
            Address = a;
        }

        public override void Write(bool fExplicit, MachineInstructionWriter writer)
        {
            writer.WriteAddress("$" + Address.ToString(), Address);
        }
    }

    public class ConditionOperand : MachineOperand
    {
        public uint condition;

        public ConditionOperand(uint condition) : base(PrimitiveType.Byte)
        {
            this.condition = condition;
        }

        public override void Write(bool fExplicit, MachineInstructionWriter writer)
        {
            if (condition > 3)
                writer.Write("cr{0}+", condition >> 2);
            var s = "";
            switch (condition & 3)
            {
            case 0: s = "lt"; break;
            case 1: s = "gt"; break;
            case 2: s = "eq"; break;
            case 3: s = "so"; break;
            }
            writer.Write(s);
        }
    }
}
