//#define TRACE_ROUTED_EVENT_BUBBLING

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Uno.Extensions;
using System.Reflection;
using Uno;
using Uno.Foundation.Logging;
using Uno.UI.Core;
using Uno.UI.Extensions;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Uno.UI.Xaml.Core;
using Microsoft.UI.Xaml.Controls.Primitives;

#if __APPLE_UIKIT__
using UIKit;
#endif

namespace Microsoft.UI.Xaml
{
	/*
		This partial file handles the registration and bubbling of routed events of a UIElement
		
		The API exposed by this file to its native parts are:
			partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
			partial void AddGestureHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
			partial void AddKeyHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
			partial void AddFocusHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
			partial void RemovePointerHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
			partial void RemoveGestureHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
			partial void RemoveKeyHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
			partial void RemoveFocusHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
			internal bool RaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args);

		The native components are responsible to subscribe to the native events, interpret them,
		and then raise the recognized events using the "RaiseEvent" API.

		Here the state machine of the bubbling logic:

	[1]---------------------+
	| An event is fired     |
	+--------+--------------+
	         |                            [12]--------------------+
	[2]------v--------------+             | Processing finished   |
	| Event is dispatched   |             | for this event.       |
	| to corresponding      |             +--------+--------------+
	| element               |                      ^
	+-------yes-------------+                      |
	         |                             [11]---no--------------+
	         |<---[13]-raise on parent---yes A parent is          |
	         |                             | defined?             |
	[3]------v--------------+              |                      |
	| Any local handlers?   no--------+    +-------^--------------+
	+-------yes-------------+         |            |
	         |                        |    [10]----+--------------+
	[4]------v--------------+         |    | Event is bubbling    |
	| Invoke local handlers |         |    | to parent in         <--+
	+--------+--------------+         |    | managed code (Uno)   |  |
	         |                        |    +-------^--------------+  |
	[5]------v--------------+         |            |                 |
	| Is the event handled  |         v    [6]-----no-------------+  |
	| by local handlers?    no------------>| Event is coming from |  |
	+-------yes-------------+              | platform?            |  |
	         |                             +------yes-------------+  |
	         v                                     |                 |
	        [10]                           [7]-----v--------------+  |
	                                       | Is the event         |  |
	                                       | bubbling natively?   no-+
	                                       +------yes-------------+
	                                               |
	                                       [8]-----v--------------+
	                                       | Event is returned    |
	                                       | for native           |
	                                       | bubbling in platform |
	                                       +----------------------+

	Note: this class is handling all this flow except [1] and [2].
	

	Steps when enabling a new RoutedEvent:

	1. Add/uncomment the routed event flag in RoutedEventFlag class.
	2. Add the event in the proper group within RoutedEventFlagExtensions.
	3. Move the RoutedEvent property and the event itself from generated partial class to here and initialize accordingly.
	4. Add the exact event type handling to UIElement.InvokeHandler method.
	5. If the event does not have an OnXYZ method on UIElement, FrameworkElement, or Control, you are done. Otherwise:
	6. Update the code in ImplementedRoutedEventsGenerator to generate the flag correctly when one of the aforementioned classes has an override.
	7. Add the handler attachment code in SubscribeToOverridenRoutedEvents in UIElement, FrameworkElement, or Control (depending on which level the method is declared).
	 
	 */

	partial class UIElement
	{
		public static RoutedEvent PointerPressedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerPressed);

		public static RoutedEvent PointerReleasedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerReleased);

		public static RoutedEvent PointerEnteredEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerEntered);

		public static RoutedEvent PointerExitedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerExited);

		public static RoutedEvent PointerMovedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerMoved);

		public static RoutedEvent PointerCanceledEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerCanceled);

		public static RoutedEvent PointerCaptureLostEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerCaptureLost);

