using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AVDump3GUI.Converters;

[ValueConversion(typeof(bool), typeof(Visibility))]
public sealed class BoolToVisibilityHiddenConverter : IValueConverter {
    public Visibility TrueValue { get; set; }
    public Visibility FalseValue { get; set; }

    public BoolToVisibilityHiddenConverter() {
        // set defaults
        TrueValue = Visibility.Visible;
        FalseValue = Visibility.Hidden;
    }

    public object? Convert(object value, Type targetType,
        object parameter, CultureInfo culture) {
        if(!(value is bool))
            return null;
        return (bool)value ? TrueValue : FalseValue;
    }

    public object? ConvertBack(object value, Type targetType,
        object parameter, CultureInfo culture) {
        if(Equals(value, TrueValue))
            return true;
        if(Equals(value, FalseValue))
            return false;
        return null;
    }
}