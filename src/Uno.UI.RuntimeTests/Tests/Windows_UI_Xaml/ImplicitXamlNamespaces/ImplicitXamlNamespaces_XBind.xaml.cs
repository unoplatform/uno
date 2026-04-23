using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls
{
	public sealed partial class ImplicitXamlNamespaces_XBind : UserControl
	{
		public ImplicitXamlNamespaces_XBind()
		{
			this.InitializeComponent();
		}

		public string TestText
		{
			get => (string)GetValue(TestTextProperty);
			set => SetValue(TestTextProperty, value);
		}

		public static readonly DependencyProperty TestTextProperty =
			DependencyProperty.Register(nameof(TestText), typeof(string), typeof(ImplicitXamlNamespaces_XBind),
				new PropertyMetadata("Default"));
	}
}
