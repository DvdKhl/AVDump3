using System.Security.Cryptography;
using System.Threading;
using AVDump3Lib.BlockBuffers;
using AVDump3Lib.Information.MetaInfo;

namespace AVDump3Lib.BlockConsumers {
    public class HashCalculator : BlockConsumer {
        public HashAlgorithm HashAlgorithm { get; }
        public HashCalculator(IBlockStreamReader reader, HashAlgorithm hashAlgorithm) : base(reader) {
            HashAlgorithm = hashAlgorithm;
        }


        protected override void DoWork(CancellationToken ct) {
			HashAlgorithm.Initialize();

			int toRead;
            do {
                ct.ThrowIfCancellationRequested();
                HashAlgorithm.TransformBlock(Reader.GetBlock(out toRead), 0, toRead, null, 0);
            } while(Reader.Advance());

            HashAlgorithm.TransformFinalBlock(new byte[0], 0, 0);
        }
    }
}
