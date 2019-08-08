using System;
using System.Diagnostics;
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
	 *		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer);
	 *
	 *		partial void OnManipulationModeChanged(ManipulationModes mode);
	 *
	 *		private bool OnNativePointerEnter(PointerRoutedEventArgs args);
	 *		private bool OnNativePointerDown(PointerRoutedEventArgs args);
	 *		private bool OnNativePointerMove(PointerRoutedEventArgs args);
	 *		private bool OnNativePointerUp(PointerRoutedEventArgs args);
	 *		private bool OnNativePointerExited(PointerRoutedEventArgs args);
	 *		private bool OnNativePointerCancel(PointerRoutedEventArgs args, bool isSwallowedBySystem)
	 *
	 * 	The native components are responsible to subscribe to the native touches events,
	 *	create the corresponding PointerEventArgs and then invoke one (or more) of the "OnNative***" methods.
	 *
	 *	This file implements the following from the "RoutedEvents"
	 *		partial void AddGestureHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
	 * 		partial void RemoveGestureHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
	 *	and is using:
	 *		internal bool RaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args);
	 */

	partial class UIElement
	{
		#region ManipulationMode (DP)
		public static DependencyProperty ManipulationModeProperty { get; } = DependencyProperty.Register(
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

		#region IsPointerPressed (Internal property with overridable callback)
		private bool _isPointerPressed;

		/// <summary>
		/// Indicates if a pointer was pressed while over the element (i.e. PressedState)
		/// </summary>
		internal bool IsPointerPressed
		{
			get => _isPointerPressed;
			set // TODO: This should be private, but we need to update all controls that are setting
			{
				if (_isPointerPressed != value)
				{
					_isPointerPressed = value;
					OnIsPointerPressedChanged(value);
				}
			}
		}

		internal virtual void OnIsPointerPressedChanged(bool isPointerPressed)
		{
		}
		#endregion

		#region IsPointerOver (Internal property with overridable callback)
		private bool _isPointerOver;

		/// <summary>
		/// Indicates if a pointer (no matter the pointer) is currently over the element (i.e. OverState)
		/// </summary>
		internal bool IsPointerOver
		{
			get => _isPointerOver;
			set // TODO: This should be private, but we need to update all controls that are setting
			{
				if (_isPointerOver != value)
				{
					_isPointerOver = value;
					OnIsPointerOverChanged(value);
				}
			}
		}

		internal virtual void OnIsPointerOverChanged(bool isPointerOver)
		{
		}
		#endregion

#if __IOS__ || __WASM__ // This is temporary until all platforms Pointers have been reworked

		private /* readonly but partial */ Lazy<GestureRecognizer> _gestures;

		// ctor
		private void InitializePointers()
		{
			_gestures = new Lazy<GestureRecognizer>(CreateGestureRecognizer);
			InitializePointersPartial();
		}

		partial void InitializePointersPartial();

		#region Gestures handling
		private GestureRecognizer CreateGestureRecognizer()
		{
			var recognizer = new GestureRecognizer();

			recognizer.Tapped += OnTapRecognized;

			// Allow partial parts to subscribe to pointer events (WASM)
			OnGestureRecognizerInitialized(recognizer);

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

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer);

		partial void AddGestureHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				// If greater than 1, it means that we already enabled the setting (and if lower than 0 ... it's weird !)
				ToggleGesture(routedEvent);
			}
		}

		partial void RemoveGestureHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
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

