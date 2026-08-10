namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Wrapper for a version-dependent native iOS WebView
/// </summary>
internal partial interface INativeWebView
{
	void SetOwner(object coreWebView2);
}
