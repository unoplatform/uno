#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that have a first-class host-to-page
/// message channel (e.g. CoreWebView2.PostWebMessageAsJson on Win32). When a
/// platform does not implement this interface, CoreWebView2 falls back to a
/// JavaScript-based polyfill via ExecuteScriptAsync.
/// </summary>
internal interface ISupportsPostWebMessage
{
	void PostWebMessageAsJson(string json);

	void PostWebMessageAsString(string message);
}