#if !__CROSSRUNTIME__
		[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__")]
#endif
		public static RoutedEvent PointerWheelChangedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerWheelChanged);

		public static RoutedEvent ManipulationStartingEvent { get; } = new RoutedEvent(RoutedEventFlag.ManipulationStarting);

		public static RoutedEvent ManipulationStartedEvent { get; } = new RoutedEvent(RoutedEventFlag.ManipulationStarted);

		public static RoutedEvent ManipulationDeltaEvent { get; } = new RoutedEvent(RoutedEventFlag.ManipulationDelta);

		public static RoutedEvent ManipulationInertiaStartingEvent { get; } = new RoutedEvent(RoutedEventFlag.ManipulationInertiaStarting);

		public static RoutedEvent ManipulationCompletedEvent { get; } = new RoutedEvent(RoutedEventFlag.ManipulationCompleted);

		public static RoutedEvent TappedEvent { get; } = new RoutedEvent(RoutedEventFlag.Tapped);

		public static RoutedEvent DoubleTappedEvent { get; } = new RoutedEvent(RoutedEventFlag.DoubleTapped);

		public static RoutedEvent RightTappedEvent { get; } = new RoutedEvent(RoutedEventFlag.RightTapped);

		public static RoutedEvent HoldingEvent { get; } = new RoutedEvent(RoutedEventFlag.Holding);

		/* ** */
		internal /* ** */ static RoutedEvent DragStartingEvent { get; } = new RoutedEvent(RoutedEventFlag.DragStarting);

		public static RoutedEvent DragEnterEvent { get; } = new RoutedEvent(RoutedEventFlag.DragEnter);

		public static RoutedEvent DragOverEvent { get; } = new RoutedEvent(RoutedEventFlag.DragOver);

		public static RoutedEvent DragLeaveEvent { get; } = new RoutedEvent(RoutedEventFlag.DragLeave);

		public static RoutedEvent DropEvent { get; } = new RoutedEvent(RoutedEventFlag.Drop);

		/* ** */
		internal /* ** */  static RoutedEvent DropCompletedEvent { get; } = new RoutedEvent(RoutedEventFlag.DropCompleted);

#if __WASM__ || __SKIA__
		public static RoutedEvent PreviewKeyDownEvent { get; } = new RoutedEvent(RoutedEventFlag.PreviewKeyDown);

		public static RoutedEvent PreviewKeyUpEvent { get; } = new RoutedEvent(RoutedEventFlag.PreviewKeyUp);
