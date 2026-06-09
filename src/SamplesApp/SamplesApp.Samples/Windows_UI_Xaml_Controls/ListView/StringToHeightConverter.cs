using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.UI.Xaml.Data;

namespace UITests.Shared.Windows_UI_Xaml_Controls.ListView
{
	public class StringToHeightConverter : IValueConverter
	{
		public object Convert(object value, [DynamicallyAccessedMembers(IValueConverter.TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			if (value is char c)
			{
				return c == 'A' ? 20d : 15d;
			}

			return 5d;
		}

		public object ConvertBack(object value, [DynamicallyAccessedMembers(IValueConverter.TargetTypeRequirements)] Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
