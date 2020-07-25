using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AVDump3Gui.Controls.Settings {
	public class SettingsView : UserControl {
		public SettingsView() {
			this.InitializeComponent();
		}

		private void InitializeComponent() {
			AvaloniaXamlLoader.Load(this);
		}
	}
}
