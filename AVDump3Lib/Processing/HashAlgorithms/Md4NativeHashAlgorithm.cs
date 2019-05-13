using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class Md4NativeHashAlgorithm : AVDNativeHashAlgorithm {
		[DllImport("AVDump3NativeLib")]
		private static extern IntPtr MD4Create(out int blockSize);
		[DllImport("AVDump3NativeLib")]
		private static extern void MD4Init(IntPtr handle);
		[DllImport("AVDump3NativeLib")]
		private static extern void MD4Transform(IntPtr handle, byte* b, int length, byte lastBlock);
		[DllImport("AVDump3NativeLib")]
		private static extern void MD4Final(IntPtr handle, byte* hash);

		[DllImport("AVDump3NativeLib")]
		public static extern void MD4ComputeHash(byte* b, int length, byte* hash);

		public Md4NativeHashAlgorithm() : base(MD4Create, MD4Init, MD4Transform, MD4Final, 16) { }

	}
}
