using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo.Core;

public class FileMetaInfo {
	public ReadOnlyCollection<MetaDataProvider> Providers { get; private set; }
	public ReadOnlyCollection<MetaDataProvider> CondensedProviders { get; private set; }

	public FileInfo FileInfo { get; private set; }

	public FileMetaInfo(FileInfo fileInfo, IEnumerable<MetaDataProvider> items) {
		FileInfo = fileInfo;
		Providers = Array.AsReadOnly(items.Where(i => i != null).ToArray());

		CondensedProviders = Array.AsReadOnly(Providers.GroupBy(x => x.Type).Select(x => x.Count() > 1 ? new CompositeMetaDataProvider(x.Key.Name, x) : x.First()).ToArray());
	}
}
