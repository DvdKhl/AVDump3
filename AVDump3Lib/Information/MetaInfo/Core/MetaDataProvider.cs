using System.Linq;

namespace AVDump3Lib.Information.MetaInfo {


    public abstract class MetaDataProvider : MetaInfoContainer {
		public MetaDataProvider(string name) : base(0, null) { Name = name; }

        public void Add<T>(MetaInfoItemType<T> type, T value, params string[][] notes) {
            Add(new MetaInfoItem<T>(type, value, this, notes.ToDictionary(x => x[0], x => x[1])));
        }
        public void Add<T>(MetaInfoItemType<T> type, T? value, params string[][] notes) where T : struct {
            if(value.HasValue) {
                Add(new MetaInfoItem<T>(type, value.Value, this, notes.ToDictionary(x => x[0], x => x[1])));
            }
        }



        public void Add<T>(MetaInfoContainer container, MetaInfoItemType<T> type, T value, params string[][] notes) {
            container.Add(new MetaInfoItem<T>(type, value, this, notes.ToDictionary(x => x[0], x => x[1])));
        }
        public void Add<T>(MetaInfoContainer container, MetaInfoItemType<T> type, T? value, params string[][] notes) where T : struct {
            if(value.HasValue) {
                container.Add(new MetaInfoItem<T>(type, value.Value, this, notes.ToDictionary(x => x[0], x => x[1])));
            }
        }






        //protected new void Add(MetaInfoItemType type, object value, MetaDataProvider provider, params string[][] notes) { base.Add(type, value, provider, notes); }
        //protected void Add(MetaInfoItemType type, Func<object> getValue, params string[][] notes) { Add(type, getValue, this, notes); }
        //protected void Add(MetaInfoContainer container, MetaInfoItemType type, object value, params string[][] notes) { container.Add(type, value, this, notes); }
        //protected void Add(MetaInfoContainer container, MetaInfoItemType type, object value, MetaDataProvider provider, params string[][] notes) { container.Add(type, value, provider, notes); }
        //protected void Add(MetaInfoContainer container, MetaInfoItemType type, Func<object> getValue, params string[][] notes) { container.Add(type, getValue, this, notes); }

        public string Name { get; private set; }
	}

}
