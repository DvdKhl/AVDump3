using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Processing.HashAlgorithms {
	public sealed class NullHashAlgorithm : HashAlgorithm {
		public override void Initialize() { }
		protected override void HashCore(byte[] array, int ibStart, int cbSize) { }
		protected override byte[] HashFinal() { return new byte[1]; }
	}
}
