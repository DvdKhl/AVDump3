using BXmlLib;
using BXmlLib.DocTypes.Matroska;
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

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.ChapterAtom) {
				Section.CreateReadAdd(new ChapterAtomSection(), reader, ChapterAtoms);
			} else if(reader.DocElement == MatroskaDocType.EditionUID) {
				EditionUId = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.EditionFlagHidden) {
				hidden = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.EditionFlagDefault) {
				def = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.EditionFlagOrdered) {
				ordered = (ulong)reader.RetrieveValue() == 1;
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
