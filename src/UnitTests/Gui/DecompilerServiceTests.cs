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
using Reko.Core;
using Reko.Core.Services;
using Reko.Gui;
using Reko.UnitTests.Mocks;
using Rhino.Mocks;
using System;
using System.ComponentModel.Design;

namespace Reko.UnitTests.Gui
{
    [TestFixture]
    public class DecompilerServiceTests
    {
        ServiceContainer sc;
        IDecompilerService svc;
        private MockRepository mr;

        [SetUp]
        public void Setup()
        {
            mr = new MockRepository();
            sc = new ServiceContainer();
            svc = new DecompilerService();
            sc.AddService(typeof(IDecompilerService), svc);
            sc.AddService(typeof(DecompilerEventListener), new FakeDecompilerEventListener());
        }
        
        [Test]
        public void DecSvc_NotifyOnChangedDecompiler()
        {
            var loader = mr.Stub<ILoader>();
            var host = mr.Stub<DecompilerHost>();
            sc.AddService<DecompilerHost>(host);
            mr.ReplayAll();

            DecompilerDriver d = new DecompilerDriver(loader, sc);
            bool decompilerChangedEventFired = true;
            svc.DecompilerChanged += delegate(object o, EventArgs e)
            {
                decompilerChangedEventFired = true;
            };

            svc.Decompiler = d;

            Assert.IsTrue(decompilerChangedEventFired, "Should have fired a change event");
        }

        [Test]
        public void DecSvc_EmptyDecompilerProjectName()
        {
            IDecompilerService svc = new DecompilerService();
            Assert.IsEmpty(svc.ProjectName, "Shouldn't have project name available.");
        }

        [Test]
        public void DecSvc_DecompilerProjectName()
        {
            IDecompilerService svc = new DecompilerService();
            var loader = mr.StrictMock<ILoader>();
            var host = mr.StrictMock<DecompilerHost>();
            var arch = mr.StrictMock<IProcessorArchitecture>();
            var platform = mr.StrictMock<IPlatform>();
            var fileName = OsPath.Relative("foo", "bar", "baz.exe");
            var bytes = new byte[100];
            var mem = new MemoryArea(Address.Ptr32(0x1000), bytes);
            var imageMap = new SegmentMap(
                    mem.BaseAddress,
                    new ImageSegment("code", mem, AccessMode.ReadWriteExecute));
            var program = new Program(imageMap, arch, platform);
            sc.AddService<DecompilerHost>(host);
            platform.Stub(p => p.CreateMetadata()).Return(new TypeLibrary());
            loader.Stub(l => l.LoadImageBytes(fileName, 0)).Return(bytes);
            loader.Stub(l => l.LoadExecutable(fileName, bytes, null, null)).Return(program);
            loader.Replay();
            var dec = new DecompilerDriver(loader, sc);
            mr.ReplayAll();

            svc.Decompiler = dec;
            svc.Decompiler.Load(fileName);

            Assert.IsNotNull(svc.Decompiler.Project);
            Assert.AreEqual("baz.exe",  svc.ProjectName, "Should have project name available.");
            mr.VerifyAll();
        }
    }
}
