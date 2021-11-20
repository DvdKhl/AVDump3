using AVDump3Lib.Misc;
using AVDump3Lib.Settings.Core;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AVDump3Lib.Settings.CLArguments {
	public class CLParseArgsResult {
		public CLParseArgsResult(bool success, string message, bool printHelp, string printHelpTopic, ImmutableArray<string> rawArgs, ImmutableDictionary<ISettingProperty, object?> settingValues, ImmutableArray<string> unnamedArgs) {
			Success = success;
			Message = message;
			PrintHelp = printHelp;
			PrintHelpTopic = printHelpTopic;
			RawArgs = rawArgs;
			UnnamedArgs = unnamedArgs;
			SettingValues = settingValues ?? throw new ArgumentNullException(nameof(settingValues));
		}

		public bool Success { get; }
		public string Message { get; }
		public bool PrintHelp { get; }
		public string PrintHelpTopic { get; }
		public ImmutableArray<string> RawArgs { get; }
		public ImmutableArray<string> UnnamedArgs { get; }
		public ImmutableDictionary<ISettingProperty, object?> SettingValues { get; }
	}

	public class CLSettingsHandler {
		//public CLSettingsHandler() {
		//	propToNames = new Dictionary<SettingsProperty, ReadOnlyCollection<string>>();
		//	items = new List<SettingsGroup>();
		//}

		//public void Register(IEnumerable<SettingsGroup> settingsGroups) {
		//	foreach(var item in settingsGroups) {
		//		items.Add(item);
		//		foreach(var prop in item.Properties) {
		//			var settingProperty = item.GetType().GetProperty(prop.Name + "Property");
		//			var attr = settingProperty != null ? (CLNamesAttribute?)Attribute.GetCustomAttribute(settingProperty, typeof(CLNamesAttribute)) : null;
		//			if(attr != null) {
		//				propToNames.Add(prop, attr.Names);
		//			} else {
		//				propToNames.Add(prop, new ReadOnlyCollection<string>(Array.Empty<string>()));
		//			}
		//		}
		//	}
		//}

		public static CLParseArgsResult ParseArgs(IEnumerable<ISettingProperty> settingProperties, string[] args) {
			if(args.Length > 0 && args[0].InvEquals("FROMFILE")) {
				if(args.Length < 2 || !File.Exists(args[1])) {
					return new CLParseArgsResult(
						false, "FROMFILE: File not found", true, "",
						args.ToImmutableArray(),
						ImmutableDictionary<ISettingProperty, object?>.Empty,
						ImmutableArray<string>.Empty
					);
				}
				args = File.ReadLines(args[1])
					.Where(x => !x.InvStartsWith("//") && !string.IsNullOrWhiteSpace(x))
					.Select(x => x.InvReplace("\r", ""))
					.Concat(args.Skip(2))
					.ToArray();
			}

			args = args.Where(x => !string.IsNullOrWhiteSpace(x) && !x.All(c => char.IsDigit(c) || char.IsUpper(c))).ToArray();

			if(args.Length == 0) {
				return new CLParseArgsResult(
					true, "Empty Args, printing help.", true, "",
					args.ToImmutableArray(),
					ImmutableDictionary<ISettingProperty, object?>.Empty,
					ImmutableArray<string>.Empty
				);
			}


			var printHelp = false;
			var printHelpTopic = "";

			var preprocessedArgs = new List<(string Raw, string? Namespace, string? Name, string? Param)>();

			var argPattern = new Regex(@"^--?(?:(?<NameSpace>[a-zA-Z0-9]+)\.)?(?<Arg>[a-zA-Z0-9_][a-zA-Z0-9\-]*)(?:=(?<Param>.*))?$");
			void ParseSub(string[] args) {
				foreach(var arg in args) {
					var match = argPattern.Match(arg);
					var nameSpace = match.Groups["NameSpace"].Success ? match.Groups["NameSpace"].Value : null;
					var param = match.Groups["Param"].Success ? match.Groups["Param"].Value : null;
					var name = match.Groups["Arg"].Value;

					if(arg[0] == '-' && string.IsNullOrEmpty(name)) throw new FormatException("Invalid argument structure");
					if(arg[0] == '-' && arg[1] != '-') {
						if(name.Length > 1) {
							if(param != null) throw new FormatException("Multiple one letter arguments may not have parameters");
							ParseSub(name.Select(ldSwitch => "--" + (nameSpace != null ? nameSpace + "." : "") + ldSwitch).ToArray());
						} else {
							ParseSub(new string[] { "--" + (nameSpace != null ? nameSpace + "." : "") + name + (param != null ? "=" + param : "") });
						}
					} else {
						preprocessedArgs.Add((arg, nameSpace, name, param));
					}
				}
			}
			ParseSub(args);

			var unnamedArgs = new List<string>();
			var settingValues = new Dictionary<ISettingProperty, object?>();

			foreach(var (Raw, Namespace, Name, Param) in preprocessedArgs) {
				if(Name.InvStartsWithOrdCI("Help")) {
					printHelp = true;
					if(!string.IsNullOrEmpty(Param)) {
						printHelpTopic = Param;
					}
					continue;
				}

				if(!string.IsNullOrEmpty(Name)) {
					var comparsionType = Name.Length == 1 ? StringComparison.InvariantCulture : StringComparison.OrdinalIgnoreCase;


					var argCandidates =
						from p in settingProperties
						where Namespace == null || Namespace.InvEqualsOrdCI(p.Group.FullName)
						where p.Name.Equals(Name, comparsionType) || p.AlternativeNames.Any(ldKey => ldKey.Equals(Name, comparsionType))
						select p;

					switch(argCandidates.Count()) {
						case 0: throw new InvalidOperationException("Argument (" + (!string.IsNullOrEmpty(Namespace) ? Namespace + "." : "") + Name + ") is not registered");
						case 1: break;
						default: throw new InvalidOperationException("Argument reference is ambiguous: " + string.Join(", ", argCandidates.Select(ldQuery => ldQuery.Group.Name + "." + ldQuery.Name).ToArray()));
					}
					var entry = argCandidates.First();

					try {
						var valueStr = Param ?? (entry.ValueType == typeof(bool) ? "true" : "");
						var value = entry.ToObject(valueStr);

						settingValues[entry] = value;

					} catch(Exception ex) {
						throw new InvalidOperationException("Property (" + entry.Group.Name + "." + entry.Name + ") could not be set", ex);
					}
				} else unnamedArgs?.Add(Raw);
			}

			return new CLParseArgsResult(true, "OK", printHelp, printHelpTopic, args.ToImmutableArray(), settingValues.ToImmutableDictionary(), unnamedArgs.ToImmutableArray());
		}


		public static string PrintHelpMarkdown(IEnumerable<ISettingProperty> settingProperties) {
			var argGroups =
			from p in settingProperties
			group p by p.Group into g
			select (Group: g.Key, Properties: g.ToArray());


			static string ArgToString(ISettingProperty arg) {
				var names = new[] { arg.Name }.Concat(arg.AlternativeNames).ToArray();
				return string.Join(", ", names.Select(ldKey => ldKey.Length == 1 ? "-" + ldKey : "--" + ldKey));
			}

			var sb = new StringBuilder();
			sb.AppendLine("|Argument|Namespace|Description|Default|Example");
			sb.AppendLine("|--|--|--|--|--");


			void Append(string line) => sb.Append((line ?? "").InvReplace("<", "\\<").InvReplace("|", "\\|").InvReplace("\r", "").InvReplace("\n", "<br>"));

			foreach(var argGroup in argGroups) {
				if(argGroup.Group.FullName.InvStartsWith("_")) continue;

				foreach(var prop in argGroup.Properties) {
					if(prop.Name.InvStartsWith("_")) continue;

					var nsDescription = argGroup.Group.ResourceManager?.GetInvString($"{argGroup.Group.FullName}.Description");
					var example = argGroup.Group.ResourceManager?.GetInvString($"{argGroup.Group.FullName}.{prop.Name}.Example");
					var description = argGroup.Group.ResourceManager?.GetInvString($"{argGroup.Group.FullName}.{prop.Name}.Description");
					var defaultValue = prop.ToString(prop.DefaultValue);


					sb.Append('|');
					Append(ArgToString(prop));
					sb.Append('|');
					Append(argGroup.Group.FullName);
					sb.Append('|');
					Append(description);
					sb.Append('|');
					Append(defaultValue);
					sb.Append('|');
					Append(example);
					sb.AppendLine();
				}
			}

			return sb.ToString();
		}

		public static void PrintHelp(IEnumerable<ISettingProperty> settingProperties, string topic, bool detailed) {
			var argGroups =
				from p in settingProperties
				group p by p.Group into g
				where string.IsNullOrEmpty(topic) || g.Key.FullName.InvEqualsOrdCI(topic)
				select (Group: g.Key, Properties: g.ToArray());


			if(!argGroups.Any()) {
				Console.WriteLine("There is no such topic");
				Console.WriteLine();
				return;
			}

			static string ArgToString(ISettingProperty arg) {
				var names = new[] { arg.Name }.Concat(arg.AlternativeNames).ToArray();
				return string.Join(", ", names.Select(ldKey => ldKey.Length == 1 ? "-" + ldKey : "--" + ldKey));
			}


			foreach(var argGroup in argGroups) {
				if(argGroup.Group.FullName.InvStartsWith("_")) continue;

				//Console.ForegroundColor = ConsoleColor.DarkGreen;
				var descPad = Math.Max(("NameSpace: " + argGroup.Group.FullName).Length, argGroup.Properties.Select(prop => ArgToString(prop).Length).Max());


				PrintLine(("▶2 NameSpace◀: " + argGroup.Group.FullName).PadRight(descPad, ' ') + argGroup.Group.ResourceManager?.GetInvString($"{argGroup.Group.FullName}.Description").OnNotNullReturn(s => " | ▶8 " + s + "◀"));
				Console.WriteLine();
				foreach(var prop in argGroup.Properties) {
					if(prop.Name.InvStartsWith("_")) continue;

					var example = argGroup.Group.ResourceManager?.GetInvString($"{argGroup.Group.FullName}.{prop.Name}.Example");
					var description = argGroup.Group.ResourceManager?.GetInvString($"{argGroup.Group.FullName}.{prop.Name}.Description");

					//var defaultValue = prop.DefaultValue?.ToString() ?? (prop.ValueType == typeof(bool) ? "true" : "");
					var defaultValue = prop.ToString(prop.DefaultValue);

					PrintLine(ArgToString(prop).PadRight(descPad, ' ') + " | " + example + " (" + ("".InvEquals(defaultValue) ? "▶8 <Empty>◀" : defaultValue ?? "▶8 <null>◀") + ")");
					if(detailed && !string.IsNullOrEmpty(description)) {
						if(!string.IsNullOrEmpty(description)) {
							PrintLine(!Utils.UsingWindows ? description : "▶8 " + description + "◀");
						}
						Console.WriteLine();
					}
				}
				if(!detailed && string.IsNullOrEmpty(topic)) {
					Console.WriteLine("Use --Help OR --Help=<NameSpace> for more detailed info");
					Console.WriteLine();
				}
				Console.WriteLine();
			}

		}


		private static void PrintLine(string msg, bool noColors = false) { Print(msg, noColors); Console.WriteLine(); }
		private static void Print(string msg, bool noColors = false) {
			var strb = new StringBuilder();

			static ConsoleColor charToColor(char c) {
				return c switch {
					'0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9' => (ConsoleColor)(c - '0'),
					'A' or 'B' or 'C' or 'D' or 'E' or 'F' => (ConsoleColor)(c - 'A' + 10),
					_ => throw new InvalidOperationException(),
				};
			}

			for(var i = 0; i < msg.Length; i++) {
				var c = msg[i];

				switch(c) {
					case '▶':
						i += 2;
						if(noColors) continue;
						Console.Write(strb.ToString()); strb.Length = 0;
						if(msg[i - 1] != ' ') Console.ForegroundColor = charToColor(msg[i - 1]);
						if(msg[i - 0] != ' ') Console.BackgroundColor = charToColor(msg[i - 0]);
						break;

					case '◀':
						if(noColors) continue;
						Console.Write(strb.ToString()); strb.Length = 0;
						Console.ResetColor();
						break;

					default: strb.Append(c); break;
				}
			}
			Console.Write(strb.ToString());
			if(!noColors) Console.ResetColor();
		}

	}
}
