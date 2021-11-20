using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class Md4NativeHashAlgorithm : AVDNativeHashAlgorithm {
		private static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern IntPtr MD4Create(ref int hashLength, out int blockSize);
			[DllImport("AVDump3NativeLib")]
			internal static extern void MD4Init(IntPtr handle);
			[DllImport("AVDump3NativeLib")]
			internal static extern void MD4Transform(IntPtr handle, byte* b, int length);
			[DllImport("AVDump3NativeLib")]
			internal static extern void MD4Final(IntPtr handle, byte* b, int length, byte* hash);
		}


		public Md4NativeHashAlgorithm() : base(NativeMethods.MD4Create, NativeMethods.MD4Init, NativeMethods.MD4Transform, NativeMethods.MD4Final, 128) { }

	}
}
