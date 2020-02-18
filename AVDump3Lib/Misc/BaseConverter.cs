// Copyright (C) 2009 DvdKhl
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Collections;
using System.Diagnostics;

namespace AVDump3Lib.Misc {
	[Flags]
	public enum BaseOption {
		LowerCase = 1 << 29,
		LittleEndian = 1 << 30,
		Pad = 1 << 31,

		Heximal = 16 | Pad,
		Base32 = 32 | Pad,
	}

	public static class BaseConverter {
		public const string base2 = "01";
		public const string base4 = "0123";
		public const string base8 = "01234567";
		public const string base10 = "0123456789";
		public const string base16 = "0123456789ABCDEF";
		public const string base32Hex = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
		public const string base32Z = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
		public const string base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
		public const string base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public const string base62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public const string base64 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";

		public static string ToString(byte[] value, BaseOption baseOption = BaseOption.Heximal) {
			int radix = (int)baseOption & ((1 << 29) - 1);

			if((baseOption & BaseOption.LittleEndian) != 0) value = value.Reverse().ToArray();

			string ret;
			if(radix == 16) {
				ret = ToBase16String(value);
			} else if(radix == 32) {
				ret = ToBase32String(value, base32);
			} else {
				throw new Exception("Invalid Radix");
			}

			if((baseOption & BaseOption.Pad) != 0) ret = ret.PadLeft((int)Math.Ceiling((value.Length << 3) * Math.Log(2, radix)), '0');

			return ret.PadLeft((int)Math.Ceiling((value.Length << 3) * Math.Log(2, radix)), '0');
		}


		public static string ToBase32String(byte[] inArray, string symbols) {
			if(inArray == null) return null;

			int len = inArray.Length;
			// divide the input into 40-bit groups, so let's see, 
			// how many groups of 5 bytes can we get out of it?
			int numberOfGroups = len / 5;
			// and how many remaining bytes are there?
			int numberOfRemainingBytes = len - 5 * numberOfGroups;

			// after this, we're gonna split it into eight 5 bit
			// values. 
			StringBuilder sb = new StringBuilder();
			//int resultLen = 4*((len + 2)/3);
			//StringBuffer result = new StringBuffer(resultLen);

			// Translate all full groups from byte array elements to Base64
			int byteIndexer = 0;
			for(int i = 0;i < numberOfGroups;i++) {
				byte b0 = inArray[byteIndexer++];
				byte b1 = inArray[byteIndexer++];
				byte b2 = inArray[byteIndexer++];
				byte b3 = inArray[byteIndexer++];
				byte b4 = inArray[byteIndexer++];

				// first 5 bits from byte 0
				sb.Append(symbols[b0 >> 3]);
				// the remaining 3, plus 2 from the next one
				sb.Append(symbols[(b0 << 2) & 0x1F | (b1 >> 6)]);
				// get bit 3, 4, 5, 6, 7 from byte 1
				sb.Append(symbols[(b1 >> 1) & 0x1F]);
				// then 1 bit from byte 1, and 4 from byte 2
				sb.Append(symbols[(b1 << 4) & 0x1F | (b2 >> 4)]);
				// 4 bits from byte 2, 1 from byte3
				sb.Append(symbols[(b2 << 1) & 0x1F | (b3 >> 7)]);
				// get bit 2, 3, 4, 5, 6 from byte 3
				sb.Append(symbols[(b3 >> 2) & 0x1F]);
				// 2 last bits from byte 3, 3 from byte 4
				sb.Append(symbols[(b3 << 3) & 0x1F | (b4 >> 5)]);
				// the last 5 bits
				sb.Append(symbols[b4 & 0x1F]);
			}

			// Now, is there any remaining bytes?
			if(numberOfRemainingBytes > 0) {
				byte b0 = inArray[byteIndexer++];
				// as usual, get the first 5 bits
				sb.Append(symbols[b0 >> 3]);
				// now let's see, depending on the 
				// number of remaining bytes, we do different
				// things
				switch(numberOfRemainingBytes) {
					case 1:
						// use the remaining 3 bits, padded with five 0 bits
						sb.Append(symbols[(b0 << 2) & 0x1F]);
						//						sb.Append("======");
						break;
					case 2:
						byte b1 = inArray[byteIndexer++];
						sb.Append(symbols[(b0 << 2) & 0x1F | (b1 >> 6)]);
						sb.Append(symbols[(b1 >> 1) & 0x1F]);
						sb.Append(symbols[(b1 << 4) & 0x1F]);
						//						sb.Append("====");
						break;
					case 3:
						b1 = inArray[byteIndexer++];
						byte b2 = inArray[byteIndexer++];
						sb.Append(symbols[(b0 << 2) & 0x1F | (b1 >> 6)]);
						sb.Append(symbols[(b1 >> 1) & 0x1F]);
						sb.Append(symbols[(b1 << 4) & 0x1F | (b2 >> 4)]);
						sb.Append(symbols[(b2 << 1) & 0x1F]);
						//						sb.Append("===");
						break;
					case 4:
						b1 = inArray[byteIndexer++];
						b2 = inArray[byteIndexer++];
						byte b3 = inArray[byteIndexer++];
						sb.Append(symbols[(b0 << 2) & 0x1F | (b1 >> 6)]);
						sb.Append(symbols[(b1 >> 1) & 0x1F]);
						sb.Append(symbols[(b1 << 4) & 0x1F | (b2 >> 4)]);
						sb.Append(symbols[(b2 << 1) & 0x1F | (b3 >> 7)]);
						sb.Append(symbols[(b3 >> 2) & 0x1F]);
						sb.Append(symbols[(b3 << 3) & 0x1F]);
						//						sb.Append("=");
						break;
				}
			}
			return sb.ToString();
		}
		private static string ToBase16String(byte[] value) { return value.Aggregate<byte, string>("", (acc, b) => acc + b.ToString("X2")); }
	}
}
