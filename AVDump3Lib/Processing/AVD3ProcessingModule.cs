using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.BlockConsumers.Matroska;
using AVDump3Lib.Processing.BlockConsumers.Ogg;
using AVDump3Lib.Processing.HashAlgorithms;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace AVDump3Lib.Processing {
	public interface IAVD3ProcessingModule : IAVD3Module {
        IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

    }
    public class AVD3ProcessingModule : IAVD3ProcessingModule {
        private List<IBlockConsumerFactory> blockConsumerFactories;

        public IReadOnlyCollection<IBlockConsumerFactory> BlockConsumerFactories { get; }

        public AVD3ProcessingModule() {
            blockConsumerFactories = new List<IBlockConsumerFactory> {
				new BlockConsumerFactory("NULL", r => new HashCalculator("NULL", r, new NullHashAlgorithm()) ),
				new BlockConsumerFactory("SHA1", r => new HashCalculator("SHA1", r, SHA1.Create()) ),
				new BlockConsumerFactory("SHA256", r => new HashCalculator("SHA256", r, SHA256.Create())),
                new BlockConsumerFactory("SHA384", r => new HashCalculator("SHA384", r, SHA384.Create())),
                new BlockConsumerFactory("SHA512", r => new HashCalculator("SHA512", r, SHA512.Create())),
                new BlockConsumerFactory("MD4", r => new HashCalculator("MD4", r, new Md4HashAlgorithm())),
                new BlockConsumerFactory("MD5", r => new HashCalculator("MD5", r, MD5.Create())),
                new BlockConsumerFactory("ED2K", r => new HashCalculator("ED2K", r, new Ed2kHashAlgorithm())),
                new BlockConsumerFactory("TIGER", r => new HashCalculator("TIGER", r, new TigerHashAlgorithm())),
                new BlockConsumerFactory("TTH", r => new HashCalculator("TTH", r, new TigerTreeHashAlgorithm(Environment.ProcessorCount)) ),
                new BlockConsumerFactory("CRC32", r => new HashCalculator("CRC32", r, new Crc32HashAlgorithm())),
                new BlockConsumerFactory("MKV", r => new MatroskaParser("MKV", r)),
                new BlockConsumerFactory("OGG", r => new OggParser("OGG", r))
           };

            BlockConsumerFactories = blockConsumerFactories.AsReadOnly();

        }

        public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {        }
        public void AfterConfiguration() { }
    }
}
