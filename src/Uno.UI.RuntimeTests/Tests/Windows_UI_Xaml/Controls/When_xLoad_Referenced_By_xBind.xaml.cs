using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using Annotations = Uno.UI.RuntimeTests.Helpers.Annotations;

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
	public object Convert(object value, [DynamicallyAccessedMembers(Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
	{
		if (value == null || value is bool b && b == false)
		{
			return false;
		}

		return true;
	}

	public object ConvertBack(object value, [DynamicallyAccessedMembers(Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language)
	{
		if (value == null || value is bool b && b == false)
		{
			return false;
		}

		return true;
	}
}
