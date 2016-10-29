using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Misc {
	public static class Utils {
		public static bool UsingMono { get; } = Type.GetType("Mono.Runtime") != null;
	}
}
