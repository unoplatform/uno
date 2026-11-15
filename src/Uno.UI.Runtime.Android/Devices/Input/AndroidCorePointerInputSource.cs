using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Uno.Foundation.Logging;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Runtime.Android;

internal sealed class AndroidCorePointerInputSource : IUnoCorePointerInputSource
{
	//private readonly Action<string>? _trace = Console.WriteLine;
	private readonly Action<string>? _trace = typeof(AndroidCorePointerInputSource).Log().IsEnabled(LogLevel.Trace)
		? msg => typeof(AndroidCorePointerInputSource).Log().Trace(msg)
		: null;

	private static readonly PointerPointProperties _noButtons = new();

	private readonly Func<View?> _cursorHost;

	// Last properties of the mouse and pen pointers, used to resolve which button changed.
	private readonly Dictionary<(PointerDeviceType, uint), PointerPointProperties> _previousProperties = new();
	private readonly HashSet<uint> _touchPadGestureFingers = new();

	// MotionEvent.CLASSIFICATION_* values of API 34, not bound by ClassificationMode.
	private const int ClassificationTwoFingerSwipe = 3;
	private const int ClassificationMultiFingerSwipe = 4;
	private const int ClassificationPinch = 5;

	// The ScrollContentPresenter zooms by 10% per Ctrl + wheel notch.
	private static readonly double _pinchZoomStep = Math.Log(1.1);

	// Touchpad gestures are reported with fake fingers while the cursor stays put.
	private (float x, float y) _lastCursorPosition;
	private (float x, float y)? _swipeLastPosition;
	private double _swipeRemainderX;
	private double _swipeRemainderY;
	private double _pinchLastSpan;
	private double _pinchLogScale;

	private const long HoverExitConfirmationDelayMs = 50;

	private readonly Handler _handler = new(Looper.MainLooper!);
	private Java.Lang.IRunnable? _pendingHoverExit;
	private CoreCursor? _pointerCursor = new(CoreCursorType.Arrow, 0);

	internal AndroidCorePointerInputSource(Func<View?> cursorHost)
	{
		_cursorHost = cursorHost;
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

	public CoreCursor? PointerCursor
	{
		get => _pointerCursor;
		set
		{
			_pointerCursor = value;
			RefreshCursor();
		}
	}

	/// <summary>
	/// Applies the current cursor to the native host, e.g. after the activity driving the window has been re-created.
	/// </summary>
	internal void RefreshCursor()
	{
		if (_cursorHost() is not { Context: { } context } host)
		{
			return;
		}

		try
		{
			// The pointer icon is resolved by the view hierarchy on each mouse hover: our render views don't set any,
			// so the one of the host is used (unless the pointer is over an embedded native view that sets its own).
			host.PointerIcon = _pointerCursor is { } cursor
				? PointerIcon.GetSystemIcon(context, AndroidPointerHelpers.ToPointerIconType(cursor.Type))
				: null;
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error("Failed to update the pointer icon.", error);
			}
		}
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
				+ $"| act={nativeArgs.Action} | actBtn={nativeArgs.ActionButton} | idx={nativeArgs.ActionIndex} | src={nativeArgs.Source} | tool={nativeArgs.GetToolType(nativeArgs.ActionIndex)} "
				+ $"| btn={nativeArgs.ButtonState} | down={nativeArgs.DownTime} | keys={nativeArgs.MetaState} | dst={nativeArgs.GetAxisValue(Axis.Distance)} "
				+ $"| vscroll={nativeArgs.GetAxisValue(Axis.Vscroll)} | hscroll={nativeArgs.GetAxisValue(Axis.Hscroll)} "
				+ $"| pressure={nativeArgs.Pressure} | or={nativeArgs.GetAxisValue(Axis.Orientation)} | tilt={nativeArgs.GetAxisValue(Axis.Tilt)} "
				+ $"| size={nativeArgs.ToolMajor}x{nativeArgs.ToolMinor} / {nativeArgs.TouchMajor}x{nativeArgs.TouchMinor}");

			var pointerCount = nativeArgs.PointerCount;
			var action = nativeArgs.ActionMasked;

			if (TryHandleTouchPadGesture(nativeArgs, action, correction, nativelyHandled))
			{
				return;
			}

