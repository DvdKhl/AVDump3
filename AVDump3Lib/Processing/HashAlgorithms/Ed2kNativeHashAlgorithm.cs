using System;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe sealed class Ed2kNativeHashAlgorithm : AVDHashAlgorithm {

		public bool BlueIsRed { get; private set; }
		public ReadOnlyMemory<byte> RedHash { get; private set; }
		public ReadOnlyMemory<byte> BlueHash { get; private set; }

		public override int BlockSize => 9728000;

		private int blockHashOffset = 0;
		private byte[] nullMd4Hash = new byte[64];
		private byte[] blockHashes = new byte[16 * 512]; //Good for ~4GB, increased if needed

		public Ed2kNativeHashAlgorithm() {
			fixed (byte* emptyPtr = Span<byte>.Empty)
			fixed (byte* emptyHashPtr = nullMd4Hash) {
				Md4NativeHashAlgorithm.MD4ComputeHash(emptyPtr, 0, emptyHashPtr);
			}
		}

		protected override void HashCore(ReadOnlySpan<byte> data) {
			if (blockHashes.Length < blockHashOffset + ((data.Length / BlockSize) + 2) * 16) {
				Array.Resize(ref blockHashes, blockHashes.Length * 2);
			}

			int offset = 0;
			while (data.Length != offset) {
				AddBlockHash(data.Slice(offset, BlockSize));
				offset += BlockSize;
			}
		}

		private void AddBlockHash(ReadOnlySpan<byte> data) {
			Md4Hash(data, ((Span<byte>)blockHashes).Slice(blockHashOffset));
			blockHashOffset += 16;
		}
		public void Md4Hash(ReadOnlySpan<byte> data, Span<byte> hash) {
			fixed (byte* dataPtr = data)
			fixed (byte* hashPtr = hash) {
				Md4NativeHashAlgorithm.MD4ComputeHash(dataPtr, data.Length, hashPtr);
			}
		}


		/// <summary>Calculates both ed2k hashes</summary>
		/// <returns>Always returns the red hash</returns>
		public override ReadOnlySpan<byte> TransformFinalBlock(ReadOnlySpan<byte> data) {
			BlueIsRed = false;
			RedHash = null;
			BlueHash = null;

			AddBlockHash(data);

			Span<byte> hashes = blockHashes;
			Span<byte> hashNoNull = new byte[16];
			Md4Hash(hashes.Slice(0, blockHashOffset), hashNoNull);

			//https://wiki.anidb.info/w/Ed2k-hash
			ReadOnlySpan<byte> hash;
			BlueIsRed = false;
			if (data.Length != 0) {
				//Data is not multiple of BlockLength (Common case)
				BlueIsRed = true;
				hash = hashNoNull;
				BlueHash = hash.ToArray();
				RedHash = BlueHash;

			} else {
				nullMd4Hash.CopyTo(hashes.Slice(blockHashOffset, 16));
				blockHashOffset += 16;

				Span<byte> hashWithNull = new byte[16];
				Md4Hash(hashes.Slice(0, blockHashOffset), hashWithNull);

				BlueHash = hashNoNull.ToArray();
				RedHash = hashWithNull.ToArray();
				hash = hashWithNull;
			}

			return hash;
		}

		public override void Initialize() {
		}
	}
}
