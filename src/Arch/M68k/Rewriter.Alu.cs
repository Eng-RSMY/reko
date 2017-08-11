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
using Reko.Core.Operators;
using Reko.Core.Machine;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Reko.Core;
using Reko.Core.Rtl;

namespace Reko.Arch.M68k
{
    /// <summary>
    /// Rewrites ALU instructions.
    /// </summary>
    public partial class Rewriter
    {
        public void RewriteArithmetic(Func<Expression, Expression, Expression> binOpGen)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address,opSrc, binOpGen);
            AllConditions(opDst);
        }

        public void RewriteRotation(string procName)
        {
            Expression opDst;
            if (di.op2 != null)
            {
                var opSrc = orw.RewriteSrc(di.op1, di.Address);
                opDst = orw.RewriteDst(di.op2, di.Address, opSrc, (s, d) =>
                    host.PseudoProcedure(procName, di.dataWidth, d, s));
            }
            else
            {
                opDst = orw.RewriteDst(di.op1, di.Address,
                    Constant.Byte(1), (s, d) =>
                        host.PseudoProcedure(procName, PrimitiveType.Word32, d, s));
            }
            if (opDst == null)
            {
                EmitInvalid();
                return;
            }
            m.Assign(
                orw.FlagGroup(FlagM.CF | FlagM.NF | FlagM.ZF),
                m.Cond(opDst));
            m.Assign(orw.FlagGroup(FlagM.VF), Constant.False());
        }

        public void RewriteRotationX(string procName)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, opSrc, (s, d) =>
                host.PseudoProcedure(procName, di.dataWidth, d, s, orw.FlagGroup(FlagM.XF)));
            if (opDst == null)
            {
                EmitInvalid();
                return;
            }
            m.Assign(
                orw.FlagGroup(FlagM.CF | FlagM.NF | FlagM.XF | FlagM.ZF),
                m.Cond(opDst));
            m.Assign(orw.FlagGroup(FlagM.VF), Constant.False());
        }

        public void RewriteTst()
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.FlagGroup(FlagM.NF | FlagM.ZF);
            m.Assign(opDst, m.Cond(m.ISub(opSrc, 0)));
            m.Assign(orw.FlagGroup(FlagM.CF), Constant.False());
            m.Assign(orw.FlagGroup(FlagM.VF), Constant.False());
        }

        public void RewriteShift(Func<Expression, Expression, Expression> binOpGen)
        {
            Expression opDst;
            if (di.op2 != null)
            {
                var opSrc = orw.RewriteSrc(di.op1, di.Address);
                opDst = orw.RewriteDst(di.op2, di.Address, opSrc, binOpGen);
            }
            else
            {
                var opSrc = Constant.Int32(1);
                opDst = orw.RewriteDst(di.op1, di.Address, PrimitiveType.Word16, opSrc, binOpGen);
            }
            AllConditions(opDst);
        }

        public void RewriteBchg()
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var tmpMask = binder.CreateTemporary(PrimitiveType.UInt32);
            m.Assign(tmpMask, m.Shl(1, opSrc));
            var opDst = orw.RewriteDst(di.op2, di.Address, tmpMask, (s, d) => m.Xor(d, s));
            if (opDst == null)
            {
                EmitInvalid();
                return;
            }
            m.Assign(
                orw.FlagGroup(FlagM.ZF),
                m.Cond(m.And(opDst, tmpMask)));
        }

        public void RewriteBclrBset(string name)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            PrimitiveType w = (di.op2 is RegisterOperand)
                 ? PrimitiveType.Word32 
                 : PrimitiveType.Byte;
            di.op2.Width = w;
            orw.DataWidth = w;
            var opDst = orw.RewriteSrc(di.op2, di.Address);
            m.Assign(
                orw.FlagGroup(FlagM.ZF),
                host.PseudoProcedure(name, PrimitiveType.Bool, opDst, opSrc, m.Out(PrimitiveType.Pointer32, opDst)));
        }

        public void RewriteExg()
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteSrc(di.op2, di.Address);
            var tmp = binder.CreateTemporary(PrimitiveType.Word32);
            m.Assign(tmp, opSrc);
            m.Assign(opSrc, opDst);
            m.Assign(opDst, tmp);
            LogicalConditions(opDst);
        }

        public void RewriteExt()
        {
            PrimitiveType dtSrc;
            PrimitiveType dtDst;
            if (di.dataWidth.Size == 4)
            {
                dtSrc = PrimitiveType.Int16;
                dtDst = PrimitiveType.Int32;
            }
            else
            {
                dtSrc = PrimitiveType.SByte;
                dtDst = PrimitiveType.Int16;
            }
            var dReg = binder.EnsureRegister(((RegisterOperand) di.op1).Register);
            m.Assign(
                dReg,
                m.Cast(dtDst, m.Cast(dtSrc, dReg)));
            m.Assign(
                orw.FlagGroup(FlagM.NF | FlagM.ZF),
                m.Cond(dReg));
        }

        public void RewriteExtb()
        {
            var dReg = orw.RewriteSrc(di.op1, di.Address);
            m.Assign(
                dReg, 
                m.Cast(PrimitiveType.Int32,
                    m.Cast(PrimitiveType.SByte, dReg)));
            m.Assign(
                orw.FlagGroup(FlagM.NF | FlagM.ZF),
                m.Cond(dReg));
        }

        public void RewriteLogical(Func<Expression, Expression, Expression> binOpGen)
        {
            var width = di.dataWidth;
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, opSrc, binOpGen);
            LogicalConditions(opDst);
        }

        public void RewriteMul(Func<Expression, Expression, Expression> binOpGen)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, PrimitiveType.Int32, opSrc, binOpGen);
            if (opDst == null)
            {
                EmitInvalid();
                return;
            }
            m.Assign(orw.FlagGroup(FlagM.NF | FlagM.ZF | FlagM.VF), m.Cond(opDst));
            m.Assign(orw.FlagGroup(FlagM.CF), Constant.False());
        }

        public void RewriteUnary(Func<Expression, Expression> unaryOpGen, Action<Expression> generateFlags)
        {
            var op = orw.RewriteUnary(di.op1, di.Address, di.dataWidth, unaryOpGen);
            generateFlags(op);
        }

        private Expression MaybeCast(PrimitiveType width, Expression expr)
        {
            if (expr.DataType.Size == width.Size)
                return expr;
            else
                return m.Cast(width, expr);
        }

        private void RewriteAddSubq(Func<Expression,Expression,Expression> opGen)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var regDst = di.op2 as RegisterOperand;
            if (regDst != null && regDst.Register is AddressRegister)
            {
                var opDst = binder.EnsureRegister(regDst.Register);
                m.Assign(opDst, opGen(opSrc, opDst));
            }
            else
            {
                var opDst = orw.RewriteDst(di.op2, di.Address, opSrc, opGen);
                if (opDst == null)
                {
                    EmitInvalid();
                    return;
                }
                m.Assign(orw.FlagGroup(FlagM.CVZNX), m.Cond(opDst));
            }
        }

        public void RewriteAddSubx(Func<Expression,Expression,Expression> opr)
        {
            // We do not take the trouble of widening the CF to the word size
            // to simplify code analysis in later stages. 
            var x = orw.FlagGroup(FlagM.XF);
            var src = orw.RewriteSrc(di.op1, di.Address);
            var dst = orw.RewriteDst(di.op2, di.Address, src, (d, s) => 
                    opr(opr(d, s), x));
            if (dst == null)
            {
                EmitInvalid();
                return;
            }
            m.Assign(orw.FlagGroup(FlagM.CVZNX), m.Cond(dst));
        }

        private void RewriteScc(ConditionCode cc, FlagM flagsUsed)
        {
            orw.RewriteMoveDst(di.op1, di.Address, PrimitiveType.Byte, orw.FlagGroup(flagsUsed));
        }
        
        private void RewriteSwap()
        {
            var r = (RegisterOperand) di.op1;
            var reg = binder.EnsureRegister(r.Register);
            m.Assign(reg, host.PseudoProcedure("__swap", PrimitiveType.Word32, reg));
            m.Assign(orw.FlagGroup(FlagM.NF | FlagM.ZF), m.Cond(reg));
            m.Assign(orw.FlagGroup(FlagM.CF), Constant.False());
            m.Assign(orw.FlagGroup(FlagM.VF), Constant.False());
        }

        private void RewriteBinOp(Func<Expression,Expression,Expression> opGen)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, opSrc, opGen);
            if (opDst == null)
            {
                EmitInvalid();
                return;
            }
        }

        private void RewriteBinOp(Func<Expression, Expression, Expression> opGen, FlagM flags)
        {
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, opSrc, opGen);
            if (opDst == null)
            {
                EmitInvalid();
                return;
            }
            m.Assign(orw.FlagGroup(flags), m.Cond(opDst));
        }

        private void RewriteBtst()
        {
            orw.DataWidth = di.op1 is RegisterOperand ? PrimitiveType.Word32 : PrimitiveType.Byte;
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteSrc(di.op2, di.Address);
            m.Assign(
                orw.FlagGroup(FlagM.ZF),
                host.PseudoProcedure("__btst", PrimitiveType.Bool,
                    opDst, opSrc));
        }

        private void RewriteCas()
        {
            m.Assign(
                orw.FlagGroup(FlagM.CVZN),
                host.PseudoProcedure("atomic_compare_exchange_weak", PrimitiveType.Bool,
                    m.AddrOf(orw.RewriteSrc(di.op3, di.Address)),
                    orw.RewriteSrc(di.op2, di.Address),
                    orw.RewriteSrc(di.op1, di.Address)));
        }

        private void RewriteClr()
        {
            var src = Constant.Create(di.dataWidth, 0);
            var opDst = orw.RewriteMoveDst(di.op1, di.Address, di.dataWidth, src);
            m.Assign(orw.FlagGroup(FlagM.ZF), Constant.True());
            m.Assign(orw.FlagGroup(FlagM.CF), Constant.False());
            m.Assign(orw.FlagGroup(FlagM.NF), Constant.False());
            m.Assign(orw.FlagGroup(FlagM.VF), Constant.False());
        }

        private void RewriteCmp()
        {
            var src = orw.RewriteSrc(di.op1, di.Address);
            var dst = orw.RewriteSrc(di.op2, di.Address);
            var tmp = binder.CreateTemporary(dst.DataType);
            m.Assign(tmp, m.ISub(dst, src));
            m.Assign(
                orw.FlagGroup(FlagM.CF | FlagM.NF | FlagM.VF | FlagM.ZF),
                m.Cond(tmp));
        }

        private void RewriteCmp2()
        {
            var reg = orw.RewriteSrc(di.op2, di.Address);
            var lowBound = orw.RewriteSrc(di.op1, di.Address);
            var ea = ((MemoryAccess)lowBound).EffectiveAddress;
            var hiBound = m.Load(lowBound.DataType, m.IAdd(ea, lowBound.DataType.Size));
            var C = orw.FlagGroup(FlagM.CF);
            var Z = orw.FlagGroup(FlagM.ZF);
            m.Assign(C, m.Cor(m.Lt(reg, lowBound), m.Gt(reg, hiBound)));
            m.Assign(Z, m.Cor(m.Eq(reg, lowBound), m.Eq(reg, hiBound)));
        }

        private void RewriteDiv(Func<Expression,Expression,Expression> op, DataType dt)
        {
            Debug.Print(di.dataWidth.ToString());
            if (di.dataWidth.BitSize == 16)
            {
                di.dataWidth = PrimitiveType.UInt32;
                var src = orw.RewriteSrc(di.op1, di.Address);
                var rem = binder.CreateTemporary(dt);
                var quot = binder.CreateTemporary(dt);
                var regDst = binder.EnsureRegister(((RegisterOperand) di.op2).Register);
                m.Assign(rem, m.Cast(rem.DataType, m.Remainder(regDst, src)));
                m.Assign(quot, m.Cast(quot.DataType, op(regDst, src)));
                m.Assign(regDst, m.Dpb(regDst, rem, 16));
                m.Assign(regDst, m.Dpb(regDst, quot, 0));
                m.Assign(
                    orw.FlagGroup(FlagM.NF | FlagM.VF | FlagM.ZF),
                    m.Cond(quot));
                m.Assign(
                    orw.FlagGroup(FlagM.CF), Constant.False());
                return;
            }
            throw new NotImplementedException(di.ToString());
        }

        private Expression RewriteSrcOperand(MachineOperand mop)
        {
            return RewriteDstOperand(mop, null, (s, d) => { });
        }

        private bool NeedsSpilling(Expression op)
        {
            //$REVIEW: May not need to spill here if opSrc is immediate / register other than reg
            if (op == null)
                return false;
            if (op is Constant)
                return false;
            return true;
        }

        private Expression RewriteDstOperand(MachineOperand mop, Expression opSrc, Action<Expression, Expression> m)
        {
            var preDec = mop as PredecrementMemoryOperand;
            if (preDec != null)
            {
                var reg = binder.EnsureRegister(preDec.Register);
                var t = binder.CreateTemporary(opSrc.DataType);
                if (NeedsSpilling(opSrc))
                {
                    this.m.Assign(t, opSrc);
                    opSrc = t;
                }
                this.m.Assign(reg, this.m.ISub(reg, this.di.dataWidth.Size));
                var op = this.m.Load(this.di.dataWidth, reg);
                m(opSrc, op);
                return op;
            }
            var postInc = mop as PostIncrementMemoryOperand;
            if (postInc != null)
            {
                var reg = binder.EnsureRegister(postInc.Register);
                var t = binder.CreateTemporary(di.dataWidth);
                if (NeedsSpilling(opSrc))
                {
                    this.m.Assign(t, opSrc);
                    opSrc = t;
                }
                m(opSrc, this.m.Load(this.di.dataWidth, reg));
                this.m.Assign(reg, this.m.IAdd(reg, this.di.dataWidth.Size));
                return t;
            }
            return orw.RewriteSrc(mop, di.Address);
        }

        public Expression GetEffectiveAddress(MachineOperand op)
        {
            var mem = op as MemoryOperand;
            if (mem != null)
            {
                if (mem.Base == null)
                {
                    return mem.Offset;
                }
                else if (mem.Base == Registers.pc)
                {
                    return di.Address + mem.Offset.ToInt32();
                }
                else if (mem.Offset == null)
                {
                    return binder.EnsureRegister(mem.Base);
                }
                else {
                    return m.IAdd(
                        binder.EnsureRegister(mem.Base),
                        Constant.Int32(mem.Offset.ToInt32()));
                }
            }
            var addrOp = di.op1 as AddressOperand;
            if (addrOp != null)
            {
                return addrOp.Address;
            }
            var indIdx = di.op1 as IndirectIndexedOperand;
            if (indIdx != null)
            {
                var a = binder.EnsureRegister(indIdx.ARegister);
                var x = binder.EnsureRegister(indIdx.XRegister);
                return m.IAdd(a, x);        //$REVIEW: woefully incomplete...
            }
            throw new NotImplementedException(string.Format("{0} ({1})", op, op.GetType().Name));
        }

        public void RewriteLea()
        {
            var dst = orw.RewriteSrc(di.op2, di.Address);
            var src = GetEffectiveAddress(di.op1);
            m.Assign(dst, src);
        }

        public void RewritePea()
        {
            var sp = binder.EnsureRegister(arch.StackRegister);
            m.Assign(sp, m.ISub(sp, 4));
            var ea = GetEffectiveAddress(di.op1);
            m.Assign(m.LoadDw(sp), ea);
        }

        public void RewriteMove(bool setFlag)
        {
            if (GetRegister(di.op1) == Registers.ccr)
            {
                // move from ccr.
                var src = m.Cast(PrimitiveType.UInt16, binder.EnsureRegister(Registers.ccr));
                var dst = orw.RewriteDst(di.op2, di.Address, PrimitiveType.UInt16, src, (s, d) => s);
                return;
            }
            else if (GetRegister(di.op2) == Registers.ccr)
            {
                var src = orw.RewriteSrc(di.op1, di.Address);
                var dst = orw.RewriteDst(di.op2, di.Address, di.dataWidth ?? (PrimitiveType)src.DataType, src, (s, d) => s);
                return;
            }
            var opSrc = orw.RewriteSrc(di.op1, di.Address);
            var opDst = orw.RewriteDst(di.op2, di.Address, di.dataWidth ?? (PrimitiveType)opSrc.DataType, opSrc, (s, d) => s);
            if (opDst == null)
            {
                EmitInvalid();
                return;
            }
            var isSr = GetRegister(di.op1) == Registers.sr || GetRegister(di.op2) == Registers.sr;
            if (setFlag && !isSr)
            {
                m.Assign(
                    orw.FlagGroup(FlagM.CVZN),
                    m.Cond(opDst));
            }
        }

        public void RewriteMoveq()
        {
            var opSrc = (sbyte) ((M68kImmediateOperand) di.op1).Constant.ToInt32();
            var opDst = binder.EnsureRegister(((RegisterOperand)di.op2).Register);
            m.Assign(opDst, Constant.Int32(opSrc));
            m.Assign(
                orw.FlagGroup(FlagM.CVZN),
                m.Cond(opDst));
        }

        public IEnumerable<Identifier> RegisterMaskIncreasing(Domain dom, uint bitSet, Func<int,RegisterStorage> regGenerator)
        {
            int maxReg = dom == Domain.Real ? 8 : 16;
            int mask = dom == Domain.Real ? 0x80 : 0x8000;

            for (int i = 0; i < maxReg; ++i, mask >>= 1)
            {
                if ((bitSet & mask) != 0)
                {
                    yield return binder.EnsureRegister(regGenerator(i));
                }
            }
        }

        public IEnumerable<Identifier> RegisterMaskDecreasing(Domain dom, uint bitSet, Func<int, RegisterStorage> regGenerator)
        {
            int maxReg = dom == Domain.Real ? 8 : 16;
            for (int i = maxReg-1, mask = 1; i >= 0; --i, mask <<= 1)
            {
                if ((bitSet & mask) != 0)
                    yield return binder.EnsureRegister(regGenerator(i));
            }
        }

        public void RewriteMovem(Func<int, RegisterStorage> regGenerator)
        {
            var dstRegs = di.op2 as RegisterSetOperand;
            if (dstRegs != null)
            {
                var postInc = di.op1 as PostIncrementMemoryOperand;
                Identifier srcReg = null;
                if (postInc != null)
                {
                    srcReg = binder.EnsureRegister(postInc.Register);
                }
                else
                {
                    var src = orw.RewriteSrc(di.op1, di.Address) as MemoryAccess;
                    if (src == null)
                        throw new AddressCorrelatedException(di.Address, "Unsupported addressing mode for {0}.", di);
                    srcReg = binder.CreateTemporary(src.EffectiveAddress.DataType);
                    m.Assign(srcReg, src.EffectiveAddress);
                }
                foreach (var reg in RegisterMaskIncreasing(dstRegs.Width.Domain, dstRegs.BitSet, regGenerator))
                {
                    m.Assign(reg, m.Load(di.dataWidth, srcReg));
                    m.Assign(srcReg, m.IAdd(srcReg, di.dataWidth.Size));
                }
                return;
            }
            dstRegs = di.op1 as RegisterSetOperand;
            if (dstRegs != null)
            {
                var preDec = di.op2 as PredecrementMemoryOperand;
                if (preDec != null)
                {
                    var dstReg = binder.EnsureRegister(preDec.Register);
                    foreach (var reg in RegisterMaskDecreasing(dstRegs.Width.Domain, dstRegs.BitSet, regGenerator))
                    {
                        m.Assign(dstReg, m.ISub(dstReg, di.dataWidth.Size));
                        m.Assign(m.Load(di.dataWidth, dstReg), reg);
                    }
                }
                else
                {
                    var src = orw.RewriteSrc(di.op2, di.Address) as MemoryAccess;
                    if (src == null)
                        throw new AddressCorrelatedException(di.Address, "Unsupported addressing mode for {0}.", di);
                    var srcReg = binder.CreateTemporary(di.dataWidth);
                    m.Assign(srcReg, src.EffectiveAddress);
                    foreach (var reg in RegisterMaskIncreasing(dstRegs.Width.Domain, dstRegs.BitSet, regGenerator))
                    {
                        m.Assign(reg, m.Load(di.dataWidth, srcReg));
                        m.Assign(srcReg, m.IAdd(srcReg, di.dataWidth.Size));
                    }
                }
                return;
            }
            throw new AddressCorrelatedException(di.Address, "Unsupported addressing mode for {0}.", di);
        }

        public void RewriteMovep()
        {
            var pname = (di.dataWidth == PrimitiveType.Word32)
                ? "__movep_l"
                : "__movep_w";
            var op1 = RewriteSrcOperand(di.op1);
            var op2 = RewriteSrcOperand(di.op2);
            m.SideEffect(host.PseudoProcedure(pname, VoidType.Instance, op1, op2));
        }

        private Expression RewriteNegx(Expression expr)
        {
            expr = m.Neg(expr);
            return m.ISub(expr, orw.FlagGroup(FlagM.XF));
        }

        private void RewriteLink()
        {
            var aReg = orw.RewriteSrc(di.op1, di.Address);
            var aSp = binder.EnsureRegister(arch.StackRegister);
            var imm = ((M68kImmediateOperand) di.op2).Constant.ToInt32();
            m.Assign(aSp, m.ISub(aSp, 4));
            m.Assign(m.LoadDw(aSp), aReg);
            m.Assign(aReg, aSp);
            if (imm < 0)
            {
                m.Assign(aSp, m.ISub(aSp, -imm));
            }
            else
            {
                m.Assign(aSp, m.IAdd(aSp, -imm));
            }
        }

        private void RewriteUnlk()
        {
            var aReg = orw.RewriteSrc(di.op1, di.Address);
            var aSp = binder.EnsureRegister(arch.StackRegister);
            m.Assign(aSp, aReg);
            m.Assign(aReg, m.LoadDw(aSp));
            m.Assign(aSp, m.IAdd(aSp, 4));
        }

        private void Copy(Expression dst, Expression src, int bitSize)
        {
            if (dst is Identifier && dst.DataType.BitSize > bitSize)
            {
                var dpb = m.Dpb(dst, src, 0);
                m.Assign(dst, dpb);
            }
            else
            {
                m.Assign(dst, src);
            }
        }

        private void AllConditions(Expression expr)
        {
            if (expr == null)
            {
                EmitInvalid();
                return;
            }
            var f = orw.FlagGroup(FlagM.CF | FlagM.NF | FlagM.VF | FlagM.XF | FlagM.ZF);
            m.Assign(f, m.Cond(expr));
        }

        private void LogicalConditions(Expression expr)
        {
            if (expr == null)
            {
                EmitInvalid();
                return;
            }
            m.Assign(orw.FlagGroup(FlagM.NF | FlagM.ZF), m.Cond(expr));
            m.Assign(orw.FlagGroup(FlagM.CF), Constant.False());
            m.Assign(orw.FlagGroup(FlagM.VF), Constant.False());
        }
    }
}
