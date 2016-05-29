using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Modules {
	public interface IAVD3Module {
		void Initialize(IReadOnlyCollection<IAVD3Module> modules);
	}
}
