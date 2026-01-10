#nullable enable

using CoreGraphics;
using Foundation;
using System;
using WebKit;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Uno.UI.Xaml.Controls;
using System.Net.Http;
using Microsoft.Web.WebView2.Core;
using System.Collections.Generic;
using System.Globalization;
using Windows.UI.Core;
using Uno.UI;

#if !__MACCATALYST__ // catalyst https://github.com/xamarin/xamarin-macios/issues/13935
using MessageUI;
#endif

#if __APPLE_UIKIT__
using UIKit;
#endif

#pragma warning disable CA1422 // TODO Uno: Deprecated APIs in iOS 17

namespace Microsoft.UI.Xaml.Controls;

#if UIKIT_SKIA
internal
#else
public
#endif
	partial class UnoWKWebView : WKWebView, INativeWebView, IWKScriptMessageHandler
{
	private string? _previousTitle;
	private CoreWebView2? _coreWebView;
	private bool _isCancelling;
	private bool _shouldQueueHistoryChange;
	private string? _lastNavigationUrl;

	private const string WebMessageHandlerName = "unoWebView";
	private const string OkResourceKey = "WebView_Ok";
	private const string CancelResourceKey = "WebView_Cancel";

	private readonly string OkString;
	private readonly string CancelString;

	private bool _isHistoryChangeQueued;
	private bool _isNavigationCompleted;

	/// <summary>
	/// Object of the last navigation. Can be a Uri or HTML string.
	/// </summary>
	internal object? _lastNavigationData;

	public UnoWKWebView() : base(CGRect.Empty, new WebKit.WKWebViewConfiguration())
	{
		_shouldQueueHistoryChange = false;

		var resourceLoader = ResourceLoader.GetForCurrentView();
		var ok = resourceLoader.GetString("OkResourceKey");
		var cancel = resourceLoader.GetString("CancelResourceKey");

		if (NSLocale.CurrentLocale.LanguageCode == "en")
		{
			if (ok == $"[{OkResourceKey}]")
			{
				ok = "OK";
			}
			if (cancel == $"[{CancelResourceKey}]")
			{
				cancel = "Cancel";
			}
		}

#if __IOS__
		if (UIDevice.CurrentDevice.CheckSystemVersion(16, 4))
		{
			Inspectable = Uno.UI.FeatureConfiguration.WebView2.IsInspectable;
		}
#endif

		Configuration.UserContentController.AddScriptMessageHandler(this, WebMessageHandlerName);

		// Set strings with fallback to default English
		OkString = !string.IsNullOrEmpty(ok) ? ok : "OK";
		CancelString = !string.IsNullOrEmpty(cancel) ? cancel : "Cancel";

#if __APPLE_UIKIT__
		if (UIDevice.CurrentDevice.CheckSystemVersion(10, 3))
		{
			_errorMap.Add(NSUrlError.FileOutsideSafeArea, CoreWebView2WebErrorStatus.UnexpectedError);
		}
#endif
	}

	public string DocumentTitle => Title!;

	public void Stop() => StopLoading();

	void INativeWebView.ProcessNavigation(HttpRequestMessage requestMessage)
	{
		if (requestMessage is null)
		{
			this.Log().Warn("HttpRequestMessage is null. Please make sure the http request is complete.");
			return;
		}

		var urlRequest = new NSMutableUrlRequest(requestMessage.RequestUri!);
		var headerDictionnary = new NSMutableDictionary();

		foreach (var header in requestMessage.Headers)
		{
			headerDictionnary.AddDistinct(new KeyValuePair<NSObject, NSObject>(NSObject.FromObject(header.Key), NSObject.FromObject(header.Value.JoinBy(", "))));
		}

		urlRequest.Headers = headerDictionnary;

		ProcessNSUrlRequest(urlRequest);
	}

