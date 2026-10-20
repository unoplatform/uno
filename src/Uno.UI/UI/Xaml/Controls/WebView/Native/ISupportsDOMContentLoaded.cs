#nullable enable

using System;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebViews that report when the top-level document has been parsed (DOMContentLoaded).
/// </summary>
/// <remarks>
/// The event carries no navigation id: CoreWebView2 stamps it with its own id, the one NavigationStarting and
/// NavigationCompleted report.
/// </remarks>
internal interface ISupportsDOMContentLoaded
{
	event EventHandler DOMContentLoaded;
}
