using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml.Data;

namespace XamlGenerationTests.Shared
{
	public class ObjectConverter : IValueConverter
	{
		public object TrueValue { get; set; }

		public object FalseValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