#endif
		public static RoutedEvent KeyDownEvent { get; } = new RoutedEvent(RoutedEventFlag.KeyDown);

		public static RoutedEvent KeyUpEvent { get; } = new RoutedEvent(RoutedEventFlag.KeyUp);

		internal static RoutedEvent GotFocusEvent { get; } = new RoutedEvent(RoutedEventFlag.GotFocus);

		internal static RoutedEvent LostFocusEvent { get; } = new RoutedEvent(RoutedEventFlag.LostFocus);

		public static RoutedEvent GettingFocusEvent { get; } = new RoutedEvent(RoutedEventFlag.GettingFocus);

		public static RoutedEvent LosingFocusEvent { get; } = new RoutedEvent(RoutedEventFlag.LosingFocus);

		public static RoutedEvent NoFocusCandidateFoundEvent { get; } = new RoutedEvent(RoutedEventFlag.NoFocusCandidateFound);

		/// <summary>
		/// Gets the identifier for the BringIntoViewRequested routed event.
		/// </summary>
		public static RoutedEvent BringIntoViewRequestedEvent { get; } = new RoutedEvent(RoutedEventFlag.BringIntoViewRequested);

		private struct RoutedEventHandlerInfo
		{
			internal RoutedEventHandlerInfo(object handler, bool handledEventsToo)
			{
				Handler = handler;
				HandledEventsToo = handledEventsToo;
			}

			internal object Handler { get; }

			internal bool HandledEventsToo { get; }
		}

		#region EventsBubblingInManagedCode DependencyProperty

		public static DependencyProperty EventsBubblingInManagedCodeProperty { get; } = DependencyProperty.Register(
			"EventsBubblingInManagedCode",
			typeof(RoutedEventFlag),
			typeof(UIElement),
			new FrameworkPropertyMetadata(
				RoutedEventFlag.None,
				FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.KeepCoercedWhenEquals)
			{
				CoerceValueCallback = CoerceRoutedEventFlag
			}
		);

		public RoutedEventFlag EventsBubblingInManagedCode
		{
			get => (RoutedEventFlag)GetValue(EventsBubblingInManagedCodeProperty);
			set => SetValue(EventsBubblingInManagedCodeProperty, value);
		}

		#endregion

		private static object CoerceRoutedEventFlag(DependencyObject dependencyObject, object baseValue, DependencyPropertyValuePrecedences precedence)
		{
			var @this = (UIElement)dependencyObject;

			// ReadLocalValue will read an outdated value for the precedence currently being set
			var localValue = precedence is DependencyPropertyValuePrecedences.Local ?
				baseValue :
				@this.ReadLocalValue(EventsBubblingInManagedCodeProperty);

			var inheritedValue = precedence is DependencyPropertyValuePrecedences.Inheritance ?
				baseValue :
				((IDependencyObjectStoreProvider)@this).Store.ReadInheritedValueOrDefaultValue(EventsBubblingInManagedCodeProperty);

			var combinedFlag = RoutedEventFlag.None;
			if (localValue is RoutedEventFlag local)
			{
				combinedFlag |= local;
			}
			if (inheritedValue is RoutedEventFlag inherited)
			{
				combinedFlag |= inherited;
			}

			return combinedFlag;
		}

		private readonly Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>> _eventHandlerStore
			= new Dictionary<RoutedEvent, List<RoutedEventHandlerInfo>>();

		public event RoutedEventHandler LostFocus
		{
			add => AddHandler(LostFocusEvent, value, false);
			remove => RemoveHandler(LostFocusEvent, value);
		}

		public event RoutedEventHandler GotFocus
		{
			add => AddHandler(GotFocusEvent, value, false);
			remove => RemoveHandler(GotFocusEvent, value);
		}

		public event TypedEventHandler<UIElement, LosingFocusEventArgs> LosingFocus
		{
			add => AddHandler(LosingFocusEvent, value, false);
			remove => RemoveHandler(LosingFocusEvent, value);
		}

		public event TypedEventHandler<UIElement, GettingFocusEventArgs> GettingFocus
		{
			add => AddHandler(GettingFocusEvent, value, false);
			remove => RemoveHandler(GettingFocusEvent, value);
		}

		public event TypedEventHandler<UIElement, NoFocusCandidateFoundEventArgs> NoFocusCandidateFound
		{
			add => AddHandler(NoFocusCandidateFoundEvent, value, false);
			remove => RemoveHandler(NoFocusCandidateFoundEvent, value);
		}

		/// <summary>
		/// Occurs when StartBringIntoView is called on this element or one of its descendants.
		/// </summary>
		public event TypedEventHandler<UIElement, BringIntoViewRequestedEventArgs> BringIntoViewRequested
		{
			add => AddHandler(BringIntoViewRequestedEvent, value, false);
			remove => RemoveHandler(BringIntoViewRequestedEvent, value);
		}

		public event PointerEventHandler PointerCanceled
		{
			add => AddHandler(PointerCanceledEvent, value, false);
			remove => RemoveHandler(PointerCanceledEvent, value);
		}

		public event PointerEventHandler PointerCaptureLost
		{
			add => AddHandler(PointerCaptureLostEvent, value, false);
			remove => RemoveHandler(PointerCaptureLostEvent, value);
		}

		public event PointerEventHandler PointerEntered
		{
			add => AddHandler(PointerEnteredEvent, value, false);
			remove => RemoveHandler(PointerEnteredEvent, value);
		}

		public event PointerEventHandler PointerExited
		{
			add => AddHandler(PointerExitedEvent, value, false);
			remove => RemoveHandler(PointerExitedEvent, value);
		}

		public event PointerEventHandler PointerMoved
		{
			add => AddHandler(PointerMovedEvent, value, false);
			remove => RemoveHandler(PointerMovedEvent, value);
		}

		public event PointerEventHandler PointerPressed
		{
			add => AddHandler(PointerPressedEvent, value, false);
			remove => RemoveHandler(PointerPressedEvent, value);
		}

		public event PointerEventHandler PointerReleased
		{
			add => AddHandler(PointerReleasedEvent, value, false);
			remove => RemoveHandler(PointerReleasedEvent, value);
		}

#if !__CROSSRUNTIME__
		[global::Uno.NotImplemented("__ANDROID__", "__APPLE_UIKIT__")]