	void INativeWebView.ProcessNavigation(string html)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"LoadHtmlString: {html}");
		}

		LoadHtmlString(html, null!);

		_lastNavigationData = html;
	}

	void INativeWebView.ProcessNavigation(Uri uri)
	{
		if (uri.Scheme.Equals("local", StringComparison.OrdinalIgnoreCase))
		{
			var path = $"{NSBundle.MainBundle.BundlePath}/{uri.PathAndQuery}";

			ProcessNSUrlRequest(new NSUrlRequest(new NSUrl(path, false)));
			return;
		}

		if (_coreWebView?.HostToFolderMap.TryGetValue(uri.Host.ToLowerInvariant(), out var folderName) == true)
		{
			// Load Url with folder
			var relativePath = uri.PathAndQuery;

			if (relativePath.StartsWith('/'))
			{
				relativePath = relativePath.Substring(1);
			}

			var fullPath = Path.Combine(NSBundle.MainBundle.ResourcePath, folderName, relativePath);

			var nsUrl = new NSUrl("file://" + fullPath);
			ProcessNSUrlRequest(new NSUrlRequest(nsUrl));
		}
		else
		{
			ProcessNSUrlRequest(new NSUrlRequest(new NSUrl(uri.AbsoluteUri)));
		}
	}

	void INativeWebView.SetOwner(object coreWebView2)
	{
		_coreWebView = (CoreWebView2)coreWebView2;

		this.Configuration.Preferences.JavaScriptCanOpenWindowsAutomatically = true;
		if (this.Configuration.DefaultWebpagePreferences is not null)
		{
			this.Configuration.DefaultWebpagePreferences.AllowsContentJavaScript = true;
		}

		NavigationDelegate = new WebViewNavigationDelegate(this);

		UIDelegate = new LocalWKUIDelegate(
			onCreateWebView: OnCreateWebView,
			didClose: OnDidClose,
			onRunJavaScriptAlertPanel: OnRunJavaScriptAlertPanel,
			onRunJavaScriptConfirmPanel: OnRunJavaScriptConfirmPanel,
			onRunJavaScriptTextInputPanel: OnRunJavaScriptTextInputPanel
		);
	}

	internal bool OnUnsupportedUriSchemeIdentified(Uri targetUri)
	{
		bool handled = false;
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnUnsupportedUriSchemeIdentified: {targetUri}");
		}

		_coreWebView?.RaiseUnsupportedUriSchemeIdentified(targetUri, out handled);

		return handled;
	}

	internal void OnNavigationFinished(Uri? destinationUrl)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("OnNavigationFinished: {0}", destinationUrl);
		}

		if (_coreWebView is null || _isNavigationCompleted)
		{
			return;
		}

		_isNavigationCompleted = true;
		CheckForTitleChange();

		if (destinationUrl != null)
		{
			// Only update source and history for non-anchor navigations
			if (!IsAnchorNavigation(destinationUrl.AbsoluteUri))
			{
				_lastNavigationUrl = destinationUrl.AbsoluteUri;
				_coreWebView.Source = destinationUrl.AbsoluteUri;
			}
		}

		RaiseNavigationCompleted(destinationUrl, true, 200, CoreWebView2WebErrorStatus.Unknown);
		_lastNavigationData = destinationUrl;
		_isNavigationCompleted = false;

#if __IOS__ || __MACOS__
		// Inject the JavaScript interceptor for fetch/XMLHttpRequest header injection
		// This is done after navigation completes so the DOM is available
		InjectWebResourceInterceptor();
