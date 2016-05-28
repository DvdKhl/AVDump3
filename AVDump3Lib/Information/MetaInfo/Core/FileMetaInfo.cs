using AVDump3Lib.Information.MetaInfo.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo {
    public class FileMetaInfo {
		public IEnumerable<MetaDataProvider> Providers { get; private set; }
		public IEnumerable<MetaDataProvider> CondensedProviders { get; private set; }

		public FileInfo FileInfo { get; private set; }

		public FileMetaInfo(FileInfo fileInfo, IEnumerable<MetaDataProvider> items) {
			FileInfo = fileInfo;
			Providers = Array.AsReadOnly(items.Where(i => i != null).ToArray());

			CondensedProviders = Providers.GroupBy(x => x.GetType()).Select(x => x.Count() > 1 ? new CompositeMetaDataProvider(x.Key.Name, x) : x.First());
		}
	}
}
