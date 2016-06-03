using AVDump3Lib.Processing.BlockConsumers.Ogg;
using AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams;
using CSEBML.DataSource;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg {
	public class OggFile {
		public long FileSize { get; private set; }
		public long Overhead { get; private set; }
		public ReadOnlyCollection<OGGBitStream> Bitstreams { get; private set; }

		private Dictionary<uint, OGGBitStream> bitStreams = new Dictionary<uint, OGGBitStream>();


        public void ProcessOggPage(OggPage page) {
            Overhead += 27 + page.SegmentCount;

            OGGBitStream bitStream = null;
            if(bitStreams.TryGetValue(page.StreamId, out bitStream)) {
                bitStream.ProcessPage(page);

            } else if(page.Flags.HasFlag(PageFlags.Header)) {
                bitStream = OGGBitStream.ProcessBeginPage(page);
                bitStreams.Add(bitStream.Id, bitStream);

            } else {
                Overhead += page.DataLength;
            }
        }
    }
}
