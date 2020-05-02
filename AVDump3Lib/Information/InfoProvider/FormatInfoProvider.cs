using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static AVDump3Lib.Information.InfoProvider.MediaInfoLibNativeMethods;

namespace AVDump3Lib.Information.InfoProvider {
	public class FormatInfoProvider : MetaDataProvider {
		public FormatInfoProvider(string filePath) : base("FormatInfoProvider", MediaProvider.MediaProviderType) {
			var fileExtensionProvider = new FileExtensionProvider();
			fileExtensionProvider.AddMetaData(this, filePath);

		}


		public static readonly MetaInfoContainerType FormatInfoProviderType = new MetaInfoContainerType("FormatInfoProvider");

	}

	public interface IMagicBytePattern {

	}
	public interface IFormatPattern {

	}



	//TODO: Complete rewrite
	public class FileExtensionProvider {
		private static byte[] Conv(string str) => Encoding.ASCII.GetBytes(str);


		public void AddMetaData(MetaDataProvider provider, string path) {
			var fileTypes = new IFileType[] {
				//new MpegAudioFileType(),
				//new MpegVideoFileType(),
				//new WavFileType(),
				new SrtFileType(),
				new AssSsaFileType(),
				new IdxFileType(),
				new SamiFileType(),
				new C7zFileType(),
				new ZipFileType(),
				new RarFileType(),
				new RaFileType(),
				new FlacFileType(),
				new LrcFileType(),
				new AviFileType(),
				new SubFileType(),
				new TmpFileType(),
				new PJSFileType(),
				new JSFileType(),
				new RTFileType(),
				new SMILFileType(),
				new TTSFileType(),
				new XSSFileType(),
				new ZeroGFileType(),
				new SUPFileType(),
				new FanSubberFileType(),
				new Sasami2kFileType()
			};

			using(Stream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
				int b;
				var needMoreBytes = true;
				while(needMoreBytes) {
					b = stream.ReadByte();
					if(b == -1 || stream.Position > 1 << 30) return;

					needMoreBytes = false;
					foreach(var fileType in fileTypes.Where(f => f.IsCandidate)) {
						fileType.CheckMagicByte((byte)b);
						needMoreBytes |= fileType.NeedsMoreMagicBytes;
					}
				}

				foreach(var fileType in fileTypes.Where(f => f.IsCandidate)) {
					stream.Position = 0;
					fileType.ElaborateCheck(stream);
				}
			}

			var exts = fileTypes.Where(f => f.IsCandidate);

			if(exts.Count() != 1) {
				if(exts.Any(ft => ft.PossibleExtensions[0].Equals("zip"))) exts = new List<IFileType>(new IFileType[] { new ZipFileType() });
				if(exts.Any(ft => ft.PossibleExtensions[0].Equals("7z"))) exts = new List<IFileType>(new IFileType[] { new C7zFileType() });
				if(exts.Any(ft => ft.PossibleExtensions[0].Equals("rar"))) exts = new List<IFileType>(new IFileType[] { new RarFileType() });
			}

			string extsStr;
			if(fileTypes.Any(f => f.IsCandidate)) {
				extsStr = exts.Aggregate<IFileType, IEnumerable<string>>(new string[0], (acc, f) => acc.Concat(f.PossibleExtensions)).Aggregate((acc, str) => acc + " " + str);
			} else {
				extsStr = null;
			}

			if(!string.IsNullOrEmpty(extsStr)) provider.Add(MediaProvider.SuggestedFileExtensionType, extsStr);
			if(exts.Count() == 1) {
				exts.Single().AddInfo(provider);
			}
		}

		public FileExtensionProvider() {



			//Add(EntryKey.Extension, extsStr, null);



			//if(exts.Count() == 1) exts.Single().AddInfo(Add);
		}

	}

	public class MpegAudioFileType : FileType {
		//public MpegAudioFileType() : base(new byte[][] { new byte[] { 0xff }, new byte[] { (byte)'I', (byte)'D', (byte)'3' } }) { }
		public MpegAudioFileType() : base("") => fileType = MediaProvider.AudioStreamType;

