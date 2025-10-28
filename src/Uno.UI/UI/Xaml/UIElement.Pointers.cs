using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Devices.Haptics;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Uno;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Dispatching;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml
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
	 *		partial void PrepareManagedPointerEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
	 *		partial void PrepareManagedManipulationEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
	 *		partial void PrepareManagedGestureEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
	 *	and is using:
	 *		internal bool SafeRaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args);
	 */

	partial class UIElement
	{
		static UIElement()
		{
			var uiElement = typeof(UIElement);
			VisibilityProperty.GetMetadata(uiElement).MergePropertyChangedCallback(ClearPointersStateIfNeeded);
			Microsoft.UI.Xaml.Controls.Control.IsEnabledProperty.GetMetadata(typeof(Microsoft.UI.Xaml.Controls.Control)).MergePropertyChangedCallback(ClearPointersStateIfNeeded);
#if UNO_HAS_MANAGED_POINTERS
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

		#region CanDrag (DP)
		public static DependencyProperty CanDragProperty { get; } = DependencyProperty.Register(
			nameof(CanDrag),
			typeof(bool),
			typeof(UIElement),
			new FrameworkPropertyMetadata(default(bool), OnCanDragChanged));

		private static void OnCanDragChanged(DependencyObject snd, DependencyPropertyChangedEventArgs args)
		{
			if (snd is UIElement elt && args.NewValue is bool canDrag)
			{
				elt.UpdateDragAndDrop(canDrag);
				elt.OnCanDragChanged(args.OldValue is bool oldCanDrag && oldCanDrag, canDrag);
			}
		}

		partial void OnCanDragChanged(bool oldValue, bool newValue);

		public bool CanDrag
		{
			get => (bool)GetValue(CanDragProperty);
			set => SetValue(CanDragProperty, value);
		}
		#endregion

		#region AllowDrop (DP)
		public static DependencyProperty AllowDropProperty { get; } = DependencyProperty.Register(
			nameof(AllowDrop),
			typeof(bool),
			typeof(UIElement),
			new FrameworkPropertyMetadata(default(bool)));

		public bool AllowDrop
		{
			get => (bool)GetValue(AllowDropProperty);
			set => SetValue(AllowDropProperty, value);
		}
		#endregion

		private /* readonly but partial */ GestureRecognizer _gestures;

#if __ANDROID__ || __APPLE_UIKIT__
		/// <summary>
		/// Validates that this element is able to manage pointer events.
		/// If this element is only the shadow of a ghost native view that was instantiated for marshalling purposes by Xamarin,
		/// the _gestures instance will be invalid and trying to interpret a native pointer event might crash the app.
		/// This flag should be checked when receiving a pointer related event from the native view to prevent this case.
		/// </summary>
		private bool ArePointersEnabled { get; set; }
#endif

		// ctor
		private void InitializePointers()
		{
			// Setting MotionEventSplittingEnabled to false makes sure that for multi-touches, all the pointers are
			// be dispatched on the same target (the one on which the first pressed occured).
			// This is **not** the behavior of windows which dispatches each pointer to the right target,
			// so we keep the initial value which is true.
			// MotionEventSplittingEnabled = true;

			InitializePointersPartial();
			if (this is FrameworkElement fwElt)
			{
				fwElt.Unloaded += ClearPointersStateOnUnload;
			}
		}

		partial void InitializePointersPartial();

		private GestureRecognizer GestureRecognizer => _gestures ??= CreateGestureRecognizer();

		private bool IsGestureRecognizerCreated => _gestures != null;

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
				for (var captureIndex = 0; captureIndex < captures.Count; captureIndex++)
				{
					var capture = captures[captureIndex];
					var targets = capture.Targets.ToList();

					for (var targetIndex = 0; targetIndex < targets.Count; targetIndex++)
					{
						var target = targets[targetIndex];
						if (target.Element.HasParent(sender))
						{
							target.Element.Release(capture, PointerCaptureKind.Any);
						}
					}
				}
			}

			if (sender is UIElement elt && elt.GetHitTestVisibility() == HitTestability.Collapsed)
			{
				_currentPointerEventDispatch.VisualTreeAltered = true;
				elt.ClearPointerState();
			}
		};

		private static readonly RoutedEventHandler ClearPointersStateOnUnload = (object sender, RoutedEventArgs args) =>
		{
			_currentPointerEventDispatch.VisualTreeAltered = true;
			(sender as UIElement)?.ClearPointerState();
		};

		private partial void ClearPointerStateOnRecycle()
		{
			_currentPointerEventDispatch.VisualTreeAltered = true;
			ClearPointerState();
		}

		internal void ClearPointerState()
		{
			Release(PointerCaptureKind.Any);
			ClearPressed();
			SetOver(null, false, ctx: BubblingContext.NoBubbling);
			ClearDragOver();
		}

		[ThreadStatic]
		private static PointerEventDispatchResult _currentPointerEventDispatch;

		internal static void BeginPointerEventDispatch()
			=> _currentPointerEventDispatch = new();

		internal static PointerEventDispatchResult EndPointerEventDispatch()
			=> _currentPointerEventDispatch; // No need to clean it right now, we can safely wait for the next sequence to do it.

		internal struct PointerEventDispatchResult
		{
			/// <summary>
			/// Indicates that the visual tree has been modified in a way that the input manager must perform a complete hit-testing sequence before dispatching a new event.
			/// </summary>
			/// <remarks>
			/// This is designed for the case where for a single native pointer event, we are dispatching multiple managed events (e.g. managed Enter/Exit when we get only a native Move)
			/// for all other cases **a full hit test must be performed**.
			/// This means that we must not "capture"/cache the current top-most-element (a.k.a. OriginalSource) and try to update it on the next event
			/// as this flag does not take into consideration RenderTransform and other layout modification that does not alter the state of the pointer.
			/// </remarks>
			/// <remarks>This is used only for managed dispatch.</remarks>
			public bool VisualTreeAltered { get; set; }

			public static PointerEventDispatchResult operator +(PointerEventDispatchResult left, PointerEventDispatchResult right)
				=> new() { VisualTreeAltered = left.VisualTreeAltered || right.VisualTreeAltered };
		}

		/// <summary>
		/// Indicates if this element or one of its children might be target pointer events.
		/// Be aware this doesn't means that the element itself can be actually touched by the user,
		/// but only that pointer events can be raised on this element.
		/// I.e. this element is NOT <see cref="HitTestability.Collapsed"/>.
		/// </summary>
		internal HitTestability GetHitTestVisibility()
		{
#if UNO_HAS_MANAGED_POINTERS
			return HitTestVisibility;
#else
			// This is a coalesced HitTestVisible and should be unified with it
			// We should follow the WASM way and unify it on all platforms!
			// Note: This is currently only a port of the old behavior and reports only Collapsed and Visible.
			if (Visibility != Visibility.Visible || !IsHitTestVisible)
			{
				return HitTestability.Collapsed;
			}

			if (this is Microsoft.UI.Xaml.Controls.Control ctrl)
			{
				return ctrl.IsLoaded && ctrl.IsEnabled
					? HitTestability.Visible
					: HitTestability.Collapsed;
			}
			else if (this is Microsoft.UI.Xaml.FrameworkElement fwElt)
			{
				return fwElt.IsLoaded
					? HitTestability.Visible
					: HitTestability.Collapsed;
			}
			else
			{
				return HitTestability.Visible;
			}
#endif
		}

		#region GestureRecognizer wire-up

		#region Event to RoutedEvent handler adapters
		// Note: For the manipulation and gesture event args, the original source has to be the element that raise the event
		//		 As those events are bubbling in managed only, the original source will be right one for all.

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartingEventArgs> OnRecognizerManipulationStarting = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			var src = PointerRoutedEventArgs.LastPointerEvent?.OriginalSource as UIElement ?? that;

			that.SafeRaiseEvent(ManipulationStartingEvent, new ManipulationStartingRoutedEventArgs(src, that, args));
