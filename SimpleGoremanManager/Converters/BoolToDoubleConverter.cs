using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SimpleGoremanManager.Converters
{
    public class BoolToDoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is bool))
                return DependencyProperty.UnsetValue;
            if (parameter == null || !(parameter is string))
                return DependencyProperty.UnsetValue;

            var valueSplitted = ((string)parameter).Split('|');
            if (valueSplitted.Length != 2)
                return DependencyProperty.UnsetValue;

            double trueValue;
            double falseValue;
            if (!double.TryParse(valueSplitted[0], out trueValue))
                return DependencyProperty.UnsetValue;
            if (!double.TryParse(valueSplitted[1], out falseValue))
                return DependencyProperty.UnsetValue;

            return (bool)value ? trueValue : falseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
