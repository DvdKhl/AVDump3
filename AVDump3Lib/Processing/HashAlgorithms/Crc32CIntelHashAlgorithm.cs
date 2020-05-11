using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class Crc32CIntelHashAlgorithm : AVDNativeHashAlgorithm {
		private static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern IntPtr CRC32CCreate(ref int hashLength, out int blockSize);
			[DllImport("AVDump3NativeLib")]
			internal static extern void CRC32CInit(IntPtr handle);
			[DllImport("AVDump3NativeLib")]
			internal static extern void CRC32CTransform(IntPtr handle, byte* b, int length);
			[DllImport("AVDump3NativeLib")]
			internal static extern void CRC32CFinal(IntPtr handle, byte* b, int length, byte* hash);
		}

		public Crc32CIntelHashAlgorithm() : base(NativeMethods.CRC32CCreate, NativeMethods.CRC32CInit, NativeMethods.CRC32CTransform, NativeMethods.CRC32CFinal, 32) { }
	}
}
