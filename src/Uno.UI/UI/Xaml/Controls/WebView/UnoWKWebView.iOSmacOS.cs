using CoreGraphics;
using Foundation;
using System;
using System.Collections.Generic;
using System.Text;
using WebKit;
using System.Threading;
using System.Threading.Tasks;
using ObjCRuntime;
using Uno.Extensions;
using Uno.UI.Extensions;
using Uno.Logging;
using Uno.UI.Web;
using System.IO;
using System.Linq;
using Uno.UI.Services;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel.Resources;
using Uno.UI;
#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class UnoWKWebView : WKWebView, INativeWebView
#if __MACOS__
		,IHasSizeThatFits
#endif
	{
		private WebView _parentWebView;
		private bool _isCancelling;

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

			// Set strings with fallback to default English
			OkString = !string.IsNullOrEmpty(ok) ? ok : "OK"; 
			CancelString = !string.IsNullOrEmpty(cancel) ? cancel : "Cancel";

#if __IOS__
			if (UIDevice.CurrentDevice.CheckSystemVersion(10, 3))
			{
				_errorMap.Add(NSUrlError.FileOutsideSafeArea, WebErrorStatus.UnexpectedServerError);
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

		public void RegisterNavigationEvents(WebView xamlWebView)
		{
			_parentWebView = xamlWebView;

			this.Configuration.Preferences.JavaScriptCanOpenWindowsAutomatically = true;
			this.Configuration.Preferences.JavaScriptEnabled = true;

			NavigationDelegate = new WebViewNavigationDelegate(
				//Removed the OnStarted Here because it was calling OnStarted Event twice, On NavigationCommited and OnNavigationStarted
				onNavigationCommited: null,
				onNavigationFinished: OnNavigationFinished,
				onNavigationFailed: OnError,
				onProvisionalNavigationStarted: OnStarted,
				onProvisionalNavigationFailed: OnError,
				onUnsupportedUriSchemeIdentified: OnUnsupportedUriSchemeIdentified
			);

			UIDelegate = new LocalWKUIDelegate(
				onCreateWebView: OnCreateWebView,
				didClose: OnDidClose,
				onRunJavaScriptAlertPanel: OnRunJavaScriptAlertPanel,
				onRunJavaScriptConfirmPanel: OnRunJavaScriptConfirmPanel,
				onRunJavaScriptTextInputPanel: OnRunJavaScriptTextInputPanel
			);
		}

		private bool OnUnsupportedUriSchemeIdentified(Uri targetUri)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"OnUnsupportedUriSchemeIdentified: {targetUri}");
			}

			var args = new WebViewUnsupportedUriSchemeIdentifiedEventArgs(targetUri);

			_parentWebView.OnUnsupportedUriSchemeIdentified(args);

			return args.Handled;
		}

		/// <summary>
		/// Url of the last navigation ; is null if the last web page was displayed by other means,
		/// such as raw HTML
		/// </summary>
		private NSUrl _urlLastNavigation;

		private void OnNavigationFinished(WKWebView webView, WKNavigation navigation)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("OnNavigationFinished: {0}", webView.Url?.ToUri());
			}

			_parentWebView.DocumentTitle = webView.Title;
			_parentWebView.OnComplete(webView.Url, isSuccessful: true, status: WebErrorStatus.Unknown);
			_urlLastNavigation = webView.Url;
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

			var args = new WebViewNewWindowRequestedEventArgs(
				referrer: action.SourceFrame?.Request?.Url?.ToUri(),
				uri: target
			);

			_parentWebView.OnNewWindowRequested(args);

			if (args.Handled)
			{
				return null;
			}
			else
			{
				var navigationArgs = new WebViewNavigationStartingEventArgs()
				{
					Cancel = false,
					Uri = target
				};

				_parentWebView.OnNavigationStarting(navigationArgs);

				if (!navigationArgs.Cancel)
				{
#if __IOS__
					if (UIKit.UIApplication.SharedApplication.CanOpenUrl(target))
					{
						UIKit.UIApplication.SharedApplication.OpenUrl(target);
						_parentWebView.OnComplete(target, isSuccessful: true, status: WebErrorStatus.Unknown);
					}
#else
					if (target != null && NSWorkspace.SharedWorkspace.UrlForApplication(new NSUrl(target.AbsoluteUri)) != null)
					{
						NSWorkspace.SharedWorkspace.OpenUrl(target);
						_parentWebView.OnComplete(target, isSuccessful: true, status: WebErrorStatus.Unknown);
					}
#endif
					else
					{
						_parentWebView.OnNavigationFailed(new WebViewNavigationFailedEventArgs()
						{
							Uri = target,
							WebErrorStatus = WebErrorStatus.Unknown
						});
					}
				}
			}

			return null;
		}

		private void OnDidClose(WKWebView obj)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("OnDidClose");
			}
		}

		private void OnRunJavaScriptAlertPanel(WKWebView webview, string message, WKFrameInfo frame, Action completionHandler)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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

		private void OnStarted(WKWebView webView, WKNavigation navigation)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().DebugFormat("OnStarted: {0}", webView.Url.ToUri());
			}

			_isCancelling = false;

			var args = new WebViewNavigationStartingEventArgs()
			{
				Cancel = false,
				Uri = webView.Url.ToUri() ?? _parentWebView.Source
			};

			_parentWebView.OnNavigationStarting(args);

			if (args.Cancel)
			{
				_isCancelling = true;
				StopLoading();
			}
		}

		private void OnError(WKWebView webView, WKNavigation navigation, NSError error)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				this.Log().ErrorFormat("Could not navigate to web page: {0}", error.LocalizedDescription);
			}

			WebErrorStatus status = WebErrorStatus.OperationCanceled;

			_errorMap.TryGetValue((NSUrlError)(int)error.Code, out status);

			// We use the _isCancelling flag because the NSError caused by the StopLoading() doesn't always translate to WebErrorStatus.OperationCanceled.
			if (status != WebErrorStatus.OperationCanceled && !_isCancelling)
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
					uri = webView.Url?.ToUri() ?? _parentWebView.Source;
				}

				_parentWebView.OnNavigationFailed(new WebViewNavigationFailedEventArgs()
				{
					Uri = uri,
					WebErrorStatus = status
				});

				_parentWebView.OnComplete(uri, false, status);
			}

			_isCancelling = false;
		}

		public override bool CanGoBack => base.CanGoBack && GetNearestValidHistoryItem(direction: -1) != null;

		public override bool CanGoForward => base.CanGoForward && GetNearestValidHistoryItem(direction: 1) != null;

		void INativeWebView.GoBack()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"GoBack");
			}

			GoTo(GetNearestValidHistoryItem(direction: -1));
		}

		void INativeWebView.GoForward()
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"GoForward");
			}

			GoTo(GetNearestValidHistoryItem(direction: 1));
		}

		public async Task<string> EvaluateJavascriptAsync(CancellationToken ct, string javascript)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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
						tcs.TrySetResult(result as NSString);
					}
				});

				return await tcs.Task;
			}
		}

		void INativeWebView.LoadRequest(NSUrlRequest request)
		{
			if (request == null)
			{
				throw new ArgumentNullException(nameof(request));
			}

			var uri = request.Url?.ToUri();

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
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

			// If we navigate to the exact same page but with a different location, the native control will not notify us of
			// any navigation. We need to create this notification to indicate that the navigation worked (only a scroll in the page)
			var currentUrlParts = _urlLastNavigation?.AbsoluteUrl?.ToString().Split(new string[] { "#" }, StringSplitOptions.None);
			var newUrlParts = uri?.AbsoluteUri?.ToString().Split(new string[] { "#" }, StringSplitOptions.None);

			if (currentUrlParts?.Length > 0
				&& newUrlParts?.Length > 0
				&& newUrlParts.Length > 1
				&& currentUrlParts[0].Equals(newUrlParts[0]))
			{
				OnNavigationFinished(this, null);
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

				base.LoadRequest(request);
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
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
				{
					this.Log().Error($"The uri [{uri}] is invalid.");
				}

				_parentWebView.OnNavigationFailed(new WebViewNavigationFailedEventArgs()
				{
					Uri = uri,
					WebErrorStatus = WebErrorStatus.UnexpectedClientError
				});
			}
		}

		void INativeWebView.LoadHtmlString(string s, NSUrl baseUrl)
		{
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"LoadHtmlString: {s}");
			}

			LoadHtmlString(s, baseUrl);

			_urlLastNavigation = null;
		}

		void INativeWebView.SetScrollingEnabled(bool isScrollingEnabled)
		{
#if __IOS__
			ScrollView.ScrollEnabled = isScrollingEnabled;
			ScrollView.Bounces = isScrollingEnabled;
#else
			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"ScrollingEnabled is not currently supported on macOS");
			}
