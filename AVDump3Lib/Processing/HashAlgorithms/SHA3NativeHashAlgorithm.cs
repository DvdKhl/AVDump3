using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {

	public class SHA3NativeHashAlgorithm : ICryptoTransform {
		[DllImport("AVDump3NativeLib.dll")]
		private static extern IntPtr SHA3Create();
		[DllImport("AVDump3NativeLib.dll")]
		private static extern void SHA3Init(IntPtr handle);
		[DllImport("AVDump3NativeLib.dll")]
		private unsafe static extern void SHA3Transform(IntPtr handle, byte* b, int length, byte lastBlock);
		[DllImport("AVDump3NativeLib.dll")]
		private unsafe static extern void SHA3Final(IntPtr handle, byte* hash);
		[DllImport("AVDump3NativeLib.dll")]
		private static extern void FreeHashObject(IntPtr handle);


		private IntPtr handle;
		public SHA3NativeHashAlgorithm() {
			handle = SHA3Create();
		}

		public int InputBlockSize => 25 * 8;
		public int OutputBlockSize => 64;
		public bool CanTransformMultipleBlocks => true;

		public bool CanReuseTransform => true;


		public unsafe int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) {
			fixed (byte* bPtr = inputBuffer) {
				SHA3Transform(handle, bPtr + inputOffset, inputCount, 0);
			}
			return inputCount;
		}

		public unsafe byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) {
			var b = new byte[OutputBlockSize];
			fixed (byte* bPtr = b, iPtr = inputBuffer) {
				SHA3Transform(handle, iPtr + inputOffset, inputCount, 1);
				SHA3Final(handle, bPtr);
			}
			return b;
		}

		public void Dispose() {
			FreeHashObject(handle);
		}
	}
}
