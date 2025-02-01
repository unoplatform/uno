using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Samples.Converters
{
	public class StringEmptyToVisibilityConverter : IValueConverter
	{
		public StringEmptyToVisibilityConverter()
		{
		}

		public Visibility ValueIfEmpty { get; set; } = Visibility.Collapsed;

		public Visibility ValueIfNotEmpty { get; set; } = Visibility.Visible;

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			if (value is not string stringValue)
			{
				return ValueIfEmpty;
			}
			else
			{
				return string.IsNullOrEmpty(stringValue) ? ValueIfEmpty : ValueIfNotEmpty;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
