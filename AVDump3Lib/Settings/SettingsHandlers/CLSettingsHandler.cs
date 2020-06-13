using AVDump3Lib.Misc;
using AVDump3Lib.Settings.Core;
using ExtKnot.StringInvariants;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AVDump3Lib.Settings.CLArguments {
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class CLNamesAttribute : Attribute {
		public ReadOnlyCollection<string> Names { get; }

		public CLNamesAttribute(params string[] names) { Names = Array.AsReadOnly(names); }

	}

	public interface ICLConvert {
		string? ToCLString(SettingsProperty prop, object? obj);
		object? FromCLString(SettingsProperty prop, string? str);
	}


	public class CLSettingsHandler : ICLSettingsHandler {

		private Dictionary<SettingsProperty, ReadOnlyCollection<string>> propToNames;

		private List<SettingsObject> items;

		public CLSettingsHandler() {
			propToNames = new Dictionary<SettingsProperty, ReadOnlyCollection<string>>();
			items = new List<SettingsObject>();
		}

		public void Register(params SettingsObject[] settingsObjects) {
			foreach(var item in settingsObjects) {
				items.Add(item);
				foreach(var prop in item.Properties) {
					var settingProperty = item.GetType().GetProperty(prop.Name + "Property");
					var attr = settingProperty != null ? (CLNamesAttribute?)Attribute.GetCustomAttribute(settingProperty, typeof(CLNamesAttribute)) : null;
					if(attr != null) {
						propToNames.Add(prop, attr.Names);
					} else {
						propToNames.Add(prop, new ReadOnlyCollection<string>(Array.Empty<string>()));
					}
				}
			}
		}

		public bool ParseArgs(string[] args, ICollection<string>? unnamedArgs) {
			args = args.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();

			if(args.Length == 0 || args[0].InvEqualsOrdCI("--Help")) {
				if(args.Length == 2) PrintHelpTopic(args[1], true); else PrintHelp(true);
				return false;
			}

			for(var i = 0; i < args.Length; i++) {
				var match = Regex.Match(args[i], @"^--?(?:(?<NameSpace>[a-zA-Z0-9]+)\.)?(?<Arg>[a-zA-Z0-9][a-zA-Z0-9\-]*)(?:=(?<Param>.*))?$");
				var nameSpace = match.Groups["NameSpace"].Success ? match.Groups["NameSpace"].Value : null;
				var param = match.Groups["Param"].Success ? match.Groups["Param"].Value : null;
				var name = match.Groups["Arg"].Value;

				if(args[i][0] == '-' && string.IsNullOrEmpty(name)) throw new FormatException("Invalid argument structure");
				if(args[i][0] == '-' && args[i][1] != '-') {
					if(name.Length > 1) {
						if(param != null) throw new FormatException("Multiple one letter arguments may not have parameters");
						ParseArgs(name.Select(ldSwitch => "--" + (nameSpace != null ? nameSpace + "." : "") + ldSwitch).ToArray(), unnamedArgs);
					} else {
						ParseArgs(new string[] { "--" + (nameSpace != null ? nameSpace + "." : "") + name + (param != null ? "=" + param : "") }, unnamedArgs);
					}

				} else if(args[i][0] == '-' && args[i][1] == '-') {
					var comparsionType = name.Length == 1 ? StringComparison.InvariantCulture : StringComparison.OrdinalIgnoreCase;


					var argCandidates =
						from g in items
						where nameSpace == null || nameSpace.InvEqualsOrdCI(g.Name)
						from a in g.Properties
						where a.Name.Equals(name, comparsionType) || propToNames[a].Any(ldKey => ldKey.Equals(name, comparsionType))
						select new { Group = g, Property = a };

					switch(argCandidates.Count()) {
						case 0: throw new InvalidOperationException("Argument (" + (!string.IsNullOrEmpty(nameSpace) ? nameSpace + "." : "") + name + ") is not registered");
						case 1: break;
						default: throw new InvalidOperationException("Argument reference is ambiguous: " + string.Join(", ", argCandidates.Select(ldQuery => ldQuery.Group.Name + "." + ldQuery.Property.Name).ToArray()));
					}
					var entry = argCandidates.First();

					try {
						param ??= (entry.Property.ValueType == typeof(bool) ? "true" : "");
						object? value = null;
						if(entry.Group is ICLConvert) {
							value = ((ICLConvert)entry.Group).FromCLString(entry.Property, param);
						}
						if(value == null && !string.IsNullOrWhiteSpace(param)) {
							value = Convert.ChangeType(param, entry.Property.ValueType, CultureInfo.InvariantCulture);
						}
						entry.Group.SetValue(entry.Property, value);


					} catch(Exception ex) {
						throw new InvalidOperationException("Property (" + entry.Group.Name + "." + entry.Property.Name + ") could not be set", ex);
					}
				} else unnamedArgs?.Add(args[i]);
			}

			return true;
		}

		public void PrintHelp(bool detailed) {
			foreach(var item in items) PrintHelpTopic(item.Name, detailed);
			if(!detailed) {
				Console.WriteLine("Use --Help OR --Help <NameSpace> for more detailed info");
				Console.WriteLine();
			}
		}

		public void PrintHelpTopic(string topic, bool detailed) {
			var argGroup = items.SingleOrDefault(ldArgGroup => ldArgGroup.Name.InvEqualsOrdCI(topic));
			if(argGroup == null) {
				Console.WriteLine("There is no such topic");
				Console.WriteLine();
				return;
			}

			string argToString(SettingsProperty arg) {
				var names = new[] { arg.Name }.Concat(propToNames[arg]).ToArray();
				return string.Join(", ", names.Select(ldKey => ldKey.Length == 1 ? "-" + ldKey : "--" + ldKey));
			}


			//Console.ForegroundColor = ConsoleColor.DarkGreen;
			var descPad = Math.Max(("NameSpace: " + argGroup.Name).Length, argGroup.Properties.Select(prop => argToString(prop).Length).Max());


			var resMan = argGroup.ResourceManager;
			PrintLine(("▶2 NameSpace◀: " + argGroup.Name).PadRight(descPad, ' ') + resMan?.GetInvString($"{argGroup.Name}Description").OnNotNullReturn(s => " | ▶8 " + s + "◀"));
			Console.WriteLine();
			foreach(var prop in argGroup.Properties) {
				var example = resMan?.GetInvString($"{prop.Name}Example");
				var description = resMan?.GetInvString($"{prop.Name}Description");

				string? defaultValue;
				if(argGroup is ICLConvert clCOnvert) {
					defaultValue = clCOnvert.ToCLString(prop, prop.DefaultValue);
				} else {
					defaultValue = prop.DefaultValue?.ToString();
				}

				PrintLine(argToString(prop).PadRight(descPad, ' ') + " | " + example + " (" + ("".InvEquals(defaultValue) ? "▶8 <Empty>◀" : defaultValue ?? "▶8 <null>◀") + ")");
				if(detailed && !string.IsNullOrEmpty(description)) {
					if(!string.IsNullOrEmpty(description)) {
						PrintLine(!Utils.UsingWindows ? description : "▶8 " + description + "◀");
					}
					Console.WriteLine();
				}
			}
			Console.WriteLine();
		}


		private void PrintLine(string msg, bool noColors = false) { Print(msg, noColors); Console.WriteLine(); }
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
