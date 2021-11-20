using Avalonia.Data;
using Avalonia.Data.Converters;
using System.Globalization;

namespace AVDump3Gui.Controls.Settings;

public class SettingValueConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {

		if(value is ISettingPropertyItem settingProperty) {
			return new SettingValueDisplay(settingProperty, (SettingValueDisplayType)parameter);

		} else {
			return BindingOperations.DoNothing;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
