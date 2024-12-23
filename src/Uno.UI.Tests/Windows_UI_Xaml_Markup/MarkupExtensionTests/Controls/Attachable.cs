using Windows.UI.Xaml;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.MarkupExtensionTests.Controls;

public static class Attachable
{
	#region DependencyProperty: Value

	public static DependencyProperty ValueProperty { get; } = DependencyProperty.RegisterAttached(
		"Value",
		typeof(object),
		typeof(Attachable),
		new PropertyMetadata(default(object)));

	public static object GetValue(DependencyObject obj) => (object)obj.GetValue(ValueProperty);
	public static void SetValue(DependencyObject obj, object value) => obj.SetValue(ValueProperty, value);

	#endregion
	#region DependencyProperty: Value2

	public static DependencyProperty Value2Property { get; } = DependencyProperty.RegisterAttached(
		"Value2",
		typeof(int),
		typeof(Attachable),
		new PropertyMetadata(default(int)));

	public static int GetValue2(DependencyObject obj) => (int)obj.GetValue(Value2Property);
	public static void SetValue2(DependencyObject obj, int value) => obj.SetValue(Value2Property, value);

	#endregion
}
