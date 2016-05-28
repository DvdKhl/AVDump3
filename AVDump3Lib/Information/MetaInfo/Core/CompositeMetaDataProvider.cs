using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Information.MetaInfo.Core {
    public class CompositeMetaDataProvider : MetaDataProvider {
        public CompositeMetaDataProvider(string name, IEnumerable<MetaDataProvider> providers) : base(name) {
			CreateFrom(providers);
		}

        private static MetaInfoContainer CreateFrom(IEnumerable<MetaInfoContainer> sources) {
            var type = sources.First().Type;
            var id = sources.First().Id;

            if(sources.Any(x => x.Type != type || x.Id != id)) {
                throw new InvalidOperationException();
            }

			var destContainer = new MetaInfoContainer(id, type);
            foreach(var container in sources) {
                foreach(var item in container.Items) {
					destContainer.Add(item);
                }
            }

			foreach(var group in from source in sources
								 from container in source.Nodes
								 group container by new { container.Id, container.Type }) {

				destContainer.AddNode(CreateFrom(group));
			}

			return destContainer;
        }
    }
}
