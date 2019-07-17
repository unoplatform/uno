using System;
using System.Linq;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Uno.Extensions;
using Uno.Logging;

namespace Windows.UI.Xaml
{
	/*
	 *	This partial file
	 *		- Ensures to raise the right PointerXXX events sequences
	 *		- Handles the gestures events registration, and adjusts the configuration of the GestureRecognizer accordingly
	 *		- Forwards the pointers events to the gesture recognizer and then raise back the recognized gesture events
	 *	
	 *	The API exposed by this file to its native parts are:
	 *		partial void InitializePointersPartial();
	 *		partial void OnManipulationModeChanged(ManipulationModes mode);
	 *		private bool RaiseNativelyBubbledDown(PointerRoutedEventArgs args);
	 *		private bool RaiseNativelyBubbledMove(PointerRoutedEventArgs args);
	 *		private bool RaiseNativelyBubbledUp(PointerRoutedEventArgs args);
	 *		private bool RaiseNativelyBubbledLost(PointerRoutedEventArgs args);
	 *
	 * 	The native components are responsible to subscribe to the native touches events,
	 *	create the corresponding PointerEventArgs and then invoke one of the "RaiseNativelyBubbledXXX" method.
	 *
	 *	This file implements the following from the "RoutedEvents"
	 *		partial void AddHandlerPartial(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
	 * 		partial void RemoveHandlerPartial(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
	 *	and is using:
	 *		internal bool RaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args);
	 */

	partial class UIElement
	{
		#region ManipulationMode (DP)
		public static readonly DependencyProperty ManipulationModeProperty = DependencyProperty.Register(
			"ManipulationMode",
			typeof(ManipulationModes),
			typeof(UIElement),
			new FrameworkPropertyMetadata(ManipulationModes.System, FrameworkPropertyMetadataOptions.None, OnManipulationModeChanged));

		private static void OnManipulationModeChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
		{
			if (snd is UIElement elt)
			{
				elt.OnManipulationModeChanged((ManipulationModes)args.NewValue);
			}
		}

		partial void OnManipulationModeChanged(ManipulationModes mode);

		public ManipulationModes ManipulationMode
		{
			get => (ManipulationModes)this.GetValue(ManipulationModeProperty);
			set => this.SetValue(ManipulationModeProperty, value);
		}
		#endregion

#if __IOS__ // This is temporary until all platforms Pointers have been reworked

		private /* readonly */ Lazy<GestureRecognizer> _gestures;

		// ctor
		private void InitializePointers()
		{
			_gestures = new Lazy<GestureRecognizer>(CreateGestureRecognizer);
			InitializePointersPartial();
		}

		partial void InitializePointersPartial();

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

		#region Add/Remove handler
		partial void AddHandlerPartial(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				// If greater than 1, it means that we already enabled the setting (and if lower than 0 ... it's weird !)
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

		#region Raise pointer events and gesture recognition
		/// <summary>
		/// This should be invoked by the native part of the UIElement when a native touch starts
		/// </summary>
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
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessDownEvent(args.GetCurrentPoint(this));
			}

			return handledInManaged;
		}

		/// <summary>
		/// This should be invoked by the native part of the UIElement when a native pointer moved is received
		/// </summary>
		private bool RaiseNativelyBubbledMove(PointerRoutedEventArgs args)
		{
			args.Handled = false; // reset event
			var handledInManaged = RaiseEvent(PointerMovedEvent, args);

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this));
			}

			return handledInManaged;
		}

		/// <summary>
		/// This should be invoked by the native part of the UIElement when a native pointer up is received
		/// </summary>
		private bool RaiseNativelyBubbledUp(PointerRoutedEventArgs args)
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
			if (_pointCaptures.Count > 0)
			{
				ReleasePointerCaptures();
				args.Handled = false;
				handledInManaged |= RaiseEvent(PointerCaptureLostEvent, args);
			}

			return handledInManaged;
		}

		/// <summary>
		/// This occurs when the pointer is lost (e.g. when captured by a native control like the ScrollViewer)
		/// which prevents us to continue the touches handling.
		/// </summary>
		private bool RaiseNativelyBubbledLost(PointerRoutedEventArgs args)
		{
			// When a pointer is captured, we don't even receive "Released" nor "Exited"

			IsPointerPressed = false;

			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}

			// If the pointer was natively captured, it means that we lost all managed captures
			// Note: Here we should raise either PointerCaptureLost or PointerCancelled depending of the reason which
			//		 drives the system to bubble a lost. However we don't have this kind of information on iOS, and it's
			//		 usually due to the ScrollView which kicks in. So we always raise the CaptureLost which is the behavior
			//		 on UWP when scroll starts (even if no capture are actives at this time).
			ReleasePointerCaptures(); // Note this should raise the CaptureLost only if pointer was effectively captured TODO
			args.Handled = false;
			var handledInManaged = RaiseEvent(PointerCaptureLostEvent, args);

			return handledInManaged;
		}
		#endregion
#else
		private void InitializePointers() { }
#endif

		#region Pointer capture handling
		/*
		 * About pointer capture
		 *
		 * - When a pointer is captured, it will still bubble up, but it will bubble up from the element
		 *   that captured the touch (so the a inner control won't receive it, even if under the pointer !)
		 *   !!! BUT !!! The OriginalSource will still be the inner control!
		 * - Captured are exclusive : first come, first served! (For a given pointer)
		 * - A control can capture a pointer, even if not under the pointer
		 * - The PointersCapture property remains `null` until a pointer is captured
		 */


		public bool CapturePointer(Pointer value)
		{
			if (_pointCaptures.Contains(value))
			{
				this.Log().Error($"{this}: Pointer {value} already captured.");
			}
			else
			{
				_pointCaptures.Add(value);
#if __WASM__
				CapturePointerNative(value);
#endif
			}
			return true;
		}

		public void ReleasePointerCapture(Pointer value)
		{
			if (_pointCaptures.Contains(value))
			{
				_pointCaptures.Remove(value);
#if __WASM__ || __IOS__
				ReleasePointerCaptureNative(value);
#endif
			}
			else
			{
				this.Log().Error($"{this}: Cannot release pointer {value}: not captured by this control.");
			}
		}

		public void ReleasePointerCaptures()
		{
			if (_pointCaptures.Count == 0)
			{
				this.Log().Warn($"{this}: no pointers to release.");
				return;
			}
#if __WASM__ || __IOS__
			foreach (var pointer in _pointCaptures)
			{
				ReleasePointerCaptureNative(pointer);
			}
#endif
			_pointCaptures.Clear();
		}
		#endregion
	}
}
