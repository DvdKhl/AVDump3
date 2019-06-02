using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace AVDump3Lib.Information.InfoProvider {

	public sealed class MediaInfoLibNativeMethods : IDisposable {
		public const string MILNAME = "MediaInfo";

		public static readonly bool UseUnicode = Environment.OSVersion.Platform == PlatformID.Win32NT;
		public static readonly bool Use64Bit = IntPtr.Size == 8;

		public enum StreamTypes { General, Video, Audio, Text, Chapters, Image, Menu }
		public enum InfoTypes { Name, Text, Measure, Options, NameText, MeasureText, Info, HowTo }
		public enum InfoOptions { ShowInInform, Reserved, ShowInSupported, TypeOfValue }
		public enum InfoFileOptions { None = 0x00, Recursive = 0x01, CloseAll = 0x02 };

		#region MIL API
#pragma warning disable CS0618 // Type or member is obsolete
		[DllImport(MILNAME, EntryPoint = "MediaInfo_New")]
		private static extern IntPtr MediaInfo_New(); //Tested


		[DllImport(MILNAME, EntryPoint = "MediaInfo_Delete")]
		private static extern void MediaInfo_Delete(IntPtr handle);


		[DllImport(MILNAME, EntryPoint = "MediaInfo_Open", CharSet = CharSet.Unicode)]
		private static extern IntPtr MediaInfoU_Open(IntPtr handle, [MarshalAs(UnmanagedType.BStr)] string filename); //Tested

		[DllImport(MILNAME, EntryPoint = "MediaInfoA_Open", CharSet = CharSet.Ansi)]
		private static extern IntPtr MediaInfoA_Open(IntPtr handle, [MarshalAs(UnmanagedType.AnsiBStr)] string fileName);

		private static IntPtr MediaInfo_Open(IntPtr handle, string fileName) { return UseUnicode ? MediaInfoU_Open(handle, fileName) : MediaInfoU_Open(handle, fileName); }


		[DllImport(MILNAME, EntryPoint = "MediaInfo_Close")]
		private static extern void MediaInfo_Close(IntPtr handle);


		[DllImport(MILNAME, EntryPoint = "MediaInfo_Inform", CharSet = CharSet.Unicode)]
		private static extern IntPtr MediaInfoU_Inform(IntPtr handle, IntPtr reserved); //Tested

		[DllImport(MILNAME, EntryPoint = "MediaInfoA_Inform", CharSet = CharSet.Ansi)]
		private static extern IntPtr MediaInfoA_Inform(IntPtr handle, IntPtr reserved);

		private static string MediaInfo_Inform(IntPtr handle, IntPtr reserved) { return Marshal.PtrToStringUni(UseUnicode ? MediaInfoU_Inform(handle, reserved) : MediaInfoA_Inform(handle, reserved)); }


		[DllImport(MILNAME, EntryPoint = "MediaInfo_GetI", CharSet = CharSet.Unicode)]
		private static extern IntPtr MediaInfoU_GetI(IntPtr handle, StreamTypes streamKind, IntPtr streamNumber, IntPtr parameter, InfoTypes kindOfInfo);

		[DllImport(MILNAME, EntryPoint = "MediaInfoA_GetI", CharSet = CharSet.Ansi)]
		private static extern IntPtr MediaInfoA_GetI(IntPtr handle, StreamTypes streamKind, IntPtr streamNumber, IntPtr parameter, InfoTypes kindOfInfo);

		private static string MediaInfo_GetI(IntPtr handle, StreamTypes streamKind, IntPtr streamNumber, IntPtr parameter, InfoTypes kindOfInfo) { return Marshal.PtrToStringUni(UseUnicode ? MediaInfoU_GetI(handle, streamKind, streamNumber, parameter, kindOfInfo) : MediaInfoA_GetI(handle, streamKind, streamNumber, parameter, kindOfInfo)); }


		[DllImport(MILNAME, EntryPoint = "MediaInfo_Get", CharSet = CharSet.Unicode)]
		private static extern IntPtr MediaInfoU_Get(IntPtr handle, StreamTypes streamKind, IntPtr streamNumber, [MarshalAs(UnmanagedType.BStr)] string parameter, InfoTypes kindOfInfo, InfoTypes kindOfSearch); //Tested

		[DllImport(MILNAME, EntryPoint = "MediaInfoA_Get", CharSet = CharSet.Ansi)]
		private static extern IntPtr MediaInfoA_Get(IntPtr handle, StreamTypes streamKind, IntPtr streamNumber, [MarshalAs(UnmanagedType.AnsiBStr)] string parameter, InfoTypes kindOfInfo, InfoTypes kindOfSearch);

		private static string MediaInfo_Get(IntPtr handle, StreamTypes streamKind, IntPtr streamNumber, string parameter, InfoTypes kindOfInfo, InfoTypes kindOfSearch) { return Marshal.PtrToStringUni(UseUnicode ? MediaInfoU_Get(handle, streamKind, streamNumber, parameter, kindOfInfo, kindOfSearch) : MediaInfoA_Get(handle, streamKind, streamNumber, parameter, kindOfInfo, kindOfSearch)); }


		[DllImport(MILNAME, EntryPoint = "MediaInfo_Option", CharSet = CharSet.Unicode)]
		private static extern IntPtr MediaInfoU_Option(IntPtr handle, [MarshalAs(UnmanagedType.BStr)] string option, [MarshalAs(UnmanagedType.BStr)] string value); //Tested

		[DllImport(MILNAME, EntryPoint = "MediaInfoA_Option", CharSet = CharSet.Ansi)]
		private static extern IntPtr MediaInfoA_Option(IntPtr handle, [MarshalAs(UnmanagedType.AnsiBStr)] string option, [MarshalAs(UnmanagedType.AnsiBStr)] string value);

		private static string MediaInfo_Option(IntPtr handle, string option, string value) { return Marshal.PtrToStringUni(UseUnicode ? MediaInfoU_Option(handle, option, value) : MediaInfoA_Option(handle, option, value)); }


		[DllImport(MILNAME, EntryPoint = "MediaInfo_State_Get")]
		private static extern IntPtr MediaInfo_State_Get(IntPtr handle);


		[DllImport(MILNAME, EntryPoint = "MediaInfo_Count_Get")]
		private static extern IntPtr MediaInfo_Count_Get(IntPtr handle, StreamTypes streamKind, IntPtr streamNumber);
#pragma warning restore CS0618 // Type or member is obsolete
		#endregion


		public IntPtr Handle { get; private set; }

		public MediaInfoLibNativeMethods() {
			Handle = MediaInfo_New();
			Option("Internet", "No");
		}

		public bool Open(string filePath) { var retVal = MediaInfo_Open(Handle, filePath); return (int)retVal == 1; }
		public string Inform() { return MediaInfo_Inform(Handle, IntPtr.Zero); }
		public string Get(StreamTypes streamKind, int streamNumber, string parameter, InfoTypes kindOfInfo, InfoTypes kindOfSearch) { return MediaInfo_Get(Handle, streamKind, (IntPtr)streamNumber, parameter, kindOfInfo, kindOfSearch); }
		public string Get(StreamTypes streamKind, int streamNumber, int parameter, InfoTypes kindOfInfo) { return MediaInfo_GetI(Handle, streamKind, (IntPtr)streamNumber, (IntPtr)parameter, kindOfInfo); }
		public string Option(string option, string value) { return MediaInfo_Option(Handle, option, value); }
		public int GetState() { return (int)MediaInfo_State_Get(Handle); }
		public int GetCount(StreamTypes streamKind, int streamNumber) { return (int)MediaInfo_Count_Get(Handle, streamKind, (IntPtr)streamNumber); }

		public string Get(StreamTypes streamKind, int streamNumber, string parameter, InfoTypes KindOfInfo) { return Get(streamKind, streamNumber, parameter, KindOfInfo, InfoTypes.Name); }
		public string Get(StreamTypes streamKind, int streamNumber, string parameter) { return Get(streamKind, streamNumber, parameter, InfoTypes.Text, InfoTypes.Name); }
		public string Get(StreamTypes streamKind, int streamNumber, int parameter) { return Get(streamKind, streamNumber, parameter, InfoTypes.Text); }
		public string Get(string parameter) { return Get(StreamTypes.General, 0, parameter); }

		public string Option(string option) { return Option(option, ""); }
		public int GetCount(StreamTypes streamKind) { return GetCount(streamKind, -1); }

		public void Dispose() {
			if(Handle != IntPtr.Zero) {
				MediaInfo_Close(Handle);
				MediaInfo_Delete(Handle);
			}
			Handle = IntPtr.Zero;
		}
	}

	public class MediaInfoLibProvider : MediaProvider {

		private void Populate(MediaInfoLibNativeMethods mil) {
			string removeNonNumerics(string s) => Regex.Replace(s, "[^-,.0-9]", "");
			string splitTakeFirst(string s) => s.Split('\\', '/', '|')[0];
			//string nonEmpty(string a, string b) => string.IsNullOrEmpty(a) ? b : a;

			Add(FileSizeType, () => mil.Get("FileSize"), s => s.ToInvInt64(), splitTakeFirst, removeNonNumerics);
			Add(DurationType, () => mil.Get("Duration"), s => s.ToInvDouble() / 1000, splitTakeFirst, removeNonNumerics);
			Add(FileExtensionType, () => mil.Get("FileExtension"), s => s.ToLowerInvariant(), splitTakeFirst); //TODO: Add multiple if multiple
			Add(WritingAppType, () => mil.Get("Encoded_Application"));
			Add(MuxingAppType, () => mil.Get("Encoded_Library"));


			bool hasAudio = false, hasVideo = false, hasSubtitle = false;
			foreach(var streamKind in new[] { MediaInfoLibNativeMethods.StreamTypes.Video, MediaInfoLibNativeMethods.StreamTypes.Audio, MediaInfoLibNativeMethods.StreamTypes.Text }) {
				var streamCount = mil.GetCount(streamKind);

				for(var streamNumber = 0; streamNumber < streamCount; streamNumber++) {
					string streamGet(string key) => mil.Get(streamKind, streamNumber, key);

					ulong? id = null;
					if(!string.IsNullOrEmpty(streamGet("UniqueID"))) {
						id = streamGet("UniqueID").ToInvUInt64();
					}

					MetaInfoContainer stream = null;
					switch(streamKind) {
						case MediaInfoLibNativeMethods.StreamTypes.Video:
							stream = new MetaInfoContainer(id ?? (ulong)Nodes.Count(x => x.Type == ChaptersType), VideoStreamType); hasVideo = true;
							Add(stream, MediaStream.StatedSampleRateType, () => streamGet("FrameRate").ToInvDouble());
							Add(stream, MediaStream.SampleCountType, () => streamGet("FrameCount").ToInvInt64());
							Add(stream, VideoStream.PixelAspectRatioType, () => streamGet("PixelAspectRatio").ToInvDouble());
							Add(stream, VideoStream.PixelDimensionsType, () => new Dimensions(streamGet("Width").ToInvInt32(), streamGet("Height").ToInvInt32()));
							Add(stream, VideoStream.DisplayAspectRatioType, () => streamGet("DisplayAspectRatio").ToInvDouble());
							Add(stream, VideoStream.ColorBitDepthType, () => streamGet("BitDepth").ToInvInt32());

							Add(stream, VideoStream.IsInterlacedType, () => streamGet("ScanType").Equals("Interlaced"));
							Add(stream, VideoStream.HasVariableFrameRateType, () => streamGet("FrameRate_Mode").Equals("VFR", StringComparison.OrdinalIgnoreCase));
							Add(stream, VideoStream.ChromaSubsamplingType, () => new ChromeSubsampling(streamGet("ChromaSubsampling")));
							//Add(stream, VideoStream.ColorSpaceType, () => streamGet("ColorSpace"));
							AddNode(stream);
							break;

						case MediaInfoLibNativeMethods.StreamTypes.Audio:
							stream = new MetaInfoContainer(id ?? (ulong)Nodes.Count(x => x.Type == AudioStreamType), AudioStreamType); hasAudio = true;
							Add(stream, MediaStream.StatedSampleRateType, () => streamGet("SamplingRate").ToInvDouble());
							Add(stream, MediaStream.SampleCountType, () => streamGet("SamplingCount").ToInvInt32());
							Add(stream, AudioStream.ChannelCountType, () => streamGet("Channel(s)").ToInvInt32());
							AddNode(stream);
							break;

						case MediaInfoLibNativeMethods.StreamTypes.Text:
							stream = new MetaInfoContainer(id ?? (ulong)Nodes.Count(x => x.Type == SubtitleStreamType), SubtitleStreamType); hasSubtitle = true;
							AddNode(stream);
							break;

						default:
							stream = new MetaInfoContainer(id ?? (ulong)Nodes.Count(x => x.Type == MediaStreamType), MediaStreamType);
							AddNode(stream);
							break;
					}

					Add(stream, MediaStream.SizeType, () => streamGet("StreamSize").ToInvInt64());
					Add(stream, MediaStream.TitleType, () => streamGet("Title"));
					Add(stream, MediaStream.IsForcedType, () => streamGet("Forced").Equals("yes", StringComparison.OrdinalIgnoreCase));
					Add(stream, MediaStream.IsDefaultType, () => streamGet("Default").Equals("yes", StringComparison.OrdinalIgnoreCase));
					Add(stream, MediaStream.IdType, () => streamGet("UniqueID").ToInvUInt64());
					Add(stream, MediaStream.LanguageType, () => streamGet("Language"));
					Add(stream, MediaStream.DurationType, () => streamGet("Duration"), s => TimeSpan.FromSeconds(s.ToInvDouble() / 1000), (Func<string, string>)splitTakeFirst);
					Add(stream, MediaStream.BitrateType, () => streamGet("BitRate").ToInvDouble());
					Add(stream, MediaStream.ContainerCodecIdType, () => streamGet("CodecID"));
					Add(stream, MediaStream.CodecIdType, () => streamGet("Format"));
					Add(stream, MediaStream.CodecProfileType, () => streamGet("Format_Profile"));
					Add(stream, MediaStream.CodecVersionType, () => streamGet("Format_Version"));
					Add(stream, MediaStream.CodecNameType, () => streamGet("Format-Info"));
					Add(stream, MediaStream.EncoderSettingsType, () => streamGet("Encoded_Library_Settings"));
					Add(stream, MediaStream.EncoderNameType, () => streamGet("Encoded_Library"));
					Add(stream, MediaStream.StatedBitrateModeType, () => streamGet("BitRate_Mode"));
				}
			}

			AddSuggestedFileExtension(mil, hasAudio, hasVideo, hasSubtitle);
		}

		private void AddSuggestedFileExtension(MediaInfoLibNativeMethods mil, bool hasAudio, bool hasVideo, bool hasSubtitle) {
			var milInfo = (mil.Get("Format/Extensions") ?? "").ToLowerInvariant();
			var fileExt = (mil.Get("FileExtension") ?? "").ToLowerInvariant();
			if(milInfo.Contains("asf") && milInfo.Contains("wmv") && milInfo.Contains("wma")) {
				if(!hasVideo && hasAudio && !hasSubtitle) {
					Add(SuggestedFileExtensionType, "wma");
				} else {
					Add(SuggestedFileExtensionType, "wmv");
				}
			} else if(milInfo.Contains("ts") && milInfo.Contains("m2t")) {
				if(fileExt.Equals(".ts")) Add(SuggestedFileExtensionType, "ts"); //Blame worf

			} else if(milInfo.Contains("mpeg") && milInfo.Contains("mpg")) {
				if(!hasVideo || !hasAudio && hasAudio) {
					Add(SuggestedFileExtensionType, "sub");
				} else {
					Add(SuggestedFileExtensionType, fileExt.Equals("mpeg") ? "mpeg" : "mpg");
				}
			} else if((milInfo.Contains("mp1") && milInfo.Contains("mp2") && milInfo.Contains("mp3")) || milInfo.Contains("wav")) {
				switch(mil.Get(MediaInfoLibNativeMethods.StreamTypes.Audio, 0, "Format_Profile")) {
					case "Layer 1": Add(SuggestedFileExtensionType, "mp1"); break;
					case "Layer 2": Add(SuggestedFileExtensionType, "mp2"); break;
					case "Layer 3": Add(SuggestedFileExtensionType, "mp3"); break;
				}

			} else if(milInfo.Contains("mp4") && milInfo.Contains("m4a") && milInfo.Contains("m4v")) {
				if(hasSubtitle || (hasVideo && hasAudio)) {
					Add(SuggestedFileExtensionType, "mp4");
				} else if(hasVideo && !hasAudio) {
					Add(SuggestedFileExtensionType, "m4v");
				} else if(!hasVideo && hasAudio) {
					Add(SuggestedFileExtensionType, "m4a");
				}
			}

			if(Select(SuggestedFileExtensionType) == null) {
				Add(SuggestedFileExtensionType, milInfo);
			}
		}

		public MediaInfoLibProvider(string filePath)
			: base("MediaInfoLibProvider") {

			using(var mil = new MediaInfoLibNativeMethods()) {

				if(File.Exists(filePath)) {
					if(mil.Open(filePath)) {
						Populate(mil);
					} else {
						throw new InvalidOperationException("MediaInfoLib couldn't open the file");
					}
				}
			}
		}

		private void Add(MetaInfoItemType<string> type, string value) {
			if(!string.IsNullOrWhiteSpace(value)) {
				base.Add(type, value);
			}
		}


		private void Add<T>(MetaInfoItemType<T> type, Func<string> getValue, Func<string, T> transform, params Func<string, string>[] processingChain) {
			Add(this, type, getValue, transform, processingChain);
		}
		private void Add<T>(MetaInfoItemType<T> type, Func<T> getValue) {
			Add(this, type, getValue);
		}
		private void Add<T>(MetaInfoContainer container, MetaInfoItemType<T> type, Func<string> getValue, Func<string, T> transform, params Func<string, string>[] processingChain) {
			Add(container, type, () => transform(processingChain.Aggregate(getValue(), (val, chain) => chain(val))));
		}
		private void Add<T>(MetaInfoContainer container, MetaInfoItemType<T> type, Func<T> getValue) {
			T value;
			try {
				value = getValue();
			} catch {
				return;
			}

			if(value is string && string.IsNullOrWhiteSpace(value as string)) {
				return;
			}

			Add(container, type, value);
		}
	}
}
