using System;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Data;

public sealed partial class CustomPanel : Panel
{
	public BindingBase NonDP1 { get; set; }

	public BindingBase NonDP2
	{
		get => (BindingBase)GetValue(NonDP2Property);
		set => SetValue(NonDP2Property, value);
	}

	public static DependencyProperty NonDP2Property { get; } =
		DependencyProperty.Register($"Not_{nameof(NonDP2)}", typeof(BindingBase), typeof(CustomPanel), new PropertyMetadata(null));

	public BindingBase DP
	{
		get => (BindingBase)GetValue(DPProperty);
		set => SetValue(DPProperty, value);
	}

	public static DependencyProperty DPProperty { get; } =
		DependencyProperty.Register(nameof(DP), typeof(BindingBase), typeof(CustomPanel), new PropertyMetadata(null));
}

public sealed partial class Binding_DP_TestCases : Page
{
	public Binding_DP_TestCases()
	{
		this.InitializeComponent();
	}
}
