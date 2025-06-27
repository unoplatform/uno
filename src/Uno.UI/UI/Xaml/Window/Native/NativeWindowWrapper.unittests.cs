using System;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.Graphics;

namespace Uno.UI.Xaml.Controls;

internal partial class NativeWindowWrapper : NativeWindowWrapperBase, INativeWindowWrapper
{
	private static readonly Lazy<NativeWindowWrapper> _instance = new(() => new NativeWindowWrapper());

	internal static NativeWindowWrapper Instance => _instance.Value;

	public NativeWindowWrapper()
	{
		RasterizationScale = 1f;
	}

	public override object NativeWindow => null;

	internal void OnNativeClosed() => RaiseClosing();

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void RaiseNativeSizeChanged(double width, double height)
	{
		var bounds = new Rect(default, new Size(width, height));

		SetBoundsAndVisibleBounds(bounds, bounds);
		Size = new((int)(bounds.Width * RasterizationScale), (int)(bounds.Height * RasterizationScale));
	}
}
