using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class Crc32NativeHashAlgorithm : AVDNativeHashAlgorithm {
		internal static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			internal static extern IntPtr CRC32Create(ref int hashLength, out int blockSize);
			[DllImport("AVDump3NativeLib")]
			internal static extern void CRC32Init(IntPtr handle);
			[DllImport("AVDump3NativeLib")]
			internal static extern void CRC32Transform(IntPtr handle, byte* b, int length);
			[DllImport("AVDump3NativeLib")]
			internal static extern void CRC32Final(IntPtr handle, byte* b, int length, byte* hash);
		}

		public Crc32NativeHashAlgorithm() : base(NativeMethods.CRC32Create, NativeMethods.CRC32Init, NativeMethods.CRC32Transform, NativeMethods.CRC32Final, 32) { }
	}
}
