using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Information.MetaInfo.Core {
    public class CompositeMetaDataProvider : MetaDataProvider {
        public CompositeMetaDataProvider(string name, IEnumerable<MetaDataProvider> providers) : base(name) {
            
        }

        public MetaInfoContainer CreateFrom(IEnumerable<MetaInfoContainer> sources) {
            var type = sources.First().Type;
            var id = sources.First().Id;

            if(sources.Any(x => x.Type != type || x.Id != id)) {
                throw new InvalidOperationException();
            }

            foreach(var container in sources) {
                foreach(var item in container.Items) {

                }
            }
        }
    }
}
