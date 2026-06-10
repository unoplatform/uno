using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

using Annotations = Uno.UI.RuntimeTests.Helpers.Annotations;

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
	public object Convert(object value, [DynamicallyAccessedMembers(Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language) => value;
	public object ConvertBack(object value, [DynamicallyAccessedMembers(Annotations.IValueConverter_TargetTypeRequirements)] Type targetType, object parameter, string language) => value;
}
