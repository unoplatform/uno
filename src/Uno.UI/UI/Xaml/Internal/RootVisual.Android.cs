#nullable enable

using Android.Views;
using Windows.Devices.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Extensions;
using Uno.Foundation.Logging;

namespace Uno.UI.Xaml.Core;

internal partial class RootVisual
{
	protected sealed override bool OnNativeMotionEvent(MotionEvent nativeEvent)
	{
		if (!ArePointersEnabled)
		{
			return false;
		}

		var pointerCount = nativeEvent.PointerCount;
		var action = nativeEvent.Action;
		var actionMasked = action & MotionEventActions.Mask;
		if (pointerCount > 1 && actionMasked == MotionEventActions.Move)
		{
			// When we get a move, we make sure to raise the move for all pointers.
			// Note: We could probably also raise a move for pointers other than ActionIndex for other actions
			//		 but as multi touch is only for fingers, we get a lot of events (due to the approx.) and it's
			//		 safer to not try to over-interpret events.

			for (var pointerIndex = 0; pointerIndex < pointerCount; pointerIndex++)
			{
				var args = nativeEvent.ToPointerEventArgs(pointerIndex);
				var argsAction = MotionEventActions.Move;

				OnNativeMotionEvent(nativeEvent, args, argsAction);
			}
		}
		else
		{
			var args = nativeEvent.ToPointerEventArgs(nativeEvent.ActionIndex);
			var argsAction = actionMasked;

			OnNativeMotionEvent(nativeEvent, args, argsAction);
		}

		return true;
	}

	private void OnNativeMotionEvent(MotionEvent nativeEvent, PointerEventArgs args, MotionEventActions action)
	{
		var ownerEvents = (ICoreWindowEvents)CoreWindow.GetForCurrentThread()!;

		// Warning: MotionEvent of other kinds are filtered out in native code (UnoMotionHelper.java)
		switch (action)
		{
			case MotionEventActions.HoverEnter:
				ownerEvents.RaisePointerEntered(args);
				break;
			case MotionEventActions.HoverExit when !args.CurrentPoint.IsInContact:
				// When a mouse button is pressed or pen touches the screen (a.k.a. becomes in contact), we receive an HoverExit before the Down.
				// We validate here if pointer 'isInContact' (which is the case for HoverExit when mouse button pressed / pen touched the screen)
				// and we ignore them (as on UWP Exit is raised only when pointer moves out of bounds of the control, no matter the pressed state).
				// As a side effect we will have to update the hover state on each Move in order to handle the case of press -> move out -> release.
				ownerEvents.RaisePointerExited(args);
				break;
			case MotionEventActions.HoverExit:
				break; // avoid useless logging

			case MotionEventActions.Down when args.CurrentPoint.PointerDeviceType == PointerDeviceType.Touch:
			case MotionEventActions.PointerDown when args.CurrentPoint.PointerDeviceType == PointerDeviceType.Touch:
				ownerEvents.RaisePointerEntered(args); // We don't have any enter / exit on Android for touches, so we explicitly generate one on down / up.
				ownerEvents.RaisePointerPressed(args);
				break;
			case MotionEventExtensions.StylusWithBarrelDown:
			case MotionEventActions.Down:
			case MotionEventActions.PointerDown:
				ownerEvents.RaisePointerPressed(args);
				break;
			case MotionEventActions.Up when args.CurrentPoint.PointerDeviceType == PointerDeviceType.Touch:
			case MotionEventActions.PointerUp when args.CurrentPoint.PointerDeviceType == PointerDeviceType.Touch:
				ownerEvents.RaisePointerReleased(args);
				ownerEvents.RaisePointerExited(args); // We don't have any enter / exit on Android for touches, so we explicitly generate one on down / up.


				break;
			case MotionEventExtensions.StylusWithBarrelUp:
			case MotionEventActions.Up:
			case MotionEventActions.PointerUp:
				ownerEvents.RaisePointerReleased(args);
				break;

			// We get ACTION_DOWN and ACTION_UP only for "left" button, and instead we get a HOVER_MOVE when pressing/releasing the right button of the mouse.
			// So on each POINTER_MOVE we make sure to update the pressed state if it does not match.
			// Note: We can also have HOVER_MOVE with barrel button pressed, so we make sure to "PointerDown" only for Mouse.
			case MotionEventActions.HoverMove when args.CurrentPoint.PointerDeviceType == PointerDeviceType.Mouse && args.CurrentPoint.Properties.HasPressedButton && !IsPressed(args.CurrentPoint.PointerId):
				// TODO: As we are now tracking that at root, we can probably detect this case is a clever way
				ownerEvents.RaisePointerPressed(args);
				ownerEvents.RaisePointerMoved(args);
				break;
			case MotionEventActions.HoverMove when !args.CurrentPoint.Properties.HasPressedButton && IsPressed(args.CurrentPoint.PointerId):
				// TODO: As we are now tracking that at root, we can probably detect this case is a clever way
				ownerEvents.RaisePointerReleased(args);
				ownerEvents.RaisePointerMoved(args); // TODO: Is this still relevant ???? TODO: Why after the Released ???
				break;

			case MotionEventExtensions.StylusWithBarrelMove:
			case MotionEventActions.Move:
			case MotionEventActions.HoverMove:
				// Note: We use the OnNativePointerMove**WithOverCheck** in order to update the over state in case of press -> move out -> release
				//		 where Android won't raise the HoverExit (as it has raised it on press, but we have ignored it cf. HoverExit case.)
				ownerEvents.RaisePointerMoved(args);
				break;

			case MotionEventActions.Cancel:
				ownerEvents.RaisePointerCancelled(args); // TODO: isSwallowedBySystem: true
				break;

			default:
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"We receive a native motion event of '{action}', but this is not supported and should have been filtered out in native code.");
				}
				break;
		}
	}
}
