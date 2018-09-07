using AVDump3Lib.Processing.BlockBuffers;
using BXmlLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.BlockConsumers.MP4 {
	//public class MP4Parser : BlockConsumer {
	//	public MP4Parser(string name, IBlockStreamReader reader) : base(name, reader) { }

	//	protected override void DoWork(CancellationToken ct) {
	//		var dataSrc = new MP4BlockDataSource(Reader);
	//		using(var cts = new CancellationTokenSource())
	//		using(ct.Register(() => cts.Cancel())) {
	//			var matroskaDocType = new MP4DocType();
	//			var reader = new EBMLReader(dataSrc, matroskaDocType);

	//			ElementInfo elem;
	//			while((elem = reader.Next()) != null) {
	//				File.WriteAllText("MP4Test.txt", elem.ToString() + "\n");
	//			}
	//		}
	//	}
	//}
}