#endif
		public event PointerEventHandler PointerWheelChanged
		{
			add => AddHandler(PointerWheelChangedEvent, value, false);
			remove => RemoveHandler(PointerWheelChangedEvent, value);
		}

		public event ManipulationStartingEventHandler ManipulationStarting
		{
			add => AddHandler(ManipulationStartingEvent, value, false);
			remove => RemoveHandler(ManipulationStartingEvent, value);
		}

		public event ManipulationStartedEventHandler ManipulationStarted
		{
			add => AddHandler(ManipulationStartedEvent, value, false);
			remove => RemoveHandler(ManipulationStartedEvent, value);
		}

		public event ManipulationDeltaEventHandler ManipulationDelta
		{
			add => AddHandler(ManipulationDeltaEvent, value, false);
			remove => RemoveHandler(ManipulationDeltaEvent, value);
		}

		public event ManipulationInertiaStartingEventHandler ManipulationInertiaStarting
		{
			add => AddHandler(ManipulationInertiaStartingEvent, value, false);
			remove => RemoveHandler(ManipulationInertiaStartingEvent, value);
		}

		public event ManipulationCompletedEventHandler ManipulationCompleted
		{
			add => AddHandler(ManipulationCompletedEvent, value, false);
			remove => RemoveHandler(ManipulationCompletedEvent, value);
		}

		public event TappedEventHandler Tapped
		{
			add => AddHandler(TappedEvent, value, false);
			remove => RemoveHandler(TappedEvent, value);
		}

		public event DoubleTappedEventHandler DoubleTapped
		{
			add => AddHandler(DoubleTappedEvent, value, false);
			remove => RemoveHandler(DoubleTappedEvent, value);
		}

		public event RightTappedEventHandler RightTapped
		{
			add => AddHandler(RightTappedEvent, value, false);
			remove => RemoveHandler(RightTappedEvent, value);
		}

		public event HoldingEventHandler Holding
		{
			add => AddHandler(HoldingEvent, value, false);
			remove => RemoveHandler(HoldingEvent, value);
		}

		public event TypedEventHandler<UIElement, DragStartingEventArgs> DragStarting
		{
			add => AddHandler(DragStartingEvent, value, false);
			remove => RemoveHandler(DragStartingEvent, value);
		}

		public event DragEventHandler DragEnter
		{
			add => AddHandler(DragEnterEvent, value, false);
			remove => RemoveHandler(DragEnterEvent, value);
		}

		public event DragEventHandler DragLeave
		{
			add => AddHandler(DragLeaveEvent, value, false);
			remove => RemoveHandler(DragLeaveEvent, value);
		}

		public event DragEventHandler DragOver
		{
			add => AddHandler(DragOverEvent, value, false);
			remove => RemoveHandler(DragOverEvent, value);
		}

		public event DragEventHandler Drop
		{
			add => AddHandler(DropEvent, value, false);
			remove => RemoveHandler(DropEvent, value);
		}

		public event TypedEventHandler<UIElement, DropCompletedEventArgs> DropCompleted
		{
			add => AddHandler(DropCompletedEvent, value, false);
			remove => RemoveHandler(DropCompletedEvent, value);
		}

#if __WASM__ || __SKIA__
		public event KeyEventHandler PreviewKeyDown
		{
			add => AddHandler(PreviewKeyDownEvent, value, false);
			remove => RemoveHandler(PreviewKeyDownEvent, value);
		}

		public event KeyEventHandler PreviewKeyUp
		{
			add => AddHandler(PreviewKeyUpEvent, value, false);
			remove => RemoveHandler(PreviewKeyUpEvent, value);
		}
