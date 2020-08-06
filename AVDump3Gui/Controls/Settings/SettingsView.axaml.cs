using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Resources;

namespace AVDump3Gui.Controls.Settings {

	public interface ISettingPropertyItem {
		static object Unset { get; } = new object();

		ImmutableArray<string> AlternativeNames { get; }
		object DefaultValue { get; }
		string Description { get; }
		string Example { get; }
		string Name { get; }
		Type ValueType { get; }
		//ISettingGroupItem Parent { get; }
		object? ToObject(string? stringValue);
		string? ToString(object? objectValue);

		object ValueRaw { get; set; }

		bool IsUnset => ValueRaw == Unset;

		string ValueTypeKey { get; }
		object Value { get; set; }
	}
	public interface ISettingGroupItem {
		string Description { get; }
		string Name { get; }
		ImmutableArray<ISettingPropertyItem> Properties { get; }
		ResourceManager ResourceManager { get; }
	}


	public class SettingsView : UserControl {
		private HeaderedItemsControl SettingsGroupsControl { get; }


		public SettingsView() {
			InitializeComponent();

			SettingsGroupsControl = this.Find<HeaderedItemsControl>("SettingsGroupsControl");
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}

		public IDataTemplate SettingValueTemplate { get => GetValue(SettingValueTemplateProperty); set => SetValue(SettingValueTemplateProperty, value); }
		public static readonly StyledProperty<IDataTemplate> SettingValueTemplateProperty = AvaloniaProperty.Register<SettingsView, IDataTemplate>(nameof(SettingValueTemplate));

		public IEnumerable<ISettingGroupItem> SettingGroups { get => GetValue(SettingGroupsProperty); set => SetValue(SettingGroupsProperty, value); }
		public static readonly StyledProperty<IEnumerable<ISettingGroupItem>> SettingGroupsProperty = AvaloniaProperty.Register<SettingsView, IEnumerable<ISettingGroupItem>>(nameof(SettingGroups), notifying: SettingsSourceChange);

		private static void SettingsSourceChange(IAvaloniaObject avaloniaObject, bool isChanging) {
			var view = avaloniaObject as SettingsView;
			if(!isChanging) view.SettingsGroupsControl.Items = view.SettingGroups;

		//	if(!isChanging) {
		//		foreach(var prop in view.SettingsSource.Groups.SelectMany(x => x.Properties)) {
		//			if(!view.SettingsSource.Values.Any(x => x.Property == prop)) {
		//				view.SettingsSource.Values.Add(view.SettingsSource.CreateValue(prop));
		//			}
		//		}
		//	}
		}

	}
}
