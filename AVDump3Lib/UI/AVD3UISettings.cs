using AVDump3Lib.Processing.StreamProvider;
using AVDump3Lib.Settings.Core;
using ExtKnot.StringInvariants;
using System.Collections.Immutable;
using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;

namespace AVDump3Lib.UI;

public class AVD3UISettings {
	public static readonly object UnspecifiedType = new();
	public static readonly object PasswordType = new();

	protected class ResourceManagerMerged : ResourceManager {
		private readonly ResourceManager main;
		private readonly ResourceManager fallback;


		public ResourceManagerMerged(ResourceManager main, ResourceManager fallback) {
			this.main = main;
			this.fallback = fallback;
		}

		public override object? GetObject(string name) => base.GetObject(name, CultureInfo.CurrentUICulture);
		public override object? GetObject(string name, CultureInfo? culture) => main.GetObject(name, culture) ?? fallback.GetObject(name, culture);
		public override string? GetString(string name) => base.GetString(name, CultureInfo.CurrentUICulture);
		public override string? GetString(string name, CultureInfo? culture) => main.GetString(name, culture) ?? fallback.GetString(name, culture);
	}


	public static ResourceManager ResourceManager => Lang.ResourceManager;

	public FileDiscoverySettings FileDiscovery { get; }
	public ProcessingSettings Processing { get; }
	public ReportingSettings Reporting { get; }
	public DiagnosticsSettings Diagnostics { get; }
	public FileMoveSettings FileMove { get; }
	public ISettingStore Store { get; }


	public AVD3UISettings(ISettingStore store) {
		Store = store;

		FileDiscovery = new FileDiscoverySettings(store);
		Processing = new ProcessingSettings(store);
		Reporting = new ReportingSettings(store);
		Diagnostics = new DiagnosticsSettings(store);
		FileMove = new FileMoveSettings(store);
	}

	public static IEnumerable<ISettingProperty> GetProperties() {
		return Enumerable.Empty<ISettingProperty>()
			.Concat(FileDiscoverySettings.CreateProperties())
			.Concat(ProcessingSettings.CreateProperties())
			.Concat(FileMoveSettings.CreateProperties())
			.Concat(ReportingSettings.CreateProperties())
			.Concat(DiagnosticsSettings.CreateProperties());

		//yield return ProcessingSettings.CreateSettingsGroup();
		//yield return ReportingSettings.CreateSettingsGroup();
		//yield return FileMoveSettings.CreateSettingsGroup();
		//yield return DiagnosticsSettings.CreateSettingsGroup();
	}

}

public class ConsumerSettings {
	public ConsumerSettings(string name, IEnumerable<string> arguments) {
		Name = name;
		Arguments = ImmutableArray.CreateRange(arguments);
	}

	public string Name { get; private set; }
	public ImmutableArray<string> Arguments { get; private set; }
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
public class FileExtensionsSetting {
	public bool Allow { get; set; }
	public ImmutableArray<string> Items { get; set; }

	public FileExtensionsSetting() {
		Allow = false;
		Items = ImmutableArray<string>.Empty;
	}
}
public enum FileMoveMode { None, PlaceholderInline, PlaceholderFile, CSharpScriptInline, CSharpScriptFile, DotNetAssembly }


public class FileDiscoverySettings : SettingFacade {
	public FileDiscoverySettings(ISettingStore store) : base(SettingGroup, store) { }

	public bool Recursive => (bool)GetRequiredValue();
	public IEnumerable<string> ProcessedLogPath => ((ImmutableArray<string>)GetRequiredValue()).Append((string)GetRequiredValue(nameof(DoneLogPath))).Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.InvariantCultureIgnoreCase);
	public IEnumerable<string> SkipLogPath => ((ImmutableArray<string>)GetRequiredValue()).Append((string)GetRequiredValue(nameof(DoneLogPath))).Where(x => !string.IsNullOrEmpty(x)).Distinct(StringComparer.InvariantCultureIgnoreCase);
	public string DoneLogPath => (string)GetRequiredValue();
	public PathPartitions Concurrent => (PathPartitions)GetRequiredValue();
	public FileExtensionsSetting WithExtensions => (FileExtensionsSetting)GetRequiredValue();


