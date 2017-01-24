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

using Reko.Core.Code;
using Reko.Core.Output;
using Reko.Core.Serialization;
using Reko.Core.Types;
using System;
using System.IO;

namespace Reko.Core
{
    /// <summary>
    /// Models a procedure in an external API, whose signature is known, but 
    /// whose code is irrelevant to the decompilation.
    /// </summary>
	public class ExternalProcedure : ProcedureBase
	{
		public ExternalProcedure(string name, FunctionType signature) : base(name)
		{
			this.Signature = signature;
		}

        public ExternalProcedure(string name, FunctionType signature, ProcedureCharacteristics chars) : base(name)
        {
            this.Signature = signature;
            this.Characteristics = chars;
        }

		public override FunctionType Signature { get; set; }

		public override string ToString()
		{
			StringWriter sw = new StringWriter();
            TextFormatter fmt = new TextFormatter(sw);
            CodeFormatter cf = new CodeFormatter(fmt);
            TypeFormatter tf = new TypeFormatter(fmt);
			Signature.Emit(Name, FunctionType.EmitFlags.ArgumentKind, fmt, cf, tf);
			return sw.ToString();
		}
	}
}
