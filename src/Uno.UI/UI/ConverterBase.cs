using System;
using System.Reflection;
using Windows.UI.Xaml.Data;

using GenericCulture = System.String;

namespace Uno.UI.Converters
{
	// We removed TargetType validation because of implicit conversion verification.  Since in some platforms
	// (not all), we can implicitly convert from one type to another either by using the implicit operators on
	// the type definitions, or by creating a TypeConverter, we had a lot of trouble validating the TargetType
	// in all scenarios in a lean and fast manner.  E.G. Verifying that a "Visible" string value can indeed
	// be valid when TargetType is Visibility.

	public abstract class ConverterBase : IValueConverter
	{
		object IValueConverter.Convert(object value, Type targetType, object parameter, GenericCulture culture)
		{
			return Convert(value, targetType, parameter);
		}

		protected abstract object Convert(object value, Type targetType, object parameter);

		object IValueConverter.ConvertBack(object value, Type targetType, object parameter, GenericCulture culture)
		{
			return ConvertBack(value, targetType, parameter);
		}

		protected virtual object ConvertBack(object value, Type targetType, object parameter)
		{
			throw new NotSupportedException();
		}
	}
}
