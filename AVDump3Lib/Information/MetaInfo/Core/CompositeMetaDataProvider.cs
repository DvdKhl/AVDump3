using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVDump3Lib.Information.MetaInfo.Core {
    public class CompositeMetaDataProvider : MetaDataProvider {
        public CompositeMetaDataProvider(string name, IEnumerable<MetaDataProvider> providers) : base(name) {
        }
    }
}
