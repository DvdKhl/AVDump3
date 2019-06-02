using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {

	public unsafe class SHA3NativeHashAlgorithm : AVDNativeHashAlgorithm {
		[DllImport("AVDump3NativeLib")]
		private static extern IntPtr SHA3Create(out int blockSize);
		[DllImport("AVDump3NativeLib")]
		private static extern void SHA3Init(IntPtr handle);
		[DllImport("AVDump3NativeLib")]
		private unsafe static extern void SHA3Transform(IntPtr handle, byte* b, int length, byte lastBlock);
		[DllImport("AVDump3NativeLib")]
		private unsafe static extern void SHA3Final(IntPtr handle, byte* hash);

		public SHA3NativeHashAlgorithm() : base(SHA3Create, SHA3Init, SHA3Transform, SHA3Final, -1) { } //TODO
	}
}