#if UNO_HAS_MANAGED_POINTERS
			if (args.Settings is not GestureSettings.None)
			{
				that.XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.RegisterUiElementManipulationRecognizer(args.Pointer, that, sender);
			}
#endif
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationStartedEventArgs> OnRecognizerManipulationStarted = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			var src = PointerRoutedEventArgs.LastPointerEvent?.OriginalSource as UIElement ?? that;

			that.CancelAllDirectManipulations(args.Pointers);
			that.SafeRaiseEvent(ManipulationStartedEvent, new ManipulationStartedRoutedEventArgs(src, that, sender, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationUpdatedEventArgs> OnRecognizerManipulationUpdated = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			var src = PointerRoutedEventArgs.LastPointerEvent?.OriginalSource as UIElement ?? that;

			that.SafeRaiseEvent(ManipulationDeltaEvent, new ManipulationDeltaRoutedEventArgs(src, that, sender, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationInertiaStartingEventArgs> OnRecognizerManipulationInertiaStarting = (sender, args) =>
		{
			args.UseCompositionTimer = WinRTFeatureConfiguration.GestureRecognizer.UseCompositionTimerForUiElement;

			var that = (UIElement)sender.Owner;
			var src = PointerRoutedEventArgs.LastPointerEvent?.OriginalSource as UIElement ?? that;

			that.SafeRaiseEvent(ManipulationInertiaStartingEvent, new ManipulationInertiaStartingRoutedEventArgs(src, that, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, ManipulationCompletedEventArgs> OnRecognizerManipulationCompleted = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			var src = PointerRoutedEventArgs.LastPointerEvent?.OriginalSource as UIElement ?? that;

#if !HAS_NATIVE_IMPLICIT_POINTER_CAPTURE
			foreach (var pointer in args.Pointers)
			{
				that.ReleasePointerCapture(pointer, muteEvent: true, PointerCaptureKind.Implicit);
			}
#endif

			that.SafeRaiseEvent(ManipulationCompletedEvent, new ManipulationCompletedRoutedEventArgs(src, that, args));

#if UNO_HAS_MANAGED_POINTERS
			that.XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.UnregisterUiElementManipulationRecognizer(args.Pointers, sender);
#endif
		};

		private static readonly TypedEventHandler<GestureRecognizer, TappedEventArgs> OnRecognizerTapped = (sender, args) =>
		{
			var src = (UIElement)sender.Owner;

			if (args.TapCount == 1)
			{
				src.SafeRaiseEvent(TappedEvent, new TappedRoutedEventArgs(src, args));
			}
			else // i.e. args.TapCount == 2
			{
				src.SafeRaiseEvent(DoubleTappedEvent, new DoubleTappedRoutedEventArgs(src, args));
			}
		};

		private static readonly TypedEventHandler<GestureRecognizer, RightTappedEventArgs> OnRecognizerRightTapped = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			var src = PointerRoutedEventArgs.LastPointerEvent?.OriginalSource as UIElement ?? that;

			that.SafeRaiseEvent(RightTappedEvent, new RightTappedRoutedEventArgs(src, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, HoldingEventArgs> OnRecognizerHolding = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			var src = PointerRoutedEventArgs.LastPointerEvent?.OriginalSource as UIElement ?? that;

			that.SafeRaiseEvent(HoldingEvent, new HoldingRoutedEventArgs(src, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, DraggingEventArgs> OnRecognizerDragging = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;

			that.CancelAllDirectManipulations(args.Pointer.Pointer);
			that.OnDragStarting(args);
		};
		#endregion

		private GestureRecognizer CreateGestureRecognizer()
		{
			var recognizer = new GestureRecognizer(this)
			{
				PatchCases = WinRTFeatureConfiguration.GestureRecognizer.PatchCasesForUiElement
			};

			// Allow partial parts to subscribe to pointer events (WASM)
			// or to subscribe to events for platform specific needs (iOS)
			OnGestureRecognizerInitialized(recognizer);

			recognizer.ManipulationStarting += OnRecognizerManipulationStarting;
			recognizer.ManipulationStarted += OnRecognizerManipulationStarted;
			recognizer.ManipulationUpdated += OnRecognizerManipulationUpdated;
			recognizer.ManipulationInertiaStarting += OnRecognizerManipulationInertiaStarting;
			recognizer.ManipulationCompleted += OnRecognizerManipulationCompleted;
			recognizer.Tapped += OnRecognizerTapped;
			recognizer.RightTapped += OnRecognizerRightTapped;
			recognizer.Holding += OnRecognizerHolding;
			recognizer.Dragging += OnRecognizerDragging;

			if (Uno.WinRTFeatureConfiguration.GestureRecognizer.ShouldProvideHapticFeedback)
			{
				recognizer.DragReady += HapticFeedbackWhenReadyToDrag;
			}

			return recognizer;
		}

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer);

		private async void HapticFeedbackWhenReadyToDrag(GestureRecognizer sender, GestureRecognizer.Manipulation args)
		{
			try
			{
				if (await VibrationDevice.RequestAccessAsync() != VibrationAccessStatus.Allowed)
				{
					return;
				}

				var vibrationDevice = await VibrationDevice.GetDefaultAsync();
				if (vibrationDevice is null)
				{
					return;
				}

				var controller = vibrationDevice.SimpleHapticsController;
				var feedback = controller.SupportedFeedback.FirstOrDefault(f => f.Waveform == KnownSimpleHapticsControllerWaveforms.Press);
				if (feedback != null)
				{
					controller.SendHapticFeedback(feedback);
				}
			}
			catch (Exception error)
			{
				this.Log().Error("Haptic feedback for drag failed", error);
			}
		}
		#endregion

		#region Manipulations (recognizer settings / custom bubbling)
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
				if (!IsGestureRecognizerCreated)
				{
					return;
				}
				else
				{
					GestureRecognizer.GestureSettings &= ~GestureSettingsHelper.Manipulations;
					return;
				}
			}

			var settings = GestureRecognizer.GestureSettings;
			settings &= ~GestureSettingsHelper.Manipulations; // Remove all configured manipulation flags
			settings |= mode.ToGestureSettings(); // Then set them back from the mode

			GestureRecognizer.GestureSettings = settings;
		}

		partial void PrepareManagedManipulationEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
		{
			// When we bubble a manipulation event from a child, we make sure to abort any pending gesture/manipulation on the current element
			if (routedEvent != ManipulationStartingEvent && IsGestureRecognizerCreated)
			{
				GestureRecognizer.CompleteGesture();
			}
			// Note: We do not need to alter the location of the events, on UWP they are always relative to the OriginalSource.
		}

		private void CancelAllDirectManipulations(params PointerIdentifier[] identifiers)
		{
#if UNO_HAS_MANAGED_POINTERS
			XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.CancelAllDirectManipulations(identifiers);
#endif
		}

		// Not implemented for native pointers (i.e. !UNO_HAS_MANAGED_POINTERS)
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "__TVOS__", "IS_UNIT_TESTS", "__NETSTD_REFERENCE__")]
		public bool CancelDirectManipulations()
		{
#if UNO_HAS_MANAGED_POINTERS
			return XamlRoot?.VisualTree.ContentRoot.InputManager.Pointers.CancelDirectManipulations(this) ?? false;
#else
			return false;
#endif
		}
		#endregion

		#region Gestures (recognizer settings / custom bubbling / early completion)
		private bool _isGestureCompleted;

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
				GestureRecognizer.GestureSettings |= GestureSettings.Tap;
			}
			else if (routedEvent == DoubleTappedEvent)
			{
				GestureRecognizer.GestureSettings |= GestureSettings.DoubleTap;
			}
			else if (routedEvent == RightTappedEvent)
			{
				GestureRecognizer.GestureSettings |= GestureSettings.RightTap;
			}
			else if (routedEvent == HoldingEvent)
			{
				GestureRecognizer.GestureSettings |= GestureSettings.Hold; // Note: We do not set GestureSettings.HoldWithMouse as WinUI never raises Holding for mouse pointers
			}
		}

		partial void PrepareManagedGestureEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
		{
			if (routedEvent != HoldingEvent && FeatureConfiguration.UIElement.DisablePointersSpecificEventPrevention)
			{
				// If the feature flag is set, call CompleteGesture.
				// This is known to not be correct, but it's there just in case the prevention logic caused regressions.
				CompleteGesture();
				return;
			}

			// When we bubble a gesture event from a child, we make sure to abort any pending gesture/manipulation on the current element
			if (IsGestureRecognizerCreated)
			{
				if (routedEvent == TappedEvent)
				{
					var tappedArgs = (TappedRoutedEventArgs)args;
					GestureRecognizer.PreventEvents(
						new PointerIdentifier((PointerDeviceType)tappedArgs.PointerDeviceType, tappedArgs.PointerId),
						GestureSettings.Tap);
				}
				else if (routedEvent == DoubleTappedEvent)
				{
					var doubleTappedArgs = (DoubleTappedRoutedEventArgs)args;
					GestureRecognizer.PreventEvents(
						new PointerIdentifier((PointerDeviceType)doubleTappedArgs.PointerDeviceType, doubleTappedArgs.PointerId),
						GestureSettings.DoubleTap);
				}
				else if (routedEvent == RightTappedEvent)
				{
					var rightTappedArgs = (RightTappedRoutedEventArgs)args;
					GestureRecognizer.PreventEvents(
						new PointerIdentifier((PointerDeviceType)rightTappedArgs.PointerDeviceType, rightTappedArgs.PointerId),
						GestureSettings.RightTap);
				}
				else if (routedEvent == HoldingEvent)
				{
					var holdingArgs = (HoldingRoutedEventArgs)args;
					GestureRecognizer.PreventEvents(
						new PointerIdentifier((PointerDeviceType)holdingArgs.PointerDeviceType, holdingArgs.PointerId),
						GestureSettings.Hold);
				}
			}
		}

		/// <summary>
		/// Prevents the gesture recognizer to generate a manipulation. It's designed to be invoked in Pointers events handlers.
		/// </summary>
		private protected void CompleteGesture()
		{
			// This flags allow us to complete the gesture on pressed (i.e. even before the gesture started)
			_isGestureCompleted = true;

			if (IsGestureRecognizerCreated)
			{
				GestureRecognizer.CompleteGesture();
			}
		}

		internal void CompleteGesturesOnTree()
		{
			this.CompleteGesture();
			foreach (var element in this.EnumerateAncestors())
			{
				(element as UIElement)?.CompleteGesture();
			}
		}

		private void UpdateRaisedGestureEventsFlag(PointerRoutedEventArgs args)
		{
			if (!IsGestureRecognizerCreated)
			{
				return;
			}

			var pointerId = args.Pointer.PointerId;
			args.GestureEventsAlreadyRaised |= GestureRecognizer.PreventEvents(new PointerIdentifier((PointerDeviceType)args.Pointer.PointerDeviceType, args.Pointer.PointerId), args.GestureEventsAlreadyRaised);
		}
		#endregion

		#region Drag And Drop (recognizer settings / custom bubbling / drag starting event)
		private void UpdateDragAndDrop(bool isEnabled)
		{
			// Note: The drag and drop recognizer setting is only driven by the CanDrag,
			//		 no matter which events are subscribed nor the AllowDrop.

			var settings = GestureRecognizer.GestureSettings;
			settings &= ~GestureSettingsHelper.DragAndDrop; // Remove all configured drag and drop flags
			if (isEnabled)
			{
				settings |= GestureSettings.Drag;
			}

			GestureRecognizer.GestureSettings = settings;
		}

		partial void PrepareManagedDragAndDropEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
		{
			switch (routedEvent.Flag)
			{
				case RoutedEventFlag.DragStarting:
				case RoutedEventFlag.DropCompleted:
					// Those are actually not routed events :O
					bubblingMode = BubblingMode.NoBubbling;
					break;

				case RoutedEventFlag.DragEnter:
					{
						var pt = ((global::Microsoft.UI.Xaml.DragEventArgs)args).SourceId;
						var wasDragOver = IsDragOver(pt);

						// As the IsDragOver is expected to reflect the state of the current element **and the state of its children**,
						// even if the AllowDrop flag has not been set, we have to update the IsDragOver state.
						SetIsDragOver(pt, true);

						if (!AllowDrop // The Drag and Drop "routed" events are raised only on controls that opted-in
							|| wasDragOver) // If we already had a DragEnter do not raise it twice
						{
							bubblingMode = BubblingMode.IgnoreElement;
						}
						break;
					}

				case RoutedEventFlag.DragOver:
					// As the IsDragOver is expected to reflect the state of the current element **and the state of its children**,
					// even if the AllowDrop flag has not been set, we have to update the IsDragOver state.
					SetIsDragOver(((global::Microsoft.UI.Xaml.DragEventArgs)args).SourceId, true);

					if (!AllowDrop) // The Drag and Drop "routed" events are raised only on controls that opted-in
					{
						bubblingMode = BubblingMode.IgnoreElement;
					}
					break;

				case RoutedEventFlag.DragLeave:
				case RoutedEventFlag.Drop:
					{
						var pt = ((global::Microsoft.UI.Xaml.DragEventArgs)args).SourceId;
						var wasDragOver = IsDragOver(pt);

						// As the IsDragOver is expected to reflect the state of the current element **and the state of its children**,
						// even if the AllowDrop flag has not been set, we have to update the IsDragOver state.
						SetIsDragOver(pt, false);

						if (!AllowDrop // The Drag and Drop "routed" events are raised only on controls that opted-in
							|| !wasDragOver) // No Leave or Drop if we was not effectively over ^^
						{
							bubblingMode = BubblingMode.IgnoreElement;
						}
						break;
					}
			}
		}

		[ThreadStatic]
		private static uint _lastDragStartFrameId;

		private void OnDragStarting(DraggingEventArgs args)
		{
			if (args.DraggingState != DraggingState.Started // This UIElement is actually interested only by the starting
				|| !CanDrag // Sanity ... should never happen!
				|| !args.Pointer.Properties.IsLeftButtonPressed

				// As the pointer args are always bubbling (for to properly update pressed/over state and manipulations),
				// if a parent is CanDrag == true, its gesture recognizer might (should) also trigger the DragStarting.
				// But: (1.) on UWP only the top-most draggable element starts the drag operation;
				// (2.) as CoreDragDropManager.AreConcurrentOperationsEnabled is false by default, the parent would cancel the drag of its child
				// So here we allow only one "starting" per "FrameId".
				|| args.Pointer.FrameId <= _lastDragStartFrameId)
			{
				return;
			}

			_lastDragStartFrameId = args.Pointer.FrameId;

			// Note: We do not provide the _pendingRaisedEvent.args since it has probably not been updated yet,
			//		 but as we are in the handler of an event from the gesture recognizer,
			//		 the LastPointerEvent from the CoreWindow will be up to date.
			_ = StartDragAsyncCore(args.Pointer, ptArgs: null, CancellationToken.None);
		}

		public IAsyncOperation<DataPackageOperation> StartDragAsync(PointerPoint pointerPoint)
			=> AsyncOperation.FromTask(ct => StartDragAsyncCore(pointerPoint, _pendingRaisedEvent.args, ct));

		private async Task<DataPackageOperation> StartDragAsyncCore(PointerPoint pointer, PointerRoutedEventArgs ptArgs, CancellationToken ct)
		{
			ptArgs ??= PointerRoutedEventArgs.LastPointerEvent;
			if (ptArgs is null || ptArgs.Pointer.PointerDeviceType != pointer.PointerDeviceType)
			{
				// Fairly impossible case ...
				return DataPackageOperation.None;
			}

			// Note: originalSource = this => DragStarting is not actually a routed event, the original source is always the sender
			var routedArgs = new DragStartingEventArgs(this, ptArgs);
			PrepareShare(routedArgs.Data); // Gives opportunity to the control to fulfill the data
			SafeRaiseEvent(DragStartingEvent, routedArgs); // The event won't bubble, cf. PrepareManagedDragAndDropEventBubbling

			// We need to give a chance for layout updates, etc. This is particularly problematic with TreeView
			// dragging where the DragStarting event on the TreeView will "internally" collapse some nodes,
			// but actually removing them from the visual tree needs a layout cycle. Without waiting here,
			// we can also get a DragEnter event on one of the to-be-collapsed containers in the same
			// pointer event. This crashes the dragging logic.
			// The way WinUI asynchronously handles StartDragAsync (relevant code is in AutomaticDragHelper::AutomaticDragHelper::HandlePointerMovedEventArgs)
			// is not very clear and the way it interacts with layout timing is not similar at all to
			// what we do in Uno, so the closest thing is to wait for Low/Idle here.
			// Note that waiting for Idle causes some unrelated problems in ListView dragging, so we choose Low.
			await NativeDispatcher.Main.EnqueueAsync(() => { }, NativeDispatcherPriority.Low);

			// We capture the original position of the pointer before going async,
			// so we have the closet location of the "down" possible.
			var ptPosition = ptArgs.GetCurrentPoint(this).Position;

			if (routedArgs.Deferral is { } deferral)
			{
				await deferral.Completed(ct);
			}

			if (routedArgs.Cancel)
			{
				// The completed event is not raised if the starting has been cancelled
				throw new TaskCanceledException();
			}

			var dragInfo = new CoreDragInfo(
				source: ptArgs,
				data: routedArgs.Data.GetView(),
				routedArgs.AllowedOperations,
				dragUI: routedArgs.DragUI);

			if (!pointer.Properties.HasPressedButton)
			{
				// This is the UWP behavior: if no button is pressed, then the drag is completed immediately
				OnDropCompleted(dragInfo, DataPackageOperation.None);
				return DataPackageOperation.None;
			}

			if (RenderTargetBitmap.IsImplemented && routedArgs.DragUI.Content is null)
			{
				// Note: Bitmap rendered by the RenderTargetBitmap is in physical pixels,
				//		 so we provide the ActualSize to request the image to be scaled back in logical pixels. 

				var target = new RenderTargetBitmap();
				await target.RenderAsync(this, (int)ActualSize.X, (int)ActualSize.Y);

				routedArgs.DragUI.Content = target;
				routedArgs.DragUI.Anchor = -ptPosition;
			}

			var asyncResult = new TaskCompletionSource<DataPackageOperation>();

			dragInfo.RegisterCompletedCallback(result =>
			{
				OnDropCompleted(dragInfo, result);
				asyncResult.SetResult(result);
			});

			var coreDragDropManager = XamlRoot.GetCoreDragDropManager(XamlRoot);
			coreDragDropManager.DragStarted(dragInfo);
			// Synchronously fire DragEnter+DragOver without waiting for another "mouse tick". This matches WinUI.
			coreDragDropManager.ProcessMoved(ptArgs);

			var result = await asyncResult.Task;

			return result;
		}

		private void OnDropCompleted(CoreDragInfo info, DataPackageOperation result)
			// Note: originalSource = this => DropCompleted is not actually a routed event, the original source is always the sender
			=> SafeRaiseEvent(DropCompletedEvent, new DropCompletedEventArgs(this, info, result));

		/// <summary>
		/// Provides ability to a control to fulfill the data that is going to be shared, by drag-and-drop for instance.
		/// </summary>
		/// <remarks>This is expected to be overriden by controls like Image or TextBlock to self fulfill data.</remarks>
		/// <param name="data">The <see cref="DataPackage"/> to fulfill.</param>
		private protected virtual void PrepareShare(DataPackage data)
		{
		}

		internal void RaiseDragEnterOrOver(global::Microsoft.UI.Xaml.DragEventArgs args)
		{
			var evt = IsDragOver(args.SourceId)
				? DragOverEvent
				: DragEnterEvent;

			(_draggingOver ??= new HashSet<long>()).Add(args.SourceId);

			SafeRaiseEvent(evt, args);
		}

		internal void RaiseDragLeave(global::Microsoft.UI.Xaml.DragEventArgs args, UIElement upTo = null)
		{
			if (_draggingOver?.Remove(args.SourceId) ?? false)
			{
				SafeRaiseEvent(DragLeaveEvent, args, BubblingContext.BubbleUpTo(upTo));
			}
		}

		internal void RaiseDrop(global::Microsoft.UI.Xaml.DragEventArgs args)
		{
			if (_draggingOver?.Remove(args.SourceId) ?? false)
			{
				SafeRaiseEvent(DropEvent, args);
			}
		}
		#endregion

		partial void PrepareManagedPointerEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
		{
			var ptArgs = (PointerRoutedEventArgs)args;
			switch (routedEvent.Flag)
			{
				case RoutedEventFlag.PointerEntered:
					if (IsOver(ptArgs.Pointer))
					{
						// If pointer is already flagged as "over",
						// it's the same on parents and we should just stop bubbling right now.
						// (Depending of the platform, this might be really common to reach this case.)
						bubblingMode = BubblingMode.NoBubbling;
					}
					else
					{
						OnPointerEnter(ptArgs, BubblingContext.OnManagedBubbling);
					}
					break;
				case RoutedEventFlag.PointerPressed:
					OnPointerDown(ptArgs, BubblingContext.OnManagedBubbling);
					break;
				case RoutedEventFlag.PointerMoved:
#if __APPLE_UIKIT__ || __ANDROID__
					OnNativePointerMoveWithOverCheck(ptArgs, ptArgs.IsPointCoordinatesOver(this), BubblingContext.OnManagedBubbling);
#else
					OnPointerMove(ptArgs, BubblingContext.OnManagedBubbling);
#endif
					break;
				case RoutedEventFlag.PointerReleased:
					OnPointerUp(ptArgs, BubblingContext.OnManagedBubbling);

					break;
				case RoutedEventFlag.PointerExited:
					// Here the pointer has crossed the boundaries of an element and the event is bubbling in managed code
					// (either because there is no native bubbling, either because is has been handled).
					// On UWP, the 'exit' is bubbling/raised only on elements where the pointer is effectively no longer "over" it.
#if UNO_HAS_MANAGED_POINTERS
					// On Skia and macOS the pointer exit is raised properly by the PointerManager with a "Root" (a.k.a. UpTo) element.
					// If we are here, it means that we just have to update private state and let the bubbling algorithm do its job!
					// Debug.Assert(IsOver(ptArgs.Pointer)); // Fails when fast scrolling samples categories list on Skia
					OnPointerExited(ptArgs, BubblingContext.OnManagedBubbling);
#else
#if __APPLE_UIKIT__
					// On iOS all pointers are handled just like if they were touches by the platform and there isn't any notion of "over".
					// So we can consider pointer over as soon as is touching the screen while being within element bounds.
					var isOver = ptArgs.Pointer.IsInContact && ptArgs.IsPointCoordinatesOver(this);
#else // __WASM__ || __ANDROID__
					// On WASM the pointer 'exit' is raise by the platform for all pointer types,
					// while on Android they are raised only for mouses and pens (i.e. not for touch).
					// (For touch on Android we are "re-dispatching exit" in managed code only (i.e. we will pass here)
					// when we receive the 'up' in the 'RootVisual' or after having completed bubbling of the 'up' it in managed code).
					// For both platforms, we validate that the pointer is effectively within the element bounds,
					// with an exception for touch which has no notion of "over": if not "in contact" (i.e. no longer touching the screen),
					// no matter the location, we consider the pointer has out.

					// TODO: Once IsInRange has been implemented on all platform, we can simplify this condition to something like
					//		 ptArgs.Pointer.IsInRange && ptArgs.IsPointCoordinatesOver(this) (and probably share it on all platforms).
					var isOver = ptArgs.Pointer.IsInRange && (ptArgs.Pointer.PointerDeviceType, ptArgs.Pointer.IsInContact) switch
					{
#if HAS_UNO_WINUI
						(global::Microsoft.UI.Input.PointerDeviceType.Touch, false) => false,
#else
						(PointerDeviceType.Touch, false) => false,
#endif
						_ => ptArgs.IsPointCoordinatesOver(this),
					};
#endif

					if (isOver)
					{
						bubblingMode = BubblingMode.NoBubbling;
					}
					else
					{
						OnPointerExited(ptArgs, BubblingContext.OnManagedBubbling);
					}
#endif
					break;
				case RoutedEventFlag.PointerCanceled:
				case RoutedEventFlag.PointerCaptureLost when ptArgs.CanceledByDirectManipulation:
					OnPointerCancel(ptArgs, BubblingContext.OnManagedBubbling);
					break;

					// No local state (over/pressed/manipulation/gestures) to update for
					//	- PointerCaptureLost:
					//	- PointerWheelChanged:
			}
		}

		#region Partial API to raise pointer events and gesture recognition (OnNative***)

		internal bool OnPointerEnter(PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			// We override the isOver for the relevancy check as we will update it right after.
			var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver: true);
			if (!isOverOrCaptured)
			{
				// We receive this event due to implicit capture, just ignore it locally and let is bubble
				// note: If bubbling in managed then we are going to ignore element anyway as ctx is flagged as IsInternal.
				// note 2: This case is actually impossible (when implicitly captured, we don't receive native enter)!
				ctx = ctx.WithMode(ctx.Mode | BubblingMode.IgnoreElement);
			}

			UpdateRaisedGestureEventsFlag(args);
			var handledInManaged = SetOver(args, true, ctx);

			return handledInManaged;
		}

		internal bool OnPointerDown(PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			_isGestureCompleted = false;

			// "forceRelease: true": as we are in pointer pressed, if the pointer is already captured,
			// it due to an invalid state. So here we make sure to not stay in an invalid state that would
			// prevent any interaction with the application.
			var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver: true, forceRelease: true);
			if (!isOverOrCaptured)
			{
				// We receive this event due to implicit capture, just ignore it locally and let is bubble
				// note: If bubbling in managed then we are going to ignore the element anyway as ctx is flagged as IsInternal.
				// note 2: This case is actually impossible (no implicit capture on down)!
				ctx = ctx.WithMode(ctx.Mode | BubblingMode.IgnoreElement);
			}

			UpdateRaisedGestureEventsFlag(args);
			var handledInManaged = SetPressed(args, true, ctx);

			if (PointerRoutedEventArgs.PlatformSupportsNativeBubbling && !ctx.IsInternal && !isOverOrCaptured)
			{
				// This case is for safety only, it should not happen as we should never get a Pointer down while not
				// on this UIElement, and no capture should prevent the dispatch as no parent should hold a capture at this point.
				// (Even if a Parent of this listen on pressed on a child of this and captured the pointer, the FrameId will be
				// the same so we won't consider this event as irrelevant)

				return handledInManaged; // always false, as the 'pressed' event was mute
			}

			if (!_isGestureCompleted && IsGestureRecognizerCreated)
			{
				// We need to process only events that are bubbling natively to this control,
				// if they are bubbling in managed it means that they were handled by a child control,
				// so we should not use them for gesture recognition.

				var recognizer = GestureRecognizer;
				var point = args.GetCurrentPoint(this);

				recognizer.ProcessDownEvent(point);

#if !HAS_NATIVE_IMPLICIT_POINTER_CAPTURE
				if (recognizer.PendingManipulation?.IsActive(point.Pointer) ?? false)
				{
					Capture(args.Pointer, PointerCaptureKind.Implicit, default, args);
				}
#endif
			}

			if (!ManipulationMode.HasFlag(ManipulationModes.System))
			{
				CancelDirectManipulations();
			}

			return handledInManaged;
		}

		internal bool OnPointerMove(PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args);

			UpdateRaisedGestureEventsFlag(args);
			if (!ctx.IsInternal && isOverOrCaptured)
			{
				// If this pointer was wrongly dispatched here (out of the bounds and not captured),
				// we don't raise the 'move' event

				args.Handled = false;
				handledInManaged |= RaisePointerEvent(PointerMovedEvent, args);
			}

