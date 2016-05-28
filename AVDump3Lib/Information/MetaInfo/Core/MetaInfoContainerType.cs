using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AVDump3Lib.Information.MetaInfo.Core {
	public class MetaInfoContainerType {
		public string Name { get; }

		public MetaInfoContainerType(string name) {
			Name = name;
		}
	}
}