	public static ISettingGroup SettingGroup { get; } = new SettingGroup(nameof(FileDiscoverySettings)[0..^8], Lang.ResourceManager);
	public static ImmutableArray<ISettingProperty> SettingProperties { get; private set; } = CreateProperties().ToImmutableArray();
	public static IEnumerable<ISettingProperty> CreateProperties() {
		yield return From(SettingGroup, nameof(Recursive), Names("R"), AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(ProcessedLogPath), Names("PLPath"), AVD3UISettings.UnspecifiedType, ImmutableArray<string>.Empty);
		yield return From(SettingGroup, nameof(SkipLogPath), Names("SLPath"), AVD3UISettings.UnspecifiedType, ImmutableArray<string>.Empty);
		yield return From(SettingGroup, nameof(DoneLogPath), Names("DLPath"), AVD3UISettings.UnspecifiedType, "");

		yield return FromWithNullToNull(SettingGroup, nameof(WithExtensions), Names("WExts"), AVD3UISettings.UnspecifiedType, new FileExtensionsSetting() { Allow = true },
			(p, s) => {
				var value = new FileExtensionsSetting { Allow = s == null || s.Length != 0 && s[0] != '-' };
				if(!value.Allow) s = s?[1..] ?? "";
				value.Items = ImmutableArray.Create((s ?? "").Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries));
				return value;
			},
			(p, o) => (o.Allow ? "" : "-") + string.Join(",", o.Items)
		);

		yield return FromWithNullToNull(SettingGroup, nameof(Concurrent), Names("Conc"), AVD3UISettings.UnspecifiedType, new PathPartitions(1, Array.Empty<PathPartition>()),
			(p, s) => {
				var raw = (s ?? "").Split(new char[] { ':' }, 2);
				return new PathPartitions(
					raw[0].ToInvInt32(),
					from item in (raw.Length > 1 ? raw[1].Split(';') : Array.Empty<string>())
					let parts = item.Split(',')
					select new PathPartition(parts[0], parts[1].ToInvInt32())
				);
			},
			(p, o) => o.ConcurrentCount + (o.Partitions.Count > 0 ? ":" : "") + string.Join(",", o.Partitions.Select(x => x.Path + "," + x.ConcurrentCount))
		);
	}
}
public class ProcessingSettings : SettingFacade {
	public ProcessingSettings(ISettingStore store) : base(SettingGroup, store) { }

	public int BufferLength => (int)GetRequiredValue();
	public int ProducerMinReadLength => (int)GetRequiredValue();
	public int ProducerMaxReadLength => (int)GetRequiredValue();
	public bool PauseBeforeExit => (bool)GetRequiredValue();
	public ImmutableArray<ConsumerSettings>? Consumers => (ImmutableArray<ConsumerSettings>?)GetValue();
	public bool PrintAvailableSIMDs => (bool)GetRequiredValue();


