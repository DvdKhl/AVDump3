using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class SHA3NativeHashAlgorithm : AVDNativeHashAlgorithm {
		internal static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern IntPtr SHA3Create(ref int hashLength, out int blockSize);
			[DllImport("AVDump3NativeLib")]
			internal static extern void SHA3Init(IntPtr handle);
			[DllImport("AVDump3NativeLib")]
			internal static extern void SHA3Transform(IntPtr handle, byte* b, int length);
			[DllImport("AVDump3NativeLib")]
			internal static extern void SHA3Final(IntPtr handle, byte* b, int length, byte* hash);
		}

		public SHA3NativeHashAlgorithm(int hashBitCount) : base(NativeMethods.SHA3Create, NativeMethods.SHA3Init, NativeMethods.SHA3Transform, NativeMethods.SHA3Final, hashBitCount) { } //TODO
	}
}
