using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

#if HAS_UNO
using Uno.Foundation.Logging;
#else
using Uno.Logging;
using Uno.Extensions;
#endif

namespace Uno.UI.Samples.Converters
{
	public class StringFormatConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string culture)
		{
#if WINAPPSDK || __ANDROID__ || __IOS__
			var currentCulture = string.IsNullOrWhiteSpace(culture) ? CultureInfo.CurrentUICulture : new CultureInfo(culture);
#else
			var currentCulture = culture;
#endif

			if (parameter == null)
			{
				this.Log().Warn("You didn't specified the pattern to use for a string format converter. You should specified it using the ConverterParameter.");
				parameter = string.Empty;
			}

			return String.Format(currentCulture, parameter.ToString(), value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string culture)
		{
			throw new NotSupportedException();
		}
	}
}
