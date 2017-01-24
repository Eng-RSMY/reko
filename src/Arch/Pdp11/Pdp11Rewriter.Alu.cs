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
using Reko.Core.Expressions;
using Reko.Core.Machine;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.Arch.Pdp11
{
    public partial class Pdp11Rewriter
    {
        private void RewriteAdc(Pdp11Instruction instr)
        {
            var src = frame.EnsureFlagGroup(this.arch.GetFlagGroup((uint)FlagM.CF));
            var dst = RewriteDst(instr.op1, src, m.IAdd);
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteAdd(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op2, src, m.IAdd);
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteAsl(Pdp11Instruction instr)
        {
            var src = Constant.Int16(1);
            var dst = RewriteDst(instr.op1, src, m.Shl);
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteAsr(Pdp11Instruction instr)
        {
            var src = Constant.Int16(1);
            var dst = RewriteDst(instr.op1, src, m.Sar);
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteBic(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op2, src, (a, b) => m.And(a, m.Comp(b)));
            SetFlags(dst, FlagM.NF | FlagM.ZF, FlagM.VF, 0);
        }

        private void RewriteBis(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op2, src, m.Or);
            SetFlags(dst, FlagM.NF | FlagM.ZF, FlagM.VF, 0);
        }

        private void RewriteBit(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op2, src, m.And);
            SetFlags(dst, FlagM.NF | FlagM.ZF, FlagM.VF, 0);
        }

        private void RewriteClr(Pdp11Instruction instr, Expression src)
        {
            var dst = RewriteDst(instr.op1, src, s => s);
            SetFlags(dst, 0, FlagM.NF | FlagM.CF | FlagM.VF, FlagM.ZF);
        }

        private void RewriteCmp(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteSrc(instr.op2);
            var tmp = frame.CreateTemporary(src.DataType);
            m.Assign(tmp, m.ISub(dst, src));
            SetFlags(tmp, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteCom(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op1, src, m.Comp);
            SetFlags(dst, FlagM.NF | FlagM.ZF, FlagM.VF, FlagM.CF);
        }
    
        private void RewriteDiv(Pdp11Instruction instr)
        {
            var reg = ((RegisterOperand)instr.op2).Register;
            var reg1 = arch.GetRegister(reg.Number | 1);
            var reg_reg = frame.EnsureSequence(reg, reg1, PrimitiveType.Int32);
            var dividend = frame.CreateTemporary(PrimitiveType.Int32);
            var divisor = RewriteSrc(instr.op1);
            var quotient = frame.EnsureRegister(reg);
            var remainder = frame.EnsureRegister(reg1);
            m.Assign(dividend, reg_reg);
            m.Assign(quotient, m.SDiv(dividend, divisor));
            m.Assign(remainder, m.Mod(dividend, divisor));
            SetFlags(quotient, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteIncDec(Pdp11Instruction instr, Func<Expression, int, Expression> fn)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op1, src, s => fn(s, 1));
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF, 0, 0);
        }

        private void RewriteMov(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            Expression dst;
            if (instr.op2 is RegisterOperand && instr.DataWidth.Size == 1)
            {
                dst = RewriteDst(instr.op2, src, s => m.Cast(PrimitiveType.Int16, s));
            }
            else
            {
                dst = RewriteDst(instr.op2, src, s => s);
            }
            SetFlags(dst, FlagM.ZF | FlagM.NF, FlagM.VF, 0);
        }

        private void RewriteNeg(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op1, src, m.Neg);
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF, 0, 0);
        }

        private void RewriteRol(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op1, src, (a, b) =>
                host.PseudoProcedure(PseudoProcedure.Rol, instr.DataWidth, a, b));
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteShift(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var immSrc = src as Constant;
            Func<Expression, Expression, Expression> fn = null;
            if (immSrc != null)
            {
                int sh = immSrc.ToInt32();
                if (sh < 0)
                {
                    fn = m.Sar;
                    src = Constant.Int16((short)-sh);
                }
                else
                {
                    fn = m.Shl;
                    src = Constant.Int16((short)sh);
                }
            }
            else
            {
                fn = (a, b) => 
                    host.PseudoProcedure("__shift", instr.DataWidth, a, b);
            }
            var dst = RewriteDst(instr.op2, src, fn);
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteSub(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op2, src, m.ISub);
            SetFlags(dst, FlagM.NF | FlagM.ZF | FlagM.VF | FlagM.CF, 0, 0);
        }

        private void RewriteSxt(Pdp11Instruction instr)
        {
            var n  = frame.EnsureFlagGroup(this.arch.GetFlagGroup((uint)FlagM.NF));

            var src = m.ISub(Constant.Int16(0), n);
            var dst = RewriteDst(instr.op1, src, s => s);
            SetFlags(dst, FlagM.ZF, 0, 0);
        }

        private void RewriteTst(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var tmp = frame.CreateTemporary(src.DataType);
            m.Assign(tmp, src);
            m.Assign(tmp, m.And(tmp, tmp));
            SetFlags(tmp, FlagM.NF | FlagM.ZF, FlagM.VF | FlagM.CF, 0);
        }

        private void RewriteXor(Pdp11Instruction instr)
        {
            var src = RewriteSrc(instr.op1);
            var dst = RewriteDst(instr.op2, src, m.Xor);
            SetFlags(dst, FlagM.ZF | FlagM.NF, FlagM.CF | FlagM.VF, 0);
        }
    }
}
