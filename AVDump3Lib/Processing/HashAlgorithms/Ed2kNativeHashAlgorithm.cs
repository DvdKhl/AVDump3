using System;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public unsafe sealed class Ed2kNativeHashAlgorithm : AVDHashAlgorithm {
		private static class NativeMethods {
			[DllImport("AVDump3NativeLib")]
			public static extern void MD4ComputeHash(byte* b, int length, byte* hash);
		}


		public bool BlueIsRed { get; private set; }
		public ImmutableArray<byte> RedHash { get; private set; }
		public ImmutableArray<byte> BlueHash { get; private set; }

		public override int BlockSize => 9728000;

		private int blockHashOffset;
		private byte[] blockHashes = new byte[16 * 512]; //Good for ~4GB, increased if needed

		protected override void HashCore(in ReadOnlySpan<byte> data) {
			if(blockHashes.Length < blockHashOffset + (data.Length / BlockSize + 2) * 16) {
				Array.Resize(ref blockHashes, blockHashes.Length * 2);
			}

			var offset = 0;
			while(data.Length != offset) {
				AddBlockHash(data.Slice(offset, BlockSize));
				offset += BlockSize;
			}
		}

		private void AddBlockHash(in ReadOnlySpan<byte> data) {
			Md4Hash(data, ((Span<byte>)blockHashes).Slice(blockHashOffset));
			blockHashOffset += 16;
		}
		public static void Md4Hash(ReadOnlySpan<byte> data, Span<byte> hash) {
			fixed(byte* dataPtr = data)
			fixed(byte* hashPtr = hash) {
				NativeMethods.MD4ComputeHash(dataPtr, data.Length, hashPtr);
			}
		}


		/// <summary>Calculates both ed2k hashes</summary>
		/// <returns>Always returns the red hash</returns>
		public override ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) {
			BlueIsRed = false;

			AddBlockHash(data);

			Span<byte> hashes = blockHashes;
			Span<byte> hashWithNull = new byte[16];

			Md4Hash(hashes.Slice(0, blockHashOffset), hashWithNull);

			//https://wiki.anidb.info/w/Ed2k-hash
			BlueIsRed = false;
			ReadOnlySpan<byte> hash;
			if(!data.IsEmpty || data.IsEmpty && blockHashOffset == 0) {
				//Data is not multiple of BlockLength (Common case)
				BlueIsRed = true;
				hash = hashWithNull;
				BlueHash = hash.ToArray().ToImmutableArray();
				RedHash = BlueHash;

			} else {

				Span<byte> hashNoNull = new byte[16];
				if(blockHashOffset == 32) {
					hashNoNull = hashes.Slice(0, 16);
				} else {
					Md4Hash(hashes.Slice(0, blockHashOffset - 16), hashNoNull);
				}

				BlueHash = hashNoNull.ToArray().ToImmutableArray();
				RedHash = hashWithNull.ToArray().ToImmutableArray();
				hash = hashWithNull;
			}

			AdditionalHashes = ImmutableArray.Create(BlueHash);
			return hash;
		}

		public override void Initialize() {
			BlueIsRed = false;
			RedHash = ImmutableArray<byte>.Empty;
			BlueHash = ImmutableArray<byte>.Empty;
			Array.Clear(blockHashes, 0, blockHashes.Length);
			blockHashOffset = 0;
		}
	}
}
