using CoreGraphics;
using Foundation;
using System;
using WebKit;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Web;
using System.IO;
using System.Linq;
using Windows.ApplicationModel.Resources;
using Uno.UI.Xaml.Controls;
using System.Net.Http;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Extensions;
using Windows.Foundation;
using System.Collections.Generic;
using System.Globalization;
using System.Net;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls;

public partial class UnoWKWebView : WKWebView, INativeWebView, IWKScriptMessageHandler
#if __MACOS__
	,IHasSizeThatFits
#endif
{
	private CoreWebView2 _coreWebView;
	private bool _isCancelling;

	private const string WebMessageHandlerName = "unoWebView";
	private const string OkResourceKey = "WebView_Ok";
	private const string CancelResourceKey = "WebView_Cancel";

	private readonly string OkString;
	private readonly string CancelString;

	public UnoWKWebView() : base(CGRect.Empty, new WebKit.WKWebViewConfiguration())
	{
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

		Configuration.UserContentController.AddScriptMessageHandler(this, WebMessageHandlerName);

		// Set strings with fallback to default English
		OkString = !string.IsNullOrEmpty(ok) ? ok : "OK";
		CancelString = !string.IsNullOrEmpty(cancel) ? cancel : "Cancel";

#if __IOS__
		if (UIDevice.CurrentDevice.CheckSystemVersion(10, 3))
		{
			_errorMap.Add(NSUrlError.FileOutsideSafeArea, CoreWebView2WebErrorStatus.UnexpectedError);
		}
#endif
	}

#if __MACOS__
	public CGSize SizeThatFits(CGSize availableSize)
	{
		var height = Math.Min(availableSize.Height, FittingSize.Height);
		var width = Math.Min(availableSize.Width, FittingSize.Width);
		return new CGSize(width, height);
	} 
#endif


	public void Stop() => StopLoading();

	void INativeWebView.ProcessNavigation(HttpRequestMessage requestMessage)
	{
		if (requestMessage == null)
		{
			this.Log().Warn("HttpRequestMessage is null. Please make sure the http request is complete.");
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

	void INativeWebView.ProcessNavigation(string html)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"LoadHtmlString: {html}");
		}

		LoadHtmlString(html, null);

		_lastNavigationData = html;
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

	void INativeWebView.SetOwner(CoreWebView2 coreWebView)
	{
		_coreWebView = coreWebView;

		this.Configuration.Preferences.JavaScriptCanOpenWindowsAutomatically = true;
		this.Configuration.Preferences.JavaScriptEnabled = true;

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
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnUnsupportedUriSchemeIdentified: {targetUri}");
		}

		var args = new WebViewUnsupportedUriSchemeIdentifiedEventArgs(targetUri);

		// TODO:MZ:
		//_coreWebView.RaiseUnsupportedUriSchemeIdentified(args);

		return args.Handled;
	}

	/// <summary>
	/// Url of the last navigation ; is null if the last web page was displayed by other means,
	/// such as raw HTML
	/// </summary>
	internal object _lastNavigationData;

	internal void OnNavigationFinished(Uri destinationUrl)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("OnNavigationFinished: {0}", destinationUrl);
		}

		_coreWebView.DocumentTitle = Title;
		_coreWebView.RaiseNavigationCompleted(destinationUrl, true, 200, CoreWebView2WebErrorStatus.Unknown);
		_lastNavigationData = destinationUrl;
	}

	private WKWebView OnCreateWebView(WKWebView owner, WKWebViewConfiguration configuration, WKNavigationAction action, WKWindowFeatures windowFeatures)
	{
		Uri target;

		if (action?.TargetFrame != null)
		{
			target = action?.TargetFrame.Request?.Url?.ToUri();
		}
		else
		{
			target = action.Request.Url.ToUri();
		}
		var targetString = target?.ToString();

		_coreWebView.RaiseNewWindowRequested(targetString, action.SourceFrame?.Request?.Url?.ToUri(), out var handled);

		if (handled)
		{
			return null;
		}
		else
		{
			_coreWebView.RaiseNavigationStarting(targetString, out var cancel);

			if (!cancel)
			{
#if __IOS__
				if (UIKit.UIApplication.SharedApplication.CanOpenUrl(target))
				{
					UIKit.UIApplication.SharedApplication.OpenUrl(target);

					_coreWebView.RaiseNavigationCompleted(target, true, 200, CoreWebView2WebErrorStatus.Unknown);
				}
#else
				if (target != null && NSWorkspace.SharedWorkspace.UrlForApplication(new NSUrl(target.AbsoluteUri)) != null)
				{
					NSWorkspace.SharedWorkspace.OpenUrl(target);
					_coreWebView.RaiseNavigationCompleted(target, true, 200, CoreWebView2WebErrorStatus.Unknown);
				}
#endif
				else
				{
					_coreWebView.RaiseNavigationCompleted(target, false, 400, CoreWebView2WebErrorStatus.Unknown);
				}
			}
		}

		return null;
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

#if __IOS__
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

#if __IOS__
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
		alert.BeginSheetForResponse(webview.Window, (result) => {
			var okButtonClicked = result == 1000;
			completionHandler(okButtonClicked);
		});
#endif
	}

	private void OnRunJavaScriptTextInputPanel(WKWebView webview, string prompt, string defaultText, WKFrameInfo frame, Action<string> completionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"OnRunJavaScriptTextInputPanel: {prompt}, {defaultText}");
		}

