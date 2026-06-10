using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

#if WinUI
using Microsoft.UI.Xaml.Data;
#else
using Microsoft.UI.Xaml.Data;
#endif

namespace UITests.Windows_UI_Xaml_Controls.TextBox
{
	public class FromEmptyStringToValueConverter : IValueConverter
	{
		public object NullOrEmptyValue { get; set; }

		public object NotNullOrEmptyValue { get; set; }

		public object Convert(object value, [DynamicallyAccessedMembers(Uno.UI.RuntimeTests.Helpers.Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			if (!(value is string str) || string.IsNullOrEmpty(str))
			{
				return NullOrEmptyValue;
			}

			return NotNullOrEmptyValue;
		}

		public object ConvertBack(object value, [DynamicallyAccessedMembers(Uno.UI.RuntimeTests.Helpers.Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
