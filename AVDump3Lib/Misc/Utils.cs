using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Misc {
	public static class Utils {
		public static bool UsingWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		public static UTF8Encoding UTF8EncodingNoBOM { get; } = new UTF8Encoding(false);
	}
}
