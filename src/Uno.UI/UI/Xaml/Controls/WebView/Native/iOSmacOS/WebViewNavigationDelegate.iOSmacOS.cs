using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls;

internal class WebViewNavigationDelegate : WKNavigationDelegate
{
	/// <summary>
	/// The reference to the parent UnoWKWebView class on which we invoke callbacks.
	/// </summary>
	private readonly WeakReference<UnoWKWebView> _unoWKWebView;

	public WebViewNavigationDelegate(UnoWKWebView unoWKWebView)
	{
		_unoWKWebView = new WeakReference<UnoWKWebView>(unoWKWebView);
	}

	public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
	{
		var requestUrl = navigationAction.Request?.Url.ToUri();

		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DecidePolicy: NavigationType: {navigationAction.NavigationType} Request:{requestUrl} TargetRequest: {navigationAction.TargetFrame?.Request}");
			}

			var scheme = requestUrl.Scheme;

			// Note that the "file" scheme is not officially supported by the UWP WebView (https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.webview.unsupportedurischemeidentified?view=winrt-19041#remarks).
			// We have to support it here for anchor navigation (as long as https://github.com/unoplatform/uno/issues/2998 is not resolved).
			var isUnsupportedScheme = !scheme.Equals("http", StringComparison.OrdinalIgnoreCase) && !scheme.Equals("https", StringComparison.OrdinalIgnoreCase) && !scheme.Equals("file", StringComparison.OrdinalIgnoreCase);
			if (isUnsupportedScheme)
			{
				bool cancelled = unoWKWebView.OnUnsupportedUriSchemeIdentified(requestUrl);

				decisionHandler(cancelled ? WKNavigationActionPolicy.Cancel : WKNavigationActionPolicy.Allow);

				return;
			}

			// The WKWebView doesn't raise navigation event for anchor navigation.
			// When we detect anchor navigation, we must raise the events (NavigationStarting & NavigationFinished) ourselves.
			var isAnchorNavigation = GetIsAnchorNavigation();
			if (isAnchorNavigation)
			{
				bool cancelled = unoWKWebView.OnStarted(requestUrl, stopLoadingOnCanceled: false);

				decisionHandler(cancelled ? WKNavigationActionPolicy.Cancel : WKNavigationActionPolicy.Allow);

				if (!cancelled)
				{
					unoWKWebView.OnNavigationFinished(requestUrl);
				}

				return;
			}

			// For all other cases, we allow the navigation. This will results in other WKNavigationDelegate methods being called.
			decisionHandler(WKNavigationActionPolicy.Allow);

