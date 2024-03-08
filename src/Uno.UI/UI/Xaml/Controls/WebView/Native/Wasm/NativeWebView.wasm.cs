using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Uno.Foundation;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

public class NativeWebView : FrameworkElement, INativeWebView
{
	private CoreWebView2 _coreWebView;

	public NativeWebView() : base("iframe")
	{
		this.HorizontalAlignment = HorizontalAlignment.Stretch;
		this.VerticalAlignment = VerticalAlignment.Stretch;

		this.RegisterEventHandler("load", OnNavigationCompleted, GenericEventHandlers.RaiseRoutedEventHandler);
	}

	private void OnNavigationCompleted(object sender, RoutedEventArgs e)
	{
		var uriString = this.GetAttribute("src");
		Uri uri = null;
		if (!string.IsNullOrEmpty(uriString))
		{
			uri = new Uri(uriString);
		}
		_coreWebView.RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);
	}

	public void SetOwner(CoreWebView2 coreWebView)
	{
		_coreWebView = coreWebView;
	}

	public Task<string> ExecuteScriptAsync(string script, CancellationToken token)
	{
		var scriptString = WebAssemblyRuntime.EscapeJs(script);
		return Task.FromResult(WebAssemblyRuntime.InvokeJS($"document.getElementById('{HtmlId}').contentWindow.eval(\"{scriptString}\")"));
	}

	public void GoBack() { }
	public void GoForward() { }
	public Task<string> InvokeScriptAsync(string script, string[] arguments, CancellationToken token) => Task.FromResult<string>("");

	private void ScheduleNavigationStarting(string url, Action loadAction)
	{
		_ = _coreWebView.Owner.Dispatcher.RunAsync(global::Windows.UI.Core.CoreDispatcherPriority.High, () =>
		{
			_coreWebView.RaiseNavigationStarting(url, out var cancel);

			if (!cancel)
			{
				loadAction?.Invoke();
			}
		});
	}

	public void ProcessNavigation(Uri uri)
	{
		var uriString = uri.OriginalString;
		ScheduleNavigationStarting(uriString, () => this.SetAttribute("src", uriString));
	}


	public void ProcessNavigation(string html)
	{
		ScheduleNavigationStarting(null, () => this.SetAttribute("srcdoc", html));
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{

	}

	public void Reload()
	{
		WebAssemblyRuntime.InvokeJS($"document.getElementById('{HtmlId}').contentWindow.location.reload()");
	}

	public void SetScrollingEnabled(bool isScrollingEnabled) { }
	public void Stop() { }
}
