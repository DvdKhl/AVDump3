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
		var path = AppDomain.CurrentDomain.BaseDirectory;

		if(libraryName.Equals("AVDump3NativeLib")) {
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				if(RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
					return
						NativeLibrary.TryLoad(Path.Combine(path, "AVDump3NativeLib-arm64"), assembly, searchPath, out var handle) ||
						NativeLibrary.TryLoad(Path.Combine(path, "AVDump3NativeLib-arm64-musl"), assembly, searchPath, out handle)
						? handle : IntPtr.Zero;
				} else if(RuntimeInformation.ProcessArchitecture == Architecture.X64) {
					return
						NativeLibrary.TryLoad(Path.Combine(path, "AVDump3NativeLib-x64"), assembly, searchPath, out var handle) ||
						NativeLibrary.TryLoad(Path.Combine(path, "AVDump3NativeLib-x64-musl"), assembly, searchPath, out handle)
						? handle : IntPtr.Zero;
				}
			}
		}
		if(libraryName.Equals("MediaInfo")) {
			if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
				if(RuntimeInformation.ProcessArchitecture == Architecture.Arm64) {
					return
						NativeLibrary.TryLoad(Path.Combine(path, "MediaInfo-arm64"), assembly, searchPath, out var handle) ||
						NativeLibrary.TryLoad(Path.Combine(path, "MediaInfo-arm64-musl"), assembly, searchPath, out handle)
						? handle : IntPtr.Zero;
				} else if(RuntimeInformation.ProcessArchitecture == Architecture.X64) {
					return
						NativeLibrary.TryLoad(Path.Combine(path, "MediaInfo-x64"), assembly, searchPath, out var handle) ||
						NativeLibrary.TryLoad(Path.Combine(path, "MediaInfo-x64-musl"), assembly, searchPath, out handle)
						? handle : IntPtr.Zero;
				}
			}
		}

		return IntPtr.Zero;
	}
}
