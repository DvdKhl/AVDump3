using BXmlLib;
using BXmlLib.DocTypes.Matroska;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks;

public class TracksSection : Section {
	public EbmlList<TrackEntrySection> Items { get; private set; }

	public TrackEntrySection this[TrackEntrySection.Types type, int index] {
		get {
			for(var i = 0; i < Items.Count; i++) if(Items[i].TrackType == type && index-- == 0) return Items[i];
			throw new Exception("Index out of range");
		}
	}
	public int Count(TrackEntrySection.Types type) { return Items.Sum(ldItem => ldItem.TrackType == type ? 1 : 0); }

	public TracksSection() { Items = new EbmlList<TrackEntrySection>(); }

	protected override bool ProcessElement(IBXmlReader reader) {
		if(reader.DocElement == MatroskaDocType.TrackEntry) {
			Section.CreateReadAdd(new TrackEntrySection(), reader, Items);
			return true;
		}
		return false;
	}
	protected override void Validate() { }

	public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
		foreach(var item in Items) yield return CreatePair("Track", item);
	}
}