		private static int[,] bitRateTable = {
			{-1, -1 ,-1, -1, -1},
			{32,32,32,32,8},
			{64,48,40,48,16},
			{96,56,48,56,24},
			{128,64,56,64,32},
			{160,80,64,80,40},
			{192,96,80,96,48},
			{224,112,96,112,56},
			{256,128,112,128,64},
			{288,160,128,144,80},
			{320,192,160,160,96},
			{352,224,192,176,112},
			{384,256,224,192,128},
			{416,320,256,224,144},
			{448,384,320,256,160},
			{-1, -1 ,-1, -1, -1},
		};
		private static int[,] samplerateTable = {
			{44100,22050,11025},
			{48000,24000,12000},
			{32000,16000,8000},
			{-1,-1,-1},
		};

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;

			if(stream.Length > 10 && stream.ReadByte() == 'I' && stream.ReadByte() == 'D' && stream.ReadByte() == '3') {
				stream.Position += 3;

				var length = 0;
				for(var i = 0; i < 4; i++) {
					length += (byte)stream.ReadByte() << (7 * (3 - i));
				}
				stream.Position = length;
			} else {
				stream.Position = 0;
			}
			//stream.Position = Math.Max(stream.Length / 2 , stream.Position);

			var counter = 0;
			Layer layer = 0, layerTmp;
			for(var i = 0; i < 100; i++) if(ReadFrame(stream, out layer) && ReadFrame(stream, out layerTmp)) break;
			for(var i = 0; i < 10; i++) counter += ReadFrame(stream, out layerTmp) ? 1 : 0;


			if(counter < 7) { IsCandidate = false; return; }

			PossibleExtensions = new string[] { (layer == Layer.Layer1) ? "mp1" : ((layer == Layer.Layer2) ? "mp2" : "mp3") };
		}

		private bool ReadFrame(Stream stream, out Layer layer) {
			int oldByte = 0, newByte, counter = 0;
			while((newByte = stream.ReadByte()) != -1) {
				if(oldByte == 0xFF && (newByte & 0xE0) == 0xE0) break;
				oldByte = newByte;
				counter++;
			}

			var mpegVer = (MpegVer)newByte & MpegVer.MASK;
			layer = (Layer)newByte & Layer.MASK;
			if((newByte = stream.ReadByte()) == -1) return false;

			var rowIndex = (newByte & 0xF0) >> 4;
			var columnIndex = mpegVer == MpegVer.Mpeg1 ? (layer == Layer.Layer1 ? 0 : (layer == Layer.Layer2 ? 1 : 2)) : ((mpegVer == MpegVer.Mpeg2 || mpegVer == MpegVer.Mpeg2_5) ? ((layer == Layer.Layer1) ? 3 : 4) : -1);
			if(columnIndex == -1) return false;

			var bitrate = bitRateTable[rowIndex, columnIndex];
			if(bitrate == -1) return false;


			rowIndex = (newByte & 0x0C) >> 2;
			columnIndex = mpegVer == MpegVer.Mpeg1 ? 0 : (mpegVer == MpegVer.Mpeg2 ? 1 : 2);
			if(columnIndex == -1) return false;

			var sampleRate = samplerateTable[rowIndex, columnIndex];
			if(sampleRate == -1) return false;

			var padding = (newByte & 0x02) != 0;

			var frameLength = layer == Layer.Layer1 ? (12 * bitrate / sampleRate + (padding ? 1 : 0)) * 4 : 144000 * bitrate / sampleRate + (padding ? 1 : 0);
			//Debug.Print(layer.ToString());

			stream.Position += frameLength - 3;

			return counter <= 2 && frameLength != 0;
		}

