using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {

    public class Crc32CIntelHashAlgorithm : HashAlgorithm {
        [DllImport("AVDump3NativeLib.dll")]
        private static extern IntPtr CRC32CCreate();
        [DllImport("AVDump3NativeLib.dll")]
        private static extern void CRC32CInit(IntPtr handle);
        [DllImport("AVDump3NativeLib.dll")]
        private unsafe static extern void CRC32CTransform(IntPtr handle, byte* b, int length);
        [DllImport("AVDump3NativeLib.dll")]
        private unsafe static extern void CRC32CFinal(IntPtr handle, byte* hash);
        [DllImport("AVDump3NativeLib.dll")]
        private static extern void FreeHashObject(IntPtr handle);


        private IntPtr handle;
        private bool disposed;

        public Crc32CIntelHashAlgorithm() {
            handle = CRC32CCreate();
        }

        public override void Initialize() {
            CRC32CInit(handle);
        }

        protected unsafe override void HashCore(byte[] array, int ibStart, int cbSize) {
            fixed(byte* bPtr = array) {
                CRC32CTransform(handle, bPtr + ibStart, cbSize);
            }
        }

        protected unsafe override byte[] HashFinal() {
            var b = new byte[4];
            fixed (byte* bPtr = b) {
                CRC32CFinal(handle, bPtr);
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
