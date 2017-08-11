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

using Reko.Core;
using Reko.Core.Expressions;
using Reko.Core.Lib;
using Reko.Core.Machine;
using Reko.Core.Rtl;
using Reko.Core.Serialization;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.Arch.Pdp11
{
    public class Registers
    {
        public static RegisterStorage r0 = new RegisterStorage("r0", 0, 0, PrimitiveType.Word16);
        public static RegisterStorage r1 = new RegisterStorage("r1", 1, 0, PrimitiveType.Word16);
        public static RegisterStorage r2 = new RegisterStorage("r2", 2, 0, PrimitiveType.Word16);
        public static RegisterStorage r3 = new RegisterStorage("r3", 3, 0, PrimitiveType.Word16);
        public static RegisterStorage r4 = new RegisterStorage("r4", 4, 0, PrimitiveType.Word16);
        public static RegisterStorage r5 = new RegisterStorage("r5", 5, 0, PrimitiveType.Word16);
        public static RegisterStorage sp = new RegisterStorage("sp", 6, 0, PrimitiveType.Word16);
        public static RegisterStorage pc = new RegisterStorage("pc", 7, 0, PrimitiveType.Word16);

        public static FlagRegister psw = new FlagRegister("psw", 12, PrimitiveType.Word16);

        public static RegisterStorage ac0 = new RegisterStorage("ac0", 16, 0, PrimitiveType.Real64);
        public static RegisterStorage ac1 = new RegisterStorage("ac1", 17, 0, PrimitiveType.Real64);
        public static RegisterStorage ac2 = new RegisterStorage("ac2", 18, 0, PrimitiveType.Real64);
        public static RegisterStorage ac3 = new RegisterStorage("ac3", 19, 0, PrimitiveType.Real64);
        public static RegisterStorage ac4 = new RegisterStorage("ac4", 20, 0, PrimitiveType.Real64);
        public static RegisterStorage ac5 = new RegisterStorage("ac5", 21, 0, PrimitiveType.Real64);

        public static FlagGroupStorage N = new FlagGroupStorage(psw, 8, "N",PrimitiveType.Bool);
        public static FlagGroupStorage Z = new FlagGroupStorage(psw, 4, "Z",PrimitiveType.Bool);
        public static FlagGroupStorage V = new FlagGroupStorage(psw, 2, "V", PrimitiveType.Bool);
        public static FlagGroupStorage C = new FlagGroupStorage(psw, 1, "C", PrimitiveType.Bool);
    }

    [Flags]
    public enum FlagM
    {
        NF = 8,
        ZF = 4,
        VF = 2,
        CF = 1,
    }

    public class Pdp11Architecture : ProcessorArchitecture
    {
        private RegisterStorage[] regs;
        private RegisterStorage[] fpuRegs;
        private FlagGroupStorage[] flagRegs;
        private Dictionary<uint, FlagGroupStorage> flagGroups;

        public Pdp11Architecture()
        {
            regs = new RegisterStorage[] { 
                Registers.r0, Registers.r1, Registers.r2, Registers.r3, 
                Registers.r4, Registers.r5, Registers.sp, Registers.pc,
            };
            fpuRegs = new RegisterStorage[]
            {
                Registers.ac0, Registers.ac1, Registers.ac2,Registers.ac3,
                Registers.ac4, Registers.ac5, 
            };
            flagRegs = new FlagGroupStorage[] 
            {
                Registers.N, Registers.Z, Registers.V, Registers.C
            };
            this.flagGroups = new Dictionary<uint, FlagGroupStorage>();

            InstructionBitSize = 16;
            StackRegister = Registers.sp;
            //CarryFlagMask  = { get { throw new NotImplementedException(); } }
            FramePointerType = PrimitiveType.Ptr16;
            PointerType = PrimitiveType.Ptr16;
            WordWidth = PrimitiveType.Word16;
        }

        #region IProcessorArchitecture Members

        public override IEnumerable<MachineInstruction> CreateDisassembler(EndianImageReader rdr)
        {
            return new Pdp11Disassembler(rdr, this);
        }

        public override EndianImageReader CreateImageReader(MemoryArea image, Address addr)
        {
            return new LeImageReader(image, addr);
        }

        public override EndianImageReader CreateImageReader(MemoryArea image, Address addrBegin, Address addrEnd)
        {
            return new LeImageReader(image, addrBegin, addrEnd);
        }

        public override EndianImageReader CreateImageReader(MemoryArea image, ulong offset)
        {
            return new LeImageReader(image, offset);
        }

        public override ImageWriter CreateImageWriter()
        {
            return new LeImageWriter();
        }

        public override ImageWriter CreateImageWriter(MemoryArea mem, Address addr)
        {
            return new LeImageWriter(mem, addr);
        }

        public override IEqualityComparer<MachineInstruction> CreateInstructionComparer(Normalize norm)
        {
            return new Pdp11InstructionComparer(norm);
        }

        public override ProcessorState CreateProcessorState()
        {
            return new Pdp11ProcessorState(this);
        }

        public override IEnumerable<Address> CreatePointerScanner(SegmentMap map, EndianImageReader rdr, IEnumerable<Address> knownAddresses, PointerScannerFlags flags)
        {
            throw new NotImplementedException();
        }

        public override RegisterStorage GetRegister(int i)
        {
            return (0 <= i && i < regs.Length)
                ? regs[i]
                : null;
        }

        internal RegisterStorage GetFpuRegister(int i)
        {
            return (0 <= i && i < fpuRegs.Length)
                ? fpuRegs[i]
                : null;
        }

        public override SortedList<string, int> GetOpcodeNames()
        {
            return Enum.GetValues(typeof(Opcode))
                .Cast<Opcode>()
                .ToSortedList(
                    v => v.ToString(),
                    v => (int)v);
        }

        public override int? GetOpcodeNumber(string name)
        {
            Opcode result;
            if (!Enum.TryParse(name, true, out result))
                return null;
            return (int)result;
        }

        public override RegisterStorage GetRegister(string name)
        {
            foreach (RegisterStorage reg in regs)
            {
                if (string.Compare(reg.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return reg;
            }
            return null;
        }

        public override RegisterStorage[] GetRegisters()
        {
            return regs;
        }

        public override bool TryGetRegister(string name, out RegisterStorage result)
        {
            result = null;
            foreach (RegisterStorage reg in regs)
            {
                if (string.Compare(reg.Name, name, StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    result = reg;
                    return true;
                }
            }
            return false;
        }

        public override RegisterStorage GetSubregister(RegisterStorage reg, int offset, int width)
        {
            return reg;
        }

        public override FlagGroupStorage GetFlagGroup(uint grf)
		{
            FlagGroupStorage f;
            if (flagGroups.TryGetValue(grf, out f))
                return f;

			PrimitiveType dt = Bits.IsSingleBitSet(grf) ? PrimitiveType.Bool : PrimitiveType.Byte;
            var fl = new FlagGroupStorage(Registers.psw, grf, GrfToString(grf), dt);
			flagGroups.Add(grf, fl);
			return fl;
		}

        public override FlagGroupStorage GetFlagGroup(string name)
        {
            uint grf = 0;
            foreach (var c in name)
            {
                switch (c)
                {
                case 'N': grf |= Registers.N.FlagGroupBits; break;
                case 'Z': grf |= Registers.Z.FlagGroupBits; break;
                case 'V': grf |= Registers.V.FlagGroupBits; break;
                case 'C': grf |= Registers.C.FlagGroupBits; break;
                }
            }
            return new FlagGroupStorage(Registers.psw, grf, name, PrimitiveType.Byte);
        }

        public override string GrfToString(uint grf)
        {
			var s = new StringBuilder();
			foreach (var r in flagRegs)
			{
				if ((grf & r.FlagGroupBits) != 0)
					s.Append(r.Name);
			}
			return s.ToString();
		}

        public override Expression CreateStackAccess(IStorageBinder frame, int cbOffset, DataType dataType)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<RtlInstructionCluster> CreateRewriter(EndianImageReader rdr, ProcessorState state, IStorageBinder binder, IRewriterHost host)
        {
            return new Pdp11Rewriter(this, new Pdp11Disassembler(rdr, this), binder, host);
        }

        public override Address MakeAddressFromConstant(Constant c)
        {
            return Address.Ptr16(c.ToUInt16());
        }

        public override Address ReadCodeAddress(int size, EndianImageReader rdr, ProcessorState state)
        {
            ushort uAddr = rdr.ReadLeUInt16();
            return Address.Ptr16(uAddr);
        }

        public override bool TryParseAddress(string txtAddress, out Address addr)
        {
            return Address.TryParse16(txtAddress, out addr);
        }

        #endregion
    }
}
