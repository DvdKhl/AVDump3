using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
using AVDump3UI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AVDump3UI {
	public class AVD3UIModuleSettings {
		public FileDiscoverySettings FileDiscovery { get; }
		public ProcessingSettings Processing { get; }
		public ReportingSettings Reporting { get; }
		public DisplaySettings Display { get; }
		public DiagnosticsSettings Diagnostics { get; }
		public FileMoveSettings FileMove { get; }

		public AVD3UIModuleSettings() {
			FileDiscovery = new FileDiscoverySettings();
			Processing = new ProcessingSettings();
			Reporting = new ReportingSettings();
			Display = new DisplaySettings();
			Diagnostics = new DiagnosticsSettings();
			FileMove = new FileMoveSettings();
		}

		public IEnumerable<SettingsObject> SettingObjects () {
			yield return FileDiscovery;
			yield return Processing;
			yield return Reporting;
			yield return Display;
			yield return Diagnostics;
			yield return FileMove;
		}
	}

	public class FileExtensionsSetting {
		public bool Allow { get; set; }
		public ReadOnlyCollection<string> Items { get; set; }

		public FileExtensionsSetting() {
			Allow = false;
			Items = Array.AsReadOnly(new string[0]);
		}
	}
	public class FileDiscoverySettings : SettingsObject, ICLConvert {
		[CLNames("R")]
		public SettingsProperty RecursiveProperty { get; }
		public bool Recursive {
			get => (bool)GetRequiredValue(RecursiveProperty);
			set => SetValue(RecursiveProperty, value);
		}

		[CLNames("PLPath")]
		public SettingsProperty ProcessedLogPathProperty { get; }
		public string ProcessedLogPath {
			get => (string)GetRequiredValue(ProcessedLogPathProperty);
			set => SetValue(ProcessedLogPathProperty, value);
		}

		[CLNames("SLPath")]
		public SettingsProperty SkipLogPathProperty { get; }
		public string SkipLogPath {
			get => (string)GetRequiredValue(SkipLogPathProperty);
			set => SetValue(SkipLogPathProperty, value);
		}

		[CLNames("DLPath")]
		public SettingsProperty DoneLogPathProperty { get; }
		public string DoneLogPath {
			get => (string)GetRequiredValue(DoneLogPathProperty);
			set => SetValue(DoneLogPathProperty, value);
		}

		[CLNames("Conc")]
		public SettingsProperty ConcurrentProperty { get; }
		public PathPartitions Concurrent {
			get => (PathPartitions)GetRequiredValue(ConcurrentProperty);
			set => SetValue(ConcurrentProperty, value);
		}

		[CLNames("WExts")]
		public SettingsProperty WithExtensionsProperty { get; }
		public FileExtensionsSetting WithExtensions {
			get => (FileExtensionsSetting)GetRequiredValue(WithExtensionsProperty);
			set => SetValue(WithExtensionsProperty, value);
		}

		public FileDiscoverySettings() : base("FileDiscovery", Lang.ResourceManager) {
			RecursiveProperty = Register(nameof(Recursive), false);
			ProcessedLogPathProperty = Register(nameof(ProcessedLogPath), "");
			SkipLogPathProperty = Register(nameof(SkipLogPath), "");
			DoneLogPathProperty = Register(nameof(DoneLogPath), "");
			ConcurrentProperty = Register(nameof(Concurrent), new PathPartitions(1, new PathPartition[0]));
			WithExtensionsProperty = Register(nameof(WithExtensions), new FileExtensionsSetting() { Allow = true });
		}

		string? ICLConvert.ToCLString(SettingsProperty property, object? obj) {
			if(property == WithExtensionsProperty && obj is FileExtensionsSetting extSettings) {
				return (extSettings.Allow ? "" : "-") + string.Join(",", extSettings.Items);

			} else if(property == ConcurrentProperty && obj is PathPartitions value) {
				return value.ConcurrentCount + (value.Partitions.Count > 0 ? ":" : "") + string.Join(",", value.Partitions.Select(x => x.Path + "," + x.ConcurrentCount));
			}

			return obj?.ToString();
		}

		object? ICLConvert.FromCLString(SettingsProperty property, string? str) {
			if(property == WithExtensionsProperty) {
				var value = new FileExtensionsSetting { Allow = str == null || str.Length != 0 && str[0] != '-' };
				if(!value.Allow) str = str.Substring(1);
				value.Items = Array.AsReadOnly(str.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
				return value;

			} else if(property == ConcurrentProperty) {
				var raw = (str ?? "").Split(new char[] { ':' }, 2);

				return new PathPartitions(
					int.Parse(raw[0]),
					from item in (raw.Length > 1 ? raw[1].Split(';') : new string[0])
					let parts = item.Split(',')
					select new PathPartition(parts[0], int.Parse(parts[1]))
				);
			} else if(property == DoneLogPathProperty && str != null) {
				SkipLogPath = str;
				ProcessedLogPath = str;
			}

			return Convert.ChangeType(str, property.ValueType);
		}
	}

	public class ProcessingSettings : SettingsObject, ICLConvert {
		public class ConsumerSettings {
			public ConsumerSettings(string name, IEnumerable<string> arguments) {
				Name = name;
				Arguments = ImmutableArray.CreateRange(arguments);
			}

			public string Name { get; private set; }
			public ImmutableArray<string> Arguments { get; private set; }
		}

		[CLNames("BLength")]
		public SettingsProperty BufferLengthProperty { get; }
		public int BufferLength {
			get => (int)GetRequiredValue(BufferLengthProperty);
			set => SetValue(BufferLengthProperty, value);
		}

		public SettingsProperty ProducerMinReadLengthProperty { get; }
		public int ProducerMinReadLength {
			get => (int)GetRequiredValue(ProducerMinReadLengthProperty);
			set => SetValue(ProducerMinReadLengthProperty, value);
		}
		public SettingsProperty ProducerMaxReadLengthProperty { get; }
		public int ProducerMaxReadLength {
			get => (int)GetValue(ProducerMaxReadLengthProperty);
			set => SetValue(ProducerMaxReadLengthProperty, value);
		}

		[CLNames("PBExit")]
		public SettingsProperty PauseBeforeExitProperty { get; }
		public bool PauseBeforeExit {
			get => (bool)GetRequiredValue(PauseBeforeExitProperty);
			set => SetValue(PauseBeforeExitProperty, value);
		}

		[CLNames("Cons")]
		public SettingsProperty ConsumersProperty { get; }
		public IReadOnlyCollection<ConsumerSettings>? Consumers {
			get => (IReadOnlyCollection<ConsumerSettings>?)GetValue(ConsumersProperty);
			set => SetValue(ConsumersProperty, value);
		}

		public SettingsProperty PrintAvailableSIMDsProperty { get; }
		public bool PrintAvailableSIMDs {
			get => (bool)GetRequiredValue(PrintAvailableSIMDsProperty);
			set => SetValue(PrintAvailableSIMDsProperty, value);
		}

		public ProcessingSettings() : base("Processing", Lang.ResourceManager) {
			BufferLengthProperty = Register(nameof(BufferLength), 64 << 20);
			ProducerMinReadLengthProperty = Register(nameof(ProducerMinReadLength), 1 << 20);
			ProducerMaxReadLengthProperty = Register(nameof(ProducerMaxReadLength), 8 << 20);
			ConsumersProperty = Register(nameof(Consumers), Array.Empty<ConsumerSettings>());
			PrintAvailableSIMDsProperty = Register(nameof(PrintAvailableSIMDs), false);
			PauseBeforeExitProperty = Register(nameof(PauseBeforeExit), false);
		}

		string? ICLConvert.ToCLString(SettingsProperty property, object? obj) {
			if(property == BufferLengthProperty || property == ProducerMinReadLengthProperty || property == ProducerMaxReadLengthProperty) {
				var value = (int?)obj ?? throw new Exception("BufferLengthProperty was null");
				return (value >> 20).ToString();

			} else if(property == ConsumersProperty) {
				if(!(obj is IReadOnlyCollection<ConsumerSettings> lst) || lst.Count == 0) return null;
				//A bit odd at first, but with this we make Consumers==null the special case (i.e. list the consumers)
				return lst.Count == 0 ? "" : string.Join(",", lst.Select(x => x.Name + string.Concat(x.Arguments.Select(y => ":" + y))));
			}
			return obj?.ToString();
		}

		object? ICLConvert.FromCLString(SettingsProperty property, string? str) {
			if(property == BufferLengthProperty || property == ProducerMinReadLengthProperty || property == ProducerMaxReadLengthProperty) {
				return int.Parse(str ?? throw new Exception()) << 20;

			} else if(property == ConsumersProperty) {
				if(str != null && str.Length == 0) return null;
				//See ToCLString
				return Array.AsReadOnly((str ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => {
					var args = x.Split(':', 2);
					return new ConsumerSettings(args[0].Trim(), args.Skip(1).FirstOrDefault()?.Split('|') ?? Array.Empty<string>());
				}).ToArray());
			}
			return Convert.ChangeType(str, property.ValueType);
		}
	}


	public enum FileMoveMode { None, PlaceholderInline, PlaceholderFile, CSharpScriptInline, CSharpScriptFile, DotNetAssembly}

	public class FileMoveSettings : SettingsObject, ICLConvert {
		public SettingsProperty ModeProperty { get; }
		public FileMoveMode Mode {
			get => (FileMoveMode)GetRequiredValue(ModeProperty);
			set => SetValue(ModeProperty, value);
		}

		public SettingsProperty TestProperty { get; }
		public bool Test {
			get => (bool)GetRequiredValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public SettingsProperty PatternProperty { get; }
		public string Pattern {
			get => (string)GetRequiredValue(PatternProperty);
			set => SetValue(PatternProperty, value);
		}


		public SettingsProperty DisableFileMoveProperty { get; }
		public bool DisableFileMove {
			get => (bool)GetRequiredValue(DisableFileMoveProperty);
			set => SetValue(DisableFileMoveProperty, value);
		}



		public SettingsProperty DisableFileRenameProperty { get; }
		public bool DisableFileRename {
			get => (bool)GetRequiredValue(DisableFileRenameProperty);
			set => SetValue(DisableFileRenameProperty, value);
		}

		public SettingsProperty ReplacementsProperty { get; }
		public IEnumerable<(string Value, string Replacement)> Replacements {
			get => (IEnumerable<(string, string)>)GetRequiredValue(ReplacementsProperty);
			set => SetValue(ReplacementsProperty, value);
		}

		public SettingsProperty LogPathProperty { get; }
		public string LogPath {
			get => (string)GetRequiredValue(LogPathProperty);
			set => SetValue(LogPathProperty, value);
		}

		public FileMoveSettings() : base("FileMove", Lang.ResourceManager) {
			TestProperty = Register(nameof(Test), false);
			LogPathProperty = Register(nameof(LogPath), "");
			ModeProperty = Register(nameof(Mode), FileMoveMode.None);
			PatternProperty = Register(nameof(Pattern), "");
			DisableFileMoveProperty = Register(nameof(DisableFileMove), false);
			DisableFileRenameProperty = Register(nameof(DisableFileRename), false);
			ReplacementsProperty = Register(nameof(Replacements), Enumerable.Empty<(string Value, string Replacement)>());
		}

		object? ICLConvert.FromCLString(SettingsProperty prop, string? str) {
			if(prop == ReplacementsProperty) {
				return (str ?? "").Split(';').Select(x => x.Split(',')).Select(x => (x[0], x[1]));
			}
			if(prop == ModeProperty && str != null) {
				return Enum.Parse<FileMoveMode>(str);
			}
			return Convert.ChangeType(str, prop.ValueType);
		}

		string? ICLConvert.ToCLString(SettingsProperty prop, object? obj) {
			if(prop == ReplacementsProperty) {
				return string.Join(", ", Replacements.Select(x => $"({x.Replacement}, {x.Value})"));
			}

			return obj?.ToString();
		}
	}

	public class ReportingSettings : SettingsObject, ICLConvert {

		public SettingsProperty PrintHashesProperty { get; }
		public bool PrintHashes {
			get => (bool)GetRequiredValue(PrintHashesProperty);
			set => SetValue(PrintHashesProperty, value);
		}

		public SettingsProperty PrintReportsProperty { get; }
		public bool PrintReports {
			get => (bool)GetRequiredValue(PrintReportsProperty);
			set => SetValue(PrintReportsProperty, value);
		}

		public SettingsProperty ReportsProperty { get; }
		public IReadOnlyCollection<string>? Reports {
			get => (IReadOnlyCollection<string>?)GetValue(ReportsProperty);
			set => SetValue(ReportsProperty, value);
		}

		[CLNames("RDir")]
		public SettingsProperty ReportDirectoryProperty { get; }
		public string ReportDirectory {
			get => (string)GetRequiredValue(ReportDirectoryProperty);
			set => SetValue(ReportDirectoryProperty, value);
		}

		public SettingsProperty ReportFileNameProperty { get; }
		public string ReportFileName {
			get => (string)GetRequiredValue(ReportFileNameProperty);
			set => SetValue(ReportFileNameProperty, value);
		}

		[CLNames("EDPath")]
		public SettingsProperty ExtensionDifferencePathProperty { get; }
		public string ExtensionDifferencePath {
			get => (string)GetRequiredValue(ExtensionDifferencePathProperty);
			set => SetValue(ExtensionDifferencePathProperty, value);
		}

		public SettingsProperty CRC32ErrorProperty { get; }
		public (string Path, string Pattern)? CRC32Error {
			get => ((string, string)?)GetValue(CRC32ErrorProperty);
			set => SetValue(CRC32ErrorProperty, value);
		}

		public ReportingSettings() : base("Reporting", Lang.ResourceManager) {
			PrintHashesProperty = Register(nameof(PrintHashes), false);
			PrintReportsProperty = Register(nameof(PrintReports), false);
			ReportsProperty = Register(nameof(Reports), Array.Empty<string>());
			ReportDirectoryProperty = Register(nameof(ReportDirectory), Environment.CurrentDirectory);
			ReportFileNameProperty = Register(nameof(ReportFileName), "${FileName}.${ReportName}.${ReportFileExtension}");
			ExtensionDifferencePathProperty = Register(nameof(ExtensionDifferencePath), "");
			CRC32ErrorProperty = Register(nameof(CRC32Error), default((string, string)));
		}

		string? ICLConvert.ToCLString(SettingsProperty property, object? obj) {
			if(property == ReportsProperty) {
				var lst = (IReadOnlyCollection<string>?)obj;
				//A bit odd at first, but with this we make Reports==null the special case (i.e. list the consumers)
				return lst != null ? (lst.Count == 0 ? null : string.Join(",", lst)) : "";
			}
			return obj?.ToString();
		}

		object? ICLConvert.FromCLString(SettingsProperty property, string? str) {
			if(property == ReportsProperty) {
				if(str != null && str.Length == 0) return null;
				//See ToCLString
				return Array.AsReadOnly((str ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray());

			} else if(property == CRC32ErrorProperty) {
				var parts = str?.Split(':') ?? Array.Empty<string>();
				var retVal = parts.Length == 1 ? (parts[0], "(?i)${CRC32}") : (parts[0], parts[1]);

				Regex.IsMatch("12345678", retVal.Item2.Replace("${CRC32}", "12345678")); //Throw Early on invalid Regex

				return retVal;

			}

			return Convert.ChangeType(str, property.ValueType);
		}
	}

	public class DisplaySettings : SettingsObject {
		public SettingsProperty HideBuffersProperty { get; }
		public bool HideBuffers {
			get => (bool)GetRequiredValue(HideBuffersProperty);
			set => SetValue(HideBuffersProperty, value);
		}

		public SettingsProperty HideFileProgressProperty { get; }
		public bool HideFileProgress {
			get => (bool)GetRequiredValue(HideFileProgressProperty);
			set => SetValue(HideFileProgressProperty, value);
		}

		public SettingsProperty HideTotalProgressProperty { get; }
		public bool HideTotalProgress {
			get => (bool)GetValue(HideTotalProgressProperty);
			set => SetValue(HideTotalProgressProperty, value);
		}

		public SettingsProperty ShowDisplayJitterProperty { get; }
		public bool ShowDisplayJitter {
			get => (bool)GetRequiredValue(ShowDisplayJitterProperty);
			set => SetValue(ShowDisplayJitterProperty, value);
		}

		public SettingsProperty ForwardConsoleCursorOnlyProperty { get; }
		public bool ForwardConsoleCursorOnly {
			get => (bool)GetRequiredValue(ForwardConsoleCursorOnlyProperty);
			set => SetValue(ForwardConsoleCursorOnlyProperty, value);
		}

		public DisplaySettings() : base("Display", Lang.ResourceManager) {
			HideBuffersProperty = Register(nameof(HideBuffers), false);
			HideFileProgressProperty = Register(nameof(HideFileProgress), false);
			HideTotalProgressProperty = Register(nameof(HideTotalProgress), false);
			ShowDisplayJitterProperty = Register(nameof(ShowDisplayJitter), false);
			ForwardConsoleCursorOnlyProperty = Register(nameof(ForwardConsoleCursorOnly), false);
		}
	}


	public class NullStreamTestSettings {
		public NullStreamTestSettings(int streamCount, long streamLength, int parallelStreamCount) {
			StreamCount = streamCount;
			StreamLength = streamLength;
			ParallelStreamCount = parallelStreamCount;
		}

		public int StreamCount { get; }
		public long StreamLength { get; }
		public int ParallelStreamCount { get; internal set; }
	}


	public class DiagnosticsSettings : SettingsObject, ICLConvert {
		public SettingsProperty SaveErrorsProperty { get; }
		public bool SaveErrors {
			get => (bool)GetRequiredValue(SaveErrorsProperty);
			set => SetValue(SaveErrorsProperty, value);
		}

		public SettingsProperty SkipEnvironmentElementProperty { get; }
		public bool SkipEnvironmentElement {
			get => (bool)GetRequiredValue(SkipEnvironmentElementProperty);
			set => SetValue(SkipEnvironmentElementProperty, value);
		}

		public SettingsProperty IncludePersonalDataProperty { get; }
		public bool IncludePersonalData {
			get => (bool)GetRequiredValue(IncludePersonalDataProperty);
			set => SetValue(IncludePersonalDataProperty, value);
		}

		public SettingsProperty ErrorDirectoryProperty { get; }
		public string ErrorDirectory {
			get => (string)GetRequiredValue(ErrorDirectoryProperty);
			set => SetValue(ErrorDirectoryProperty, value);
		}

		public SettingsProperty NullStreamTestProperty { get; }
		public NullStreamTestSettings? NullStreamTest {
			get => (NullStreamTestSettings?)GetValue(NullStreamTestProperty);
			set => SetValue(NullStreamTestProperty, value);
		}

		public DiagnosticsSettings() : base("Diagnostics", Lang.ResourceManager) {
			SaveErrorsProperty = Register(nameof(SaveErrors), false);
			SkipEnvironmentElementProperty = Register(nameof(SkipEnvironmentElement), false);
			IncludePersonalDataProperty = Register(nameof(IncludePersonalData), false);
			ErrorDirectoryProperty = Register(nameof(ErrorDirectory), Environment.CurrentDirectory);
			NullStreamTestProperty = Register<NullStreamTestSettings?>(nameof(NullStreamTest), null);

		}

		public string? ToCLString(SettingsProperty property, object? obj) {
			if(property == NullStreamTestProperty) {
				var nullStreamTestSettings = (NullStreamTestSettings?)obj;

				return nullStreamTestSettings == null ? "" :
					nullStreamTestSettings.StreamCount +
					":" + nullStreamTestSettings.StreamLength +
					":" + nullStreamTestSettings.ParallelStreamCount;
			}
			return obj?.ToString();
		}

		public object? FromCLString(SettingsProperty property, string? str) {
			if(property == NullStreamTestProperty) {
				var args = str?.Split(':') ?? Array.Empty<string>();
				return args.Length == 0 ? null :
					new NullStreamTestSettings(
						int.Parse(args[0]),
						long.Parse(args[1]) * (1 << 20),
						int.Parse(args[2])
					);
			}
			return Convert.ChangeType(str, property.ValueType);
		}
	}

}
