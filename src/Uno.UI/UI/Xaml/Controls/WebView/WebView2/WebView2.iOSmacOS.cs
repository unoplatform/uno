using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Uno.UI;
using Uno.Extensions;
using Uno.UI.Extensions;
using Windows.UI.Xaml;
using Uno.UI.Web;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Windows.Foundation;

#if XAMARIN_IOS_UNIFIED

#if !__MACCATALYST__  // catalyst https://github.com/xamarin/xamarin-macios/issues/13935
using MessageUI;
#endif
using Foundation;
using UIKit;
using CoreGraphics;
#else
using Foundation;
using AppKit;
using CoreGraphics;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class WebView2
	{
		// A version-dependent webView. WKWebView should be used where available (8.0+) to avoid memory leaks, UIWebView is used on older versions
		private INativeWebView _nativeWebView;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_nativeWebView = this
				.FindSubviewsOfType<INativeWebView>()
				.FirstOrDefault();

			if (_nativeWebView == null)
			{
				this.Log().Error($"No view of type {nameof(UnoWKWebView)} found in children, are you missing one of these types in a template ? ");
			}

			_nativeWebView?.RegisterNavigationEvents(this);

			UpdateFromInternalSource();
		}

		partial void GoBackPartial()
		{
			_nativeWebView.GoBack();
		}

		partial void GoForwardPartial()
		{
			_nativeWebView.GoForward();
		}

		//This should be IAsyncOperation<string> instead of Task<string> but we use an extension method to enable the same signature in Win.
		//IAsyncOperation is not available in Xamarin.
		public async Task<string> InvokeScriptAsync(CancellationToken ct, string script, string[] arguments)
		{
			var argumentString = ConcatenateJavascriptArguments(arguments);
			return await _nativeWebView.EvaluateJavascriptAsync(ct, string.Format(CultureInfo.InvariantCulture, "javascript:{0}(\"{1}\")", script, argumentString));
		}

		public IAsyncOperation<string> InvokeScriptAsync(string scriptName, IEnumerable<string> arguments) =>
			AsyncOperation.FromTask(ct => InvokeScriptAsync(ct, scriptName, arguments?.ToArray()));

		partial void NavigateWithHttpRequestMessagePartial(HttpRequestMessage requestMessage)
		{
			if (requestMessage == null)
			{
				this.Log().Warn("HttpRequestMessage is null. Please make sure the http request is complete.");
				return;
			}

			if (!this.VerifyNativeWebViewAvailability())
			{
				return;
			}

			var urlRequest = new NSMutableUrlRequest(requestMessage.RequestUri);
			var headerDictionnary = new NSMutableDictionary();

			foreach (var header in requestMessage.Headers)
			{
				headerDictionnary.AddDistinct(new KeyValuePair<NSObject, NSObject>(NSObject.FromObject(header.Key), NSObject.FromObject(header.Value.JoinBy(", "))));
			}

			urlRequest.Headers = headerDictionnary;

			_nativeWebView.LoadRequest(urlRequest);
		}

		partial void NavigateToStringPartial(string text)
		{
			if (!this.VerifyNativeWebViewAvailability())
			{
				return;
			}

			_nativeWebView.LoadHtmlString(text, null);
		}

		partial void NavigatePartial(Uri uri)
		{
			if (!this.VerifyNativeWebViewAvailability())
			{
				return;
			}

			if (uri.Scheme.Equals("local", StringComparison.OrdinalIgnoreCase))
			{
				var path = $"{NSBundle.MainBundle.BundlePath}/{uri.PathAndQuery}";

				_nativeWebView.LoadRequest(new NSUrlRequest(new NSUrl(path, false)));
			}
			else
			{
				_nativeWebView.LoadRequest(new NSUrlRequest(new NSUrl(uri.AbsoluteUri)));
			}
		}

		private void OpenUrl(string url)
		{
			var nsUrl = new NSUrl(url);
			//Opens the specified URL, launching the app that's registered to handle the scheme.
#if __IOS__
			UIApplication.SharedApplication.OpenUrl(nsUrl);
#else
			NSWorkspace.SharedWorkspace.OpenUrl(nsUrl);
#endif
		}
		
		internal void OnComplete(Uri uri, bool isSuccessful, WebErrorStatus status)
		{
			var args = new WebViewNavigationCompletedEventArgs()
			{
				IsSuccess = isSuccessful,
				Uri = uri ?? Source,
				WebErrorStatus = status
			};

			CanGoBack = _nativeWebView.CanGoBack;
			CanGoForward = _nativeWebView.CanGoForward;

			NavigationCompleted?.Invoke(this, args);
		}

		partial void StopPartial()
		{
			_nativeWebView.StopLoading();
		}

		public void Refresh()
		{
			_nativeWebView.Reload();
		}

		internal void OnNavigationStarting(WebViewNavigationStartingEventArgs args)
		{
			if (args.Uri == null)
			{
				//This case should not happen when navigating normally using http requests.
				//This is to stop a scenario where the webview is initialized without having a source
				args.Cancel = true;
				return;
			}

			if (args.Uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
			{
#if __IOS__
				ParseUriAndLauchMailto(args.Uri);
#else
				NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(args.Uri.ToString()));
#endif
				args.Cancel = true;
				return;
			}

			NavigationStarting?.Invoke(this, args);
		}

