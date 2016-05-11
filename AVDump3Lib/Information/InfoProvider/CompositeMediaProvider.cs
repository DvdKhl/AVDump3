using AVDump3Lib.Information.MetaInfo;
using AVDump3Lib.Information.MetaInfo.Media;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace AVDump2Lib.InfoProvider {
    public class CompositeMediaProvider : MediaProvider {
        public ReadOnlyCollection<MediaProvider> Providers { get; private set; }


        public CompositeMediaProvider(params MediaProvider[] providers)
            : base("CompositeMediaProvider") {

            Providers = Array.AsReadOnly(providers.Where(p => p != null).ToArray());
            foreach(var provider in Providers) CombineProvider(provider);
        }

        private void CombineProvider(MetaInfoContainer container) {
            foreach(var item in container.Items) {
                Add(item.Type, item.Value, item.Provider);
            }
        }

        private void AddItems(MetaInfoContainer containerA, MetaInfoContainer containerB, MetaInfoItemType type) {
            var itemsA = containerA.Select(type).ToArray();
            var itemsB = containerB.Select(type).ToArray();

            if(!itemsA.Any() && itemsB.Any()) foreach(var item in itemsB) Add(containerB, type, item.Value, item.Provider);
        }
    }
}
