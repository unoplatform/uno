#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that can toggle JavaScript execution.
/// </summary>
internal interface ISupportsScriptEnabled
{
	bool IsScriptEnabled { get; set; }
}
