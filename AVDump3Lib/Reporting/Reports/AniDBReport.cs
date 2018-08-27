using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Reporting.Core;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3Lib.Reporting.Reports {
	public class AniDBReport : XmlReport {
		protected override XDocument Report { get; }

		private static string ConvertDate(DateTime? date) {
			if(!date.HasValue) return "";
			return Convert.ToString((int)(date.Value - new DateTime(1970, 1, 1)).TotalSeconds, 16);
		}

		public AniDBReport(FileMetaInfo fileMetaInfo) {
			Report = new XDocument();

			var fileElem = new XElement("file");
			var hashProvider = fileMetaInfo.CondensedProviders.OfType<HashProvider>().Single();
			fileElem.Add(hashProvider.Items.Select(x => new XElement(x.Type.Key, BitConverter.ToString((byte[])x.Value).Replace("-", ""))));
			fileElem.Add(new XElement("size", fileMetaInfo.FileInfo.Length));

			var mediaProvider = fileMetaInfo.CondensedProviders.OfType<CompositeMetaDataProvider>().Single();
			var dateElem = new XElement("date", ConvertDate(mediaProvider.Select(MediaProvider.CreationDateType)?.Value));
			var durationElem = new XElement("duration", mediaProvider.Select(MediaProvider.DurationType));
			var appElem = new XElement("app", mediaProvider.Select(MediaProvider.MuxingAppType));
			var extensionElem = new XElement("extension", mediaProvider.Select(MediaProvider.SuggestedFileExtensionType));
			var fileExtElem = new XElement("file_extension", fileMetaInfo.FileInfo.Extension);

			XElement segmentElem = null;
			var idValue = mediaProvider.Select(MediaProvider.IdType)?.Value;
			if(idValue != null) {
				segmentElem = new XElement("segmient_uid", BitConverter.ToString(idValue).Replace("-", ""));
			}

			var extraSizeElem = new XElement("extra_size", mediaProvider.Select(MediaProvider.OverheadType));
			var avmfElem = new XElement("avmd", dateElem, durationElem, appElem, extensionElem, fileExtElem, segmentElem, extraSizeElem
		   );

			avmfElem.Add(
				mediaProvider.Nodes.Where(x => x.Type == MediaProvider.VideoStreamType).Select(x => AddStreamData(x))
			);
			avmfElem.Add(
				mediaProvider.Nodes.Where(x => x.Type == MediaProvider.AudioStreamType).Select(x => AddStreamData(x))
			);
			avmfElem.Add(
				mediaProvider.Nodes.Where(x => x.Type == MediaProvider.SubtitleStreamType).Select(x => AddStreamData(x))
			);
			avmfElem.Add(
				mediaProvider.Nodes.Where(x => x.Type == MediaProvider.MediaStreamType).Select(x => AddStreamData(x))
			);


			fileElem.Add(avmfElem);
			Report.Add(fileElem);
		}

		public static XElement AddStreamData(MetaInfoContainer stream) {
			XElement streamElem;
			if(stream.Type == MediaProvider.VideoStreamType) {
				streamElem = new XElement("video");

				streamElem.Add(new XElement("res_p",
					new XAttribute("width", stream.Select(VideoStream.PixelDimensionsType).Value.Width),
					new XAttribute("height", stream.Select(VideoStream.PixelDimensionsType).Value.Height)
				));
				streamElem.Add(new XElement("res_d",
					new XAttribute("width", stream.Select(VideoStream.DisplayDimensionsType).Value.Width),
					new XAttribute("height", stream.Select(VideoStream.DisplayDimensionsType).Value.Height)
				));
				streamElem.Add(new XElement("colorbitdepth", stream.Select(VideoStream.ColorBitDepthType).Value));
				streamElem.Add(new XElement("fps",
					new XAttribute("avg", stream.Select(MediaStream.AverageSampleRateType).Value),
					new XAttribute("max", stream.Select(MediaStream.MinSampleRateType).Value),
					new XAttribute("min", stream.Select(MediaStream.MaxSampleRateType).Value),
					stream.Select(MediaStream.StatedSampleRateType).Value
				));
				streamElem.Add(new XAttribute("ar", stream.Select(VideoStream.DisplayAspectRatioType).Value));
				streamElem.Add(new XAttribute("settings", stream.Select(MediaStream.EncoderSettingsType).Value));
				streamElem.Add(new XAttribute("encoder", stream.Select(MediaStream.EncoderNameType).Value));


			} else if(stream.Type == MediaProvider.VideoStreamType) {
				streamElem = new XElement("audio");
				streamElem.Add(new XAttribute("sampling_rate", stream.Select(MediaStream.StatedSampleRateType).Value));
				streamElem.Add(new XAttribute("channels", stream.Select(AudioStream.ChannelCountType).Value));

			} else if(stream.Type == MediaProvider.VideoStreamType) {
				streamElem = new XElement("subtitle");

			} else {
				streamElem = new XElement("other");
			}
			streamElem.Add(new XAttribute("default", stream.Select(MediaStream.IsDefaultType).Value ? "1" : "0"));
			streamElem.Add(new XElement("size", stream.Select(MediaStream.SizeType).Value));
			streamElem.Add(new XElement("bitrate", stream.Select(MediaStream.BitrateType).Value));
			streamElem.Add(new XElement("duration", stream.Select(MediaStream.DurationType).Value.TotalSeconds));
			streamElem.Add(new XElement("title", stream.Select(MediaStream.TitleType)?.Value));
			streamElem.Add(new XElement("language", stream.Select(MediaStream.LanguageType).Value));
			streamElem.Add(new XElement("identifier", stream.Select(MediaStream.CodecIdType).Value));
			streamElem.Add(new XElement("identifier2", stream.Select(MediaStream.CodecNameType)?.Value));
			streamElem.Add(new XElement("id", stream.Select(MediaStream.IdType)?.Value));
			streamElem.Add(new XElement("sample_count", stream.Select(MediaStream.SampleCountType)?.Value));


			return streamElem;
		}
	}
}
