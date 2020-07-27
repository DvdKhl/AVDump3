using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Immutable;

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
		ImmutableArray<ISettingsPropertyItem> Properties { get; set; }
	}

	public interface ISettings {
	}


	public class SettingsView : UserControl {
		public SettingsView() {
			this.InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}



		public static readonly StyledProperty<ISettings> SettingsProperty = AvaloniaProperty.Register<SettingsView, ISettings>(nameof(Settings));

		public ISettings Settings {
			get { return GetValue(SettingsProperty); }
			set { SetValue(SettingsProperty, value); }
		}


	}
}
