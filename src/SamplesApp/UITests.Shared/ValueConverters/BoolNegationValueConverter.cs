using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace UITests.ValueConverters
{
	public class BoolNegationValueConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language) =>
			NegateValue(value);

		public object ConvertBack(object value, Type targetType, object parameter, string language) =>
			NegateValue(value);

		private static object NegateValue(object value)
		{
			if (value is bool boolValue)
			{
				return !boolValue;
			}
			return false;
		}
	}
}