#endif
	}

	private bool IsAnchorNavigation(string url) => WebViewUtils.IsAnchorNavigation(_lastNavigationUrl, url);

	internal void OnAnchorNavigation(Uri uri)
	{
		// For anchor navigation, update source, raise history changed, and navigation completed
		// This matches Windows WebView2 behavior
		_lastNavigationData = uri;
		_lastNavigationUrl = uri.AbsoluteUri;
		_coreWebView!.Source = uri.AbsoluteUri;

		// Raise NavigationCompleted for anchor navigation to ensure proper event handling
		RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);

		_shouldQueueHistoryChange = true;
		QueueHistoryChange();
	}

	private WKWebView? OnCreateWebView(WKWebView owner, WKWebViewConfiguration configuration, WKNavigationAction? action, WKWindowFeatures windowFeatures)
	{
		Uri? target;

		if (action?.TargetFrame != null)
		{
			target = action?.TargetFrame.Request?.Url?.ToUri();
		}
		else
		{
			target = action?.Request.Url.ToUri();
		}

		var targetString = target?.ToString() ?? CoreWebView2.BlankUrl;
		var refererUri = action?.SourceFrame?.Request?.Url?.ToUri() ?? CoreWebView2.BlankUri;

		var handled = false;

		_coreWebView?.RaiseNewWindowRequested(
			targetString,
			refererUri,
			out handled);

		if (handled)
		{
			return null;
		}
		else
		{
			RaiseNavigationStarting(target!, out var cancel);

			if (!cancel)
			{
#if __APPLE_UIKIT__
				if (UIKit.UIApplication.SharedApplication.CanOpenUrl(target!))
#else
				if (target != null && NSWorkspace.SharedWorkspace.UrlForApplication(new NSUrl(target.AbsoluteUri)) != null)
#endif
				{
					OpenUrl(targetString!);
					RaiseNavigationCompleted(target!, true, 200, CoreWebView2WebErrorStatus.Unknown);
				}
				else
				{
					RaiseNavigationCompleted(target!, false, 400, CoreWebView2WebErrorStatus.Unknown);
				}
			}
		}

		return null;
	}

	private void OpenUrl(string url)
	{
		var nsUrl = new NSUrl(url);
		//Opens the specified URL, launching the app that's registered to handle the scheme.
#if __IOS__
		Task.Run(() => UIApplication.SharedApplication.OpenUrlAsync(nsUrl, new UIApplicationOpenUrlOptions()));
#else
		NSWorkspace.SharedWorkspace.OpenUrl(nsUrl);
#endif
	}

	private void OnDidClose(WKWebView obj)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("OnDidClose");
		}
	}

	private void OnRunJavaScriptAlertPanel(WKWebView webview, string message, WKFrameInfo frame, Action completionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("OnRunJavaScriptAlertPanel: {0}", message);
		}

		var controller = webview.FindViewController();

#if __APPLE_UIKIT__
		var alert = UIKit.UIAlertController.Create(string.Empty, message, UIKit.UIAlertControllerStyle.Alert);
		alert.AddAction(UIKit.UIAlertAction.Create(OkString, UIKit.UIAlertActionStyle.Default, null));
		controller?.PresentViewController(alert, true, null);
		completionHandler();
#else
		var alert = new NSAlert()
		{
			AlertStyle = NSAlertStyle.Informational,
			InformativeText = message
		};
		alert.RunModal();
		completionHandler();
#endif
	}

	private void OnRunJavaScriptConfirmPanel(WKWebView webview, string message, WKFrameInfo frame, Action<bool> completionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("OnRunJavaScriptConfirmPanel: {0}", message);
		}

		/*
		 *	Code taken from:
		 *  https://github.com/xamarin/recipes/pull/20/files
		 */
		var controller = webview.FindViewController();

#if __APPLE_UIKIT__
		var alert = UIKit.UIAlertController.Create(string.Empty, message, UIKit.UIAlertControllerStyle.Alert);
		alert.AddAction(UIKit.UIAlertAction.Create(OkString, UIKit.UIAlertActionStyle.Default,
			okAction => completionHandler(true)));

		alert.AddAction(UIKit.UIAlertAction.Create(CancelString, UIKit.UIAlertActionStyle.Cancel,
			cancelAction => completionHandler(false)));

		controller?.PresentViewController(alert, true, null);
#else
		var alert = new NSAlert()
		{
			AlertStyle = NSAlertStyle.Informational,
			InformativeText = message,
		};
		alert.AddButton(OkString);
		alert.AddButton(CancelString);
		alert.BeginSheetForResponse(webview.Window, (result) =>
		{
			var okButtonClicked = result == 1000;
			completionHandler(okButtonClicked);
		});
