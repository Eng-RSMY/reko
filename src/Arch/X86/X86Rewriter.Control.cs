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
using Reko.Core.Code;
using Reko.Core.Expressions;
using Reko.Core.Machine;
using Reko.Core.Operators;
using Reko.Core.Rtl;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace Reko.Arch.X86
{
    /// <summary>
    /// Rewrite rules for control flow instructions.
    /// </summary>
    public partial class X86Rewriter
    {
        private Expression CreateTestCondition(ConditionCode cc, Opcode opcode)
        {
            var grf = orw.FlagGroup(X86Instruction.UseCc(opcode));
            var tc = new TestCondition(cc, grf);
            return tc;
        }

            /*
            if (i < 2)
                return tc;
            if (instrs[i-1].code != Opcode.test)
                return tc;
            var ah = instrs[i-1].op1 as RegisterOperand;
            if (ah == null || ah.Register != Registers.ah)
                return tc;
            var m = instrs[i-1].op2 as ImmediateOperand;
            if (m == null)
                return tc;

            if (instrs[i-2].code != Opcode.fstsw)
                return tc;
            int mask = m.Value.ToInt32();

            var fpuf = orw.FlagGroup(FlagM.FPUF);
            switch (cc)
            {
            case ConditionCode.PE:
                if (mask == 0x05) return new TestCondition(ConditionCode.LE, fpuf);
                if (mask == 0x41) return new TestCondition(ConditionCode.GE, fpuf);
                if (mask == 0x44) return new TestCondition(ConditionCode.NE, fpuf);
                break;
            case ConditionCode.PO:
                if (mask == 0x44) return new TestCondition(ConditionCode.EQ, fpuf);
                if (mask == 0x41) return new TestCondition(ConditionCode.GE, fpuf);
                if (mask == 0x05) return new TestCondition(ConditionCode.GT, fpuf);
                break;
            case ConditionCode.EQ:
                if (mask == 0x40) return new TestCondition(ConditionCode.NE, fpuf);
                if (mask == 0x41) return new TestCondition(ConditionCode.LT, fpuf);
                break;
            case ConditionCode.NE:
                if (mask == 0x40) return new TestCondition(ConditionCode.EQ, fpuf);
                if (mask == 0x41) return new TestCondition(ConditionCode.GE, fpuf);
                if (mask == 0x01) return new TestCondition(ConditionCode.GT, fpuf);
                break;
            }
            throw new NotImplementedException(string.Format(
                "FSTSW/TEST AH,0x{0:X2}/J{1} not implemented.", mask, cc));
             */

        private void RewriteCall(MachineOperand callTarget, PrimitiveType opsize)
        {
            Address addr = OperandAsCodeAddress(callTarget);
            if (addr != null)
            {
                if (addr.ToLinear() == (dasm.Current.Address + dasm.Current.Length).ToLinear())
                {
                    // Calling the following address. Is the call followed by a 
                    // pop?
                    var next = dasm.Peek(1);
                    RegisterOperand reg = next.op1 as RegisterOperand;
                    if (next.code == Opcode.pop && reg != null)
                    {
                        // call $+5,pop<reg> idiom
                        dasm.MoveNext();
                        m.Assign(
                            orw.AluRegister(reg),
                            addr);
                        this.len += 1;
                        return;
                    }
                }
                m.Call(addr, (byte) opsize.Size);
            }
            else
            {
                var target = SrcOp(callTarget);
                if (target.DataType.Size == 2)
                    target = m.Seq(orw.AluRegister(Registers.cs), target);
                m.Call(target, (byte) opsize.Size);
            }
            rtlc = RtlClass.Transfer | RtlClass.Call;
        }

        private void RewriteConditionalGoto(ConditionCode cc, MachineOperand op1)
        {
            rtlc = RtlClass.ConditionalTransfer;
            m.Branch(CreateTestCondition(cc, instrCur.code), OperandAsCodeAddress(op1), RtlClass.ConditionalTransfer);
        }

        private void RewriteInt()
        {
            m.SideEffect(host.PseudoProcedure(PseudoProcedure.Syscall, VoidType.Instance, SrcOp(instrCur.op1)));
            rtlc = RtlClass.Call | RtlClass.Transfer;
        }

        private void RewriteInto()
        {
            m.BranchInMiddleOfInstruction(
                m.Test(ConditionCode.NO, orw.FlagGroup(FlagM.OF)),
                instrCur.Address + instrCur.Length,
                RtlClass.ConditionalTransfer);
            m.SideEffect(
                    host.PseudoProcedure(PseudoProcedure.Syscall, VoidType.Instance, Constant.Byte(4)));
        }

        private void RewriteJcxz()
        {
            m.Branch(
                m.Eq0(orw.AluRegister(Registers.ecx, instrCur.dataWidth)),
                OperandAsCodeAddress(instrCur.op1),
                RtlClass.ConditionalTransfer);
            rtlc = RtlClass.ConditionalTransfer;
        }

        private void RewriteJmp()
        {
            if (IsRealModeReboot(instrCur))
			{
                PseudoProcedure reboot = host.EnsurePseudoProcedure("__bios_reboot", VoidType.Instance, 0);
                reboot.Characteristics = new Core.Serialization.ProcedureCharacteristics();
                reboot.Characteristics.Terminates = true;
                m.SideEffect(PseudoProc(reboot, VoidType.Instance));
				return;
			}

            rtlc = RtlClass.Transfer;
			if (instrCur.op1 is ImmediateOperand)
			{
				Address addr = OperandAsCodeAddress(instrCur.op1);
                m.Goto(addr);
				return;
			}
            m.Goto(SrcOp(instrCur.op1));
        }

        private void RewriteLoop(FlagM useFlags, ConditionCode cc)
        {
            Identifier cx = orw.AluRegister(Registers.rcx, instrCur.dataWidth);
            m.Assign(cx, m.ISub(cx, 1));
            if (useFlags != 0)
            {
                m.Branch(
                    m.Cand(
                        m.Test(cc, orw.FlagGroup(useFlags)),
                        m.Ne0(cx)),
                    OperandAsCodeAddress(instrCur.op1),
                    RtlClass.ConditionalTransfer);
                rtlc = RtlClass.ConditionalTransfer;
            }
            else
            {
                m.Branch(m.Ne0(cx), OperandAsCodeAddress(instrCur.op1), RtlClass.ConditionalTransfer);
                rtlc = RtlClass.ConditionalTransfer;
            }
        }

        public void RewriteRet()
        {
            int extraBytesPopped = instrCur.Operands == 1 
                ? ((ImmediateOperand)instrCur.op1).Value.ToInt32() 
                : 0;
            if ((extraBytesPopped & 1) == 1)
            {
                // Unlikely that an odd number of bytes are pushed on the stack.
                rtlc = RtlClass.Invalid;
                m.Invalid();
                return;
            }
            m.Return(
                this.arch.WordWidth.Size + (instrCur.code == Opcode.retf ? Registers.cs.DataType.Size : 0),
                extraBytesPopped);
            rtlc = RtlClass.Transfer;
        }

        public void RewriteIret()
        {
            RewritePop(
                orw.FlagGroup(FlagM.SF | FlagM.CF | FlagM.ZF | FlagM.OF), instrCur.dataWidth);
            m.Return(
                Registers.cs.DataType.Size +
                arch.WordWidth.Size, 
                0);
            rtlc = RtlClass.Transfer;
        }

        /// <summary>
        /// A jump to 0xFFFF:0x0000 in real mode is a reboot.
        /// </summary>
        /// <param name="instrCur"></param>
        /// <returns></returns>
        private bool IsRealModeReboot(X86Instruction instrCur)
        {
            var addrOp = instrCur.op1 as X86AddressOperand;
            bool isRealModeReboot = addrOp != null && addrOp.Address.ToLinear() == 0xFFFF0;
            return isRealModeReboot;
        }

        public Address OperandAsCodeAddress(MachineOperand op)
        {
            AddressOperand ado = op as AddressOperand;
            if (ado != null)
                return ado.Address;
            ImmediateOperand imm = op as ImmediateOperand;
            if (imm != null)
            {
                return orw.ImmediateAsAddress(instrCur.Address, imm);
            }
            return null;
        }
    }
}
