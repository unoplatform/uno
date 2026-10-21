#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebViews that can deliver host messages to the page's <c>chrome.webview</c> message listeners.
/// </summary>
internal interface ISupportsPostWebMessage
{
	void PostWebMessageAsJson(string webMessageAsJson);

	void PostWebMessageAsString(string webMessageAsString);
}
