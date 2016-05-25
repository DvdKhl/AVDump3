using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo {
    public class FileMetaInfo {
		public IEnumerable<MetaDataProvider> Items { get; private set; }

		public long Size { get; private set; }

		public FileMetaInfo(long size, IEnumerable<MetaDataProvider> items) {
			Size = size;
			Items = Array.AsReadOnly(items.Where(i => i != null).ToArray());
		}
	}
}