#endif

		public event KeyEventHandler KeyDown
		{
			add => AddHandler(KeyDownEvent, value, false);
			remove => RemoveHandler(KeyDownEvent, value);
		}

		internal event KeyEventHandler PostKeyDown;

		public event KeyEventHandler KeyUp
		{
			add => AddHandler(KeyUpEvent, value, false);
			remove => RemoveHandler(KeyUpEvent, value);
		}

		/// <summary>
		/// Inserts an event handler as the first event handler.
		/// This is for internal use only and allow controls to lazily subscribe to event only when required while remaining the first invoked handler,
		/// which is ** really ** important when marking an event as handled.
		/// </summary>
		private protected void InsertHandler(RoutedEvent routedEvent, object handler, bool handledEventsToo = false)
		{
			var handlers = _eventHandlerStore.FindOrCreate(routedEvent, () => new List<RoutedEventHandlerInfo>());
			if (handlers.Count > 0)
			{
				handlers.Insert(0, new RoutedEventHandlerInfo(handler, handledEventsToo));
			}
			else
			{
				handlers.Add(new RoutedEventHandlerInfo(handler, handledEventsToo));
			}

			AddHandler(routedEvent, handlers.Count, handler, handledEventsToo);
		}

		public void AddHandler(RoutedEvent routedEvent, object handler, bool handledEventsToo)
		{
			var handlers = _eventHandlerStore.FindOrCreate(routedEvent, () => new List<RoutedEventHandlerInfo>());
			handlers.Add(new RoutedEventHandlerInfo(handler, handledEventsToo));

			AddHandler(routedEvent, handlers.Count, handler, handledEventsToo);
		}

		private void AddHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (routedEvent.IsPointerEvent)
			{
				AddPointerHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.IsKeyEvent)
			{
				AddKeyHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.IsFocusEvent)
			{
				AddFocusHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.IsManipulationEvent)
			{
				AddManipulationHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.IsGestureEvent)
			{
				AddGestureHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.IsDragAndDropEvent)
			{
				AddDragAndDropHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
		}

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddKeyHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddFocusHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddManipulationHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddGestureHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddDragAndDropHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);

		public void RemoveHandler(RoutedEvent routedEvent, object handler)
		{
			if (_eventHandlerStore.TryGetValue(routedEvent, out var handlers))
			{
				var matchingHandler = handlers.FirstOrDefault(handlerInfo => (handlerInfo.Handler as Delegate).Equals(handler as Delegate));

				if (!matchingHandler.Equals(default(RoutedEventHandlerInfo)))
				{
					handlers.Remove(matchingHandler);
				}

				RemoveHandler(routedEvent, handlers.Count, handler);
			}
			else
			{
				RemoveHandler(routedEvent, remainingHandlersCount: -1, handler);
			}
		}

		private void RemoveHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
		{
			if (routedEvent.IsPointerEvent)
			{
				RemovePointerHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.IsKeyEvent)
			{
				RemoveKeyHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.IsFocusEvent)
			{
				RemoveFocusHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.IsManipulationEvent)
			{
				RemoveManipulationHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.IsGestureEvent)
			{
				RemoveGestureHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.IsDragAndDropEvent)
			{
				RemoveDragAndDropHandler(routedEvent, remainingHandlersCount, handler);
			}
		}

		partial void RemovePointerHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveKeyHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveFocusHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveManipulationHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveGestureHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveDragAndDropHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);

		private int CountHandler(RoutedEvent routedEvent)
			=> _eventHandlerStore.TryGetValue(routedEvent, out var handlers)
				? handlers.Count
				: 0;

		internal bool SafeRaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args, BubblingContext ctx = default)
		{
			try
			{
				return RaiseEvent(routedEvent, args, ctx);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to raise '{routedEvent.Name}': {e}");
				}

				return false;
			}
		}

		internal bool SafeRaiseTunnelingEvent(RoutedEvent routedEvent, RoutedEventArgs args)
		{
			try
			{
				RaiseTunnelingEvent(routedEvent, args);
				return true;
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Failed to raise '{routedEvent.Name}': {e}");
				}
				return false;
			}
		}

		/// <summary>
		/// Raise a routed event
		/// </summary>
		/// <remarks>
		/// Return true if event is handled in managed code (shouldn't bubble natively)
		/// </remarks>
		internal bool RaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args, BubblingContext ctx = default)
		{
#if TRACE_ROUTED_EVENT_BUBBLING
			global::System.Diagnostics.Debug.Write($"{this.GetDebugIdentifier()} - [{routedEvent.Name.TrimEnd("Event")}-{args?.GetHashCode():X8}] (ctx: {ctx}){(this is Microsoft.UI.Xaml.Controls.ContentControl ctrl ? ctrl.DataContext : "")}\r\n");
#endif
			global::System.Diagnostics.Debug.Assert(routedEvent.Flag is not RoutedEventFlag.None, $"Flag not defined for routed event {routedEvent.Name}.");

#if !__WASM__
			global::System.Diagnostics.Debug.Assert(!routedEvent.IsTunnelingEvent, $"Tunneling event {routedEvent.Name} should be raised through {nameof(RaiseTunnelingEvent)}");
#endif

			// TODO: This is just temporary workaround before proper
			// keyboard event infrastructure is implemented everywhere
			// (issue #6074)
			if (routedEvent.IsKeyEvent)
			{
				TrackKeyState(routedEvent, args);
			}

			// [3] Any local handlers?
			var isHandled = IsHandled(args);
			if (!ctx.ModeHasFlag(BubblingMode.IgnoreElement)
				&& !ctx.IsInternal
				&& !ctx.IsCleanup
				&& _eventHandlerStore.TryGetValue(routedEvent, out var handlers)
				&& handlers is { Count: > 0 })
			{
				// [4] Invoke local handlers
				InvokeHandlers(handlers, args, ref isHandled);

				// [5] Event handled by local handlers?
				if (isHandled)
				{
					// Make sure the event is marked as not bubbling natively anymore
					// --> [10]
					if (args != null)
					{
						args.CanBubbleNatively = false;
					}
				}
			}

			if (routedEvent == KeyDownEvent)
			{
				PostKeyDown?.Invoke(this, (KeyRoutedEventArgs)args);
			}

			if (routedEvent.IsTunnelingEvent || ctx.ModeHasFlag(BubblingMode.IgnoreParents) || ctx.Root == this)
			{
				return isHandled;
			}

			// [6] & [7] Will the event bubbling natively or in managed code?
			var isBubblingInManagedCode = IsBubblingInManagedCode(routedEvent, args);
			if (!isBubblingInManagedCode)
			{
				return false; // [8] Return for native bubbling
			}

			// [10] Bubbling in managed code

			// Make sure the event is marked as not bubbling natively anymore
			if (args != null)
			{
				args.CanBubbleNatively = false;
			}

			UIElement parent = null;
			if (this is not PopupPanel || this.GetParent() is PopupRoot)
			{
				// Sometimes, a PopupPanel will be a parent of an element's template (e.g. ComboBox)
				// and the parent will not be PopupRoot. In that case, we shouldn't propagate to the element
				parent = this.GetParent() as UIElement;

#if __APPLE_UIKIT__ || __ANDROID__
				// This is for safety (legacy support) and should be removed.
				// A common issue is the managed parent being cleared before unload event raised.
				parent ??= this.FindFirstParent<UIElement>();
#endif
			}
			else
			{
				// Make sure that the visual tree root element still gets notified that the event has reached the top of the tree
				// This is important to ensure to clear any relevant global state, e.g. for pointers to clear capture on pointer up (cf. InputManager).
				parent = this.XamlRoot?.VisualTree.RootElement;
			}

			// [11] A parent is defined?
			if (parent is null)
			{
				return isHandled; // [12] processing finished
			}

			// [13] Raise on parent
			return RaiseOnParent(routedEvent, args, parent, ctx);
		}

		/// <summary>
		/// Raise a tunneling routed event starting from the root and tunneling down to this element.
		/// </summary>
		internal void RaiseTunnelingEvent(RoutedEvent routedEvent, RoutedEventArgs args)
		{
			global::System.Diagnostics.Debug.Assert(routedEvent.IsTunnelingEvent, $"Event {routedEvent.Name} is not marked as a tunneling event.");

			// TODO: This is just temporary workaround before proper
			// keyboard event infrastructure is implemented everywhere
			// (issue #6074)
			// The key states will be tracked again in an accompanying bubbling event (e.g. KeyDown for PreviewKeyDown),
			// but this is fine, since it's idempotent.
			if (routedEvent.IsKeyEvent)
			{
				TrackKeyState(routedEvent, args);
			}

			// On WinUI, if one of the event handlers reparents this element, the tunneling still goes through the
			// original path.
			foreach (var p in this.GetAllParents().Reverse())
			{
				if (p is not UIElement parent)
				{
					break;
				}

				if (parent._eventHandlerStore.TryGetValue(routedEvent, out var handlers))
				{
					foreach (var handler in handlers.ToArray())
					{
						if (!IsHandled(args) || handler.HandledEventsToo)
						{
							parent.InvokeHandler(handler.Handler, args);
						}
					}
				}
			}
		}

		private static void TrackKeyState(RoutedEvent routedEvent, RoutedEventArgs args)
		{
			if (args is KeyRoutedEventArgs keyArgs)
			{
				if (routedEvent == KeyDownEvent
#if __WASM__ || __SKIA__
					|| routedEvent == PreviewKeyDownEvent
#endif
					)
				{
					KeyboardStateTracker.OnKeyDown(keyArgs.OriginalKey);
				}
				else if (routedEvent == KeyUpEvent
#if __WASM__ || __SKIA__
					|| routedEvent == PreviewKeyUpEvent
#endif
					)
				{
					KeyboardStateTracker.OnKeyUp(keyArgs.OriginalKey);
				}
			}
		}

		// This method is a workaround for https://github.com/mono/mono/issues/12981
		// It can be inlined in RaiseEvent when fixed.
		private static bool RaiseOnParent(RoutedEvent routedEvent, RoutedEventArgs args, UIElement parent, BubblingContext ctx)
		{
			var mode = parent.PrepareManagedEventBubbling(routedEvent, args, out args);

			// If we have reached the requested root element on which this event should bubble,
			// we make sure to not allow bubbling on parents.
			if (parent == ctx.Root)
			{
				mode |= BubblingMode.IgnoreParents;
			}

			var handledByAnyParent = parent.RaiseEvent(routedEvent, args, ctx.WithMode(mode));

			return handledByAnyParent;
		}

		private BubblingMode PrepareManagedEventBubbling(RoutedEvent routedEvent, RoutedEventArgs args, out RoutedEventArgs alteredArgs)
		{
			var bubblingMode = BubblingMode.Bubble;
			alteredArgs = args;
			if (routedEvent.IsPointerEvent)
			{
				PrepareManagedPointerEventBubbling(routedEvent, ref alteredArgs, ref bubblingMode);
			}
			else if (routedEvent.IsKeyEvent)
			{
				PrepareManagedKeyEventBubbling(routedEvent, ref alteredArgs, ref bubblingMode);
			}
			else if (routedEvent.IsFocusEvent)
			{
				PrepareManagedFocusEventBubbling(routedEvent, ref alteredArgs, ref bubblingMode);
			}
			else if (routedEvent.IsManipulationEvent)
			{
				PrepareManagedManipulationEventBubbling(routedEvent, ref alteredArgs, ref bubblingMode);
			}
			else if (routedEvent.IsGestureEvent)
			{
				PrepareManagedGestureEventBubbling(routedEvent, ref alteredArgs, ref bubblingMode);
			}
			else if (routedEvent.IsDragAndDropEvent)
			{
				PrepareManagedDragAndDropEventBubbling(routedEvent, ref alteredArgs, ref bubblingMode);
			}

			return bubblingMode;
		}

