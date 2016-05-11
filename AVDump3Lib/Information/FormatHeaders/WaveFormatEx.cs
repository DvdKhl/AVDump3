using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AVDump2Lib.FormatHeaders {
	public class WaveFormatEx {
		public const int LENGTH = 18;

		public ushort FormatTag { get; private set; }
		public ushort Channels { get; private set; }
		public uint SamplesPerSecond { get; private set; }
		public uint AverageBytesPerSecond { get; private set; }
		public ushort BlockAlign { get; private set; }
		public ushort BitsPerSample { get; private set; }
		public ushort Size { get; private set; }

		public string TwoCC { get { return Convert.ToString(FormatTag, 16); } }

		public WaveFormatEx(byte[] b) {
			var pos = 0;

			FormatTag = BitConverter.ToUInt16(b, pos); pos += 2;
			Channels = BitConverter.ToUInt16(b, pos); pos += 2;
			SamplesPerSecond = BitConverter.ToUInt32(b, pos); pos += 4;
			AverageBytesPerSecond = BitConverter.ToUInt32(b, pos); pos += 4;
			BlockAlign = BitConverter.ToUInt16(b, pos); pos += 2;
			BitsPerSample = BitConverter.ToUInt16(b, pos); pos += 2;
			Size = BitConverter.ToUInt16(b, pos); pos += 2;

		}
	}
}
