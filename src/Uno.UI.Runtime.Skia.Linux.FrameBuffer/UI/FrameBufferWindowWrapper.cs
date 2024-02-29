using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml;
using Windows.UI.ViewManagement;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<FrameBufferWindowWrapper> _instance = new Lazy<FrameBufferWindowWrapper>(() => new());

	internal static FrameBufferWindowWrapper Instance => _instance.Value;

	public override object? NativeWindow => null;

	internal Window? Window { get; private set; }

	internal XamlRoot? XamlRoot { get; private set; }

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		var newBounds = new Rect(default, newWindowSize);
		var shouldRaise = newBounds != VisibleBounds;
		VisibleBounds = newBounds;
		Bounds = newBounds;
		if (shouldRaise && Window.IsCurrentSet)
		{
			ApplicationView.GetForWindowId(Window!.AppWindow.Id).RaiseVisibleBoundsChanged();
		}
	}

	internal void SetWindow(Window window, XamlRoot xamlRoot)
	{
		Window = window;
		XamlRoot = xamlRoot;
	}
}
