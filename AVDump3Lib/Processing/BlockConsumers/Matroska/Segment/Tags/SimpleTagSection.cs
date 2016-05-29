using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags {
    public class SimpleTagSection : Section {
		private string tagLanguage;
		private bool? tagdefault;
		private byte[] tagBinary;

		public EbmlList<SimpleTagSection> SimpleTags { get; private set; }


		public string TagName { get; private set; }
		public string TagLanguage { get { return tagLanguage ?? "und"; } } //Def: und
		public string TagString { get; private set; }
		public bool TagDefault { get { return tagdefault.GetValueOrDefault(true); } } //Def: True
		public byte[] TagBinary { get { return tagBinary.ToArray(); } }

		public SimpleTagSection() { SimpleTags = new EbmlList<SimpleTagSection>(); }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.TagName.Id) {
				TagName = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagLanguage.Id) {
				tagLanguage = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagString.Id) {
				TagString = (string)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagDefault.Id) {
				tagdefault = (ulong)reader.RetrieveValue(elemInfo) == 1;
			} else if(elemInfo.DocElement.Id == MatroskaDocType.TagBinary.Id) {
				tagBinary = (byte[])reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.SimpleTag.Id) {
				Section.CreateReadAdd(new SimpleTagSection(), reader, elemInfo, SimpleTags);
			} else return false;

			return true;
		}
		protected override void Validate() { }

		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("TagName", TagName);
			yield return CreatePair("TagLanguage", TagLanguage);
			yield return CreatePair("TagString", TagString);
			yield return CreatePair("TagDefault", TagDefault);
			yield return CreatePair("TagBinary", TagBinary);
			foreach(var simpleTag in SimpleTags) yield return CreatePair("SimpleTag", simpleTag);
		}
	}
}
