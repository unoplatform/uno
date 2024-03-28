using System;
using System.Globalization;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UITests.Microsoft_UI_Xaml_Controls.ProgressBar
{
	internal class DoubleToThicknessConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is Thickness thickness)
			{
				return thickness;
			}

			var doubleValue = System.Convert.ToDouble(value, NumberFormatInfo.InvariantInfo);
			return new Thickness(doubleValue);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotSupportedException();
	}
}
