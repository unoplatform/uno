using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// Register the custom namespace to the global URI so types resolve unprefixed in XAML
[assembly: System.Windows.Markup.XmlnsDefinition(
	"http://schemas.microsoft.com/winfx/2006/xaml/presentation/global",
	"Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.CustomGlobal")]

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.CustomGlobal
{
	/// <summary>
	/// A custom control registered to the global namespace URI for testing
	/// that custom types resolve unprefixed in XAML.
	/// </summary>
	public partial class CustomGlobalControl : ContentControl
	{
		public string CustomLabel
		{
			get => (string)GetValue(CustomLabelProperty);
			set => SetValue(CustomLabelProperty, value);
		}

		public static readonly DependencyProperty CustomLabelProperty =
			DependencyProperty.Register(nameof(CustomLabel), typeof(string), typeof(CustomGlobalControl),
				new PropertyMetadata("Default Label"));
	}

	/// <summary>
	/// A second custom control in a different sub-namespace, also registered globally,
	/// to test that multiple types from the same global namespace resolve.
	/// </summary>
	public partial class AnotherGlobalControl : ContentControl
	{
		public int CustomValue
		{
			get => (int)GetValue(CustomValueProperty);
			set => SetValue(CustomValueProperty, value);
		}

		public static readonly DependencyProperty CustomValueProperty =
			DependencyProperty.Register(nameof(CustomValue), typeof(int), typeof(AnotherGlobalControl),
				new PropertyMetadata(0));
	}
}