			if (pointerCount > 1 && action == MotionEventActions.Move)
			{
				// When we get a move, we make sure to raise the move for all pointers.
				// Note: We could probably also raise a move for pointers other than ActionIndex for other actions
				//		 but as multi touch is only for fingers, we get a lot of events (due to the approx.) and it's
				//		 safer to not try to over-interpret events.
				for (var pointerIndex = 0; pointerIndex < pointerCount; pointerIndex++)
				{
					OnNativeMotionEvent(nativeArgs, action, pointerIndex, correction, nativelyHandled);
				}
			}
			else
			{
				OnNativeMotionEvent(nativeArgs, action, nativeArgs.ActionIndex, correction, nativelyHandled);
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

	private void OnNativeMotionEvent(MotionEvent nativeArgs, MotionEventActions action, int pointerIndex, int[] correction, bool nativelyHandled)
	{
		var toolType = nativeArgs.GetToolType(pointerIndex);
		var pointerId = AndroidPointerHelpers.GetPointerId(nativeArgs, pointerIndex);

		switch (toolType.ToPointerDeviceType())
		{
			case PointerDeviceType.Mouse:
				OnMouseEvent(nativeArgs, action, pointerIndex, pointerId, isTouchPad: false, correction, nativelyHandled);
				break;

			case PointerDeviceType.Touch when IsTouchPadCursor(nativeArgs, action, pointerId):
				OnMouseEvent(nativeArgs, action, pointerIndex, pointerId, isTouchPad: true, correction, nativelyHandled);
				break;

			case PointerDeviceType.Pen:
				OnPenEvent(nativeArgs, action, pointerIndex, pointerId, toolType, correction, nativelyHandled);
				break;

			default:
				OnTouchEvent(nativeArgs, action, pointerIndex, pointerId, correction, nativelyHandled);
				break;
		}
	}

	/// <summary>
	/// Converts the touchpad gestures classified by Android 14+ into the wheel events WinUI raises for them.
	/// </summary>
	/// <remarks>
	/// Those gestures are reported as fake fingers, which would otherwise press whatever is under the cursor.
	/// </remarks>
	private bool TryHandleTouchPadGesture(MotionEvent nativeArgs, MotionEventActions action, int[] correction, bool nativelyHandled)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(34) || !nativeArgs.IsFromSource(InputSourceType.Mouse))
		{
			return false;
		}

		var classification = (int)nativeArgs.Classification;
		if (classification is ClassificationTwoFingerSwipe or ClassificationPinch or ClassificationMultiFingerSwipe
			&& action is MotionEventActions.Down)
		{
			CancelPendingHoverExit();
		}

