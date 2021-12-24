using System;
using System.Linq;
using System.Windows.Data;
using System.Globalization;

namespace AVDump3GUI.Controls.Settings;

public class EnumToStringsValueConverter : IValueConverter {
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
		if(value.GetType().IsEnum) {
			return Enum.GetNames(value.GetType()).Select(x => Enum.Parse(value.GetType(), x));

		} else {
			return Binding.DoNothing;
		}
	}

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
}
