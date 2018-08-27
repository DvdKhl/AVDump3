using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {

	public class TigerNativeHashAlgorithm : ICryptoTransform {
		[DllImport("AVDump3NativeLib.dll")]
		private static extern IntPtr TigerCreate();
		[DllImport("AVDump3NativeLib.dll")]
		private static extern void TigerInit(IntPtr handle);
		[DllImport("AVDump3NativeLib.dll")]
		private unsafe static extern void TigerTransform(IntPtr handle, byte* b, int length, byte lastBlock);
		[DllImport("AVDump3NativeLib.dll")]
		private unsafe static extern void TigerFinal(IntPtr handle, byte* hash);
		[DllImport("AVDump3NativeLib.dll")]
		private static extern void FreeHashObject(IntPtr handle);


		private IntPtr handle;
		public TigerNativeHashAlgorithm() {
			handle = TigerCreate();
		}

		public int InputBlockSize => 8 * 8;
		public int OutputBlockSize => 3 * 8;
		public bool CanTransformMultipleBlocks => true;

		public bool CanReuseTransform => true;


		public unsafe int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset) {
			fixed (byte* bPtr = inputBuffer) {
				TigerTransform(handle, bPtr + inputOffset, inputCount, 0);
			}
			return inputCount;
		}

		public unsafe byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount) {
			var b = new byte[3 * 8];
			fixed (byte* bPtr = b, iPtr = inputBuffer) {
				TigerTransform(handle, iPtr + inputOffset, inputCount, 1);
				TigerFinal(handle, bPtr);
			}
			return b;
		}

		public void Dispose() {
			FreeHashObject(handle);
		}
	}
}
