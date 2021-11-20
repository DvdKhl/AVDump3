using System;
using System.Collections.Generic;
using System.Linq;

namespace AVDump3Lib.Information.MetaInfo.Core {
	public class CompositeMetaDataProvider : MetaDataProvider {
		public CompositeMetaDataProvider(string name, IEnumerable<MetaDataProvider> providers) : base(name, providers.First().Type) {
			ChooseFrom(providers, this);
		}

		private static void ChooseFrom(IEnumerable<MetaInfoContainer> sources, MetaInfoContainer dest) {
			var type = sources.First().Type;
			var id = sources.First().Id;

			if(sources.Any(x => x.Type != type || x.Id != id)) {
				throw new InvalidOperationException();
			}

			foreach(var container in sources) {
				foreach(var item in container.Items) {
					dest.Add(item);
				}
			}

			foreach(var group in from source in sources
								 from container in source.Nodes
								 group container by new { container.Id, container.Type }) {

				var subContainer = new MetaInfoContainer(group.Key.Id, group.Key.Type);
				ChooseFrom(group, subContainer);

				dest.AddNode(subContainer);
			}
		}
	}
}
