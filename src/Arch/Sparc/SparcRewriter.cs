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

using Reko.Arch.Sparc;
using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Lib;
using Reko.Core.Machine;
using Reko.Core.Operators;
using Reko.Core.Rtl;
using Reko.Core.Types;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.Arch.Sparc
{
    public partial class SparcRewriter : IEnumerable<RtlInstructionCluster>
    {
        private SparcArchitecture arch;
        private IStorageBinder frame;
        private IRewriterHost host;
        private LookaheadEnumerator<SparcInstruction> dasm;
        private SparcInstruction instrCur;
        private RtlEmitter m;
        private List<RtlInstruction> rtlInstructions;
        private RtlClass rtlc;

        public SparcRewriter(SparcArchitecture arch, EndianImageReader rdr, SparcProcessorState state, IStorageBinder frame, IRewriterHost host)
        {
            this.arch = arch;
            this.frame = frame;
            this.host = host;
            this.dasm = new LookaheadEnumerator<SparcInstruction>(CreateDisassemblyStream(rdr));
        }

        public SparcRewriter(SparcArchitecture arch, IEnumerator<SparcInstruction> instrs, SparcProcessorState state, IStorageBinder frame, IRewriterHost host)
        {
            this.arch = arch;
            this.frame = frame;
            this.host = host;
            this.dasm = new LookaheadEnumerator<SparcInstruction>(instrs);
        }

        private IEnumerable<SparcInstruction> CreateDisassemblyStream(EndianImageReader rdr)
        {
            return new SparcDisassembler(arch, rdr);
        }

        public IEnumerator<RtlInstructionCluster> GetEnumerator()
        {
            while (dasm.MoveNext())
            {
                instrCur = dasm.Current;
                var addr = instrCur.Address;
                rtlInstructions = new List<RtlInstruction>();
                rtlc = RtlClass.Linear;
                m = new RtlEmitter(rtlInstructions);
                switch (instrCur.Opcode)
                {
                default: 
                    throw new AddressCorrelatedException(
                        instrCur.Address,
                        "Rewriting SPARC opcode '{0}' is not supported yet.",
                        instrCur.Opcode);
                case Opcode.illegal:
                    rtlc = RtlClass.Invalid;
                    m.Invalid();
                    break;
                case Opcode.add: RewriteAlu(m.IAdd, false); break;
                case Opcode.addcc: RewriteAluCc(m.IAdd, false); break;
                case Opcode.addx: RewriteAddxSubx(m.IAdd, false); break;
                case Opcode.addxcc: RewriteAddxSubx(m.IAdd, true); break;
                case Opcode.and: RewriteAlu(m.And, false); break;
                case Opcode.andcc: RewriteAluCc(m.And, false); break;
                case Opcode.andn: RewriteAlu(m.And, true); break;
                case Opcode.ba: RewriteBranch(Constant.True()); break;
                case Opcode.bn: RewriteBranch(Constant.False()); break;
                case Opcode.bne: RewriteBranch(m.Test(ConditionCode.NE, Grf(FlagM.ZF))); break;
                case Opcode.be: RewriteBranch(m.Test(ConditionCode.EQ, Grf(FlagM.ZF))); break;
                case Opcode.bg: RewriteBranch(m.Test(ConditionCode.GT, Grf(FlagM.ZF | FlagM.NF | FlagM.VF))); break;
                case Opcode.bge: RewriteBranch(m.Test(ConditionCode.GE, Grf(FlagM.NF | FlagM.VF))); break;
                case Opcode.bgu: RewriteBranch(m.Test(ConditionCode.UGE, Grf(FlagM.CF | FlagM.ZF))); break;
                case Opcode.bl: RewriteBranch(m.Test(ConditionCode.LT, Grf(FlagM.ZF | FlagM.NF | FlagM.VF))); break;
                case Opcode.ble: RewriteBranch(m.Test(ConditionCode.LE, Grf(FlagM.ZF | FlagM.NF | FlagM.VF))); break;
                case Opcode.bleu: RewriteBranch(m.Test(ConditionCode.ULE, Grf(FlagM.CF | FlagM.ZF))); break;
                case Opcode.bcc: RewriteBranch(m.Test(ConditionCode.UGE, Grf(FlagM.CF))); break;
                case Opcode.bcs: RewriteBranch(m.Test(ConditionCode.ULT, Grf(FlagM.CF))); break;
                case Opcode.bneg: RewriteBranch(m.Test(ConditionCode.LT, Grf(FlagM.NF))); break;
                case Opcode.bpos: RewriteBranch(m.Test(ConditionCode.GE, Grf(FlagM.NF))); break;
                //                    Z
                //case Opcode.bgu  not (C or Z)
                //case Opcode.bleu (C or Z)
                //case Opcode.bcc  not C
                //case Opcode.bcs   C
                //case Opcode.bpos not N
                //case Opcode.bneg N
                //case Opcode.bvc  not V
                //case Opcode.bvs  V

                case Opcode.call: RewriteCall(); break;
                case Opcode.fabss: RewriteFabss(); break;
                case Opcode.fadds: RewriteFadds(); break;
                case Opcode.fbne: RewriteBranch(m.Test(ConditionCode.NE, Grf(FlagM.LF | FlagM.GF))); break;
                case Opcode.fba: RewriteBranch(Constant.True()); break;
                case Opcode.fbn: RewriteBranch(Constant.False()); break;

                case Opcode.fbu   : RewriteBranch(m.Test(ConditionCode.NE, Grf(FlagM.UF))); break;
                case Opcode.fbg   : RewriteBranch(m.Test(ConditionCode.GT, Grf(FlagM.GF))); break;
                case Opcode.fbug  : RewriteBranch(m.Test(ConditionCode.GT, Grf(FlagM.GF | FlagM.UF))); break;
                //case Opcode.fbug  : on Unordered or Greater G or U
                //case Opcode.fbl   : on Less L
                case Opcode.fbul: RewriteBranch(m.Test(ConditionCode.LT, Grf(FlagM.GF|FlagM.UF))); break;
                //case Opcode.fbul  : on Unordered or Less L or U
                //case Opcode.fblg  : on Less or Greater L or G
                //case Opcode.fbne  : on Not Equal L or G or U
                //case Opcode.fbe   : on Equal E
                //case Opcode.fbue  : on Unordered or Equal E or U
                //case Opcode.fbge  : on Greater or Equal E or G
                //case Opcode.fbuge : on Unordered or Greater or Equal E or G or U
                case Opcode.fbuge: RewriteBranch(m.Test(ConditionCode.GE, Grf(FlagM.EF | FlagM.GF | FlagM.UF))); break;

                //case Opcode.fble  : on Less or Equal E or L
                //case Opcode.fbule : on Unordered or Less or Equal E or L or U
                case Opcode.fbule: RewriteBranch(m.Test(ConditionCode.LE, Grf(FlagM.EF | FlagM.LF | FlagM.UF))); break;
                //                case Opcode.FBO   : on Ordered E or L or G


                case Opcode.fcmpes: RewriteFcmpes(); break;
                case Opcode.fdivs: RewriteFdivs(); break;
                case Opcode.fitod: RewriteFitod(); break;
                case Opcode.fitoq: RewriteFitoq(); break;
                case Opcode.fitos: RewriteFitos(); break;
                case Opcode.fmovs: RewriteFmovs(); break;
                case Opcode.fmuls: RewriteFmuls(); break;
                case Opcode.fnegs: RewriteFmovs(); break;
                case Opcode.fsubs: RewriteFsubs(); break;
                case Opcode.jmpl: RewriteJmpl(); break;
                case Opcode.ld: RewriteLoad(PrimitiveType.Word32); break;
                case Opcode.lddf: RewriteDLoad(PrimitiveType.Real64); break;
                case Opcode.ldf: RewriteLoad(PrimitiveType.Real32); break;
                case Opcode.ldd: RewriteLoad(PrimitiveType.Word64); break;
                case Opcode.ldsb: RewriteLoad(PrimitiveType.SByte); break;
                case Opcode.ldsh: RewriteLoad(PrimitiveType.Int16); break;
                case Opcode.ldub: RewriteLoad(PrimitiveType.Byte); break;
                case Opcode.lduh: RewriteLoad(PrimitiveType.Word16); break;
                case Opcode.mulscc: RewriteMulscc(); break;
                case Opcode.or: RewriteAlu(m.Or, false); break;
                case Opcode.orcc: RewriteAluCc(m.Or, false); break;
                case Opcode.restore: RewriteRestore(); break;
                case Opcode.rett: RewriteRett(); break;
                case Opcode.save: RewriteSave(); break;
                case Opcode.sethi: RewriteSethi(); break;
                case Opcode.sdiv: RewriteAlu(m.SDiv, false); break;
                case Opcode.sdivcc: RewriteAlu(m.SDiv, false); break;
                case Opcode.sll: RewriteAlu(m.Shl, false); break;
                case Opcode.smul: RewriteAlu(m.SMul, false); break;
                case Opcode.smulcc: RewriteAluCc(m.SMul, false); break;
                case Opcode.sra: RewriteAlu(m.Sar, false); break;
                case Opcode.srl: RewriteAlu(m.Shr, false); break;
                case Opcode.st: RewriteStore(PrimitiveType.Word32); break;
                case Opcode.stb: RewriteStore(PrimitiveType.Byte); break;
                case Opcode.std: RewriteStore(PrimitiveType.Word64); break;
                case Opcode.stf: RewriteStore(PrimitiveType.Real32); break;
                case Opcode.sth: RewriteStore(PrimitiveType.Word16); break;
                case Opcode.sub: RewriteAlu(m.ISub, false); break;
                case Opcode.subcc: RewriteAluCc(m.ISub, false); break;
                case Opcode.subx: RewriteAddxSubx(m.ISub, false); break;
                case Opcode.subxcc: RewriteAddxSubx(m.ISub, true); break;
                case Opcode.ta: RewriteTrap(Constant.True()); break;
                case Opcode.tn: RewriteTrap(Constant.False()); break;
                case Opcode.tne: RewriteTrap(m.Test(ConditionCode.NE, Grf(FlagM.ZF))); break;
                case Opcode.te: RewriteTrap(m.Test(ConditionCode.EQ, Grf(FlagM.ZF))); break;

                case Opcode.udiv: RewriteAlu(m.UDiv, false); break;
                case Opcode.udivcc: RewriteAluCc(m.UDiv, false); break;
                case Opcode.umul: RewriteAlu(m.UMul, false); break;
                case Opcode.umulcc: RewriteAluCc(m.UMul, false); break;
                case Opcode.unimp: m.Invalid(); break;
                case Opcode.xor: RewriteAlu(m.Xor, false); break;
                }
                yield return new RtlInstructionCluster(addr, 4, rtlInstructions.ToArray())
                {
                    Class = rtlc
                };
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void EmitCc(Expression dst)
        {
            m.Assign(
                frame.EnsureFlagGroup(
                    Registers.psr,
                    0xF, "NZVC",
                    PrimitiveType.Byte),
                m.Cond(dst));
        }

        private Expression RewriteOp(MachineOperand op)
        {
            return RewriteOp(op, false);
        }

        private Expression RewriteOp(MachineOperand op, bool g0_becomes_null)
        {
            var r = op as RegisterOperand;
            if (r != null)
            {
                if (r.Register == Registers.g0)
                {
                    if (g0_becomes_null)
                        return null;
                    else 
                        return Constant.Zero(PrimitiveType.Word32);
                }
                else
                    return frame.EnsureRegister(r.Register);
            }
            var imm = op as ImmediateOperand;
            if (imm != null)
                return imm.Value;
            throw new NotImplementedException(string.Format("Unsupported operand {0} ({1})", op, op.GetType().Name));
        }

        private Expression RewriteRegister(MachineOperand op)
        {
            return frame.EnsureRegister(((RegisterOperand)op).Register);
        }
        
        private Expression RewriteMemOp(MachineOperand op, PrimitiveType size)
        {
            var m = op as MemoryOperand;
            Expression baseReg;
            Expression offset;
            if (m != null)
            {
                baseReg = m.Base == Registers.g0 ? null : frame.EnsureRegister(m.Base);
                offset = m.Offset.IsIntegerZero ? null : m.Offset;
            }
            else
            {
                var i = op as IndexedMemoryOperand;
                if (i != null)
                {
                    baseReg = i.Base == Registers.g0 ? null : frame.EnsureRegister(i.Base);
                    offset = i.Index == Registers.g0 ? null : frame.EnsureRegister(i.Index);
                }
                else
                    throw new NotImplementedException(string.Format("Unknown memory operand {0} ({1})", op, op.GetType().Name));
            }
            return new MemoryAccess(SimplifySum(baseReg, offset), size);
        }

        private Expression SimplifySum(Expression srcLeft, Expression srcRight)
        {
            if (srcLeft == null && srcRight == null)
                return Constant.Zero(PrimitiveType.Pointer32);
            else if (srcLeft == null)
                return srcRight;
            else if (srcRight == null)
                return srcLeft;
            else
                return m.IAdd(srcLeft, srcRight);
        }
    }
}
