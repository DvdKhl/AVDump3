using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump2Lib.FormatHeaders {
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
			var header = new VMpeg4IsoAvcHeader();

			header.ConfigurationVersion = b[0];
			header.Profile = b[1];
			header.ProfileCompatibility = b[2];
			header.Level = b[3];
			header.ReservedC = (byte)((b[4] & 0xFC) >> 2);
			header.NALULengthSizeMinus1 = (byte)(b[4] & 0x3);
			header.ReservedD = (byte)((b[5] & 0xE0) >> 5);

			int pos = 6;
			byte count = (byte)(b[5] & 0x1F);
			header.SequenceParameterSets = GetSet(b, count, ref pos); count = b[pos++];
			header.PictureParameterSets = GetSet(b, count, ref pos);

			return header;
		}
		private static IEnumerable<string> GetSet(byte[] b, byte count, ref int pos) {
			Int16 size;

			var sets = new string[count];
			for(int i = 0;i < count;i++) {
				size = (Int16)((b[pos] << 8) + b[pos + 1]); pos += 2;
				sets[i] = System.Text.Encoding.ASCII.GetString(b, pos, size);
				pos += size;
			}

			return sets;
		}
	}
}