		private enum MpegVer { Mpeg1 = 24, Mpeg2 = 16, Mpeg2_5 = 0, MASK = 0x18 }
		private enum Layer { Layer1 = 6, Layer2 = 4, Layer3 = 2, MASK = 0x06 }
	}
	public class SrtFileType : FileType {
		public SrtFileType() : base("", identifier: "text/srt") { PossibleExtensions = new string[] { "srt" }; fileType = MediaProvider.SubtitleStreamType; }

		private static Regex regexParse = new Regex(@"^(?<start>\d{1,4} ?\: ?\d{1,4} ?\: ?\d{1,4} ?[,:.] ?\d{1,4}) ?--\> ?(?<end>\d{1,4} ?\: ?\d{1,4} ?\: ?\d{1,4} ?[,:.] ?\d{1,4})", RegexOptions.Compiled | RegexOptions.ECMAScript);

		public override void ElaborateCheck(Stream stream) {
			if(stream.Length > 10 * 1024 * 1024) IsCandidate = false;
			if(!IsCandidate) return;

			//bool accept = false;
			var count = 0;
			//int lineType = 0, dummy;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);

			foreach(var line in ReadLines(sr, 1024)) {
				if(regexParse.IsMatch(line)) count++;
				if(count > 20) break;

				/*if(int.TryParse(line, out dummy)) {
					lineType = 1;
					continue;
				}
				if(string.IsNullOrEmpty(line)) {
					continue;
				}
				
				switch(lineType) {
					//case 0: IsCandidate = int.TryParse(line, out dummy); break;
					case 1: IsCandidate = regexParse.IsMatch(line); break;
					case 2: accept = true; break;
				}
				if(!IsCandidate) return;

				lineType++;
				count++;

				if(count > 20) break;*/
			}

			if(count == 0) IsCandidate = false;
		}
	}

	public class AssSsaFileType : FileType {
		public AssSsaFileType() : base("", identifier: "text/") { PossibleExtensions = new string[] { "ssa", "ass" }; fileType = MediaProvider.SubtitleStreamType; }
		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;

			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			var chars = new char[2048];
			var length = sr.Read(chars, 0, chars.Length);
			var str = new string(chars, 0, length).ToLowerInvariant();

			//int pos = str.IndexOf("[script info]");
			//if(pos < 0) { IsCandidate = false; return; }

			var pos = str.IndexOf(" styles]");
			if(pos != -1) {
				pos = str.IndexOf("v4", pos - 4, 10);
				if(pos != -1) pos += 2;
			}

			if(pos == -1) {
				pos = str.IndexOf(" styles]");
				if(pos != -1) {
					pos = str.IndexOf("v3", pos - 4, 10);
					if(pos != -1) pos += 2;
				}
			}

			if(pos == -1) {
				pos = str.IndexOf("scripttype:");
				if(pos < 0) { IsCandidate = false; return; }
				if((pos = str.IndexOf("v4.00", pos, 20)) < 0) { IsCandidate = false; return; }
				pos += 5;
			}
			if(pos == -1) {
				pos = str.IndexOf("scripttype:");
				if(pos < 0) { IsCandidate = false; return; }
				if((pos = str.IndexOf("v3.00", pos, 20)) < 0) { IsCandidate = false; return; }
				pos += 5;
			}


			if(stream.Length > pos + 2) {
				PossibleExtensions = new string[] { str[pos] == '+' ? "ass" : "ssa" };
				identifier += PossibleExtensions[0];
			} else {
				IsCandidate = false;
			}
		}
	}
	public class IdxFileType : FileType {
		private IDX idx;

		public IdxFileType() : base(Array.Empty<byte>(), identifier: "text/idx") { PossibleExtensions = new string[] { "idx" }; fileType = MediaProvider.SubtitleStreamType; }
		public override void ElaborateCheck(Stream stream) {
			if(stream.Length > 10 * 1024 * 1024) IsCandidate = false;
			if(!IsCandidate) return;

			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			var line = ReadLines(sr, 1024).FirstOrDefault() ?? "";
			IsCandidate = line.Contains("VobSub index file, v");

			if(IsCandidate) {
				stream.Position = 0;
				sr.DiscardBufferedData();
				try {
					idx = new IDX(sr.ReadToEnd());

				} catch { }
			}
		}

		public override void AddInfo(MetaDataProvider provider) {
			if(idx != null) {
				for(var i = 0; i < idx.Subtitles.Length; i++) {
					if(idx.Subtitles[i].SubtitleCount == 0) continue;

					var container = new MetaInfoContainer((ulong)i, fileType);
					provider.AddNode(container);
					provider.Add(container, MediaStream.ContainerCodecIdType, identifier);
					provider.Add(container, MediaStream.LanguageType, idx.Subtitles[i].language);
					provider.Add(container, MediaStream.IndexType, idx.Subtitles[i].index);
				}

			} else {
				base.AddInfo(provider);
			}
		}
	}
	public class LrcFileType : FileType { //http://en.wikipedia.org/wiki/LRC_(file_format)
		public LrcFileType() : base("", identifier: "text/lyric") { PossibleExtensions = new string[] { "lrc" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			int i = 0, matches = 0;
			var regex = new Regex(@"\[\d\d\:\d\d\.\d\d\].*");
			foreach(var line in ReadLines(sr, 1024)) {
				//if(line == null) break;
				matches += regex.IsMatch(line) ? 1 : 0;
				i++;
			}
			if(matches / (double)i < 0.8 || matches == 0) { IsCandidate = false; return; }
		}
	}
	public class TmpFileType : FileType {
		public TmpFileType() : base("", identifier: "text/TMPlayer") { PossibleExtensions = new string[] { "tmp" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			int i = 0, matches = 0;
			var regex = new Regex(@"^\d\d\:\d\d\:\d\d(\.\d)?\:.*");
			foreach(var line in ReadLines(sr, 1024)) {
				//if(line == null) break;
				matches += regex.IsMatch(line) ? 1 : 0;
				i++;
			}
			if(matches / (double)i < 0.8 || matches == 0) { IsCandidate = false; return; }
		}
	}
	public class PJSFileType : FileType {
		public PJSFileType() : base("", identifier: "text/PhoenixJapanimationSociety") { PossibleExtensions = new string[] { "pjs" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			int i = 0, matches = 0;
			var regex = new Regex(@"^\s*\d*,\s*\d*," + "\".*\"");
			var checkLine = "";
			foreach(var line in ReadLines(sr, 1024)) {
				checkLine += line;
				if(!line.Contains(",\"") || line.EndsWith("\"")) {
					matches += regex.IsMatch(checkLine) ? 1 : 0;
					checkLine = "";
					i++;
				}
			}
			if(matches / (double)i < 0.8 || matches == 0) { IsCandidate = false; return; }
		}
	}
	public class JSFileType : FileType {
		public JSFileType() : base("", identifier: "text/JS") { PossibleExtensions = new string[] { "js" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			int i = 0, matches = 0;
			var regex = new Regex(@"^\d*\:\d*\:\d*\.\d*\s*\d*\:\d*\:\d*\.\d*.*");
			var hasContinuation = false;
			var hasMagicString = false;
			foreach(var line in ReadLines(sr, 1024)) {
				if(line == null) break;
				hasMagicString |= line.ToLowerInvariant().Contains("jaco");
				if(line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;

				var isStartMatch = regex.IsMatch(line);
				matches += hasContinuation || isStartMatch ? 1 : 0;
				hasContinuation = (isStartMatch && line.EndsWith("\\")) || (hasContinuation && line.EndsWith("\\"));
				i++;
			}

			var isMatch = matches / (double)i > 0.5 && hasMagicString;
			isMatch |= matches / (double)i > 0.8;

			if(!isMatch || matches == 0) { IsCandidate = false; return; }
		}
	}
	public class TTSFileType : FileType {
		public TTSFileType() : base("", identifier: "text/TTS") { PossibleExtensions = new string[] { "tts" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			int i = 0, matches = 0;
			var regex = new Regex(@"^\d*\:\d*\:\d*\.\d*,\d*\:\d*\:\d*\.\d*,.*");
			foreach(var line in ReadLines(sr, 1024).Select(ldLine => ldLine.Trim())) {
				//if(line == null) break;
				if(line.StartsWith("#") || string.IsNullOrEmpty(line)) continue;

				var isMatch = regex.IsMatch(line);
				matches += isMatch ? 1 : 0;
				if(!isMatch) {
					if(!string.IsNullOrEmpty(line) && line.Average(ldChar => char.IsLetter(ldChar) || char.IsWhiteSpace(ldChar) || char.IsPunctuation(ldChar) ? 1 : 0) > 0.8) continue;
				}
				i++;
			}
			if(matches / (double)i < 0.8 || matches == 0) { IsCandidate = false; return; }
		}
	}
	public class RTFileType : FileType {
		public RTFileType() : base("", identifier: "text/RT") { PossibleExtensions = new string[] { "rt" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			int i, j = 0, matches = 0;
			foreach(var line in ReadLines(sr, 1024).Select(ldLine => ldLine.ToLower())) {
				//if(line == null) break;

				if(string.IsNullOrEmpty(line) || !line.StartsWith("<")) continue;

				j++;

				matches += (line.StartsWith("<window") || line.StartsWith("<time begin")) ? 1 : 0;
			}
			if(matches / (double)j < 0.6 || matches == 0 || j < 10) { IsCandidate = false; return; }
		}
	}
	public class XSSFileType : FileType {
		public XSSFileType() : base("", identifier: "text/XombieSub") { PossibleExtensions = new string[] { "xss" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;

			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			var chars = new char[2048];
			var length = sr.Read(chars, 0, chars.Length);
			var str = new string(chars, 0, length).ToLowerInvariant();
			IsCandidate = str.Contains("script=xombiesub");
		}
	}
	public class SUPFileType : FileType {
		public SUPFileType() : base("", identifier: "text/SubtitleBitmapFile") { PossibleExtensions = new string[] { "sup" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;

			var startingBytes = new byte[12];
			stream.Read(startingBytes, 0, 12);
			IsCandidate &= startingBytes[0] == 0x50;
			IsCandidate &= startingBytes[1] == 0x47;
			IsCandidate &= startingBytes[10] == 0x16;
			IsCandidate &= startingBytes[11] == 0x00;
		}
	}
	public class SMILFileType : FileType {
		public SMILFileType() : base("", identifier: "SMIL") { PossibleExtensions = new string[] { "smil" }; fileType = MediaProvider.MediaStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, false, 2048);
			int i = 0, matches = 0;
			foreach(var line in ReadLines(sr, 1024).Select(ldLine => ldLine.ToLower().Trim())) {
				if(line == null) break;
				if(line.StartsWith("<smil>")) matches++;
				i++;
			}
			if(matches == 0) { IsCandidate = false; return; }
		}
	}
	public class FanSubberFileType : FileType {
		public FanSubberFileType() : base("FanSubber v", identifier: "text/FanSubber") { PossibleExtensions = new string[] { "fsb" }; fileType = MediaProvider.SubtitleStreamType; }

		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, false, 2048);
			var line = ReadLines(sr, 1024).FirstOrDefault() ?? "";

			IsCandidate &= Regex.IsMatch(line, @"FanSubber v[0-9]+(\.[0-9]+)?");
		}
	}

	public class Sasami2kFileType : FileType { public Sasami2kFileType() : base("// translated by Sami2Sasami", identifier: "text/Sasami2k") { PossibleExtensions = new string[] { "s2k" }; fileType = MediaProvider.SubtitleStreamType; } }

	public class MpegVideoFileType : FileType { public MpegVideoFileType() : base(new byte[] { 0x00, 0x00, 0x01, 0xB3 }) { PossibleExtensions = new string[] { "mpg" }; fileType = MediaProvider.VideoStreamType; } }
	public class SamiFileType : FileType {
		public SamiFileType() : base("", identifier: "text/sami") { PossibleExtensions = new string[] { "smi" }; fileType = MediaProvider.SubtitleStreamType; }
		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			var chars = new char[2048];
			var length = sr.Read(chars, 0, chars.Length);
			var str = new string(chars, 0, length).ToLowerInvariant();

			if(str.IndexOf("<sami>", 0, Math.Min(10, str.Length)) < 0) { IsCandidate = false; return; }
		}
	}
	public class SubFileType : FileType { //http://forum.doom9.org/archive/index.php/t-81059.html
		public SubFileType() : base("", identifier: "text/") { PossibleExtensions = new string[] { "sub" }; fileType = MediaProvider.SubtitleStreamType; }
		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;

			if(Subviewer(stream)) identifier += "subviewer";
			else if(MicroDVD(stream)) identifier += "microdvd";
			else IsCandidate = false;
		}

		private bool Subviewer(Stream stream) {
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			var chars = new char[1024];
			var length = sr.Read(chars, 0, chars.Length);
			var str = new string(chars, 0, length).ToUpperInvariant();

			var matches = 0;
			string[] keys = { "[BEGIN]", "[INFORMATION]", "[TITLE]", "[AUTHOR]", "[SOURCE]", "[PRG]", "[FILEPATH]", "[DELAY]", "[CD TRACK]", "[COMMENT]", "[END INFORMATION]", "[SUBTITLE]", "[COLF]", "[STYLE]", "[SIZE]", "[FONT]" };
			foreach(var key in keys) matches += str.Contains(key) ? 1 : 0;


			if(matches < 5) return false;
			//identifier += "subviewer";
			return true;
		}
		private bool MicroDVD(Stream stream) {
			if(stream.Length > 10 * 1024 * 1024) return false;
			stream.Position = 0;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);

			int count = 0, matches = 0;
			var regex = new Regex(@"^\{\d*\}\{\d*\}.*$");
			foreach(var line in ReadLines(sr, 1024).Select(ldLine => ldLine.Trim())) {
				if(line.Equals("")) continue;
				if(regex.IsMatch(line)) matches++;
				count++;
				if(count > 20) break;
			}
			return matches / (double)count > 0.8 && count > 4;
		}
	}
	public class ZeroGFileType : FileType {
		public ZeroGFileType() : base("", identifier: "text/ZeroG") {
			PossibleExtensions = new string[] { "zeg" }; fileType = MediaProvider.SubtitleStreamType;
		}
		public override void ElaborateCheck(Stream stream) {
			if(!IsCandidate) return;
			var sr = new StreamReader(stream, Encoding.UTF8, true, 2048);
			var chars = new char[2048];
			var length = sr.Read(chars, 0, chars.Length);
			var str = new string(chars, 0, length).ToLowerInvariant();

			if(str.IndexOf("% zerog") < 0) { IsCandidate = false; return; }
		}
	}
	public class C7zFileType : FileType { public C7zFileType() : base(new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, identifier: "compression/7z") => PossibleExtensions = new string[] { "7z" }; }
	public class ZipFileType : FileType { public ZipFileType() : base(new byte[][] { new byte[] { 0x50, 0x4b, 0x03, 0x04 }, new byte[] { 0x50, 0x4b, 0x30, 0x30, 0x50, 0x4b } }, identifier: "compression/zip") => PossibleExtensions = new string[] { "zip" }; }
	public class RarFileType : FileType { public RarFileType() : base(new byte[] { 0x52, 0x61, 0x72, 0x21 }, identifier: "compression/rar") => PossibleExtensions = new string[] { "rar" }; }
	public class RaFileType : FileType { public RaFileType() : base(".ra" + (char)0xfd) { PossibleExtensions = new string[] { "ra" }; fileType = MediaProvider.AudioStreamType; } }
	public class FlacFileType : FileType { public FlacFileType() : base("fLaC") { PossibleExtensions = new string[] { "flac" }; fileType = MediaProvider.AudioStreamType; } }
	public class AviFileType : FileType { public AviFileType() : base("RIFF") { PossibleExtensions = new string[] { "avi" }; fileType = MediaProvider.VideoStreamType; } public override void ElaborateCheck(Stream stream) => IsCandidate &= Check(stream, 8, "AVI LIST"); }
	public class WavFileType : FileType { public WavFileType() : base(new string[] { "RIFX", "RIFF" }) => PossibleExtensions = new string[] { "wav" }; public override void ElaborateCheck(Stream stream) => IsCandidate &= Check(stream, 8, "WAVE"); }

	public abstract class FileType : IFileType {
		private byte[][] magicBytesLst; 
		private int[] magicBytesPos; 
		private int offset;

		protected string identifier;
		protected MetaInfoContainerType fileType = MediaProvider.MediaStreamType;

		public bool NeedsMoreMagicBytes { get; private set; }
		public bool IsCandidate { get; protected set; }
		public string[] PossibleExtensions { get; protected set; }

		public FileType(string magicString, int offset = 0, string identifier = null) : this(new string[] { magicString }, offset, identifier) { }
		public FileType(byte[] magicBytes, int offset = 0, string identifier = null) : this(new byte[][] { magicBytes }, offset, identifier) { }
		public FileType(string[] magicStringLst, int offset = 0, string identifier = null) : this(magicStringLst.Select(magicString => Encoding.ASCII.GetBytes(magicString)).ToArray(), offset, identifier) { }
		public FileType(byte[][] magicBytesLst, int offset = 0, string identifier = null) {
			this.magicBytesLst = magicBytesLst;
			this.offset = offset;

			this.identifier = identifier;

			magicBytesPos = new int[magicBytesLst.Length];

			IsCandidate = true;

			foreach(var magicBytes in magicBytesLst) NeedsMoreMagicBytes |= magicBytes.Length != 0;
		}

		public void CheckMagicByte(byte b) {
			if(!NeedsMoreMagicBytes || offset-- > 0) return;

			bool needsMoreMagicBytes = false, isCandidate = false;
			for(var i = 0; i < magicBytesLst.Length; i++) {
				if(magicBytesLst[i] == null) {
					isCandidate = true;
				} else if(magicBytesLst[i].Length > magicBytesPos[i]) {
					isCandidate |= magicBytesLst[i][magicBytesPos[i]++] == b;
					if(magicBytesLst[i].Length == magicBytesPos[i] && isCandidate) magicBytesLst[i] = null;
				}
				needsMoreMagicBytes |= isCandidate && (magicBytesLst[i] != null && magicBytesLst[i].Length != magicBytesPos[i]);
			}

			NeedsMoreMagicBytes &= needsMoreMagicBytes;
			IsCandidate &= isCandidate;
		}

		public virtual void ElaborateCheck(Stream stream) { }

		protected static bool Check(Stream stream, int totalOffset, string str) => Check(stream, totalOffset, Encoding.ASCII.GetBytes(str));
		protected static bool Check(Stream stream, int totalOffset, byte[] b) {
			var isValid = true;

			stream.Position = totalOffset;
			foreach(var item in b) isValid &= item == stream.ReadByte();
			return isValid;
		}

		protected static bool FindSequence(Stream stream, string sequenceStr, int bytesToCheck) => FindSequence(stream, Encoding.ASCII.GetBytes(sequenceStr), 2048);
		protected static bool FindSequence(Stream stream, byte[] sequence, int bytesToCheck) {
			var sequencePos = 0;
			while(stream.Position != stream.Length && bytesToCheck-- != 0 && sequence.Length != sequencePos) if(sequence[sequencePos++] != stream.ReadByte()) sequencePos = 0;
			return sequence.Length == sequencePos;
		}

		public override string ToString() => base.ToString() + " IsCandidate " + IsCandidate;

		public virtual void AddInfo(MetaDataProvider provider) {
			if(!string.IsNullOrEmpty(identifier)) {
				var container = new MetaInfoContainer(0, fileType);
				provider.AddNode(container);

				provider.Add(container, MediaStream.ContainerCodecIdType, identifier);
			}
		}


		protected static IEnumerable<string> ReadLines(StreamReader streamReader, int maxLineLength) {
			var currentLine = new StringBuilder(maxLineLength);
			int i;
			while((i = streamReader.Read()) > -1) {
				var c = (char)i;
				if(c == '\r' || c == '\n') {
					if(currentLine.Length > 0) {
						yield return currentLine.ToString();
					}
					currentLine.Length = 0;
					continue;
				}
				currentLine.Append(c);
				if(currentLine.Length > maxLineLength) {
					yield break;
				}
			}
			if(currentLine.Length > 0) {
				yield return currentLine.ToString();
			}
		}
	}

	public interface IFileType {
		bool IsCandidate { get; }
		bool NeedsMoreMagicBytes { get; }
		string[] PossibleExtensions { get; }

		void ElaborateCheck(Stream stream);
		void CheckMagicByte(byte b);

		void AddInfo(MetaDataProvider provider);
	}





	public class IDX {
		private Dictionary<string, string> info;
		private Subs[] subtitles;

		public Subs[] Subtitles {
			get { return (Subs[])subtitles.Clone(); }
		}

		public IDX(string source) {
			info = new Dictionary<string, string>();
			Parse(source);
		}

		private static Regex tagRegex = new Regex(@"^([^#]+?)\:[ ]?" + "([^\r\n]+)", RegexOptions.Compiled | RegexOptions.ECMAScript | RegexOptions.Multiline);
		private void Parse(string source) {
			MatchCollection matches = tagRegex.Matches(source);

			Match match, subMatch;

			int index;
			string language;
			string languageId;
			TimeSpan? delay = null;
			List<TimeSpan> sub = null;
			List<Subs> subs = new List<Subs>();

			for(int i = 0; i < matches.Count; i++) {
				match = matches[i];

				string key = match.Groups[1].Value.Trim();
				if(key.Equals("timestamp")) {
					try {
						var timestampstr = Regex.Match(match.Groups[2].Value, "([^,]+)").Groups[1].Value;
						timestampstr = timestampstr.Substring(0, timestampstr.LastIndexOf(':')) + "." + timestampstr.Substring(timestampstr.LastIndexOf(':') + 1);
						sub.Add(TimeSpan.Parse(timestampstr).Add(delay != null ? delay.Value : TimeSpan.FromTicks(0)));

					} catch(Exception ex) { }

				} else if(key.Equals("Delay")) {
					var timestampstr = Regex.Match(match.Groups[2].Value, "([^,]+)").Groups[1].Value;
					timestampstr = timestampstr.Substring(0, timestampstr.LastIndexOf(':')) + "." + timestampstr.Substring(timestampstr.LastIndexOf(':') + 1);
					delay = TimeSpan.Parse(timestampstr);

				} else if(key.Equals("id")) {
					subMatch = Regex.Match(match.Groups[2].Value, @"([^,]+), index\: (.+)");
					if(!subMatch.Success) throw new Exception();

					languageId = subMatch.Groups[1].Value;
					if(!int.TryParse(subMatch.Groups[2].Value, out index)) throw new Exception();

					language = source.Substring(match.Index, (i + 1 < matches.Count ? matches[i + 1].Index : source.Length) - match.Index);
					language = Regex.Match(language, @"alt\: " + "([^\r\n]+)").Groups[1].Value;
					delay = null;

					sub = new List<TimeSpan>();
					subs.Add(new Subs(index, languageId, language, sub));

				} else {
					info[key] = match.Groups[2].Value;
				}
			}

			subtitles = subs.ToArray();
		}

		public class Subs {
			public int index { get; private set; }
			public string languageId { get; private set; }
			public string language { get; private set; }

			private List<TimeSpan> subtitles;

			public int SubtitleCount { get { return subtitles.Count; } }

			internal Subs(int index, string langId, string lang, List<TimeSpan> subs) {
				this.index = index;
				this.language = lang;
				this.languageId = langId;
				this.subtitles = subs;
			}
		}
	}

}
