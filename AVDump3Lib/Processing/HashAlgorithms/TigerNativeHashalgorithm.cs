using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {

	public class TigerNativeHashAlgorithm : HashAlgorithm {
		[DllImport("AVDump3NativeLib.dll")]
		private static extern IntPtr TigerCreate();
		[DllImport("AVDump3NativeLib.dll")]
		private static extern void TigerInit(IntPtr handle);
		[DllImport("AVDump3NativeLib.dll")]
		private unsafe static extern void TigerTransform(IntPtr handle, byte* b, int length);
		[DllImport("AVDump3NativeLib.dll")]
		private unsafe static extern void TigerFinal(IntPtr handle, byte* hash);
		[DllImport("AVDump3NativeLib.dll")]
		private static extern void FreeHashObject(IntPtr handle);


		private IntPtr handle;
		private bool disposed;

		public TigerNativeHashAlgorithm() {
			handle = TigerCreate();
		}

		public override void Initialize() {
			TigerInit(handle);
		}

		protected unsafe override void HashCore(byte[] array, int ibStart, int cbSize) {
			fixed (byte* bPtr = array) {
				TigerTransform(handle, bPtr + ibStart, cbSize);
			}
		}

		protected unsafe override byte[] HashFinal() {
			var b = new byte[3 * 8];
			fixed (byte* bPtr = b) {
				TigerFinal(handle, bPtr);
			}
			return b;
		}

		protected override void Dispose(bool disposing) {
			base.Dispose(disposing);
			if(!disposed) {
				FreeHashObject(handle);
				disposed = true;
			}
		}
	}
}