#endif
	}

	private void OnRunJavaScriptTextInputPanel(WKWebView webview, string prompt, string? defaultText, WKFrameInfo frame, Action<string> completionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnRunJavaScriptTextInputPanel: {prompt}, {defaultText}");
		}

#if __APPLE_UIKIT__
		var alert = UIKit.UIAlertController.Create(string.Empty, prompt, UIKit.UIAlertControllerStyle.Alert);
		UITextField? alertTextField = null;

		alert.AddTextField((textField) =>
		{
			textField.Placeholder = defaultText;
			alertTextField = textField;
		});

		alert.AddAction(UIKit.UIAlertAction.Create(OkString, UIKit.UIAlertActionStyle.Default,
			okAction => completionHandler(alertTextField?.Text ?? "")));

		alert.AddAction(UIKit.UIAlertAction.Create(CancelString, UIKit.UIAlertActionStyle.Cancel,
			cancelAction => completionHandler("")));

		var controller = webview.FindViewController();
		controller?.PresentViewController(alert, true, null);
#else
		var alert = new NSAlert()
		{
			AlertStyle = NSAlertStyle.Informational,
			InformativeText = prompt,
		};
		var textField = new NSTextField(new CGRect(0, 0, 300, 20))
		{
			PlaceholderString = defaultText,
		};
		alert.AccessoryView = textField;
		alert.AddButton(OkString);
		alert.AddButton(CancelString);
		alert.BeginSheetForResponse(webview.Window, (result) =>
		{
			var okButtonClicked = result == 1000;
			completionHandler(okButtonClicked ? textField.StringValue : "");
		});
#endif
	}

	/// <summary>
	/// Raises <see cref="WebView.NavigationStarting"/> to allow cancellation of navigation.
	/// </summary>
	/// <param name="stopLoadingOnCanceled">
	/// Whether <see cref="WKWebView.StopLoading"/> should be called if the user cancels the navigation.
	/// This parameter should be false when the <see cref="WKWebView"/> is not actually loading a request (like for anchors navigation).
	/// </param>
	/// <returns>True if the user cancelled the navigation, false otherwise.</returns>
	internal bool OnStarted(Uri? targetUrl, bool stopLoadingOnCanceled = true)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("OnStarted: {0}", targetUrl);
		}

		_isCancelling = false;

		RaiseNavigationStarting(targetUrl ?? _lastNavigationData!, out var cancel); //TODO:MZ: For HTML content

		if (cancel)
		{
			_isCancelling = true;
			if (stopLoadingOnCanceled)
			{
				StopLoading();
			}
		}

		return cancel;
	}

	private void RaiseNavigationStarting(object navigationData, out bool cancel)
	{
		cancel = false;

		if (navigationData is null)
		{
			// This ase should not happen when navigating normally using http requests.
			// This is to stop a scenario where the webview is initialized without having a source
			cancel = true;
			return;
		}

		if (navigationData is Uri uri)
		{
			// Handle email links (mailto:)
			if (uri != null && uri.Scheme.Equals(Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase))
			{
#if __APPLE_UIKIT__
				ParseUriAndLauchMailto(uri);
#else
				NSWorkspace.SharedWorkspace.OpenUrl(new NSUrl(uri.ToString()));
#endif
				cancel = true;
				return;
			}
			else
			{
				// Anchor navigation shouldn't trigger NavigationStarting event, but should still allow queuing history changes
				if (uri != null && IsAnchorNavigation(uri.AbsoluteUri))
				{
					_shouldQueueHistoryChange = true;
					QueueHistoryChange();

					cancel = true;
					return;
				}
			}
		}

		_coreWebView?.SetHistoryProperties(CanGoBack, CanGoForward);
		_coreWebView?.RaiseNavigationStarting(navigationData, out cancel);
	}

	private void RaiseNavigationCompleted(Uri? uri, bool isSuccess, int httpStatusCode, CoreWebView2WebErrorStatus errorStatus)
	{
		if (_coreWebView == null)
		{
			return;
		}

		_coreWebView.RaiseNavigationCompleted(uri, isSuccess, httpStatusCode, errorStatus, shouldSetSource: false);
		_coreWebView.SetHistoryProperties(CanGoBack, CanGoForward);
	}

	private void QueueHistoryChange()
	{
		if (!_isHistoryChangeQueued && _shouldQueueHistoryChange)
		{
			_isHistoryChangeQueued = true;
			_ = _coreWebView?.Owner.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, RaiseQueuedHistoryChange);
		}

		// Reset the flag to control the queuing of history changes
		_shouldQueueHistoryChange = false;
	}

	private void RaiseQueuedHistoryChange()
	{
		_coreWebView?.RaiseHistoryChanged();
		_isHistoryChangeQueued = false;
	}

