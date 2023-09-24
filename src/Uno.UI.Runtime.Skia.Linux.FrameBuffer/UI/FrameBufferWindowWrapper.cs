using System;
using Uno.UI.Xaml.Controls;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

internal class FrameBufferWindowWrapper : INativeWindowWrapper
{
	private static readonly Lazy<FrameBufferWindowWrapper> _instance = new Lazy<FrameBufferWindowWrapper>(() => new());

	private Size _previousWindowSize = new Size(-1, -1);

	internal static FrameBufferWindowWrapper Instance => _instance.Value;

	internal Window? Window { get; private set; }

	internal XamlRoot? XamlRoot { get; private set; }

	public bool Visible { get; private set; }

	public event EventHandler<Size>? SizeChanged;
	public event EventHandler<CoreWindowActivationState>? ActivationChanged;
	public event EventHandler<bool>? VisibilityChanged;
	public event EventHandler? Closed;
	public event EventHandler? Shown;

	internal void RaiseNativeSizeChanged(Size newWindowSize)
	{
		ApplicationView.IShouldntUseGetForCurrentView()?.SetVisibleBounds(new Rect(default, newWindowSize));

		if (_previousWindowSize != newWindowSize)
		{
			_previousWindowSize = newWindowSize;

			SizeChanged?.Invoke(this, newWindowSize);
		}
	}

	internal void OnNativeVisibilityChanged(bool visible)
	{
		Visible = visible;
		VisibilityChanged?.Invoke(this, visible);
	}

	internal void OnNativeActivated(CoreWindowActivationState state) => ActivationChanged?.Invoke(this, state);

	internal void OnNativeClosed() => Closed?.Invoke(this, EventArgs.Empty); //TODO:MZ: Handle closing

	public void Activate() { }

	public void Show() => Shown?.Invoke(this, EventArgs.Empty);

	internal void SetWindow(Window window, XamlRoot xamlRoot)
	{
		Window = window;
		XamlRoot = xamlRoot;
	}
}
