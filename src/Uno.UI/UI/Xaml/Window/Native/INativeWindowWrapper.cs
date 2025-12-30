#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing.Native;
using Microsoft.UI.Content;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWindowWrapper : INativeAppWindow
{
	ContentSiteView ContentSiteView { get; }

	Rect Bounds { get; }

	Rect VisibleBounds { get; }

	object? NativeWindow { get; }

	CoreWindowActivationState ActivationState { get; }

	bool WasShown { get; set; }

	event EventHandler<Size>? SizeChanged;

	event EventHandler<Rect>? VisibleBoundsChanged;

	event EventHandler<CoreWindowActivationState>? ActivationChanged;

	event EventHandler<bool>? VisibilityChanged;

	event EventHandler<AppWindowClosingEventArgs>? Closing;

	event EventHandler? Shown;

	void Close();

	void ExtendContentIntoTitleBar(bool extend);

#if __APPLE_UIKIT__
	Size GetWindowSize();
#endif
}
