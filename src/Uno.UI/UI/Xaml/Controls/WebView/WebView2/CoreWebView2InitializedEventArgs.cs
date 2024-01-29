#nullable enable

using System;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides data for the CoreWebView2Initialized event.
/// </summary>
public sealed partial class CoreWebView2InitializedEventArgs
{
	/// <summary>
	/// Gets the exception raised when a WebView2 is created.
	/// </summary>
	public Exception? Exception { get; }
}
