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

using Reko.Arch.X86;
using Reko.Core;
using Reko.Core.Serialization;        // May need this for Win64 support.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using TWord = System.UInt32;
using Reko.Core.Expressions;
using Reko.Core.Types;

namespace Reko.Environments.Windows
{
    /// <summary>
    /// Emulates the Win32 operating environment. In particular, intercepts calls to GetProcAddress
    /// so that the procedures used by the decompiled program can be gleaned. 
    /// </summary>
    public class Win32Emulator : IPlatformEmulator, IImportResolver
    {
        private Dictionary<string, Module> modules;
        private TWord uPseudoFn;
        private SegmentMap map;
        private IPlatform platform;

        public Win32Emulator(SegmentMap map, IPlatform platform, Dictionary<Address, ImportReference> importReferences)
        {
            this.map = map;
            this.platform = platform;
            this.uPseudoFn = 0xDEAD0000u;   // unlikely to be a real pointer to a function
            this.InterceptedCalls = new Dictionary<uint, ExternalProcedure>();

            modules = new Dictionary<string, Module>(StringComparer.InvariantCultureIgnoreCase);
            AddWellKnownProcedures();
            InterceptCallsToImports(importReferences);
        }

        private void AddWellKnownProcedures()
        {
            var kernel32 = EnsureModule("kernel32.dll");
            EnsureProc(kernel32, "LoadLibraryA", LoadLibraryA);
            EnsureProc(kernel32, "GetProcAddress", GetProcAddress);
            EnsureProc(kernel32, "ExitProcess", ExitProcess, new ProcedureCharacteristics { Terminates = true });
            EnsureProc(kernel32, "VirtualProtect", VirtualProtect);
        }

        private Module EnsureModule(string moduleName)
        {
            Module module;
            if (!modules.TryGetValue(moduleName, out module))
            {
                module = new Module(moduleName);
                modules.Add(module.Name, module);
                module.Handle = (TWord)modules.Count * 16u;
            }
            return module;
        }

        public SimulatedProc EnsureProc(
            Module module, 
            string procName,
            Action<IProcessorEmulator> emulator,
            ProcedureCharacteristics chars = null)
        {
            SimulatedProc proc;
            if (!module.Procedures.TryGetValue(procName, out proc))
            {
                var extProc = platform.LookupProcedureByName(module.Name, procName);
                proc = new SimulatedProc(procName, emulator);
                proc.Signature = extProc.Signature;
                if (chars != null)
                    proc.Characteristics = chars;
                proc.uFakedAddress = ++this.uPseudoFn;
                InterceptedCalls[proc.uFakedAddress] = proc;
                module.Procedures.Add(procName, proc);
            }
            return proc;
        }

        public Dictionary<TWord, ExternalProcedure> InterceptedCalls { get; private set; }

        private void InterceptCallsToImports(Dictionary<Address, ImportReference> importReferences)
        {
            foreach (var imp in importReferences)
            {
                uint pseudoPfn = ((SimulatedProc)imp.Value.ResolveImportedProcedure(this, null, null)).uFakedAddress;
                WriteLeUInt32(imp.Key, pseudoPfn);
            }
        }

        ExternalProcedure IImportResolver.ResolveProcedure(string moduleName, string importName, IPlatform platform)
        {
            Module module = EnsureModule(moduleName);
            return EnsureProc(module, importName, NYI);
        }

        ExternalProcedure IImportResolver.ResolveProcedure(string moduleName, int ordinal, IPlatform platform)
        {
            throw new NotImplementedException();
        }

        void LoadLibraryA(IProcessorEmulator emulator)
        {
            // M[Esp] is return address.
            // M[Esp + 4] is pointer to DLL name.
            uint esp = (uint)emulator.ReadRegister(Registers.esp);
            uint pstrLibName = ReadLeUInt32(esp + 4u);
            string szLibName = ReadMbString(pstrLibName);
            Module module = EnsureModule(szLibName);
            emulator.WriteRegister(Registers.eax, module.Handle);

            // Clean up the stack.
            emulator.WriteRegister(Registers.esp, esp + 8);
        }

