using System;
using System.Collections.Immutable;
using System.Security.Cryptography;

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
			BlueIsRed = false;
			RedHash = null;
			BlueHash = null;

			Span<byte> hashes = blockHashes;
			md4.ComputeHash(data, hashes.Slice(blockHashOffset, 16));
			blockHashOffset += 16;

			Span<byte> hashWithNull = new byte[16];
			md4.ComputeHash(hashes.Slice(0, blockHashOffset), hashWithNull);

			//https://wiki.anidb.info/w/Ed2k-hash
			ReadOnlySpan<byte> hash;
			BlueIsRed = false;
			if(!data.IsEmpty || data.IsEmpty && blockHashOffset == 0) {
				//Data is not multiple of BlockLength (Common case)
				BlueIsRed = true;
				hash = hashWithNull;
				BlueHash = hash.ToArray();
				RedHash = BlueHash;

			} else {
				Span<byte> hashNoNull = new byte[16];
				if(blockHashOffset == 32) {
					hashNoNull = hashes.Slice(0, 16);
				} else {
					md4.ComputeHash(hashes.Slice(0, blockHashOffset - 16), hashNoNull);
				}

				BlueHash = hashNoNull.ToArray();
				RedHash = hashWithNull.ToArray();
				hash = hashWithNull;
			}

			AdditionalHashes = ImmutableArray.Create(BlueHash.ToArray().ToImmutableArray());

			return hash;
		}

		public override void Initialize() {
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
