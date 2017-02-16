﻿using Reko.Core.Expressions;
using Reko.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reko.Core
{
	public abstract class EndianImageReader : ImageReader
	{
		/// <summary>
		/// If nothing else is specified, this is the address at which the image will be loaded.
		/// </summary>
		public virtual Address PreferredBaseAddress { get; set; }

		protected EndianImageReader(MemoryArea img, Address addr) : base(img, addr) { }
		protected EndianImageReader(MemoryArea img, Address addrBegin, Address addrEnd) : base(img, addrBegin, addrEnd) { }
		protected EndianImageReader(MemoryArea img, ulong off) : base(img, off) { }
		protected EndianImageReader(byte[] img, ulong off) : base(img, off) { }
		protected EndianImageReader(byte[] img) : this(img, 0) { }

		public abstract EndianImageReader CreateNew(byte[] bytes, ulong offset);
		public abstract EndianImageReader CreateNew(MemoryArea image, Address addr);

		public virtual EndianImageReader Clone()
		{
			EndianImageReader rdr;
			if (image != null)
			{
				rdr = CreateNew(image, addrStart);
				rdr.off = off;
			}
			else
			{
				rdr = CreateNew(bytes, (ulong)off);
			}
			return rdr;
		}


		/// <summary>
		/// </summary>
		/// <param name="charType"></param>
		/// <returns></returns>
		public bool ReadNullCharTerminator(DataType charType)
		{
			switch (charType.Size)
			{
			case 1: return (char)ReadByte() == 0;
			case 2: return (char)ReadUInt16() == 0;
			default: throw new NotSupportedException(string.Format("Character size {0} not supported.", charType.Size));
			}
		}

		/// <summary>
		/// Reads a NUL-terminated string starting at the current position.
		/// </summary>
		/// <param name="charType"></param>
		/// <returns></returns>
		public StringConstant ReadCString(DataType charType, Encoding encoding)
		{
			int iStart = (int)Offset;
			while (IsValid && !ReadNullCharTerminator(charType))
				;
			return new StringConstant(
				StringType.NullTerminated(charType),
				encoding.GetString(
					bytes,
					iStart,
					(int)Offset - iStart - 1));
		}

		/// <summary>
		/// Read a character string that is preceded by a character count. 
		/// </summary>
		/// <param name="lengthType"></param>
		/// <param name="charType"></param>
		/// <param name="encoding"></param>
		/// <returns></returns>
		public StringConstant ReadLengthPrefixedString(PrimitiveType lengthType, PrimitiveType charType, Encoding encoding)
		{
			int length = Read(lengthType).ToInt32();
			int iStart = (int)Offset;
			return new StringConstant(
				StringType.LengthPrefixedStringType(charType, lengthType),
				encoding.GetString(
					bytes,
					iStart,
					length * charType.Size));
		}

		public abstract short ReadInt16();
		public abstract int ReadInt32();
		public abstract bool TryReadInt32(out int i32);
		public abstract long ReadInt64();
		public abstract bool TryReadInt64(out long value);

		public abstract ushort ReadUInt16();
		public abstract bool TryReadUInt16(out ushort ui16);
		public abstract uint ReadUInt32();
		public abstract bool TryReadUInt32(out uint ui32);
		public abstract ulong ReadUInt64();
		public abstract bool TryReadUInt64(out ulong ui64);

		public abstract short ReadInt16(uint offset);
		public abstract int ReadInt32(uint offset);
		public abstract long ReadInt64(uint offset);

		public abstract ushort ReadUInt16(uint offset);
		public abstract uint ReadUInt32(uint offset);
		public abstract ulong ReadUInt64(uint offset);

		public abstract Constant Read(PrimitiveType dataType);
		public abstract bool TryRead(PrimitiveType dataType, out Constant c);

		public abstract bool TryPeekUInt32(int offset, out uint value);
	}

	/// <summary>
	/// Use this reader when the processor is in Little-Endian mode to read multi-
	/// byte quantities from memory.
	/// </summary>
	public class LeImageReader : EndianImageReader
	{
		public LeImageReader(byte[] bytes, ulong offset = 0) : base(bytes, offset) { }
		public LeImageReader(MemoryArea image, ulong offset) : base(image, offset) { }
		public LeImageReader(MemoryArea image, Address addr) : base(image, addr) { }
		public LeImageReader(MemoryArea image, Address addrBegin, Address addrEnd) : base(image, addrBegin, addrEnd) { }
		public LeImageReader(byte[] bytes) : this(bytes, 0) { }

		public override EndianImageReader CreateNew(byte[] bytes, ulong offset)
		{
			return new LeImageReader(bytes, offset);
		}

		public override EndianImageReader CreateNew(MemoryArea image, Address addr)
		{
			return new LeImageReader(image, (uint)(addr - image.BaseAddress));
		}

		public override short ReadInt16() { return ReadLeInt16(); }
		public override int ReadInt32() { return ReadLeInt32(); }
		public override long ReadInt64() { return ReadLeInt64(); }
		public override ushort ReadUInt16() { return ReadLeUInt16(); }
		public override uint ReadUInt32() { return ReadLeUInt32(); }
		public override ulong ReadUInt64() { return ReadLeUInt64(); }
		public override bool TryPeekUInt32(int offset, out uint value) { return TryPeekLeUInt32(offset, out value); }

		public override bool TryReadInt32(out int i32) { return TryReadLeInt32(out i32); }
		public override bool TryReadInt64(out long value) { return TryReadLeInt64(out value); }
		public override bool TryReadUInt16(out ushort value) { return TryReadLeUInt16(out value); }
		public override bool TryReadUInt32(out uint ui32) { return TryReadLeUInt32(out ui32); }
		public override bool TryReadUInt64(out ulong ui64) { return TryReadLeUInt64(out ui64); }

		public override short ReadInt16(uint offset) { return PeekLeInt16(offset); }
		public override int ReadInt32(uint offset) { return PeekLeInt32(offset); }
		public override long ReadInt64(uint offset) { return PeekLeInt64(offset); }
		public override ushort ReadUInt16(uint offset) { return PeekLeUInt16(offset); }
		public override uint ReadUInt32(uint offset) { return PeekLeUInt32(offset); }
		public override ulong ReadUInt64(uint offset) { return PeekLeUInt64(offset); }

		public override Constant Read(PrimitiveType dataType) { return ReadLe(dataType); }
		public override bool TryRead(PrimitiveType dataType, out Constant c) { return TryReadLe(dataType, out c); }

	}

	/// <summary>
	/// Use this reader when the processor is in Big-Endian mode to read multi-
	/// byte quantities from memory.
	/// </summary>
	public class BeImageReader : EndianImageReader
	{
		public BeImageReader(byte[] bytes, ulong offset) : base(bytes, offset) { }
		public BeImageReader(MemoryArea image, ulong offset) : base(image, offset) { }
		public BeImageReader(MemoryArea image, Address addr) : base(image, addr) { }
		public BeImageReader(MemoryArea image, Address addrBegin, Address addrEnd) : base(image, addrBegin, addrEnd) { }
		public BeImageReader(byte[] bytes) : this(bytes, 0) { }

		public override EndianImageReader CreateNew(byte[] bytes, ulong offset)
		{
			return new BeImageReader(bytes, offset);
		}

		public override EndianImageReader CreateNew(MemoryArea image, Address addr)
		{
			return new BeImageReader(image, (uint)(addr - image.BaseAddress));
		}

		public override short ReadInt16() { return ReadBeInt16(); }
		public override int ReadInt32() { return ReadBeInt32(); }
		public override long ReadInt64() { return ReadBeInt64(); }
		public override ushort ReadUInt16() { return ReadBeUInt16(); }
		public override uint ReadUInt32() { return ReadBeUInt32(); }
		public override ulong ReadUInt64() { return ReadBeUInt64(); }
		public override bool TryPeekUInt32(int offset, out uint value) { return TryPeekBeUInt32(offset, out value); }
		public override bool TryReadInt32(out int i32) { return TryReadBeInt32(out i32); }
		public override bool TryReadInt64(out long value) { return TryReadBeInt64(out value); }
		public override bool TryReadUInt16(out ushort ui16) { return TryReadBeUInt16(out ui16); }
		public override bool TryReadUInt32(out uint ui32) { return TryReadBeUInt32(out ui32); }
		public override bool TryReadUInt64(out ulong ui64) { return TryReadBeUInt64(out ui64); }

		public override short ReadInt16(uint offset) { return PeekBeInt16(offset); }
		public override int ReadInt32(uint offset) { return PeekBeInt32(offset); }
		public override long ReadInt64(uint offset) { return PeekBeInt64(offset); }
		public override ushort ReadUInt16(uint offset) { return PeekBeUInt16(offset); }
		public override uint ReadUInt32(uint offset) { return PeekBeUInt32(offset); }
		public override ulong ReadUInt64(uint offset) { return PeekBeUInt64(offset); }

		public override Constant Read(PrimitiveType dataType) { return ReadBe(dataType); }
		public override bool TryRead(PrimitiveType dataType, out Constant c) { return TryReadBe(dataType, out c); }
	}
}
