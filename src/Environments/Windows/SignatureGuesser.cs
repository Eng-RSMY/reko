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
using Reko.Core.Serialization;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Reko.Environments.Windows
{
    public class SignatureGuesser
    {
        public static Tuple<string, DataType, SerializedType> InferTypeFromName(string fnName, TypeLibraryDeserializer loader, IPlatform platform)
        {
            if (fnName[0] == '?')
            {
                // Microsoft-mangled signatures begin with '?'
                var pmnp = new MsMangledNameParser(fnName);
                Tuple<string, SerializedType, SerializedType> field = null;
                try
                {
                    field = pmnp.Parse();
                }
                catch (Exception ex)
                {
                    Debug.Print("*** Error parsing {0}. {1}", fnName, ex.Message);
                    return null;
                }
                var sproc = field.Item2 as SerializedSignature;
                if (sproc != null)
                {
                    var sser = platform.CreateProcedureSerializer(loader, sproc.Convention);
                    var sig = sser.Deserialize(sproc, platform.Architecture.CreateFrame());    //$BUGBUG: catch dupes?
                    return new Tuple<string, DataType, SerializedType>(
                        field.Item1,
                        sig,
                        field.Item3);
                }
                else
                {
                    var dt = (field.Item2 == null) ?
                        new UnknownType() :
                        field.Item2.Accept(loader);
                    return Tuple.Create(field.Item1, dt, field.Item3);
                }
            }
            return null;
        }

        /// <summary>
        /// Guesses the signature of a procedure based on its name.
        /// </summary>
        /// <param name="fnName"></param>
        /// <param name="loader"></param>
        /// <param name="arch"></param>
        /// <returns></returns>
        public static ExternalProcedure SignatureFromName(string fnName, TypeLibraryDeserializer loader, IPlatform platform)
        {
            int argBytes;
            if (fnName[0] == '_')
            {
                // Win32 prefixes cdecl and stdcall functions with '_'. Stdcalls will have @<nn> 
                // where <nn> is the number of bytes pushed on the stack. If 0 bytes are pushed
                // the result is indistinguishable from the corresponding cdecl call, which is OK.
                int lastAt = fnName.LastIndexOf('@');
                if (lastAt < 0)
                    return CdeclSignature(fnName.Substring(1), platform.Architecture);
                string name = fnName.Substring(1, lastAt - 1);
                if (!Int32.TryParse(fnName.Substring(lastAt + 1), out argBytes))
                    return CdeclSignature(name, platform.Architecture);
                else
                    return StdcallSignature(name, argBytes, platform.Architecture);
            }
            else if (fnName[0] == '@')
            {
                // Win32 prefixes fastcall functions with '@'.
                int lastAt = fnName.LastIndexOf('@');
                if (lastAt <= 0)
                    return CdeclSignature(fnName.Substring(1), platform.Architecture);
                string name = fnName.Substring(1, lastAt - 1);
                if (!Int32.TryParse(fnName.Substring(lastAt + 1), out argBytes))
                    return CdeclSignature(name, platform.Architecture);
                else
                    return FastcallSignature(name, argBytes, platform.Architecture);
            }
            else if (fnName[0] == '?')
            {
                // Microsoft-mangled signatures begin with '?'
                var pmnp = new MsMangledNameParser(fnName);
                Tuple<string,SerializedType,SerializedType> field = null;
                try
                {
                    field = pmnp.Parse();
                }
                catch (Exception ex)
                {
                    Debug.Print("*** Error parsing {0}. {1}", fnName, ex.Message);
                    return null;
                }
                var sproc = field.Item2 as SerializedSignature;
                if (sproc != null)
                {
                    var sser = platform.CreateProcedureSerializer(loader, sproc.Convention);
                    var sig = sser.Deserialize(sproc, platform.Architecture.CreateFrame());    //$BUGBUG: catch dupes?
                    return new ExternalProcedure(field.Item1, sig)
                    {
                        EnclosingType = sproc.EnclosingType
                    };
                }
            }
            return null;
        }

        private static ExternalProcedure CdeclSignature(string name, IProcessorArchitecture arch)
        {
            return new ExternalProcedure(name, new FunctionType
            {
                ReturnAddressOnStack = arch.PointerType.Size,
            });
        }

        private static ExternalProcedure StdcallSignature(string name, int argBytes, IProcessorArchitecture arch)
        {
            return new ExternalProcedure(name, new FunctionType
            {
                ReturnAddressOnStack = arch.PointerType.Size,
                StackDelta = argBytes + arch.PointerType.Size,
            });
        }

        private static ExternalProcedure FastcallSignature(string name, int argBytes, IProcessorArchitecture arch)
        {
            return new ExternalProcedure(name, new FunctionType
            {
                ReturnAddressOnStack = arch.PointerType.Size,
                StackDelta = argBytes - 2 * arch.PointerType.Size, // ecx, edx
            });
        }
    }
}
