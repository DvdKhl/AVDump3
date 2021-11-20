using AVDump3Lib.Processing.BlockBuffers;
using BXmlLib;
using BXmlLib.DataSource;
using BXmlLib.DocTypes.MP4;
using System;
using System.Threading;

namespace AVDump3Lib.Processing.BlockConsumers.MP4;

public class MP4Parser : BlockConsumer {
	public MP4Node RootBox { get; private set; }

	public MP4Parser(string name, IBlockStreamReader reader) : base(name, reader) { }

	protected override void DoWork(CancellationToken ct) {
		IBXmlDataSource dataSrc = new AVDMP4BlockDataSource(Reader);
		using(var cts = new CancellationTokenSource())
		using(ct.Register(() => cts.Cancel())) {
			var matroskaDocType = new MP4DocType(); //(MatroskaVersion.V3);
			var bxmlReader = new BXmlReader(dataSrc, matroskaDocType);

			try {
				RootBox = MP4Node.Read(bxmlReader, Reader.Length);

			} catch(Exception ex) {
				//TODO: Only ignore excpetion when it is not a matroska file
			}

		}
	}
}
