using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.BlockConsumers.MP4 {
	//public class MP4BlockDataSource : EBMLBlockDataSource {
	//	public MP4BlockDataSource(IBlockStreamReader reader) : base(reader) { }

	//	public override int ReadIdentifier() {
	//		if(reader.BytesRead + relativePosition + 4 > Length) return ~CSEBML.DataSource.VIntConsts.BASESOURCE_ERROR; //Unexpected EOF

	//		var value = 0;
	//		int blockLength;
	//		var block = reader.GetBlock(out blockLength);

	//		var bytesNeeded = 4;
	//		for(int i = 0; i < bytesNeeded; i++) {
	//			if(relativePosition == blockLength) {
	//				Advance();
	//				block = reader.GetBlock(out blockLength);
	//			}
	//			value |= block[relativePosition++] << ((bytesNeeded - i - 1) << 3);
	//			if(--bytesNeeded == 0 && value == 1) {
	//				bytesNeeded = 8;
	//				value = 0;
	//				i = 0;
	//			}
	//		}
	//		if(relativePosition == blockLength) Advance();
	//		return value;
	//	}

	//	private const int uuidFlag = ('u' << 24) | ('u' << 16) | ('i' << 8) | ('d' << 0);
	//	public override long ReadVInt() {
	//		if(reader.BytesRead + relativePosition + 4 > Length) return ~CSEBML.DataSource.VIntConsts.BASESOURCE_ERROR; //Unexpected EOF

	//		var value = 0L;
	//		int blockLength;
	//		var block = reader.GetBlock(out blockLength);

	//		var bytesNeeded = 4;
	//		for(int i = 0; i < bytesNeeded; i++) {
	//			if(relativePosition == blockLength) {
	//				Advance();
	//				block = reader.GetBlock(out blockLength);
	//			}
	//			value |= (long)block[relativePosition++] << (((bytesNeeded - i - 1) << 3));
	//			if(--bytesNeeded == 0 && value == uuidFlag) {
	//				bytesNeeded = 16;
	//				value = 0;
	//				i = 0;
	//			}
	//		}
	//		if(relativePosition == blockLength) Advance();
	//		return value;
	//	}

	//}
}
