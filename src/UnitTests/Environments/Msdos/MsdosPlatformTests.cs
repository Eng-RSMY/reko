#region License
/* 
 * Copyright (C) 1999-2017 John K�ll�n.
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
using Reko.Arch.X86;
using Reko.Core;
using Reko.Core.Configuration;
using Reko.Core.Expressions;
using Reko.Core.Services;
using Reko.Core.Types;
using Reko.Environments.Msdos;
using Rhino.Mocks;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Reko.UnitTests.Environments.Msdos
{
    [TestFixture]
    public class MsDosPlatformTests
    {
        private MockRepository mr;
        private ServiceContainer sc;
        private X86ArchitectureReal arch;
        private MsdosPlatform platform;
        private Procedure proc;

        [SetUp]
        public void Setup()
        {
            mr = new MockRepository();
            var cfgSvc = mr.Stub<IConfigurationService>();
            var tlSvc = mr.Stub<ITypeLibraryLoaderService>();
            var env = mr.Stub<OperatingEnvironment>();
            env.Stub(e => e.TypeLibraries).Return(new List<ITypeLibraryElement>());
            env.Stub(e => e.CharacteristicsLibraries).Return(new List<ITypeLibraryElement>());
            cfgSvc.Stub(c => c.GetEnvironment("ms-dos")).Return(env);
            sc = new ServiceContainer();
            sc.AddService<IFileSystemService>(new FileSystemServiceImpl());
            sc.AddService<IConfigurationService>(cfgSvc);
            sc.AddService<ITypeLibraryLoaderService>(tlSvc);
        }

        private void Given_MsdosPlatform()
        {
            this.arch = new X86ArchitectureReal();
            this.platform = new MsdosPlatform(sc, arch);
        }

        private void Given_Procedure()
        {
            this.proc = Procedure.Create(Address.Ptr32(0x1000), arch.CreateFrame());
        }

        [Test]
        public void MspRealModeServices()
        {
            mr.ReplayAll();
            Given_MsdosPlatform();

            var state = arch.CreateProcessorState();
            state.SetRegister(Registers.ah, Constant.Byte(0x3E));
            SystemService svc = platform.FindService(0x21, state);
            Assert.AreEqual("msdos_close_file", svc.Name);
            Assert.AreEqual(1, svc.Signature.Parameters.Length);
            Assert.IsFalse(svc.Characteristics.Terminates, "close() shouldn't terminate program");

            state.SetRegister(Registers.ah, Constant.Byte(0x4C));
            svc = platform.FindService(0x21, state);
            Assert.AreEqual("msdos_terminate", svc.Name);
            Assert.AreEqual(1, svc.Signature.Parameters.Length);
            Assert.IsTrue(svc.Characteristics.Terminates, "terminate() should terminate program");

            state.SetRegister(Registers.ah, Constant.Byte(0x2F));
            svc = platform.FindService(0x21, state);
            Assert.AreEqual("msdos_get_disk_transfer_area_address", svc.Name);
            Assert.AreEqual(0, svc.Signature.Parameters.Length);
            SequenceStorage seq = (SequenceStorage)svc.Signature.ReturnValue.Storage;
            Assert.AreEqual("es", seq.Head.Name);
            Assert.AreEqual("bx", seq.Tail.Name);
        }

        [Test]
        public void Msp_cdecl_call()
        {
            Given_MsdosPlatform();
            Given_Procedure();
            var ax = proc.Frame.EnsureRegister(Registers.ax);
            var arg06 = proc.Frame.EnsureStackArgument(6, PrimitiveType.Word16);
            var sb = new SignatureBuilder(proc.Frame, arch);
            sb.AddOutParam(ax);
            sb.AddInParam(arg06);
            var sig = sb.BuildSignature();
            sig.ReturnAddressOnStack = 4;
            sig.StackDelta = 0;
            var cc = platform.DetermineCallingConvention(sig);
            Assert.AreEqual("__cdecl", cc);
        }

        [Test]
        public void Msp_cdecl_long_return_value()
        {
            Given_MsdosPlatform();
            Given_Procedure();
            var dx_ax = proc.Frame.EnsureSequence(Registers.dx, Registers.ax, PrimitiveType.Word32);
            var arg06 = proc.Frame.EnsureStackArgument(6, PrimitiveType.Word16);
            var sb = new SignatureBuilder(proc.Frame, arch);
            sb.AddOutParam(dx_ax);
            sb.AddInParam(arg06);
            var sig = sb.BuildSignature();
            sig.ReturnAddressOnStack = 4;
            sig.StackDelta = 0;
            var cc = platform.DetermineCallingConvention(sig);
            Assert.AreEqual("__cdecl", cc);
        }
    }
}
