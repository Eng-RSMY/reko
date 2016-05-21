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
using Reko.Core.Lib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Reko.UnitTests.Core.Lib
{
    [TestFixture]
    public class UbjsonReaderTests
    {
        [Test]
        public void Ubjl_Null()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x5A });
            Assert.IsNull(ubjl.Read());
        }

        [Test]
        public void Ubjl_Noops()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x4E, 0x4E, 0x4E, 0x5A });
            Assert.IsNull(ubjl.Read());
        }

        [Test]
        public void Ubjl_False()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x4E, 0x46, 0x4E, 0x5A });
            Assert.IsFalse((bool)ubjl.Read());
        }

        [Test]
        public void Ubjl_True()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x4E, 0x54 });
            Assert.IsTrue((bool)ubjl.Read());
        }

        [Test]
        public void Ubjl_Int8()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x69, 0xFF });
            var o = ubjl.Read();
            Assert.AreEqual((sbyte)-1, (sbyte)o);
        }

        [Test]
        public void Ubjl_UInt8()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x55, 0xFF });
            var o = ubjl.Read();
            Assert.AreEqual((byte)255, (byte)o);
        }

        [Test]
        public void Ubjl_Int16()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x49, 0xFF, 0xF8 });
            var o = ubjl.Read();
            Assert.AreEqual((short)-8, (short)o);
        }

        [Test]
        public void Ubjl_Int32()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x6C, 0x12, 0x34, 0x56, 0x78 });
            var o = ubjl.Read();
            Assert.AreEqual(0x12345678, (int)o);
        }

        [Test]
        public void Ubjl_Int64()
        {
            var ubjl = new UbjsonReader(new byte[] { 0x4C, 0x12, 0x34, 0x56, 0x78, 0xAA, 0xBB, 0xCC, 0xDD });
            var o = ubjl.Read();
            Assert.AreEqual(0x12345678AABBCCDDL, (long)o);
        }

        private byte[] B(params byte[][] bytes)
        {
            return bytes.SelectMany(b => b).ToArray();
        }

        private byte[] B(float f)
        {
            var bytes = BitConverter.GetBytes(f);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        private byte[] B(double d)
        {
            var bytes = BitConverter.GetBytes(d);
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            return bytes;
        }

        [Test]
        public void Ubjl_Float32()
        {
            var ubjl = new UbjsonReader(B(new byte[] { 0x64 }, B(-1.5F)));
            var o = ubjl.Read();
            Assert.AreEqual(-1.5F, (float)o);
        }

        [Test]
        public void Ubjl_Float64()
        {
            var ubjl = new UbjsonReader(B(new byte[] { 0x44 }, B(-1.5)));
            var o = ubjl.Read();
            Assert.AreEqual(-1.5, (double)o);
        }

        [Test]
        public void Ubjl_SmallString()
        {
            var sExp = "hello";
            var stm = new MemoryStream();
            new UbjsonWriter(stm).Save(sExp);
            stm.Position = 0;
            var str = (string)new UbjsonReader(stm).Read();
            Assert.AreEqual(sExp, str);
        }
    }
}
