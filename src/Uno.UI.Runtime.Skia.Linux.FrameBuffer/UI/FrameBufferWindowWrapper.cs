using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.UI.Dispatching;
using Uno.UI.Runtime.Skia;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static FrameBufferWindowWrapper? _instance;
	internal static FrameBufferWindowWrapper Instance => _instance!;

	public static void Init(DisplayOrientations orientation) => _instance = new(orientation);

	public override object? NativeWindow => null;

	private FrameBufferWindowWrapper(DisplayOrientations orientation)
	{
		if (_instance != null)
		{
			throw new InvalidOperationException($"{nameof(FrameBufferWindowWrapper)} should be created once.");
		}
		_instance = this;

		Orientation = orientation;
	}

	public DisplayOrientations Orientation { get; }

	internal void SetSize(Size rawScreenSize)
	{
		if (XamlRoot is { })
		{
			var scale = RasterizationScale = (float)DisplayInformation.GetForCurrentViewSafe().RawPixelsPerViewPixel;
			if (Orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped)
			{
				(rawScreenSize.Height, rawScreenSize.Width) = (rawScreenSize.Width, rawScreenSize.Height);
			}
			var bounds = new Rect(0, 0, rawScreenSize.Width / scale, rawScreenSize.Height / scale);
			SetBoundsAndVisibleBounds(bounds, bounds);
			Size = new((int)rawScreenSize.Width, (int)rawScreenSize.Height);
			FrameBufferPointerInputSource.Instance.MousePosition = bounds.GetCenter();
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
