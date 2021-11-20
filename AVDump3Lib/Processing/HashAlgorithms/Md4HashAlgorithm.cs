//// MD4Managed.cs - Message Digest 4 Managed Implementation
//
// Author:
//	Sebastien Pouliot (sebastien@ximian.com)
//
// (C) 2003 Motus Technologies Inc. (http://www.motus.com)
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

//Modified by DvdKhl

using System;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public sealed class Md4HashAlgorithm : AVDHashAlgorithm {
		public const int HASHLENGTH = 16;
		public const int BLOCKLENGTH = 64;
		private const uint A0 = 0x67452301U, B0 = 0xEFCDAB89U, C0 = 0x98BADCFEU, D0 = 0x10325476U;

		private uint A, B, C, D;
		public long BytesProcessed { get; private set; }

		public override int BlockSize => 64;

		public Md4HashAlgorithm() { Initialize(); }


		private unsafe void TransformMd4Block(uint* dataPtr) {
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

		protected override unsafe void HashCore(in ReadOnlySpan<byte> data) {
			fixed(byte* pData = data) {
				for(var offset = 0; offset < data.Length; offset += BLOCKLENGTH) {
					TransformMd4Block((uint*)(pData + offset));
				}
			}
			BytesProcessed += data.Length;
		}


		private int PadBuffer(Span<byte> tail) {
			int padding;
			var n = (int)(BytesProcessed % BLOCKLENGTH);
			if(n < 56) padding = 56; else padding = 120;
			var bits = BytesProcessed << 3;

			tail.Slice(n + 1, padding - n - 1).Clear();

			tail[n] = 0x80;
			tail[padding] = (byte)(bits & 0xFF);
			tail[padding + 1] = (byte)(bits >> 8 & 0xFF);
			tail[padding + 2] = (byte)(bits >> 16 & 0xFF);
			tail[padding + 3] = (byte)(bits >> 24 & 0xFF);
			tail[padding + 4] = (byte)(bits >> 32 & 0xFF);
			tail[padding + 5] = (byte)(bits >> 40 & 0xFF);
			tail[padding + 6] = (byte)(bits >> 48 & 0xFF);
			tail[padding + 7] = (byte)(bits >> 56 & 0xFF);
			return padding;
		}

		public void ComputeHash(in ReadOnlySpan<byte> data, in Span<byte> hash) {
			Span<byte> tail = stackalloc byte[128];

			var toProcess = data[..((data.Length / BlockSize) * BlockSize)];
			if(toProcess.Length > 0) HashCore(toProcess);

			data[toProcess.Length..].TryCopyTo(tail);
			BytesProcessed += data.Length - toProcess.Length;

			var padding = PadBuffer(tail);
			HashCore(tail[..(padding + 8)]);

			hash[00] = (byte)(A); hash[01] = (byte)(A >> 8); hash[02] = (byte)(A >> 16); hash[03] = (byte)(A >> 24);
			hash[04] = (byte)(B); hash[05] = (byte)(B >> 8); hash[06] = (byte)(B >> 16); hash[07] = (byte)(B >> 24);
			hash[08] = (byte)(C); hash[09] = (byte)(C >> 8); hash[10] = (byte)(C >> 16); hash[11] = (byte)(C >> 24);
			hash[12] = (byte)(D); hash[13] = (byte)(D >> 8); hash[14] = (byte)(D >> 16); hash[15] = (byte)(D >> 24);

			Initialize();
		}

		public override ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) {
			Span<byte> hash = new byte[16];
			ComputeHash(data, hash);
			return hash;
		}



		protected override void InitializeInternal() {
			A = A0;
			B = B0;
			C = C0;
			D = D0;
			BytesProcessed = 0;
		}
	}

}
