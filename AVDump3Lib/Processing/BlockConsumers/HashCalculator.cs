using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.HashAlgorithms;
using System;
using System.Security.Cryptography;
using System.Threading;

namespace AVDump3Lib.Processing.BlockConsumers {
    public class HashCalculator : BlockConsumer {
        public int ReadLength { get; }
        public ReadOnlyMemory<byte> HashValue;

        public AVDHashAlgorithm HashAlgorithm { get; }
        public HashCalculator(string name, IBlockStreamReader reader, AVDHashAlgorithm transform) : base(name, reader) {
            HashAlgorithm = transform;

            var length = ((reader.SuggestedReadLength / transform.BlockSize) + 1) * transform.BlockSize;
            if(length > reader.MaxReadLength) {
                length -= transform.BlockSize;
                if(length == 0) {
                    throw new Exception("Min/Max BlockLength too restrictive") {
                        Data = {
                            { "TransformName", Name },
                            { "MaxBlockLength", reader.MaxReadLength },
                            { "HashBlockLength", transform.BlockSize }
                        }
                    };
                }
            }
            ReadLength = length;
        }

        protected override void DoWork(CancellationToken ct) {
            HashAlgorithm.Initialize();

            ReadOnlySpan<byte> block;
            int bytesProcessed;
            do {
                ct.ThrowIfCancellationRequested();

                block = Reader.GetBlock(ReadLength);
                bytesProcessed = HashAlgorithm.TransformFullBlocks(block);
            } while(Reader.Advance(bytesProcessed) && bytesProcessed == block.Length);

            HashValue = HashAlgorithm.TransformFinalBlock(block.Slice(bytesProcessed, block.Length - bytesProcessed)).ToArray();
        }
    }
}
