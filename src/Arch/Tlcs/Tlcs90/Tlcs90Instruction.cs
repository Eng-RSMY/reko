﻿#region License
/* 
 * Copyright (C) 1999-2016 John Källén.
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

using Reko.Core.Machine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reko.Arch.Tlcs.Tlcs90
{
    public class Tlcs90Instruction : MachineInstruction
    {
        internal MachineOperand op1;
        internal MachineOperand op2;

        public Opcode Opcode { get; set; }

        public override InstructionClass InstructionClass 
        {
            get { return InstructionClass.Linear; }
        }

        public override bool IsValid
        {
            get { return Opcode == Opcode.invalid; }
        }

        public override int OpcodeAsInteger
        {
            get { return (int)Opcode; }
        }

        public override MachineOperand GetOperand(int i)
        {
            throw new NotImplementedException();
        }

        public override void Render(MachineInstructionWriter writer, MachineInstructionWriterOptions options)
        {
            writer.WriteOpcode(Opcode.ToString());
            if (op1 == null)
                return;
            writer.Tab();
            op1.Write(writer, options);
            if (op2 == null)
                return;
            writer.Write(",");
            op2.Write(writer, options);
        }
    }
}
