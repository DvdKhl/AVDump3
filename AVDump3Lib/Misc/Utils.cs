using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Misc {
	public static class Utils {
		public static bool UsingWindows { get; } = Environment.OSVersion.Platform == PlatformID.Win32NT;

		public static string ToBase32(byte[] input) {
			if(input == null || input.Length == 0) {
				throw new ArgumentNullException("input");
			}

			int charCount = (int)Math.Ceiling(input.Length / 5d) * 8;
			char[] returnArray = new char[charCount];

			byte nextChar = 0, bitsRemaining = 5;
			int arrayIndex = 0;

			foreach(byte b in input) {
				nextChar = (byte)(nextChar | (b >> (8 - bitsRemaining)));
				returnArray[arrayIndex++] = ToBase32Sub(nextChar);

				if(bitsRemaining < 4) {
					nextChar = (byte)((b >> (3 - bitsRemaining)) & 31);
					returnArray[arrayIndex++] = ToBase32Sub(nextChar);
					bitsRemaining += 5;
				}

				bitsRemaining -= 3;
				nextChar = (byte)((b << bitsRemaining) & 31);
			}

			if(arrayIndex != charCount) {
				returnArray[arrayIndex++] = ToBase32Sub(nextChar);
				while(arrayIndex != charCount) returnArray[arrayIndex++] = '=';
			}

			return new string(returnArray);
		}
		private static char ToBase32Sub(byte b) {
			if(b < 26) return (char)(b + 65);
			if(b < 32) return (char)(b + 24);
			throw new ArgumentException("Byte is not a value Base32 value.", "b");
		}
	}
}
