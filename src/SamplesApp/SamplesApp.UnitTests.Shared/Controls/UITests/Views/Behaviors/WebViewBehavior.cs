#if XAMARIN || __WASM__
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Logging;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using _WebView = Windows.UI.Xaml.Controls.WebView;

namespace Uno.UI.Samples.Behaviors
{
	/// <summary>
	/// Encapsulates webview behaviors
	/// </summary>
	public class WebViewBehavior
	{
		/// <summary>
		/// Register attached source string
		/// </summary>
		public static DependencyProperty SourceStringProperty { get ; } =
			DependencyProperty.RegisterAttached("SourceString", typeof(string), typeof(WebViewBehavior), new FrameworkPropertyMetadata(string.Empty, OnSourceStringChanged));

		/// <summary>
		/// Gets source string
		/// </summary>
		/// <param name="obj">Webview</param>
		/// <returns>Source string</returns>
		public static string GetSourceString(_WebView obj)
		{
			return (string)obj.GetValue(SourceStringProperty);
		}

		/// <summary>
		/// Sets source string
		/// </summary>
		/// <param name="obj">Webview</param>
		/// <param name="value">Source string</param>
		public static void SetSourceString(_WebView obj, string value)
		{
			obj.SetValue(SourceStringProperty, value);
		}

		private static void OnSourceStringChanged(object d, DependencyPropertyChangedEventArgs e)
		{
			(d as _WebView).NavigateToString(e.NewValue.ToString());
		}
	}
}
#endif
