﻿#region License
/* 
 * Copyright (C) 1999-2013 John Källén.
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

using Decompiler.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Decompiler.Arch.Mos6502
{
    public class Mos6502ProcessorState : ProcessorState
    {
        private Mos6502ProcessorArchitecture arch;

        public Mos6502ProcessorState(Mos6502ProcessorArchitecture arch)
        {
            this.arch = arch;
        }

        public override IProcessorArchitecture Architecture
        {
            get { return arch; }
        }

        public override ProcessorState Clone()
        {
            throw new NotImplementedException();
        }

        public override Core.Expressions.Constant GetRegister(RegisterStorage r)
        {
            throw new NotImplementedException();
        }

        public override void SetRegister(RegisterStorage r, Core.Expressions.Constant v)
        {
            throw new NotImplementedException();
        }

        public override void SetInstructionPointer(Address addr)
        {
            throw new NotImplementedException();
        }

        public override void OnProcedureEntered()
        {
            throw new NotImplementedException();
        }

        public override void OnProcedureLeft(ProcedureSignature procedureSignature)
        {
            throw new NotImplementedException();
        }

        public override Core.Code.CallSite OnBeforeCall(Core.Expressions.Identifier stackReg, int returnAddressSize)
        {
            throw new NotImplementedException();
        }

        public override void OnAfterCall(Core.Expressions.Identifier stackReg, ProcedureSignature sigCallee, Core.Expressions.ExpressionVisitor<Core.Expressions.Expression> eval)
        {
            throw new NotImplementedException();
        }
    }
}