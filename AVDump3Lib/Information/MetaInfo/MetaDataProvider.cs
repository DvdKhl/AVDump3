namespace AVDump3Lib.Information.MetaInfo {
    public abstract class MetaDataProvider : MetaInfoContainer {
		public MetaDataProvider(string name) : base(null) { Name = name; }

		protected void Add(MetaInfoItemType type, object value, params string[][] notes) { Add(type, value, this, notes); }
		protected new void Add(MetaInfoItemType type, object value, MetaDataProvider provider, params string[][] notes) { base.Add(type, value, provider, notes); }
		//protected void Add(MetaInfoItemType type, Func<object> getValue, params string[][] notes) { Add(type, getValue, this, notes); }
		protected void Add(MetaInfoContainer container, MetaInfoItemType type, object value, params string[][] notes) { container.Add(type, value, this, notes); }
		protected void Add(MetaInfoContainer container, MetaInfoItemType type, object value, MetaDataProvider provider, params string[][] notes) { container.Add(type, value, provider, notes); }
		//protected void Add(MetaInfoContainer container, MetaInfoItemType type, Func<object> getValue, params string[][] notes) { container.Add(type, getValue, this, notes); }

		public string Name { get; private set; }
	}
}
