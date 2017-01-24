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

using Reko.Core.Expressions;
using Reko.Core.Rtl;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.Arch.M68k
{
    public partial class Rewriter
    {
        private Expression IsNan(Expression arg)
        {
            return host.PseudoProcedure("__is_nan", PrimitiveType.Bool, arg);
        }

        private void RewriteFbcc(ConditionCode cc)
        {
            ric.Class = RtlClass.ConditionalTransfer;
            emitter.Branch(
                emitter.Test(cc, frame.EnsureIdentifier(Registers.fpsr)),
                ((M68kAddressOperand)di.op1).Address,
                RtlClass.ConditionalTransfer);
        }

        private void RewriteFbcc(Func<Expression, Expression> fnTest)
        {
            ric.Class = RtlClass.ConditionalTransfer;
            emitter.Branch(fnTest(
                frame.EnsureIdentifier(Registers.fpsr)),
                ((M68kAddressOperand)di.op1).Address,
                RtlClass.ConditionalTransfer);
        }


        private void RewriteFBinOp(Func<Expression, Expression, Expression> binOpGen)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, opSrc, binOpGen);
            emitter.Assign(frame.EnsureIdentifier(Registers.fpsr), emitter.Cond(opDst));
        }

        private void RewriteFUnaryOp(Func<Expression, Expression> unaryOpGen)
        {
            var op = orw.RewriteUnary(di.op1, di.Address, di.dataWidth, unaryOpGen);
            emitter.Assign(frame.EnsureIdentifier(Registers.fpsr), emitter.Cond(op));
        }

        private void RewriteFcmp()
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteSrc(di.op2, di.Address);
            var tmp = frame.CreateTemporary(opDst.DataType);
            emitter.Assign(
                frame.EnsureIdentifier(Registers.fpsr), 
                emitter.Cond(emitter.FSub(opDst, opSrc)));
        }

        private void RewriteFmove()
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, opSrc, MaybeCastFpuArgs);
            emitter.Assign(frame.EnsureIdentifier(Registers.fpsr), emitter.Cond(opDst));
        }

        private void RewriteFmovecr()
        {
            var opSrc = (M68kImmediateOperand)di.op1;
            int n = opSrc.Constant.ToInt32();
            Expression src;
            double d;
            if (fpuRomConstants.TryGetValue(n, out d))
            {
                src = Constant.Real64(d);
                src.DataType = PrimitiveType.Real80;
            }
            else
            {
                src = host.PseudoProcedure("__fmovecr", PrimitiveType.Real64, opSrc.Constant);
            }
            var dst = orw.RewriteSrc(di.op2, di.Address);
            emitter.Assign(dst, src);
            emitter.Assign(frame.EnsureIdentifier(Registers.fpsr), emitter.Cond(dst));
        }

        private Expression MaybeCastFpuArgs(Expression src, Expression dst)
        {
            if (src.DataType != dst.DataType)
                return emitter.Cast(dst.DataType, src);
            else
                return src;
        }
    }
}
