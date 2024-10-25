using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public sealed partial class When_xLoad_Referenced_By_xBind : Page
{
	public When_xLoad_Referenced_By_xBind()
	{
		this.InitializeComponent();
	}
}

internal class NullableBoolConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value == null || value is bool b && b == false)
		{
			return false;
		}

		return true;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		if (value == null || value is bool b && b == false)
		{
			return false;
		}

		return true;
	}
}
