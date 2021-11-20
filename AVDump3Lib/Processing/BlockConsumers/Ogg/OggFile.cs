using AVDump3Lib.Processing.BlockConsumers.Ogg.BitStreams;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg {
	public class OggFile {
		public long FileSize { get; private set; }
		public long Overhead { get; private set; }
		public IEnumerable<OGGBitStream> Bitstreams => bitStreams.Values;

		private readonly Dictionary<uint, OGGBitStream> bitStreams = new();

		public void ProcessOggPage(ref OggPage page) {
			Overhead += 27 + page.SegmentCount;

			if(bitStreams.TryGetValue(page.StreamId, out var bitStream)) {
				bitStream.ProcessPage(ref page);

			} else if(page.Flags.HasFlag(PageFlags.Header)) {
				bitStream = OGGBitStream.ProcessBeginPage(ref page);
				bitStreams.Add(bitStream.Id, bitStream);

			} else {
				Overhead += page.Data.Length;
			}
		}
	}
}
