using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;

public static class TestAttachedProperties
{
	public static readonly DependencyProperty MyTagProperty =
		DependencyProperty.RegisterAttached("MyTag", typeof(string), typeof(TestAttachedProperties),
			new PropertyMetadata(default(string)));

	public static void SetMyTag(DependencyObject element, string value) => element.SetValue(MyTagProperty, value);

	public static string GetMyTag(DependencyObject element) => (string)element.GetValue(MyTagProperty);
}

public sealed partial class When_xBind_AttachedProperty_OneWay : Page
{
	public When_xBind_AttachedProperty_OneWay()
	{
		this.InitializeComponent();
	}
}
