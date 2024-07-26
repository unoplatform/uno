using System;
using Android.Views;
using Microsoft.UI.Input;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Extensions;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using PointerEventArgs = Windows.UI.Core.PointerEventArgs;

#if !HAS_UNO_WINUI
using Windows.UI.Input;
#endif

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidCorePointerInputSource : IUnoCorePointerInputSource
{
	public static AndroidCorePointerInputSource Instance { get; } = new();

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

	internal bool OnNativeMotionEvent(MotionEvent e, int[] correction, bool nativelyHandled)
	{
		try
		{
			var pointerIndex = 0; // TODO: ?
			var nativePointerType = e.GetToolType(pointerIndex);
			var pointerType = nativePointerType.ToPointerDeviceType();
			var pointerDevice = PointerDevice.For(pointerType);
			var pointerIdentifier = new PointerIdentifier(pointerType, id: 0);

			var nativePointerAction = e.Action;
			var nativePointerButtons = e.ButtonState;
			var frameId = (uint)e.EventTime;
			var ts = (ulong)(TimeSpan.TicksPerMillisecond * frameId);
			var isInContact = PointerHelpers.IsInContact(e, pointerType, nativePointerAction, nativePointerButtons);
			var isInRange = true; // TODO: ?
			var keyModifiers = e.MetaState.ToVirtualKeyModifiers();
			var x = e.GetX();
			var y = e.GetY();
			var position = new Point((int)x - correction[0], (int)y - correction[1]).PhysicalToLogicalPixels();

			var properties = PointerHelpers.GetProperties(e, pointerIndex, nativePointerType, nativePointerAction, nativePointerButtons, isInRange, isInContact);

			var point = new PointerPoint(frameId, ts, pointerDevice, pointerIdentifier.Id, position, position, isInContact, new PointerPointProperties(properties));
			var args = new PointerEventArgs(point, keyModifiers)
			{
				Handled = nativelyHandled
			};

			switch (nativePointerAction)
			{
				case MotionEventActions.HoverEnter when pointerType == Windows.Devices.Input.PointerDeviceType.Touch:
				case MotionEventActions.HoverExit when pointerType == Windows.Devices.Input.PointerDeviceType.Touch:
					// We get HoverEnter and HoverExit for touch only when TalkBack is enabled.
					// We ignore these events.
					break;

				case MotionEventActions.HoverEnter:
					PointerEntered?.Invoke(this, args);
					break;

				case MotionEventActions.HoverExit when !isInContact:
					// When a mouse button is pressed or pen touches the screen (a.k.a. becomes in contact), we receive an HoverExit before the Down.
					// We validate here if pointer 'isInContact' (which is the case for HoverExit when mouse button pressed / pen touched the screen)
					// and we ignore them (as on UWP Exit is raised only when pointer moves out of bounds of the control, no matter the pressed state).
					// As a side effect we will have to update the hover state on each Move in order to handle the case of press -> move out -> release.
					PointerExited?.Invoke(this, args);
					break;

				case MotionEventActions.HoverExit:
					break;

				case MotionEventActions.Move:
					PointerMoved?.Invoke(this, args);
					break;

				case MotionEventActions.Down:
				case MotionEventActions.PointerDown:
					if (pointerType == Windows.Devices.Input.PointerDeviceType.Touch)
					{
						PointerEntered?.Invoke(this, args);
					}

					PointerPressed?.Invoke(this, args);
					break;

				case MotionEventActions.Cancel:
				case MotionEventActions.Up:
					PointerReleased?.Invoke(this, args);

					if (pointerType == Windows.Devices.Input.PointerDeviceType.Touch)
					{
						PointerExited?.Invoke(this, args);
					}
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(e), $"Unknown event ({e}-{nativePointerAction}).");
			}

			return args.Handled;
		}
		catch (Exception error)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"Failed to dispatch native pointer event: {error}");
			}

			return false;
		}
	}
}