#if __APPLE_UIKIT__

	private static readonly string[] _emptyStringArray = new[] { "" };

	private void ParseUriAndLauchMailto(Uri mailtoUri)
	{
		_ = Uno.UI.Dispatching.NativeDispatcher.Main.EnqueueCancellableOperation(
			async (ct) =>
			{
				try
				{
					var subject = "";
					var body = "";
					var cc = _emptyStringArray;
					var bcc = _emptyStringArray;

					var recipients = mailtoUri.AbsoluteUri.Split(':')[1].Split('?')[0].Split(',');
					var parameters = mailtoUri.Query.Split('?');

					parameters = parameters.Length > 1 ?
									parameters[1].Split('&') :
									Array.Empty<string>();

					foreach (string param in parameters)
					{
						var keyValue = param.Split('=');
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
	Task LaunchMailto(CancellationToken ct, string? subject = null, string? body = null, string[]? to = null, string[]? cc = null, string[]? bcc = null)
	{
#if !__MACCATALYST__  // catalyst https://github.com/xamarin/xamarin-macios/issues/13935
		if (!MFMailComposeViewController.CanSendMail)
		{
			return;
		}

		var mailController = new MFMailComposeViewController();

		mailController.SetToRecipients(to);
		mailController.SetSubject(subject!);
		mailController.SetMessageBody(body!, false);
		mailController.SetCcRecipients(cc);
		mailController.SetBccRecipients(bcc);

		var finished = new TaskCompletionSource<object>();
		var handler = new EventHandler<MFComposeResultEventArgs>((snd, args) => finished.TrySetResult(args));

		try
		{
			mailController.Finished += handler;
			UIApplication.SharedApplication.KeyWindow?.RootViewController?.PresentViewController(mailController, true, null);

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

	internal void OnError(WKWebView webView, WKNavigation navigation, NSError error)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
		{
			this.Log().ErrorFormat("Could not navigate to web page: {0}", error.LocalizedDescription);
		}

		CoreWebView2WebErrorStatus status = CoreWebView2WebErrorStatus.OperationCanceled;

		_errorMap.TryGetValue((NSUrlError)(int)error.Code, out status);

		// We use the _isCancelling flag because the NSError caused by the StopLoading() doesn't always translate to WebErrorStatus.OperationCanceled.
		if (status != CoreWebView2WebErrorStatus.OperationCanceled && !_isCancelling)
		{
			Uri? uri;
			//If the url which failed to load is available in the user info, use it because with the WKWebView the 
			//field webView.Url is equal to last successfully loaded URL and not to the failed URL
			var failedUrl = error.UserInfo.UnoGetValueOrDefault(new NSString("NSErrorFailingURLStringKey")) as NSString;
			if (failedUrl != null)
			{
				uri = new Uri(failedUrl);
			}
			else
			{
				uri = webView.Url?.ToUri() ?? (_coreWebView is not null ? new Uri(_coreWebView.Source) : null); // TODO:MZ: What if Source is invalid URI?
			}

			RaiseNavigationCompleted(uri, false, 0, status); // TODO:MZ: What HTTP Status code?
		}

		_isCancelling = false;
	}

	public override void DidChangeValue(string forKey)
	{
		base.DidChangeValue(forKey);

		if (_coreWebView is null)
		{
			return;
		}

		if (forKey.Equals(nameof(Title), StringComparison.OrdinalIgnoreCase))
		{
			CheckForTitleChange();
		}
		else if (
			forKey.Equals(nameof(CanGoBack), StringComparison.OrdinalIgnoreCase) ||
			forKey.Equals(nameof(CanGoForward), StringComparison.OrdinalIgnoreCase))
		{
			_coreWebView.SetHistoryProperties(CanGoBack, CanGoForward);
		}
		else if (forKey.Equals(nameof(Url), StringComparison.OrdinalIgnoreCase))
		{
			var currentUri = Url?.ToUri();
			if (currentUri != null)
			{
				if (!IsAnchorNavigation(currentUri.AbsoluteUri))
				{
					_lastNavigationUrl = currentUri.AbsoluteUri;
					QueueHistoryChange();
				}

				_coreWebView.Source = currentUri.ToString();
			}
		}
	}

	public override bool CanGoBack => base.CanGoBack && GetNearestValidHistoryItem(direction: -1) != null;

	public override bool CanGoForward => base.CanGoForward && GetNearestValidHistoryItem(direction: 1) != null;

	void INativeWebView.GoBack()
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"GoBack");
		}

		GoTo(GetNearestValidHistoryItem(direction: -1)!);
	}

	void INativeWebView.GoForward()
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"GoForward");
		}

		GoTo(GetNearestValidHistoryItem(direction: 1)!);
	}

	async Task<string?> INativeWebView.ExecuteScriptAsync(string script, CancellationToken token)
	{
		var executedScript = string.Format(CultureInfo.InvariantCulture, "javascript:{0}", script);

		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"ExecuteScriptAsync: {executedScript}");
		}

		var tcs = new TaskCompletionSource<string?>();

		using var _ = token.Register(() => tcs.TrySetCanceled());

		EvaluateJavaScript(executedScript, (result, error) =>
		{
			if (error != null)
			{
				tcs.TrySetException(new InvalidOperationException($"Failed to execute javascript {error.LocalizedDescription}, {error.LocalizedFailureReason}, {error.LocalizedRecoverySuggestion}"));
			}
			else
			{
				if (result is not null && NSJsonSerialization.IsValidJSONObject(result))
				{
					var serializedData = NSJsonSerialization.Serialize(result, default, out var serializationError);
					if (serializationError != null)
					{
						tcs.TrySetException(new InvalidOperationException($"Failed to serialize javascript result {serializationError.LocalizedDescription}, {serializationError.LocalizedFailureReason}, {serializationError.LocalizedRecoverySuggestion}"));
					}
					else
					{
						tcs.TrySetResult(NSString.FromData(serializedData, NSStringEncoding.UTF8)?.ToString());
					}
				}
				else if (result is NSString resultString)
				{
					// String needs to be wrapped in quotes to match Windows behavior
					var netString = resultString.ToString();
					tcs.TrySetResult($"\"{netString.Replace("\"", "\\\"")}\"");
				}
				else
				{
					tcs.TrySetResult(result?.ToString());
				}
			}
		});

		return await tcs.Task;
	}

	async Task<string?> INativeWebView.InvokeScriptAsync(string script, string[]? arguments, CancellationToken ct)
	{
		var argumentString = Microsoft.UI.Xaml.Controls.WebView.ConcatenateJavascriptArguments(arguments);
		var javascript = string.Format(CultureInfo.InvariantCulture, "javascript:{0}(\"{1}\")", script, argumentString);

		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"EvaluateJavascriptAsync: {javascript}");
		}

		var tcs = new TaskCompletionSource<string>();

		using (ct.Register(() => tcs.TrySetCanceled()))
		{
			EvaluateJavaScript(javascript, (result, error) =>
			{
				if (error != null)
				{
					tcs.TrySetException(new InvalidOperationException($"Failed to execute javascript {error.LocalizedDescription}, {error.LocalizedFailureReason}, {error.LocalizedRecoverySuggestion}"));
				}
				else
				{
					tcs.TrySetResult((result as NSString) ?? "");
				}
			});

			return await tcs.Task;
		}
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

	private void HandleFileNavigation(NSUrlRequest request)
	{
		var uri = request.Url.ToUri();

#if __APPLE_UIKIT__
		if (!UIKit.UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("Trying to load a local file using WKWebView on iOS < 9.0. Please make sure to use WKWebViewLocalSourceBehavior.");
			}

			LoadRequest(request);
			return;
		}
