using System;
using Android.Views;
using Microsoft.UI.Input;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidCorePointerInputSource : IUnoCorePointerInputSource
{
	public static AndroidCorePointerInputSource Instance { get; } = new();

	//private readonly Action<string>? _trace = Console.WriteLine;
	private readonly Action<string>? _trace = typeof(AndroidCorePointerInputSource).Log().IsEnabled(LogLevel.Trace)
		? msg => typeof(AndroidCorePointerInputSource).Log().Trace(msg)
		: null;

	private IAsyncAction? _pendingAsyncHoverExit;

	private AndroidCorePointerInputSource()
	{
	}

#pragma warning disable CS0067
	public event TypedEventHandler<object, PointerEventArgs>? PointerCaptureLost;
	public event TypedEventHandler<object, PointerEventArgs>? PointerEntered;
	public event TypedEventHandler<object, PointerEventArgs>? PointerExited;
	public event TypedEventHandler<object, PointerEventArgs>? PointerMoved;
	public event TypedEventHandler<object, PointerEventArgs>? PointerPressed;
	public event TypedEventHandler<object, PointerEventArgs>? PointerReleased;
	public event TypedEventHandler<object, PointerEventArgs>? PointerWheelChanged;
	public event TypedEventHandler<object, PointerEventArgs>? PointerCancelled; // Uno Only
#pragma warning restore CS0067

	public bool HasCapture => false;

	public Point PointerPosition => default;

	public CoreCursor PointerCursor
	{
		get => new(CoreCursorType.Arrow, 0);
		set { }
	}

	public void SetPointerCapture()
	{

	}

	public void SetPointerCapture(PointerIdentifier pointer)
	{
	}

	public void ReleasePointerCapture()
	{
	}

	public void ReleasePointerCapture(PointerIdentifier pointer)
	{
	}

	internal void OnNativeMotionEvent(MotionEvent nativeArgs, int[] correction, bool nativelyHandled)
	{
		try
		{
			_trace?.Invoke($"OnNativeMotionEvent: ts={nativeArgs.EventTime} | cnt={nativeArgs.PointerCount} | x={nativeArgs.RawX} | y={nativeArgs.RawY}"
				+ $"| act={nativeArgs.Action} | actBtn={nativeArgs.ActionButton} | idx={nativeArgs.ActionIndex} "
				+ $"| btn={nativeArgs.ButtonState} | down={nativeArgs.DownTime} | keys={nativeArgs.MetaState} | dst={nativeArgs.GetAxisValue(Axis.Distance)} | wheel={nativeArgs.GetAxisValue(Axis.Wheel)} "
				+ $"| pressure={nativeArgs.Pressure} | or={nativeArgs.GetAxisValue(Axis.Orientation)} | tilt={nativeArgs.GetAxisValue(Axis.Tilt)} "
				+ $"| size={nativeArgs.ToolMajor}x{nativeArgs.ToolMinor} / {nativeArgs.TouchMajor}x{nativeArgs.TouchMinor}"
				+ $"| buttons={(nativeArgs.IsButtonPressed(MotionEventButtonState.Primary) ? "prime " : "")}"
				+ $"{(nativeArgs.IsButtonPressed(MotionEventButtonState.Secondary) ? "second " : "")}"
				+ $"{(nativeArgs.IsButtonPressed(MotionEventButtonState.Tertiary) ? "third " : "")}"
				+ $"{(nativeArgs.IsButtonPressed(MotionEventButtonState.StylusPrimary) ? "pen-prime " : "")}"
				+ $"{(nativeArgs.IsButtonPressed(MotionEventButtonState.StylusSecondary) ? "pen-second " : "")}"
				+ $"{(nativeArgs.IsButtonPressed(MotionEventButtonState.Back) ? "back " : "")}"
				+ $"{(nativeArgs.IsButtonPressed(MotionEventButtonState.Forward) ? "forward " : "")}");

			var pointerCount = nativeArgs.PointerCount;
			var action = nativeArgs.Action & MotionEventActions.Mask;

			if (pointerCount > 1 && action == MotionEventActions.Move)
			{
				// When we get a move, we make sure to raise the move for all pointers.
				// Note: We could probably also raise a move for pointers other than ActionIndex for other actions
				//		 but as multi touch is only for fingers, we get a lot of events (due to the approx.) and it's
				//		 safer to not try to over-interpret events.

				for (var pointerIndex = 0; pointerIndex < pointerCount; pointerIndex++)
				{
					var args = ToManaged(nativeArgs, correction, pointerIndex, nativelyHandled);

					OnNativeMotionEvent(MotionEventActions.Move, args);
				}
			}
			else
			{
				var args = ToManaged(nativeArgs, correction, nativeArgs.ActionIndex, nativelyHandled);

				OnNativeMotionEvent(action, args);
			}
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to dispatch native pointer event: {error}");
			}
		}
	}

	private void OnNativeMotionEvent(MotionEventActions action, PointerEventArgs args)
	{
		_trace?.Invoke($"[{action}] {args}");

		switch (action)
		{
			case MotionEventActions.HoverEnter when args.CurrentPoint.PointerDeviceType is Windows.Devices.Input.PointerDeviceType.Touch:
			case MotionEventActions.HoverExit when args.CurrentPoint.PointerDeviceType is Windows.Devices.Input.PointerDeviceType.Touch:
				// We get HoverEnter and HoverExit for touch only when TalkBack is enabled.
				// We ignore these events.
				break;

			case MotionEventActions.HoverEnter:
				PointerEntered?.Invoke(this, args);
				break;

			case MotionEventActions.HoverExit when !args.CurrentPoint.IsInContact:
				// When a mouse button is pressed or pen touches the screen (a.k.a. becomes in contact), we receive an HoverExit before the Down.
				// We validate here if pointer 'isInContact' (which is the case for HoverExit when mouse button pressed / pen touched the screen)
				// and we ignore them (as on UWP Exit is raised only when pointer moves out of bounds of the control, no matter the pressed state).
				// As a side effect we will have to update the hover state on each Move in order to handle the case of press -> move out -> release.
				PointerExited?.Invoke(this, args);
				break;

			case MotionEventActions.HoverExit when args.CurrentPoint.PointerDeviceType is PointerDeviceType.Pen:
				// Unfortunately, on some android devices (Surface Duo) "Distance" will always be 0, even when the pen is not in contact.
				// This prevents us to properly filter out the hover exit event on pointer ... the only solution is to defer this to the next loop of the dispatcher.
				// If a press is raised before the next dispatcher loop, we can safely ignore the hover exit.
				_pendingAsyncHoverExit = CoreDispatcher.Main.RunAsync(
					CoreDispatcherPriority.High,
					() => PointerExited?.Invoke(this, args));
				break;

			case MotionEventActions.HoverExit:
				break;

			case PointerHelpers.StylusWithBarrelDown:
			case MotionEventActions.Down:
			case MotionEventActions.PointerDown:
				_pendingAsyncHoverExit?.Cancel();
				PointerPressed?.Invoke(this, args);
				break;

			case PointerHelpers.StylusWithBarrelUp:
			case MotionEventActions.Up:
			case MotionEventActions.PointerUp:
				PointerReleased?.Invoke(this, args);
				break;

			// This is ported from the previous version using native rendering.
			// However, since this was made, we noticed that windows is also firing down and up only for the first button, so we can ignore it.
			////// We get ACTION_DOWN and ACTION_UP only for "left" button, and instead we get a HOVER_MOVE when pressing/releasing the right button of the mouse.
			////// So on each POINTER_MOVE we make sure to update the pressed state if it does not match.
			////// Note: We can also have HOVER_MOVE with barrel button pressed, so we make sure to "PointerDown" only for Mouse.
			//////case MotionEventActions.HoverMove when args.CurrentPoint.PointerDeviceType is Windows.Devices.Input.PointerDeviceType.Mouse && args.HasPressedButton && !IsPressed(args.Pointer):
			//////	return OnNativePointerDown(args) | OnNativePointerMoveWithOverCheck(args.Reset(), isInView);
			//////case MotionEventActions.HoverMove when !args.HasPressedButton && IsPressed(args.Pointer):
			//////	return OnNativePointerUp(args) | OnNativePointerMoveWithOverCheck(args.Reset(), isInView);

			case PointerHelpers.StylusWithBarrelMove:
			case MotionEventActions.Move:
			case MotionEventActions.HoverMove:
				PointerMoved?.Invoke(this, args);
				break;

			case MotionEventActions.Cancel:
				PointerCancelled?.Invoke(this, args);
				break;

			default:
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"We receive a native motion event of '{action}', but this is not supported and should have been filtered out in native code.");
				}
				break;
		}
	}

	private PointerEventArgs ToManaged(MotionEvent nativeArgs, int[] correction, int pointerIndex, bool nativelyHandled)
	{
		var nativePointerType = nativeArgs.GetToolType(pointerIndex);
		var pointerType = nativePointerType.ToPointerDeviceType();
		var pointerDevice = PointerDevice.For(pointerType);
		var pointerId = PointerHelpers.GetPointerId(nativeArgs, pointerIndex);
		var nativePointerAction = nativeArgs.Action;
		var nativePointerButtons = nativeArgs.ButtonState;

		uint frameId;
		ulong ts;
		if ((int)global::Android.OS.Build.VERSION.SdkInt >= 34)
		{
			var nativeTimestamp = nativeArgs.EventTimeNanos;
			frameId = (uint)nativeTimestamp;
			ts = (ulong)nativeTimestamp / 1000; // ns to µs
		}
		else
		{
			var nativeTimestamp = nativeArgs.EventTime;
			frameId = (uint)nativeTimestamp;
			ts = (ulong)nativeTimestamp * 1000; // ms to µs
		}

		var isInContact = PointerHelpers.IsInContact(nativeArgs, pointerType, nativePointerAction, nativePointerButtons);
		var isInRange = true; // TODO: ?
		var keyModifiers = nativeArgs.MetaState.ToVirtualKeyModifiers();
		var x = nativeArgs.GetX(pointerIndex);
		var y = nativeArgs.GetY(pointerIndex);

		var position = new Point((int)x - correction[0], (int)y - correction[1]).PhysicalToLogicalPixels();
		var properties = PointerHelpers.GetProperties(nativeArgs, pointerIndex, nativePointerType, nativePointerAction, nativePointerButtons, isInRange, isInContact);
		var point = new PointerPoint(frameId, ts, pointerDevice, pointerId, position, position, isInContact, new PointerPointProperties(properties));
		var args = new PointerEventArgs(point, keyModifiers)
		{
			Handled = nativelyHandled
		};

		return args;
	}
}
