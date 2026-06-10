using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.UI.Xaml.Data;

namespace UITests.ValueConverters
{
	public class FromNullableBoolToCustomValueConverter : IValueConverter
	{
		public object NullOrFalseValue { get; set; }
		public object TrueValue { get; set; }

		public object Convert(object value, [DynamicallyAccessedMembers(Uno.UI.RuntimeTests.Helpers.Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			if (value == null || !System.Convert.ToBoolean(value, CultureInfo.InvariantCulture))
			{
				return NullOrFalseValue;
			}
			else
			{
				return TrueValue;
			}
		}

		public object ConvertBack(object value, [DynamicallyAccessedMembers(Uno.UI.RuntimeTests.Helpers.Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
