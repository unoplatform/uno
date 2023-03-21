#nullable enable

#if __WASM__ || __MACOS__
#pragma warning disable CS0067, CS0414
#endif

using Microsoft.Web.WebView2.Core;
using Uno.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Controls;

#if __WASM__ || __SKIA__
[NotImplemented]
#endif
public partial class WebView : Control, IWebView
{
	/// <summary>
	/// Initializes a new instance of the WebView class.
	/// </summary>
	public WebView()
	{
		DefaultStyleKey = typeof(WebView);

		CoreWebView2 = new CoreWebView2(this);
	}

	internal CoreWebView2 CoreWebView2 { get; }

	protected override void OnApplyTemplate() => CoreWebView2.OnOwnerApplyTemplate();

	public void Navigate(global::System.Uri source) => CoreWebView2.Navigate(source.ToString());

	public void NavigateToString(string text) => CoreWebView2.NavigateToString(text);

	public void GoForward() => CoreWebView2.GoForward();

	public void GoBack() => CoreWebView2.GoBack();

	public void Refresh() => CoreWebView2.Reload();

	public void Stop() => CoreWebView2.Stop();
}
