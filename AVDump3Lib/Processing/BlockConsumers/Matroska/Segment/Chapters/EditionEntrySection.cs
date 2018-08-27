using CSEBML;
using CSEBML.DocTypes.Matroska;
using System;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters {
    public class EditionEntrySection : Section {
		private bool? hidden, ordered, def;

		public ulong? EditionUId { get; private set; }
		public Options EditionFlags {
			get {
				return (hidden.HasValue && hidden.Value ? Options.Hidden : Options.None) |
					   (ordered.HasValue && ordered.Value ? Options.Ordered : Options.None) |
					   (def.HasValue && def.Value ? Options.Default : Options.None);
			}
		}
		public EbmlList<ChapterAtomSection> ChapterAtoms { get; private set; }

		public EditionEntrySection() { ChapterAtoms = new EbmlList<ChapterAtomSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ChapterAtom.Id) {
				Section.CreateReadAdd(new ChapterAtomSection(), reader, elemInfo, ChapterAtoms);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.EditionUID.Id) {
				EditionUId = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.EditionFlagHidden.Id) {
				hidden = (ulong)reader.RetrieveValue(elemInfo) == 1;
			} else if(elemInfo.DocElement.Id == MatroskaDocType.EditionFlagDefault.Id) {
				def = (ulong)reader.RetrieveValue(elemInfo) == 1;
			} else if(elemInfo.DocElement.Id == MatroskaDocType.EditionFlagOrdered.Id) {
				ordered = (ulong)reader.RetrieveValue(elemInfo) == 1;
			} else return false;

			return true;
		}
		protected override void Validate() { }

		[Flags]
		public enum Options { None = 0, Hidden = 1, Default = 2, Ordered = 4 }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("EditionUId", EditionUId);
			yield return CreatePair("EditionFlags", EditionFlags);
			foreach(var chapterAtom in ChapterAtoms) yield return CreatePair("ChapterAtom", chapterAtom);
		}
	}
}
