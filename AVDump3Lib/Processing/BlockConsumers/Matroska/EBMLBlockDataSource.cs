using AVDump3Lib.BlockBuffers;
using CSEBML.DataSource;
using System;

namespace AVDump3Lib.BlockConsumers.Matroska {
    public class EBMLBlockDataSource : IEBMLDataSource {
		IBlockStreamReader reader;

		//private Int64 absolutePosition;
		private long relativePosition;
		private long length;

		public EBMLBlockDataSource(IBlockStreamReader reader) {
            this.reader = reader;
            length = reader.Length;
		}

		private void Advance() {
			relativePosition = 0;
			reader.Advance();
		}

		public bool CanSeek { get { return false; } }
		public long Length { get { return length; } }
		public long Position {
			get { return reader.ReadBytes + relativePosition; }
			set {
				if(value > Length) throw new Exception("Cannot set position greater than the filelength");
				long bytesToSkip = value - (reader.ReadBytes + relativePosition);
				if(bytesToSkip < 0) throw new InvalidOperationException("Cannot seek backwards");

				int blockLength;
				reader.GetBlock(out blockLength);

				long bytesSkipped = Math.Min(blockLength - relativePosition, bytesToSkip);
				relativePosition += bytesSkipped;
				bytesToSkip -= bytesSkipped;

				while(bytesToSkip != 0) {
					reader.GetBlock(out blockLength);
					bytesSkipped = blockLength - relativePosition;

					if(bytesToSkip >= bytesSkipped) {
						bytesToSkip -= bytesSkipped;
						Advance();

					} else {
						relativePosition += bytesToSkip;
						bytesToSkip = 0;
					}
				}
			}
		}

		public byte[] GetData(long neededBytes, out long offset) {
			int blockLength;
			byte[] block = reader.GetBlock(out blockLength);
			if(blockLength - relativePosition > neededBytes) {
				offset = relativePosition;
				relativePosition += neededBytes;

			} else if(blockLength - relativePosition == neededBytes) {
				offset = relativePosition;
				relativePosition += neededBytes;
				Advance();

			} else {
				if(reader.ReadBytes + relativePosition + neededBytes > length) { //Requesting more than available
					Position = length;
					offset = 0;
					return null;
				}

				int bytesCopied = 0;
				byte[] b = new byte[neededBytes];

				bytesCopied = blockLength - (int)relativePosition;
				Buffer.BlockCopy(block, (int)relativePosition, b, 0, bytesCopied);

				Advance();
				block = reader.GetBlock(out blockLength);
				while(bytesCopied + blockLength <= neededBytes) {
					Buffer.BlockCopy(block, 0, b, bytesCopied, blockLength);
					bytesCopied += blockLength;

					Advance();
					block = reader.GetBlock(out blockLength);
				}


				Buffer.BlockCopy(block, 0, b, bytesCopied, (int)neededBytes - bytesCopied);
				relativePosition = neededBytes - bytesCopied;

				offset = 0;
				block = b;
			}

			return block;
		}

		public void SyncTo(BytePatterns bytePatterns, long seekUntil) {
			bytePatterns.Reset();
			int foundRelativePosition = -1;
			while(foundRelativePosition < 0 && !EOF) {
				int blockLength;
				var block = reader.GetBlock(out blockLength);

				bytePatterns.Match(block, (int)relativePosition, (pattern, i) => { foundRelativePosition = i; return false; });
				if(foundRelativePosition < 0) {
					bytePatterns.Reset();
					if(seekUntil == -1 || reader.ReadBytes + blockLength <= seekUntil) Advance();
					else break;
				}
			}

			if(seekUntil != -1 && reader.ReadBytes + foundRelativePosition > seekUntil) foundRelativePosition = (int)(seekUntil - reader.ReadBytes);
			if(foundRelativePosition >= 0) relativePosition = foundRelativePosition;
		}

		public int ReadIdentifier() {
			int bytesToRead = 0;
			byte mask = 1 << 7;
			int blockLength;
			var block = reader.GetBlock(out blockLength);
			byte encodedSize = block[relativePosition++];

			while((mask & encodedSize) == 0 && ++bytesToRead < 4) mask = (byte)(mask >> 1);
			if(bytesToRead == 4) return ~VIntConsts.INVALID_LENGTH_DESCRIPTOR_ERROR; //Identifiers are Int32

			int value = 0;
			for(int i = 0; i < bytesToRead; i++) {
				if(relativePosition == blockLength) {
					if(reader.ReadBytes + relativePosition + bytesToRead > length) return ~VIntConsts.BASESOURCE_ERROR; //Unexpected EOF

					Advance();
					block = reader.GetBlock(out blockLength);
				}
				value += block[relativePosition++] << ((bytesToRead - i - 1) << 3);
			}
			if(relativePosition == blockLength) Advance();

			value += (encodedSize << (bytesToRead << 3));

			return value == VIntConsts.RESERVEDVINTS[bytesToRead] ? ~VIntConsts.RESERVED : value;
		}

		public long ReadVInt() {
			int bytesToRead = 0;
			byte mask = 1 << 7;
			int blockLength;
			var block = reader.GetBlock(out blockLength);
			byte encodedSize = block[relativePosition++];

			while((mask & encodedSize) == 0 && ++bytesToRead < 8) { mask = (byte)(mask >> 1); }
			if(bytesToRead == 8) return ~VIntConsts.INVALID_LENGTH_DESCRIPTOR_ERROR; //Identifiers are Int64

			long value = 0;
			for(int i = 0; i < bytesToRead; i++) {
				if(relativePosition == blockLength) {
					if(reader.ReadBytes + relativePosition + bytesToRead > length) return ~VIntConsts.BASESOURCE_ERROR; //Unexpected EOF
					Advance();
					block = reader.GetBlock(out blockLength);
				}
				value += (long)block[relativePosition++] << ((bytesToRead - i - 1) << 3);
			}
			value += (encodedSize ^ mask) << (bytesToRead << 3);

			if(relativePosition == blockLength) Advance();

			return value == VIntConsts.RESERVEDVINTS[bytesToRead] ? ~VIntConsts.UNKNOWN_LENGTH : value;
		}

		public bool HasKnownLength { get { return true; } }

		public bool EOF { get { return Length == Position; } }

		public void WriteIdentifier(int id) { throw new NotSupportedException(); }
		public void WriteVInt(long value, int vIntLength) { throw new NotSupportedException(); }
		public void WriteFakeVInt(int vIntLength) { throw new NotSupportedException(); }
		public void Write(byte[] b, int offset, int length) { throw new NotSupportedException(); }
		public long Write(System.IO.Stream source) { throw new NotSupportedException(); }
	}
}
