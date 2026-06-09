using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Microsoft.UI.Xaml.Data;

namespace XamlGenerationTests.Shared
{
	public class ObjectConverter : IValueConverter
	{
		public object TrueValue { get; set; }

		public object FalseValue { get; set; }

		public object Convert(object value, [DynamicallyAccessedMembers(IValueConverter.TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}

		public object ConvertBack(object value, [DynamicallyAccessedMembers(IValueConverter.TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
