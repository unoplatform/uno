using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Foundation;
using UIKit;
using Uno.UI.Extensions;

namespace Windows.UI.Xaml
{
	partial class UIElement
	{
		private readonly Lazy<GestureRecognizer> _gestures;

		private GestureRecognizer CreateGestureRecognizer()
		{
			var recognizer = new GestureRecognizer();

			recognizer.Tapped += OnTapRecognized;

			return recognizer;

			void OnTapRecognized(GestureRecognizer sender, TappedEventArgs args)
			{
				if (args.TapCount == 1)
				{
					RaiseEvent(TappedEvent, new TappedRoutedEventArgs(args.PointerDeviceType, args.Position));
				}
				else // i.e. args.TapCount == 2
				{
					RaiseEvent(DoubleTappedEvent, new DoubleTappedRoutedEventArgs(args.PointerDeviceType, args.Position));
				}
			}
		}

		#region Add/Remove handler (This should be moved in the shared file once all platform use the GestureRecognizer)
		partial void AddHandlerPartial(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				// Is greater than 1, it means that we already enabled the setting (and if lower than 0 ... it's weird !)
				ToggleGesture(routedEvent);
			}
		}

		partial void RemoveHandlerPartial(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (remainingHandlersCount == 0)
			{
				ToggleGesture(routedEvent);
			}
		}

		private void ToggleGesture(RoutedEvent routedEvent)
		{
			if (routedEvent == TappedEvent)
			{
				_gestures.Value.GestureSettings |= GestureSettings.Tap;
			}
			else if (routedEvent == DoubleTappedEvent)
			{
				_gestures.Value.GestureSettings |= GestureSettings.DoubleTap;
			}
		}
		#endregion

		#region Native touch handling (i.e. source of the pointer / gesture events)
		public override void TouchesBegan(NSSet touches, UIEvent evt)
		{
			/* Note: Here we have a mismatching behavior with UWP, if the events bubble natively we're going to get
					 (with Ctrl_02 is a child of Ctrl_01):
							Ctrl_02: Entered
									 Pressed
							Ctrl_01: Entered
									 Pressed

					While on UWP we will get:
							Ctrl_02: Entered
							Ctrl_01: Entered
							Ctrl_02: Pressed
							Ctrl_01: Pressed

					However, to fix this is would mean that we handle all events in managed code, but this would
					break lots of control (ScrollViewer) and ability to easily integrate an external component.
			*/

			try
			{
				var isPointerOver = evt.IsTouchInView(this);
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledInManaged = false;
				if (isPointerOver) // TODO: usefull ?
				{
					pointerEventIsHandledInManaged = RaiseNativelyBubbledDown(new PointerRoutedEventArgs(touches, evt, this));
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesBegan(touches, evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void TouchesMoved(NSSet touches, UIEvent evt)
		{
			try
			{
				var isPointerOver = evt.IsTouchInView(this);
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledInManaged = false;
				if (IsPointerCaptured || isPointerOver)
				{
					pointerEventIsHandledInManaged = RaiseNativelyBubbledMove(new PointerRoutedEventArgs(touches, evt, this));
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesMoved(touches, evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void TouchesEnded(NSSet touches, UIEvent evt)
		{
			try
			{
				var wasPointerOver = IsPointerOver;
				var isPointerOver = false;
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledInManaged = false;
				if (IsPointerCaptured || wasPointerOver)
				{
					pointerEventIsHandledInManaged = RaiseNativelyBubbledUp(new PointerRoutedEventArgs(touches, evt, this));
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesEnded(touches, evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}

		public override void TouchesCancelled(NSSet touches, UIEvent evt)
		{
			try
			{
				var wasPointerOver = IsPointerOver;
				var isPointerOver = false;
				IsPointerOver = isPointerOver;

				var pointerEventIsHandledInManaged = false;
				if (IsPointerCaptured || wasPointerOver)
				{
					pointerEventIsHandledInManaged = RaiseNativelyCaptured();
				}

				if (!pointerEventIsHandledInManaged)
				{
					// Continue native bubbling up of the event
					base.TouchesCancelled(touches, evt);
				}
			}
			catch (Exception e)
			{
				Application.Current.RaiseRecoverableUnhandledException(e);
			}
		}
		#endregion

		#region Raise pointer events and gesture recognition (This should be moved in the shared file once all platform use the GestureRecognizer)
		private bool RaiseNativelyBubbledDown(PointerRoutedEventArgs args)
		{
			IsPointerPressed = true;

			args.Handled = false; // reset event
			var handledInManaged = RaiseEvent(PointerEnteredEvent, args);

			args.Handled = false; // reset event
			handledInManaged |= RaiseEvent(PointerPressedEvent, args);

			// Note: We process the DownEvent *after* the Raise(Pressed), so in case of DoubleTapped
			//		 the event is fired after
			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that tey where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessDownEvent(args.GetCurrentPoint(this));
			}

			return handledInManaged;
		}

		private bool RaiseNativelyBubbledMove(PointerRoutedEventArgs args)
		{
			args.Handled = false; // reset event
			var handledInManaged = RaiseEvent(PointerMovedEvent, args);

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that tey where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this));
			}

			return handledInManaged;
		}

		private bool RaiseNativelyBubbledUp(PointerRoutedEventArgs args, bool isPointerLost = false)
		{
			IsPointerPressed = false;

			args.Handled = false; // reset event
			var handledInManaged = RaiseEvent(PointerReleasedEvent, args);

			// Note: We process the UpEvent between Release and Exited as the gestures like "Tap"
			//		 are fired between those events.
			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessUpEvent(args.GetCurrentPoint(this));
			}

			args.Handled = false; // reset event
			handledInManaged |= RaiseEvent(PointerExitedEvent, args);

			// On pointer up (and *after* the exited) we request to release an remaining captures
			ReleasePointerCaptures();

			return handledInManaged;
		}

		private bool RaiseNativelyCaptured()
		{
			// When a pointer is captured, we don't even receive "Released" nor "Exited"

			IsPointerPressed = false;

			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}

			// On pointer up (and *after* the exited) we request to release an remaining captures
			ReleasePointerCaptures();

			return false;
		}
		#endregion

		#region Pointer capture handling
		/*
		 * About pointer capture
		 *
		 * - When a pointer is captured, it will still bubble up, but it will bubble up from the element
		 *   that captured the touch (so the a inner control won't receive it, even if under the pointer !)
		 *   !!! BUT !!! The OriginalSource will still be the inner control!
		 * - Captured are exclusive : first come, first served!
		 * - A control can capture a pointer, even if not under the pointer
		 *
		 *
		 */


		private void ReleasePointerCaptureNative(Pointer value)
		{
		}
		#endregion
	}
}
