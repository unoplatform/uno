#if HAS_UNO
using System;
using Windows.UI.Xaml;
using WebView2Uno = Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	public static class WebView2SampleBehavior
	{
		public static string GetSourceUri(DependencyObject obj)
		{
			return (string)obj.GetValue(SourceUriProperty);
		}

		public static void SetSourceUri(DependencyObject obj, string value)
		{
			obj.SetValue(SourceUriProperty, value);
		}

		public static DependencyProperty SourceUriProperty { get; } =
			DependencyProperty.RegisterAttached("SourceUri", typeof(string), typeof(WebView2SampleBehavior), new PropertyMetadata("", OnSourceUriChanged));

		private static void OnSourceUriChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			var uriString = e.NewValue?.ToString();
			if (!string.IsNullOrEmpty(uriString))
			{
				((WebView2Uno)d).Source = new Uri(uriString, UriKind.Absolute);
			}
		}
	}
}
#endif