#endif
		var readAccessFolderPath = GetBestFolderPath(uri);

		Uri? readAccessUri;

		if (Uri.TryCreate("file://" + readAccessFolderPath, UriKind.Absolute, out readAccessUri))
		{
			try
			{
				// LoadFileUrl will always fail on physical devices if readAccessUri changes for the same WebView instance.
				LoadFileUrl(uri!, readAccessUri!);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
				{
					this.Log().Error($"Failed to load file URL [{uri}]: {ex.Message}");
				}

				// If LoadFileUrl fails, try alternative approaches
				try
				{
					// Try to read the file content and load as HTML string
					var filePath = uri.LocalPath;
					if (File.Exists(filePath))
					{
						var content = File.ReadAllText(filePath);
						if (uri != null)
						{
							LoadHtmlString(content, uri!);
						}

						_lastNavigationData = uri;
						return;
					}
				}
				catch (Exception readEx)
				{
					if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
					{
						this.Log().Error($"Failed to read file content [{uri}]: {readEx.Message}");
					}
				}

				RaiseNavigationCompleted(uri, false, 404, CoreWebView2WebErrorStatus.UnexpectedError);
			}
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
			{
				this.Log().Error($"The uri [{uri}] is invalid.");
			}

			RaiseNavigationCompleted(uri, false, 404, CoreWebView2WebErrorStatus.UnexpectedError);
		}
	}

	void INativeWebView.Reload() => Reload();

	void INativeWebView.SetScrollingEnabled(bool isScrollingEnabled)
	{
#if __APPLE_UIKIT__
		ScrollView.ScrollEnabled = isScrollingEnabled;
		ScrollView.Bounces = isScrollingEnabled;
#else
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"ScrollingEnabled is not currently supported on macOS");
		}
