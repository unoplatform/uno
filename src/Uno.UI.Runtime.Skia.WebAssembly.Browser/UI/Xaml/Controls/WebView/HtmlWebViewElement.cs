using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Runtime.Skia;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.WebAssembly.Browser.UI.Xaml.Controls.WebView;

internal class HtmlWebViewElement : INativeWebView
{
	public string DocumentTitle => throw new NotImplementedException();

	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token) => throw new NotImplementedException();
	public void GoBack() => throw new NotImplementedException();
	public void GoForward() => throw new NotImplementedException();
	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) => throw new NotImplementedException();
	public void ProcessNavigation(Uri uri) => throw new NotImplementedException();
	public void ProcessNavigation(string html) => throw new NotImplementedException();
	public void ProcessNavigation(HttpRequestMessage httpRequestMessage) => throw new NotImplementedException();
	public void Reload() => throw new NotImplementedException();
	public void SetScrollingEnabled(bool isScrollingEnabled) => throw new NotImplementedException();
	public void Stop() => throw new NotImplementedException();
}
