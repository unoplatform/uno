using Microsoft.UI.Xaml;
using _WebView2 = Microsoft.UI.Xaml.Controls.WebView2;

namespace Uno.UI.Samples.Behaviors;

public static class WebView2Behavior
{
	public static DependencyProperty SourceStringProperty { get; } =
		DependencyProperty.RegisterAttached("SourceString", typeof(string), typeof(WebView2Behavior), new PropertyMetadata(string.Empty, OnSourceStringChanged));

	public static string GetSourceString(_WebView2 obj) => (string)obj.GetValue(SourceStringProperty);

	public static void SetSourceString(_WebView2 obj, string value) => obj.SetValue(SourceStringProperty, value);

	private static void OnSourceStringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		=> (d as _WebView2)?.NavigateToString(e.NewValue?.ToString() ?? string.Empty);
}
