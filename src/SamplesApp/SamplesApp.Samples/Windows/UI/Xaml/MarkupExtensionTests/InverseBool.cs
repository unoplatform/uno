using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;

namespace UITests.Shared.Windows_UI_Xaml.MarkupExtension
{
	[MarkupExtensionReturnType(ReturnType = typeof(IValueConverter))]
	public class InverseBool : Microsoft.UI.Xaml.Markup.MarkupExtension, IValueConverter
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
