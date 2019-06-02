using AVDump3Lib.Processing.BlockBuffers;
using BXmlLib;
using BXmlLib.DataSource;
using BXmlLib.DocTypes.Matroska;
using System;
using System.Threading;

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