using AVDump3Lib.BlockConsumers;
using AVDump3Lib.BlockConsumers.Matroska;
using AVDump3Lib.HashAlgorithms;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockConsumers;
using AVDump3Lib.Processing.HashAlgorithms;
using AVDump3Lib.Processing.StreamConsumer;
using AVDump3Lib.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
                new BlockConsumerFactory("MD4", r => new HashCalculator("MD4", r, new Md4())),
                new BlockConsumerFactory("MD5", r => new HashCalculator("MD5", r, MD5.Create())),
                new BlockConsumerFactory("ED2K", r => new HashCalculator("ED2K", r, new Ed2k())),
                new BlockConsumerFactory("TIGER", r => new HashCalculator("TIGER", r, new Tiger())),
                new BlockConsumerFactory("TTH", r => new HashCalculator("TTH", r, new TTH(Environment.ProcessorCount)) ),
                new BlockConsumerFactory("CRC32", r => new HashCalculator("CRC32", r, new Crc32())),
                new BlockConsumerFactory("MKV", r => new MatroskaParser("MKV", r))
           };

            BlockConsumerFactories = blockConsumerFactories.AsReadOnly();

        }


        public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {


        }
    }
}
