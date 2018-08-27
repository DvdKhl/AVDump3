using AVDump3Lib.Processing.BlockBuffers;
using System.Security.Cryptography;
using System.Threading;

namespace AVDump3Lib.Processing.BlockConsumers {
	public class HashCalculator : BlockConsumer {
		//Needs to be non-empty otherwise pinvoke makes it a null pointer (See CRC32Native hash)
		private readonly static byte[] EmptyArray = new byte[] { 0 };

		public byte[] Hash { get; private set; }

		public HashAlgorithm Transform { get; }
        public HashCalculator(string name, IBlockStreamReader reader, ICryptoTransform transform) : base(name, reader) {
            Transform = transform;
        }



		protected override void DoWork(CancellationToken ct) {
			if(Transform is HashAlgorithm) {
				((HashAlgorithm)Transform).Initialize();
			}

			int toRead;
			do {
				ct.ThrowIfCancellationRequested();
				Transform.TransformBlock(Reader.GetBlock(out toRead), 0, toRead, null, 0);
			} while(Reader.Advance());

			Hash = Transform.TransformFinalBlock(EmptyArray, 0, 0);
			if(Transform is HashAlgorithm) {
				Hash = ((HashAlgorithm)Transform).Hash;
			}
		}
	}
}
