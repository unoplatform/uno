using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Windows.Graphics.Display;
using Uno.UI.Dispatching;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<FrameBufferWindowWrapper> _instance = new(() => new());

	internal static FrameBufferWindowWrapper Instance => _instance.Value;

	public override object? NativeWindow => null;

	internal void SetSize(Size newWindowSize, float rasterizationScale)
	{
		if (XamlRoot is { } xamlRoot)
		{
			RasterizationScale = rasterizationScale;
			var scale = xamlRoot.RasterizationScale;
			var bounds = new Rect(0, 0,  newWindowSize.Width / scale, newWindowSize.Height /  scale);
			SetBoundsAndVisibleBounds(bounds, bounds);
			Size = new((int)newWindowSize.Width, (int)newWindowSize.Height);
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
