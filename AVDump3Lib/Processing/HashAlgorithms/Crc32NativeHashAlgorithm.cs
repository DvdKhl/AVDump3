using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
    public class Crc32NativeHashAlgorithm : HashAlgorithm {
        [DllImport("AVDump3NativeLib.dll")]
        private static extern IntPtr CRC32Create();
        [DllImport("AVDump3NativeLib.dll")]
        private static extern void CRC32Init(IntPtr handle);
        [DllImport("AVDump3NativeLib.dll")]
        private unsafe static extern void CRC32Transform(IntPtr handle, byte* b, int length);
        [DllImport("AVDump3NativeLib.dll")]
        private unsafe static extern void CRC32Final(IntPtr handle, byte* hash);
        [DllImport("AVDump3NativeLib.dll")]
        private static extern void FreeHashObject(IntPtr handle);

        private IntPtr handle;
        private bool disposed;

        public Crc32NativeHashAlgorithm() {
            handle = CRC32Create();
        }

        public override void Initialize() {
            CRC32Init(handle);
        }

        protected unsafe override void HashCore(byte[] array, int ibStart, int cbSize) {
			if(cbSize == 0) return;

            fixed (byte* bPtr = array) {
                CRC32Transform(handle, bPtr + ibStart, cbSize);
            }
        }

        protected unsafe override byte[] HashFinal() {
            var b = new byte[4];
            fixed (byte* bPtr = b) {
                CRC32Final(handle, bPtr);
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
