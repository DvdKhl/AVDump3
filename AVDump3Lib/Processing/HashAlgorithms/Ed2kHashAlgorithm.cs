using System.Security.Cryptography;

namespace AVDump3Lib.Processing.HashAlgorithms {
    public class Ed2kHashAlgorithm : HashAlgorithm {
		public const int BLOCKSIZE = 9500 * 1024;

		public bool BlueIsRed { get { return blueIsRed; } } private bool blueIsRed;
		public byte[] RedHash { get { var hash = Hash; return redHash != null ? (byte[])redHash.Clone() : hash; } } private byte[] redHash;
		public byte[] BlueHash { get { var hash = Hash; return blueHash != null ? (byte[])blueHash.Clone() : hash; } } private byte[] blueHash;

		private byte[] nullArray = new byte[0];
		private byte[] nullMd4Hash, blockHash = new byte[16];

		private long processedBytes;
		private int missing = BLOCKSIZE;
		private Ed2kMd4 md4BlockHash;
		private Md4HashAlgorithm md4NodeHash;

		public Ed2kHashAlgorithm() {
			md4BlockHash = new Ed2kMd4();
			md4NodeHash = new Md4HashAlgorithm();

			nullMd4Hash = md4NodeHash.ComputeHash(nullArray);
			md4BlockHash.Initialize();
			md4NodeHash.Initialize();
		}

		public override bool CanReuseTransform { get { return true; } }

		protected override void HashCore(byte[] b, int offset, int length) {
			processedBytes += length;
			while(length != 0) {
				if(length < missing) {
					md4BlockHash.TransformBlock(b, offset, length);
					missing -= length;
					length = 0;

				} else {
					md4BlockHash.TransformBlock(b, offset, missing);
					md4BlockHash.GetHash(blockHash);

					md4NodeHash.TransformBlock(blockHash, 0, 16, null, 0);
					md4BlockHash.Initialize();

					length -= missing;
					offset += missing;
					missing = BLOCKSIZE;
				}
			}
		}


		/// <summary>Calculates both ed2k hashes</summary>
		/// <returns>Always returns the red hash</returns>
		protected override byte[] HashFinal() {
			blueIsRed = false;
			redHash = null;
			blueHash = null;

			if(processedBytes < BLOCKSIZE) {
				md4BlockHash.TransformBlock(nullArray, 0, 0);
				md4BlockHash.GetHash(blueHash = new byte[16]);

			} else if(processedBytes == BLOCKSIZE) {
				md4BlockHash.GetHash(blueHash = new byte[16]);

				md4BlockHash.TransformBlock(md4NodeHash.Hash, 0, 16);
				md4BlockHash.TransformBlock(nullMd4Hash, 0, 16);
				md4BlockHash.GetHash(redHash = new byte[16]);

			} else {
				if(missing != BLOCKSIZE) {
					var hash = new byte[16];
					md4BlockHash.GetHash(hash);
					md4NodeHash.TransformBlock(hash, 0, 16, null, 0);
				}
				md4BlockHash.Initialize();

				//foreach(var md4HashBlock in md4HashBlocks) md4BlockHash.TransformBlock(md4HashBlock, 0, 16, null, 0);
				var state = md4NodeHash.GetState();

				md4NodeHash.TransformFinalBlock(nullArray, 0, 0);
				blueHash = md4NodeHash.Hash;

				if(missing == BLOCKSIZE) {
					md4NodeHash.Initialize(state);
					md4NodeHash.TransformFinalBlock(nullMd4Hash, 0, 16);
					redHash = md4NodeHash.Hash;
				}
			}

			if(redHash == null) blueIsRed = true;
			return redHash == null ? blueHash : redHash;
		}

		public override void Initialize() {
			//Called when TransformFinalBlock is called in Mono (not in NET) !
			missing = BLOCKSIZE;
			md4BlockHash.Initialize();
			md4NodeHash.Initialize();
		}
	}

