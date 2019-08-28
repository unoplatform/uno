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
				// TODO: Disable pointer events reporting
			}
		}

		protected sealed override bool OnNativeMotionEvent(MotionEvent nativeEvent, View originalSource, bool isInView)
		{
			if (nativeEvent.PointerCount > 1)
			{
				this.Log().Error("Multi touches is not supported yet by UNO. You will receive only event only for the first pointer.");
			}

			if (!(originalSource is UIElement srcElement))
			{
				this.Log().Error("Invalid OriginalSource in OnNativeMotionEvent, fall-backing to the receiver");
				srcElement = this;
			}

			var args = new PointerRoutedEventArgs(nativeEvent, srcElement, this);

			// Warning: MotionEvent of other kinds are filtered out in native code (UnoMotionHelper.java)
			switch (nativeEvent.ActionMasked)
			{
				case MotionEventActions.HoverEnter:
					return OnNativePointerEnter(args);
				case MotionEventActions.HoverExit when nativeEvent.ButtonState == 0:
					// When a mouse button is pressed, we receive an HoverExit the the Down. As on UWP Exit is raised only when pointer moves
					// out of bounds of the control, we ignore the HoverExit when a button is pressed, and then update the Over state on each Move.
					return OnNativePointerExited(args);
				case MotionEventActions.HoverExit:
					return false; // avoid useless logging

				case MotionEventActions.Down when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerDown when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					return OnNativePointerEnter(args) | OnNativePointerDown(args);
				case MotionEventActions.Down:
				case MotionEventActions.PointerDown:
					return OnNativePointerDown(args);
				case MotionEventActions.Up when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerUp when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					return OnNativePointerUp(args) | OnNativePointerExited(args);
				case MotionEventActions.Up:
				case MotionEventActions.PointerUp:
					return OnNativePointerUp(args);

				case MotionEventActions.Move:
				case MotionEventActions.HoverMove:
					// As the HoverExit is raised when pointer is pressed (and we are filtering them out),
					// if the user moves the pointer out (still while pressing the button), we won't receive the OverExit.
					// However if the pointer was captured, we have to raise the PointerExit, so make sure update the Over state before raising the move.
					return SetOver(args, isInView) | OnNativePointerMove(args);

				case MotionEventActions.Cancel:
					return OnNativePointerCancel(args, isSwallowedBySystem: true);

				default:
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"We receive a native motion event of '{nativeEvent.ActionMasked}', but this is not supported and should have been filtered out in native code.");
					}
					return false;
			}
		}

		partial void OnManipulationModeChanged(ManipulationModes newMode)
			=> IsNativeMotionEventsInterceptForbidden = newMode != ManipulationModes.System;

		#region Capture
		// No needs to explicitly capture pointers on Android, they are implicitly captured
		// partial void CapturePointerNative(Pointer pointer);
		// partial void ReleasePointerCaptureNative(Pointer pointer);

		// TODO: For the capture RequestDisallowInterceptTouchEvent
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
