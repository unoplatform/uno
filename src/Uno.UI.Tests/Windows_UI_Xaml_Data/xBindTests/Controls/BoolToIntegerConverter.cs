using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace Uno.UI.Tests.Windows_UI_Xaml_Data.xBindTests.Controls
{
	public class BoolToIntegerConverter : IValueConverter
	{
		public bool IsInverse { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			var boolValue = value as bool?;
			if (!boolValue.HasValue)
			{
				return null;
			}

			if (IsInverse)
			{
				boolValue = !boolValue.Value;
			}

			return boolValue.Value ? 1 : 0;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			var intValue = value as int?;
			if (!intValue.HasValue)
			{
				return null;
			}

			var result = intValue.Value >= 1;

			if (IsInverse)
			{
				result = !result;
			}

			return result;
		}
	}
}
