#nullable enable

using System;
using Microsoft.Web.WebView2.Core;
using Windows.UI;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Projects native controller operations used by the MUX WebView2 control port.
/// Native element hosting remains responsible for bounds, pointer input, and composition.
/// </summary>
internal interface INativeWebViewController
{
	bool IsVisible { get; set; }
	Color DefaultBackgroundColor { get; set; }
	double RasterizationScale { get; set; }
	void MoveFocus(CoreWebView2MoveFocusReason reason);
	event EventHandler<CoreWebView2MoveFocusRequestedEventArgs>? MoveFocusRequested;
}
