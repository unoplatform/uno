#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWindowWrapper
{
	Rect Bounds { get; }

	Rect VisibleBounds { get; }

	CoreWindowActivationState ActivationState { get; }

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
