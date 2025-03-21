#if HAS_UNO
using Uno.Foundation.Logging;
using Windows.UI.Xaml;

namespace SamplesApp.Microsoft_UI_Xaml_Controls.WebView2Tests
{
	public static class WebView2ObserverBehavior
	{
		#region IsAttached ATTACHED PROPERTY

		public static bool GetIsAttached(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 obj)
		{
			return (bool)obj.GetValue(IsAttachedProperty);
		}

		public static void SetIsAttached(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 obj, bool value)
		{
			obj.SetValue(IsAttachedProperty, value);
		}

		public static DependencyProperty IsAttachedProperty { get; } =
			DependencyProperty.RegisterAttached("IsAttached", typeof(bool), typeof(WebView2ObserverBehavior), new PropertyMetadata(false, OnIsAttachedChanged));

		private static void OnIsAttachedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var webView2 = d as Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2;

			if (webView2 != null)
			{
				UnregisterEvents(webView2);

				if ((bool)e.NewValue)
				{
					RegisterEvents(webView2);
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
			DependencyProperty.RegisterAttached("Message", typeof(string), typeof(WebView2ObserverBehavior), new PropertyMetadata(null));

		#endregion

		private static void RegisterEvents(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 webView)
		{
			webView.NavigationStarting += WebView2_NavigationStarting;
			webView.NavigationCompleted += WebView2_NavigationCompleted;
			//webView.NavigationFailed += WebView2_NavigationFailed;//TODO:MZ:
		}

		private static void UnregisterEvents(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 webView)
		{
			webView.NavigationStarting -= WebView2_NavigationStarting;
			webView.NavigationCompleted -= WebView2_NavigationCompleted;
			//webView.NavigationFailed -= WebView2_NavigationFailed; //TODO:MZ:
		}

		private static void WebView2_NavigationStarting(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs args)
		{
			var message = $"NavigationStarting @ {args.Uri} [{sender.Source}]";

			SetMessage(sender, message);
			sender.Log().Debug(message);
		}

		//private static void WebView2_NavigationFailed(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationFailedEventArgs e)
		//{
		//	//var webView2 = sender as Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2;
		//	//var message = $"NavigationFailed {e.WebErrorStatus} @ {e.Uri} [{webView.Source}]";

		//	//SetMessage(webView, message);
		//	//sender.Log().Debug(message); //TODO:MZ:
		//}

		private static void WebView2_NavigationCompleted(Microsoft/* UWP don't rename */.UI.Xaml.Controls.WebView2 sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs args)
		{
			//var message = $"NavigationCompleted @ {args.Uri} [{sender.Source}]";

			//SetMessage(sender, message);
			//sender.Log().Debug(message); //TODO:MZ:
		}
	}
}
#endif
