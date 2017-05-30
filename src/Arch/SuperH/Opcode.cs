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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reko.Arch.SuperH
{
    public enum Opcode
    {
        invalid,
        add,
        addc,
        addv,
        and,
        and_b,
        bf,
        bf_s,
        bra,
        braf,
        brk,
        bsr,
        bsrf,
        bt,
        bt_s,
        clrmac,
        clrs,
        clrt,
        cmp_eq,
        cmp_ge,
        cmp_gt,
        cmp_hi,
        cmp_hs,
        cmp_pl,
        cmp_pz,
        cmp_str,
        div0s,
        div0u,
        div1,
        dmuls_l,
        dmulu_l,
        dt,
        exts_b,
        exts_w,
        extu_b,
        extu_w,
        fabs,
        fadd,
        fcmp_eq,
        fcmp_gt,
        fcnvds,
        fcnvsd,
        fdiv,
        fipr,
        flds,
        fldi0,
        fldi1,
        jmp,
        jsr,
        lds_l,
        mov,
        mov_b,
        mov_w,
        mov_l,
        mova,
        movt,
        mul_l,
        neg,
        nop,
        not,
        or,
        rts,
        sts_l,
        sub,
        tst,
        xor,
        shll2,
        shlr,
        shll8,
        shlr8,
        shll,
    }
}
