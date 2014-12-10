﻿#region License
/* 
 * Copyright (C) 1999-2014 John Källén.
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Decompiler.Core
{
    /// <summary>
    /// PointerScanners are used by the user to guess at pointers based on byte patterns.
    /// </summary>
    public abstract class PointerScanner : IEnumerable<uint>
    {
        private ImageReader rdr;
        private HashSet<uint> knownLinAddresses;
        private PointerScannerFlags flags;

        public PointerScanner(ImageReader rdr, HashSet<uint> knownLinAddresses, PointerScannerFlags flags)
        {
            this.rdr = rdr;
            this.knownLinAddresses = knownLinAddresses;
            this.flags = flags;
        }

        public IEnumerator<uint> GetEnumerator()
        {
            return new Enumerator(this, this.rdr.Clone());
        }

        IEnumerator IEnumerable.GetEnumerator() { return GetEnumerator(); }

        private class Enumerator : IEnumerator<uint>
        {
            private PointerScanner scanner;
            private ImageReader r;

            public Enumerator(PointerScanner scanner, ImageReader rdr)
            {
                this.scanner = scanner;
                this.r = rdr;
            }

            public uint Current { get; set; }

            object System.Collections.IEnumerator.Current { get { return Current; } }

            public bool MoveNext()
            {
                while (r.IsValid)
                {
                    var rdr = this.r;
                    uint linAddrInstr;
                    if (scanner.ProbeForPointer(rdr, out linAddrInstr))
                    {
                        Current = linAddrInstr;
                        return true;
                    }
                }
                return false;
            }

            public void Reset()
            {
                throw new NotSupportedException();
            }

            public void Dispose() { }
        }

        public virtual bool ProbeForPointer(ImageReader rdr, out uint linAddrInstr)
        {
            linAddrInstr = rdr.Address.Linear;
            uint target;
            var opcode = ReadOpcode(rdr);
            if ((flags & PointerScannerFlags.Calls) != 0)
            {
                if (MatchCall(rdr, opcode, out target) && knownLinAddresses.Contains(target))
                {
                    rdr.Seek(PointerAlignment);
                    return true;
                }
            }
            if ((flags & PointerScannerFlags.Jumps) != 0)
            {
                if (MatchJump(rdr, opcode, out target) && knownLinAddresses.Contains(target))
                {
                    rdr.Seek(PointerAlignment);
                    return true;
                }
            }
            if ((flags & PointerScannerFlags.Pointers) != 0)
            {
                if (PeekPointer(rdr, out target) && knownLinAddresses.Contains(target))
                {
                    rdr.Seek(PointerAlignment);
                    return true;
                }
            }
            rdr.Seek(PointerAlignment);
            return false;
        }

        public abstract int PointerAlignment { get; }

        public abstract uint ReadOpcode(ImageReader rdr);

        public abstract bool PeekPointer(ImageReader rdr, out uint target);

        public abstract bool MatchCall(ImageReader rdr, uint opcode, out uint target);

        public abstract bool MatchJump(ImageReader rdr, uint opcode, out uint target);
    }
}