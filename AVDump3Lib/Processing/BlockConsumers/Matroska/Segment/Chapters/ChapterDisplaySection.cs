using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Chapters {
    public class ChapterDisplaySection : Section {
		public string ChapterString { get; private set; }
		public EbmlList<string> ChapterLanguages { get; private set; } //Def: eng
		public EbmlList<string> ChapterCountries { get; private set; }

		public ChapterDisplaySection() {
			ChapterLanguages = new EbmlList<string>();
			ChapterCountries = new EbmlList<string>();
		}

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.ChapString) {
				ChapterString = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.ChapLanguage) {
				ChapterLanguages.Add((string)reader.RetrieveValue());
			} else if(reader.DocElement == MatroskaDocType.ChapCountry) {
				ChapterCountries.Add((string)reader.RetrieveValue());
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var chapterLanguage in ChapterLanguages) yield return CreatePair("ChapterLanguage", chapterLanguage);
			foreach(var chapterCountry in ChapterCountries) yield return CreatePair("ChapterCountry", chapterCountry);
			yield return new KeyValuePair<string, object>("ChapterString", ChapterString);
		}
	}
}
