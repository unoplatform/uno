using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

public sealed partial class When_Binding_Sources_Setup : Button
{
	public When_Binding_Sources_Setup()
	{
		this.InitializeComponent();
	}
}

public partial class When_Binding_Sources_Setup_DebugConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language) => value;
	public object ConvertBack(object value, Type targetType, object parameter, string language) => value;
}
