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

	internal void SetSize(Size newWindowSize, float rasterizationScale)
	{
		if (XamlRoot is { } xamlRoot)
		{
			RasterizationScale = rasterizationScale;
			var scale = xamlRoot.RasterizationScale;
			var bounds = new Rect(0, 0,  newWindowSize.Width / scale, newWindowSize.Height /  scale);
			if (Orientation is DisplayOrientations.Portrait or DisplayOrientations.PortraitFlipped)
			{
				(bounds.Height, bounds.Width) = (bounds.Width, bounds.Height);
			}
			SetBoundsAndVisibleBounds(bounds, bounds);
			Size = new((int)newWindowSize.Width, (int)newWindowSize.Height);
			FrameBufferPointerInputSource.Instance.MousePosition = bounds.GetCenter();
		}
		else
		{
			NativeDispatcher.Main.Enqueue(() => SetSize(newWindowSize, rasterizationScale));
		}
	}

	internal void OnNativeVisibilityChanged(bool visible) => IsVisible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosing();
}