		switch (classification)
		{
			case ClassificationTwoFingerSwipe:
				var x = nativeArgs.GetX(0);
				var y = nativeArgs.GetY(0);
				if (action is MotionEventActions.Move && _swipeLastPosition is { } last)
				{
					// The fake finger moves like the content would on a touch screen (so honoring the "reverse scrolling" setting):
					// moving down scrolls up (positive delta) and moving right scrolls left (negative horizontal delta).
					RaiseTouchPadWheel(nativeArgs, ref _swipeRemainderY, ViewHelper.PhysicalToLogicalPixels(y - last.y), isHorizontal: false, VirtualKeyModifiers.None, correction, nativelyHandled);
					RaiseTouchPadWheel(nativeArgs, ref _swipeRemainderX, -ViewHelper.PhysicalToLogicalPixels(x - last.x), isHorizontal: true, VirtualKeyModifiers.None, correction, nativelyHandled);
				}
				else if (action is MotionEventActions.Down)
				{
					_swipeRemainderX = _swipeRemainderY = 0;
				}

				_swipeLastPosition = action is MotionEventActions.Up or MotionEventActions.Cancel ? null : (x, y);
				return true;

			case ClassificationPinch:
				var span = nativeArgs.PointerCount >= 2
					? Math.Sqrt(Math.Pow(nativeArgs.GetX(1) - nativeArgs.GetX(0), 2) + Math.Pow(nativeArgs.GetY(1) - nativeArgs.GetY(0), 2))
					: 0;
				if (action is not MotionEventActions.Move)
				{
					_pinchLogScale = 0;
				}
				else if (span > 0 && _pinchLastSpan > 0)
				{
					// Like Windows, we report a touchpad pinch as Ctrl + wheel, one notch per zoom step of the ScrollViewer.
					_pinchLogScale += Math.Log(span / _pinchLastSpan);
					while (Math.Abs(_pinchLogScale) >= _pinchZoomStep)
					{
						var notch = Math.Sign(_pinchLogScale) * ScrollContentPresenter.ScrollViewerDefaultMouseWheelDelta;
						var remainder = 0d;
						RaiseTouchPadWheel(nativeArgs, ref remainder, notch, isHorizontal: false, VirtualKeyModifiers.Control, correction, nativelyHandled);
						_pinchLogScale -= Math.Sign(_pinchLogScale) * _pinchZoomStep;
					}
				}

				_pinchLastSpan = span;
				return true;

			case ClassificationMultiFingerSwipe:
				// System navigation gestures.
				return true;

			default:
				return false;
		}
	}

	private void RaiseTouchPadWheel(MotionEvent nativeArgs, ref double remainder, double delta, bool isHorizontal, VirtualKeyModifiers extraModifiers, int[] correction, bool nativelyHandled)
	{
		remainder += delta;
		var wheelDelta = (int)Math.Truncate(remainder);
		if (wheelDelta is 0)
		{
			return;
		}

		remainder -= wheelDelta;

		var properties = AndroidPointerHelpers.GetMouseProperties(0, isTouchPad: true);
		properties.MouseWheelDelta = wheelDelta;
		properties.IsHorizontalMouseWheel = isHorizontal;

		var pointerId = AndroidPointerHelpers.GetPointerId(nativeArgs, 0);
		var (x, y) = _lastCursorPosition;
		var args = CreateArgs(nativeArgs, x, y, PointerDeviceType.Mouse, pointerId, isInContact: false, properties, correction, nativelyHandled, extraModifiers);

		PointerWheelChanged?.Invoke(this, args);
	}

	/// <summary>
	/// Determines if a finger event comes from a touchpad driving the mouse cursor.
	/// </summary>
	/// <remarks>
	/// Touchpads report the cursor with a finger tool type, but a mouse source. Their scroll and pinch gestures are
	/// also reported as (fake) fingers from a mouse source, but pressed without any button: we keep those as touch
	/// until released, so they still pan and zoom like on a touch screen.
	/// </remarks>
	private bool IsTouchPadCursor(MotionEvent nativeArgs, MotionEventActions action, uint pointerId)
	{
		if (!nativeArgs.IsFromSource(InputSourceType.Mouse))
		{
			return false;
		}

		switch (action)
		{
			case MotionEventActions.Down:
			case MotionEventActions.PointerDown:
				if (AndroidPointerHelpers.HasMouseButtonPressed(nativeArgs.ButtonState))
				{
					return true;
				}

				_touchPadGestureFingers.Add(pointerId);
				return false;

			case MotionEventActions.Move:
				return !_touchPadGestureFingers.Contains(pointerId);

			case MotionEventActions.Up:
			case MotionEventActions.PointerUp:
			case MotionEventActions.Cancel:
				return !_touchPadGestureFingers.Remove(pointerId);

			default:
				return true;
		}
	}

	private void OnMouseEvent(MotionEvent nativeArgs, MotionEventActions action, int pointerIndex, uint pointerId, bool isTouchPad, int[] correction, bool nativelyHandled)
	{
		var key = (PointerDeviceType.Mouse, pointerId);
		var buttons = nativeArgs.ButtonState;
		var isInContact = AndroidPointerHelpers.HasMouseButtonPressed(buttons);
		_lastCursorPosition = (nativeArgs.GetX(pointerIndex), nativeArgs.GetY(pointerIndex));

		switch (action)
		{
			case MotionEventActions.HoverEnter:
				PointerEntered?.Invoke(this, CreateMouseArgs());
				break;

			case MotionEventActions.HoverExit when isInContact:
				// When a mouse button is pressed, we receive an HoverExit before the Down. We ignore it,
				// as on WinUI Exit is raised only when pointer moves out of bounds of the control, no matter the pressed state.
				break;

			case MotionEventActions.HoverExit when isTouchPad:
				// Touchpads end the hover before a click without any button pressed yet: confirm it's not one (cf. pen below).
				var exitArgs = CreateMouseArgs();
				DeferHoverExit(() =>
				{
					_previousProperties.Remove(key);
					PointerExited?.Invoke(this, exitArgs);
				});
				break;

			case MotionEventActions.HoverExit:
				_previousProperties.Remove(key);
				PointerExited?.Invoke(this, CreateMouseArgs());
				break;

			case MotionEventActions.Scroll:
				RaiseWheel(Axis.Vscroll, isHorizontal: false);
				RaiseWheel(Axis.Hscroll, isHorizontal: true);
				break;

			case MotionEventActions.Cancel:
				_previousProperties.Remove(key);
				PointerCancelled?.Invoke(this, CreateMouseArgs());
				break;

			case MotionEventActions.Down:
			case MotionEventActions.Up:
			case MotionEventActions.PointerDown:
			case MotionEventActions.PointerUp:
			case MotionEventActions.ButtonPress:
			case MotionEventActions.ButtonRelease:
			case MotionEventActions.Move:
			case MotionEventActions.HoverMove:
				// Android raises a Down/Up only for the primary, secondary and tertiary buttons, and a ButtonPress/Release
				// for each button (in addition to the Down/Up). Like on WinUI, we raise a single pressed for the first
				// button and a single released for the last one, any other button change being raised as a move.
				var hadButtons = _previousProperties.TryGetValue(key, out var previous) && previous.HasPressedButton;
				var args = CreateMouseArgs(previous);
				var properties = args.CurrentPoint.Properties;
				var buttonsChanged = properties.PointerUpdateKind is not PointerUpdateKind.Other;

				_previousProperties[key] = properties;

				if (buttonsChanged && !hadButtons && properties.HasPressedButton)
				{
					CancelPendingHoverExit();
					PointerPressed?.Invoke(this, args);
				}
				else if (buttonsChanged && hadButtons && !properties.HasPressedButton)
				{
					PointerReleased?.Invoke(this, args);
				}
				else if (buttonsChanged || action is MotionEventActions.Move or MotionEventActions.HoverMove)
				{
					PointerMoved?.Invoke(this, args);
				}
				break;

			default:
				TraceUnsupported(action);
				break;
		}

		PointerEventArgs CreateMouseArgs(PointerPointProperties? previous = null)
		{
			var properties = AndroidPointerHelpers
				.GetMouseProperties(buttons, isTouchPad)
				.SetUpdateKindFromPrevious(previous ?? _noButtons);

			return CreateArgs(nativeArgs, pointerIndex, PointerDeviceType.Mouse, pointerId, isInContact, properties, correction, nativelyHandled);
		}

		void RaiseWheel(Axis axis, bool isHorizontal)
		{
			var delta = AndroidPointerHelpers.ToMouseWheelDelta(nativeArgs.GetAxisValue(axis, pointerIndex));
			if (delta is 0)
			{
				return;
			}

			var properties = AndroidPointerHelpers.GetMouseProperties(buttons, isTouchPad);
			properties.MouseWheelDelta = delta;
			properties.IsHorizontalMouseWheel = isHorizontal;

			PointerWheelChanged?.Invoke(this, CreateArgs(nativeArgs, pointerIndex, PointerDeviceType.Mouse, pointerId, isInContact, properties, correction, nativelyHandled));
		}
	}

	private void OnPenEvent(MotionEvent nativeArgs, MotionEventActions action, int pointerIndex, uint pointerId, MotionEventToolType toolType, int[] correction, bool nativelyHandled)
	{
		var key = (PointerDeviceType.Pen, pointerId);
		var buttons = nativeArgs.ButtonState;

		_previousProperties.TryGetValue(key, out var previous);
		var wasInContact = previous is { IsLeftButtonPressed: true } or { IsRightButtonPressed: true };
		var isInContact = AndroidPointerHelpers.IsPenInContact(nativeArgs, pointerIndex, action, wasInContact);
		var isRightContact = AndroidPointerHelpers.IsStylusWithBarrelAction(action)
			|| (wasInContact
				? previous!.IsRightButtonPressed
				: buttons.HasFlag(MotionEventButtonState.StylusPrimary));

		var properties = AndroidPointerHelpers
			.GetPenProperties(nativeArgs, pointerIndex, toolType, buttons, isInContact, isRightContact)
			.SetUpdateKindFromPrevious(previous ?? _noButtons);
		var args = CreateArgs(nativeArgs, pointerIndex, PointerDeviceType.Pen, pointerId, isInContact, properties, correction, nativelyHandled);

		switch (action)
		{
			case MotionEventActions.HoverEnter:
				_previousProperties[key] = properties;
				PointerEntered?.Invoke(this, args);
				break;

			case MotionEventActions.HoverExit when !isInContact:
				_previousProperties.Remove(key);
				PointerExited?.Invoke(this, args);
				break;

			case MotionEventActions.HoverExit:
				// Unfortunately, on some android devices (Surface Duo) "Distance" will always be 0, even when the pen is not in contact.
				// This prevents us to properly filter out the hover exit event on pointer ... the only solution is to defer it.
				// If a press is raised in the meantime, we can safely ignore the hover exit.
				DeferHoverExit(() =>
				{
					_previousProperties.Remove(key);
					PointerExited?.Invoke(this, args);
				});
				break;

			case AndroidPointerHelpers.StylusWithBarrelDown:
			case MotionEventActions.Down:
			case MotionEventActions.PointerDown:
				CancelPendingHoverExit();
				_previousProperties[key] = properties;
				PointerPressed?.Invoke(this, args);
				break;

			case AndroidPointerHelpers.StylusWithBarrelUp:
			case MotionEventActions.Up:
			case MotionEventActions.PointerUp:
				_previousProperties[key] = properties;
				PointerReleased?.Invoke(this, args);
				break;

			case AndroidPointerHelpers.StylusWithBarrelMove:
			case MotionEventActions.Move:
			case MotionEventActions.HoverMove:
			case MotionEventActions.ButtonPress:
			case MotionEventActions.ButtonRelease:
				_previousProperties[key] = properties;
				PointerMoved?.Invoke(this, args);
				break;

			case MotionEventActions.Cancel:
				_previousProperties.Remove(key);
				PointerCancelled?.Invoke(this, args);
				break;

			default:
				TraceUnsupported(action);
				break;
		}
	}

	private void OnTouchEvent(MotionEvent nativeArgs, MotionEventActions action, int pointerIndex, uint pointerId, int[] correction, bool nativelyHandled)
	{
		var isInContact = AndroidPointerHelpers.IsTouchInContact(action);
		var properties = AndroidPointerHelpers.GetTouchProperties(action, isInContact);
		var args = CreateArgs(nativeArgs, pointerIndex, PointerDeviceType.Touch, pointerId, isInContact, properties, correction, nativelyHandled);

		switch (action)
		{
			case MotionEventActions.HoverEnter:
			case MotionEventActions.HoverExit:
				// We get HoverEnter and HoverExit for touch only when TalkBack is enabled.
				// We ignore these events.
				break;

			case MotionEventActions.Down:
			case MotionEventActions.PointerDown:
				PointerPressed?.Invoke(this, args);
				break;

			case MotionEventActions.Up:
			case MotionEventActions.PointerUp:
				PointerReleased?.Invoke(this, args);
				break;

			case MotionEventActions.Move:
			case MotionEventActions.HoverMove:
				PointerMoved?.Invoke(this, args);
				break;

			case MotionEventActions.Cancel:
				PointerCancelled?.Invoke(this, args);
				break;

			default:
				TraceUnsupported(action);
				break;
		}
	}

	/// <summary>
	/// Raises a hover exit only if not followed by a press: Android ends the hover right before a contact begins.
	/// </summary>
	/// <remarks>The press is a distinct input message, so it is not dispatched yet when the exit is.</remarks>
	private void DeferHoverExit(Action raiseExited)
	{
		CancelPendingHoverExit();

		Java.Lang.IRunnable? runnable = null;
		runnable = new Java.Lang.Runnable(() =>
		{
			if (ReferenceEquals(_pendingHoverExit, runnable))
			{
				_pendingHoverExit = null;
				raiseExited();
			}
		});
		_pendingHoverExit = runnable;
		_handler.PostDelayed(runnable, HoverExitConfirmationDelayMs);
	}

	private void CancelPendingHoverExit()
	{
		if (_pendingHoverExit is { } pending)
		{
			_handler.RemoveCallbacks(pending);
			_pendingHoverExit = null;
		}
	}

	private void TraceUnsupported(MotionEventActions action)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Ignoring unsupported native motion event '{action}'.");
		}
	}

	private static PointerEventArgs CreateArgs(MotionEvent nativeArgs, int pointerIndex, PointerDeviceType pointerType, uint pointerId, bool isInContact, PointerPointProperties properties, int[] correction, bool nativelyHandled)
		=> CreateArgs(nativeArgs, nativeArgs.GetX(pointerIndex), nativeArgs.GetY(pointerIndex), pointerType, pointerId, isInContact, properties, correction, nativelyHandled);

	private static PointerEventArgs CreateArgs(MotionEvent nativeArgs, float x, float y, PointerDeviceType pointerType, uint pointerId, bool isInContact, PointerPointProperties properties, int[] correction, bool nativelyHandled, VirtualKeyModifiers extraModifiers = VirtualKeyModifiers.None)
	{
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

		var keyModifiers = nativeArgs.MetaState.ToVirtualKeyModifiers() | extraModifiers;
		var position = new Point(x - correction[0], y - correction[1]).PhysicalToLogicalPixels();
		var point = new PointerPoint(frameId, ts, PointerDevice.For(pointerType), pointerId, position, position, isInContact, properties);

		return new PointerEventArgs(point, keyModifiers)
		{
			Handled = nativelyHandled
		};
	}
}
