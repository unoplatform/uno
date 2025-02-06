#if HAS_UNO
using Uno.Foundation.Logging;
using Windows.UI.Xaml;

namespace Uno.UI.Samples.Content.UITests.WebView
{
	public static class WebViewObserverBehavior
	{
		#region IsAttached ATTACHED PROPERTY

		public static bool GetIsAttached(Windows.UI.Xaml.Controls.WebView obj)
		{
			return (bool)obj.GetValue(IsAttachedProperty);
		}

		public static void SetIsAttached(Windows.UI.Xaml.Controls.WebView obj, bool value)
		{
			obj.SetValue(IsAttachedProperty, value);
		}

		public static DependencyProperty IsAttachedProperty { get; } =
			DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(WebViewObserverBehavior), new PropertyMetadata(false, OnIsAttachedChanged));

		private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var webView = d as Windows.UI.Xaml.Controls.WebView;

			if (webView != null)
			{
				UnregisterEvents(webView);

				if ((bool)e.NewValue)
				{
					RegisterEvents(webView);
				}
			}
		}

		#endregion

		#region Message ATTACHED PROPERTY

		public static string GetMessage(DependencyObject obj)
		{
			return (string)obj.GetValue(MessageProperty);
		}

		public static void SetMessage(DependencyObject obj, string value)
		{
			obj.SetValue(MessageProperty, value);
		}

		public static DependencyProperty MessageProperty { get; } =
			DependencyProperty.RegisterAttached("Message", typeof(string), typeof(WebViewObserverBehavior), new PropertyMetadata(null));

		#endregion

		private static void RegisterEvents(Windows.UI.Xaml.Controls.WebView webView)
		{
			webView.NavigationStarting += WebView_NavigationStarting;
			webView.NavigationCompleted += WebView_NavigationCompleted;
			webView.NavigationFailed += WebView_NavigationFailed;
		}

		private static void UnregisterEvents(Windows.UI.Xaml.Controls.WebView webView)
		{
			webView.NavigationStarting -= WebView_NavigationStarting;
			webView.NavigationCompleted -= WebView_NavigationCompleted;
			webView.NavigationFailed -= WebView_NavigationFailed;
		}

		private static void WebView_NavigationStarting(Windows.UI.Xaml.Controls.WebView sender, Windows.UI.Xaml.Controls.WebViewNavigationStartingEventArgs args)
		{
			var message = $"NavigationStarting @ {args.Uri} [{sender.Source}]";

			SetMessage(sender, message);
			sender.Log().Debug(message);
		}

		private static void WebView_NavigationFailed(object sender, Windows.UI.Xaml.Controls.WebViewNavigationFailedEventArgs e)
		{
			var webView = sender as Windows.UI.Xaml.Controls.WebView;
			var message = $"NavigationFailed {e.WebErrorStatus} @ {e.Uri} [{webView.Source}]";

			SetMessage(webView, message);
			sender.Log().Debug(message);
		}

		private static void WebView_NavigationCompleted(Windows.UI.Xaml.Controls.WebView sender, Windows.UI.Xaml.Controls.WebViewNavigationCompletedEventArgs args)
		{
			var message = $"NavigationCompleted @ {args.Uri} [{sender.Source}]";

			SetMessage(sender, message);
			sender.Log().Debug(message);
		}
	}
}
#endif
