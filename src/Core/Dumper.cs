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

using Reko.Core.Machine;
using Reko.Core.Output;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Reko.Core
{
	/// <summary>
	/// Dumps low-level information about a binary.
	/// </summary>
	public class Dumper
	{
        private Program program;
        private IProcessorArchitecture arch;

		public Dumper(Program program)
		{
            this.program = program;
            this.arch = program.Architecture;
		}

        public bool ShowAddresses { get; set; }
        public bool ShowCodeBytes { get; set; }

        public void Dump(Formatter formatter)
        {
            var map = program.SegmentMap;
            var mappedItems =
                from seg in map.Segments.Values
                from item in program.ImageMap.Items.Values
                where seg.IsInRange(item.Address)
                group new { seg, item } by seg into g
                orderby g.Key.Address
                select new { g.Key, Items = g.Select(gg => gg.item) }; 

            foreach (var g in mappedItems)
            {
                formatter.WriteLine(";;; Segment {0} ({1})", g.Key.Name, g.Key.Address);
                if (g.Key.Designer != null)
                {
                    g.Key.Designer.Render(g.Key, program, new AsmCommentFormatter(formatter));
                }
                else
                {
                    foreach (var item in g.Items)
                    {
                        DumpItem(g.Key, item, formatter);
                    }
                }
            }
        }

        private void DumpItem(ImageSegment segment, ImageMapItem i, Formatter formatter)
        {
            ImageMapBlock block = i as ImageMapBlock;
            if (block != null)
            {
                formatter.WriteLine();
                Procedure proc;
                if (program.Procedures.TryGetValue(block.Address, out proc))
                {
                    formatter.WriteComment(string.Format(
                        ";; {0}: {1}", proc.Name, block.Address));
                    formatter.WriteLine();

                    formatter.Write(proc.Name);
                    formatter.Write(" ");
                    formatter.Write("proc");
                    formatter.WriteLine();
                }
                else
                {
                    formatter.Write(block.Block.Name);
                    formatter.Write(":");
                    formatter.WriteLine();
                }
                DumpAssembler(program.SegmentMap, block.Address, block.Address + block.Size, formatter);
                return;
            }

            ImageMapVectorTable table = i as ImageMapVectorTable;
            if (table != null)
            {
                formatter.WriteLine(";; Code vector at {0} ({1} bytes)",
                    table.Address, table.Size);
                foreach (Address addr in table.Addresses)
                {
                    formatter.WriteLine("\t{0}", addr != null ? addr.ToString() : "-- null --");
                }
                DumpData(program.SegmentMap, i.Address, i.Size, formatter);
            }
            else
            {
                var segLast = segment.Address + segment.Size;
                var size = segLast - i.Address;
                size = Math.Min(i.Size, size);
                if (i.DataType == null || i.DataType is UnknownType ||
                    i.DataType is CodeType)
                {
                    DumpData(program.SegmentMap, i.Address, size, formatter);
                }
                else
                {
                    DumpTypedData(program.SegmentMap, i, formatter);
                }
            }
        }

        public void DumpData(SegmentMap map, Address address, int cbBytes, Formatter stm)
        {
            if (cbBytes < 0)
                throw new ArgumentException("Must be a nonnegative number.", "cbBytes"); 
            DumpData(map, address, (uint)cbBytes, stm);
        }

        public void DumpData(SegmentMap map, AddressRange range, Formatter stm)
        {
            DumpData(map, range.Begin, (long) (range.End - range.Begin), stm);
        }

		public void DumpData(SegmentMap map, Address address, long cbBytes, Formatter stm)
		{
            const int BytesPerLine = 16;
            var linAddr = address.ToLinear();
            ulong cSkip = linAddr - BytesPerLine * (linAddr / BytesPerLine);
            ImageSegment segment;
            if (!map.TryFindSegment(address, out segment) || segment.MemoryArea == null)
                return;
            byte[] prevLine = null;
            bool showEllipsis = true;
			EndianImageReader rdr = arch.CreateImageReader(segment.MemoryArea, address);
			while (cbBytes > 0)
			{
				StringBuilder sb = new StringBuilder(0x12);
                var bytes = new List<byte>();
                var sbBytes = new StringBuilder();
				try 
				{
					sbBytes.AppendFormat("{0} ", rdr.Address);
					for (int i = 0; i < BytesPerLine; ++i)
					{
						if (cbBytes > 0 && cSkip == 0)
						{
							byte b = rdr.ReadByte();
                            bytes.Add(b);
							sbBytes.AppendFormat("{0:X2} ", b);
							sb.Append(0x20 <= b && b < 0x7F
								? (char) b
								: '.');
							--cbBytes;
						}
						else
						{
							sbBytes.Append("   ");
							if (cSkip > 0)
								sb.Append(' ');
							--cSkip;
						}
					}
                    var ab = bytes.ToArray();
                    if (!HaveSameZeroBytes(prevLine, ab))
                    {
                        stm.Write(sbBytes.ToString());
                        stm.WriteLine(sb.ToString());
                        showEllipsis = true;
                    }
                    else
                    {
                        if (showEllipsis)
                        {
                            stm.WriteLine("; ...");
                            showEllipsis = false;
                        }
                    }
                    prevLine = ab;
                } 
				catch
				{
					stm.WriteLine();
					stm.WriteLine(";;; ...end of image");
					return;
				}
			}
		}

        private bool HaveSameZeroBytes(byte[] prevLine, byte[] ab)
        {
            if (prevLine == null)
                return false;
            if (prevLine.Length != ab.Length)
                return false;
            for (int i = 0; i < ab.Length;++i)
            {
                if (prevLine[i] != ab[i] || ab[i] != 0)
                    return false;
            }
            return true;
        }

        public void DumpAssembler(SegmentMap map, Address addrStart, Address addrLast, Formatter formatter)
        {
            ImageSegment segment;
            if (!map.TryFindSegment(addrStart, out segment))
                return;
            var dasm = arch.CreateDisassembler(arch.CreateImageReader(segment.MemoryArea, addrStart));
            try
            {
                var writer = new InstrWriter(formatter);
                foreach (var instr in dasm)
                {
                    if (instr.Address >= addrLast)
                        break;
                    if (!DumpAssemblerLine(segment.MemoryArea, instr, writer))
                        break;
                }
            }
            catch (Exception ex)
            {
                formatter.WriteLine(ex.Message);
                formatter.WriteLine();
            }
        }

        public bool DumpAssemblerLine(MemoryArea mem, MachineInstruction instr, InstrWriter writer)
        {
            Address addrBegin = instr.Address;
            if (ShowAddresses)
                writer.Write("{0} ", addrBegin);
            if (ShowCodeBytes)
            {
                WriteByteRange(mem, instr.Address, instr.Address + instr.Length, writer);
                if (instr.Length * 3 < 16)
                {
                    writer.Write(new string(' ', 16 - (instr.Length * 3)));
                }
            }
            writer.Write("\t");
            writer.Address = addrBegin;
            writer.Address = instr.Address;
            instr.Render(writer, MachineInstructionWriterOptions.None);
            writer.WriteLine();
            return true;
        }

        private void DumpTypedData(SegmentMap map, ImageMapItem item, Formatter w)
        {
            ImageSegment segment;
            if (!map.TryFindSegment(item.Address, out segment) || segment.MemoryArea == null)
                return;
            WriteLabel(item.Address, w);

            var rdr = arch.CreateImageReader(segment.MemoryArea, item.Address);
            item.DataType.Accept(new TypedDataDumper(rdr, item.Size, w));
        }

        private void WriteLabel(Address addr, Formatter w)
        {
            ImageSymbol sym;
            if (program.ImageSymbols.TryGetValue(addr, out sym) &&
             !string.IsNullOrEmpty(sym.Name))
            {
                w.Write(sym.Name);
                w.Write("\t\t; {0}",addr);

                w.WriteLine();
            }
            else
            {
                w.Write(Block.GenerateName(addr));
            }
            w.Write("\t");
        }

        public void WriteByteRange(MemoryArea image, Address begin, Address addrEnd, InstrWriter writer)
		{
			EndianImageReader rdr = arch.CreateImageReader(image, begin);
			while (rdr.Address < addrEnd)
			{
				writer.Write(string.Format("{0:X2} ", rdr.ReadByte()));
			}
		}

        public class InstrWriter : MachineInstructionWriter
        {
            private Formatter formatter;

            public InstrWriter(Formatter formatter)
            {
                this.formatter = formatter;
            }

            public IPlatform Platform { get; private set; }
            public Address Address { get; set; }

            public void Tab()
            {
                formatter.Write("\t");
            }

            public void Write(string s)
            {
                formatter.Write(s);
            }

            public void Write(uint n)
            {
                formatter.Write(n.ToString());
            }

            public void Write(char c)
            {
                formatter.Write(c);
            }

            public void Write(string fmt, params object[] parms)
            {
                formatter.Write(fmt, parms);
            }

            public void WriteAddress(string formattedAddress, Address addr)
            {
                formatter.WriteHyperlink(formattedAddress, addr);
            }

            public void WriteOpcode(string opcode)
            {
                formatter.Write(opcode);
            }

            public void WriteLine()
            {
                formatter.WriteLine();
            }
        }

        class AsmCommentFormatter : Formatter
        {
            private Formatter w;
            private bool needPrefix;

            public AsmCommentFormatter(Formatter w)
            {
                this.w = w;
                this.needPrefix = true;
            }

            public override void Terminate()
            {
                WritePrefix();
                w.Terminate();
            }

            public override Formatter Write(char ch)
            {
                WritePrefix();
                return w.Write(ch);
            }

            public override void Write(string s)
            {
                WritePrefix();
                w.Write(s);
            }

            public override void Write(string format, params object[] arguments)
            {
                WritePrefix();
                w.Write(format, arguments);
            }

            public override void WriteComment(string comment)
            {
                WritePrefix();
                w.WriteComment(comment);
            }

            public override void WriteHyperlink(string text, object href)
            {
                WritePrefix();
                w.WriteHyperlink(text, href);
            }

            public override void WriteKeyword(string keyword)
            {
                WritePrefix();
                w.WriteKeyword(keyword);
            }

            public override void WriteLine()
            {
                WritePrefix();
                w.WriteLine();
                needPrefix = true;
            }

            public override void WriteLine(string s)
            {
                WritePrefix();
                w.WriteLine(s);
                needPrefix = true;
            }

            public override void WriteLine(string format, params object[] arguments)
            {
                WritePrefix();
                w.WriteLine(format, arguments);
                needPrefix = true;
            }

            public override void WriteType(string typeName, DataType dt)
            {
                WritePrefix();
                w.WriteType(typeName, dt);
            }

            private void WritePrefix()
            {
                if (needPrefix)
                {
                    w.Write("; ");
                    needPrefix = false;
                }
            }
        }
    }
}
