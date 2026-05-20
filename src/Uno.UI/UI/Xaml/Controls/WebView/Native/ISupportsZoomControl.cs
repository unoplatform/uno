#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that can enable or disable user-driven
/// zoom (Ctrl+/Ctrl- and pinch gestures).
/// </summary>
internal interface ISupportsZoomControl
{
	bool IsZoomControlEnabled { get; set; }
}
