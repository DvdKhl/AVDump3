using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace AVDump3Lib.Misc;

public static class Utils {
	public static bool UsingWindows { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
	public static UTF8Encoding UTF8EncodingNoBOM { get; } = new UTF8Encoding(false);


	public static void AddNativeLibraryResolver() {
		NativeLibrary.SetDllImportResolver(Assembly.GetExecutingAssembly(), DllImportResolver);
	}
	private static IntPtr DllImportResolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath) {
		if(libraryName.Equals("AVDump3NativeLib")) {
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
				if(RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
					return NativeLibrary.Load("AVDump3NativeLib-aarch64", assembly, searchPath);
				} else if(RuntimeInformation.ProcessArchitecture == Architecture.X64) {
					return NativeLibrary.Load("AVDump3NativeLib-x64", assembly, searchPath);
				}
			}
		}
		return IntPtr.Zero;
	}
}
