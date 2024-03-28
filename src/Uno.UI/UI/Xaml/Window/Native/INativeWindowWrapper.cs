#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing.Native;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWindowWrapper : INativeAppWindow
{
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
}
