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

using Reko.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Reko.ImageLoaders.Elf.Relocators
{
    public class ArmRelocator : ElfRelocator32
    {
        private ElfLoader32 loader;

        public ArmRelocator(ElfLoader32 loader) : base(loader)
        {
            this.loader = loader;
        }

        public override void RelocateEntry(Program program, ElfSymbol symbol, ElfSection referringSection, Elf32_Rela rela)
        {
            throw new NotImplementedException();
        }

        public override string RelocationTypeToString(uint type)
        {
            throw new NotImplementedException();
        }
    }
}
