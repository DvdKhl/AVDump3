using AVDump3Lib.Processing.BlockBuffers;
using System;
using System.Collections.Generic;
using System.Text;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
    public class EbmlBlockStreamDataSource : BXmlLib.DocTypes.Ebml.EbmlBlockDataSource {
        private IBlockStreamReader Reader { get; }


        public override long Position { get => Reader.BytesRead; set => Advance(checked((int)(value - position))); }

        protected override bool Advance(int length) { position += length; }
        protected override ReadOnlySpan<byte> Data(int minLength) => throw new NotImplementedException();
    }
}
