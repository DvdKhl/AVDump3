using AVDump3Lib.Processing.BlockBuffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace AVDump3Lib.Processing.BlockConsumers.Ogg {
	public class OggParser : BlockConsumer {

		public OggFile Info { get; private set; }

		public OggParser(string name, IBlockStreamReader reader) : base(name, reader) { }


		protected override void DoWork(CancellationToken ct) {
			var info = new OggFile();

			var page = new OggPage();
			var stream = new OggBlockDataSource(Reader);

			if(!stream.SeekPastSyncBytes(false)) return;


			while(stream.ReadOggPage(ref page)) {
				info.ProcessOggPage(ref page);
			}

			Info = info;
		}
	}
}
