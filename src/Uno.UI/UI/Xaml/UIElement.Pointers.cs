using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
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
				var oldMode = (ManipulationModes)args.OldValue;
				var newMode = (ManipulationModes)args.NewValue;

				if (!newMode.IsSupported()
					&& snd.Log().IsEnabled(LogLevel.Warning))
				{
					snd.Log().Warn(
						$"The ManipulationMode '{newMode}' is not supported by Uno. "
						+ "Only 'None', 'All' and 'System' are supported, setting any other mode will be handled as 'All'. "
						+ "Note that with Uno the 'All' and 'System' are handled the same way.");
				}

				elt.OnManipulationModeChanged(oldMode, newMode);
			}
		}

		partial void OnManipulationModeChanged(ManipulationModes oldMode, ManipulationModes newMode);

		public ManipulationModes ManipulationMode
		{
			get => (ManipulationModes)this.GetValue(ManipulationModeProperty);
			set => this.SetValue(ManipulationModeProperty, value);
		}
		#endregion

		private /* readonly but partial */ Lazy<GestureRecognizer> _gestures;

		// ctor
		private void InitializePointers()
		{
			// Setting MotionEventSplittingEnabled to false makes sure that for multi-touches, all the pointers are
			// be dispatched on the same target (the one on which the first pressed occured).
			// This is **not** the behavior of windows which dispatches each pointer to the right target,
			// so we keep the initial value which is true.
			// MotionEventSplittingEnabled = true;

			_gestures = new Lazy<GestureRecognizer>(CreateGestureRecognizer);

			InitializePointersPartial();
			if (this is FrameworkElement fwElt)
			{
				fwElt.Unloaded += OnPointersUnloaded;
			}
		}

		partial void InitializePointersPartial();

		private static readonly RoutedEventHandler OnPointersUnloaded = (object sender, RoutedEventArgs args) =>
		{
			if (sender is UIElement elt)
			{
				elt.ReleasePointerCaptures();
			}
		};

		#region Gestures recognition
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

		#region Partial API to raise pointer events and gesture recognition (OnNative***)
		private bool OnNativePointerEnter(PointerRoutedEventArgs args)
		{
			// We override the isOver for the relevancy check as we will update it right after.
			var isIrrelevant = ValidateAndUpdateCapture(args, isOver: true);
			var handledInManaged = SetOver(args, true, muteEvent: isIrrelevant);

			return handledInManaged;
		}

		private bool OnNativePointerDown(PointerRoutedEventArgs args)
		{
			// "forceRelease: true": as we are in pointer pressed, if the pointer is already captured,
			// it due to an invalid state. So here we make sure to not stay in an invalid state that would
			// prevent any interaction with the application.
			var isIrrelevant = ValidateAndUpdateCapture(args, isOver: true, forceRelease: true);
			var handledInManaged = SetPressed(args, true, muteEvent: isIrrelevant);

			if (isIrrelevant)
			{
				return handledInManaged; // always false, as the event was mute
			}

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessDownEvent(args.GetCurrentPoint(this));
			}

			return handledInManaged;
		}

		// This is for iOS and Android which not raising the Exit properly and for which we have to re-compute the over state for each move
		private bool OnNativePointerMoveWithOverCheck(PointerRoutedEventArgs args, bool isOver)
		{
			var handledInManaged = false;
			var isIrrelevant = ValidateAndUpdateCapture(args, isOver);

			handledInManaged |= SetOver(args, true, muteEvent: isIrrelevant);

			if (isIrrelevant)
			{
				return handledInManaged; // always false, as the event was mute
			}

			args.Handled = false;
			handledInManaged |= RaiseEvent(PointerMovedEvent, args);

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this));
			}

			return handledInManaged;
		}

		private bool OnNativePointerMove(PointerRoutedEventArgs args)
		{
			var isIrrelevant = ValidateAndUpdateCapture(args);

			if (isIrrelevant)
			{
				return false;
			}

			args.Handled = false;
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

		private bool OnNativePointerUp(PointerRoutedEventArgs args)
		{
			var handledInManaged = false;
			var isIrrelevant = ValidateAndUpdateCapture(args, out var isOver);

			handledInManaged |= SetPressed(args, false, muteEvent: isIrrelevant);

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
			if (!isOver) // so isCaptured == true as isIrrelevant was false
			{
				handledInManaged |= ReleaseCapture(args);
			}

			return handledInManaged;
		}

		private bool OnNativePointerExited(PointerRoutedEventArgs args)
		{
			var handledInManaged = false;
			var isIrrelevant = ValidateAndUpdateCapture(args);

			handledInManaged |= SetOver(args, false, muteEvent: isIrrelevant);

			// We release the captures on exit when pointer is not pressed the control
			// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
			if (!IsPressed(args.Pointer))
			{
				handledInManaged |= ReleaseCapture(args);
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
			var isIrrelevant = ValidateAndUpdateCapture(args, out _); // Check this *before* updating the sate!

			// When a pointer is cancelled / swallowed by the system, we don't even receive "Released" nor "Exited"
			SetPressed(args, false, muteEvent: true);
			SetOver(args, false, muteEvent: true);

			if (isIrrelevant)
			{
				return false;
			}
		
			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}

			var handledInManaged = false;
			if (isSwallowedBySystem)
			{
				handledInManaged |= ReleaseCapture(args, forceCaptureLostEvent: true);
			}
			else
			{
				args.Handled = false;
				handledInManaged |= RaiseEvent(PointerCanceledEvent, args);
				handledInManaged |= ReleaseCapture(args);
			}

			return handledInManaged;
		}
		#endregion

		#region Pointer over state (Updated by the partial API OnNative***)
		/// <summary>
		/// Indicates if a pointer (no matter the pointer) is currently over the element (i.e. OverState)
		/// WARNING: This might not be maintained for all controls, cf. remarks.
		/// </summary>
		/// <remarks>
		/// This flag is updated by the managed Pointers events handling, however on Android and WebAssembly,
		/// pointer events are marshaled to the managed code only if there is some listeners to the events.
		/// So it means that this flag will be maintained only if you subscribe at least to one pointer event
		/// (or override one of the OnPointer*** methods).
		/// </remarks>
		internal bool IsPointerOver { get; set; } // TODO: 'Set' should be private, but we need to update all controls that are setting

		/// <summary>
		/// Indicates if a pointer is currently over the element (i.e. OverState)
		/// WARNING: This might not be maintained for all controls, cf. remarks.
		/// </summary>
		/// <remarks>
		/// The over state is updated by the managed Pointers events handling, however on Android and WebAssembly,
		/// pointer events are marshaled to the managed code only if there is some listeners to the events.
		/// So it means that this method will give valid state only if you subscribe at least to one pointer event
		/// (or override one of the OnPointer*** methods).
		/// </remarks>
		internal bool IsOver(Pointer pointer) => IsPointerOver;

		private bool SetOver(PointerRoutedEventArgs args, bool isOver, bool muteEvent = false)
		{
			var wasOver = IsPointerOver;
			IsPointerOver = isOver;

			if (muteEvent
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
		#endregion

		#region Pointer pressed state (Updated by the partial API OnNative***)
		/// <summary>
		/// Indicates if a pointer was pressed while over the element (i.e. PressedState).
		/// Note: The pressed state will remain true even if the pointer exits the control (while pressed)
		/// WARNING: This might not be maintained for all controls, cf. remarks.
		/// </summary>
		/// <remarks>
		/// This flag is updated by the managed Pointers events handling, however on Android and WebAssembly,
		/// pointer events are marshaled to the managed code only if there is some listeners to the events.
		/// So it means that this flag will be maintained only if you subscribe at least to one pointer event
		/// (or override one of the OnPointer*** methods).
		/// </remarks>
		internal bool IsPointerPressed { get; set; } // TODO: 'Set' should be private, but we need to update all controls that are setting

		/// <summary>
		/// Indicates if a pointer was pressed while over the element (i.e. PressedState)
		/// Note: The pressed state will remain true even if the pointer exits the control (while pressed)
		/// WARNING: This might not be maintained for all controls, cf. remarks.
		/// </summary>
		/// <remarks>
		/// The pressed state is updated by the managed Pointers events handling, however on Android and WebAssembly,
		/// pointer events are marshaled to the managed code only if there is some listeners to the events.
		/// So it means that this method will give valid state only if you subscribe at least to one pointer event
		/// (or override one of the OnPointer*** methods).
		/// </remarks>
		internal bool IsPressed(Pointer pointer) => IsPointerPressed;

		private bool SetPressed(PointerRoutedEventArgs args, bool isPressed, bool muteEvent = false)
		{
			var wasPressed = IsPointerPressed;
			IsPointerPressed = isPressed;

			if (wasPressed != isPressed)
			{
				Console.WriteLine($"{this} IS PRESSED CHANGED, now {isPressed}");
			}

			if (muteEvent
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
		#endregion

		#region Pointer capture state (Updated by the partial API OnNative***)
		/*
		 * About pointer capture
		 *
		 * - When a pointer is captured, it will still bubble up, but it will bubble up from the element
		 *   that captured the touch (so the a inner control won't receive it, even if under the pointer)
		 *   ** but the OriginalSource will still be the inner control! **
		 * - Captured are exclusive : first come, first served! (For a given pointer)
		 * - A control can capture a pointer, even if not under the pointer (not supported by uno yet)
		 * - The PointersCapture property remains `null` until a pointer is captured
		 */

		private static IDictionary<Pointer, PointerCapture> _allCaptures;
		private List<Pointer> _localCaptures;

		#region Capture public API
		public static DependencyProperty PointerCapturesProperty { get; } = DependencyProperty.Register(
			"PointerCaptures",
			typeof(IReadOnlyList<Pointer>),
			typeof(UIElement),
			new FrameworkPropertyMetadata(defaultValue: null));

		public IReadOnlyList<Pointer> PointerCaptures => (IReadOnlyList<Pointer>)this.GetValue(PointerCapturesProperty);

		public bool CapturePointer(Pointer value)
		{
			var pointer = value ?? throw new ArgumentNullException(nameof(value));

			if (_allCaptures == null)
			{
				_allCaptures = new Dictionary<Pointer, PointerCapture>(EqualityComparer<Pointer>.Default);
			}

			if (_localCaptures == null)
			{
				_localCaptures = new List<Pointer>();
				this.SetValue(PointerCapturesProperty, _localCaptures); // Note: On UWP this is done only on first capture (like here)
			}

			if (_allCaptures.TryGetValue(pointer, out var capture))
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"{this}: Pointer {pointer} already captured by {capture.Owner}");
				}

				return false;
			}
			else
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"{this}: Capturing pointer {pointer}");
				}

				capture = new PointerCapture(this, pointer);
				_allCaptures.Add(pointer, capture);
				_localCaptures.Add(pointer);

				CapturePointerNative(pointer);

				return true;
			}
		}

		public void ReleasePointerCapture(Pointer value)
		{
			var pointer = value ?? throw new ArgumentNullException(nameof(value));

			if (IsCaptured(pointer, out var capture))
			{
				Release(capture);
			}
			else if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"{this}: Cannot release pointer {pointer}: not captured by this control.");
			}
		}

		public void ReleasePointerCaptures()
		{
			if (!HasCapture)
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"{this}: no pointers to release.");
				}

				return;
			}

			var localCaptures = _allCaptures
				.Values
				.Where(capture => capture.Owner == this)
				.ToList();
			foreach (var capture in localCaptures)
			{
				Release(capture);
			}
		}
		#endregion

		partial void CapturePointerNative(Pointer pointer);
		partial void ReleasePointerCaptureNative(Pointer pointer);

		private bool HasCapture => (_localCaptures?.Count ?? 0) != 0;

		internal bool IsCaptured(Pointer pointer)
			=> IsCaptured(pointer, out _);

		private bool IsCaptured(Pointer pointer, out PointerCapture capture)
		{
			if (HasCapture // Do not event check the _allCaptures if no capture defined on this element
				&& _allCaptures.TryGetValue(pointer, out capture)
				&& capture.Owner == this)
			{
				return true;
			}
			else
			{
				capture = null;
				return false;
			}
		}

		private bool ValidateAndUpdateCapture(PointerRoutedEventArgs args)
			=> ValidateAndUpdateCapture(args, IsOver(args.Pointer));

		private bool ValidateAndUpdateCapture(PointerRoutedEventArgs args, out bool isOver)
			=> ValidateAndUpdateCapture(args, isOver = IsOver(args.Pointer));

		// Used by all OnNativeXXX to validate and update the common over/pressed/capture states
		private bool ValidateAndUpdateCapture(PointerRoutedEventArgs args, bool isOver, bool forceRelease = false)
		{
			// We might receive some unexpected move/up/cancel for a pointer over an element,
			// we have to mute them to avoid invalid event sequence.
			// Notes:
			//   iOS:  This may happen on iOS where the pointers are implicitly captured.
			//   Android:  This may happen on Android where the pointers are implicitly captured.
			//   WASM: On wasm, if this check mutes your event, it's because you didn't received the "pointerenter" (not bubbling natively).
			//         This is usually because your control is covered by an element which is IsHitTestVisible == true / has transparent background.

			PointerCapture capture = default;
			if (!(_allCaptures?.TryGetValue(args.Pointer, out capture) ?? false))
			{
				return !isOver;
			}

			if ((forceRelease && capture.LastDispatchedEventFrameId < args.FrameId)
				|| (capture.Owner is FrameworkElement fwElt && !fwElt.IsLoaded))
			{
				// If 'forceRelease' we want to release any previous capture that was not release properly no matter the reason.
				// BUT we don't want to release a capture that was made by a child control.
				// We also do not allow a control that is not loaded to keep a capture (they should all have been release on unload).
				// ** This is an IMPORTANT safety catch to prevent the application to become unresponsive **
				capture.Owner.Release(capture);

				return false;
			}
			else if (capture.Owner == this)
			{
				capture.LastDispatchedEventFrameId = args.FrameId;
				capture.LastDispatchedEventArgs = args;

				return false;
			}
			else
			{
				// We should dispatch the event only if the control which has captured the pointer has already dispatched the event
				// (Which actually means that the current control is a parent of the control which has captured the pointer)
				// Remarks: This is not enough to determine parent-child relationship when we dispatch multiple events base on the same native event,
				//			(as they will all have the same FrameId), however in that case we dispatch events layer per layer
				//			instead of bubbling a single event before raising the next one, so we are safe.
				//			The only limitation would be when mixing native vs. managed bubbling, but this check only prevents
				//			the leaf of the tree to raise the event, so we cannot mix bubbling mode in that case.
				return capture.LastDispatchedEventFrameId <= args.FrameId;
			}
		}

		private bool ReleaseCapture(PointerRoutedEventArgs args, bool forceCaptureLostEvent = false)
		{
			if (IsCaptured(args.Pointer, out var capture))
			{
				return Release(capture, args);
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

		private bool Release(PointerCapture capture, PointerRoutedEventArgs args = null)
		{
			global::System.Diagnostics.Debug.Assert(capture.Owner == this, "Can release only capture made by the element itself.");

			if (this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"{capture.Owner}: Releasing capture of pointer {capture.Pointer}");
			}

			var pointer = capture.Pointer;

			_allCaptures.Remove(pointer);
			_localCaptures.Remove(pointer);

			ReleasePointerCaptureNative(pointer);

			args = args ?? capture.LastDispatchedEventArgs;
			if (args == null)
			{
				return false; // TODO: We should create a new instance of event args with dummy location
			}
			args.Handled = false;
			return RaiseEvent(PointerCaptureLostEvent, args);
		}

		private class PointerCapture
		{
			public PointerCapture(UIElement owner, Pointer pointer)
			{
				Owner = owner;
				Pointer = pointer;
			}

			/// <summary>
			/// The captured pointer
			/// </summary>
			public Pointer Pointer { get; }

			/// <summary>
			/// The element on for which the pointer was captured
			/// </summary>
			public UIElement Owner { get; }

			/// <summary>
			/// Determines if the <see cref="Owner"/> is in the native bubbling tree.
			/// If so we could rely on standard events bubbling to reach it.
			/// Otherwise this means that we have to bubble the veent in managed only.
			///
			/// This makes sens only for platform that has "implicit capture"
			/// (i.e. all pointers events are sent to the element on which the pointer pressed
			/// occured at the beginning of the gesture). This is the case on iOS and Android.
			/// </summary>
			public bool? IsInNativeBubblingTree { get; set; }

			/// <summary>
			/// Gets the timestamp of the last event dispatched by the <see cref="Owner"/>.
			/// In case of native bubbling (cf. <see cref="IsInNativeBubblingTree"/>),
			/// this helps to determine that an event was already dispatched by the Owner:
			/// if a UIElement is receiving and event with the same timestamp, it means that the element
			/// is a parent of the Owner and we are only bubbling the routed event, so this element can
			/// raise the event (if the opposite, it means that the element is a child, so it has to mute the event).
			/// </summary>
			public long LastDispatchedEventFrameId { get; set; }

			public PointerRoutedEventArgs LastDispatchedEventArgs { get; set; }
		}
		#endregion
	}
}
