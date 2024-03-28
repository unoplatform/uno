using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Samples.Converters
{
	public class FromNullableBoolToVisibilityConverter : IValueConverter
	{
		public FromNullableBoolToVisibilityConverter()
		{
			this.VisibilityIfTrue = Converters.VisibilityIfTrue.Visible;
		}

		public VisibilityIfTrue VisibilityIfTrue { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			bool inverse = this.VisibilityIfTrue == VisibilityIfTrue.Collapsed;

			Visibility visibilityOnTrue = (!inverse) ? Visibility.Visible : Visibility.Collapsed;
			Visibility visibilityOnFalse = (!inverse) ? Visibility.Collapsed : Visibility.Visible;

			if (value != null && !(value is bool))
			{
				throw new ArgumentException($"Value must either be null or of type bool. Got {value} ({value.GetType().FullName})");
			}

			var valueToConvert = value != null && System.Convert.ToBoolean(value, CultureInfo.InvariantCulture);

			return valueToConvert ? visibilityOnTrue : visibilityOnFalse;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}

	public enum VisibilityIfTrue
	{
		Visible,
		Collapsed
	}
}
