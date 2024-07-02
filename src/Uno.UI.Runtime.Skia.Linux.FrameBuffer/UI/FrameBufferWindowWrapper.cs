using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
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

	internal Window? Window { get; private set; }

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = new Rect(default, newWindowSize);
	}

	internal void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosed();

	internal void SetWindow(Window window, XamlRoot xamlRoot)
	{
		Window = window;
		SetXamlRoot(xamlRoot);
	}
}
