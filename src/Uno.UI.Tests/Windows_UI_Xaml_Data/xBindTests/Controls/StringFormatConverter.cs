using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public class InvariantStringFormatConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string culture)
		{
			if (parameter == null)
			{
				parameter = string.Empty;
			}

			return String.Format(CultureInfo.InvariantCulture, parameter.ToString(), value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string culture)
		{
			throw new NotSupportedException();
		}
	}
}
