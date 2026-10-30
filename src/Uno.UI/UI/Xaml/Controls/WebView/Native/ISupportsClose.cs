#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// A native WebView that can release its browser when its WebView2 is closed or retemplated.
/// </summary>
internal interface ISupportsClose
{
	/// <summary>
	/// Releases the browser and removes it from the window. The native WebView is not used afterwards.
	/// </summary>
	void Close();
}
