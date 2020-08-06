using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace AVDump3Gui.Controls.Settings {
	public class EnumToStringsValueConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			if(value.GetType().IsEnum) {
				return Enum.GetNames(value.GetType()).Select(x => Enum.Parse(value.GetType(), x));

			} else {
				return BindingOperations.DoNothing;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();
	}



}
