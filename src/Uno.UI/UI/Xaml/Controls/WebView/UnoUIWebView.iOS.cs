using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Extensions;
using Uno.UI.Web;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml.Controls
{
	public partial class UnoUIWebView : UIWebView, INativeWebView
	{
		private WebView _parentWebView;

		public UnoUIWebView() : base()
		{
			ScalesPageToFit = false;
		}

		public void RegisterNavigationEvents(WebView xamlWebView)
		{
			_parentWebView = xamlWebView;

			LoadStarted += (s, e) =>
			{
				var args = new WebViewNavigationStartingEventArgs()
				{
					Cancel = false,
					Uri = Request.Url.ToUri() ?? _parentWebView.Source
				};

				_parentWebView.OnNavigationStarting(args);

				if (args.Cancel)
				{
					StopLoading();
				}
			};

			LoadError += (s, e) =>
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					this.Log().ErrorFormat("Could not navigate to web page: {0}", e.Error.LocalizedDescription);
				}

				WebErrorStatus status;
				var code = (NSUrlError)(int)e.Error.Code;

				switch (code)
				{//Mapping known errors
					case NSUrlError.CannotFindHost:
						status = WebErrorStatus.CannotConnect;
						break;
					case NSUrlError.Cancelled:
						//ignore completely
						return; //http://stackoverflow.com/questions/1024748/how-do-i-fix-nsurlerrordomain-error-999-in-iphone-3-0-os
					default:
						status = (WebErrorStatus)code; // Where codes are identical (eg. 400, 401...)
						break;
				}

				//In windows NavigationFailed is called after NavigationCompleted - go figure
				_parentWebView.OnNavigationFailed(new WebViewNavigationFailedEventArgs()
				{
					Uri = Request.Url.ToUri() ?? _parentWebView.Source,
					WebErrorStatus = status
				});

				_parentWebView.OnComplete(Request.Url.ToUri(), false, status);
			};

			LoadFinished += (s, e) =>
			{
				//Http Status is not exposed. NavigationFailed will not be called on a 404 for example and the status will be UNKNOWN
				//http://stackoverflow.com/questions/14451012/uiwebview-not-go-to-didfailloadwitherror-when-weblink-not-found

				_parentWebView.OnComplete(Request.Url.ToUri(), isSuccessful: true, status: WebErrorStatus.Unknown);
			};
		}

		public Task<string> EvaluateJavascriptAsync(CancellationToken ct, string javascript)
		{
			var result = EvaluateJavascript(javascript);
			return Task.FromResult(result);
		}

		void INativeWebView.SetScrollingEnabled(bool isScrollingEnabled)
		{
			ScrollView.ScrollEnabled = isScrollingEnabled;
			ScrollView.Bounces = isScrollingEnabled;
		}
	}
}
