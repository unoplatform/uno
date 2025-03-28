using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;

namespace UITests.Shared.Windows_UI_Xaml.MarkupExtension
{
	[MarkupExtensionReturnType(ReturnType = typeof(IValueConverter))]
	public class InverseBool : Windows.UI.Xaml.Markup.MarkupExtension, IValueConverter
	{
		protected override object ProvideValue() => this;

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return !(bool)(value ?? false);
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return !(bool)(value ?? false);
		}
	}
}
