using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public sealed class NullHashAlgorithm : AVDHashAlgorithm {
		public override int BlockSize { get; }

		public override void Initialize() { }
		public override ReadOnlySpan<byte> TransformFinalBlock(ReadOnlySpan<byte> data) => ReadOnlySpan<byte>.Empty;
		protected override unsafe void HashCore(ReadOnlySpan<byte> data) { }

		public NullHashAlgorithm(int blockSize) => BlockSize = blockSize;
	}
}
