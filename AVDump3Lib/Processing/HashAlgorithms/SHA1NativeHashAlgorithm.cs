using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class SHA1NativeHashAlgorithm : AVDNativeHashAlgorithm {
		internal static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern IntPtr SHA1Create(ref int hashLength, out int blockSize);
			[DllImport("AVDump3NativeLib")]
			internal static extern void SHA1Init(IntPtr handle);
			[DllImport("AVDump3NativeLib")]
			internal static extern void SHA1Transform(IntPtr handle, byte* b, int length);
			[DllImport("AVDump3NativeLib")]
			internal static extern void SHA1Final(IntPtr handle, byte* b, int length, byte* hash);
		}

		public SHA1NativeHashAlgorithm() : base(NativeMethods.SHA1Create, NativeMethods.SHA1Init, NativeMethods.SHA1Transform, NativeMethods.SHA1Final, 160) { } //TODO
	}
}
