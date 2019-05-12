using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe class TigerNativeHashAlgorithm : AVDNativeHashAlgorithm {
		[DllImport("AVDump3NativeLib")]
		private static extern IntPtr TigerCreate(out int blockSize);
		[DllImport("AVDump3NativeLib")]
		private static extern void TigerInit(IntPtr handle);
		[DllImport("AVDump3NativeLib")]
		private static extern void TigerTransform (IntPtr handle, byte* b, int length, byte lastBlock);
        [DllImport("AVDump3NativeLib")]
		private static extern void TigerFinal(IntPtr handle, byte* hash);

        public TigerNativeHashAlgorithm() : base(TigerCreate, TigerInit, TigerTransform, TigerFinal, 24) { }
        
	}
}
