using AVDump3Lib.Processing.BlockBuffers;
using BXmlLib.DocTypes.Ebml;
using System;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
	public class AVDEbmlBlockDataSource : EbmlBlockDataSource {
		private readonly IBlockStreamReader reader;
		private int offset;

		public AVDEbmlBlockDataSource(IBlockStreamReader reader) => this.reader = reader;

		public override long Position {
			get => reader.BytesRead + offset;
			set => Advance(checked((int)(value - reader.BytesRead)));
		}

		public override void CommitPosition() {
			reader.Advance(offset);
			offset = 0;
		}

		protected override void Advance(int length) {
			offset += length;
			if(offset >= reader.SuggestedReadLength) CommitPosition();
			IsEndOfStream = reader.BytesRead + offset >= reader.Length;
		}

		protected override ReadOnlySpan<byte> GetDataBlock(int minLength) => reader.GetBlock(minLength + offset)[offset..];
	}
}
