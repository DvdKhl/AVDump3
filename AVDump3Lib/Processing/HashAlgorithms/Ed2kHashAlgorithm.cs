using System;
using System.Security.Cryptography;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public sealed class Ed2kHashAlgorithm : AVDHashAlgorithm {

		public bool BlueIsRed { get; private set; }
		public ReadOnlyMemory<byte> RedHash { get; private set; }
		public ReadOnlyMemory<byte> BlueHash { get; private set; }

		public override int BlockSize => 9728000;

		private int blockHashOffset = 0;
		private readonly byte[] nullMd4Hash = new byte[64];
		private byte[] blockHashes = new byte[16 * 512]; //Good for ~4GB, increased if needed

		private readonly Md4HashAlgorithm md4;

		public Ed2kHashAlgorithm() {
			md4 = new Md4HashAlgorithm();
			md4.ComputeHash(Span<byte>.Empty, nullMd4Hash);
		}

		protected override unsafe void HashCore(in ReadOnlySpan<byte> data) {
			if(blockHashes.Length < blockHashOffset + ((data.Length / BlockSize) + 2) * 16) {
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

			Span<byte> hashNoNull = new byte[16];
			md4.ComputeHash(hashes.Slice(0, blockHashOffset), hashNoNull);

			//https://wiki.anidb.info/w/Ed2k-hash
			ReadOnlySpan<byte> hash;
			BlueIsRed = false;
			if(data.Length != 0) {
				//Data is not multiple of BlockLength (Common case)
				BlueIsRed = true;
				hash = hashNoNull;
				BlueHash = hash.ToArray();
				RedHash = BlueHash;

			} else {
				Span<byte> hashWithNull = new byte[16];
				nullMd4Hash.CopyTo(hashes.Slice(blockHashOffset, 16));
				blockHashOffset += 16;
				md4.ComputeHash(hashes.Slice(0, blockHashOffset), hashWithNull);


				BlueHash = hashNoNull.ToArray();
				RedHash = hashWithNull.ToArray();
				hash = hashWithNull;
			}

			return hash;
		}

		public override void Initialize() {
			//Called when TransformFinalBlock is called in Mono (not in NET) !
			md4.Initialize();
		}
	}
}
