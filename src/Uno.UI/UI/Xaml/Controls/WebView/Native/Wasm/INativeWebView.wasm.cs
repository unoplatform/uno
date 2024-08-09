using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Wrapper for a version-dependent native WASM WebView
/// </summary>
internal partial interface INativeWebView
{
	void SetOwner(CoreWebView2 xamlWebView);
}
