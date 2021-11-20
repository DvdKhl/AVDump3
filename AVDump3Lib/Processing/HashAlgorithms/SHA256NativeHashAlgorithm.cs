using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.HashAlgorithms;

public unsafe class SHA256NativeHashAlgorithm : AVDNativeHashAlgorithm {
	internal static class NativeMethods {
		[DllImport("AVDump3NativeLib")]
		internal static extern IntPtr SHA256Create(ref int hashLength, out int blockSize);
		[DllImport("AVDump3NativeLib")]
		internal static extern void SHA256Init(IntPtr handle);
		[DllImport("AVDump3NativeLib")]
		internal static extern void SHA256Transform(IntPtr handle, byte* b, int length);
		[DllImport("AVDump3NativeLib")]
		internal static extern void SHA256Final(IntPtr handle, byte* b, int length, byte* hash);
	}

	public SHA256NativeHashAlgorithm() : base(NativeMethods.SHA256Create, NativeMethods.SHA256Init, NativeMethods.SHA256Transform, NativeMethods.SHA256Final, 256) { } //TODO
}
