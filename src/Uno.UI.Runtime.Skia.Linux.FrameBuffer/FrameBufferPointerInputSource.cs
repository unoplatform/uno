using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia;

unsafe internal partial class FrameBufferPointerInputSource : IUnoCorePointerInputSource
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

	private readonly DisplayInformation _displayInformation;
	private CoreWindow? _window;
	private Func<VirtualKeyModifiers>? _keyboardInputSource;

	public FrameBufferPointerInputSource()
	{
		_displayInformation = DisplayInformation.GetForCurrentView();
	}

	public void Configure(CoreWindow window, Func<VirtualKeyModifiers> keyboardInputSource)
	{
		_window = window;
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
		=> _ = _window?.Dispatcher.RunAsync(
			CoreDispatcherPriority.High,
			() => raisePointerEvent(args));

	private VirtualKeyModifiers GetCurrentModifiersState()
		=> _keyboardInputSource?.Invoke() ?? VirtualKeyModifiers.None;

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Skia for FrameBuffer.");
		}
	}
}
