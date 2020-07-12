using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using AVDump3Lib.Misc;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace AVDump3Lib.Information.InfoProvider {
	public class MediaInfoLibNativeMethods : IDisposable {
		#region "NativeCode"
		public enum StreamTypes {
			General,
			Video,
			Audio,
			Text,
			Other,
			Image,
			Menu,
		}

		public enum InfoTypes {
			Name,
			Text,
			Measure,
			Options,
			NameText,
			MeasureText,
			Info,
			HowTo
		}

		public enum InfoOptions {
			ShowInInform,
			Support,
			ShowInSupported,
			TypeOfValue
		}

		public enum InfoFileOptions {
			FileOption_Nothing = 0x00,
			FileOption_NoRecursive = 0x01,
			FileOption_CloseAll = 0x02,
			FileOption_Max = 0x04
		};

		public enum Status {
			None = 0x00,
			Accepted = 0x01,
			Filled = 0x02,
			Updated = 0x04,
			Finalized = 0x08,
		}

		//Import of DLL functions. DO NOT USE until you know what you do (MediaInfo DLL do NOT use CoTaskMemAlloc to allocate memory)
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_New();
		[DllImport("MediaInfo")]
		private static extern void MediaInfo_Delete(IntPtr handle);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_Open(IntPtr handle, IntPtr fileName);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_Open_Buffer_Init(IntPtr handle, long fileSize, long fileOffset);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_Open_Buffer_Continue(IntPtr handle, IntPtr buffer, IntPtr bufferSize);
		[DllImport("MediaInfo")]
		private static extern long MediaInfo_Open_Buffer_Continue_GoTo_Get(IntPtr handle);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_Open_Buffer_Finalize(IntPtr handle);
		[DllImport("MediaInfo")]
		private static extern void MediaInfo_Close(IntPtr handle);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_Inform(IntPtr handle, IntPtr reserved);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_GetI(IntPtr handle, IntPtr streamType, IntPtr streamIndex, IntPtr parameter, IntPtr infoType);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_Get(IntPtr handle, IntPtr streamType, IntPtr streamIndex, IntPtr parameter, IntPtr infoType, IntPtr searchType);
		[DllImport("MediaInfo", CharSet = CharSet.Unicode)]
		private static extern IntPtr MediaInfo_Option(IntPtr handle, IntPtr option, IntPtr value);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_State_Get(IntPtr handle);
		[DllImport("MediaInfo")]
		private static extern IntPtr MediaInfo_Count_Get(IntPtr handle, IntPtr streamType, IntPtr streamIndex);
		#endregion

		public IntPtr Handle { get; private set; }
		public bool UsingUTF32Encoding { get; private set; }

		private static string UTF32PtrToString(IntPtr ptr) {
			var length = 0;
			while(Marshal.ReadInt32(ptr, length) != 0) length += 4;

			var buffer = new byte[length];
			Marshal.Copy(ptr, buffer, 0, buffer.Length);
			return new UTF32Encoding(!BitConverter.IsLittleEndian, false, false).GetString(buffer);
		}

		private static IntPtr StringToUTF32Ptr(string Str) {
			Encoding codec = new UTF32Encoding(!BitConverter.IsLittleEndian, false, false);
			var length = codec.GetByteCount(Str);
			var buffer = new byte[length + 4];
			codec.GetBytes(Str, 0, Str.Length, buffer, 0);
			var ptr = Marshal.AllocHGlobal(buffer.Length);
			Marshal.Copy(buffer, 0, ptr, buffer.Length);
			return ptr;
		}

		public MediaInfoLibNativeMethods() {
			try {
				Handle = MediaInfo_New();
			} catch {
				Handle = IntPtr.Zero;
			}
			if(Environment.OSVersion.ToString().InvIndexOf("Windows") == -1) {
				UsingUTF32Encoding = true;
				Option("setlocale_LC_CTYPE", "");
				Option("FileTestContinuousFileNames", "0");


			} else {
				UsingUTF32Encoding = false;
			}
		}

		public int Open(string fileName) {
			if(Handle == IntPtr.Zero) return 0;

			var fileNamePtr = UsingUTF32Encoding ? StringToUTF32Ptr(fileName) : Marshal.StringToHGlobalUni(fileName);
			var retVal = (int)MediaInfo_Open(Handle, fileNamePtr);
			Marshal.FreeHGlobal(fileNamePtr);
			return retVal;
		}

		public int OpenBufferInit(long fileSize, long fileOffset) {
			if(Handle == IntPtr.Zero) return 0;
			return (int)MediaInfo_Open_Buffer_Init(Handle, fileSize, fileOffset);
		}

		public int OpenBufferContinue(IntPtr buffer, IntPtr bufferSize) {
			if(Handle == IntPtr.Zero) return 0;
			return (int)MediaInfo_Open_Buffer_Continue(Handle, buffer, bufferSize);
		}

		public long OpenBufferContinueGotoGet() {
			if(Handle == IntPtr.Zero) return 0;
			return MediaInfo_Open_Buffer_Continue_GoTo_Get(Handle);
		}

		public int OpenBufferFinalize() {
			if(Handle == IntPtr.Zero) return 0;
			return (int)MediaInfo_Open_Buffer_Finalize(Handle);
		}

		public void Close() {
			if(Handle == IntPtr.Zero) return;
			MediaInfo_Close(Handle);
		}

		public string Inform() {
			if(Handle == IntPtr.Zero) return "Unable to load MediaInfo library";

			return UsingUTF32Encoding
				? UTF32PtrToString(MediaInfo_Inform(Handle, IntPtr.Zero))
				: Marshal.PtrToStringUni(MediaInfo_Inform(Handle, IntPtr.Zero));
		}

		public string Get(string parameter, StreamTypes streamType = StreamTypes.General, int streamIndex = 0, InfoTypes infoType = InfoTypes.Text, InfoTypes searchType = InfoTypes.Name) {
			if(Handle == IntPtr.Zero) return "Unable to load MediaInfo library";

			if(UsingUTF32Encoding) {
				var parameterPtr = StringToUTF32Ptr(parameter);
				var retVal = UTF32PtrToString(MediaInfo_Get(Handle, (IntPtr)streamType, (IntPtr)streamIndex, parameterPtr, (IntPtr)infoType, (IntPtr)searchType));
				Marshal.FreeHGlobal(parameterPtr);
				return retVal;

			} else {
				var parameterPtr = Marshal.StringToHGlobalUni(parameter);
				var retVal = Marshal.PtrToStringUni(MediaInfo_Get(Handle, (IntPtr)streamType, (IntPtr)streamIndex, parameterPtr, (IntPtr)infoType, (IntPtr)searchType));
				Marshal.FreeHGlobal(parameterPtr);
				return retVal;
			}
		}

		public string Get(int parameter, StreamTypes streamType = StreamTypes.General, int streamIndex = 0, InfoTypes infoType = InfoTypes.Text) {
			if(Handle == IntPtr.Zero) return "Unable to load MediaInfo library";

			return UsingUTF32Encoding
				? UTF32PtrToString(MediaInfo_GetI(Handle, (IntPtr)streamType, (IntPtr)streamIndex, (IntPtr)parameter, (IntPtr)infoType))
				: Marshal.PtrToStringUni(MediaInfo_GetI(Handle, (IntPtr)streamType, (IntPtr)streamIndex, (IntPtr)parameter, (IntPtr)infoType));
		}
		public string Option(string option, string value = "") {
			if(Handle == IntPtr.Zero) return "Unable to load MediaInfo library";

			if(UsingUTF32Encoding) {
				var optionPtr = StringToUTF32Ptr(option);
				var valuePtr = StringToUTF32Ptr(value);
				var retVal = UTF32PtrToString(MediaInfo_Option(Handle, optionPtr, valuePtr));
				Marshal.FreeHGlobal(optionPtr);
				Marshal.FreeHGlobal(valuePtr);
				return retVal;

			} else {
				var optionPtr = Marshal.StringToHGlobalUni(option);
				var valuePtr = Marshal.StringToHGlobalUni(value);
				var retVal = Marshal.PtrToStringUni(MediaInfo_Option(Handle, optionPtr, valuePtr));
				Marshal.FreeHGlobal(optionPtr);
				Marshal.FreeHGlobal(valuePtr);
				return retVal;
			}
		}

		public int GetState() {
			if(Handle == IntPtr.Zero) return 0;
			return (int)MediaInfo_State_Get(Handle);
		}

		public int GetCount(StreamTypes streamType, int streamIndex = -1) {
			if(Handle == IntPtr.Zero) return 0;
			return (int)MediaInfo_Count_Get(Handle, (IntPtr)streamType, (IntPtr)streamIndex);
		}

		public void Dispose() {
			if(Handle == IntPtr.Zero) return;
			MediaInfo_Delete(Handle);
		}
	}


	public class MediaInfoLibProvider : MediaProvider {
		private void Populate(MediaInfoLibNativeMethods mil) {
			static string removeNonNumerics(string s) => Regex.Replace(s, "[^-,.0-9]", "");
			static string splitTakeFirst(string s) => s.Split('\\', '/', '|')[0];
			static string skipIntDefault(string s) => string.IsNullOrEmpty(s.Trim('0', '.', ',')) ? "" : s;

			//string nonEmpty(string a, string b) => string.IsNullOrEmpty(a) ? b : a;

			Add(FileSizeType, () => mil.Get("FileSize"), s => s.ToInvInt64(), splitTakeFirst, removeNonNumerics);
			Add(DurationType, () => mil.Get("Duration"), s => s.ToInvDouble() / 1000, splitTakeFirst, removeNonNumerics);
			Add(FileExtensionType, () => mil.Get("FileExtension"), s => s.ToUpperInvariant(), splitTakeFirst); //TODO: Add multiple if multiple
			Add(WritingAppType, () => mil.Get("Encoded_Application"));
			Add(MuxingAppType, () => mil.Get("Encoded_Library"));


			bool hasAudio = false, hasVideo = false, hasSubtitle = false;
			foreach(var streamType in new[] { MediaInfoLibNativeMethods.StreamTypes.Video, MediaInfoLibNativeMethods.StreamTypes.Audio, MediaInfoLibNativeMethods.StreamTypes.Text }) {
				var streamCount = mil.GetCount(streamType);

				for(var streamIndex = 0; streamIndex < streamCount; streamIndex++) {
					string streamGet(string key) => new string(mil.Get(key, streamType, streamIndex)?.Trim().Where(c => XmlConvert.IsXmlChar(c)).ToArray() ?? Array.Empty<char>()); //TODO

					ulong? id = null;
					if(!string.IsNullOrEmpty(streamGet("UniqueID"))) {
						id = streamGet("UniqueID").ToInvUInt64();
					}
					if(!id.HasValue && !string.IsNullOrEmpty(streamGet("ID"))) {
						id = streamGet("ID").InvReplace("-", "000").ToInvUInt64();
					}

					MetaInfoContainer stream;
					switch(streamType) {
						case MediaInfoLibNativeMethods.StreamTypes.Video:
							stream = new MetaInfoContainer(id ?? (ulong)Nodes.Count(x => x.Type == ChaptersType), VideoStreamType); hasVideo = true;
							Add(stream, MediaStream.StatedSampleRateType, () => streamGet("FrameRate"), s => s.ToInvDouble(), skipIntDefault);
							Add(stream, MediaStream.SampleCountType, () => streamGet("FrameCount").ToInvInt64());
							Add(stream, VideoStream.PixelAspectRatioType, () => streamGet("PixelAspectRatio").ToInvDouble());
							Add(stream, VideoStream.PixelDimensionsType, () => new Dimensions(streamGet("Width").ToInvInt32(), streamGet("Height").ToInvInt32()));
							Add(stream, VideoStream.DisplayAspectRatioType, () => streamGet("DisplayAspectRatio").ToInvDouble());
							Add(stream, VideoStream.ColorBitDepthType, () => streamGet("BitDepth").ToInvInt32());

							Add(stream, MediaStream.AverageSampleRateType, () => streamGet("FrameRate_Mode").InvEqualsOrdCI("VFR") ? streamGet("FrameRate").ToInvDouble() : default);
							Add(stream, MediaStream.MaxSampleRateType, () => streamGet("FrameRate_Maximum").ToInvDouble());
							Add(stream, MediaStream.MinSampleRateType, () => streamGet("FrameRate_Minimum").ToInvDouble());

							Add(stream, VideoStream.IsInterlacedType, () => streamGet("ScanType").InvEqualsOrdCI("Interlaced"));
							Add(stream, VideoStream.HasVariableFrameRateType, () => streamGet("FrameRate_Mode").InvEqualsOrdCI("VFR"));
							Add(stream, VideoStream.ChromaSubsamplingType, () => new ChromeSubsampling(streamGet("ChromaSubsampling")));
							//Add(stream, VideoStream.ColorSpaceType, () => streamGet("ColorSpace"));
							AddNode(stream);
							break;

						case MediaInfoLibNativeMethods.StreamTypes.Audio:
							stream = new MetaInfoContainer(id ?? (ulong)Nodes.Count(x => x.Type == AudioStreamType), AudioStreamType); hasAudio = true;
							Add(stream, MediaStream.StatedSampleRateType, () => streamGet("SamplingRate"), s => s.ToInvDouble(), skipIntDefault);
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

					if(streamType == MediaInfoLibNativeMethods.StreamTypes.Video || streamType == MediaInfoLibNativeMethods.StreamTypes.Audio) {
						Add(stream, MediaStream.BitrateType, () => streamGet("BitRate"), s => s.ToInvDouble(), skipIntDefault);
						Add(stream, MediaStream.StatedBitrateModeType, () => streamGet("BitRate_Mode"));
					}

					Add(stream, MediaStream.SizeType, () => streamGet("StreamSize").ToInvInt64());
					Add(stream, MediaStream.TitleType, () => streamGet("Title"));
					Add(stream, MediaStream.IsForcedType, () => streamGet("Forced").InvEqualsOrdCI("yes"));
					Add(stream, MediaStream.IsDefaultType, () => streamGet("Default").InvEqualsOrdCI("yes"));
					Add(stream, MediaStream.IdType, () => streamGet("UniqueID").ToInvUInt64());
					Add(stream, MediaStream.LanguageType, () => streamGet("Language"));
					Add(stream, MediaStream.DurationType, () => streamGet("Duration"), s => TimeSpan.FromSeconds(s.ToInvDouble() / 1000), (Func<string, string>)splitTakeFirst);
					Add(stream, MediaStream.ContainerCodecIdWithCodecPrivateType, () => streamGet("CodecID"));
					Add(stream, MediaStream.CodecIdType, () => streamGet("Format"));
					Add(stream, MediaStream.CodecAdditionalFeaturesType, () => streamGet("Format_AdditionalFeatures"));
					Add(stream, MediaStream.CodecCommercialIdType, () => streamGet("Format_Commercial"));
					Add(stream, MediaStream.CodecProfileType, () => streamGet("Format_Profile"));
					Add(stream, MediaStream.CodecVersionType, () => streamGet("Format_Version"));
					Add(stream, MediaStream.CodecNameType, () => streamGet("Format-Info"));
					Add(stream, MediaStream.EncoderSettingsType, () => streamGet("Encoded_Library_Settings"));
					Add(stream, MediaStream.EncoderNameType, () => streamGet("Encoded_Library"));
				}
			}

			AddSuggestedFileExtension(mil, hasAudio, hasVideo, hasSubtitle);

			var menuStreamCount = mil.GetCount(MediaInfoLibNativeMethods.StreamTypes.Menu);
			for(var i = 0; i < menuStreamCount; i++) {
				try {
					PopulateChapters(mil, i);
				} catch(Exception) { }
			}
		}

		private void PopulateChapters(MediaInfoLibNativeMethods mil, int streamIndex) {
			var menuType = MediaInfoLibNativeMethods.StreamTypes.Menu;
			var chapters = new MetaInfoContainer((ulong)streamIndex, ChaptersType);

			static ulong conv(string str) {
				var timeParts = str.Split(new char[] { ':', '.' }).Select(s => s.Trim().ToInvUInt64()).ToArray();
				return (((timeParts[0] * 60 + timeParts[1]) * 60 + timeParts[2]) * 1000 + timeParts[3]) * 1000000;
			}

			var format = mil.Get("Format", menuType, streamIndex);
			var languageChapters = mil.Get("Language", menuType, streamIndex);
			Add(chapters, Chapters.FormatType, (format + " -- " + (string.IsNullOrEmpty(format) ? "nero" : "mov")).Trim());

			var entryCount = mil.GetCount(MediaInfoLibNativeMethods.StreamTypes.Menu, streamIndex);
			if(int.TryParse(mil.Get("Chapters_Pos_Begin", menuType, streamIndex), out var indexStart) && int.TryParse(mil.Get("Chapters_Pos_End", menuType, streamIndex), out var indexEnd)) {

				//MIL Offset Bug workaround
				var offsetFixTries = 20;
				while(offsetFixTries-- > 0 && !mil.Get(indexStart, menuType, streamIndex, MediaInfoLibNativeMethods.InfoTypes.Name).Split('-').All(x => x.Contains(':', StringComparison.Ordinal))) {
					indexStart++;
					indexEnd++;
				}
				if(offsetFixTries == 0) {
					return;
				}

				for(; indexStart < indexEnd; indexStart++) {
					var chapter = new MetaInfoContainer((ulong)indexStart, Chapters.ChapterType);
					chapters.AddNode(chapter);

					var timeStamps = mil.Get(indexStart, menuType, streamIndex, MediaInfoLibNativeMethods.InfoTypes.Name).Split('-');
					var timeStamp = conv(timeStamps[0].Trim());

					Add(chapter, Chapter.TimeStartType, timeStamp / 1000d);
					//Add(chapter, Chapter.TimeEndType, );

					var title = mil.Get(indexStart, menuType, streamIndex);

					var languages = new List<string>();
					if((uint)title.InvIndexOf(":") < 5) {
						var language = title.InvContains(":") ? title.Substring(0, title.InvIndexOf(":")) : "";
						if(!string.IsNullOrEmpty(language)) languages.Add(language);
						title = title.Substring(language.Length + 1);

					} else if(!string.IsNullOrEmpty(languageChapters)) {
						languages.Add(languageChapters);
					}

					Add(chapter, Chapter.TitlesType, ImmutableArray.Create(new ChapterTitle(title, languages, Array.Empty<string>())));
				}
			}

			AddNode(chapters);
		}


		private void AddSuggestedFileExtension(MediaInfoLibNativeMethods mil, bool hasAudio, bool hasVideo, bool hasSubtitle) {
			var fileExt = (mil.Get("FileExtension") ?? "").ToInvLower();
			var milInfo = (mil.Get("Format/Extensions") ?? "").ToInvLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var milInfoCommercial = (mil.Get("Format_Commercial") ?? "").ToInvLower();


			if(milInfo.Contains("asf") && milInfo.Contains("wmv") && milInfo.Contains("wma")) {
				if(!hasVideo && hasAudio && !hasSubtitle) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("wma"));
				} else {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("wmv"));
				}
			} else if(milInfo.Contains("ts") && milInfo.Contains("m2t")) {
				if(fileExt.Equals("ts", StringComparison.OrdinalIgnoreCase)) Add(SuggestedFileExtensionType, ImmutableArray.Create("ts")); //Blame worf

			} else if(milInfo.Contains("mpeg") && milInfo.Contains("mpg")) {
				if(!hasVideo && !hasAudio && hasSubtitle) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("sub"));
				} else {
					Add(SuggestedFileExtensionType, ImmutableArray.Create(fileExt.Equals("mpeg", StringComparison.OrdinalIgnoreCase) ? "mpeg" : "mpg"));
				}
			} else if(milInfo.Contains("mp1") && milInfo.Contains("mp2") && milInfo.Contains("mp3")) {
				switch(mil.Get("Format_Profile", MediaInfoLibNativeMethods.StreamTypes.Audio)) {
					case "Layer 1": Add(SuggestedFileExtensionType, ImmutableArray.Create("mp1")); break;
					case "Layer 2": Add(SuggestedFileExtensionType, ImmutableArray.Create("mp2")); break;
					case "Layer 3": Add(SuggestedFileExtensionType, ImmutableArray.Create("mp3")); break;
					default: Add(SuggestedFileExtensionType, ImmutableArray.Create(milInfo[0])); break;
				}


			} else if(milInfo.Contains("mp4") && milInfo.Contains("m4a") && milInfo.Contains("m4v")) {
				if(!hasVideo && hasAudio && !hasSubtitle) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("m4a"));
				} else if(hasVideo && !hasAudio && !hasSubtitle) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("m4v"));
				} else {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("mp4"));
				}

			} else if(milInfo.Contains("dts") || milInfo.Contains("thd")) {
				if(milInfoCommercial.InvContains("dts")) {
					if(milInfoCommercial.InvContains("dts-hd")) {
						Add(SuggestedFileExtensionType, ImmutableArray.Create("dtshd"));
					} else {
						Add(SuggestedFileExtensionType, ImmutableArray.Create("dts"));
					}
				} else {
					milInfo = mil.Get("Audio_Codec_List").ToInvLower().Split(' ');
					if(milInfo.Contains("truehd")) {
						Add(SuggestedFileExtensionType, ImmutableArray.Create("thd"));
					} else {
						Add(SuggestedFileExtensionType, ImmutableArray.Create("dts"));
					}
				}
			} else if(milInfo.Contains("mlp") || milInfo.Length == 0 || string.IsNullOrEmpty(milInfo[0])) {
				if(milInfoCommercial.InvContains("truehd")) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("thd"));
				} else if(milInfoCommercial.InvContains("mlp")) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("mlp"));
				}


			} else if(milInfo.Contains("mkv")) {
				if(hasVideo) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("mkv"));
				} else if(hasAudio) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("mka"));
				} else if(hasSubtitle) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("mks"));
				}

				if(!hasVideo && hasAudio && !hasSubtitle) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("m4a"));
				} else if(hasVideo && !hasAudio && !hasSubtitle) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("m4v"));
				} else {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("mp4"));
				}
			}

			if(milInfo.Contains("ts")) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("ts"));

			} else if(milInfo.Contains("m2ts") || milInfo.Contains("m2t")) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("m2ts"));

			} else if(milInfo.Contains("wav")) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("wav"));

			} else if(milInfo.Contains("m4v")) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("m4v"));

			} else if(milInfo.Contains("avc")) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create("avc"));
			}



			if(Select(SuggestedFileExtensionType) == null) {
				if(milInfo.Contains("rm") || milInfo.Contains("rmvb")) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("rm"));
				} else if(milInfo.Contains("asf") || milInfo.Contains("wmv") /*|| milInfo.Contains("wma")*/) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("wmv"));
				} else if(milInfo.Contains("mov") || milInfo.Contains("qt")) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("mov"));
				} else if(milInfo.Contains("aac") || milInfo.Contains("aacp") || milInfo.Contains("adts")) {
					Add(SuggestedFileExtensionType, ImmutableArray.Create("aac"));
				}
			}

			if(Select(SuggestedFileExtensionType) == null && milInfo.Length > 0) {
				Add(SuggestedFileExtensionType, ImmutableArray.Create(milInfo));
			}
		}

		public MediaInfoLibProvider(string filePath)
			: base("MediaInfoLibProvider") {

			using var mil = new MediaInfoLibNativeMethods();
			if(File.Exists(filePath)) {
				var retVal = mil.Open(filePath);

				if(retVal == 1) {
					Populate(mil);
				} else {
					throw new InvalidOperationException("MediaInfoLib couldn't open the file");
				}
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
