#nullable enable

using System;
using Microsoft.Web.WebView2.Core;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Implemented by native WebView providers that surface the granular
/// document-loading lifecycle events (ContentLoading, DOMContentLoaded).
/// </summary>
internal interface ISupportsLifecycleEvents
{
	event EventHandler<CoreWebView2ContentLoadingEventArgs>? ContentLoading;

	event EventHandler<CoreWebView2DOMContentLoadedEventArgs>? DOMContentLoaded;
}
