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

	/// <summary>
	/// Gets a value indicating whether a close of this window can still be cancelled. Platforms that
	/// only learn of a close once the OS has already performed it return false for that window.
	/// </summary>
	bool IsClosingCancellable { get; }

	event EventHandler<Size>? SizeChanged;

	event EventHandler<Rect>? VisibleBoundsChanged;

	event EventHandler<CoreWindowActivationState>? ActivationChanged;

	event EventHandler<bool>? VisibilityChanged;

	event EventHandler<AppWindowClosingEventArgs>? Closing;

	event EventHandler? Shown;

	void Close();

	void ExtendContentIntoTitleBar(bool extend);

	void SetSystemBackdrop(Microsoft.UI.Xaml.Media.SystemBackdrop? backdrop);
}
