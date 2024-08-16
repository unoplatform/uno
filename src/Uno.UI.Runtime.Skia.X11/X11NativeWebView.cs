using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using SharpWebview;
using SharpWebview.Content;
using Uno.UI.Runtime.Skia;
using Uno.UI.Xaml.Controls;

namespace Uno.WinUI.Runtime.Skia.X11;

#pragma warning disable CS9113 // Parameter is unread.
internal class X11NativeWebViewProvider(CoreWebView2 coreWebView2) : INativeWebViewProvider
#pragma warning restore CS9113 // Parameter is unread.
{
	public INativeWebView CreateNativeWebView(ContentPresenter contentPresenter)
	{
		var asd = new X11NativeWebView(coreWebView2, contentPresenter);
		return null!;
	}
}

internal class X11NativeWebView : INativeWebView
{
	private Webview _nativeWebview;
	private readonly CoreWebView2 _coreWebView2;
	private readonly ContentPresenter _presenter;
	private readonly string _title = $"Uno WebView {Random.Shared.Next()}";

	public X11NativeWebView(CoreWebView2 coreWebView2, ContentPresenter presenter)
	{
		_coreWebView2 = coreWebView2;
		_presenter = presenter;

		_nativeWebview = null!; // to work around nullability;
		new Thread(() =>
		{
			_nativeWebview = new Webview();
			_nativeWebview.SetSize(0, 0, WebviewHint.None);
			_nativeWebview.SetTitle(_title);
			_nativeWebview.Run();
		}).Start();

		// This doesn't seem to be stalling for very long at all, so doing this on the UI thread is
		// not problematic.
		var window = IntPtr.Zero;
		var host = (X11XamlRootHost)X11Manager.XamlRootMap.GetHostForRoot(presenter.XamlRoot!)!;
		var display = host.RootX11Window.Display;
		using var lockDisposable = X11Helper.XLock(display);
		while (window == IntPtr.Zero)
		{
			_ = XLib.XQueryTree(display, host.RootX11Window.Window, out IntPtr root, out _, out var children, out _);
			_ = XLib.XFree(children);
			window = X11NativeElementHostingExtension.FindWindowByTitle(display, root, _title);

		}
		_presenter.Content = new X11NativeWindow(window);
	}

	~X11NativeWebView()
	{
		_nativeWebview.Dispose();
	}

	public void GoBack() => _nativeWebview.Evaluate("history.back();");
	public void GoForward() => _nativeWebview.Evaluate("history.forward();");
	public void Stop() => _nativeWebview.Evaluate("window.stop();");
	public void Reload() => _nativeWebview.Evaluate("window.location.reload();");
	public void ProcessNavigation(Uri uri) => _nativeWebview.Navigate(new UrlContent(uri.AbsoluteUri));
	public void ProcessNavigation(string html) => _nativeWebview.Navigate(new HtmlContent(html));
	public void ProcessNavigation(HttpRequestMessage httpRequestMessage) { }
	public Task<string?> ExecuteScriptAsync(string script, CancellationToken token) => Task.FromResult<string?>(null);
	public Task<string?> InvokeScriptAsync(string script, string[]? arguments, CancellationToken token) => Task.FromResult<string?>(null);
	public void SetScrollingEnabled(bool isScrollingEnabled) { }
}
