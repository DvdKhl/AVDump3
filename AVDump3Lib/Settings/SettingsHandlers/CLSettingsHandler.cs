using AVDump3Lib.Misc;
using AVDump3Lib.Settings.Core;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AVDump3Lib.Settings.CLArguments {
	public class CLParseArgsResult {
		public CLParseArgsResult(bool success, string message, bool printHelp, string printHelpTopic, ImmutableDictionary<SettingsProperty, object?> settingValues, ImmutableArray<string> unnamedArgs) {
			Success = success;
			Message = message;
			PrintHelp = printHelp;
			PrintHelpTopic = printHelpTopic;
			UnnamedArgs = unnamedArgs;
			SettingValues = settingValues ?? throw new ArgumentNullException(nameof(settingValues));
		}

		public bool Success { get; }
		public string Message { get;  }
		public bool PrintHelp { get; }
		public string PrintHelpTopic { get;}

		public ImmutableArray<string> UnnamedArgs { get; }
		public ImmutableDictionary<SettingsProperty, object?> SettingValues { get; }
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

		public static CLParseArgsResult ParseArgs(IEnumerable<SettingsGroup> settingsGroups, string[] args) {
			args = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

			if(args.Length == 0) {
				return new CLParseArgsResult(
					true, "Empty Args, printing help.", true, "", 
					ImmutableDictionary<SettingsProperty, object?>.Empty, 
					ImmutableArray<string>.Empty
				);
			}

			var printHelp = false;
			var printHelpTopic = "";

			var preprocessedArgs = new List<(string Raw, string? Namespace, string? Name, string? Param)>();

			var argPattern = new Regex(@"^--?(?:(?<NameSpace>[a-zA-Z0-9]+)\.)?(?<Arg>[a-zA-Z0-9][a-zA-Z0-9\-]*)(?:=(?<Param>.*))?$");
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
			var settingValues = new Dictionary<SettingsProperty, object?>();

			foreach(var arg in preprocessedArgs) {
				if(arg.Name.InvStartsWithOrdCI("Help")) {
					printHelp = true;
					if(!string.IsNullOrEmpty(arg.Param)) {
						printHelpTopic = arg.Param;
					}
					continue;
				}

				if(!string.IsNullOrEmpty(arg.Name)) {
					var comparsionType = arg.Name.Length == 1 ? StringComparison.InvariantCulture : StringComparison.OrdinalIgnoreCase;


					var argCandidates =
						from g in settingsGroups
						where arg.Namespace == null || arg.Namespace.InvEqualsOrdCI(g.Name)
						from a in g.Properties
						where a.Name.Equals(arg.Name, comparsionType) || a.AlternativeNames.Any(ldKey => ldKey.Equals(arg.Name, comparsionType))
						select new { Group = g, Property = a };

					switch(argCandidates.Count()) {
						case 0: throw new InvalidOperationException("Argument (" + (!string.IsNullOrEmpty(arg.Namespace) ? arg.Namespace + "." : "") + arg.Name + ") is not registered");
						case 1: break;
						default: throw new InvalidOperationException("Argument reference is ambiguous: " + string.Join(", ", argCandidates.Select(ldQuery => ldQuery.Group.Name + "." + ldQuery.Property.Name).ToArray()));
					}
					var entry = argCandidates.First();

					try {
						var valueStr = arg.Param ?? (entry.Property.ValueType == typeof(bool) ? "true" : "");
						var value = entry.Group.PropertyStringToObject(entry.Property, valueStr);

						settingValues[entry.Property] = value;

					} catch(Exception ex) {
						throw new InvalidOperationException("Property (" + entry.Group.Name + "." + entry.Property.Name + ") could not be set", ex);
					}
				} else unnamedArgs?.Add(arg.Raw);
			}

			return new CLParseArgsResult(true, "OK", printHelp, printHelpTopic, settingValues.ToImmutableDictionary(), unnamedArgs.ToImmutableArray());
		}

		public static void PrintHelp(IEnumerable<SettingsGroup> settingsGroups, string topic, bool detailed) {
			var argGroups = settingsGroups.Where(ldArgGroup => string.IsNullOrEmpty(topic) || ldArgGroup.Name.InvEqualsOrdCI(topic)).ToArray();
			if(!argGroups.Any()) {
				Console.WriteLine("There is no such topic");
				Console.WriteLine();
				return;
			}

			string argToString(SettingsProperty arg) {
				var names = new[] { arg.Name }.Concat(arg.AlternativeNames).ToArray();
				return string.Join(", ", names.Select(ldKey => ldKey.Length == 1 ? "-" + ldKey : "--" + ldKey));
			}
			foreach(var argGroup in argGroups) {
				//Console.ForegroundColor = ConsoleColor.DarkGreen;
				var descPad = Math.Max(("NameSpace: " + argGroup.Name).Length, argGroup.Properties.Select(prop => argToString(prop).Length).Max());


				var resMan = argGroup.ResourceManager;
				PrintLine(("▶2 NameSpace◀: " + argGroup.Name).PadRight(descPad, ' ') + resMan?.GetInvString($"{argGroup.Name}.Description").OnNotNullReturn(s => " | ▶8 " + s + "◀"));
				Console.WriteLine();
				foreach(var prop in argGroup.Properties) {
					var example = resMan?.GetInvString($"{argGroup.Name}.{prop.Name}.Example");
					var description = resMan?.GetInvString($"{argGroup.Name}.{prop.Name}.Description");

					var defaultValue = prop.DefaultValue?.ToString() ?? (prop.ValueType == typeof(bool) ? "true" : "");

					PrintLine(argToString(prop).PadRight(descPad, ' ') + " | " + example + " (" + ("".InvEquals(defaultValue) ? "▶8 <Empty>◀" : defaultValue ?? "▶8 <null>◀") + ")");
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

			Func<char, ConsoleColor> charToColor = c => {
				switch(c) {
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9': return (ConsoleColor)(c - '0');
					case 'A':
					case 'B':
					case 'C':
					case 'D':
					case 'E':
					case 'F': return (ConsoleColor)(c - 'A' + 10);
					default: throw new InvalidOperationException();
				}
			};

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
