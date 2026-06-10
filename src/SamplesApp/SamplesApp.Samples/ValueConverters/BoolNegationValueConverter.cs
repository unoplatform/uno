using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.UI.Xaml.Data;

namespace UITests.ValueConverters
{
	public class BoolNegationValueConverter : IValueConverter
	{
		public object Convert(object value, [DynamicallyAccessedMembers(Uno.UI.RuntimeTests.Helpers.Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language) =>
			NegateValue(value);

		public object ConvertBack(object value, [DynamicallyAccessedMembers(Uno.UI.RuntimeTests.Helpers.Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language) =>
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