#nullable enable
		partial void PrepareManagedPointerEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode);
		partial void PrepareManagedKeyEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode);
		partial void PrepareManagedFocusEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode);
		partial void PrepareManagedManipulationEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode);
		partial void PrepareManagedGestureEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode);
		partial void PrepareManagedDragAndDropEventBubbling(RoutedEvent routedEvent, ref RoutedEventArgs args, ref BubblingMode bubblingMode);

		internal struct BubblingContext
		{
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
			public static readonly BubblingContext Bubble;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

			public static readonly BubblingContext NoBubbling = new() { Mode = BubblingMode.NoBubbling };

			/// <summary>
			/// When bubbling in managed code, the <see cref="UIElement.RaiseEvent"/> will take care to raise the event on each parent,
			/// considering the Handled flag.
			/// This value is used to flag events that are sent to element to maintain their internal state,
			/// but which are not meant to initiate a new event bubbling (a.k.a. invoke the "RaiseEvent" again)
			/// </summary>
			public static readonly BubblingContext OnManagedBubbling = new() { Mode = BubblingMode.NoBubbling, IsInternal = true };

			public static BubblingContext BubbleUpTo(UIElement? root)
				=> new() { Root = root };

			/// <summary>
			/// The mode to use for bubbling
			/// </summary>
			public BubblingMode Mode { get; set; }

			/// <summary>
			/// An optional root element on which the bubbling should stop.
			/// </summary>
			/// <remarks>It's expected that the event is raised on this Root element.</remarks>
			public UIElement? Root { get; set; }

			/// <summary>
			/// Indicates that the associated event should not be publicly raised.
			/// </summary>
			/// <remarks>
			/// The "internal" here refers only to the private state of the code which has initiated this event, not subclasses.
			/// This means that an event flagged as "internal" can bubble to update the private state of parents,
			/// but the UIElement.RoutedEvent won't be raised in any way (public and internal handlers) and it won't be sent to Control.On`RoutedEvent`() neither.
			/// </remarks>
			public bool IsInternal { get; set; }

			/// <summary>
			/// Used only with managed pointers to indicate that the bubbling is for cleanup.
			/// In that case even interpreted events (i.e. GestureRecognizer) should also be muted
			/// (while the IsInternal will only mute the raw event, e.g. enter/move/pressed).
			/// </summary>
			public bool IsCleanup { get; set; }

			public BubblingContext WithMode(BubblingMode mode)
				=> this with { Mode = mode };

			public bool ModeHasFlag(BubblingMode flag)
				=> (Mode & flag) != 0;

			public override string ToString()
				=> $"{Mode}{(IsInternal ? " *internal*" : "")}{(IsCleanup ? " *cleanup*" : "")}{(Root is { } r ? $" up to {Root.GetDebugName()}" : "")}";
		}
