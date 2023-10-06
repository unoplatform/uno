using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Android.Runtime;
using Android.Views;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Extensions;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
using Windows.Devices.Input;
#endif

namespace Microsoft.UI.Xaml
{
	partial class UIElement
	{
		partial void InitializePointersPartial()
		{
			ArePointersEnabled = true;
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
			if (!ArePointersEnabled)
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
					// We validate here if pointer 'isInContact' (which is the case for HoverExit when mouse button pressed / pen touched the screen)
					// and we ignore them (as on UWP Exit is raised only when pointer moves out of bounds of the control, no matter the pressed state).
					// As a side effect we will have to update the hover state on each Move in order to handle the case of press -> move out -> release.
					return OnNativePointerExited(args);
				case MotionEventActions.HoverExit:
					return false; // avoid useless logging

				case MotionEventActions.Down when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerDown when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					// We don't have any enter / exit on Android for touches, so we explicitly generate one on down / up.
					// That event args is requested to bubble in managed code only (args.CanBubbleNatively = false),
					// so we follow the same sequence as UWP (the whole tree gets entered before the pressed),
					// and we make sure that the event will bubble through the whole tree, no matter if the Pressed event is handle or not.
					// Note: Parents will also try to raise the "Enter" but they will be silent since the pointer is already considered as pressed.
					args.CanBubbleNatively = false;
					OnNativePointerEnter(args);
					return OnNativePointerDown(args.Reset());
				case PointerRoutedEventArgs.StylusWithBarrelDown:
				case MotionEventActions.Down:
				case MotionEventActions.PointerDown:
					return OnNativePointerDown(args);
				case MotionEventActions.Up when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
				case MotionEventActions.PointerUp when args.Pointer.PointerDeviceType == PointerDeviceType.Touch:
					// For touch pointer, in the RootVisual we will redispatch this event to raise exit,
					// but if the event has been handled, we need to raise it after the 'up' has been processed.
					if (OnNativePointerUp(args))
					{
						if (WinUICoreServices.Instance.MainRootVisual is not IRootElement rootElement)
						{
							rootElement = XamlRoot?.VisualTree.RootElement as IRootElement;
						}
						rootElement?.RootElementLogic.ProcessPointerUp(args, isAfterHandledUp: true);
						return true;
					}
					else
					{
						return false;
					}
				case PointerRoutedEventArgs.StylusWithBarrelUp:
				case MotionEventActions.Up:
				case MotionEventActions.PointerUp:
					return OnNativePointerUp(args);

				// We get ACTION_DOWN and ACTION_UP only for "left" button, and instead we get a HOVER_MOVE when pressing/releasing the right button of the mouse.
				// So on each POINTER_MOVE we make sure to update the pressed state if it does not match.
				// Note: We can also have HOVER_MOVE with barrel button pressed, so we make sure to "PointerDown" only for Mouse.
				case MotionEventActions.HoverMove when args.Pointer.PointerDeviceType == PointerDeviceType.Mouse && args.HasPressedButton && !IsPressed(args.Pointer):
					return OnNativePointerDown(args) | OnNativePointerMoveWithOverCheck(args.Reset(), isInView);
				case MotionEventActions.HoverMove when !args.HasPressedButton && IsPressed(args.Pointer):
					return OnNativePointerUp(args) | OnNativePointerMoveWithOverCheck(args.Reset(), isInView);

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

		/// <summary>
		/// Used by the VisualRoot to redispatch a pointer exit on pointer up
		/// </summary>
		/// <param name="args"></param>
		internal void RedispatchPointerExited(PointerRoutedEventArgs args)
			=> OnNativePointerExited(args.Reset(canBubbleNatively: false));

		partial void OnManipulationModeChanged(ManipulationModes oldMode, ManipulationModes newMode)
			=> IsNativeMotionEventsInterceptForbidden = newMode == ManipulationModes.None;

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer)
		{
			recognizer.ManipulationConfigured += (snd, manip) =>
			{
				var scrollableDirection = this
					.GetAllParents()
					.Aggregate(
						(h: false, v: false),
						(direction, parent) =>
							parent switch
							{
								ScrollContentPresenter scp => (direction.h || scp.CanHorizontallyScroll, direction.v || scp.CanVerticallyScroll),
								IScrollContentPresenter iscp => (direction.h || iscp.CanHorizontallyScroll, direction.v || iscp.CanVerticallyScroll),
								_ => direction
							});

				if ((scrollableDirection.h && manip.IsTranslateXEnabled)
					|| (scrollableDirection.v && manip.IsTranslateYEnabled))
				{
					RequestDisallowInterceptTouchEvent(true);
				}
			};
			recognizer.ManipulationStarted += (snd, args) =>
			{
				RequestDisallowInterceptTouchEvent(true);
			};

			// The manipulation can be aborted by the user before the pointer up, so the auto release on pointer up is not enough
			recognizer.ManipulationCompleted += (snd, args) =>
			{
				if (ManipulationMode != ManipulationModes.None)
				{
					RequestDisallowInterceptTouchEvent(false);
				}
			};
			recognizer.ManipulationAborted += (snd, args) =>
			{
				if (ManipulationMode != ManipulationModes.None)
				{
					RequestDisallowInterceptTouchEvent(false);
				}
			};

			// This event means that the touch was long enough and any move will actually start the manipulation,
			// so we use "Started" instead of "Starting"
			recognizer.DragReady += (snd, manip) =>
			{
				RequestDisallowInterceptTouchEvent(true);
			};
			recognizer.Dragging += (snd, args) =>
			{
				switch (args.DraggingState)
				{
					case DraggingState.Started:
						RequestDisallowInterceptTouchEvent(true); // Still usefull for mouse and pen
						break;
					case DraggingState.Completed when ManipulationMode != ManipulationModes.None:
						RequestDisallowInterceptTouchEvent(false);
						break;
				}
			};
		}

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