#if !UNO_HAS_MANAGED_POINTERS
			if (IsGestureRecognizerCreated)
			{
				// We need to process only events that were not handled by a child control,
				// so we should not use them for gesture recognition.
				var gestures = GestureRecognizer;
				gestures.ProcessMoveEvents(args.GetIntermediatePoints(this));
				if (gestures.IsDragging)
				{
					XamlRoot.GetCoreDragDropManager(XamlRoot).ProcessMoved(args);
				}
			}
#endif

			return handledInManaged;
		}

		internal bool OnPointerUp(PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args);
			if (!isOverOrCaptured)
			{
				// We receive this event due to implicit capture, just ignore it locally and let is bubble
				// note: If bubbling in managed then we are going to ignore the element anyway as ctx is flagged as IsInternal.
				ctx = ctx.WithMode(ctx.Mode | BubblingMode.IgnoreElement);
			}

			// Note: We process the UpEvent between Release and Exited as the gestures like "Tap"
			//		 are fired between those events.
			var currentPoint = default(PointerPoint);
			if (IsGestureRecognizerCreated)
			{
				currentPoint = args.GetCurrentPoint(this);
				UpdateRaisedGestureEventsFlag(args);
				GestureRecognizer.ProcessBeforeUpEvent(currentPoint, isOverOrCaptured && !ctx.IsCleanup);
			}

			handledInManaged |= SetPressed(args, false, ctx);

			// Note: We process the UpEvent between Release and Exited as the gestures like "Tap"
			//		 are fired between those events.
			if (IsGestureRecognizerCreated)
			{
				// We need to process only events that are bubbling natively to this control (i.e. isOverOrCaptured == true),
				// if they are bubbling in managed it means that they where handled a child control,
				// so we should not use them for gesture recognition.
				var isDragging = GestureRecognizer.IsDragging;
				GestureRecognizer.ProcessUpEvent(currentPoint, isOverOrCaptured && !ctx.IsCleanup);
				if (isDragging)
				{
					XamlRoot.GetCoreDragDropManager(XamlRoot).ProcessDropped(args);
				}
			}

			return handledInManaged;
		}

		internal bool OnPointerExited(PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args);
			if (!isOverOrCaptured)
			{
				// We receive this event due to implicit capture, just ignore it locally and let is bubble
				// note: If bubbling in managed then we are going to ignore element anyway as ctx is flagged as IsInternal.
				// note 2: This case is actually impossible (when implicitly captured, we don't receive native exit)!
				ctx = ctx.WithMode(ctx.Mode | BubblingMode.IgnoreElement);
			}

			UpdateRaisedGestureEventsFlag(args);
			handledInManaged |= SetOver(args, false, ctx);

			if (IsGestureRecognizerCreated && GestureRecognizer.IsDragging)
			{
				XamlRoot.GetCoreDragDropManager(XamlRoot).ProcessMoved(args);
			}

			return handledInManaged;
		}

		internal bool OnPointerCancel(PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			var isOverOrCaptured = ValidateAndUpdateCapture(args); // Check this *before* updating the pressed / over states!

			UpdateRaisedGestureEventsFlag(args);

			// When a pointer is cancelled / swallowed by the system, we don't even receive "Released" nor "Exited"
			// We update only local state as the Cancel is bubbling itself
			// Note: Make sure to keep the IsInternal / IsCleanup flags if set!
			SetPressed(args, false, ctx.WithMode(BubblingMode.NoBubbling));
			SetOver(args, false, ctx.WithMode(BubblingMode.NoBubbling));

			if (IsGestureRecognizerCreated)
			{
				GestureRecognizer.CompleteGesture();
				if (GestureRecognizer.IsDragging)
				{
					XamlRoot.GetCoreDragDropManager(XamlRoot).ProcessAborted(args.Pointer.PointerId);
				}
			}

			if (!isOverOrCaptured)
			{
				return false;
			}

			var handledInManaged = false;
			if (args.CanceledByDirectManipulation)
			{
				handledInManaged |= SetNotCaptured(args, forceCaptureLostEvent: !ctx.IsInternal);
			}
			else
			{
				args.Handled = false;
				handledInManaged |= !ctx.IsInternal && RaisePointerEvent(PointerCanceledEvent, args);
				handledInManaged |= SetNotCaptured(args);
			}

			return handledInManaged;
		}

		internal bool OnPointerWheel(PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			return RaisePointerEvent(PointerWheelChangedEvent, args);
		}

		private static (UIElement sender, RoutedEvent @event, PointerRoutedEventArgs args) _pendingRaisedEvent;
		private bool RaisePointerEvent(RoutedEvent evt, PointerRoutedEventArgs args, BubblingContext ctx = default)
		{
			if (ctx.IsInternal)
			{
				// If the event has been flagged as internal it means that it's bubbling in managed code,
				// so the RaiseEvent won't do anything. This check only avoids a potentially costly try/finally.
				// NOte: We return 'args.Handled' just like the RaiseEvent would do, but it's expected to not be used in that case.
				return args.Handled;
			}

			var originalPending = _pendingRaisedEvent;

			try
			{
				/// <remarks>
				/// This method runs in a separate method in order to workaround for the following issue:
				/// https://github.com/dotnet/runtime/issues/111281
				/// which prevents AOT on WebAssembly when try/catch/finally are found in the same method.
				/// </remarks>
				bool InnerRaiseEvent(
					RoutedEvent evt,
					PointerRoutedEventArgs args,
					BubblingContext ctx,
					ref (UIElement sender, RoutedEvent @event, PointerRoutedEventArgs args) pendingRaisedEvent)
				{
					try
					{
						_pendingRaisedEvent = (this, evt, args);

						return RaiseEvent(evt, args, ctx);
					}
					catch (Exception e)
					{
						if (this.Log().IsEnabled(LogLevel.Error))
						{
							this.Log().Error($"Failed to raise '{evt.Name}': {e}");
						}

						return false;
					}
				}

				return InnerRaiseEvent(evt, args, ctx, ref _pendingRaisedEvent);
			}
			finally
			{
				_pendingRaisedEvent = originalPending;
			}
		}
		#endregion

		#region Pointer over state (Updated by the partial API OnNative***, should not be updated externaly)
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

		private bool SetOver(PointerRoutedEventArgs args, bool isOver, BubblingContext ctx)
		{
			var wasOver = IsPointerOver;
			IsPointerOver = isOver;

			var hasNotChanged = wasOver == isOver;
			if (hasNotChanged && (ctx.IsInternal || ctx.IsCleanup))
			{
				// Already bubbling in managed, and didn't changed, nothing to do!
				return false;
			}

			if (hasNotChanged)
			{
				// Unlike up/down we have to locally raise the enter/exit only if something has changed!
				//
				// Note: But even if the state didn't changed locally (hasNotChanged),
				//		 we still raise propagate the event to parents so they can updates their internal state.
				//		 If 'isOver==true', it's only for safety as parent should never be flagged as not hovered if we are,
				//		 and if 'isOver==false' this will ensure that parents are up-to-date.
				// Note: Unlike up/down we should not get this due to an "implicit capture" (iOS, Android, WASM-touch),
				//		 as we don't get any native enter/exit in that case.

				ctx = ctx.WithMode(ctx.Mode | BubblingMode.IgnoreElement);
			}

			if (isOver) // Entered
			{
#if __SKIA__
				if (!wasOver)
				{
					// Currently works on Wasm Skia only.
					string text = (this as TextBlock)?.Text;
					Uno.Helpers.AccessibilityAnnouncer.AnnouncePolite(text);
				}
#endif
				return RaisePointerEvent(PointerEnteredEvent, args, ctx);
			}
			else // Exited
			{
				return RaisePointerEvent(PointerExitedEvent, args, ctx);
			}
		}
		#endregion

		#region Pointer pressed state (Updated by the partial API OnNative***, should not be updated externaly)
		private readonly HashSet<uint> _pressedPointers = new();

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
		internal bool IsPointerPressed => _pressedPointers.Count != 0;

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
		/// <remarks>
		/// Note that on UWP the "pressed" state is managed **PER POINTER**, and not per pressed button on the given pointer.
		/// It means that with a mouse if you follow this sequence : press left => press right => release right => release left,
		/// you will get only one 'PointerPressed' and one 'PointerReleased'.
		/// Same thing if you release left first (press left => press right => release left => release right), and for the pen's barrel button.
		/// </remarks>
		internal bool IsPressed(Pointer pointer) => _pressedPointers.Contains(pointer.PointerId);

		internal bool IsPressed(uint pointerId) => _pressedPointers.Contains(pointerId);

		private bool SetPressed(PointerRoutedEventArgs args, bool isPressed, BubblingContext ctx)
		{
			var wasPressed = IsPressed(args.Pointer);
			var hasNotChanged = wasPressed == isPressed;

			if (hasNotChanged && (ctx.IsInternal || ctx.IsCleanup))
			{
				// Already bubbling in managed, and didn't changed, nothing to do!
				return false;
			}

			// Note: Even if the state didn't changed locally (hasNotChanged),
			//		 we still have to raise the event locally and on parents like on UWP.
			//		 If this is being invoke due to an "implicit capture" (iOS, Android, WASM-touch),
			//		 the ctx is already flagged with BubblingMode.IgnoreElement (cf. isOverOrCaptured in OnDown|up)
			//		 (and if CanBubbleNatively the event won't be raised at all).

			if (isPressed) // Pressed
			{
				if (hasNotChanged is false)
				{
					_pressedPointers.Add(args.Pointer.PointerId);
				}

				return RaisePointerEvent(PointerPressedEvent, args, ctx);
			}
			else // Released
			{
				if (hasNotChanged is false)
				{
					_pressedPointers.Remove(args.Pointer.PointerId);
				}

				return RaisePointerEvent(PointerReleasedEvent, args, ctx);
			}
		}

		private void ClearPressed() => _pressedPointers.Clear();
		#endregion

		#region Pointer capture state (Updated by the partial API OnNative***, should not be updated externaly)
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

		/// <summary>
		/// DO NOT USE
		/// This is the backing field of <see cref="PointerCaptures"/>.
		/// It's internal to be accessible to the <see cref="PointerCapture"/> and must not be used directly by any other code!
		/// Use dedicated APIs like <see cref="CapturePointer(Pointer)"/>, <see cref="ReleasePointerCapture(Pointer)"/> and <see cref="IsCaptured(Pointer)"/> instead.
		/// </summary>
		internal List<Pointer> PointerCapturesBackingField;

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
		internal new bool HasPointerCapture => (PointerCapturesBackingField?.Count ?? 0) != 0;