#endif
	}

	private WKBackForwardListItem? GetNearestValidHistoryItem(int direction)
	{
		var navList = direction == 1 ? BackForwardList.ForwardList : BackForwardList.BackList.Reverse();
		return navList?.FirstOrDefault(item => CoreWebView2.GetIsHistoryEntryValid(item.InitialUrl.AbsoluteString!));
	}

	private static string GetBestFolderPath(Uri fileUri)
	{
		// Here we go up in the folder hierarchy to support as many folders as possible
		// because navigating to a subsequent file in a different folder branch will fail.
		// To do that, we try to target the first folder after the app sandbox.

		var directFileParentPath = Path.GetDirectoryName(fileUri.LocalPath)!;
		if (string.IsNullOrEmpty(directFileParentPath))
		{
			// If the path is not valid, return the original path
			return directFileParentPath;
		}

		var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		var appRootPath = directFileParentPath.Substring(0, documentsPath.LastIndexOf('/'));

		if (directFileParentPath.StartsWith(appRootPath, StringComparison.Ordinal))
		{
			var relativePath = directFileParentPath.Substring(appRootPath.Length, directFileParentPath.Length - appRootPath.Length);
			var topFolder = relativePath.Split(separator: '/', options: StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

			if (topFolder != null)
			{
				var finalPath = Path.Combine(appRootPath, topFolder);
				return finalPath;
			}
			else
			{
				return directFileParentPath;
			}
		}
		else
		{
			return directFileParentPath;
		}
	}

	public void DidReceiveScriptMessage(WKUserContentController userContentController, WKScriptMessage message)
	{
		if (message.Name == WebMessageHandlerName)
		{
			_coreWebView?.RaiseWebMessageReceived((message.Body as NSString)?.ToString()!);
		}
	}

	private void CheckForTitleChange()
	{
		var currentTitle = Title;
		if (_previousTitle != currentTitle)
		{
			_previousTitle = currentTitle;
			_coreWebView!.OnDocumentTitleChanged();
		}
	}
}
