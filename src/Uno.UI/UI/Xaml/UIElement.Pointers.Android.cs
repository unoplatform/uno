using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Android.Runtime;
using Android.Views;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private class TouchReRouter : Java.Lang.Object, View.IOnTouchListener
		{
			private readonly UIElement _target;

			public TouchReRouter(IntPtr handle, JniHandleOwnership transfer)
				: base(handle, transfer)
			{
			}

			private TouchReRouter(UIElement target)
				: base(IntPtr.Zero, JniHandleOwnership.DoNotRegister)
			{
				_target = target;
			}

			/// <inheritdoc />
			public bool OnTouch(View view, MotionEvent nativeEvent)
				=> _target.OnNativeMotionEvent(nativeEvent, view, true);
		}


		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				IsNativeMotionEventsEnabled = true;
			}
		}

		partial void RemovePointerHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (remainingHandlersCount == 0)
			{
				// TODO: Disable pointer events reporting (https://github.com/unoplatform/uno/issues/1806)
			}
		}

		protected sealed override bool OnNativeMotionEvent(MotionEvent nativeEvent, View originalSource, bool isInView)
		{
			if (IsPointersSuspended)
			{
				return false;
			}

			if (!(originalSource is UIElement srcElement))
			{
				this.Log().Error("Invalid OriginalSource in OnNativeMotionEvent, fall-backing to the receiver");
				srcElement = this;
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

				var handled = false;
				for (var pointerIndex = 0; pointerIndex < pointerCount; pointerIndex++)
				{
					var args = new PointerRoutedEventArgs(nativeEvent, pointerIndex, srcElement, this);
					var argsAction = MotionEventActions.Move;

					handled |= OnNativeMotionEvent(nativeEvent, args, argsAction, isInView);
				}

				return handled;
			}
			else
			{
				var args = new PointerRoutedEventArgs(nativeEvent, nativeEvent.ActionIndex, srcElement, this);
				var argsAction = actionMasked;

				return OnNativeMotionEvent(nativeEvent, args, argsAction, isInView);
			}
		}

		private bool OnNativeMotionEvent(MotionEvent nativeEvent, PointerRoutedEventArgs args, MotionEventActions action, bool isInView)
		{
			// Warning: MotionEvent of other kinds are filtered out in native code (UnoMotionHelper.java)
			switch (action)
			{
				case MotionEventActions.HoverEnter:
					return OnNativePointerEnter(args);
				case MotionEventActions.HoverExit when !args.Pointer.IsInContact:
					// When a mouse button is pressed or pen touches the screen (a.k.a. becomes in contact), we receive an HoverExit before the Down.
					// We validate here if pointer 'isInContact' (which is the case for HoverExit when mouse button pressed / pen touch  the screen)
					// and we ignore them (as on UWP Exit is raised only when pointer moves out of bounds of the control, no matter the pressed state).
					// As a side effect we will have to update the hover state on each Move in order to handle the case of press -> move out -> release.
					return OnNativePointerExited(args);
				case MotionEventActions.HoverExit:
					return false; // avoid useless logging

				case MotionEventActions.Down when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerDown when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					return OnNativePointerEnter(args) | OnNativePointerDown(args);
				case PointerRoutedEventArgs.StylusWithBarrelDown:
				case MotionEventActions.Down:
				case MotionEventActions.PointerDown:
					return OnNativePointerDown(args);
				case MotionEventActions.Up when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerUp when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					return OnNativePointerUp(args) | OnNativePointerExited(args);
				case PointerRoutedEventArgs.StylusWithBarrelUp:
				case MotionEventActions.Up:
				case MotionEventActions.PointerUp:
					return OnNativePointerUp(args);

				// We get ACTION_DOWN and ACTION_UP only for "left" button, and instead we get a HOVER_MOVE when pressing/releasing the right button of the mouse.
				// So on each POINTER_MOVE we make sure to update the pressed state if it does not match.
				// Note: We can also have HOVER_MOVE with barrel button pressed, so we make sure to "PointerDown" only for Mouse.
				case MotionEventActions.HoverMove when args.Pointer.PointerDeviceType == PointerDeviceType.Mouse && args.HasPressedButton && !IsPressed(args.Pointer):
					return OnNativePointerDown(args) | OnNativePointerMoveWithOverCheck(args, isInView);
				case MotionEventActions.HoverMove when !args.HasPressedButton && IsPressed(args.Pointer):
					return OnNativePointerUp(args) | OnNativePointerMoveWithOverCheck(args, isInView);

				case PointerRoutedEventArgs.StylusWithBarrelMove:
				case MotionEventActions.Move:
				case MotionEventActions.HoverMove:
					// Note: We use the OnNativePointerMove**WithOverCheck** in order to update the over state in case of press -> move out -> release
					//		 where Android won't raise the HoverExit (as it has raised it on press, but we have ignored it cf. HoverExit case.)
					return OnNativePointerMoveWithOverCheck(args, isInView);

				case MotionEventActions.Cancel:
					return OnNativePointerCancel(args, isSwallowedBySystem: true);

				default:
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"We receive a native motion event of '{action}', but this is not supported and should have been filtered out in native code.");
					}

					return false;
			}
		}

		partial void OnManipulationModeChanged(ManipulationModes oldMode, ManipulationModes newMode)
			=> IsNativeMotionEventsInterceptForbidden = newMode == ManipulationModes.None;

		#region Capture
		// No needs to explicitly capture pointers on Android, they are implicitly captured
		// partial void CapturePointerNative(Pointer pointer);
		// partial void ReleasePointerNative(Pointer pointer);
		#endregion

		partial void OnIsHitTestVisibleChangedPartial(bool oldValue, bool newValue)
		{
			base.SetNativeIsHitTestVisible(newValue);
		}

		// This section is using the UnoViewGroup overrides for performance reasons
		// where most of the work is performed on the java side.

		protected override bool NativeHitCheck()
			=> IsViewHit();
	}
}
