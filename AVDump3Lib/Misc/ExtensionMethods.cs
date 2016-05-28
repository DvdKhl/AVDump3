using System;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AVDump3Lib.Misc {
    public static class ExtensionMethods {
		#region Invariant String<->Type Conversion Extensions
		public static double ToInvDouble(this string s) { return double.Parse(s, CultureInfo.InvariantCulture); }
		public static double ToInvDouble(this string s, double defVal) { double val; if(double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
		public static double ToInvDouble(this string s, NumberStyles style, double defVal) { double val; if(double.TryParse(s, style, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
		public static string ToInvString(this double s) { return s.ToString(CultureInfo.InvariantCulture); }
		public static string ToInvString(this double s, string format) { return s.ToString(format, CultureInfo.InvariantCulture); }

		public static float ToInvFloat(this string s) { return float.Parse(s, CultureInfo.InvariantCulture); }
		public static float ToInvFloat(this string s, float defVal) { float val; if(float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
		public static float ToInvFloat(this string s, NumberStyles style, float defVal) { float val; if(float.TryParse(s, style, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
		public static string ToInvString(this float s) { return s.ToString(CultureInfo.InvariantCulture); }

        public static long ToInvInt64(this string s) { return long.Parse(s, CultureInfo.InvariantCulture); }
        public static long ToInvInt64(this string s, long defVal) { long val; if(long.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
        public static string ToInvString(this long v) { return v.ToString(CultureInfo.InvariantCulture); }

        public static ulong ToInvUInt64(this string s) { return ulong.Parse(s, CultureInfo.InvariantCulture); }
        public static ulong ToInvUInt64(this string s, ulong defVal) { ulong val; if(ulong.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
        public static string ToInvString(this ulong v) { return v.ToString(CultureInfo.InvariantCulture); }

        public static int ToInvInt32(this string s) { return int.Parse(s, CultureInfo.InvariantCulture); }
		public static int ToInvInt32(this string s, int defVal) { int val; if(int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
		public static string ToInvString(this int v) { return v.ToString(CultureInfo.InvariantCulture); }

		public static short ToInvInt16(this string s) { return short.Parse(s, CultureInfo.InvariantCulture); }
		public static short ToInvInt16(this string s, short defVal) { short val; if(short.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val)) return val; else return defVal; }
		public static string ToInvString(this short v) { return v.ToString(CultureInfo.InvariantCulture); }

		public static DateTime ToInvDateTime(this string s) { return DateTime.Parse(s, CultureInfo.InvariantCulture); }
		public static DateTime ToInvDateTime(this string s, DateTime defVal) { DateTime val; if(DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out val)) return val; else return defVal; }
		public static DateTime? ToInvDateTime(this string s, DateTime? defVal) { DateTime val; if(DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out val)) return val; else return defVal; }
		public static string ToInvString(this DateTime v) { return v.ToString(CultureInfo.InvariantCulture); }
        #endregion

        //public static TResult OnNotNullReturn<TResult, TSource>(this TSource? n, Func<TSource, TResult> transform) where TSource : struct {
        //    return n.HasValue ? transform(n.Value) : default(TResult);
        //}
        public static TResult OnNotNullReturn<TResult, TSource>(this TSource n, Func<TSource, TResult> transform) {
			return n != null ? transform(n) : default(TResult);
		}

		public static void OnNotNull<TSource>(this TSource n, Action<TSource> transform) { if(n != null) transform(n); }


		public static string Truncate(this string value, int maxLength) {
			return (value ?? "").Length <= maxLength ? value : value.Substring(0, maxLength);
		}
	}

	public class BitConverterEx {
		public const string Base2 = "01";
		public const string Base4 = "0123";
		public const string Base8 = "01234567";
		public const string Base10 = "0123456789";
		public const string Base16 = "0123456789ABCDEF";
		public const string Base32Hex = "0123456789ABCDEFGHIJKLMNOPQRSTUV";
		public const string Base32Z = "0123456789ABCDEFGHJKMNPQRSTVWXYZ";
		public const string Base32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
		public const string Base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public const string Base62 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
		public const string Base64 = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ+/";

		public static string ToBase32String(byte[] inArray, string symbols = Base32) {
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
		public static string ToBase16String(byte[] value) { return string.Concat(value.Select(b => b.ToString("X2"))); } //TODO: Improve
	}
}
