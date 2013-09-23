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
using Decompiler.Arch.M68k;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Decompiler.UnitTests.Arch.M68k
{
    [TestFixture]
    public class M68kDisassemblerTests
    {
        private M68kDisassembler dasm;
        private M68kInstruction instr;

        private void DasmSingleInstruction(params byte[] bytes)
        {
            dasm = CreateDasm(bytes, 0x10000000);
            instr = dasm.Disassemble();
        }

        private M68kDisassembler CreateDasm(byte[] bytes, uint address)
        {
            Address addr = new Address(address);
            LoadedImage img = new LoadedImage(addr, bytes);
            return new M68kDisassembler(img.CreateReader(addr));
        }

        private M68kDisassembler CreateDasm(params ushort[] words)
        {
            byte[] bytes = words.SelectMany(w => new byte[] { (byte) (w >> 8), (byte) w }).ToArray();
            return CreateDasm(bytes, 0x10000000);
        }

        [Test]
        public void MoveQ()
        {
            byte[] bytes = new byte[] {
                0x72, 0x01
            };
            DasmSingleInstruction(bytes);
            Assert.AreEqual("moveq\t#$+01,d1", instr.ToString());
        }


        [Test]
        public void AddQ()
        {
            byte[] bytes = new byte[] {
                0x5E, 0x92
            };
            DasmSingleInstruction(bytes);
            Assert.AreEqual("addq.l\t#$+07,(a2)", instr.ToString());
        }

        [Test]
        public void Ori()
        {
            byte[] bytes = new byte[] {
                0x00, 0x00, 0x00, 0x12
            };
            DasmSingleInstruction(bytes);
            Assert.AreEqual("ori.b\t#$12,d0", instr.ToString());
        }

        [Test]
        public void OriCcr()
        {
            byte[] bytes = new byte[] {
                0x00, 0x3C, 0x00, 0x42
                };
            DasmSingleInstruction(bytes);
            Assert.AreEqual("ori.b\t#$42,ccr", instr.ToString());
        }

        [Test]
        public void OriSr()
        {
            byte[] bytes = new byte[] {
                0x00, 0x7C, 0x00, 0x42
                };
            DasmSingleInstruction(bytes);
            Assert.AreEqual("ori.w\t#$0042,sr", instr.ToString());
        }

        [Test]
        public void MoveW()
        {
            DasmSingleInstruction(0x30, 0x2F, 0x47, 0x11);
            Assert.AreEqual("move.w\t$4711(a7),d0", instr.ToString());
        }

        [Test]
        public void Lea()
        {
            DasmSingleInstruction(0x43, 0xEF, 0x00, 0x04);
            Assert.AreEqual("lea\t$0004(a7),a1", instr.ToString());
        }

        [Test]
        public void LslD()
        {
            DasmSingleInstruction(0xE5, 0x49, 0x00, 0x04);
            Assert.AreEqual("lsl.w\t#$+02,d1", instr.ToString());
        }

        [Test]
        public void AddaW()
        {
            DasmSingleInstruction(0xD2, 0xC1, 0x00, 0x04);
            Assert.AreEqual("adda.w\td1,a1", instr.ToString());
        }

        [Test]
        public void Addal()
        {
            dasm = CreateDasm(0xDBDC);
            Assert.AreEqual("adda.l\t(a4)+,a5", dasm.Disassemble().ToString());
        }

        [Test]
        public void MoveA()
        {
            DasmSingleInstruction(0x20, 0x51, 0x00, 0x04);
            Assert.AreEqual("movea.l\t(a1),a0", instr.ToString());
        }

        [Test]
        public void MoveM()
        {
            DasmSingleInstruction(0x48, 0xE7, 0x00, 0x04);
            Assert.AreEqual("movem.l\ta5,-(a7)", instr.ToString());
        }

        [Test]
        public void BraB()
        {
            DasmSingleInstruction(0x60, 0x1A);
            Assert.AreEqual("bra\t$1000001C", instr.ToString());
        }

        [Test]
        public void Bchg()
        {
            DasmSingleInstruction(0x01, 0x40);
            Assert.AreEqual("bchg\td0,d0", instr.ToString());
        }

        [Test]
        public void Dbf()
        {
            DasmSingleInstruction(0x51, 0xCA, 0xFF, 0xE4);
            Assert.AreEqual("dbf\td2,$0FFFFFE6", instr.ToString());
        }

        [Test]
        public void Moveb()
        {
            DasmSingleInstruction(0x14, 0x1A);
            Assert.AreEqual("move.b\t(a2)+,d2", instr.ToString());
        }

        [Test]
        public void ManyMoves()
        {
            dasm = CreateDasm(new byte[] { 0x20, 0x00, 0x20, 0x27, 0x20, 0x40, 0x20, 0x67, 0x20, 0x80, 0x21, 0x40, 0x00, 0x00 }, 0x10000000);
            Assert.AreEqual("move.l\td0,d0", dasm.Disassemble().ToString());
            Assert.AreEqual("move.l\t-(a7),d0", dasm.Disassemble().ToString());
            Assert.AreEqual("movea.l\td0,a0", dasm.Disassemble().ToString());
            Assert.AreEqual("movea.l\t-(a7),a0", dasm.Disassemble().ToString());
            Assert.AreEqual("move.l\td0,(a0)", dasm.Disassemble().ToString());
            Assert.AreEqual("move.l\td0,$0000(a0)", dasm.Disassemble().ToString());
        }

        [Test]
        public void AddB()
        {
            DasmSingleInstruction(0xD2, 0x02);
            Assert.AreEqual("add.b\td2,d1", instr.ToString());
        }

        [Test]
        public void Eor()
        {
            dasm = CreateDasm(0xB103, 0xB143, 0xB183);
            Assert.AreEqual("eor.b\td0,d3", dasm.Disassemble().ToString());
            Assert.AreEqual("eor.w\td0,d3", dasm.Disassemble().ToString());
            Assert.AreEqual("eor.l\td0,d3", dasm.Disassemble().ToString());
        }

        [Test]
        public void Bcs()
        {
            dasm = CreateDasm(0x6572);
            Assert.AreEqual("bcs\t$10000074", dasm.Disassemble().ToString());
        }

        private void RunTest(string expected, params ushort[] words)
        {
            dasm = CreateDasm(words);
            Assert.AreEqual(expected, dasm.Disassemble().ToString());
        }

        [Test]
        public void or_s_with_immediate()
        {
            RunTest("or.b\t#$23,d3", 0x863c, 0x1123);
            RunTest("or.w\t#$1123,d3", 0x867c, 0x1123);
            RunTest("or.l\t#$11234455,d3", 0x86Bc, 0x1123, 0x4455);
        }

        [Test]
        public void M68kdis_Pre_and_post_dec()
        {
            RunTest("move.w\t-(a3),(a3)+", 0x36E3);
        }

        [Test]
        public void M68kdis_eori()
        {
            RunTest("eori.b\t#$34,d0", 0x0A00, 0x1234);
        }

        [Test]
        public void M68kdis_muls_w()
        {
            RunTest("muls.w\t-(a3),d0", 0xC1E3);
        }

        [Test]
        public void M68kdis_mulu_l()
        {
            RunTest("mulu.l\td0,d6,d7", 0x4c00, 0x7406);
        }

        [Test]
        public void M68kdis_not()
        {
            RunTest("not.b\td7", 0x4607);
        }

        [Test]
        public void M68kdis_and()
        {
            RunTest("and.l\t-(a3),d1", 0xC2A3);
        }

        [Test]
        public void M68kdis_and_rev()
        {
            RunTest("and.l\td1,-(a3)", 0xC3A3);
        }

        [Test]
        public void M68dis_andi_32()
        {
            RunTest("andi.l\t#$00010000,(a4)+", 0x029C, 0x0001, 0x0000);
        }

        [Test]
        public void M68dis_andi_8()
        {
            RunTest("andi.b\t#$F0,d2", 0x0202, 0x00F0);
        }

        [Test]
        public void M68kdis_asrb_qb()
        {
            RunTest("asr.b\t#$07,d0", 0xEE00);
        }

        [Test]
        public void M68kdis_neg_w()
        {
            RunTest("neg.w\t(a3)+", 0x445B);
        }

        [Test]
        public void M68kdis_negx_8()
        {
            RunTest("negx.w\td0", 0x4040);
        }

        [Test]
        public void M68kdis_sub_er_16()
        {
            RunTest("sub.w\t-(a4),d0", 0x9064);
        }

        [Test]
        public void M68kdis_suba_16()
        {
            RunTest("suba.w\t(a4)+,a0", 0x90DC);
        }
    }
}
