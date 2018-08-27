using CSEBML;
using CSEBML.DocTypes.Matroska;
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

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTrack.Id) {
				ChapterTrack = Section.CreateRead(new ChapterTrackSection(), reader, elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterAtom.Id) {
				Section.CreateReadAdd(new ChapterAtomSection(), reader, elemInfo, ChapterAtoms);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterDisplay.Id) {
				Section.CreateReadAdd(new ChapterDisplaySection(), reader, elemInfo, ChapterDisplays);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapProcess.Id) {
				Section.CreateReadAdd(new ChapterProcessSection(), reader, elemInfo, ChapterProcesses);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterUID.Id) {
				ChapterUId = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterStringUID.Id) {
				ChapterStringUId = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTimeStart.Id) {
				ChapterTimeStart = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterTimeEnd.Id) {
				ChapterTimeEnd = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterFlagEnabled.Id) {
				enabled = (ulong)reader.RetrieveValue(elemInfo) == 1;
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterFlagHidden.Id) {
				hidden = (ulong)reader.RetrieveValue(elemInfo) == 1;
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterSegmentUID.Id) {
				chapterSegmentUId = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterSegmentEditionUID.Id) {
				ChapterSegmentEditionUId = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.ChapterPhysicalEquiv.Id) {
				ChapterPhysicalEquiv = (ulong)reader.RetrieveValue(elemInfo);
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
