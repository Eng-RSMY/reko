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

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Reko.Core.Expressions
{
	/// <summary>
	/// Replaces the bits in the specified range with the new expression.
	/// </summary>
    /// <remarks>
    /// The name stems from the PDP-10 DPB ("deposit bits") instruction. It
    /// is used to model the common case when part of a register is replaced
    /// with bits from another source. As an example consider the following
    /// M68k instruction, which loads a byte into the low word of D0:
    /// <code>
    /// move.b D1,D0
    /// </code>
    /// This is modelled by the following assignment:
    /// d0 = DPB(d0, (byte) d1, 0)
    /// </remarks>
	public class DepositBits : Expression
	{
		private Expression src;
		private Expression bits;
		private int bitPos;

		public DepositBits(Expression src, Expression bits, int bitPos) : base(src.DataType)
		{
			this.src = src;
			this.bits = bits;
			this.bitPos = bitPos;
		}

        public override IEnumerable<Expression> Children
        {
            get { yield return Source; yield return InsertedBits ; }
        }

        public override T Accept<T, C>(ExpressionVisitor<T, C> v, C context)
        {
            return v.VisitDepositBits(this, context);
        }

        public override T Accept<T>(ExpressionVisitor<T> v)
        {
            return v.VisitDepositBits(this);
        }

		public override void Accept(IExpressionVisitor visit)
		{
			visit.VisitDepositBits(this);
		}

		public int BitPosition
		{
			get { return bitPos; }
		}

		public override Expression CloneExpression()
		{
			return new DepositBits(src.CloneExpression(), bits.CloneExpression(), bitPos);
		}

		public Expression Source
		{
			get { return src; }
			set { src = value; }
		}

		public Expression InsertedBits
		{
			get { return bits; }
		}
	}
}
