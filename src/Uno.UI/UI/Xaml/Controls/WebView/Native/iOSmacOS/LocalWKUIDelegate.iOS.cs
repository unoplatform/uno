#nullable enable

using System;
using WebKit;
using Uno.Foundation.Logging;
using Uno.Extensions;

namespace Microsoft.UI.Xaml.Controls;

internal class LocalWKUIDelegate : WKUIDelegate
{
	private readonly Func<WKWebView, WKWebViewConfiguration, WKNavigationAction?, WKWindowFeatures, WKWebView?> _createWebView;
	private readonly Action<WKWebView, string, WKFrameInfo, Action> _runJavaScriptAlertPanel;
	private readonly Action<WKWebView, string, string?, WKFrameInfo, Action<string>> _runJavaScriptTextInputPanel;
	private readonly Action<WKWebView, string, WKFrameInfo, Action<bool>> _runJavaScriptConfirmPanel;
	private readonly Action<WKWebView> _didClose;

	public LocalWKUIDelegate(
		Func<WKWebView, WKWebViewConfiguration, WKNavigationAction?, WKWindowFeatures, WKWebView?> onCreateWebView,
		Action<WKWebView, string, WKFrameInfo, Action> onRunJavaScriptAlertPanel,
		Action<WKWebView, string, string?, WKFrameInfo, Action<string>> onRunJavaScriptTextInputPanel,
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

	public override WKWebView? CreateWebView(WKWebView webView, WKWebViewConfiguration configuration, WKNavigationAction navigationAction, WKWindowFeatures windowFeatures)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"CreateWebView: TargetRequest[{navigationAction?.TargetFrame?.Request?.Url?.ToUri()}] Request:[{navigationAction?.Request?.Url?.ToUri()}]");
		}

		return _createWebView?.Invoke(webView, configuration, navigationAction, windowFeatures);
	}

	public override void RunJavaScriptAlertPanel(WKWebView webView, string message, WKFrameInfo frame, Action completionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKUIDelegate.RunJavaScriptAlertPanel: {message}");
		}

		_runJavaScriptAlertPanel?.Invoke(webView, message, frame, completionHandler);
	}

#if NET10_0_OR_GREATER || MACCATALYST18_5_OR_GREATER || IOS18_5_OR_GREATER
	[Obsolete("It's not possible to call the completion handler with a null value using this method. Please see https://github.com/dotnet/macios/issues/15728 for a workaround.")]
#endif  // NET10_0_OR_GREATER || MACCATALYST18_5_OR_GREATER || IOS18_5_OR_GREATER
	public override void RunJavaScriptTextInputPanel(WKWebView webView, string prompt, string? defaultText, WKFrameInfo frame, Action<string> completionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKUIDelegate.RunJavaScriptTextInputPanel: {prompt} / {defaultText}");
		}

		_runJavaScriptTextInputPanel?.Invoke(webView, prompt, defaultText, frame, completionHandler);
	}

	public override void RunJavaScriptConfirmPanel(WKWebView webView, string message, WKFrameInfo frame, Action<bool> completionHandler)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKUIDelegate.RunJavaScriptConfirmPanel: {message}");
		}

		_runJavaScriptConfirmPanel?.Invoke(webView, message, frame, completionHandler);
	}

	public override void DidClose(WKWebView webView)
	{
		if (this.Log().IsEnabled(Uno.Foundation.Logging.LogLevel.Debug))
		{
			this.Log().Debug($"WKUIDelegate.DidClose");
		}

		_didClose?.Invoke(webView);
	}
}
