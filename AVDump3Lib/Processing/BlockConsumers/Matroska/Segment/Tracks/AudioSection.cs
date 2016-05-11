using CSEBML;
using CSEBML.DocTypes.Matroska;
using System.Collections.Generic;

namespace AVDump3Lib.BlockConsumers.Matroska.Segment.Tracks {
    public class AudioSection : Section {
		private double? samplingFrequency, outputSamplingFrequency;
		private ulong? channelCount;

		public double SamplingFrequency { get { return samplingFrequency ?? 8000f; } } //Default: 8000
		public double? OutputSamplingFrequency { get { return outputSamplingFrequency ?? SamplingFrequency; } }
		public ulong ChannelCount { get { return channelCount ?? 1; } } //Default: 1
		public ulong? BitDepth { get; private set; }

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.SamplingFrequency.Id) {
				samplingFrequency = (double)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.OutputSamplingFrequency.Id) {
				outputSamplingFrequency = (double)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.Channels.Id) {
				channelCount = (ulong)reader.RetrieveValue(elemInfo);
			} else if(elemInfo.DocElement.Id == MatroskaDocType.BitDepth.Id) {
				BitDepth = (ulong)reader.RetrieveValue(elemInfo);
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

		protected override bool ProcessElement(EBMLReader reader, ElementInfo elemInfo) {
			if(elemInfo.DocElement.Id == MatroskaDocType.ContentEncodings.Id) {
				Section.CreateReadAdd(new ContentEncodingSection(), reader, elemInfo, Encodings);
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
