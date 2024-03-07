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
	public async void ProcessNavigation(Uri uri)
	{
		this.SetAttribute("src", uri.ToString());
		await Task.Delay(10);
		_coreWebView.RaiseNavigationCompleted(uri, true, 200, CoreWebView2WebErrorStatus.Unknown);
	}

	public async void ProcessNavigation(string html)
	{
		this.SetAttribute("srcdoc", html);
		await Task.Delay(10);
		_coreWebView.RaiseNavigationCompleted(null, true, 200, CoreWebView2WebErrorStatus.Unknown);
	}

	public void ProcessNavigation(HttpRequestMessage httpRequestMessage)
	{

	}

	public void Reload()
	{
	}
	public void SetScrollingEnabled(bool isScrollingEnabled) { }
	public void Stop() { }
}