#endif
		}

		void INativeWebView.Reload()
		{
			Reload();
		}

		private WKBackForwardListItem GetNearestValidHistoryItem(int direction)
		{
			var navList = direction == 1 ? BackForwardList.ForwardList : BackForwardList.BackList.Reverse();
			return navList.FirstOrDefault(item => _parentWebView.GetIsHistoryEntryValid(item.InitialUrl.AbsoluteString));
		}

		private static string GetBestFolderPath(Uri fileUri)
		{
			// Here we go up in the folder hierarchy to support as many folders as possible
			// because navigating to a subsequent file in a different folder branch will fail.
			// To do that, we try to target the first folder after the app sandbox.

			var directFileParentPath = Path.GetDirectoryName(fileUri.LocalPath);
			var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			var appRootPath = directFileParentPath.Substring(0, documentsPath.LastIndexOf('/'));

			if (directFileParentPath.StartsWith(appRootPath))
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

		private class LocalWKUIDelegate : WKUIDelegate
		{
			private readonly Func<WKWebView, WKWebViewConfiguration, WKNavigationAction, WKWindowFeatures, WKWebView> _createWebView;
			private readonly Action<WKWebView, string, WKFrameInfo, Action> _runJavaScriptAlertPanel;
			private readonly Action<WKWebView, string, string, WKFrameInfo, Action<string>> _runJavaScriptTextInputPanel;
			private readonly Action<WKWebView, string, WKFrameInfo, Action<bool>> _runJavaScriptConfirmPanel;
			private readonly Action<WKWebView> _didClose;

			public LocalWKUIDelegate(
				Func<WKWebView, WKWebViewConfiguration, WKNavigationAction, WKWindowFeatures, WKWebView> onCreateWebView,
				Action<WKWebView, string, WKFrameInfo, Action> onRunJavaScriptAlertPanel,
				Action<WKWebView, string, string, WKFrameInfo, Action<string>> onRunJavaScriptTextInputPanel,
				Action<WKWebView, string, WKFrameInfo, Action<bool>> onRunJavaScriptConfirmPanel,
				Action<WKWebView> didClose
			)
			{
				_createWebView = onCreateWebView;
				_runJavaScriptAlertPanel = onRunJavaScriptAlertPanel;
				_runJavaScriptTextInputPanel = onRunJavaScriptTextInputPanel;
				_runJavaScriptConfirmPanel = onRunJavaScriptConfirmPanel;
				_didClose = didClose;
			}

			public override WKWebView CreateWebView(WKWebView webView, WKWebViewConfiguration configuration, WKNavigationAction navigationAction, WKWindowFeatures windowFeatures)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"CreateWebView: TargetRequest[{navigationAction?.TargetFrame?.Request?.Url?.ToUri()}] Request:[{navigationAction.Request?.Url?.ToUri()}]");
				}

				return _createWebView?.Invoke(webView, configuration, navigationAction, windowFeatures);
			}

			public override void RunJavaScriptAlertPanel(WKWebView webView, string message, WKFrameInfo frame, Action completionHandler)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKUIDelegate.RunJavaScriptAlertPanel: {message}");
				}

				_runJavaScriptAlertPanel?.Invoke(webView, message, frame, completionHandler);
			}

			public override void RunJavaScriptTextInputPanel(WKWebView webView, string prompt, string defaultText, WKFrameInfo frame, Action<string> completionHandler)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKUIDelegate.RunJavaScriptTextInputPanel: {prompt} / {defaultText}");
				}

				_runJavaScriptTextInputPanel?.Invoke(webView, prompt, defaultText, frame, completionHandler);
			}

			public override void RunJavaScriptConfirmPanel(WKWebView webView, string message, WKFrameInfo frame, Action<bool> completionHandler)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKUIDelegate.RunJavaScriptConfirmPanel: {message}");
				}

				_runJavaScriptConfirmPanel?.Invoke(webView, message, frame, completionHandler);
			}

			public override void DidClose(WKWebView webView)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKUIDelegate.DidClose");
				}

				_didClose?.Invoke(webView);
			}
		}


		private class WebViewNavigationDelegate : WKNavigationDelegate
		{
			private readonly Action<WKWebView, WKNavigation> _onNavigationCommited;
			private readonly Action<WKWebView, WKNavigation> _onNavigationFinished;
			private readonly Action<WKWebView, WKNavigation, NSError> _onNavigationFailed;
			private readonly Action<WKWebView, WKNavigation> _onProvisionalNavigationStarted;
			private readonly Action<WKWebView, WKNavigation, NSError> _onProvisionalNavigationFailed;
			private readonly Func<Uri, bool> _onUnsupportedUriSchemeIdentified;

			public WebViewNavigationDelegate(
				Action<WKWebView, WKNavigation> onNavigationCommited,
				Action<WKWebView, WKNavigation> onNavigationFinished,
				Action<WKWebView, WKNavigation, NSError> onNavigationFailed,
				Action<WKWebView, WKNavigation> onProvisionalNavigationStarted,
				Action<WKWebView, WKNavigation, NSError> onProvisionalNavigationFailed,
				Func<Uri, bool> onUnsupportedUriSchemeIdentified
			)
			{
				_onNavigationCommited = onNavigationCommited;
				_onNavigationFinished = onNavigationFinished;
				_onNavigationFailed = onNavigationFailed;
				_onProvisionalNavigationStarted = onProvisionalNavigationStarted;
				_onProvisionalNavigationFailed = onProvisionalNavigationFailed;
				_onUnsupportedUriSchemeIdentified = onUnsupportedUriSchemeIdentified;
			}

			public override void DecidePolicy(WKWebView webView, WKNavigationAction navigationAction, Action<WKNavigationActionPolicy> decisionHandler)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKNavigationDelegate.DecidePolicy: Request:{navigationAction.Request?.Url.ToUri()} TargetRequest: {navigationAction.TargetFrame?.Request}");
				}

				var scheme = navigationAction.Request.Url.Scheme.ToLower();

				if (scheme != "http" && scheme != "https")
				{
					bool cancelled = _onUnsupportedUriSchemeIdentified?.Invoke(navigationAction.Request.Url.ToUri()) ?? false;

					decisionHandler(cancelled ? WKNavigationActionPolicy.Cancel : WKNavigationActionPolicy.Allow);
				}
				else
				{
					decisionHandler(WKNavigationActionPolicy.Allow);
				}
			}

			public override void DecidePolicy(WKWebView webView, WKNavigationResponse navigationResponse, Action<WKNavigationResponsePolicy> decisionHandler)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKNavigationDelegate.DecidePolicy {navigationResponse.Response?.Url?.ToUri()}");
				}

				decisionHandler(WKNavigationResponsePolicy.Allow);
			}

			public override void DidReceiveServerRedirectForProvisionalNavigation(WKWebView webView, WKNavigation navigation)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKNavigationDelegate.DidReceiveServerRedirectForProvisionalNavigation: Request:{webView.Url?.ToUri()}");
				}

				_onProvisionalNavigationStarted?.Invoke(webView, navigation);
			}

			public override void ContentProcessDidTerminate(WKWebView webView)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKNavigationDelegate.ContentProcessDidTerminate: Request:{webView.Url?.ToUri()}");
				}
			}

			public override void DidCommitNavigation(WKWebView webView, WKNavigation navigation)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKNavigationDelegate.DidCommitNavigation: Request:{webView.Url?.ToUri()}");
				}

				_onNavigationCommited?.Invoke(webView, navigation);
			}

			public override void DidFinishNavigation(WKWebView webView, WKNavigation navigation)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKNavigationDelegate.DidFinishNavigation: Request:{webView.Url?.ToUri()}");
				}

				_onNavigationFinished?.Invoke(webView, navigation);
			}

			public override void DidFailNavigation(WKWebView webView, WKNavigation navigation, NSError error)
			{
				if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
				{
					this.Log().Debug($"WKNavigationDelegate.DidCommitNavigation: Request:{webView.Url?.ToUri()}");
				}

				_onNavigationFailed?.Invoke(webView, navigation, error);
			}
			public override void DidStartProvisionalNavigation(WKWebView webView, WKNavigation navigation)
			{
				_onProvisionalNavigationStarted?.Invoke(webView, navigation);
			}

			public override void DidFailProvisionalNavigation(WKWebView webView, WKNavigation navigation, NSError error)
			{
				_onProvisionalNavigationFailed?.Invoke(webView, navigation, error);
			}
		}
	}
}
