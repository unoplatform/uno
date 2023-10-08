using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : NativeWindowWrapperBase
{
	private static readonly Lazy<FrameBufferWindowWrapper> _instance = new Lazy<FrameBufferWindowWrapper>(() => new());

	private Size _previousWindowSize = new Size(-1, -1);

	internal static FrameBufferWindowWrapper Instance => _instance.Value;

	internal Window? Window { get; private set; }

	internal XamlRoot? XamlRoot { get; private set; }

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		Bounds = new Rect(default, newWindowSize);
		VisibleBounds = new Rect(default, newWindowSize);
	}

	internal void OnNativeVisibilityChanged(bool visible) => Visible = visible;

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationState = state;

	internal void OnNativeClosed() => RaiseClosed();

	public override void Activate() { }

	public override void Close() { }

	protected override void ShowCore() { }

	internal void SetWindow(Window window, XamlRoot xamlRoot)
	{
		Window = window;
		XamlRoot = xamlRoot;
	}
}
