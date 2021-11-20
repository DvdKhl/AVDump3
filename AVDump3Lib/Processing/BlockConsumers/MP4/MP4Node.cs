using BXmlLib;
using BXmlLib.DocTypes.MP4;

namespace AVDump3Lib.Processing.BlockConsumers.MP4;

public class MP4Node {
	private static readonly HashSet<MP4DocElement> DocElementsWithData = new() {
		MP4DocType.MovieHeader,
		MP4DocType.TrackHeader,
		MP4DocType.MediaHeader,
		MP4DocType.Handler,
		MP4DocType.VideoMediaHeader,
		MP4DocType.SoundMediaHeader,
		MP4DocType.HintMediaHeader,
		MP4DocType.DataReference,
		MP4DocType.SampleDescription,
		MP4DocType.TimeToSample,
		MP4DocType.CompositionOffset,
		MP4DocType.SyncSample,
		MP4DocType.SampleToChunk,
		MP4DocType.SampleSize,
		MP4DocType.ChunkOffset,
		MP4DocType.ChunkLargeOffset,
		MP4DocType.DataEntryUrl,
		MP4DocType.DataEntryUrn,
		MP4DocType.Copyright,
		MP4DocType.FileType,
		MP4DocType.MovieExtendsHeader,
		MP4DocType.TrackFragmentHeader,
		MP4DocType.MovieFragmentRandomAccessOffset,
		MP4DocType.NullMediaHeader,
		MP4DocType.PaddingBits,
		MP4DocType.OriginalFormat,
		MP4DocType.PrimaryItem,
		MP4DocType.ProgressiveDownloadInfo,
		MP4DocType.SampleGroupDescription,
		MP4DocType.SchemeInformation,
		MP4DocType.SchemeType,
		MP4DocType.ShadowSyncSample,
		MP4DocType.SubSampleInformation,
		MP4DocType.TrackRun
	};


	public MP4DocElement DocElement { get; private set; }
	public ReadOnlyMemory<byte> Data { get; private set; } = ReadOnlyMemory<byte>.Empty;
	public long Size { get; private set; }
	public MP4Node Parent { get; private set; }
	public IReadOnlyList<MP4Node> Children => children; private readonly List<MP4Node> children = new();

	public IEnumerable<MP4Node> Descendents(MP4DocElement docElement) {
		var toVisit = new Queue<MP4Node>(Children);

		while(toVisit.Count > 0) {
			var current = toVisit.Dequeue();

			if(current.DocElement == docElement) {
				yield return current;
			}

			foreach(var child in current.Children) {
				toVisit.Enqueue(child);
			}
		}
	}

	public static MP4Node Read(BXmlReader reader, long fileSize) {
		var box = new MP4Node {
			DocElement = MP4DocType.Root,
			Size = fileSize
		};

		Read(reader, box);

		return box;
	}
	private static void Read(BXmlReader reader, MP4Node box) {
		while(reader.Next()) {
			var child = new MP4Node {
				Parent = box,
				DocElement = (MP4DocElement)reader.DocElement,
				Size = reader.Header.DataLength
			};
			box.children.Add(child);

			if(reader.DocElement.IsContainer) {
				using(reader.EnterElement()) {
					Read(reader, child);
				}

			} else if(DocElementsWithData.Contains(child.DocElement)) {
				child.Data = reader.RetrieveRawValue().ToArray();
			}
		}
	}
}
