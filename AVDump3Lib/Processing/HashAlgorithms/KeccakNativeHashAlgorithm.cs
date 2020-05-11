using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class KeccakNativeHashAlgorithm : AVDNativeHashAlgorithm {
		internal static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern IntPtr KeccakCreate(ref int hashLength, out int blockSize);
			[DllImport("AVDump3NativeLib")]
			internal static extern void KeccakInit(IntPtr handle);
			[DllImport("AVDump3NativeLib")]
			internal static extern void KeccakTransform(IntPtr handle, byte* b, int length);
			[DllImport("AVDump3NativeLib")]
			internal static extern void KeccakFinal(IntPtr handle, byte* b, int length, byte* hash);
		}

		public KeccakNativeHashAlgorithm(int hashBitCount) : base(NativeMethods.KeccakCreate, NativeMethods.KeccakInit, NativeMethods.KeccakTransform, NativeMethods.KeccakFinal, hashBitCount) { } //TODO
	}
}