	public class Ed2kMd4 {
		public const int HASHLENGTH = 16;
		public const int BLOCKLENGTH = 64;
		private const uint A0 = 0x67452301U, B0 = 0xEFCDAB89U, C0 = 0x98BADCFEU, D0 = 0x10325476U;

		private uint A, B, C, D;
		private long hashedLength;
		private byte[] tail = new byte[72];
		private byte[] buffer;

		public Ed2kMd4() { buffer = new byte[BLOCKLENGTH]; Initialize(); }

		public void TransformBlock(byte[] array, int ibStart, int cbSize) {
			int n = (int)(hashedLength % BLOCKLENGTH);
			int partLen = BLOCKLENGTH - n;
			int i = 0;
			hashedLength += cbSize;

			unsafe
			{
				if(cbSize >= partLen) {
					System.Buffer.BlockCopy(array, ibStart, buffer, n, partLen);
					fixed (byte* b = buffer) TransformMd4Block((uint*)b);
					i = partLen;

					fixed (byte* b = array)
					{
						byte* data = b + ibStart;
						while(i + BLOCKLENGTH - 1 < cbSize) {
							TransformMd4Block((uint*)(data + i));
							i += BLOCKLENGTH;
						}
					}
					n = 0;
				}
			}
			if(i < cbSize) System.Buffer.BlockCopy(array, ibStart + i, buffer, n, cbSize - i);
		}

