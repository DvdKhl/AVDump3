namespace AVDump3Lib.Processing.HashAlgorithms {
	using System;

	//Source: http://damieng.com/blog/2006/08/08/Calculating_CRC32_in_C_and_NET
	public sealed class Crc32HashAlgorithm : AVDHashAlgorithm {
		public const uint DefaultPolynomial = 0xedb88320;
		public const uint DefaultSeed = 0xffffffff;

		private uint hash;
		private readonly uint seed;
		private readonly uint[] table;
		private static uint[] defaultTable;

		public override int BlockSize => 1;

		public Crc32HashAlgorithm() {
			table = InitializeTable(DefaultPolynomial);
			seed = DefaultSeed;
			Initialize();
		}

		public Crc32HashAlgorithm(uint polynomial, uint seed) {
			table = InitializeTable(polynomial);
			this.seed = seed;
			Initialize();
		}

		public override void Initialize() => hash = seed;

		protected override void HashCore(in ReadOnlySpan<byte> data) {
			hash = CalculateHash(table, hash, data);
		}
		public override ReadOnlySpan<byte> TransformFinalBlock(in ReadOnlySpan<byte> data) {
			hash = CalculateHash(table, hash, data);
			return UInt32ToBigEndianBytes(~hash);
		}


		private static uint[] InitializeTable(uint polynomial) {
			if(polynomial == DefaultPolynomial && defaultTable != null) return defaultTable;

			var createTable = new uint[256];
			for(var i = 0; i < 256; i++) {
				var entry = (uint)i;
				for(var j = 0; j < 8; j++) {
					if((entry & 1) == 1) entry = (entry >> 1) ^ polynomial;
					else entry = entry >> 1;
				}
				createTable[i] = entry;
			}

			if(polynomial == DefaultPolynomial) defaultTable = createTable;
			return createTable;
		}

		private static uint CalculateHash(uint[] table, uint seed, ReadOnlySpan<byte> data) {
			var crc = seed;
			for(var i = 0; i < data.Length; i++) unchecked { crc = (crc >> 8) ^ table[data[i] ^ crc & 0xff]; }
			return crc;
		}

		private byte[] UInt32ToBigEndianBytes(uint x) {
			return new byte[] { (byte)((x >> 24) & 0xff), (byte)((x >> 16) & 0xff), (byte)((x >> 8) & 0xff), (byte)(x & 0xff) };
		}
	}
}