#if __IOS__
		private void ParseUriAndLauchMailto(Uri mailtoUri)
		{
			_ = Uno.UI.Dispatching.CoreDispatcher.Main.RunAsync(
				Uno.UI.Dispatching.CoreDispatcherPriority.Normal,
				async (ct) =>
				{
					try
					{
						var subject = "";
						var body = "";
						var cc = new[] { "" };
						var bcc = new[] { "" };

						var recipients = mailtoUri.AbsoluteUri.Split(new[] { ':' })[1].Split(new[] { '?' })[0].Split(new[] { ',' });
						var parameters = mailtoUri.Query.Split(new[] { '?' });

						parameters = parameters.Length > 1 ?
										parameters[1].Split(new[] { '&' }) :
										Array.Empty<string>();

						foreach (string param in parameters)
						{
							var keyValue = param.Split(new[] { '=' });
							var key = keyValue[0];
							var value = keyValue[1];

							switch (key)
							{
								case "subject":
									subject = value;
									break;
								case "to":
									recipients.Concat(value);
									break;
								case "body":
									body = value;
									break;
								case "cc":
									cc.Concat(value);
									break;
								case "bcc":
									bcc.Concat(value);
									break;
								default:
									break;
							}
						}

						await LaunchMailto(ct, subject, body, recipients, cc, bcc);
					}

					catch (Exception e)
					{
						if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
						{
							this.Log().Error("Unable to launch mailto", e);
						}
					}
				});
		}

		public
#if !__MACCATALYST__
		async
#endif
		Task LaunchMailto(CancellationToken ct, string subject = null, string body = null, string[] to = null, string[] cc = null, string[] bcc = null)
		{
#if !__MACCATALYST__  // catalyst https://github.com/xamarin/xamarin-macios/issues/13935
			if (!MFMailComposeViewController.CanSendMail)
			{
				return;
			}

			var mailController = new MFMailComposeViewController();

			mailController.SetToRecipients(to);
			mailController.SetSubject(subject);
			mailController.SetMessageBody(body, false);
			mailController.SetCcRecipients(cc);
			mailController.SetBccRecipients(bcc);

			var finished = new TaskCompletionSource<object>();
			var handler = new EventHandler<MFComposeResultEventArgs>((snd, args) => finished.TrySetResult(args));

			try
			{
				mailController.Finished += handler;
				UIApplication.SharedApplication.KeyWindow.RootViewController.PresentViewController(mailController, true, null);

				using (ct.Register(() => finished.TrySetCanceled()))
				{
					await finished.Task;
				}
			}
			finally
			{
				await CoreDispatcher
					.Main
					.RunAsync(CoreDispatcherPriority.High, () =>
					{
						mailController.Finished -= handler;
						mailController.DismissViewController(true, null);
					})
					.AsTask(CancellationToken.None);
			}
#else
			return Task.CompletedTask;
#endif
		}
#endif

		internal void OnNewWindowRequested(WebViewNewWindowRequestedEventArgs args)
		{
			NewWindowRequested?.Invoke(this, args);
		}

		internal void OnNavigationFailed(WebViewNavigationFailedEventArgs args)
		{
			NavigationFailed?.Invoke(this, args);
		}


		[Obsolete("https://github.com/unoplatform/uno/pull/1591")]
		public static bool MustUseWebKitWebView() => true;

		partial void OnScrollEnabledChangedPartial(bool scrollingEnabled)
		{
			_nativeWebView.SetScrollingEnabled(scrollingEnabled);
		}

		private bool VerifyNativeWebViewAvailability()
		{
			if (_nativeWebView == null)
			{
				if (_isLoaded)
				{
					this.Log().Warn("This WebView control instance does not have a native web view child, a Control template may be missing.");
				}

				return false;
			}

			return true;
		}
	}
}

