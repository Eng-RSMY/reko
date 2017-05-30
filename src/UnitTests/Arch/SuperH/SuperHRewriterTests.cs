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

using NUnit.Framework;
using Reko.Arch.SuperH;
using Reko.Core;
using Reko.Core.Configuration;
using Reko.Core.Rtl;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.UnitTests.Arch.Tlcs
{
    [TestFixture]
    public class SuperHRewriterTests : RewriterTestBase
    {
        private SuperHArchitecture arch = new SuperHArchitecture();
        private Address baseAddr = Address.Ptr32(0x00100000);
        private SuperHState state;
        private MemoryArea image;

        public override IProcessorArchitecture Architecture
        {
            get { return arch; }
        }

        protected override IEnumerable<RtlInstructionCluster> GetInstructionStream(Frame frame, IRewriterHost host)
        {
            return new SuperHRewriter(arch, new LeImageReader(image, 0), state, new Frame(arch.WordWidth), host);
        }

        public override Address LoadAddress
        {
            get { return baseAddr; }
        }

        protected override MemoryArea RewriteCode(string hexBytes)
        {
            var bytes = OperatingEnvironmentElement.LoadHexBytes(hexBytes)
                .ToArray();
            this.image = new MemoryArea(LoadAddress, bytes);
            return image;
        }

        [SetUp]
        public void Setup()
        {
            state = (SuperHState)arch.CreateProcessorState();
        }

        [Test]
        public void SHRw_add_imm_rn()
        {
            RewriteCode("FF73"); // add\t#FF,r3
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r3 = r3 + 0xFFFFFFFF");
        }

        [Test]
        public void SHRw_add_rm_rn()
        {
            RewriteCode("4C32"); // add\tr4,r2
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r2 = r2 + r4");
        }

        [Test]
        public void SHRw_addc_rm_rn()
        {
            RewriteCode("4E32"); // addc\tr4,r2
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r2 = r2 + r4 + T");
        }

        [Test]
        public void SHRw_addv_rm_rn()
        {
            RewriteCode("4F32"); // addv\tr4,r2
            AssertCode(
                "0|L--|00100000(2): 2 instructions",
                "1|L--|r2 = r2 + r4",
                "2|L--|T = Test(OV,r2)");
        }

        [Test]
        public void SHRw_and_rm_rn()
        {
            RewriteCode("4923"); // and\tr4,r3
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r3 = r3 & r4");

        }

        [Test]
        public void SHRw_and_imm_r0()
        {
            RewriteCode("F0C9"); // and\t#F0,r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = r0 & 0x000000F0");

        }

        [Test]
        public void SHRw_and_b_imm_r0()
        {
            RewriteCode("F0CD"); // and.b\t#F0,@(r0,gbr)
            AssertCode(
                "0|L--|00100000(2): 2 instructions",
                "1|L--|v2 = Mem0[r0 + gbr:byte]",
                "2|L--|Mem0[r0 + gbr:byte] = v2 & 0x000000F0");
        }

        [Test]
        public void SHRw_bf()
        {
            RewriteCode("F08B"); // bf\t000FFFE4
            AssertCode(
                "0|T--|00100000(2): 1 instructions",
                "1|T--|if (!T) branch 000FFFE4");
        }

        [Test]
        public void SHRw_bf_s()
        {
            RewriteCode("F08F"); // bf/s\t000FFFE4
            AssertCode(
               "0|TD-|00100000(2): 1 instructions",
               "1|TD-|if (!T) branch 000FFFE4");
        }

        [Test]
        public void SHRw_bra()
        {
            RewriteCode("F0AF"); // bra\t0000FFE4
            AssertCode(
                "0|TD-|00100000(2): 1 instructions",
                "1|TD-|goto 000FFFE4");
        }

        [Test]
        public void SHRw_braf_reg()
        {
            RewriteCode("2301"); // braf\tr1
            AssertCode(
                "0|TD-|00100000(2): 1 instructions",
                "1|TD-|goto 0x00100004 + r1");
        }

        [Test]
        public void SHRw_brk()
        {
            RewriteCode("3B00"); // brk
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|__brk()");
        }

        [Test]
        public void SHRw_bsr()
        {
            RewriteCode("F0BF"); // bsr\t0000FFE4
            AssertCode(
                "0|TD-|00100000(2): 1 instructions",
                "1|TD-|call 000FFFE4 (0)");
        }

        [Test]
        public void SHRw_bsrf()
        {
            RewriteCode("0301"); // bsrf\tr1
            AssertCode(
                "0|TD-|00100000(2): 1 instructions",
                "1|TD-|call 0x00100004 + r1 (0)");
        }

        [Test]
        public void SHRw_bt()
        {
            RewriteCode("F089"); // bt\t0000FFE4
            AssertCode(
                "0|T--|00100000(2): 1 instructions",
                "1|T--|if (T) branch 000FFFE4");
        }

        [Test]
        public void SHRw_bt_s()
        {
            RewriteCode("F08D"); // bt/s\t0000FFE4
            AssertCode(
                "0|TD-|00100000(2): 1 instructions",
                "1|TD-|if (T) branch 000FFFE4");
        }

        [Test]
        public void SHRw_clrmac()
        {
            RewriteCode("2800"); // clrmac
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|mac = 0x0000000000000000");
        }

        [Test]
        public void SHRw_cmpeq()
        {
            RewriteCode("4035"); // cmp/eq\tr4,r5
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = r5 == r4");
        }

        [Test]
        public void SHRw_cmpeq_imm()
        {
            RewriteCode("F088"); // cmp/eq\t#F0,r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = r0 == 0xFFFFFFF0");
        }

        [Test]
        [Ignore("Wait until encountering this in real code")]
        public void SHRw_div0s()
        {
            RewriteCode("4723"); // div0s\tr4,r3
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|@@@");
        }

        [Test]
        [Ignore("Wait until encountering this in real code")]
        public void SHRw_div0u()
        {
            RewriteCode("1900"); // div0u
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|@@@");
        }

        [Test]
        [Ignore("Wait until encountering this in real code")]
        public void SHRw_div1()
        {
            RewriteCode("4433"); // div1\tr4,r3
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|@@@");
        }

        [Test]
        public void SHRw_dmuls_l()
        {
            RewriteCode("4D33"); // dmuls.l\tr4,r3
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|mac = r3 *s r4");
        }

        [Test]
        public void SHRw_dt()
        {
            RewriteCode("104F"); // dt\tr15
            AssertCode(
                "0|L--|00100000(2): 2 instructions",
                "1|L--|r15 = r15 - 0x00000001",
                "2|L--|T = r15 == 0x00000000");
        }

        [Test]
        public void SHRw_exts_b()
        {
            RewriteCode("FE6E"); // exts.b\tr15,r14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r14 = (int8) r15");
        }

        [Test]
        public void SHRw_exts_w()
        {
            RewriteCode("FF6E"); // exts.w\tr15,r14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r14 = (int16) r15");
        }

        [Test]
        public void SHRw_extu_b()
        {
            RewriteCode("FC6E"); // extu.b\tr15,r14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r14 = (byte) r15");
        }

        [Test]
        public void SHRw_extu_w()
        {
            RewriteCode("FD6E"); // extu.w\tr15,r14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r14 = (uint16) r15");

        }

        [Test]
        public void SHRw_fabs_dr()
        {
            RewriteCode("5DFE"); // fabs\tdr14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|dr14 = fabs(dr14)");
        }

        [Test]
        public void SHRw_fabs_fr()
        {
            RewriteCode("5DFF"); // fabs\tfr15
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|fr15 = fabs(fr15)");
        }

        [Test]
        public void SHRw_fadd_dr()
        {
            RewriteCode("C0FE"); // fadd\tdr12,dr14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|dr14 = dr14 + dr12");
        }

        [Test]
        public void SHRw_fadd_fr()
        {
            RewriteCode("C0FF"); // fadd\tfr12,fr15
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|fr15 = fr15 + fr12");
        }

        [Test]
        public void SHRw_fcmp_eq_dr()
        {
            RewriteCode("C4FE"); // fcmp/eq\tdr12,dr14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = dr14 == dr12");
        }

        [Test]
        public void SHRw_fcmp_eq_fr()
        {
            RewriteCode("C4FF"); // fcmp/eq\tfr12,fr15
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = fr15 == fr12");
        }

        [Test]
        public void SHRw_fcmp_gt_dr()
        {
            RewriteCode("C5FE"); // fcmp/gt\tdr12,dr14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = dr14 > dr12");
        }

        [Test]
        public void SHRw_fcmp_gt_fr()
        {
            RewriteCode("C5FF"); // fcmp/gt\tfr12,fr15
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = fr15 > fr12");
        }

        [Test]
        public void SHRw_fcnvds()
        {
            RewriteCode("BDFE"); // fcnvds\tdr14,fpul
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|fpul = (real32) dr14");
        }

        [Test]
        public void SHRw_fcnvsd()
        {
            RewriteCode("ADFE"); // fcnvsd\tfpul,dr14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|dr14 = (real64) fpul");
        }

        [Test]
        public void SHRw_fdiv_dr()
        {
            RewriteCode("C3FE"); // fdiv\tdr12,dr14
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|dr14 = dr14 / dr12");
        }

        [Test]
        public void SHRw_fdiv_fr()
        {
            RewriteCode("C3FF"); // fdiv\tfr12,fr15
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|fr15 = fr15 / fr12");
        }

        [Test]
        [Ignore("Wait until encountering this in real code")]
        public void SHRw_fipr()
        {
            RewriteCode("EDFE"); // fipr\tfv8,fv12
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|@@@");
        }

        [Test]
        public void SHRw_flds()
        {
            RewriteCode("1DF8"); // flds\tfr8,fpul
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|fpul = fr8");
        }

        [Test]
        public void SHRw_fldi0()
        {
            RewriteCode("8DF8"); // fldi0\tfr8
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|fr8 = 0.0F");
        }

        [Test]
        public void SHRw_fldi1()
        {
            RewriteCode("9DF8"); // fldi1\tfr8
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|fr8 = 1.0F");
        }

        [Test]
        public void SHRw_jmp_r()
        {
            RewriteCode("2B40"); // jmp\t@r0
            AssertCode(
                "0|TD-|00100000(2): 1 instructions",
                "1|TD-|goto r0");
        }

        [Test]
        public void SHRw_lds_l_pr()
        {
            RewriteCode("264F"); // lds.l\t@r15+,pr
            AssertCode(
                "0|L--|00100000(2): 3 instructions",
                "1|L--|v2 = Mem0[r15:word32]",
                "2|L--|r15 = r15 + 4",
                "3|L--|pr = v2");
        }

        [Test]
        public void SHRw_mov_I_r()
        {
            RewriteCode("FFE1"); // mov\t#FF,r1
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r1 = 0xFFFFFFFF");
        }

        [Test]
        public void SHRw_mov_l_predec()
        {
            RewriteCode("862F"); // mov.l\tr8,@-r15
            AssertCode(
                "0|L--|00100000(2): 2 instructions",
                "1|L--|r15 = r15 - 4",
                "2|L--|Mem0[r15:word32] = r8");
        }

        [Test]
        public void SHRw_mov_l_disp_pc()
        {
            RewriteCode("02D0"); // mov.l\t@(08,pc),r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = Mem0[0x0010000C:word32]");
        }

        [Test]
        public void SHRw_mov_l_indexed_r()
        {
            RewriteCode("CE 01"); // mov.l\t@(r0,r12),r1
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r1 = Mem0[r0 + r12:word32]");
        }

        [Test]
        public void SHRw_mov_l_r_indexed()
        {
            RewriteCode("62 52"); // mov.l\t@(8,r6),r2
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r2 = Mem0[r6 + 8:word32]");

        }

        [Test]
        public void SHRw_mov_l_indirect_r()
        {
            RewriteCode("12 62"); // mov.l\t@r1,r2
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r2 = Mem0[r1:word32]");

        }

        [Test]
        public void SHRw_mov_r_r()
        {
            RewriteCode("7364"); // mov\tr7,r4
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r4 = r7");

        }

        [Test]
        public void SHRw_mov_w_indirect_r()
        {
            RewriteCode("0500"); // mov.w\tr0,@(r0,r0)
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|Mem0[r0 + r0:word16] = r0");
        }

        [Test]
        public void SHRw_mov()
        {
            RewriteCode("8669"); // mov.l\t@r8+,r9
            AssertCode(
                "0|L--|00100000(2): 3 instructions",
                "1|L--|v2 = Mem0[r8:word32]",
                "2|L--|r8 = r8 + 4",
                "3|L--|r9 = v2");
        }

        [Test]
        public void SHRw_mov_w_pc()
        {
            RewriteCode("3390"); // mov.w\t@(66,pc),r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = Mem0[0x0010006A:word16]");
        }

        [Test]
        public void SHRw_mova()
        {
            RewriteCode("3E C7"); // mova\t@(F8,pc),r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = 001000FC");
        }

        [Test]
        public void SHRw_movt()
        {
            RewriteCode("29 00"); // movt\tr0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = (int32) T");
        }

        [Test]
        public void SHRw_nop()
        {
            RewriteCode("0900"); // nop
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|nop");
        }

        [Test]
        public void SHRw_neg()
        {
            RewriteCode("0B60"); // neg\tr0,r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = -r0");
        }

        [Test]
        public void SHRw_not()
        {
            RewriteCode("9760"); // not\tr9,r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = ~r9");
        }

        [Test]
        public void SHRw_rts()
        {
            RewriteCode("0B00"); // rts
            AssertCode(
                "0|TD-|00100000(2): 1 instructions",
                "1|T--|return (0,0)");
        }

        [Test]
        public void SHRw_shll2()
        {
            RewriteCode("0840"); // shll2\tr0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|r0 = r0 << 2");
        }

        [Test]
        public void SHRw_sts_l_pr_predec()
        {
            RewriteCode("224F"); // sts.l\tpr,@-r15
            AssertCode(
                "0|L--|00100000(2): 2 instructions",
                "1|L--|r15 = r15 - 4",
                "2|L--|Mem0[r15:word32] = pr");
        }

        [Test]
        public void SHRw_tst_imm()
        {
            RewriteCode("01C8"); // tst\t#01,r0
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = (r0 & 0x00000001) == 0x00000000");
        }

        [Test]
        public void SHRw_tst_r_r()
        {
            RewriteCode("6826"); // tst\tr6,r6
            AssertCode(
                "0|L--|00100000(2): 1 instructions",
                "1|L--|T = (r6 & r6) == 0x00000000");
        }
    }
}

