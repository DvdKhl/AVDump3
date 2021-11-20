using System;
using System.Collections.Immutable;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public sealed class Ed2kHashAlgorithm : AVDHashAlgorithm {

		public bool BlueIsRed { get; private set; }
		public ReadOnlyMemory<byte> RedHash { get; private set; }
		public ReadOnlyMemory<byte> BlueHash { get; private set; }


		public override int BlockSize => 9728000;

		private int blockHashOffset;
		private byte[] blockHashes = new byte[16 * 512]; //Good for ~4GB, increased if needed

		private readonly Md4HashAlgorithm md4;

		public Ed2kHashAlgorithm() {
			md4 = new Md4HashAlgorithm();
		}

		protected override unsafe void HashCore(in ReadOnlySpan<byte> data) {
			while(blockHashes.Length < blockHashOffset + (data.Length / BlockSize + 2) * 16) {
				Array.Resize(ref blockHashes, blockHashes.Length * 2);
			}

			var offset = 0;
			Span<byte> hashes = blockHashes;
			while(data.Length != offset) {
				md4.ComputeHash(data.Slice(offset, BlockSize), hashes.Slice(blockHashOffset, 16));
				blockHashOffset += 16;
				offset += BlockSize;
			}
		}


		/// <summary>Calculates both ed2k hashes</summary>
		/// <returns>Always returns the red hash</returns>
		public override ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) {
			Span<byte> hashes = blockHashes;
			Span<byte> hash = new byte[16];

			if(blockHashOffset == 0) {
				md4.ComputeHash(data, hash);

				RedHash = BlueHash = hash.ToArray();
				BlueIsRed = true;

			} else if(!data.IsEmpty) {
				//Data is not multiple of BlockLength (Common case)
				md4.ComputeHash(data, hashes.Slice(blockHashOffset, 16));
				blockHashOffset += 16;

				md4.ComputeHash(hashes[..blockHashOffset], hash);

				RedHash = BlueHash = hash.ToArray();
				BlueIsRed = true;

			} else {
				md4.ComputeHash(Span<byte>.Empty, hashes.Slice(blockHashOffset, 16));
				blockHashOffset += 16;

				Span<byte> hashNoNull = new byte[16];
				if(blockHashOffset == 32) {
					hashNoNull = hashes[..16];
				} else {
					md4.ComputeHash(hashes[..(blockHashOffset - 16)], hashNoNull);
				}

				Span<byte> hashWithNull = new byte[16];
				md4.ComputeHash(hashes[..blockHashOffset], hashWithNull);
				System.IO.File.WriteAllBytes("ed2k.bin", hashes[..blockHashOffset].ToArray());
				BlueHash = hashNoNull.ToArray();
				RedHash = hashWithNull.ToArray();
				hash = hashWithNull;

				AdditionalHashes = AdditionalHashes.Add(BlueHash.ToArray().ToImmutableArray());
			}

			return hash;
		}

		protected override void InitializeInternal() {
			//Called when TransformFinalBlock is called in Mono (not in NET) !
			md4.Initialize();

			BlueIsRed = false;
			RedHash = ReadOnlyMemory<byte>.Empty;
			BlueHash = ReadOnlyMemory<byte>.Empty;
			Array.Clear(blockHashes, 0, blockHashes.Length);
			blockHashOffset = 0;
		}
	}
}
