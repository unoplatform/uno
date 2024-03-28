using Windows.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public partial class NonDefaultXamlNamespace : FrameworkElement
	{
		public int Test
		{
			get => (int)GetValue(TestProperty);
			set => SetValue(TestProperty, value);
		}

		public static DependencyProperty TestProperty { get; } =
			DependencyProperty.Register(nameof(Test), typeof(int), typeof(NonDefaultXamlNamespace), new PropertyMetadata(0));
	}
}
