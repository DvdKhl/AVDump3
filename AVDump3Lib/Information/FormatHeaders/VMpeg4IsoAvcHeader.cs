using System.Collections.Generic;

namespace AVDump3Lib.Information.FormatHeaders {
	public class VMpeg4IsoAvcHeader {
		public byte ConfigurationVersion { get; private set; }
		public byte Profile { get; private set; }
		public byte ProfileCompatibility { get; private set; }
		public byte Level { get; private set; }
		public byte ReservedC { get; private set; }
		public byte NALULengthSizeMinus1 { get; private set; }
		public byte ReservedD { get; private set; }
		public IEnumerable<string> SequenceParameterSets { get; private set; }
		public IEnumerable<string> PictureParameterSets { get; private set; }

		public static VMpeg4IsoAvcHeader Create(byte[] b) {
			var pos = 6;
			var count = (byte)(b[5] & 0x1F);

			var header = new VMpeg4IsoAvcHeader {
				ConfigurationVersion = b[0],
				Profile = b[1],
				ProfileCompatibility = b[2],
				Level = b[3],
				ReservedC = (byte)((b[4] & 0xFC) >> 2),
				NALULengthSizeMinus1 = (byte)(b[4] & 0x3),
				ReservedD = (byte)((b[5] & 0xE0) >> 5)
			};
			header.SequenceParameterSets = GetSet(b, count, ref pos); count = b[pos++];
			header.PictureParameterSets = GetSet(b, count, ref pos);


			return header;
		}
		private static IEnumerable<string> GetSet(byte[] b, byte count, ref int pos) {
			short size;

			var sets = new string[count];
			for(var i = 0; i < count; i++) {
				size = (short)((b[pos] << 8) + b[pos + 1]); pos += 2;
				sets[i] = System.Text.Encoding.ASCII.GetString(b, pos, size);
				pos += size;
			}

			return sets;
		}
	}
}
