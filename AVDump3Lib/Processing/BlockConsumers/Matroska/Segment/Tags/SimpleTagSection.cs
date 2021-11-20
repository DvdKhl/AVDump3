using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tags {
	public class SimpleTagSection : Section {
		private string tagLanguage;
		private bool? tagdefault;
		private byte[] tagBinary;

		public EbmlList<SimpleTagSection> SimpleTags { get; private set; }


		public string TagName { get; private set; }
		public string TagLanguage => tagLanguage ?? "und";  //Def: und
		public string TagString { get; private set; }
		public bool TagDefault => tagdefault.GetValueOrDefault(true);  //Def: True
		public byte[] TagBinary => tagBinary?.ToArray() ?? Array.Empty<byte>();

		public SimpleTagSection() { SimpleTags = new EbmlList<SimpleTagSection>(); }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.TagName) {
				TagName = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.TagLanguage) {
				tagLanguage = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.TagString) {
				TagString = (string)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.TagDefault) {
				tagdefault = (ulong)reader.RetrieveValue() == 1;
			} else if(reader.DocElement == MatroskaDocType.TagBinary) {
				tagBinary = (byte[])reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.SimpleTag) {
				Section.CreateReadAdd(new SimpleTagSection(), reader, SimpleTags);
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
