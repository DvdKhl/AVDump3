using AVDump3Lib.BlockConsumers;
using CSEBML.DataSource;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AVDump3Lib.BlockBuffers;

namespace AVDump2Lib.BlockConsumers.Ogg {
    public class OggParser : BlockConsumer {
        private OggFile result;

        public OggParser(IBlockStreamReader reader) : base(reader) {
        }

        //public OggParser(string name) : base(name) { }



        private IEnumerable<byte[]> Source(IEnumerable<byte[]> blocks) {
            foreach(var block in blocks) {
                yield return block;
                //ProcessedBlocks++;
            }
        }
        protected override void DoWork(CancellationToken ct) {
            var oggFile = new OggFile();
            //oggFile.Parse(dataSrc);

            result = oggFile;
        }

        public static bool IsOggFile(string filePath) {
            if(!File.Exists(filePath)) return false;
            using(var fileStream = File.OpenRead(filePath)) return IsOggFile(fileStream);
        }
        public static bool IsOggFile(Stream fileStream) {
            if(fileStream.ReadByte() == 'O' && fileStream.ReadByte() == 'g' && fileStream.ReadByte() == 'g' && fileStream.ReadByte() == 'S') {
                fileStream.Position = 0;
            } else return false;


            var dataSrc = new EBMLStreamDataSource(fileStream);

            try {
                var page = Page.Read(dataSrc);
                page.Skip();

                long offset = 0;
                var syncBytes = dataSrc.GetData(4, out offset);

                return syncBytes[offset + 0] == 'O' && syncBytes[offset + 1] == 'g' && syncBytes[offset + 2] == 'g' && syncBytes[offset + 3] == 'S';

            } catch(Exception) { return false; }
        }

    }
}
