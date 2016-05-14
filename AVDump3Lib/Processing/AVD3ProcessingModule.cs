using AVDump3Lib.BlockConsumers;
using AVDump3Lib.BlockConsumers.Matroska;
using AVDump3Lib.HashAlgorithms;
using AVDump3Lib.Modules;
using AVDump3Lib.Processing.BlockConsumers;
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
                new BlockConsumerFactory("SHA1", r => new HashCalculator(r, SHA1.Create()) ),
                new BlockConsumerFactory("SHA256", r => new HashCalculator(r, SHA256.Create())),
                new BlockConsumerFactory("SHA384", r => new HashCalculator(r, SHA384.Create())),
                new BlockConsumerFactory("SHA512", r => new HashCalculator(r, SHA512.Create())),
                new BlockConsumerFactory("MD4", r => new HashCalculator(r, new Md4())),
                new BlockConsumerFactory("MD5", r => new HashCalculator(r, MD5.Create())),
                new BlockConsumerFactory("ED2K", r => new HashCalculator(r, new Ed2k())),
                new BlockConsumerFactory("TIGER", r => new HashCalculator(r, new Tiger())),
                new BlockConsumerFactory("TTH", r => new HashCalculator(r, new TTH(Environment.ProcessorCount)) ),
                new BlockConsumerFactory("CRC32", r => new HashCalculator(r, new Crc32())),
                new BlockConsumerFactory("MKV", r => new MatroskaParser(r))
           };

            BlockConsumerFactories = blockConsumerFactories.AsReadOnly();

        }


        public void Initialize(IReadOnlyCollection<IAVD3Module> modules) {


        }
    }
}
