using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests;

public class MyClass
{
	public static readonly DependencyProperty XProperty = DependencyProperty.RegisterAttached("X", typeof(ResourceDictionary), typeof(MyClass), new PropertyMetadata(default));

	public static void SetX(DependencyObject element, ResourceDictionary value) => element.SetValue(XProperty, value);

	public static ResourceDictionary GetX(DependencyObject element) => (ResourceDictionary)element.GetValue(XProperty);
}

public sealed partial class When_ResourceDictionary_DP : UserControl
{
	public When_ResourceDictionary_DP()
	{

		this.InitializeComponent();
	}
}
