using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<FrameBufferWindowWrapper> _instance = new(() => new());

	internal static FrameBufferWindowWrapper Instance => _instance.Value;

	public override object? NativeWindow => null;

	public DisplayOrientations Orientation { get; } = FeatureConfiguration.LinuxFramebuffer.Orientation;

	internal void SetSize(Size rawScreenSize)
	{
		if (XamlRoot is { })
		{
			var scale = RasterizationScale = (float)DisplayInformation.GetForCurrentViewSafe().RawPixelsPerViewPixel;
			if (Orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped)
			{
				(rawScreenSize.Height, rawScreenSize.Width) = (rawScreenSize.Width, rawScreenSize.Height);
			}
			var bounds = new Rect(0, 0,  rawScreenSize.Width / scale, rawScreenSize.Height /  scale);
			SetBoundsAndVisibleBounds(bounds, bounds);
			Size = new((int)rawScreenSize.Width, (int)rawScreenSize.Height);
			FrameBufferPointerInputSource.Instance.MousePosition = new Point(rawScreenSize.Width / 2, rawScreenSize.Height / 2);
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() => SetSize(rawScreenSize));
		}
	}

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();
}