#nullable restore

		/// <summary>
		/// Defines the mode used to bubble an event.
		/// </summary>
		/// <remarks>
		/// Preventing default bubble behavior of an event is meant to be used only when the event has already been raised/bubbled,
		/// but we need to sent it also to some specific elements (e.g. implicit captures).
		/// </remarks>
		[Flags]
		internal enum BubblingMode
		{
			/// <summary>
			/// The event should bubble normally in this element and its parent
			/// </summary>
			Bubble = 0,

			/// <summary>
			/// The event should not be raised on current element
			/// </summary>
			IgnoreElement = 1,

			/// <summary>
			/// The event should not bubble to parent elements
			/// </summary>
			IgnoreParents = 2,

			/// <summary>
			/// The bubbling should stop here (the event won't even be raised on the current element)
			/// </summary>
			NoBubbling = IgnoreElement | IgnoreParents,
		}

		private static bool IsHandled(RoutedEventArgs args)
			=> args is IHandleableRoutedEventArgs { Handled: true };

		private bool IsBubblingInManagedCode(RoutedEvent routedEvent, RoutedEventArgs args)
		{
			if (args == null || !args.CanBubbleNatively) // [6] From platform?
			{
				// Not from platform

				return true; // -> [10] bubble in managed to parents
			}

			// [7] Event set to bubble in managed code?
			var eventsBubblingInManagedCode = EventsBubblingInManagedCode;
			var flag = routedEvent.Flag;

			return eventsBubblingInManagedCode.HasFlag(flag);
		}