#else
		internal bool HasPointerCapture => (PointerCapturesBackingField?.Count ?? 0) != 0;
#endif

		internal bool IsCaptured(Pointer pointer)
			=> HasPointerCapture
				&& PointerCapture.TryGet(pointer, out var capture)
				&& capture.IsTarget(this, PointerCaptureKind.Explicit);

		internal bool IsCaptured(Pointer pointer, PointerCaptureKind kinds)
			=> PointerCapture.TryGet(pointer, out var capture)
				&& capture.IsTarget(this, kinds);

		public bool CapturePointer(Pointer value)
		{
			var pointer = value ?? throw new ArgumentNullException(nameof(value));

			return Capture(pointer, PointerCaptureKind.Explicit, PointerCaptureOptions.None, _pendingRaisedEvent.args) is not PointerCaptureResult.Failed;
		}

		internal PointerCaptureResult CapturePointer(Pointer value, PointerCaptureKind kind = PointerCaptureKind.Explicit, PointerCaptureOptions options = PointerCaptureOptions.None)
		{
			var pointer = value ?? throw new ArgumentNullException(nameof(value));

			return Capture(pointer, kind, options, _pendingRaisedEvent.args);
		}

		public void ReleasePointerCapture(Pointer value)
			=> ReleasePointerCapture((value ?? throw new ArgumentNullException(nameof(value))).UniqueId, muteEvent: false);

		/// <summary>
		/// Release a pointer capture with the ability to not raise the <see cref="PointerCaptureLost"/> event (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// On some controls we use the Capture to track the pressed state properly, to detect click.  But in few cases (i.e. Hyperlink)
		/// UWP does not raise a PointerCaptureLost. This method give the ability to easily follow this behavior without requiring
		/// the control to track and handle the event.
		/// </remarks>
		/// <param name="pointer">The pointer to release.</param>
		/// <param name="muteEvent">Determines if the event should be raised or not.</param>
		/// <param name="kinds">The kind of captures to release.</param>
		internal void ReleasePointerCapture(global::Windows.Devices.Input.PointerIdentifier pointer, bool muteEvent = false, PointerCaptureKind kinds = PointerCaptureKind.Explicit)
		{
			if (!Release(pointer, kinds, muteEvent: muteEvent)
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

		private bool ValidateAndUpdateCapture(PointerRoutedEventArgs args)
			=> ValidateAndUpdateCapture(args, IsOver(args.Pointer));

#pragma warning disable IDE0051 // Used by native implementation
		private bool ValidateAndUpdateCapture(PointerRoutedEventArgs args, out bool isOver)
			=> ValidateAndUpdateCapture(args, isOver = IsOver(args.Pointer));
#pragma warning restore IDE0051

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

#if UNO_HAS_MANAGED_POINTERS
			if (PointerCapture.TryGet(args.Pointer, out var capture))
			{
				capture.ValidateAndUpdate(this, args, autoRelease: false); // autoRelease: false: With managed pointers, captures are released by the InputManager
			}

			return true; // With managed pointers, the pointer is always over the element!
#else
			if (PointerCapture.TryGet(args.Pointer, out var capture))
			{
				return capture.ValidateAndUpdate(this, args, forceRelease);
			}
			else
			{
				return isOver;
			}
#endif
		}

		private bool SetNotCaptured(PointerRoutedEventArgs args, bool forceCaptureLostEvent = false)
		{
			if (Release(args.Pointer.UniqueId, PointerCaptureKind.Any, args))
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

		private PointerCaptureResult Capture(Pointer pointer, PointerCaptureKind kind, PointerCaptureOptions opts, PointerRoutedEventArgs relatedArgs)
		{
			if (PointerCapturesBackingField == null)
			{
				PointerCapturesBackingField = new List<Pointer>();
				this.SetValue(PointerCapturesProperty, PointerCapturesBackingField); // Note: On UWP this is done only on first capture (like here)
			}

			return PointerCapture.GetOrCreate(pointer).TryAddTarget(this, kind, opts, relatedArgs);
		}

		private void Release(PointerCaptureKind kinds, PointerRoutedEventArgs relatedArgs = null, bool muteEvent = false)
		{
			if (PointerCapture.Any(out var captures))
			{
				foreach (var capture in captures)
				{
					Release(capture, kinds, relatedArgs, muteEvent);
				}
			}
		}

		private bool Release(global::Windows.Devices.Input.PointerIdentifier pointer, PointerCaptureKind kinds, PointerRoutedEventArgs relatedArgs = null, bool muteEvent = false)
		{
			return PointerCapture.TryGet(pointer, out var capture)
				&& Release(capture, kinds, relatedArgs, muteEvent);
		}

		private bool Release(PointerCapture capture, PointerCaptureKind kinds, PointerRoutedEventArgs relatedArgs = null, bool muteEvent = false)
		{
			if (!capture.RemoveTarget(this, kinds, out var lastDispatched).HasFlag(PointerCaptureKind.Explicit))
			{
				return false;
			}

			if (muteEvent)
			{
				return false;
			}

			relatedArgs ??= lastDispatched;
			if (relatedArgs == null)
			{
				return false;
			}

#if UNO_HAS_MANAGED_POINTERS
			return RaisePointerEvent(PointerCaptureLostEvent, new PointerRoutedEventArgs(relatedArgs.CoreArgs, relatedArgs.OriginalSource as UIElement));
#else // TODO: do the same for unmanaged pointers
			relatedArgs.Handled = false;
			return RaisePointerEvent(PointerCaptureLostEvent, relatedArgs);
#endif
		}
		#endregion

		#region Drag state (Updated by the RaiseDrag***, should not be updated externaly)
		private HashSet<long> _draggingOver;

		/// <summary>
		/// Gets a boolean which indicates if there is currently a Drag and Drop operation pending over this element.
		/// This indicates that a **data package is currently being dragged over this element and might be dropped** on this element or one of its children,
		/// and not that this element nor one of its children element is being drag.
		/// As this flag reflect the state of the element **or one of its children**, it might be True event if `DropAllowed` is `false`.
		/// </summary>
		/// <param name="pointer">The pointer associated to the drag and drop operation</param>
		internal bool IsDragOver(Pointer pointer)
			=> _draggingOver?.Contains(pointer.UniqueId) ?? false;

		internal bool IsDragOver(long sourceId)
			=> _draggingOver?.Contains(sourceId) ?? false;

		private void SetIsDragOver(long sourceId, bool isOver)
		{
			if (isOver)
			{
				(_draggingOver ??= new HashSet<long>()).Add(sourceId);
			}
			else
			{
				_draggingOver?.Remove(sourceId);
			}
		}

		private void ClearDragOver()
		{
			_draggingOver?.Clear();
		}
		#endregion
	}
}