        void GetProcAddress(IProcessorEmulator emulator)
        {
            // M[esp] is return address
            // M[esp + 4] is hmodule
            // M[esp + 4] is pointer to function name
            uint esp = (uint)emulator.ReadRegister(Registers.esp);
            uint hmodule = ReadLeUInt32(esp + 4u);
            uint pstrFnName = ReadLeUInt32(esp + 8u);
            if ((pstrFnName & 0xFFFF0000) != 0)
            {
                string importName = ReadMbString(pstrFnName);
                var module = modules.Values.First(m => m.Handle == hmodule);
                SimulatedProc fn = EnsureProc(module, importName, NYI);
                emulator.WriteRegister(Registers.eax, fn.uFakedAddress);
                emulator.WriteRegister(Registers.esp, esp + 12);
            }
            else
            {
                //$TODO: import by ordinal.
                throw new NotImplementedException();
            }
        }

        void ExitProcess(IProcessorEmulator emulator)
        {
            emulator.Stop();
        }

        void VirtualProtect(IProcessorEmulator emulator)
        {
            uint esp = (uint)emulator.ReadRegister(Registers.esp);
            uint arg1 = ReadLeUInt32(esp + 4u );
            uint arg2 = ReadLeUInt32(esp + 8u );
            uint arg3 = ReadLeUInt32(esp + 12u);
            uint arg4 = ReadLeUInt32(esp + 16u);
            Debug.Print("VirtualProtect({0:X8},{1:X8},{2:X8},{3:X8})", arg1, arg2, arg3, arg4);

            emulator.WriteRegister(Registers.eax, 1u);
            emulator.WriteRegister(Registers.esp, esp + 20);
        }

        void NYI(IProcessorEmulator emulator)
        {
            throw new NotImplementedException();
        }

        private uint ReadLeUInt32(uint ea)
        {
            //$PERF: wow this is inefficient; an allocation
            // per memory fetch. TryFindSegment needs an overload
            // that accepts ulongs / linear addresses.
            var addr = Address.Ptr32(ea);
            ImageSegment segment;
            if (!map.TryFindSegment(addr, out segment))
                throw new AccessViolationException();
            return segment.MemoryArea.ReadLeUInt32(addr);
        }

        private void WriteLeUInt32(uint ea, uint value)
        {
            //$PERF: wow this is inefficient; an allocation
            // per memory fetch. TryFindSegment needs an overload
            // that accepts ulongs / linear addresses.
            var addr = Address.Ptr32(ea);
            ImageSegment segment;
            if (!map.TryFindSegment(addr, out segment))
                throw new AccessViolationException();
            segment.MemoryArea.WriteLeUInt32(addr, value);
        }

        private void WriteLeUInt32(Address ea, uint value)
        {
            //$PERF: wow this is inefficient; an allocation
            // per memory fetch. TryFindSegment needs an overload
            // that accepts ulongs / linear addresses.
            ImageSegment segment;
            if (!map.TryFindSegment(ea, out segment))
                throw new AccessViolationException();
            segment.MemoryArea.WriteLeUInt32(ea, value);
        }

        private string ReadMbString(TWord pstrLibName)
        {
            var addr = Address.Ptr32(pstrLibName);
            ImageSegment segment;
            if (!map.TryFindSegment(addr, out segment))
                throw new AccessViolationException();
            var rdr = segment.MemoryArea.CreateLeReader(addr);
            var ab = new List<byte>();
            for (;;)
            {
                byte b = rdr.ReadByte();
                if (b == 0)
                    break;
                ab.Add(b);
            }
            return Encoding.ASCII.GetString(ab.ToArray());
        }

        public bool InterceptCall(IProcessorEmulator emu, TWord l)
        {
            ExternalProcedure epProc;
            if (!this.InterceptedCalls.TryGetValue(l, out epProc))
                return false;
            ((SimulatedProc)epProc).Emulator(emu);
            return true;
        }

        public ProcedureConstant ResolveToImportedProcedureConstant(Statement stm, Constant c)
        {
            throw new NotImplementedException();
        }

        public Expression ResolveImport(string moduleName, string name, IPlatform platform)
        {
            throw new NotImplementedException();
        }

        public Expression ResolveImport(string moduleName, int ordinal, IPlatform platform)
        {
            throw new NotImplementedException();
        }

        public class SimulatedProc : ExternalProcedure
        {
            public SimulatedProc(string name, Action<IProcessorEmulator> emulator) : base(name, null) { Emulator = emulator; }

            public TWord uFakedAddress;
            public Action<IProcessorEmulator> Emulator;
        }

        public class Module
        {
            public string Name;
            public TWord Handle;
            public Dictionary<string, SimulatedProc> Procedures;

            public Module(string p)
            {
                this.Name = p;
                Procedures = new Dictionary<string, SimulatedProc>();
            }
        }
    }
}
