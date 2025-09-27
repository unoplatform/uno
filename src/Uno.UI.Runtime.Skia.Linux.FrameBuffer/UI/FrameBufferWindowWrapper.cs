using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Windows.Graphics.Display;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<FrameBufferWindowWrapper> _instance = new Lazy<FrameBufferWindowWrapper>(() => new());

	public FrameBufferWindowWrapper()
	{
		var displayInformation = DisplayInformation.GetForCurrentViewSafe();
		RasterizationScale = (float)displayInformation.RawPixelsPerViewPixel;
	}

	internal static FrameBufferWindowWrapper Instance => _instance.Value;

	public override object? NativeWindow => null;

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		var bounds = new Rect(default, newWindowSize);
		SetBoundsAndVisibleBounds(bounds, bounds);
		Size = new((int)newWindowSize.Width, (int)newWindowSize.Height);
	}

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();
}
