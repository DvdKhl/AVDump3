using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Settings.Core {
	public interface ICLSettingsHandler {
		void Register(params SettingsObject[] settingsObjects);
	}

}
