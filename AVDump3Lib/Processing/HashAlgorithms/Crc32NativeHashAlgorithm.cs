using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class Crc32NativeHashAlgorithm : AVDNativeHashAlgorithm {
		[DllImport("AVDump3NativeLib")]
		private static extern IntPtr CRC32Create(out int blockSize);
		[DllImport("AVDump3NativeLib")]
		private static extern void CRC32Init(IntPtr handle);
		[DllImport("AVDump3NativeLib")]
		private static extern void CRC32Transform(IntPtr handle, byte* b, int length, byte lastBlock);
		[DllImport("AVDump3NativeLib")]
		private static extern void CRC32Final(IntPtr handle, byte* hash);

		public Crc32NativeHashAlgorithm() : base(CRC32Create, CRC32Init, CRC32Transform, CRC32Final, 4) { }
	}
}
