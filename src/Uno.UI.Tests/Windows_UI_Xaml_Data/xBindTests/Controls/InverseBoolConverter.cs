using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public class InverseBoolConverter : IValueConverter
	{
		public object Convert(object value, System.Type targetType, object parameter, string language)
		{
			if (value is bool)
			{
				return !(bool)value;
			}

			return null;
		}

		public object ConvertBack(object value, System.Type targetType, object parameter, string language)
		{
			if (value is bool)
			{
				return !(bool)value;
			}

			return null;
		}
	}
}