#pragma warning disable IDE0055 // Fix formatting: Current formatting is readable and rule expectation is unclear
		private void InvokeHandlers(IList<RoutedEventHandlerInfo> handlers, RoutedEventArgs args, ref bool isHandled)
		{
			switch (handlers.Count)
			{
				case 0:
					return;

				case 1:
				{
					var handler = handlers[0];
					if (!isHandled || handler.HandledEventsToo)
					{
						InvokeHandler(handler.Handler, args);
						isHandled = IsHandled(args);
					}

					break;
				}

				default:
				{
					foreach (var handler in handlers.ToArray())
					{
						if (!isHandled || handler.HandledEventsToo)
						{
							InvokeHandler(handler.Handler, args);
							isHandled = IsHandled(args);
						}
					}

					break;
				}
			}
		}
#pragma warning restore IDE0055 // Fix formatting

		private void InvokeHandler(object handler, RoutedEventArgs args)
		{
			// TODO: WPF calls a virtual RoutedEventArgs.InvokeEventHandler(Delegate handler, object target) method,
			// instead of going through all possible cases like we do here.
			switch (handler)
			{
				case RoutedEventHandler routedEventHandler:
					routedEventHandler(this, args);
					break;
				case PointerEventHandler pointerEventHandler:
					pointerEventHandler(this, (PointerRoutedEventArgs)args);
					break;
				case TappedEventHandler tappedEventHandler:
					tappedEventHandler(this, (TappedRoutedEventArgs)args);
					break;
				case DoubleTappedEventHandler doubleTappedEventHandler:
					doubleTappedEventHandler(this, (DoubleTappedRoutedEventArgs)args);
					break;
				case RightTappedEventHandler rightTappedEventHandler:
					rightTappedEventHandler(this, (RightTappedRoutedEventArgs)args);
					break;
				case HoldingEventHandler holdingEventHandler:
					holdingEventHandler(this, (HoldingRoutedEventArgs)args);
					break;
				case DragEventHandler dragEventHandler:
					dragEventHandler(this, (global::Microsoft.UI.Xaml.DragEventArgs)args);
					break;
				case TypedEventHandler<UIElement, DragStartingEventArgs> dragStartingHandler:
					dragStartingHandler(this, (DragStartingEventArgs)args);
					break;
				case TypedEventHandler<UIElement, DropCompletedEventArgs> dropCompletedHandler:
					dropCompletedHandler(this, (DropCompletedEventArgs)args);
					break;
				case KeyEventHandler keyEventHandler:
					keyEventHandler(this, (KeyRoutedEventArgs)args);
					break;
				case ManipulationStartingEventHandler manipStarting:
					manipStarting(this, (ManipulationStartingRoutedEventArgs)args);
					break;
				case ManipulationStartedEventHandler manipStarted:
					manipStarted(this, (ManipulationStartedRoutedEventArgs)args);
					break;
				case ManipulationDeltaEventHandler manipDelta:
					manipDelta(this, (ManipulationDeltaRoutedEventArgs)args);
					break;
				case ManipulationInertiaStartingEventHandler manipInertia:
					manipInertia(this, (ManipulationInertiaStartingRoutedEventArgs)args);
					break;
				case ManipulationCompletedEventHandler manipCompleted:
					manipCompleted(this, (ManipulationCompletedRoutedEventArgs)args);
					break;
				case TypedEventHandler<UIElement, GettingFocusEventArgs> gettingFocusHandler:
					gettingFocusHandler(this, (GettingFocusEventArgs)args);
					break;
				case TypedEventHandler<UIElement, LosingFocusEventArgs> losingFocusHandler:
					losingFocusHandler(this, (LosingFocusEventArgs)args);
					break;
				case TypedEventHandler<UIElement, BringIntoViewRequestedEventArgs> bringIntoViewRequestedHandler:
					bringIntoViewRequestedHandler(this, (BringIntoViewRequestedEventArgs)args);
					break;
				default:
					this.Log().Error($"The handler type {handler.GetType()} has not been registered for RoutedEvent");
					break;
			}
		}

		// Those methods are part of the internal UWP API
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool ShouldRaiseEvent(Delegate eventHandler) => eventHandler != null;
	}
}
