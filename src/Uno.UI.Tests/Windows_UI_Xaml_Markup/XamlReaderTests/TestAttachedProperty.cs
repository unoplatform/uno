using Microsoft.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests
{
	public static class TestAttachedProperty
	{
		public static readonly DependencyProperty TestIdProperty =
			DependencyProperty.RegisterAttached(
				"TestId",
				typeof(string),
				typeof(TestAttachedProperty),
				new PropertyMetadata(null));

		public static string GetTestId(DependencyObject obj)
		{
			return (string)obj.GetValue(TestIdProperty);
		}

		public static void SetTestId(DependencyObject obj, string value)
		{
			obj.SetValue(TestIdProperty, value);
		}
	}
}
