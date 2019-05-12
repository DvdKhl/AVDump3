using System;
using System.IO;
using System.Threading;
using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.BlockConsumers.Matroska.EbmlHeader;
using AVDump3Lib.Processing.StreamConsumer;
using BXmlLib;
using BXmlLib.DataSource;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska {
	public class MatroskaParser : BlockConsumer {
		public MatroskaFile Info { get; private set; }

		public MatroskaParser(string name, IBlockStreamReader reader) : base(name, reader) { }


		protected override void DoWork(CancellationToken ct) {
			IBXmlDataSource dataSrc = new AVDEbmlBlockDataSource(Reader);
			using(var cts = new CancellationTokenSource())
			using(ct.Register(() => cts.Cancel())) {
				var matroskaDocType = new MatroskaDocType(); //(MatroskaVersion.V3);
				var bxmlReader = new BXmlReader(dataSrc, matroskaDocType);

				//var updateTask = Task.Factory.StartNew(() => {
				//	long oldProcessedBytes = 0;
				//	int timerRes = 40, ttl = 10000, ticks = ttl / timerRes;
				//	while(IsRunning) {
				//		ProcessedBytes = dataSrc.Position; //TODO: Check for dispose
				//
				//		Thread.Sleep(timerRes); ticks--;
				//		if(oldProcessedBytes != ProcessedBytes) ticks = ttl / timerRes; else if(ticks == 0) cts.Cancel();
				//		oldProcessedBytes = ProcessedBytes;
				//	}
				//}, ct, TaskCreationOptions.LongRunning, TaskScheduler.Current);


				var matroskaFile = new MatroskaFile(Reader.Length);

				try {
					matroskaFile.Parse(bxmlReader, cts.Token);

					if(matroskaFile.Segment != null) {
						Info = matroskaFile;
					}
				} catch(Exception ex) {
					//TODO: Only ignore excpetion when it is not a matroska file
				}

			}
		}
	}
}