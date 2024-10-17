using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Data;

namespace UITests.ValueConverters
{
	// This converter is extraneous on Uno, but on UWP displays a readable version of the .NET-mapped type, where without the converter it
	// would otherwise display an (unreadable) string output of the underlying WinRT type
	public class StringConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			string str = value?.ToString();
			return str;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
