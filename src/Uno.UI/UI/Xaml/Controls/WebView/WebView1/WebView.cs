#nullable enable

using System;
using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.Foundation;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Controls;

#if IS_UNIT_TESTS || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__
[Uno.NotImplemented("IS_UNIT_TESTS", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__")]
#endif
public partial class WebView : Control, IWebView
{
	private bool _sourceChangeFromCore;

	/// <summary>
	/// Initializes a new instance of the WebView class.
	/// </summary>
	public WebView()
	{
		DefaultStyleKey = typeof(WebView);

		CoreWebView2 = new CoreWebView2(this);
		CoreWebView2.DocumentTitleChanged += CoreWebView2_DocumentTitleChanged;
		CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
		CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
		CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
		CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
		CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
		CoreWebView2.UnsupportedUriSchemeIdentified += CoreWebView2_UnsupportedUriSchemeIdentified;
	}

	internal CoreWebView2 CoreWebView2 { get; }

	bool IWebView.IsLoaded => IsLoaded;

	bool IWebView.SwitchSourceBeforeNavigating => true;

	CoreDispatcher IWebView.Dispatcher => Dispatcher;

	protected override void OnApplyTemplate() => CoreWebView2.OnOwnerApplyTemplate();

	public void Navigate(global::System.Uri source) => CoreWebView2.Navigate(source.ToString());

	public void NavigateToString(string text) => CoreWebView2.NavigateToString(text);

	public void GoForward() => CoreWebView2.GoForward();

	public void GoBack() => CoreWebView2.GoBack();

	public void Refresh() => CoreWebView2.Reload();

	public void Stop() => CoreWebView2.Stop();

	public IAsyncOperation<string?> InvokeScriptAsync(string scriptName, IEnumerable<string> arguments) =>
		AsyncOperation.FromTask(ct => CoreWebView2.InvokeScriptAsync(scriptName, arguments?.ToArray(), ct));

	public void NavigateWithHttpRequestMessage(global::Windows.Web.Http.HttpRequestMessage requestMessage) =>
		CoreWebView2.NavigateWithHttpRequestMessage(requestMessage);

	internal static string ConcatenateJavascriptArguments(string[]? arguments)
	{
		var argument = string.Empty;
		if (arguments != null && arguments.Length > 0)
		{
			argument = string.Join(",", arguments);
		}

		return argument;
	}

	private void CoreWebView2_DocumentTitleChanged(CoreWebView2 sender, object args) =>
		DocumentTitle = sender.DocumentTitle;

	private void CoreWebView2_HistoryChanged(CoreWebView2 sender, object args) =>
		(CanGoBack, CanGoForward) = (sender.CanGoBack, sender.CanGoForward);

	private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
	{
		var webViewArgs = args.ToWebViewArgs();
		NavigationStarting?.Invoke(this, webViewArgs);
		args.Cancel = webViewArgs.Cancel;
	}

	private void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
	{
		NavigationCompleted?.Invoke(this, args.ToWebViewArgs());
		if (!args.IsSuccess)
		{
			NavigationFailed?.Invoke(this, new WebViewNavigationFailedEventArgs(args.Uri, args.WebErrorStatus.ToWebErrorStatus()));
		}
	}

	private void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
	{
		var webViewArgs = args.ToWebViewArgs();
		NewWindowRequested?.Invoke(this, webViewArgs);
		args.Handled = webViewArgs.Handled;
	}

	private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
	{
		_sourceChangeFromCore = true;
		if (sender.Source == CoreWebView2.BlankUrl)
		{
			Source = null;
		}
		else
		{
			Source = Uri.TryCreate(sender.Source, UriKind.Absolute, out var uri) ? uri : CoreWebView2.BlankUri;
		}
		_sourceChangeFromCore = false;
	}

	private void CoreWebView2_UnsupportedUriSchemeIdentified(CoreWebView2 sender, WebViewUnsupportedUriSchemeIdentifiedEventArgs args) =>
		UnsupportedUriSchemeIdentified?.Invoke(this, args);
}
