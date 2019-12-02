using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Input;
using Windows.UI.Xaml.Input;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI;
using Uno.UI.Xaml;

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
	 *		internal bool SafeRaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args);
	 */

	partial class UIElement
	{
		static UIElement()
		{
			var uiElement = typeof(UIElement);
			VisibilityProperty.GetMetadata(uiElement).MergePropertyChangedCallback(ClearPointersStateIfNeeded);
			Windows.UI.Xaml.Controls.Control.IsEnabledProperty.GetMetadata(typeof(Windows.UI.Xaml.Controls.Control)).MergePropertyChangedCallback(ClearPointersStateIfNeeded);
#if __WASM__
			HitTestVisibilityProperty.GetMetadata(uiElement).MergePropertyChangedCallback(ClearPointersStateIfNeeded);
#endif
		}

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

				newMode.LogIfNotSupported(elt.Log());

				elt.UpdateManipulations(newMode, elt.HasManipulationHandler);
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
				fwElt.Unloaded += ClearPointersStateOnUnload;
			}
		}

		partial void InitializePointersPartial();

		private static readonly PropertyChangedCallback ClearPointersStateIfNeeded = (DependencyObject sender, DependencyPropertyChangedEventArgs dp) =>
		{
			// As the Visibility DP is not inherited, when a control becomes invisible we validate that if any
			// of its children is capturing a pointer, and we release those captures.
			// As the number of capture is usually really small, we assume that its more performant to walk the tree
			// when the visibility changes instead of creating another coalesced DP.
			if (dp.NewValue is Visibility visibility
				&& visibility != Visibility.Visible
				&& PointerCapture.Any(out var captures))
			{
				foreach (var capture in captures)
				{
					foreach (var target in capture.Targets.ToList())
					{
						if (target.Element.GetParents().Contains(sender))
						{
							target.Element.Release(capture, PointerCaptureKind.Any);
						}
					}
				}
			}

			if (sender is UIElement elt && !elt.IsHitTestVisibleCoalesced)
			{
				elt.Release(PointerCaptureKind.Any);
				elt.SetPressed(null, false, muteEvent: true);
				elt.SetOver(null, false, muteEvent: true);
			}
		};

		private static readonly RoutedEventHandler ClearPointersStateOnUnload = (object sender, RoutedEventArgs args) =>
		{
			if (sender is UIElement elt)
			{
				elt.Release(PointerCaptureKind.Any);
				elt.SetPressed(null, false, muteEvent: true);
				elt.SetOver(null, false, muteEvent: true);
			}
		};

		// This is a coalesced HitTestVisible and should be unified with it
		// We should follow the WASM way an unify it on all platforms!
		private bool IsHitTestVisibleCoalesced
		{
			get
			{
				if (Visibility != Visibility.Visible || !IsHitTestVisible)
				{
					return false;
				}

				if (this is Windows.UI.Xaml.Controls.Control ctrl)
				{
					return ctrl.IsLoaded && ctrl.IsEnabled;
				}
				else if (this is Windows.UI.Xaml.Controls.Control fwElt)
				{
					return fwElt.IsLoaded;
				}
				else
				{
					return true;
				}
			}
		}

		#region Gestures recognition (includes manipulation)

		#region Event to RoutedEvent handler adapters
		// Note: For the manipulation and gesture event args, the original source has to be the element that raise the event
		//		 As those events are bubbling in managed only, the original source will be right one for all.

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> OnRecognizerManipulationStarting = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			that.SafeRaiseEvent(ManipulationStartingEvent, new ManipulationStartingRoutedEventArgs(that, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> OnRecognizerManipulationStarted = (sender,  args) =>
		{
			var that = (UIElement)sender.Owner;
			that.SafeRaiseEvent(ManipulationStartedEvent, new ManipulationStartedRoutedEventArgs(that, sender, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> OnRecognizerManipulationUpdated = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			that.SafeRaiseEvent(ManipulationDeltaEvent, new ManipulationDeltaRoutedEventArgs(that, sender, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> OnRecognizerManipulationInertiaStarting = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			that.SafeRaiseEvent(ManipulationInertiaStartingEvent, new ManipulationInertiaStartingRoutedEventArgs(that, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> OnRecognizerManipulationCompleted = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			that.SafeRaiseEvent(ManipulationCompletedEvent, new ManipulationCompletedRoutedEventArgs(that, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, TappedEventArgs> OnRecognizerTapped = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			if (args.TapCount == 1)
			{
				that.SafeRaiseEvent(TappedEvent, new TappedRoutedEventArgs(that, args));
			}
			else // i.e. args.TapCount == 2
			{
				that.SafeRaiseEvent(DoubleTappedEvent, new DoubleTappedRoutedEventArgs(that, args));
			}
		};
		#endregion

		private bool _isGestureCompleted;

		private GestureRecognizer CreateGestureRecognizer()
		{
			var recognizer = new GestureRecognizer(this);

			recognizer.ManipulationStarting += OnRecognizerManipulationStarting;
			recognizer.ManipulationStarted += OnRecognizerManipulationStarted;
			recognizer.ManipulationUpdated += OnRecognizerManipulationUpdated;
			recognizer.ManipulationInertiaStarting += OnRecognizerManipulationInertiaStarting;
			recognizer.ManipulationCompleted += OnRecognizerManipulationCompleted;
			recognizer.Tapped += OnRecognizerTapped;

			// Allow partial parts to subscribe to pointer events (WASM)
			OnGestureRecognizerInitialized(recognizer);

			return recognizer;
		}

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer);

		#region Manipulation events wire-up
		partial void AddManipulationHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount == 1)
			{
				UpdateManipulations(ManipulationMode, hasManipulationHandler: true);
			}
		}

		partial void RemoveManipulationHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (remainingHandlersCount == 0 && !HasManipulationHandler)
			{
				UpdateManipulations(default(ManipulationModes), hasManipulationHandler: false);
			}
		}

		private bool HasManipulationHandler =>
			   CountHandler(ManipulationStartingEvent) != 0
			|| CountHandler(ManipulationStartedEvent) != 0
			|| CountHandler(ManipulationDeltaEvent) != 0
			|| CountHandler(ManipulationInertiaStartingEvent) != 0
			|| CountHandler(ManipulationCompletedEvent) != 0;

		private void UpdateManipulations(ManipulationModes mode, bool hasManipulationHandler)
		{
			if (!hasManipulationHandler || mode == ManipulationModes.None || mode == ManipulationModes.System)
			{
				if (!_gestures.IsValueCreated)
				{
					return;
				}
				else
				{
					_gestures.Value.GestureSettings &= ~GestureSettingsHelper.Manipulations;
					return;
				}
			}

			var settings = _gestures.Value.GestureSettings;
			settings &= ~GestureSettingsHelper.Manipulations; // Remove all configured manipulation flags
			settings |= mode.ToGestureSettings(); // Then set them back from the mode

			_gestures.Value.GestureSettings = settings;
		}
		#endregion

		#region Gesture events wire-up
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

		partial void PrepareBubblingPointerEvent(RoutedEvent routedEvent, ref RoutedEventArgs args, ref bool isBubblingAllowed)
		{
			var ptArgs = (PointerRoutedEventArgs)args;
			switch (routedEvent.Flag)
			{
				case RoutedEventFlag.PointerEntered:
					OnManagedPointerEnter(ptArgs);
					break;
				case RoutedEventFlag.PointerPressed:
					OnManagedPointerDown(ptArgs);
					break;
				case RoutedEventFlag.PointerMoved:
					OnManagePointerMove(ptArgs);
					break;
				case RoutedEventFlag.PointerReleased:
					OnManagedPointerUp(ptArgs);
					break;
				case RoutedEventFlag.PointerExited:
					OnManagedPointerExited(ptArgs);
					break;
				case RoutedEventFlag.PointerCanceled:
					OnManagedPointerCancel(ptArgs);
					break;
				// Nothing to do for PointerCaptureLost
			}
		}

		partial void PrepareBubblingManipulationEvent(RoutedEvent routedEvent, ref RoutedEventArgs args, ref bool isBubblingAllowed)
		{
			// When we bubble a manipulation event from a child, we make sure to abort any pending gesture/manipulation on the current element
			if (routedEvent != ManipulationStartingEvent && _gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}
			// Note: We do not need to alter the location of the events, on UWP they are always relative to the OriginalSource.
		}

		partial void PrepareBubblingGestureEvent(RoutedEvent routedEvent, ref RoutedEventArgs args, ref bool isBubblingAllowed)
		{
			// When we bubble a gesture event from a child, we make sure to abort any pending gesture/manipulation on the current element
			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}
		}

		/// <summary>
		/// Prevents the gesture recognizer to generate a manipulation. It's designed to be invoked in Pointers events handlers.
		/// </summary>
		private protected void CompleteGesture()
		{
			// This flags allow us to complete the gesture on pressed (i.e. even before the gesture started)
			_isGestureCompleted = true;

			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}
		}
		#endregion

		#region Partial API to raise pointer events and gesture recognition (OnNative***)
		private bool OnNativePointerEnter(PointerRoutedEventArgs args)
			=> OnPointerEnter(args, isManagedBubblingEvent: false);
		private void OnManagedPointerEnter(PointerRoutedEventArgs args)
			=> OnPointerEnter(args, isManagedBubblingEvent: true);

		private bool OnPointerEnter(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			// We override the isOver for the relevancy check as we will update it right after.
			var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver: true);
			var handledInManaged = SetOver(args, true, muteEvent: isManagedBubblingEvent || !isOverOrCaptured);

			return handledInManaged;
		}

		private bool OnNativePointerDown(PointerRoutedEventArgs args) => OnPointerDown(args, false);
		private void OnManagedPointerDown(PointerRoutedEventArgs args) => OnPointerDown(args, true);

		private bool OnPointerDown(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			_isGestureCompleted = false;

			// "forceRelease: true": as we are in pointer pressed, if the pointer is already captured,
			// it due to an invalid state. So here we make sure to not stay in an invalid state that would
			// prevent any interaction with the application.
			var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver: true, forceRelease: true);
			var handledInManaged = SetPressed(args, true, muteEvent: isManagedBubblingEvent || !isOverOrCaptured);

			if (!isManagedBubblingEvent && !isOverOrCaptured)
			{
				// This case is for safety only, it should not happen as we should never get a Pointer down while not
				// on this UIElement, and no capture should prevent the dispatch as no parent should hold a capture at this point.
				// (Even if a Parent of this listen on pressed on a child of this and captured the pointer, the FrameId will be
				// the same so we won't consider this event as irrelevant)

				return handledInManaged; // always false, as the 'pressed' event was mute
			}

			if (!_isGestureCompleted && _gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.

				var recognizer = _gestures.Value;
				var point = args.GetCurrentPoint(this);

				recognizer.ProcessDownEvent(point);

#if __WASM__
				// On iOS and Android, pointers are implicitly captured, so we will receive the "irrelevant" (i.e. !isOverOrCaptured)
				// pointer moves and we can use them for manipulation. But on WASM we have to explicitly request to get those events
				// (expect on FF where they are also implicitly captured ... but we still capture them).
				if (recognizer.PendingManipulation?.IsActive(point.PointerDevice.PointerDeviceType, point.PointerId) ?? false)
				{
					Capture(args.Pointer, PointerCaptureKind.Implicit, args);
				}
#endif
			}

			return handledInManaged;
		}

		// This is for iOS and Android which not raising the Exit properly and for which we have to re-compute the over state for each move
		private bool OnNativePointerMoveWithOverCheck(PointerRoutedEventArgs args, bool isOver)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver);

			handledInManaged |= SetOver(args, isOver);

			if (isOverOrCaptured)
			{
				// If this pointer was wrongly dispatched here (out of the bounds and not captured),
				// we don't raise the 'move' event

				args.Handled = false;
				handledInManaged |= RaisePointerEvent(PointerMovedEvent, args);
			}

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this), isOverOrCaptured);
			}

			return handledInManaged;
		}

		private bool OnNativePointerMove(PointerRoutedEventArgs args) => OnPointerMove(args, false);
		private void OnManagePointerMove(PointerRoutedEventArgs args) => OnPointerMove(args, true);

		private bool OnPointerMove(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			var isOverOrCaptured = ValidateAndUpdateCapture(args);
			var handledInManaged = false;

			if (!isManagedBubblingEvent && isOverOrCaptured)
			{
				// If this pointer was wrongly dispatched here (out of the bounds and not captured),
				// we don't raise the 'move' event

				args.Handled = false;
				handledInManaged |= RaisePointerEvent(PointerMovedEvent, args);
			}

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this), isManagedBubblingEvent || isOverOrCaptured);
			}

			return handledInManaged;
		}

		private bool OnNativePointerUp(PointerRoutedEventArgs args) => OnPointerUp(args, false);
		private void OnManagedPointerUp(PointerRoutedEventArgs args) => OnPointerUp(args, true);

		private bool OnPointerUp(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args, out var isOver);

			handledInManaged |= SetPressed(args, false, muteEvent: isManagedBubblingEvent || !isOverOrCaptured);

			
			// Note: We process the UpEvent between Release and Exited as the gestures like "Tap"
			//		 are fired between those events.
			if (_gestures.IsValueCreated)
			{
				// We need to process only events that are bubbling natively to this control (i.e. isOverOrCaptured == true),
				// if they are bubbling in managed it means that they where handled a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessUpEvent(args.GetCurrentPoint(this), isManagedBubblingEvent || isOverOrCaptured);
			}

			// We release the captures on up but only after the released event and processed the gesture
			// Note: For a "Tap" with a finger the sequence is Up / Exited / Lost, so we let the Exit raise the capture lost
			// Note: If '!isOver', that means that 'IsCaptured == true' otherwise 'isOverOrCaptured' would have been false.
			if (!isOver || args.Pointer.PointerDeviceType != PointerDeviceType.Touch)
			{
				handledInManaged |= SetNotCaptured(args);
			}

			return handledInManaged;
		}

		private bool OnNativePointerExited(PointerRoutedEventArgs args) => OnPointerExited(args, false);
		private void OnManagedPointerExited(PointerRoutedEventArgs args) => OnPointerExited(args, true);

		private bool OnPointerExited(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args);

			handledInManaged |= SetOver(args, false, muteEvent: isManagedBubblingEvent || !isOverOrCaptured);

			// We release the captures on exit when pointer if not pressed
			// Note: for a "Tap" with a finger the sequence is Up / Exited / Lost, so the lost cannot be raised on Up
			if (!IsPressed(args.Pointer))
			{
				handledInManaged |= SetNotCaptured(args);
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
			args.CanceledByDirectManipulation = isSwallowedBySystem;
			return OnPointerCancel(args, false);
		}
		private void OnManagedPointerCancel(PointerRoutedEventArgs args) => OnNativePointerCancel(args, true);

		private bool OnPointerCancel(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			var isOverOrCaptured = ValidateAndUpdateCapture(args); // Check this *before* updating the pressed / over states!

			// When a pointer is cancelled / swallowed by the system, we don't even receive "Released" nor "Exited"
			SetPressed(args, false, muteEvent: true);
			SetOver(args, false, muteEvent: true);

			if (!isOverOrCaptured)
			{
				return false;
			}
		
			if (_gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}

			var handledInManaged = false;
			if (args.CanceledByDirectManipulation)
			{
				handledInManaged |= SetNotCaptured(args, forceCaptureLostEvent: true);
			}
			else
			{
				args.Handled = false;
				handledInManaged |= !isManagedBubblingEvent && RaisePointerEvent(PointerCanceledEvent, args);
				handledInManaged |= SetNotCaptured(args);
			}

			return handledInManaged;
		}

		private static (UIElement sender, RoutedEvent @event, PointerRoutedEventArgs args) _pendingRaisedEvent;
		private bool RaisePointerEvent(RoutedEvent evt, PointerRoutedEventArgs args)
		{
			try
			{
				_pendingRaisedEvent = (this, evt, args);
				return RaiseEvent(evt, args);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to raise '{evt.Name}': {e}");
				}

				return false;
			}
			finally
			{
				_pendingRaisedEvent = (null, null, null);
			}
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
				return RaisePointerEvent(PointerEnteredEvent, args);
			}
			else // Exited
			{
				args.Handled = false;
				return RaisePointerEvent(PointerExitedEvent, args);
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

			if (muteEvent
				|| wasPressed == isPressed) // nothing changed
			{
				return false;
			}

			if (isPressed) // Pressed
			{
				args.Handled = false;
				return RaisePointerEvent(PointerPressedEvent, args);
			}
			else // Released
			{
				args.Handled = false;
				return RaisePointerEvent(PointerReleasedEvent, args);
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

		private List<Pointer> _localExplicitCaptures;

		#region Capture public (and internal) API ==> This manages only Explicit captures
		public static DependencyProperty PointerCapturesProperty { get; } = DependencyProperty.Register(
			"PointerCaptures",
			typeof(IReadOnlyList<Pointer>),
			typeof(UIElement),
			new FrameworkPropertyMetadata(defaultValue: null));

		public IReadOnlyList<Pointer> PointerCaptures => (IReadOnlyList<Pointer>)this.GetValue(PointerCapturesProperty);

		/// <summary>
		/// Indicates if this UIElement has any active ** EXPLICIT ** pointer capture.
		/// </summary>
#if __ANDROID__
		internal new bool HasPointerCapture => (_localExplicitCaptures?.Count ?? 0) != 0;
#else
		internal bool HasPointerCapture => (_localExplicitCaptures?.Count ?? 0) != 0;
#endif

		internal bool IsCaptured(Pointer pointer)
			=> HasPointerCapture
				&& PointerCapture.TryGet(pointer, out var capture)
				&& capture.IsTarget(this, PointerCaptureKind.Explicit);

		public bool CapturePointer(Pointer value)
		{
			var pointer = value ?? throw new ArgumentNullException(nameof(value));

			return Capture(pointer, PointerCaptureKind.Explicit, _pendingRaisedEvent.args);
		}

		public void ReleasePointerCapture(Pointer value) => ReleasePointerCapture(value, muteEvent: false);

		/// <summary>
		/// Release a pointer capture with the ability to not raise the <see cref="PointerCaptureLost"/> event (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// On some controls we use the Capture to track the pressed state properly, to detect click.  But in few cases (i.e. Hyperlink)
		/// UWP does not raise a PointerCaptureLost. This method give the ability to easily follow this behavior without requiring
		/// the control to track and handle the event.
		/// </remarks>
		/// <param name="value">The pointer to release.</param>
		/// <param name="muteEvent">Determines if the event should be raised or not.</param>
		private protected void ReleasePointerCapture(Pointer value, bool muteEvent)
		{
			var pointer = value ?? throw new ArgumentNullException(nameof(value));

			if (!Release(pointer, PointerCaptureKind.Explicit, muteEvent: muteEvent)
				&& this.Log().IsEnabled(LogLevel.Information))
			{
				this.Log().Info($"{this}: Cannot release pointer {pointer}: not captured by this control.");
			}
		}

		public void ReleasePointerCaptures()
		{
			if (!HasPointerCapture)
			{
				if (this.Log().IsEnabled(LogLevel.Information))
				{
					this.Log().Info($"{this}: no pointers to release.");
				}

				return;
			}

			Release(PointerCaptureKind.Explicit);
		}
		#endregion

		partial void CapturePointerNative(Pointer pointer);
		partial void ReleasePointerNative(Pointer pointer);

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

			// Note: even if the result of this method is usually named 'isOverOrCaptured', the result of this method will also
			//		 be "false" if the pointer is over the element BUT the pointer was captured by a parent element.

			if (PointerCapture.TryGet(args.Pointer, out var capture))
			{
				return capture.ValidateAndUpdate(this, args, forceRelease);
			}
			else
			{
				return isOver;
			}
		}

		private bool SetNotCaptured(PointerRoutedEventArgs args, bool forceCaptureLostEvent = false)
		{
			if (Release(args.Pointer, PointerCaptureKind.Any, args))
			{
				return true;
			}
			else if (forceCaptureLostEvent)
			{
				args.Handled = false;
				return RaisePointerEvent(PointerCaptureLostEvent, args);
			}
			else
			{
				return false;
			}
		}

		private bool Capture(Pointer pointer, PointerCaptureKind kind, PointerRoutedEventArgs relatedArgs)
		{
			if (_localExplicitCaptures == null)
			{
				_localExplicitCaptures = new List<Pointer>();
				this.SetValue(PointerCapturesProperty, _localExplicitCaptures); // Note: On UWP this is done only on first capture (like here)
			}

			return PointerCapture.GetOrCreate(pointer).TryAddTarget(this, kind, relatedArgs);
		}

		private void Release(PointerCaptureKind kinds, PointerRoutedEventArgs relatedARgs = null, bool muteEvent = false)
		{
			if (PointerCapture.Any(out var captures))
			{
				foreach (var capture in captures)
				{
					Release(capture, kinds, relatedARgs, muteEvent);
				}
			}
		}

		private bool Release(Pointer pointer, PointerCaptureKind kinds, PointerRoutedEventArgs relatedARgs = null, bool muteEvent = false)
		{
			return PointerCapture.TryGet(pointer, out var capture)
				&& Release(capture, kinds, relatedARgs, muteEvent);
		}

		private bool Release(PointerCapture capture, PointerCaptureKind kinds, PointerRoutedEventArgs relatedARgs = null, bool muteEvent = false)
		{
			if (!capture.RemoveTarget(this, kinds, out var lastDispatched).HasFlag(PointerCaptureKind.Explicit))
			{
				return false;
			}

			if (muteEvent)
			{
				return false;
			}

			relatedARgs = relatedARgs ?? lastDispatched;
			if (relatedARgs == null)
			{
				return false; // TODO: We should create a new instance of event args with dummy location
			}
			relatedARgs.Handled = false;
			return RaisePointerEvent(PointerCaptureLostEvent, relatedARgs);
		}
		#endregion
	}
}
