//using AVDump3Lib.FormatHeaders;
using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks {
    public class TrackEntrySection : Section {
		private bool? enabled, def, forced, lacing;
		private string language;
		private ulong? minCache;
		private ulong? maxBlockAdditionID;
		private Types? trackType;

		public ulong? TrackNumber { get; private set; } //Not 0; Mandatory
		public ulong? TrackUId { get; private set; } //Not 0
		public EbmlList<ulong> TrackOverlay { get; private set; }
		public Types TrackType { get { return trackType ?? Types.Invalid; } } //Mandatory
		public Options TrackFlags {
			get { //Set: Default, Enabled
				return (!enabled.HasValue || enabled.Value ? Options.Enabled : Options.None) |
					   (forced.HasValue && forced.Value ? Options.Forced : Options.None) |
					   (!def.HasValue || def.Value ? Options.Default : Options.None) |
					   (lacing.HasValue && lacing.Value ? Options.Lacing : Options.None);
			}
		}
		public ulong MinCache { get { return minCache ?? 0; } } //Default: 0
		public ulong? MaxCache { get; private set; }
		public ulong MaxBlockAdditionID { get { return maxBlockAdditionID ?? 0; } } //Default: 0
		public ulong? DefaultDuration { get; private set; }
		public ulong? DefaultDecodedFieldDuration { get; private set; }
		public double? TrackTimecodeScale { get; private set; }
		public string Name { get; private set; }
		public string Language { get { return language ?? "eng"; } } //Default: 'eng'
		public string CodecId { get; private set; } //Mandatory
		public byte[] CodecPrivate { get; private set; }
		public string CodecName { get; private set; }
		public string AttachmentLink { get; private set; }

		public VideoSection Video { get; private set; }
		public AudioSection Audio { get; private set; }
		public ContentEncodingsSection ContentEncodings { get; private set; }


		public TrackEntrySection() { TrackOverlay = new EbmlList<ulong>(); }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.TrackNumber) {
				TrackNumber = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.TrackUID) {
				TrackUId = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.TrackOverlay) {
				TrackOverlay.Add((ulong)reader.RetrieveValue());
			} else if(reader.DocElement == MatroskaDocType.TrackType) {
				trackType = (Types)(ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.MinCache) {
				minCache = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.MaxCache) {
				MaxCache = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.MaxBlockAdditionID) {
				maxBlockAdditionID = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.DefaultDuration) {
				DefaultDuration = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.DefaultDecodedFieldDuration) {
				DefaultDecodedFieldDuration = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.TrackTimecodeScale) {
				TrackTimecodeScale = (double)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.Name) {
				Name = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.Language) {
				language = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CodecID) {
				CodecId = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CodecName) {
				CodecName = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.CodecPrivate) {
				CodecPrivate = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.AttachmentLink) {
				AttachmentLink = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.FlagEnabled) {
				enabled = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.FlagDefault) {
				def = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.FlagForced) {
				forced = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.FlagLacing) {
				lacing = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.Video) {
				Video = Section.CreateRead(new VideoSection(), reader);
			} else if(reader.DocElement == MatroskaDocType.Audio) {
				Audio = Section.CreateRead(new AudioSection(), reader);
			} else if(reader.DocElement == MatroskaDocType.ContentEncodings) {
				ContentEncodings = Section.CreateRead(new ContentEncodingsSection(), reader);
			} else return false;

			return true;
		}
		protected override void Validate() { } //TODO: Check for TrackNumber

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("TrackNumber", TrackNumber);
			yield return CreatePair("TrackUId", TrackUId);
			foreach(var item in TrackOverlay) yield return CreatePair("TrackOverlay", item);
			yield return CreatePair("TrackType", TrackType);
			yield return CreatePair("TrackFlags", TrackFlags);
			yield return CreatePair("MinCache", MinCache);
			yield return CreatePair("MaxCache", MaxCache);
			yield return CreatePair("MaxBlockAdditionID", MaxBlockAdditionID);
			yield return CreatePair("DefaultDuration", DefaultDuration);
			yield return CreatePair("DefaultDecodedFieldDuration", DefaultDecodedFieldDuration);
			yield return CreatePair("TrackTimecodeScale", TrackTimecodeScale);
			yield return CreatePair("Name", Name);
			yield return CreatePair("Language", Language);
			yield return CreatePair("CodecId", CodecId);
			yield return CreatePair("CodecPrivate", CodecPrivate);
			yield return CreatePair("CodecName", CodecName);
			yield return CreatePair("AttachmentLink", AttachmentLink);
			yield return CreatePair("Video", Video);
			yield return CreatePair("Audio", Audio);
			yield return CreatePair("ContentEncodings", ContentEncodings);
		}


		[Flags]
		public enum Options { None = 0, Enabled = 1, Default = 2, Forced = 4, Lacing = 8 }
		public enum Types { Invalid = 0, Video = 0x1, Audio = 0x2, Complex = 0x3, Logo = 0x10, Subtitle = 0x11, Button = 0x12, Control = 0x20 }
	}
}