	public static ISettingGroup SettingGroup { get; } = new SettingGroup(nameof(ProcessingSettings)[0..^8], Lang.ResourceManager);
	public static ImmutableArray<ISettingProperty> SettingProperties { get; private set; } = CreateProperties().ToImmutableArray();
	public static IEnumerable<ISettingProperty> CreateProperties() {
		yield return From(SettingGroup, nameof(ProducerMinReadLength), None, AVD3UISettings.UnspecifiedType, 1 << 20,
			(p, s) => s.ToInvInt32() << 20,
			(p, o) => (o >> 20).ToString()
		);
		yield return From(SettingGroup, nameof(ProducerMaxReadLength), None, AVD3UISettings.UnspecifiedType, 8 << 20,
			(p, s) => s.ToInvInt32() << 20,
			(p, o) => (o >> 20).ToString()
		);
		yield return From(SettingGroup, nameof(PrintAvailableSIMDs), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(PauseBeforeExit), Names("PBExit"), AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(BufferLength), Names("BLength"), AVD3UISettings.UnspecifiedType, 64 << 20,
			(p, s) => s.ToInvInt32() << 20,
			(p, o) => (o >> 20).ToString()
		);
		yield return From(SettingGroup, nameof(Consumers), Names("Cons"), AVD3UISettings.UnspecifiedType, ImmutableArray<ConsumerSettings>.Empty,
			(p, s) => {
				if(s != null && s.Length == 0) return null;
				//See ToCLString
				return ImmutableArray.CreateRange((s ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => {
					var args = x.Split(':', 2);
					return new ConsumerSettings(args[0].Trim(), args.Skip(1).FirstOrDefault()?.Split('|') ?? Array.Empty<string>());
				}).ToArray());
			},
			(p, o) => {
				if(o is not ImmutableArray<ConsumerSettings> lst || lst.Length == 0) return null;
				//A bit odd at first, but with this we make Consumers==null the special case (i.e. list the consumers)
				return lst.Length == 0 ? "" : string.Join(",", lst.Select(x => x.Name + string.Concat(x.Arguments.Select(y => ":" + y))));
			}
		);
	}
}
public class FileMoveSettings : SettingFacade {
	public FileMoveSettings(ISettingStore store) : base(SettingGroup, store) { }

	public FileMoveMode Mode => (FileMoveMode)GetRequiredValue();
	public bool Test => (bool)GetRequiredValue();
	public string Pattern => (string)GetRequiredValue();
	public bool DisableFileMove => (bool)GetRequiredValue();
	public bool DisableFileRename => (bool)GetRequiredValue();
	public ImmutableArray<(string Value, string Replacement)> Replacements => (ImmutableArray<(string, string)>)GetRequiredValue();
	public string LogPath => (string)GetRequiredValue();

	public static ISettingGroup SettingGroup { get; } = new SettingGroup(nameof(FileMoveSettings)[0..^8], Lang.ResourceManager);
	public static ImmutableArray<ISettingProperty> SettingProperties { get; private set; } = CreateProperties().ToImmutableArray();
	public static IEnumerable<ISettingProperty> CreateProperties() {
		yield return From(SettingGroup, nameof(Test), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(LogPath), None, AVD3UISettings.UnspecifiedType, "");
		yield return From(SettingGroup, nameof(Mode), None, AVD3UISettings.UnspecifiedType, FileMoveMode.None,
			(p, s) => Enum.Parse<FileMoveMode>(s),
			(p, o) => o.ToString()
		);
		yield return From(SettingGroup, nameof(Pattern), None, AVD3UISettings.UnspecifiedType, "${DirectoryName}" + Path.DirectorySeparatorChar + "${FileNameWithoutExtension}${FileExtension}");
		yield return From(SettingGroup, nameof(DisableFileMove), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(DisableFileRename), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(Replacements), None, AVD3UISettings.UnspecifiedType, ImmutableArray<(string Value, string Replacement)>.Empty,
			(p, s) => (s ?? "").Split(';').Select(x => x.Split('=')).Select(x => (x[0], x[1])).ToImmutableArray(),
			(p, o) => string.Join(";", ((IEnumerable<(string Value, string Replacement)>)o).Select(x => $"{x.Value}={x.Replacement}"))
		);
	}

}
public class ReportingSettings : SettingFacade {
	public ReportingSettings(ISettingStore store) : base(SettingGroup, store) { }

	public bool PrintHashes => (bool)GetRequiredValue();
	public bool PrintReports => (bool)GetRequiredValue();
	public ImmutableArray<string>? Reports => (ImmutableArray<string>?)GetValue();
	public string ReportDirectory => (string)GetRequiredValue();
	public string ReportFileName => (string)GetRequiredValue();
	public string ReportContentPrefix => (string)GetRequiredValue();
	public string ExtensionDifferencePath => (string)GetRequiredValue();
	public (string Path, string Pattern)? CRC32Error => ((string, string)?)GetValue();

