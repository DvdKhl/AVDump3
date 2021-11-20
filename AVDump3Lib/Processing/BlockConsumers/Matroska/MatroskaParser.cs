using AVDump3Lib.Processing.BlockBuffers;
using BXmlLib;
using BXmlLib.DataSource;
using BXmlLib.DocTypes.Matroska;
using System.Buffers.Binary;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska;

public class MatroskaParser : BlockConsumer {
	public MatroskaFile Info { get; private set; }

	public MatroskaParser(string name, IBlockStreamReader reader) : base(name, reader) { }

	public override bool IsConsuming => base.IsConsuming && hasValidHeader;

	private bool hasValidHeader = false;

	protected override void DoWork(CancellationToken ct) {
		if(Reader.Length < 4 || BinaryPrimitives.ReadUInt32BigEndian(Reader.GetBlock(4)) != 0x1A45DFA3U) {
			return;
		}
		hasValidHeader = true;

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
