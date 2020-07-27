using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace AVDump3Gui.Controls.Settings {

	public interface ISettingsPropertyItem {
		ImmutableArray<string> AlternativeNames { get; }
		object DefaultValue { get; }
		string Description { get; }
		string Example { get; }
		string Name { get; }
		Type ValueType { get; }
	}
	public interface ISettingsGroupItem {
		string Description { get; }
		string Name { get; }
		ImmutableArray<ISettingsPropertyItem> Properties { get; }

		string? PropertyObjectToString(ISettingsPropertyItem prop, object? objectValue);
		object? PropertyStringToObject(ISettingsPropertyItem prop, string? stringValue);
	}

	public interface ISettingsValueItem {
		ISettingsPropertyItem Property { get; }
		object Value { get; set; }
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


		public IEnumerable<ISettingsGroupItem> SettingsGroups { get => GetValue(SettingsGroupsProperty); set => SetValue(SettingsGroupsProperty, value); }
		public static readonly StyledProperty<IEnumerable<ISettingsGroupItem>> SettingsGroupsProperty = AvaloniaProperty.Register<SettingsView, IEnumerable<ISettingsGroupItem>>(nameof(Settings), notifying: SettingsGroupChange);

		private static void SettingsGroupChange(IAvaloniaObject avaloniaObject, bool isChanging) {
			var view = avaloniaObject as SettingsView;

			if(!isChanging) {
				var settingsGroupsInternal = view.SettingsGroups.Select(x => new SettingsGroupInternal(x)).ToArray();
				view.SettingsGroupsControl.Items = settingsGroupsInternal;
				view.SettingsValues = settingsGroupsInternal.SelectMany(x => x.Properties).ToArray();
			}
		}

		private IEnumerable<ISettingsValueItem> settingsValues = Array.Empty<ISettingsValueItem>();
		public IEnumerable<ISettingsValueItem> SettingsValues { get => settingsValues; private set => SetAndRaise(SettingsValuesProperty, ref settingsValues, value); }
		public static readonly DirectProperty<SettingsView, IEnumerable<ISettingsValueItem>> SettingsValuesProperty = AvaloniaProperty.RegisterDirect<SettingsView, IEnumerable<ISettingsValueItem>>(nameof(SettingsValues), o => o.SettingsValues, unsetValue: Array.Empty<ISettingsValueItem>());



		public class SettingsPropertyValueInternal : ReactiveObject, ISettingsValueItem {
			private SettingsGroupInternal settingsGroup;
			private object propValue;

			public SettingsPropertyValueInternal(SettingsGroupInternal settingsGroup,  ISettingsPropertyItem settingsProperty) { this.settingsGroup = settingsGroup; Property = settingsProperty; }

			public ISettingsPropertyItem Property { get; }
			public object Value { get => propValue; set => this.RaiseAndSetIfChanged(ref propValue, value); }

			public string ValueAsString { get => settingsGroup.PropertyObjectToString(Property, Value); set => Value = settingsGroup.PropertyStringToObject(Property, value); }
			//public object DefaultValue => settingsGroup.PropertyObjectToString(Base, ((ISettingsProperty)Base).DefaultValue);
		}


		public class SettingsGroupInternal : ISettingsGroupItem {
			private readonly ISettingsGroupItem settingsGroup;

			public SettingsGroupInternal(ISettingsGroupItem settingsGroup) {
				this.settingsGroup = settingsGroup;
				Properties = settingsGroup.Properties.Select(x => new SettingsPropertyValueInternal(this, x)).ToImmutableArray();
			}

			public string Description => settingsGroup.Description;
			public string Name => settingsGroup.Name;

			public ImmutableArray<SettingsPropertyValueInternal> Properties { get; }
			ImmutableArray<ISettingsPropertyItem> ISettingsGroupItem.Properties => settingsGroup.Properties;

			public string PropertyObjectToString(ISettingsPropertyItem prop, object objectValue) => settingsGroup.PropertyObjectToString(prop, objectValue);
			public object PropertyStringToObject(ISettingsPropertyItem prop, string stringValue) => settingsGroup.PropertyStringToObject(prop, stringValue);
		}

	}
}
