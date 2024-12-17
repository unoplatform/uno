using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Samples.Converters
{
	public class FromNullableToDefaultValueConverter : IValueConverter
	{
		public FromNullableToDefaultValueConverter()
		{
			ValueIfNull = null;
			ValueIfNotNull = null;
		}

		public object ValueIfNull { get; set; }

		public object ValueIfNotNull { get; set; }
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (parameter != null)
			{
				throw new ArgumentException($"This converter does not use any parameters. You should remove \"{parameter}\" passed as parameter.");
			}

			if (value == null)
			{
				return ValueIfNull ?? GetDefaultValue(targetType);
			}
			else
			{
				return ValueIfNotNull ?? value;
			}
		}

		private static object GetDefaultValue(Type targetType)
		{
#if SILVERLIGHT
			return targetType.IsValueType ?
#else
			return targetType.GetTypeInfo().IsValueType ?
#endif
				Activator.CreateInstance(targetType) :
				null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language) => throw new NotImplementedException();
	}
}
