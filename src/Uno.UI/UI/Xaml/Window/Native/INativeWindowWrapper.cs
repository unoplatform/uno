#nullable enable

using System;
using Microsoft.UI.Windowing;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing.Native;
using Microsoft.UI.Content;
using Windows.Graphics;

namespace Uno.UI.Xaml.Controls;

internal interface INativeWindowWrapper : INativeAppWindow
{
	ContentSiteView ContentSiteView { get; }

	Rect Bounds { get; }

	Rect VisibleBounds { get; }

	object? NativeWindow { get; }

	CoreWindowActivationState ActivationState { get; }

	/// <summary>
	/// Gets whether the native window's state and bounds are ready for managed activation.
	/// </summary>
	/// <remarks>
	/// While false, the window implementation shows the window without raising activation. The wrapper then
	/// owns the obligation to set <see cref="ActivationState"/> to <see cref="CoreWindowActivationState.CodeActivated"/>
	/// once its native state is applied, before any focus-driven activation.
	/// </remarks>
	bool IsReadyForActivation { get; }

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

	PointInt32 ConvertLocalToScreen(Point localPoint);

	Point ConvertScreenToLocal(PointInt32 screenPoint);

	bool TryConvertLocalToScreen(Point localPoint, out PointInt32 screenPoint);

	bool TryConvertScreenToLocal(PointInt32 screenPoint, out Point localPoint);

	void SetSystemBackdrop(Microsoft.UI.Xaml.Media.SystemBackdrop? backdrop);

	/// <summary>
	/// Gets whether this window can actually render <paramref name="backdrop"/>.
	/// </summary>
	/// <remarks>
	/// Only heads that implement <see cref="SetSystemBackdrop"/> render a material, and even those are
	/// gated on the OS version. Callers use this to decide whether to drop the root's opaque background,
	/// so a head that would leave nothing behind it must answer false.
	/// </remarks>
	bool IsSystemBackdropSupported(Microsoft.UI.Xaml.Media.SystemBackdrop backdrop);
}
