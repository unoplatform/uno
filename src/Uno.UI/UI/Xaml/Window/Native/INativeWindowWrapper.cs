#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
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

	float RasterizationScale { get; }

	bool Visible { get; }

	event EventHandler<Size>? SizeChanged;

	event EventHandler<Rect>? VisibleBoundsChanged;

	event EventHandler<CoreWindowActivationState>? ActivationChanged;

	event EventHandler<bool>? VisibilityChanged;

	event EventHandler<AppWindowClosingEventArgs>? Closing;

	event EventHandler? Closed;

	event EventHandler? Shown;

	void Activate();

	void Show();

	void Close();

	void ExtendContentIntoTitleBar(bool extend);
}
