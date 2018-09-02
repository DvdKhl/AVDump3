using AVDump3Lib.Processing.BlockBuffers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace AVDump3Lib.Processing.BlockConsumers {
    public class CopyToFileBlockConsumer : BlockConsumer {
        public string FilePath { get; }

        public CopyToFileBlockConsumer(string name, IBlockStreamReader reader, string filePath) : base(name, reader) {
            FilePath = filePath;
        }

        protected override void DoWork(CancellationToken ct) {
            ReadOnlySpan<byte> block;

            using(var fileStream = File.OpenWrite(FilePath)) {
                do {
                    ct.ThrowIfCancellationRequested();
                    block = Reader.GetBlock(Reader.SuggestedReadLength);
                    fileStream.Write(block);
                } while(Reader.Advance(block.Length));
            }
        }
    }
}

