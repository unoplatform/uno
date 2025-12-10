using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public class TestAttachedProperty
	{
		public static readonly DependencyProperty TestValueProperty =
			DependencyProperty.RegisterAttached(
				"TestValue",
				typeof(string),
				typeof(TestAttachedProperty),
				new PropertyMetadata(default(string)));

		public static void SetTestValue(DependencyObject element, string value)
		{
			element.SetValue(TestValueProperty, value);
		}

		public static string GetTestValue(DependencyObject element)
		{
			return (string)element.GetValue(TestValueProperty);
		}
	}
}
