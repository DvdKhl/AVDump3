using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Settings.Core {
    public interface ISettingsHandler {
        void Register(IEnumerable<SettingsObject> settingsObjects);
    }

}