			bool GetIsAnchorNavigation()
			{
				// If we navigate to the exact same page but with a different location (using anchors), the native control will not notify us of
				// any navigation. We need to create this notification to indicate that the navigation worked.

				// To detect an anchor navigation, both the previous and new urls need to match on the left part of the anchor indicator ("#")
				// AND the new url needs to have content on the right of the anchor indicator.
				var currentUrlParts = unoWKWebView._urlLastNavigation?.AbsoluteUrl?.ToString().Split(new string[] { "#" }, StringSplitOptions.None);
				var newUrlParts = requestUrl?.AbsoluteUri?.ToString().Split(new string[] { "#" }, StringSplitOptions.None);

				return currentUrlParts?.Length > 0
					&& newUrlParts?.Length > 1
					&& currentUrlParts[0].Equals(newUrlParts[0]);
			}
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Warning))
			{
				this.Log().LogWarning($"WKNavigationDelegate.DecidePolicy: Cancelling navigation because owning WKWebView is null (NavigationType: {navigationAction.NavigationType} Request:{requestUrl} TargetRequest: {navigationAction.TargetFrame?.Request})");
			}

			// CancellationToken the navigation, we're in a case where the owning WKWebView is not alive anymore
			decisionHandler(WKNavigationActionPolicy.Cancel);
		}
	}

	public override void DecidePolicy(WKWebView webView, WKNavigationResponse navigationResponse, Action<WKNavigationResponsePolicy> decisionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DecidePolicy {navigationResponse.Response?.Url?.ToUri()}");
		}

		decisionHandler(WKNavigationResponsePolicy.Allow);
	}

	public override void DidReceiveServerRedirectForProvisionalNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidReceiveServerRedirectForProvisionalNavigation: Request:{webView.Url?.ToUri()}");
		}

		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnStarted(webView.Url?.ToUri());
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidReceiveServerRedirectForProvisionalNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void ContentProcessDidTerminate(WKWebView webView)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.ContentProcessDidTerminate: Request:{webView.Url?.ToUri()}");
		}
	}

	public override void DidCommitNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidCommitNavigation: Request:{webView.Url?.ToUri()}");
		}
	}

	public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
	{
		var url = webView.Url?.ToUri();
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidFinishNavigation: Request:{url}");
		}

		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnNavigationFinished(url);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidFinishNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKNavigationDelegate.DidCommitNavigation: Request:{webView.Url?.ToUri()}");
		}

		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnError(webView, navigation, error);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidFailNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}
	public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
	{
		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnStarted(webView.Url?.ToUri());
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidStartProvisionalNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
	{
		if (_unoWKWebView.TryGetTarget(out var unoWKWebView))
		{
			unoWKWebView.OnError(webView, navigation, error);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"WKNavigationDelegate.DidFailProvisionalNavigation: Ignoring because owning WKWebView is null");
			}
		}
	}

	//_owner should be IAsyncOperation<string> instead of Task<string> but we use an extension method to enable the same signature in Win.
	//IAsyncOperation is not available in Xamarin.
	internal async Task<string> InvokeScriptAsync(CancellationToken ct, string script, string[] arguments)
	{
		var argumentString = ConcatenateJavascriptArguments(arguments);
		return await _nativeWebView.EvaluateJavascriptAsync(ct, string.Format(CultureInfo.InvariantCulture, "javascript:{0}(\"{1}\")", script, argumentString));
	}

	internal IAsyncOperation<string> InvokeScriptAsync(string scriptName, IEnumerable<string> arguments) =>
		AsyncOperation.FromTask(ct => InvokeScriptAsync(ct, scriptName, arguments?.ToArray()));

	void INativeWebView.ProcessNavigation(HttpRequestMessage requestMessage)
	{
		if (requestMessage == null)
		{
			_owner.Log().Warn("HttpRequestMessage is null. Please make sure the http request is complete.");
			return;
		}

		var urlRequest = new NSMutableUrlRequest(requestMessage.RequestUri);
		var headerDictionnary = new NSMutableDictionary();

		foreach (var header in requestMessage.Headers)
		{
			headerDictionnary.AddDistinct(new KeyValuePair<NSObject, NSObject>(NSObject.FromObject(header.Key), NSObject.FromObject(header.Value.JoinBy(", "))));
		}

		urlRequest.Headers = headerDictionnary;

		ProcessNSUrlRequest(urlRequest);
	}


	private void ProcessNSUrlRequest(NSUrlRequest request)
	{
		if (request == null)
		{
			throw new ArgumentNullException(nameof(request));
		}

		var uri = request.Url?.ToUri();

		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"LoadRequest: {request.Url?.ToUri()}");
		}

		if (string.Equals(uri?.Scheme, "file", StringComparison.OrdinalIgnoreCase))
		{
			HandleFileNavigation(request);
		}
		else
		{
			LoadRequest(request);
		}
	}

	void INativeWebView.ProcessNavigation(string html)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"LoadHtmlString: {html}");
		}

		LoadHtmlString(html, null);

		_urlLastNavigation = null;
	}

	void INativeWebView.ProcessNavigation(Uri uri)
	{
		if (uri.Scheme.Equals("local", StringComparison.OrdinalIgnoreCase))
		{
			var path = $"{NSBundle.MainBundle.BundlePath}/{uri.PathAndQuery}";

			ProcessNSUrlRequest(new NSUrlRequest(new NSUrl(path, false)));
		}
		else
		{
			ProcessNSUrlRequest(new NSUrlRequest(new NSUrl(uri.AbsoluteUri)));
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

		NavigationCompleted?.Invoke(_owner, args);
	}

	internal void OnNavigationStarting(WebViewNavigationStartingEventArgs args)
	{
		if (args.Uri == null)
		{
			//_owner case should not happen when navigating normally using http requests.
			//_owner is to stop a scenario where the webview is initialized without having a source
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

		NavigationStarting?.Invoke(_owner, args);
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
					if (_owner.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						_owner.Log().Error("Unable to launch mailto", e);
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
		NewWindowRequested?.Invoke(_owner, args);
	}

	internal void OnNavigationFailed(WebViewNavigationFailedEventArgs args)
	{
		NavigationFailed?.Invoke(_owner, args);
	}


	[Obsolete("https://github.com/unoplatform/uno/pull/1591")]
	internal static bool MustUseWebKitWebView() => true;

	private void OnScrollEnabledChangedPartial(bool scrollingEnabled)
	{
		_nativeWebView.SetScrollingEnabled(scrollingEnabled);
	}

}
