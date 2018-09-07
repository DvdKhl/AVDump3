using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters {
    public class ChapterAtomSection : Section {
		private byte[] chapterSegmentUId;
		private bool? enabled, hidden;

		public ulong? ChapterUId { get; private set; }
		public string ChapterStringUId { get; private set; }
		public ulong? ChapterTimeStart { get; private set; } //Def: 0?
		public ulong? ChapterTimeEnd { get; private set; }
		public Options ChapterFlags { get { return (hidden.HasValue && hidden.Value ? Options.Hidden : Options.None) | (!enabled.HasValue || enabled.Value ? Options.Enabled : Options.None); } }
		public byte[] ChapterSegmentUId { get { return chapterSegmentUId == null ? null : (byte[])chapterSegmentUId.Clone(); } }
		public ulong? ChapterSegmentEditionUId { get; private set; }
		public ulong? ChapterPhysicalEquiv { get; private set; }
		public ChapterTrackSection ChapterTrack { get; private set; }
		public EbmlList<ChapterAtomSection> ChapterAtoms { get; private set; }
		public EbmlList<ChapterDisplaySection> ChapterDisplays { get; private set; }
		public EbmlList<ChapterProcessSection> ChapterProcesses { get; private set; }

		public ChapterAtomSection() {
			ChapterAtoms = new EbmlList<ChapterAtomSection>();
			ChapterDisplays = new EbmlList<ChapterDisplaySection>();
			ChapterProcesses = new EbmlList<ChapterProcessSection>();
		}

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.ChapterTrack) {
				ChapterTrack = CreateRead(new ChapterTrackSection(), reader);
			} else if(reader.DocElement == MatroskaDocType.ChapterAtom) {
				CreateReadAdd(new ChapterAtomSection(), reader, ChapterAtoms);
			} else if(reader.DocElement == MatroskaDocType.ChapterDisplay) {
				CreateReadAdd(new ChapterDisplaySection(), reader, ChapterDisplays);
			} else if(reader.DocElement == MatroskaDocType.ChapProcess) {
				CreateReadAdd(new ChapterProcessSection(), reader, ChapterProcesses);
			} else if(reader.DocElement == MatroskaDocType.ChapterUID) {
				ChapterUId = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ChapterStringUID) {
				ChapterStringUId = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ChapterTimeStart) {
				ChapterTimeStart = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ChapterTimeEnd) {
				ChapterTimeEnd = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ChapterFlagEnabled) {
				enabled = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.ChapterFlagHidden) {
				hidden = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.ChapterSegmentUID) {
				chapterSegmentUId = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ChapterSegmentEditionUID) {
				ChapterSegmentEditionUId = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ChapterPhysicalEquiv) {
				ChapterPhysicalEquiv = (ulong)reader.RetrieveValue();
			} else return false;

			return true;
		}
		protected override void Validate() { }

		[Flags]
		public enum Options { None = 0, Hidden = 1, Enabled = 2 }


		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return new KeyValuePair<string, object>("ChapterTrack", ChapterTrack);
			foreach(var chapterAtoms in ChapterAtoms) yield return CreatePair("ChapterAtom", chapterAtoms);
			foreach(var chapterDisplay in ChapterDisplays) yield return CreatePair("ChapterDisplay", chapterDisplay);
			foreach(var chapterProcess in ChapterProcesses) yield return CreatePair("ChapterProcess", chapterProcess);
			yield return CreatePair("ChapterUId", ChapterUId);
			yield return CreatePair("ChapterTimeStart", ChapterTimeStart);
			yield return CreatePair("ChapterTimeEnd", ChapterTimeEnd);
			yield return CreatePair("ChapterFlags", ChapterFlags);
			yield return CreatePair("ChapterSegmentUId", ChapterSegmentUId);
			yield return CreatePair("ChapterSegmentEditionUId", ChapterSegmentEditionUId);
		}
	}
}
