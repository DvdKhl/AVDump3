using AVDump3Lib.Processing.BlockBuffers;
using AVDump3Lib.Processing.HashAlgorithms;
using System;
using System.Collections.Immutable;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace AVDump3Lib.Processing.BlockConsumers {
	public class HashCalculator : BlockConsumer {
		public int ReadLength { get; }
		public ImmutableArray<byte> HashValue { get; private set; }

		public IAVDHashAlgorithm HashAlgorithm { get; }
		public HashCalculator(string name, IBlockStreamReader reader, IAVDHashAlgorithm transform) : base(name, reader) {
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
			} while(Reader.Advance(bytesProcessed) && bytesProcessed != 0);

			var lastBytes = block.Length - bytesProcessed;

			HashValue = HashAlgorithm.TransformFinalBlock(block.Slice(bytesProcessed, lastBytes)).ToArray().ToImmutableArray();

			Reader.Advance(lastBytes);
		}

		public override void Dispose() {
			HashAlgorithm.Dispose();
			base.Dispose();
		}
	}



}
