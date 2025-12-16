using System;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;
using Uno.WinUI.Runtime.Skia.Linux.FrameBuffer.UI;

namespace Uno.UI.Runtime.Skia;

internal partial class FrameBufferPointerInputSource : IUnoCorePointerInputSource
{
#pragma warning disable CS0067 // Some event are not raised on FrameBuffer ... yet!
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	private Func<VirtualKeyModifiers>? _keyboardInputSource;
	private IXamlRootHost? _host;

	private FrameBufferPointerInputSource()
	{
	}

	internal static FrameBufferPointerInputSource Instance { get; } = new FrameBufferPointerInputSource();

	internal void SetHost(IXamlRootHost host) => _host = host;

	public void Configure(Func<VirtualKeyModifiers> keyboardInputSource)
	{
		_keyboardInputSource = keyboardInputSource;
	}

	[NotImplemented] public bool HasCapture => false;

	[NotImplemented] public CoreCursor PointerCursor { get; set; } = new(CoreCursorType.Arrow, 0);

	[NotImplemented] public Point PointerPosition => default!;

	[NotImplemented] public void SetPointerCapture(PointerIdentifier pointer) => LogNotSupported();
	[NotImplemented] public void SetPointerCapture() => LogNotSupported();
	[NotImplemented] public void ReleasePointerCapture(PointerIdentifier pointer) => LogNotSupported();
	[NotImplemented] public void ReleasePointerCapture() => LogNotSupported();

	private void RaisePointerMoved(PointerEventArgs args)
		=> PointerMoved?.Invoke(this, args);

	private void RaisePointerPressed(PointerEventArgs args)
		=> PointerPressed?.Invoke(this, args);

	private void RaisePointerReleased(PointerEventArgs args)
		=> PointerReleased?.Invoke(this, args);

	private void RaisePointerCancelled(PointerEventArgs args)
		=> PointerCancelled?.Invoke(this, args);

	private void RaisePointerWheelChanged(PointerEventArgs args)
		=> PointerWheelChanged?.Invoke(this, args);

	private void RaisePointerEvent(Action<PointerEventArgs> raisePointerEvent, PointerEventArgs args)
	{
		if (_host?.RootElement is { } rootElement)
		{
			_ = rootElement.Dispatcher.RunAsync(
				CoreDispatcherPriority.High,
				() => raisePointerEvent(args));
		}
	}

	private VirtualKeyModifiers GetCurrentModifiersState()
		=> _keyboardInputSource?.Invoke() ?? VirtualKeyModifiers.None;

	private (double x, double y) GetOrientationAdjustedAbsolutionPosition(IntPtr rawEvent, Func<IntPtr, int, double> getX, Func<IntPtr, int, double> getY)
	{
		double x, y;
		switch (FrameBufferWindowWrapper.Instance.Orientation)
		{
			case DisplayOrientations.None:
			case DisplayOrientations.Landscape:
				x = getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				y = getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				break;
			case DisplayOrientations.Portrait:
				y = FrameBufferWindowWrapper.Instance.Bounds.Height - getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				x = getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				break;
			case DisplayOrientations.LandscapeFlipped:
				x = FrameBufferWindowWrapper.Instance.Bounds.Width - getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				y = FrameBufferWindowWrapper.Instance.Bounds.Height - getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				break;
			case DisplayOrientations.PortraitFlipped:
				y = getX(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Height);
				x = FrameBufferWindowWrapper.Instance.Bounds.Width - getY(rawEvent, (int)FrameBufferWindowWrapper.Instance.Bounds.Width);
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}

		return (x, y);
	}

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Skia for FrameBuffer.");
		}
	}
}
