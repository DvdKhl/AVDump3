using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Settings.CLArguments;
using AVDump3Lib.Settings.Core;
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
	public class AVD3CLModuleSettings {
		public FileDiscoverySettings FileDiscovery { get; }
		public ProcessingSettings Processing { get; }
		public ReportingSettings Reporting { get; }
		public DisplaySettings Display { get; }
		public DiagnosticsSettings Diagnostics { get; }
		public FileMoveSettings FileMove { get; }
		public SettingsStore Store { get; }

		public AVD3CLModuleSettings(SettingsStore store) {
			Store = store ?? throw new ArgumentNullException(nameof(store));

			FileDiscovery = new FileDiscoverySettings(store);
			Processing = new ProcessingSettings(store);
			Reporting = new ReportingSettings(store);
			Display = new DisplaySettings(store);
			Diagnostics = new DiagnosticsSettings(store);
			FileMove = new FileMoveSettings(store);
		}

		public static IEnumerable<SettingsGroup> GetGroups() {
			yield return FileDiscoverySettings.CreateSettingsGroup(Lang.ResourceManager);
			yield return ProcessingSettings.CreateSettingsGroup(Lang.ResourceManager);
			yield return ReportingSettings.CreateSettingsGroup(Lang.ResourceManager);
			yield return FileMoveSettings.CreateSettingsGroup(Lang.ResourceManager);
			yield return DisplaySettings.CreateSettingsGroup(Lang.ResourceManager);
			yield return DiagnosticsSettings.CreateSettingsGroup(Lang.ResourceManager);
		}

	}
	public enum FileMoveMode { None, PlaceholderInline, PlaceholderFile, CSharpScriptInline, CSharpScriptFile, DotNetAssembly }

	public class FileExtensionsSetting {
		public bool Allow { get; set; }
		public ReadOnlyCollection<string> Items { get; set; }

		public FileExtensionsSetting() {
			Allow = false;
			Items = Array.AsReadOnly(new string[0]);
		}
	}

	public class FileDiscoverySettings : SettingsGroupStore {
		private string FirstNonEmpty(params string[] values) => values.FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? "";

		public bool Recursive => (bool)GetRequiredValue();
		public string ProcessedLogPath => FirstNonEmpty((string)GetRequiredValue(), (string)GetRequiredValue(nameof(DoneLogPath)));
		public string SkipLogPath => FirstNonEmpty((string)GetRequiredValue(), (string)GetRequiredValue(nameof(DoneLogPath)));
		public string DoneLogPath => (string)GetRequiredValue();
		public PathPartitions Concurrent => (PathPartitions)GetRequiredValue();
		public FileExtensionsSetting WithExtensions => (FileExtensionsSetting)GetRequiredValue();

		public static SettingsGroup CreateSettingsGroup(ResourceManager resourceManager) {
			var settingsGroup = new SettingsGroup(nameof(FileDiscoverySettings)[0..^8], resourceManager,
				(prop, str) => {
					if(prop.Name.Equals(nameof(WithExtensions))) {
						var value = new FileExtensionsSetting { Allow = str == null || str.Length != 0 && str[0] != '-' };
						if(!value.Allow) str = str?.Substring(1) ?? "";
						value.Items = Array.AsReadOnly((str ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
						return value;

					} else if(prop.Name.Equals(nameof(Concurrent))) {
						var raw = (str ?? "").Split(new char[] { ':' }, 2);

						return new PathPartitions(
							int.Parse(raw[0]),
							from item in (raw.Length > 1 ? raw[1].Split(';') : new string[0])
							let parts = item.Split(',')
							select new PathPartition(parts[0], int.Parse(parts[1]))
						);
					}

					return Convert.ChangeType(str, prop.ValueType);
				},
				(prop, obj) => {
					if(prop.Name.Equals(nameof(WithExtensions)) && obj is FileExtensionsSetting extSettings) {
						return (extSettings.Allow ? "" : "-") + string.Join(",", extSettings.Items);
					} else if(prop.Name.Equals(nameof(Concurrent)) && obj is PathPartitions value) {
						return value.ConcurrentCount + (value.Partitions.Count > 0 ? ":" : "") + string.Join(",", value.Partitions.Select(x => x.Path + "," + x.ConcurrentCount));
					}
					return obj?.ToString();
				},
				SettingsProperty.From(nameof(Recursive), new[] { "R" }, false),
				SettingsProperty.From(nameof(ProcessedLogPath), new[] { "PLPath" }, ""),
				SettingsProperty.From(nameof(SkipLogPath), new[] { "SLPath" }, ""),
				SettingsProperty.From(nameof(DoneLogPath), new[] { "DLPath" }, ""),
				SettingsProperty.From(nameof(Concurrent), new[] { "Conc" }, new PathPartitions(1, new PathPartition[0])),
				SettingsProperty.From(nameof(WithExtensions), new[] { "WExts" }, new FileExtensionsSetting() { Allow = true })
			);
			return settingsGroup;
		}

		public FileDiscoverySettings(SettingsStore store) : base(nameof(FileDiscoverySettings)[0..^8], store) { }
	}

	public class ProcessingSettings : SettingsGroupStore {
		public class ConsumerSettings {
			public ConsumerSettings(string name, IEnumerable<string> arguments) {
				Name = name;
				Arguments = ImmutableArray.CreateRange(arguments);
			}

			public string Name { get; private set; }
			public ImmutableArray<string> Arguments { get; private set; }
		}

		public int BufferLength => (int)GetRequiredValue();
		public int ProducerMinReadLength => (int)GetRequiredValue();
		public int ProducerMaxReadLength => (int)GetRequiredValue();
		public bool PauseBeforeExit => (bool)GetRequiredValue();
		public IReadOnlyCollection<ConsumerSettings>? Consumers => (IReadOnlyCollection<ConsumerSettings>?)GetRequiredValue();
		public bool PrintAvailableSIMDs => (bool)GetRequiredValue();

		public ProcessingSettings(SettingsStore store) : base(nameof(ProcessingSettings)[0..^8], store) { }

		public static SettingsGroup CreateSettingsGroup(ResourceManager resourceManager) {
			var settingsGroup = new SettingsGroup(
				nameof(ProcessingSettings)[0..^8], resourceManager, 
				FromCLString, ToCLString,
				SettingsProperty.From(nameof(BufferLength), new[] { "BLength" }, 64 << 20),
				SettingsProperty.From(nameof(ProducerMinReadLength), Array.Empty<string>(), 1 << 20),
				SettingsProperty.From(nameof(ProducerMaxReadLength), Array.Empty<string>(), 8 << 20),
				SettingsProperty.From(nameof(Consumers), new[] { "Cons" }, Array.Empty<ConsumerSettings>()),
				SettingsProperty.From(nameof(PrintAvailableSIMDs), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(PauseBeforeExit), new[] { "PBExit" }, false)
			);
			return settingsGroup;
		}

		private static string? ToCLString(SettingsProperty prop, object? obj) {
			if(prop.Name.Equals(nameof(BufferLength)) || prop.Name.Equals(nameof(ProducerMinReadLength)) || prop.Name.Equals(nameof(ProducerMaxReadLength))) {
				var value = (int?)obj ?? throw new Exception("BufferLengthProperty was null");
				return (value >> 20).ToString();

			} else if(prop.Name.Equals(nameof(Consumers))) {
				if(!(obj is IReadOnlyCollection<ConsumerSettings> lst) || lst.Count == 0) return null;
				//A bit odd at first, but with this we make Consumers==null the special case (i.e. list the consumers)
				return lst.Count == 0 ? "" : string.Join(",", lst.Select(x => x.Name + string.Concat(x.Arguments.Select(y => ":" + y))));
			}
			return obj?.ToString();
		}

		private static object? FromCLString(SettingsProperty prop, string? str) {
			if(prop.Name.Equals(nameof(BufferLength)) || prop.Name.Equals(nameof(ProducerMinReadLength)) || prop.Name.Equals(nameof(ProducerMaxReadLength))) {
				return int.Parse(str ?? throw new Exception()) << 20;

			} else if(prop.Name.Equals(nameof(Consumers))) {
				if(str != null && str.Length == 0) return null;
				//See ToCLString
				return Array.AsReadOnly((str ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => {
					var args = x.Split(':', 2);
					return new ConsumerSettings(args[0].Trim(), args.Skip(1).FirstOrDefault()?.Split('|') ?? Array.Empty<string>());
				}).ToArray());
			}
			return Convert.ChangeType(str, prop.ValueType);
		}
	}

	public class FileMoveSettings : SettingsGroupStore {
		public FileMoveMode Mode => (FileMoveMode)GetRequiredValue();
		public bool Test => (bool)GetRequiredValue();
		public string Pattern => (string)GetRequiredValue();
		public bool DisableFileMove => (bool)GetRequiredValue();
		public bool DisableFileRename => (bool)GetRequiredValue();

		public IEnumerable<(string Value, string Replacement)> Replacements => (IEnumerable<(string, string)>)GetRequiredValue();

		public string LogPath => (string)GetRequiredValue();

		public static SettingsGroup CreateSettingsGroup(ResourceManager resourceManager) {
			var settingsGroup = new SettingsGroup(
				nameof(FileMoveSettings)[0..^8], resourceManager, 
				FromCLString, ToCLString,
				SettingsProperty.From(nameof(Test), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(LogPath), Array.Empty<string>(), ""),
				SettingsProperty.From(nameof(Mode), Array.Empty<string>(), FileMoveMode.None),
				SettingsProperty.From(nameof(Pattern), Array.Empty<string>(), ""),
				SettingsProperty.From(nameof(DisableFileMove), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(DisableFileRename), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(Replacements), Array.Empty<string>(), Enumerable.Empty<(string Value, string Replacement)>())
			);
			return settingsGroup;
		}
		public FileMoveSettings(SettingsStore store) : base(nameof(FileMoveSettings)[0..^8], store) { }

		private static object? FromCLString(SettingsProperty prop, string? str) {
			if(prop.Name.Equals(nameof(Replacements))) {
				return (str ?? "").Split(';').Select(x => x.Split(',')).Select(x => (x[0], x[1]));
			}
			if(prop.Name.Equals(nameof(Mode)) && str != null) {
				return Enum.Parse<FileMoveMode>(str);
			}
			return Convert.ChangeType(str, prop.ValueType);
		}

		private static string? ToCLString(SettingsProperty prop, object? obj) {
			if(prop.Name.Equals(nameof(Replacements)) && obj is IEnumerable<(string Value, string Replacement)> replacements) {
				return string.Join(", ", replacements.Select(x => $"({x.Value}, {x.Replacement})"));
			}

			return obj?.ToString();
		}
	}

	public class ReportingSettings : SettingsGroupStore {

		public bool PrintHashes => (bool)GetRequiredValue();

		public bool PrintReports => (bool)GetRequiredValue();

		public IReadOnlyCollection<string>? Reports => (IReadOnlyCollection<string>?)GetValue();

		public string ReportDirectory => (string)GetRequiredValue();

		public string ReportFileName => (string)GetRequiredValue();

		public string ExtensionDifferencePath => (string)GetRequiredValue();

		public (string Path, string Pattern)? CRC32Error => ((string, string)?)GetValue();

		public static SettingsGroup CreateSettingsGroup(ResourceManager resourceManager) {
			var settingsGroup = new SettingsGroup(
				nameof(ReportingSettings)[0..^8], resourceManager, FromCLString, ToCLString,
				SettingsProperty.From(nameof(PrintHashes), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(PrintReports), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(Reports), Array.Empty<string>(), Array.Empty<string>()),
				SettingsProperty.From(nameof(ReportDirectory), new[] { "RDir" }, Environment.CurrentDirectory),
				SettingsProperty.From(nameof(ReportFileName), Array.Empty<string>(), "${FileName}.${ReportName}.${ReportFileExtension}"),
				SettingsProperty.From(nameof(ExtensionDifferencePath), new[] { "EDPath" }, ""),
				SettingsProperty.From(nameof(CRC32Error), Array.Empty<string>(), default((string, string)))
			);
			return settingsGroup;
		}
		public ReportingSettings(SettingsStore store) : base(nameof(ReportingSettings)[0..^8], store) { }

		private static string? ToCLString(SettingsProperty prop, object? obj) {
			if(prop.Name.Equals(nameof(Reports))) {
				var lst = (IReadOnlyCollection<string>?)obj;
				//A bit odd at first, but with this we make Reports==null the special case (i.e. list the consumers)
				return lst != null ? (lst.Count == 0 ? null : string.Join(",", lst)) : "";
			}
			return obj?.ToString();
		}

		private static object? FromCLString(SettingsProperty prop, string? str) {
			if(prop.Name.Equals(nameof(Reports))) {
				if(str != null && str.Length == 0) return null;
				//See ToCLString
				return Array.AsReadOnly((str ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToArray());

			} else if(prop.Name.Equals(nameof(CRC32Error))) {
				var parts = str?.Split(':') ?? Array.Empty<string>();
				var retVal = parts.Length == 1 ? (parts[0], "(?i)${CRC32}") : (parts[0], parts[1]);

				Regex.IsMatch("12345678", retVal.Item2.Replace("${CRC32}", "12345678")); //Throw Early on invalid Regex

				return retVal;

			}

			return Convert.ChangeType(str, prop.ValueType);
		}
	}

	public class DisplaySettings : SettingsGroupStore {
		public bool HideBuffers => (bool)GetRequiredValue();
		public bool HideFileProgress => (bool)GetRequiredValue();
		public bool HideTotalProgress => (bool)GetRequiredValue();
		public bool ShowDisplayJitter => (bool)GetRequiredValue();
		public bool ForwardConsoleCursorOnly => (bool)GetRequiredValue();

		public static SettingsGroup CreateSettingsGroup(ResourceManager resourceManager) {
			var settingsGroup = new SettingsGroup(
				nameof(DisplaySettings)[0..^8], resourceManager, 
				SettingsGroup.DefaultToObject, SettingsGroup.DefaultToString,
				SettingsProperty.From(nameof(HideBuffers), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(HideFileProgress), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(HideTotalProgress), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(ShowDisplayJitter), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(ForwardConsoleCursorOnly), Array.Empty<string>(), false)
			);
			return settingsGroup;
		}
		public DisplaySettings(SettingsStore store) : base(nameof(DisplaySettings)[0..^8], store) { }
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

	public class DiagnosticsSettings : SettingsGroupStore {
		public bool SaveErrors => (bool)GetRequiredValue();

		public bool SkipEnvironmentElement => (bool)GetRequiredValue();

		public bool IncludePersonalData => (bool)GetRequiredValue();

		public string ErrorDirectory => (string)GetRequiredValue();

		public NullStreamTestSettings? NullStreamTest => (NullStreamTestSettings?)GetValue();

		public static SettingsGroup CreateSettingsGroup(ResourceManager resourceManager) {
			var settingsGroup = new SettingsGroup(
				nameof(DiagnosticsSettings)[0..^8], resourceManager, FromCLString, ToCLString,
				SettingsProperty.From(nameof(SaveErrors), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(SkipEnvironmentElement), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(IncludePersonalData), Array.Empty<string>(), false),
				SettingsProperty.From(nameof(ErrorDirectory), Array.Empty<string>(), Environment.CurrentDirectory),
				SettingsProperty.From<NullStreamTestSettings?>(nameof(NullStreamTest), Array.Empty<string>(), null)
			);
			return settingsGroup;
		}
		public DiagnosticsSettings(SettingsStore store) : base(nameof(DiagnosticsSettings)[0..^8], store) { }

		private static string? ToCLString(SettingsProperty prop, object? obj) {
			if(prop.Name.Equals(nameof(NullStreamTest))) {
				var nullStreamTestSettings = (NullStreamTestSettings?)obj;

				return nullStreamTestSettings == null ? "" :
					nullStreamTestSettings.StreamCount +
					":" + nullStreamTestSettings.StreamLength +
					":" + nullStreamTestSettings.ParallelStreamCount;
			}
			return obj?.ToString();
		}

		private static object? FromCLString(SettingsProperty prop, string? str) {
			if(prop.Name.Equals(nameof(NullStreamTest))) {
				var args = str?.Split(':') ?? Array.Empty<string>();
				return args.Length == 0 ? null :
					new NullStreamTestSettings(
						int.Parse(args[0]),
						long.Parse(args[1]) * (1 << 20),
						int.Parse(args[2])
					);
			}
			return Convert.ChangeType(str, prop.ValueType);
		}
	}

}
