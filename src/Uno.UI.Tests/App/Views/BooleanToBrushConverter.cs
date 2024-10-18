using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Tests.App.Views
{
	public class BooleanToBrushConverter : IValueConverter
	{
		public Brush TrueValue { get; set; }
		public Brush FalseValue { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value is bool b)
			{
				return b ? TrueValue
					: FalseValue;
			}

			return null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
