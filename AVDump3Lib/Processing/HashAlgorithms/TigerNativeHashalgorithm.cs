using System;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class TigerNativeHashAlgorithm : AVDNativeHashAlgorithm {
		private static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern IntPtr TigerCreate(ref int hashLength, out int blockSize);
			[DllImport("AVDump3NativeLib")]
			internal static extern void TigerInit(IntPtr handle);
			[DllImport("AVDump3NativeLib")]
			internal static extern void TigerTransform(IntPtr handle, byte* b, int length);
			[DllImport("AVDump3NativeLib")]
			internal static extern void TigerFinal(IntPtr handle, byte* b, int length, byte* hash);
		}

		public TigerNativeHashAlgorithm() : base(NativeMethods.TigerCreate, NativeMethods.TigerInit, NativeMethods.TigerTransform, NativeMethods.TigerFinal, 192) { }

	}
}
