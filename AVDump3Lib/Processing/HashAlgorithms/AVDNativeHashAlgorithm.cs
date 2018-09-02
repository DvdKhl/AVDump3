using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace AVDump3Lib.Processing.HashAlgorithms {
    public unsafe class AVDNativeHashAlgorithm : AVDHashAlgorithm {
        protected delegate IntPtr CreateHandler(out int blockSize);
        protected delegate void InitHandler(IntPtr handle);
        protected delegate void TransformHandler(IntPtr handle, byte* b, int length, byte lastBlock);
        protected delegate void FinalHandler(IntPtr handle, byte* hash);

        [DllImport("AVDump3NativeLib.dll")]
        private static extern void FreeHashObject(IntPtr handle);

        private readonly IntPtr handle;
        private readonly InitHandler init;
        private readonly TransformHandler transform;
        private readonly FinalHandler final;

        protected AVDNativeHashAlgorithm(CreateHandler create, InitHandler init, TransformHandler transform, FinalHandler final) {
            handle = create(out var blockSize);
            BlockSize = blockSize;

            this.init = init;
            this.transform = transform;
            this.final = final;
        }

        public override int BlockSize { get; }

        public override void Initialize() => init(handle);

        protected override unsafe void HashCore(ReadOnlySpan<byte> data) {
            fixed (byte* bPtr = &data[0]) {
                transform(handle, bPtr, data.Length, 0);
            }
        }
        public override ReadOnlySpan<byte> TransformFinalBlock(ReadOnlySpan<byte> data) {
            var b = new byte[4];
            fixed (byte* hashPtr = b, bPtr = data) {
                if(data.Length > 0) transform(handle, bPtr, data.Length, 1);
                final(handle, hashPtr);
            }
            return b;
        }

        protected override void Dispose(bool disposing) {
            if(!DisposedValue) FreeHashObject(handle);
            base.Dispose(disposing);
        }

    }
}
