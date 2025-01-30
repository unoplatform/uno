using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.System;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Uno.UI.Hosting;

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
	private Func<VirtualKeyModifiers>? _keyboardInputSource;
	private IXamlRootHost? _host;

	private FrameBufferPointerInputSource()
	{
		_displayInformation = DisplayInformation.GetForCurrentViewSafe();
	}

	internal static FrameBufferPointerInputSource Instance { get; } = new FrameBufferPointerInputSource();

	internal void SetHost(IXamlRootHost host)
	{
		_host = host;
	}

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

	private void LogNotSupported([CallerMemberName] string member = "")
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"{member} not supported on Skia for FrameBuffer.");
		}
	}
}
