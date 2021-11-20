using AVDump3Lib.Settings.Core;
using System.Collections.Generic;

namespace AVDump3Lib.Settings.CLArguments;

public interface ICLSettingsHandler : ISettingsHandler {
	bool ParseArgs(string[] args, ICollection<string> unnamedArgs);
	void PrintHelp(bool detailed);
	void PrintHelpTopic(string topic, bool detailed);
}