		protected unsafe void TransformMd4Block(uint* dataPtr) {
			uint aa, bb, cc, dd;

			aa = A;
			bb = B;
			cc = C;
			dd = D;


			aa += ((bb & cc) | ((~bb) & dd)) + *(dataPtr++);
			aa = aa << 3 | aa >> -3;
			dd += ((aa & bb) | ((~aa) & cc)) + *(dataPtr++);
			dd = dd << 7 | dd >> -7;
			cc += ((dd & aa) | ((~dd) & bb)) + *(dataPtr++);
			cc = cc << 11 | cc >> -11;
			bb += ((cc & dd) | ((~cc) & aa)) + *(dataPtr++);
			bb = bb << 19 | bb >> -19;
			aa += ((bb & cc) | ((~bb) & dd)) + *(dataPtr++);
			aa = aa << 3 | aa >> -3;
			dd += ((aa & bb) | ((~aa) & cc)) + *(dataPtr++);
			dd = dd << 7 | dd >> -7;
			cc += ((dd & aa) | ((~dd) & bb)) + *(dataPtr++);
			cc = cc << 11 | cc >> -11;
			bb += ((cc & dd) | ((~cc) & aa)) + *(dataPtr++);
			bb = bb << 19 | bb >> -19;
			aa += ((bb & cc) | ((~bb) & dd)) + *(dataPtr++);
			aa = aa << 3 | aa >> -3;
			dd += ((aa & bb) | ((~aa) & cc)) + *(dataPtr++);
			dd = dd << 7 | dd >> -7;
			cc += ((dd & aa) | ((~dd) & bb)) + *(dataPtr++);
			cc = cc << 11 | cc >> -11;
			bb += ((cc & dd) | ((~cc) & aa)) + *(dataPtr++);
			bb = bb << 19 | bb >> -19;
			aa += ((bb & cc) | ((~bb) & dd)) + *(dataPtr++);
			aa = aa << 3 | aa >> -3;
			dd += ((aa & bb) | ((~aa) & cc)) + *(dataPtr++);
			dd = dd << 7 | dd >> -7;
			cc += ((dd & aa) | ((~dd) & bb)) + *(dataPtr++);
			cc = cc << 11 | cc >> -11;
			bb += ((cc & dd) | ((~cc) & aa)) + *(dataPtr++);
			bb = bb << 19 | bb >> -19;
			dataPtr -= 16;

			aa += ((bb & (cc | dd)) | (cc & dd)) + *(dataPtr + 0x0) + 0x5A827999U;
			aa = aa << 3 | aa >> -3;
			dd += ((aa & (bb | cc)) | (bb & cc)) + *(dataPtr + 0x4) + 0x5A827999U;
			dd = dd << 5 | dd >> -5;
			cc += ((dd & (aa | bb)) | (aa & bb)) + *(dataPtr + 0x8) + 0x5A827999U;
			cc = cc << 9 | cc >> -9;
			bb += ((cc & (dd | aa)) | (dd & aa)) + *(dataPtr + 0xC) + 0x5A827999U;
			bb = bb << 13 | bb >> -13;
			aa += ((bb & (cc | dd)) | (cc & dd)) + *(dataPtr + 0x1) + 0x5A827999U;
			aa = aa << 3 | aa >> -3;
			dd += ((aa & (bb | cc)) | (bb & cc)) + *(dataPtr + 0x5) + 0x5A827999U;
			dd = dd << 5 | dd >> -5;
			cc += ((dd & (aa | bb)) | (aa & bb)) + *(dataPtr + 0x9) + 0x5A827999U;
			cc = cc << 9 | cc >> -9;
			bb += ((cc & (dd | aa)) | (dd & aa)) + *(dataPtr + 0xD) + 0x5A827999U;
			bb = bb << 13 | bb >> -13;
			aa += ((bb & (cc | dd)) | (cc & dd)) + *(dataPtr + 0x2) + 0x5A827999U;
			aa = aa << 3 | aa >> -3;
			dd += ((aa & (bb | cc)) | (bb & cc)) + *(dataPtr + 0x6) + 0x5A827999U;
			dd = dd << 5 | dd >> -5;
			cc += ((dd & (aa | bb)) | (aa & bb)) + *(dataPtr + 0xA) + 0x5A827999U;
			cc = cc << 9 | cc >> -9;
			bb += ((cc & (dd | aa)) | (dd & aa)) + *(dataPtr + 0xE) + 0x5A827999U;
			bb = bb << 13 | bb >> -13;
			aa += ((bb & (cc | dd)) | (cc & dd)) + *(dataPtr + 0x3) + 0x5A827999U;
			aa = aa << 3 | aa >> -3;
			dd += ((aa & (bb | cc)) | (bb & cc)) + *(dataPtr + 0x7) + 0x5A827999U;
			dd = dd << 5 | dd >> -5;
			cc += ((dd & (aa | bb)) | (aa & bb)) + *(dataPtr + 0xB) + 0x5A827999U;
			cc = cc << 9 | cc >> -9;
			bb += ((cc & (dd | aa)) | (dd & aa)) + *(dataPtr + 0xF) + 0x5A827999U;
			bb = bb << 13 | bb >> -13;

			aa += (bb ^ cc ^ dd) + *(dataPtr + 0x0) + 0x6ED9EBA1U;
			aa = aa << 3 | aa >> -3;
			dd += (aa ^ bb ^ cc) + *(dataPtr + 0x8) + 0x6ED9EBA1U;
			dd = dd << 9 | dd >> -9;
			cc += (dd ^ aa ^ bb) + *(dataPtr + 0x4) + 0x6ED9EBA1U;
			cc = cc << 11 | cc >> -11;
			bb += (cc ^ dd ^ aa) + *(dataPtr + 0xC) + 0x6ED9EBA1U;
			bb = bb << 15 | bb >> -15;
			aa += (bb ^ cc ^ dd) + *(dataPtr + 0x2) + 0x6ED9EBA1U;
			aa = aa << 3 | aa >> -3;
			dd += (aa ^ bb ^ cc) + *(dataPtr + 0xA) + 0x6ED9EBA1U;
			dd = dd << 9 | dd >> -9;
			cc += (dd ^ aa ^ bb) + *(dataPtr + 0x6) + 0x6ED9EBA1U;
			cc = cc << 11 | cc >> -11;
			bb += (cc ^ dd ^ aa) + *(dataPtr + 0xE) + 0x6ED9EBA1U;
			bb = bb << 15 | bb >> -15;
			aa += (bb ^ cc ^ dd) + *(dataPtr + 0x1) + 0x6ED9EBA1U;
			aa = aa << 3 | aa >> -3;
			dd += (aa ^ bb ^ cc) + *(dataPtr + 0x9) + 0x6ED9EBA1U;
			dd = dd << 9 | dd >> -9;
			cc += (dd ^ aa ^ bb) + *(dataPtr + 0x5) + 0x6ED9EBA1U;
			cc = cc << 11 | cc >> -11;
			bb += (cc ^ dd ^ aa) + *(dataPtr + 0xD) + 0x6ED9EBA1U;
			bb = bb << 15 | bb >> -15;
			aa += (bb ^ cc ^ dd) + *(dataPtr + 0x3) + 0x6ED9EBA1U;
			aa = aa << 3 | aa >> -3;
			dd += (aa ^ bb ^ cc) + *(dataPtr + 0xB) + 0x6ED9EBA1U;
			dd = dd << 9 | dd >> -9;
			cc += (dd ^ aa ^ bb) + *(dataPtr + 0x7) + 0x6ED9EBA1U;
			cc = cc << 11 | cc >> -11;
			bb += (cc ^ dd ^ aa) + *(dataPtr + 0xF) + 0x6ED9EBA1U;
			bb = bb << 15 | bb >> -15;

			A += aa;
			B += bb;
			C += cc;
			D += dd;
		}

