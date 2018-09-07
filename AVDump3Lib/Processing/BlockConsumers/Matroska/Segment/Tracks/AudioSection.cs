using BXmlLib;
using BXmlLib.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.Processing.BlockConsumers.Matroska.Segment.Tracks {
    public class AudioSection : Section {
		private double? samplingFrequency, outputSamplingFrequency;
		private ulong? channelCount;

		public double SamplingFrequency { get { return samplingFrequency ?? 8000f; } } //Default: 8000
		public double? OutputSamplingFrequency { get { return outputSamplingFrequency ?? SamplingFrequency; } }
		public ulong ChannelCount { get { return channelCount ?? 1; } } //Default: 1
		public ulong? BitDepth { get; private set; }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.SamplingFrequency) {
				samplingFrequency = (double)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.OutputSamplingFrequency) {
				outputSamplingFrequency = (double)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.Channels) {
				channelCount = (ulong)reader.RetrieveValue();
			} else if(reader.DocElement == MatroskaDocType.BitDepth) {
				BitDepth = (ulong)reader.RetrieveValue();
			} else return false;

			return true;
		}
		protected override void Validate() { }
		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			yield return CreatePair("SamplingFrequency", SamplingFrequency);
			yield return CreatePair("OutputSamplingFrequency", OutputSamplingFrequency);
			yield return CreatePair("ChannelCount", ChannelCount);
			yield return CreatePair("BitDepth", BitDepth);
		}
	}
	public class ContentEncodingsSection : Section {
		public EbmlList<ContentEncodingSection> Encodings { get; private set; }

		public ContentEncodingsSection() { Encodings = new EbmlList<ContentEncodingSection>(); }

		protected override bool ProcessElement(IBXmlReader reader) {
			if(reader.DocElement == MatroskaDocType.ContentEncodings) {
				Section.CreateReadAdd(new ContentEncodingSection(), reader, Encodings);
				return true;
			}
			return false;
		}
		protected override void Validate() { }
		public override IEnumerator<KeyValuePair<string, object>> GetEnumerator() {
			foreach(var encoding in Encodings) yield return CreatePair("ContentEncoding", encoding);
		}
	}
}
