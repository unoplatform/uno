#nullable enable

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that can override the User-Agent string.
/// </summary>
internal interface ISupportsUserAgent
{
	string? UserAgent { get; set; }
}