		public void GetHash(byte[] hash) {
			var length = PadBuffer();
			TransformBlock(tail, 0, length);

			hash[00] = (byte)(A); hash[01] = (byte)(A >> 8); hash[02] = (byte)(A >> 16); hash[03] = (byte)(A >> 24);
			hash[04] = (byte)(B); hash[05] = (byte)(B >> 8); hash[06] = (byte)(B >> 16); hash[07] = (byte)(B >> 24);
			hash[08] = (byte)(C); hash[09] = (byte)(C >> 8); hash[10] = (byte)(C >> 16); hash[11] = (byte)(C >> 24);
			hash[12] = (byte)(D); hash[13] = (byte)(D >> 8); hash[14] = (byte)(D >> 16); hash[15] = (byte)(D >> 24);
		}

		public void Initialize() {
			A = A0;
			B = B0;
			C = C0;
			D = D0;
			hashedLength = 0;
		}
		public void Initialize(InternalState state) {
			hashedLength = state.hashedLength;
			A = state.A;
			B = state.B;
			C = state.C;
			D = state.D;
			buffer = state.Buffer;
		}


		protected int PadBuffer() {
			int padding;
			int n = (int)(hashedLength % BLOCKLENGTH);
			if(n < 56) padding = 56 - n; else padding = 120 - n;
			long bits = hashedLength << 3;

			//byte[] pad = new byte[padding + 8];
			tail[0] = 0x80;
			tail[padding] = (byte)(bits & 0xFF);
			tail[padding + 1] = (byte)(bits >> 8 & 0xFF);
			tail[padding + 2] = (byte)(bits >> 16 & 0xFF);
			tail[padding + 3] = (byte)(bits >> 24 & 0xFF);
			tail[padding + 4] = (byte)(bits >> 32 & 0xFF);
			tail[padding + 5] = (byte)(bits >> 40 & 0xFF);
			tail[padding + 6] = (byte)(bits >> 48 & 0xFF);
			tail[padding + 7] = (byte)(bits >> 56 & 0xFF);
			return padding + 8;
		}

		public struct InternalState {
			public uint A, B, C, D;
			public long hashedLength;
			public byte[] Buffer;

			public InternalState(long hashedLength, uint A, uint B, uint C, uint D, byte[] Buffer) {
				this.hashedLength = hashedLength;
				this.A = A;
				this.B = B;
				this.C = C;
				this.D = D;
				this.Buffer = (byte[])Buffer.Clone();
			}
		}
		public InternalState GetState() { return new InternalState(hashedLength, A, B, C, D, buffer); }
	}
}
