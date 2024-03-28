using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Windows.UI.Xaml.Data
{
	public partial interface IValueConverter
	{
		object Convert(object value, Type targetType, object parameter, string language);
		object ConvertBack(object value, Type targetType, object parameter, string language);
	}
}
