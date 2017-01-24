﻿#region License
/* 
 * Copyright (C) 1999-2017 Pavel Tomin.
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
using Reko.Core;
using Reko.Core.CLanguage;
using Reko.Core.Types;
using Reko.Libraries.Python;
using Rhino.Mocks;

namespace Reko.UnitTests.Core.Analysis
{
    [TestFixture]
    public class PyBuildValueFormatParserTests
    {
        private PyBuildValueFormatParser parser;
        private MockRepository mr;
        private Program program;

        [SetUp]
        public void Setup()
        {
            this.mr = new MockRepository();
            var arch = mr.Stub<IProcessorArchitecture>();
            var platform = mr.Stub<IPlatform>();
            arch.Stub(a => a.WordWidth).Return(PrimitiveType.Word32);
            platform.Stub(p => p.Architecture).Return(arch);
            platform.Stub(p => p.GetByteSizeFromCBasicType(CBasicType.Long)).Return(4);
            platform.Stub(p => p.GetByteSizeFromCBasicType(CBasicType.LongLong)).Return(8);
            platform.Stub(p => p.GetByteSizeFromCBasicType(CBasicType.Double)).Return(8);
            platform.Stub(p => p.PointerType).Return(PrimitiveType.Pointer32);
            this.program = new Program { Platform = platform };
        }

        private void ParseChar32(string formatString)
        {
            mr.ReplayAll();

            this.parser = new PyBuildValueFormatParser(
                program,
                Address.Ptr32(0x123400),
                formatString,
                null);
            parser.Parse();

            mr.VerifyAll();
        }

        [Test]
        public void PBFP_Empty()
        {
            ParseChar32("");
            Assert.AreEqual(0, parser.ArgumentTypes.Count);
        }

        [Test]
        public void PBFP_NoFormat()
        {
            ParseChar32(":, \t");
            Assert.AreEqual(0, parser.ArgumentTypes.Count);
        }

        [Test]
        public void PBFP_Integer()
        {
            ParseChar32("i:");
            Assert.AreEqual(1, parser.ArgumentTypes.Count);
            Assert.AreEqual("int32", parser.ArgumentTypes[0].ToString());
        }

        [Test]
        public void PBFP_Float()
        {
            ParseChar32("f");
            Assert.AreEqual(1, parser.ArgumentTypes.Count);
            Assert.AreEqual("real64", parser.ArgumentTypes[0].ToString());
        }

        [Test]
        public void PBFP_Double()
        {
            ParseChar32("d");
            Assert.AreEqual(1, parser.ArgumentTypes.Count);
            Assert.AreEqual("real64", parser.ArgumentTypes[0].ToString());
        }

        [Test]
        public void PBFP_Char()
        {
            ParseChar32("c");
            Assert.AreEqual(1, parser.ArgumentTypes.Count);
            Assert.AreEqual("int32", parser.ArgumentTypes[0].ToString());
        }

        [Test]
        public void PBFP_TwoFormats()
        {
            ParseChar32("cs");
            Assert.AreEqual(2, parser.ArgumentTypes.Count);
            Assert.AreEqual("int32", parser.ArgumentTypes[0].ToString());
            Assert.AreEqual("(ptr char)", parser.ArgumentTypes[1].ToString());

        }

        [Test]
        public void PBFP_PyObject()
        {
            ParseChar32("O");
            Assert.AreEqual(1, parser.ArgumentTypes.Count);
            Assert.AreEqual("(ptr PyObject)", parser.ArgumentTypes[0].ToString());
        }

        [Test]
        public void PBFP_PyObjectConverter()
        {
            ParseChar32("O&");
            Assert.AreEqual(2, parser.ArgumentTypes.Count);
            Assert.AreEqual("(ptr code)", parser.ArgumentTypes[0].ToString());
            Assert.AreEqual("(ptr void)", parser.ArgumentTypes[1].ToString());
        }

        [Test]
        public void PBFP_longlong()
        {
            ParseChar32("(LK)");
            Assert.AreEqual(2, parser.ArgumentTypes.Count);
            Assert.AreEqual("int64", parser.ArgumentTypes[0].ToString());
            Assert.AreEqual("uint64", parser.ArgumentTypes[1].ToString());
        }

        [Test]
        public void PBFP_list()
        {
            ParseChar32("[(d){d:d}[d,d,d]]");
            Assert.AreEqual(6, parser.ArgumentTypes.Count);
        }

        [Test]
        public void PBFP_StringAndLength()
        {
            ParseChar32("s#");
            Assert.AreEqual(2, parser.ArgumentTypes.Count);
            Assert.AreEqual("(ptr char)", parser.ArgumentTypes[0].ToString());
            Assert.AreEqual("int32", parser.ArgumentTypes[1].ToString());
        }
    }
}
