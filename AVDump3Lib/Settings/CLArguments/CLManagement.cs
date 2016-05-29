using AVDump3Lib.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace AVDump3Lib.Settings.CLArguments {

	public class CLManagement {
		private List<ArgGroup> argGroups;
		private Action<string> unnamedArgHandler;

		public CLManagement() { argGroups = new List<ArgGroup>(); }


		public void RegisterArgGroups(params ArgGroup[] argGroups) {
			foreach(var argGroup in argGroups)  RegisterArgGroup(argGroup);
		}

		public void RegisterArgGroup(ArgGroup argGroup) {
			if(argGroup == null) throw new ArgumentNullException("argGroup");
			if(argGroups.Any(ldArgGroup => ldArgGroup.NameSpace.Equals(argGroup.NameSpace))) throw new ArgumentException("Owner already registered");
			argGroups.Add(argGroup);
		}

		public void SetUnnamedParamHandler(Action<string> unnamedArgHandler) { this.unnamedArgHandler = unnamedArgHandler; }


		public bool ParseArgs(string[] args) {

			if(args.Length == 0 || args[0].ToLower().Equals("--Help".ToLower())) {
				if(args.Length == 2) PrintTopic(args[1], true); else PrintHelp(true);
				return false;
			}

			for(int i = 0; i < args.Length; i++) {
				var match = Regex.Match(args[i], @"^--?(?:(?<NameSpace>[a-zA-Z0-9]+)\.)?(?<Arg>[a-zA-Z0-9][a-zA-Z0-9\-]*)(?:=(?<Param>.*))?$");
				var nameSpace = match.Groups["NameSpace"].Success ? match.Groups["NameSpace"].Value : null;
				var param = match.Groups["Param"].Success ? match.Groups["Param"].Value : null;
				var name = match.Groups["Arg"].Value;

				if(args[i][0] == '-' && name.Equals(string.Empty)) throw new FormatException("Invalid argument structure");
				if(args[i][0] == '-' && args[i][1] != '-') {
					if(name.Length > 1) {
						if(param != null) throw new FormatException("Multiple one letter arguments may not have parameters");
						ParseArgs(name.Select(ldSwitch => "--" + (nameSpace != null ? nameSpace + "." : "") + ldSwitch).ToArray());
					} else {
						ParseArgs(new string[] { "--" + (nameSpace != null ? nameSpace + "." : "") + name + (param != null ? "=" + param : "") });
					}

				} else if(args[i][0] == '-' && args[i][1] == '-') {
					var argCandidates = from g in argGroups
										where nameSpace == null || nameSpace.ToLower().Equals(g.NameSpace.ToLower())
										from a in g.Args
										where a.Keys.Any(ldKey => ldKey.Equals(name))
										select new { Group = g, Arg = a };

					switch(argCandidates.Count()) {
						case 0: throw new InvalidOperationException("Argument (" + (!string.IsNullOrEmpty(nameSpace) ? nameSpace + "." : "") + name + ") is not registered");
						case 1: break;
						default: throw new InvalidOperationException("Argument reference is ambiguous: " + string.Join(", ", argCandidates.Select(ldQuery => ldQuery.Group.NameSpace + "." + name).ToArray()));
					}
					var entry = argCandidates.First();


					try {
						entry.Arg.ApplyCommand(param);
					} catch(Exception ex) {
						throw new Exception("Arg handler (" + entry.Group.NameSpace + "." + entry.Arg.Keys.First() + ") threw an error", ex);
					}
				} else unnamedArgHandler?.Invoke(args[i]);
			}

			foreach(var argGroup in argGroups) argGroup.Parsed?.Invoke();

			return true;
		}


		public void PrintHelp(bool detailed) {
			foreach(var argGroup in argGroups) PrintTopic(argGroup.NameSpace, detailed);
			if(!detailed) {
				Console.WriteLine("Use --Help OR --Help <NameSpace> for more detailed info");
				Console.WriteLine();
			}
		}

		public void PrintTopic(string topic, bool detailed) {
			var argGroup = argGroups.SingleOrDefault(ldArgGroup => ldArgGroup.NameSpace.ToLower().Equals(topic.ToLower()));
			if(argGroup == null) {
				Console.WriteLine("There is no such topic");
				Console.WriteLine();
				return;
			}

			var colorset = new ColorSet {
				NameSpace = ConsoleColor.DarkGreen
			};


			Func<ArgStructure, string> argToString = arg => string.Join(", ", arg.Keys.Select(ldKey => ldKey.Length == 1 ? "-" + ldKey : "--" + ldKey));

			//Console.ForegroundColor = ConsoleColor.DarkGreen;
			var descPad = Math.Max(("NameSpace: " + argGroup.NameSpace).Length, argGroup.Args.Select(arg => argToString(arg).Length).Max());


			PrintLine(("▶2 NameSpace◀: " + argGroup.NameSpace).PadRight(descPad, ' ') + argGroup.Description.OnNotNullReturn(s => " | ▶8 " + s));
			Console.WriteLine();
			foreach(var arg in argGroup.Args) {

				PrintLine(argToString(arg).PadRight(descPad, ' ') + " | " + arg.Example);
				if(detailed && !string.IsNullOrEmpty(arg.Description)) {
					if(!string.IsNullOrEmpty(arg.Description)) PrintLine("▶8 " + arg.Description);
					Console.WriteLine();
				}
			}
			Console.WriteLine();
		}

		private class ColorSet {
			public ConsoleColor NameSpace;
		}


		private void PrintLine(string msg, bool noColors = false) { Print(msg, noColors); Console.WriteLine(); }
		private void Print(string msg, bool noColors = false) {
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

			for(int i = 0; i < msg.Length; i++) {
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
