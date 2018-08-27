using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.System.Security.Cryptography;
using System;
using System.Security.Cryptography;
using System.Threading;

namespace AVDump3Lib.Processing.BlockConsumers {
    public class HashCalculator : BlockConsumer {
        //Needs to be non-empty otherwise pinvoke makes it a null pointer (See CRC32Native hash)
        private static readonly byte[] EmptyArray = new byte[] { 0 };

        public ReadOnlyMemory<byte> Hash { get; private set; }

        public IHashAlgorithmWithSpan Transform { get; }
        public HashCalculator(string name, IBlockStreamReader reader, IHashAlgorithmWithSpan transform) : base(name, reader) {
            Transform = transform;
        }

        protected override void DoWork(CancellationToken ct) {
            Transform.Initialize();

            int bytesProcessed;
            do {
                ct.ThrowIfCancellationRequested();

                var block = Reader.GetBlock();
                Transform.HashCore(block);
                bytesProcessed = block.Length;

            } while(Reader.Advance(bytesProcessed));

            Hash = Transform.HashFinal();
        }
    }
}
