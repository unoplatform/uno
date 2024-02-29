using System;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	internal static NativeWindowWrapper Instance => _instance.Value;

	public override object NativeWindow => null;

	internal void OnNativeClosed() => RaiseClosed();

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	internal void RaiseNativeSizeChanged(double width, double height)
	{
		var bounds = new Rect(default, new Size(width, height));
		var shouldRaise = bounds != VisibleBounds;

		VisibleBounds = bounds;
		Bounds = bounds;
		if (shouldRaise && Window.IsCurrentSet)
		{
			ApplicationView.GetForCurrentView()?.RaiseVisibleBoundsChanged();
		}
	}
}
