using System;
using System.Globalization;
using System.Windows.Data;

namespace HellcardSaveManager
{
    public sealed class ExitCodeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (!(value is int))
                return "";

            var i = (int)value;

            return i == int.MaxValue ? "None" : $"{i}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
