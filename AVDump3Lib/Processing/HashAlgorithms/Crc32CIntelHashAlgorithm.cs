using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
    public unsafe class Crc32CIntelHashAlgorithm : AVDNativeHashAlgorithm {
        [DllImport("AVDump3NativeLib.dll")]
        private static extern IntPtr CRC32CCreate(out int blockSize);
        [DllImport("AVDump3NativeLib.dll")]
        private static extern void CRC32CInit(IntPtr handle);
        [DllImport("AVDump3NativeLib.dll")]
        private static extern void CRC32CTransform(IntPtr handle, byte* b, int length, byte lastBlock);
        [DllImport("AVDump3NativeLib.dll")]
        private static extern void CRC32CFinal(IntPtr handle, byte* hash);

        public Crc32CIntelHashAlgorithm() : base(CRC32CCreate, CRC32CInit, CRC32CTransform, CRC32CFinal) { }
    }
}