#if __IOS__
		var alert = UIKit.UIAlertController.Create(string.Empty, prompt, UIKit.UIAlertControllerStyle.Alert);
		UITextField alertTextField = null;

		alert.AddTextField((textField) =>
		{
			textField.Placeholder = defaultText;
			alertTextField = textField;
		});

		alert.AddAction(UIKit.UIAlertAction.Create(OkString, UIKit.UIAlertActionStyle.Default,
			okAction => completionHandler(alertTextField.Text)));

		alert.AddAction(UIKit.UIAlertAction.Create(CancelString, UIKit.UIAlertActionStyle.Cancel,
			cancelAction => completionHandler(null)));

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
		alert.BeginSheetForResponse(webview.Window, (result) => {
			var okButtonClicked = result == 1000;
			completionHandler(okButtonClicked ? textField.StringValue : null);
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
	internal bool OnStarted(Uri targetUrl, bool stopLoadingOnCanceled = true)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().DebugFormat("OnStarted: {0}", targetUrl);
		}

		_isCancelling = false;

		_coreWebView.RaiseNavigationStarting(CoreWebView2.BlankUrl, out var cancel); //TODO:MZ: For HTML content 		var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(htmlContent);
																					 //var base64String = System.Convert.ToBase64String(plainTextBytes);
																					 //var htmlUri = new Uri(string.Format(CultureInfo.InvariantCulture, DataUriFormatString, base64String));

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
			Uri uri;
			//If the url which failed to load is available in the user info, use it because with the WKWebView the 
			//field webView.Url is equal to last successfully loaded URL and not to the failed URL
			var failedUrl = error.UserInfo.UnoGetValueOrDefault(new NSString("NSErrorFailingURLStringKey")) as NSString;
			if (failedUrl != null)
			{
				uri = new Uri(failedUrl);
			}
			else
			{
				uri = webView.Url?.ToUri() ?? new Uri(_coreWebView.Source); // TODO:MZ: What if Source is invalid URI?
			}

			_coreWebView.RaiseNavigationCompleted(uri, false, 0, status); // TODO:MZ: What HTTP Status code?
		}

		_isCancelling = false;
	}

	public override bool CanGoBack => base.CanGoBack && GetNearestValidHistoryItem(direction: -1) != null;

	public override bool CanGoForward => base.CanGoForward && GetNearestValidHistoryItem(direction: 1) != null;

	void INativeWebView.GoBack()
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"GoBack");
		}

		GoTo(GetNearestValidHistoryItem(direction: -1));
	}

	void INativeWebView.GoForward()
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"GoForward");
		}

		GoTo(GetNearestValidHistoryItem(direction: 1));
	}

	async Task<string> INativeWebView.ExecuteScriptAsync(string script, CancellationToken token)
	{
		var executedScript = string.Format(CultureInfo.InvariantCulture, "javascript:{0}", script);

		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"ExecuteScriptAsync: {executedScript}");
		}

		var tcs = new TaskCompletionSource<string>();

		using var _ = token.Register(() => tcs.TrySetCanceled());

		EvaluateJavaScript(executedScript, (result, error) =>
		{
			if (error != null)
			{
				tcs.TrySetException(new InvalidOperationException($"Failed to execute javascript {error.LocalizedDescription}, {error.LocalizedFailureReason}, {error.LocalizedRecoverySuggestion}"));
			}
			else
			{
				tcs.TrySetResult(result as NSString);
			}
		});

		return await tcs.Task;
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

#if __IOS__
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

		Uri readAccessUri;

		if (Uri.TryCreate("file://" + readAccessFolderPath, UriKind.Absolute, out readAccessUri))
		{
			// LoadFileUrl will always fail on physical devices if readAccessUri changes for the same WebView instance.
			LoadFileUrl(uri, readAccessUri);
		}
		else
		{
			if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Error))
			{
				this.Log().Error($"The uri [{uri}] is invalid.");
			}

			_coreWebView.RaiseNavigationCompleted(uri, false, 404, CoreWebView2WebErrorStatus.UnexpectedError);
		}
	}

	void INativeWebView.SetScrollingEnabled(bool isScrollingEnabled)
	{
#if __IOS__
		ScrollView.ScrollEnabled = isScrollingEnabled;
		ScrollView.Bounces = isScrollingEnabled;
#else
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"ScrollingEnabled is not currently supported on macOS");
		}
#endif
	}

	void INativeWebView.Reload() => Reload();

	private WKBackForwardListItem GetNearestValidHistoryItem(int direction)
	{
		var navList = direction == 1 ? BackForwardList.ForwardList : BackForwardList.BackList.Reverse();
		return navList.FirstOrDefault(item => CoreWebView2.GetIsHistoryEntryValid(item.InitialUrl.AbsoluteString));
	}

	private static string GetBestFolderPath(Uri fileUri)
	{
		// Here we go up in the folder hierarchy to support as many folders as possible
		// because navigating to a subsequent file in a different folder branch will fail.
		// To do that, we try to target the first folder after the app sandbox.

		var directFileParentPath = Path.GetDirectoryName(fileUri.LocalPath);
		var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
		var appRootPath = directFileParentPath.Substring(0, documentsPath.LastIndexOf('/'));

		if (directFileParentPath.StartsWith(appRootPath, StringComparison.Ordinal))
		{
			var relativePath = directFileParentPath.Substring(appRootPath.Length, directFileParentPath.Length - appRootPath.Length);
			var topFolder = relativePath.Split(separator: new char[] { '/' }, options: StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

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
			_coreWebView.RaiseWebMessageReceived((message.Body as NSString)?.ToString());
		}
	}
}