	public static ISettingGroup SettingGroup { get; } = new SettingGroup(nameof(ReportingSettings)[0..^8], Lang.ResourceManager);
	public static ImmutableArray<ISettingProperty> SettingProperties { get; private set; } = CreateProperties().ToImmutableArray();
	public static IEnumerable<ISettingProperty> CreateProperties() {
		yield return From(SettingGroup, nameof(PrintHashes), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(PrintReports), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(Reports), None, AVD3UISettings.UnspecifiedType, ImmutableArray<string>.Empty,
			(p, s) => {
				if(s != null && s.Length == 0) return null;
				return (s ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToImmutableArray();
			},
			(p, o) => {
				var lst = (ImmutableArray<string>?)o;
				return lst != null ? (lst.Value.Length == 0 ? null : string.Join(",", lst)) : "";
			}
		);
		yield return From(SettingGroup, nameof(ReportDirectory), Names("RDir"), AVD3UISettings.UnspecifiedType, Environment.CurrentDirectory);
		yield return From(SettingGroup, nameof(ReportFileName), None, AVD3UISettings.UnspecifiedType, "${FileName}.${ReportName}.${ReportFileExtension}");
		yield return From(SettingGroup, nameof(ReportContentPrefix), None, AVD3UISettings.UnspecifiedType, "");
		yield return From(SettingGroup, nameof(ExtensionDifferencePath), Names("EDPath"), AVD3UISettings.UnspecifiedType, "");
		yield return From(SettingGroup, nameof(CRC32Error), None, AVD3UISettings.UnspecifiedType, ("", "(?i)${CRC32}"),
			(p, s) => {
				var parts = s?.Split(new[] { ',' }, 2) ?? Array.Empty<string>();
				var retVal = parts.Length == 1 ? (parts[0], "(?i)${CRC32}") : (parts[0], parts[1]);
				Regex.IsMatch("12345678", retVal.Item2.Replace("${CRC32}", "12345678")); //Throw Early on invalid Regex
				return retVal;
			},
			(p, o) => $"{o.Item1},{o.Item2}"
		);
	}
}
public class DiagnosticsSettings : SettingFacade {
	public DiagnosticsSettings(ISettingStore store) : base(SettingGroup, store) { }

	public bool Version => (bool)GetRequiredValue();
	public bool SaveErrors => (bool)GetRequiredValue();
	public bool SkipEnvironmentElement => (bool)GetRequiredValue();
	public bool IncludePersonalData => (bool)GetRequiredValue();
	public string ErrorDirectory => (string)GetRequiredValue();
	public bool PrintDiscoveredFiles => (bool)GetRequiredValue();
	public NullStreamTestSettings NullStreamTest => (NullStreamTestSettings)GetRequiredValue();

	public static ISettingGroup SettingGroup { get; } = new SettingGroup(nameof(DiagnosticsSettings)[0..^8], Lang.ResourceManager);
	public static ImmutableArray<ISettingProperty> SettingProperties { get; private set; } = CreateProperties().ToImmutableArray();
	public static IEnumerable<ISettingProperty> CreateProperties() {
		yield return From(SettingGroup, nameof(Version), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(SaveErrors), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(SkipEnvironmentElement), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(IncludePersonalData), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(PrintDiscoveredFiles), None, AVD3UISettings.UnspecifiedType, false);
		yield return From(SettingGroup, nameof(ErrorDirectory), None, AVD3UISettings.UnspecifiedType, Environment.CurrentDirectory);
		yield return From(SettingGroup, nameof(NullStreamTest), None, AVD3UISettings.UnspecifiedType, new NullStreamTestSettings(0, 0, 0),
			(p, s) => {
				var args = s?.Split(':') ?? Array.Empty<string>();
				return args.Length == 0 ? null :
					new NullStreamTestSettings(
						int.Parse(args[0]),
						long.Parse(args[1]) * (1 << 20),
						int.Parse(args[2])
					);
			},
			(p, o) => o == null ? "" : o.StreamCount + ":" + o.StreamLength + ":" + o.ParallelStreamCount
		);
	}
}
