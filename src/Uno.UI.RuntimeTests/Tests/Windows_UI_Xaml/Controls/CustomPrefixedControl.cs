using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// Register a custom namespace with an XmlnsPrefix so it's implicitly available as "tc:" in XAML
[assembly: System.Windows.Markup.XmlnsDefinition(
	"http://test.example.com/custom",
	"Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.CustomPrefixed")]
[assembly: System.Windows.Markup.XmlnsPrefix(
	"http://test.example.com/custom",
	"tc")]

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls.CustomPrefixed
{
	/// <summary>
	/// A custom control in a prefixed namespace for testing that XmlnsPrefix
	/// makes the prefix implicitly available in XAML without explicit xmlns:tc declaration.
	/// </summary>
	public class PrefixedControl : ContentControl
	{
		public string PrefixedLabel
		{
			get => (string)GetValue(PrefixedLabelProperty);
			set => SetValue(PrefixedLabelProperty, value);
		}

		public static readonly DependencyProperty PrefixedLabelProperty =
			DependencyProperty.Register(nameof(PrefixedLabel), typeof(string), typeof(PrefixedControl),
				new PropertyMetadata("Default Prefixed"));
	}
}
