using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.ApplicationModel.DataTransfer.DragDrop.Core;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Core;
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
			}
		}

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

		private /* readonly but partial */ Lazy<GestureRecognizer> _gestures;

		/// <summary>
		/// Validates that this element is able to manage pointer events.
		/// If this element is only the shadow of a ghost native view that was instantiated for marshalling purposes by Xamarin,
		/// the _gestures will be null and trying to interpret a native pointer event might crash the app.
		/// This flag should be checked when receiving a pointer related event from the native view to prevent this case.
		/// </summary>
		private bool IsPointersSuspended => _gestures == null;

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

			if (sender is UIElement elt && elt.GetHitTestVisibility() == HitTestability.Collapsed)
			{
				elt.Release(PointerCaptureKind.Any);
				elt.ClearPressed();
				elt.SetOver(null, false, muteEvent: true);
				elt.ClearDragOver();
			}
		};

		private static readonly RoutedEventHandler ClearPointersStateOnUnload = (object sender, RoutedEventArgs args) =>
		{
			if (sender is UIElement elt)
			{
				elt.Release(PointerCaptureKind.Any);
				elt.ClearPressed();
				elt.SetOver(null, false, muteEvent: true);
				elt.ClearDragOver();
			}
		};

		/// <summary>
		/// Indicates if this element or one of its child might be target pointer pointer events.
		/// Be aware this doesn't means that the element itself can be actually touched by user,
		/// but only that pointer events can be raised on this element.
		/// I.e. this element is NOT <see cref="HitTestability.Collapsed"/>.
		/// </summary>
		internal HitTestability GetHitTestVisibility()
		{
#if __WASM__ || __SKIA__
			return (HitTestability)this.GetValue(HitTestVisibilityProperty);
#else
			// This is a coalesced HitTestVisible and should be unified with it
			// We should follow the WASM way and unify it on all platforms!
			// Note: This is currently only a port of the old behavior and reports only Collapsed and Visible.
			if (Visibility != Visibility.Visible || !IsHitTestVisible)
			{
				return HitTestability.Collapsed;
			}

			if (this is Windows.UI.Xaml.Controls.Control ctrl)
			{
				return ctrl.IsLoaded && ctrl.IsEnabled
					? HitTestability.Visible
					: HitTestability.Collapsed;
			}
			else if (this is Windows.UI.Xaml.FrameworkElement fwElt)
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

		private static readonly TypedEventHandler<GestureRecognizer, RightTappedEventArgs> OnRecognizerRightTapped = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			that.SafeRaiseEvent(RightTappedEvent, new RightTappedRoutedEventArgs(that, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, HoldingEventArgs> OnRecognizerHolding = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			that.SafeRaiseEvent(HoldingEvent, new HoldingRoutedEventArgs(that, args));
		};

		private static readonly TypedEventHandler<GestureRecognizer, DraggingEventArgs> OnRecognizerDragging = (sender, args) =>
		{
			var that = (UIElement)sender.Owner;
			that.OnDragStarting(args);
		};
		#endregion

		private GestureRecognizer CreateGestureRecognizer()
		{
			var recognizer = new GestureRecognizer(this);

			recognizer.ManipulationStarting += OnRecognizerManipulationStarting;
			recognizer.ManipulationStarted += OnRecognizerManipulationStarted;
			recognizer.ManipulationUpdated += OnRecognizerManipulationUpdated;
			recognizer.ManipulationInertiaStarting += OnRecognizerManipulationInertiaStarting;
			recognizer.ManipulationCompleted += OnRecognizerManipulationCompleted;
			recognizer.Tapped += OnRecognizerTapped;
			recognizer.RightTapped += OnRecognizerRightTapped;
			recognizer.Holding += OnRecognizerHolding;
			recognizer.Dragging += OnRecognizerDragging;

			// Allow partial parts to subscribe to pointer events (WASM)
			OnGestureRecognizerInitialized(recognizer);

			return recognizer;
		}

		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer);
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

		partial void PrepareManagedManipulationEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
		{
			// When we bubble a manipulation event from a child, we make sure to abort any pending gesture/manipulation on the current element
			if (routedEvent != ManipulationStartingEvent && _gestures.IsValueCreated)
			{
				_gestures.Value.CompleteGesture();
			}
			// Note: We do not need to alter the location of the events, on UWP they are always relative to the OriginalSource.
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
				_gestures.Value.GestureSettings |= GestureSettings.Tap;
			}
			else if (routedEvent == DoubleTappedEvent)
			{
				_gestures.Value.GestureSettings |= GestureSettings.DoubleTap;
			}
			else if (routedEvent == RightTappedEvent)
			{
				_gestures.Value.GestureSettings |= GestureSettings.RightTap;
			}
			else if (routedEvent == HoldingEvent)
			{
				_gestures.Value.GestureSettings |= GestureSettings.Hold; // Note: We do not set GestureSettings.HoldWithMouse as WinUI never raises Holding for mouse pointers
			}
		}

		partial void PrepareManagedGestureEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
		{
			// When we bubble a gesture event from a child, we make sure to abort any pending gesture/manipulation on the current element
			if (routedEvent == HoldingEvent)
			{
				if (_gestures.IsValueCreated)
				{
					_gestures.Value.PreventHolding(((HoldingRoutedEventArgs)args).PointerId);
				}
			}
			else
			{
				// Note: Here we should prevent only the same gesture ... but actually currently supported gestures
				// are mutually exclusive, so if a child element detected a gesture, it's safe to prevent all of them.
				CompleteGesture(); // Make sure to set the flag _isGestureCompleted, so won't try to recognize double tap
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

		#region Drag And Drop (recognizer settings / custom bubbling / drag starting event)
		private void UpdateDragAndDrop(bool isEnabled)
		{
			// Note: The drag and drop recognizer setting is only driven by the CanDrag,
			//		 no matter which events are subscribed nor the AllowDrop.

			var settings = _gestures.Value.GestureSettings;
			settings &= ~GestureSettingsHelper.DragAndDrop; // Remove all configured drag and drop flags
			if (isEnabled)
			{
				settings |= GestureSettings.Drag;
			}

			_gestures.Value.GestureSettings = settings;
		}

		partial void PrepareManagedDragAndDropEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode)
		{
			switch (routedEvent.Flag)
			{
				case RoutedEventFlag.DragStarting:
				case RoutedEventFlag.DropCompleted:
					// Those are actually not routed events :O
					bubblingMode = BubblingMode.StopBubbling;
					break;

				case RoutedEventFlag.DragEnter:
				{
					var pt = ((global::Windows.UI.Xaml.DragEventArgs)args).SourceId;
					var wasDragOver = IsDragOver(pt);

					// As the IsDragOver is expected to reflect teh state of the current element **and the state of its children**,
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
					// As the IsDragOver is expected to reflect teh state of the current element **and the state of its children**,
					// even if the AllowDrop flag has not been set, we have to update the IsDragOver state.
					SetIsDragOver(((global::Windows.UI.Xaml.DragEventArgs)args).SourceId, true);

					if (!AllowDrop) // The Drag and Drop "routed" events are raised only on controls that opted-in
					{
						bubblingMode = BubblingMode.IgnoreElement;
					}
					break;

				case RoutedEventFlag.DragLeave:
				case RoutedEventFlag.Drop:
				{
					var pt = ((global::Windows.UI.Xaml.DragEventArgs)args).SourceId;
					var wasDragOver = IsDragOver(pt);

					// As the IsDragOver is expected to reflect teh state of the current element **and the state of its children**,
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

		private void OnDragStarting(DraggingEventArgs args)
		{
			if (args.DraggingState != DraggingState.Started // This UIElement is actually interested only by the starting
				|| !CanDrag // Sanity ... should never happen!
				|| !args.Pointer.Properties.IsLeftButtonPressed)
			{
				return;
			}

			// Note: We do not provide the _pendingRaisedEvent.args since it has probably not been updated yet,
			//		 but as we are in the handler of an event from the gesture recognizer,
			//		 the LastPointerEvent from the CoreWindow will be up to date.
			StartDragAsyncCore(args.Pointer, ptArgs: null, CancellationToken.None);
		}

		public IAsyncOperation<DataPackageOperation> StartDragAsync(PointerPoint pointerPoint)
			=> AsyncOperation.FromTask(ct => StartDragAsyncCore(pointerPoint, _pendingRaisedEvent.args, ct));

		private Task<DataPackageOperation> StartDragAsyncCore(PointerPoint pointer, PointerRoutedEventArgs ptArgs, CancellationToken ct)
		{
			ptArgs ??= CoreWindow.GetForCurrentThread()!.LastPointerEvent as PointerRoutedEventArgs;
			if (ptArgs is null || ptArgs.Pointer.PointerDeviceType != pointer.PointerDevice.PointerDeviceType)
			{
				// Fairly impossible case ...
				return Task.FromResult(DataPackageOperation.None);
			}

			// Note: originalSource = this => DragStarting is not actually a routed event, the original source is always the sender
			var routedArgs = new DragStartingEventArgs(this, ptArgs);
			PrepareShare(routedArgs.Data); // Gives opportunity to the control to fulfill the data
			SafeRaiseEvent(DragStartingEvent, routedArgs); // The event won't bubble, cf. PrepareManagedDragAndDropEventBubbling

			// TODO: Add support for the starting deferral!

			if (routedArgs.Cancel)
			{
				// The completed event is not raised if the starting has been cancelled
				return Task.FromCanceled<DataPackageOperation>(CancellationToken.None);
			}

			if (!pointer.Properties.HasPressedButton)
			{
				// This is the UWP behavior: if no button is pressed, then the drag is completed immediately
				var noneResult = Task.FromResult(DataPackageOperation.None);
				OnDragCompleted(noneResult, this);
				return noneResult;
			}

			var result = new TaskCompletionSource<DataPackageOperation>();
			result.Task.ContinueWith(OnDragCompleted, this, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.RunContinuationsAsynchronously);

			var dragInfo = new CoreDragInfo(
				source: ptArgs,
				data: routedArgs.Data.GetView(),
				routedArgs.AllowedOperations,
				dragUI: routedArgs.DragUI);
			dragInfo.RegisterCompletedCallback(result.SetResult);

			CoreDragDropManager.GetForCurrentView()!.DragStarted(dragInfo);

			return result.Task;
		}

		private static void OnDragCompleted(Task<DataPackageOperation> resultTask, object snd)
		{
			var that = (UIElement)snd;
			var result = resultTask.IsFaulted
				? DataPackageOperation.None
				: resultTask.Result;
			// Note: originalSource = this => DropCompleted is not actually a routed event, the original source is always the sender
			var args = new DropCompletedEventArgs(that, result); 

			that.SafeRaiseEvent(DropCompletedEvent, args);
		}

		/// <summary>
		/// Provides ability to a control to fulfill the data that is going to be shared, by drag-and-drop for instance.
		/// </summary>
		/// <remarks>This is expected to be overriden by controls like Image or TextBlock to self fulfill data.</remarks>
		/// <param name="data">The <see cref="DataPackage"/> to fulfill.</param>
		private protected virtual void PrepareShare(DataPackage data)
		{
		}

		internal void RaiseDragEnterOrOver(global::Windows.UI.Xaml.DragEventArgs args)
		{
			var evt = IsDragOver(args.SourceId)
				? DragOverEvent
				: DragEnterEvent;

			(_draggingOver ??= new HashSet<long>()).Add(args.SourceId);

			SafeRaiseEvent(evt, args);
		}

		internal void RaiseDragLeave(global::Windows.UI.Xaml.DragEventArgs args)
		{
			if (_draggingOver?.Remove(args.SourceId) ?? false)
			{
				SafeRaiseEvent(DragLeaveEvent, args);
			}
		}

		internal void RaiseDrop(global::Windows.UI.Xaml.DragEventArgs args)
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
				// No local state (over/pressed/manipulation/gestures) to update for
				//	- PointerCaptureLost:
				//	- PointerWheelChanged:
			}
		}

		#region Partial API to raise pointer events and gesture recognition (OnNative***)
		private bool OnNativePointerEnter(PointerRoutedEventArgs args) => OnPointerEnter(args, isManagedBubblingEvent: false);
		private void OnManagedPointerEnter(PointerRoutedEventArgs args) => OnPointerEnter(args, isManagedBubblingEvent: true);

		private bool OnPointerEnter(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			// We override the isOver for the relevancy check as we will update it right after.
			var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver: true);
			var handledInManaged = SetOver(args, true, muteEvent: isManagedBubblingEvent || !isOverOrCaptured);

			return handledInManaged;
		}

		private bool OnNativePointerDown(PointerRoutedEventArgs args) => OnPointerDown(args, isManagedBubblingEvent: false);
		private void OnManagedPointerDown(PointerRoutedEventArgs args) => OnPointerDown(args, isManagedBubblingEvent: true);

		private bool OnPointerDown(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			_isGestureCompleted = false;

			// "forceRelease: true": as we are in pointer pressed, if the pointer is already captured,
			// it due to an invalid state. So here we make sure to not stay in an invalid state that would
			// prevent any interaction with the application.
			var isOverOrCaptured = ValidateAndUpdateCapture(args, isOver: true, forceRelease: true);
			var handledInManaged = SetPressed(args, true, muteEvent: isManagedBubblingEvent || !isOverOrCaptured);

			if (PointerRoutedEventArgs.PlatformSupportsNativeBubbling && !isManagedBubblingEvent && !isOverOrCaptured)
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

#if __WASM__ || __SKIA__
				// On iOS and Android, pointers are implicitly captured, so we will receive the "irrelevant" (i.e. !isOverOrCaptured)
				// pointer moves and we can use them for manipulation. But on WASM and SKIA we have to explicitly request to get those events
				// (expect on FF where they are also implicitly captured ... but we still capture them anyway).
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
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this), isOverOrCaptured);
				if (_gestures.Value.IsDragging)
				{
					global::Windows.UI.Xaml.Window.Current.DragDrop.ProcessMoved(args);
				}
			}

			return handledInManaged;
		}

		private bool OnNativePointerMove(PointerRoutedEventArgs args) => OnPointerMove(args, isManagedBubblingEvent: false);
		private void OnManagePointerMove(PointerRoutedEventArgs args) => OnPointerMove(args, isManagedBubblingEvent: true);

		private bool OnPointerMove(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args);

			if (!isManagedBubblingEvent && isOverOrCaptured)
			{
				// If this pointer was wrongly dispatched here (out of the bounds and not captured),
				// we don't raise the 'move' event

				args.Handled = false;
				handledInManaged |= RaisePointerEvent(PointerMovedEvent, args);
			}

			if (_gestures.IsValueCreated)
			{
				// We need to process only events that were not handled by a child control,
				// so we should not use them for gesture recognition.
				_gestures.Value.ProcessMoveEvents(args.GetIntermediatePoints(this), !isManagedBubblingEvent || isOverOrCaptured);
				if (_gestures.Value.IsDragging)
				{
					global::Windows.UI.Xaml.Window.Current.DragDrop.ProcessMoved(args);
				}
			}

			return handledInManaged;
		}

		private bool OnNativePointerUp(PointerRoutedEventArgs args) => OnPointerUp(args, isManagedBubblingEvent: false);
		private void OnManagedPointerUp(PointerRoutedEventArgs args) => OnPointerUp(args, isManagedBubblingEvent: true);

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
				var isDragging = _gestures.Value.IsDragging;
				_gestures.Value.ProcessUpEvent(args.GetCurrentPoint(this), !isManagedBubblingEvent || isOverOrCaptured);
				if (isDragging)
				{
					global::Windows.UI.Xaml.Window.Current.DragDrop.ProcessDropped(args);
				}
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

		private bool OnNativePointerExited(PointerRoutedEventArgs args) => OnPointerExited(args, isManagedBubblingEvent: false);
		private void OnManagedPointerExited(PointerRoutedEventArgs args) => OnPointerExited(args, isManagedBubblingEvent: true);

		private bool OnPointerExited(PointerRoutedEventArgs args, bool isManagedBubblingEvent)
		{
			var handledInManaged = false;
			var isOverOrCaptured = ValidateAndUpdateCapture(args);

			handledInManaged |= SetOver(args, false, muteEvent: isManagedBubblingEvent || !isOverOrCaptured);

			if (_gestures.IsValueCreated && _gestures.Value.IsDragging)
			{
				global::Windows.UI.Xaml.Window.Current.DragDrop.ProcessMoved(args);
			}

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
			return OnPointerCancel(args, isManagedBubblingEvent: false);
		}
		private void OnManagedPointerCancel(PointerRoutedEventArgs args) => OnPointerCancel(args, isManagedBubblingEvent: true);

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
				if (_gestures.Value.IsDragging)
				{
					global::Windows.UI.Xaml.Window.Current.DragDrop.ProcessAborted(args);
				}
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

		private bool OnNativePointerWheel(PointerRoutedEventArgs args)
		{
			return RaisePointerEvent(PointerWheelChangedEvent, args);
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

		#region Pointer pressed state (Updated by the partial API OnNative***, should not be updated externaly)
		private readonly HashSet<uint> _pressedPointers = new HashSet<uint>();

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

		private bool IsPressed(uint pointerId) => _pressedPointers.Contains(pointerId);

		private bool SetPressed(PointerRoutedEventArgs args, bool isPressed, bool muteEvent = false)
		{
			var wasPressed = IsPressed(args.Pointer);
			if (wasPressed == isPressed) // nothing changed
			{
				return false;
			}

			if (isPressed) // Pressed
			{
				_pressedPointers.Add(args.Pointer.PointerId);

				if (muteEvent)
				{
					return false;
				}

				args.Handled = false;
				return RaisePointerEvent(PointerPressedEvent, args);
			}
			else // Released
			{
				_pressedPointers.Remove(args.Pointer.PointerId);

				if (muteEvent)
				{
					return false;
				}

				args.Handled = false;
				return RaisePointerEvent(PointerReleasedEvent, args);
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