#if __IOS__ || __WASM__
		#region Pointer states (Usually updated by the partial API OnNative***)
		internal bool IsOver(Pointer pointer) => IsPointerOver;
		internal bool IsPressed(Pointer pointer) => IsPointerPressed;

		private bool SetOver(PointerRoutedEventArgs args, bool isOver, bool isPointerCancelled = false)
		{
			var wasOver = IsPointerOver;
			IsPointerOver = isOver;

			if (isPointerCancelled
				|| wasOver == isOver) // nothing changed
			{
				return false;
			}

			if (isOver) // Entered
			{
				args.Handled = false;
				return RaiseEvent(PointerEnteredEvent, args);
			}
			else // Exited
			{
				args.Handled = false;
				return RaiseEvent(PointerExitedEvent, args);
			}
		}

		private bool SetPressed(PointerRoutedEventArgs args, bool isPressed, bool isPointerCancelled = false)
		{
			var wasPressed = IsPointerPressed;
			IsPointerPressed = isPressed;

			if (isPointerCancelled
				|| wasPressed == isPressed) // nothing changed
			{
				return false;
			}

			if (isPressed) // Pressed
			{
				args.Handled = false;
				return RaiseEvent(PointerPressedEvent, args);
			}
			else // Released
			{
				args.Handled = false;
				return RaiseEvent(PointerReleasedEvent, args);
			}
		}

		private bool ReleaseCaptures(PointerRoutedEventArgs args, bool forceCaptureLostEvent = false)
		{
			if (_pointCaptures.Count > 0)
			{
				ReleasePointerCaptures();
				args.Handled = false;
				return RaiseEvent(PointerCaptureLostEvent, args);
			}
			else if (forceCaptureLostEvent)
			{
				return RaiseEvent(PointerCaptureLostEvent, args);
			}
			else
			{
				return false;
			}
		}
		#endregion

		#region Partial API to raise pointer events and gesture recognition (OnNative***)
		private bool OnNativePointerEnter(PointerRoutedEventArgs args)
		{
			return SetOver(args, true);
		}

		private bool OnNativePointerDown(PointerRoutedEventArgs args)
		{
			var handledInManaged = SetPressed(args, true);

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessDownEvent(args.GetCurrentPoint(this));
			}

			return handledInManaged;
		}

		private bool OnNativePointerMove(PointerRoutedEventArgs args)
		{
			var handledInManaged = false;
			var isOver = IsOver(args.Pointer);
			var isCaptured = IsCaptured(args.Pointer);

			// We are receiving an unexpected move for this pointer on this element,
			// we mute it to avoid invalid event sequence.
			// Notes:
			//   iOS:  This may happen on iOS where the pointers are implicitly captured.
			//   WASM: On wasm, if this check mutes your event, it's because you didn't received the "pointerenter" (not bubbling natively).
			//         This is usually because your control is covered by an element which is IsHitTestVisible == true / has transparent background.
			var isIrrelevant = !isOver && !isCaptured;
			if (isIrrelevant)
			{
				return handledInManaged; // Always false
			}

			args.Handled = false;
			handledInManaged = RaiseEvent(PointerMovedEvent, args);

			// 4. Process gestures
			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this));
			}

			return handledInManaged;
		}

		private bool OnNativePointerUp(PointerRoutedEventArgs args)
		{
			var handledInManaged = false;
			var isOver = IsOver(args.Pointer);
			var isCaptured = IsCaptured(args.Pointer);

			// we are receiving an unexpected up for this pointer on this control, handle it a cancel event in order to properly
			// update the state without raising invalid events (this is the case on iOS which implicitly captures pointers).
			var isIrrelevant = !isOver && !isCaptured; 

			handledInManaged |= SetPressed(args, false, isPointerCancelled: isIrrelevant);

			if (isIrrelevant)
			{
				return handledInManaged; // always false as SetPressed with isPointerCancelled==true always returns false;
			}

			// Note: We process the UpEvent between Release and Exited as the gestures like "Tap"
			//		 are fired between those events.
			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessUpEvent(args.GetCurrentPoint(this));
			}

			// We release the captures on up but only when pointer is not over the control (i.e. mouse that moved away)
			if (!isOver) // so isCaptured == true
			{
				handledInManaged |= ReleaseCaptures(args);
			}

			return handledInManaged;
		}

		private bool OnNativePointerExited(PointerRoutedEventArgs args)
		{
			var handledInManaged = false;

			handledInManaged |= SetOver(args, false);

			// We release the captures on exit when pointer is not pressed the control
			// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
			if (!IsPressed(args.Pointer))
			{
				handledInManaged |= ReleaseCaptures(args);
			}

			return handledInManaged;
		}

		/// <summary>
		/// When the system cancel a pointer pressed, either
		/// 1. because the pointing device was lost/disconnected,
		/// 2. or the system detected something meaning full and will handle this pointer internally.
		/// This second case is the more common (e.g. ScrollViewer) and should be indicated using the <paramref name="isSwallowedBySystem"/> flag.
		/// </summary>
		/// <param name="isSwallowedBySystem">Indicates that the pointer was muted by the system which will handle it internally.</param>
		private bool OnNativePointerCancel(PointerRoutedEventArgs args, bool isSwallowedBySystem)
		{
			var handledInManaged = false;
			var isOver = IsOver(args.Pointer);
			var isCaptured = IsCaptured(args.Pointer);

			// we are receiving an unexpected up for this pointer on this control, handle it a cancel event in order to properly
			// update the state without raising invalid events (this is the case on iOS which implicitly captures pointers).
			var isIrrelevant = !isOver && !isCaptured;

			// When a pointer is cancelled / swallowed by the system, we don't even receive "Released" nor "Exited"
			SetPressed(args, false, isPointerCancelled: true);
			SetOver(args, false, isPointerCancelled: true);

			if (isIrrelevant)
			{
				return handledInManaged; // always false
			}
		
			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}

			if (isSwallowedBySystem)
			{
				handledInManaged |= ReleaseCaptures(args, forceCaptureLostEvent: true);
			}
			else
			{
				args.Handled = false;
				handledInManaged |= RaiseEvent(PointerCanceledEvent, args);
				handledInManaged |= ReleaseCaptures(args);
			}

			return handledInManaged;
		}
		#endregion
#endif
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

		internal bool IsCaptured(Pointer pointer) => _pointCaptures.Any();

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
