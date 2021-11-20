using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Misc {
	public static class Utils {
		public static bool UsingWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		public static UTF8Encoding UTF8EncodingNoBOM { get; } = new UTF8Encoding(false);
	}
}
