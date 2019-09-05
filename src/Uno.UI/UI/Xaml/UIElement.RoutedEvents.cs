using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml.Input;
using Uno;
using Uno.Extensions;
using Uno.UI;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Input;

#if __IOS__
using UIKit;
#endif

namespace Windows.UI.Xaml
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
	         |
	[2]------v--------------+
	| Event is dispatched   |
	| to corresponding      |                    [12]
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
	[9]------v--------------+                      |                 |
	| Any parent interested |              [7]-----v--------------+  |
	| by this event?        yes-+          | Is the event         |  |
	+-------no--------------+   |          | bubbling natively?   no-+
	         |                  |          +------yes-------------+
	[12]-----v--------------+   |                  |
	| Processing finished   |   v          [8]-----v--------------+
	| for this event.       |  [10]        | Event is returned    |
	+-----------------------+              | for native           |
	                                       | bubbling in platform |
	                                       +----------------------+

	Note: this class is handling all this flow except [1] and [2]. */

	partial class UIElement
	{
		public static RoutedEvent PointerPressedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerPressed);

		public static RoutedEvent PointerReleasedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerReleased);

		public static RoutedEvent PointerEnteredEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerEntered);

		public static RoutedEvent PointerExitedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerExited);

		public static RoutedEvent PointerMovedEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerMoved);

		public static RoutedEvent PointerCanceledEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerCanceled);

		public static RoutedEvent PointerCaptureLostEvent { get; } = new RoutedEvent(RoutedEventFlag.PointerCaptureLost);

		public static RoutedEvent TappedEvent { get; } = new RoutedEvent(RoutedEventFlag.Tapped);

		public static RoutedEvent DoubleTappedEvent { get; } = new RoutedEvent(RoutedEventFlag.DoubleTapped);

		public static RoutedEvent KeyDownEvent { get; } = new RoutedEvent(RoutedEventFlag.KeyDown);

		public static RoutedEvent KeyUpEvent { get; } = new RoutedEvent(RoutedEventFlag.KeyUp);

		internal static RoutedEvent GotFocusEvent { get; } = new RoutedEvent(RoutedEventFlag.GotFocus);

		internal static RoutedEvent LostFocusEvent { get; } = new RoutedEvent(RoutedEventFlag.LostFocus);

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

		public static readonly DependencyProperty EventsBubblingInManagedCodeProperty = DependencyProperty.Register(
			"EventsBubblingInManagedCode",
			typeof(RoutedEventFlag),
			typeof(UIElement),
			new FrameworkPropertyMetadata(
				RoutedEventFlag.None,
				FrameworkPropertyMetadataOptions.Inherits)
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

		#region SubscribedToHandledEventsToo DependencyProperty

		private static readonly DependencyProperty SubscribedToHandledEventsTooProperty =
			DependencyProperty.Register(
				"SubscribedToHandledEventsToo",
				typeof(RoutedEventFlag),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					RoutedEventFlag.None,
					FrameworkPropertyMetadataOptions.Inherits)
				{
					CoerceValueCallback = CoerceRoutedEventFlag
				}
			);

		private RoutedEventFlag SubscribedToHandledEventsToo
		{
			get => (RoutedEventFlag) GetValue(SubscribedToHandledEventsTooProperty);
			set => SetValue(SubscribedToHandledEventsTooProperty, value);
		}

		#endregion

		private static object CoerceRoutedEventFlag(DependencyObject dependencyObject, object baseValue)
		{
			// This is a Coerce method for both EventsBubblingInManagedCodeProperty and SubscribedToHandledEventsTooProperty

			var @this = (UIElement)dependencyObject;

			var localValue = @this.GetPrecedenceSpecificValue(
				SubscribedToHandledEventsTooProperty,
				DependencyPropertyValuePrecedences.Local); // should be the same than localValue on first assignment

			if (!(localValue is RoutedEventFlag local))
			{
				return baseValue; // local not set, no coerced value to set
			}

			var inheritedValue = @this.GetPrecedenceSpecificValue(
				SubscribedToHandledEventsTooProperty,
				DependencyPropertyValuePrecedences.Inheritance);

			if (inheritedValue is RoutedEventFlag inherited)
			{
				return local | inherited; // coerced value is a merge between local and inherited
			}

			return baseValue; // no inherited value, nothing to do
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

#pragma warning disable 67 // Unused member
		public event PointerEventHandler PointerCanceled
		{
			add => AddHandler(PointerCanceledEvent, value, false);
			remove => RemoveHandler(PointerCanceledEvent, value);
		}
#pragma warning restore 67 // Unused member

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

#if __MACOS__
		public new event KeyEventHandler KeyDown
#else
		public event KeyEventHandler KeyDown
#endif
		{
			add => AddHandler(KeyDownEvent, value, false);
			remove => RemoveHandler(KeyDownEvent, value);
		}

#if __MACOS__
		public new event KeyEventHandler KeyUp
#else
		public event KeyEventHandler KeyUp
#endif
		{
			add => AddHandler(KeyUpEvent, value, false);
			remove => RemoveHandler(KeyUpEvent, value);
		}

		public void AddHandler(RoutedEvent routedEvent, object handler, bool handledEventsToo)
		{
			var handlers = _eventHandlerStore.FindOrCreate(routedEvent, () => new List<RoutedEventHandlerInfo>());
			handlers.Add(new RoutedEventHandlerInfo(handler, handledEventsToo));

			AddHandler(routedEvent, handlers.Count, handler, handledEventsToo);

			if (handledEventsToo)
			{
				UpdateSubscribedToHandledEventsToo();
			}
		}

		private void AddHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (routedEvent.Flag.IsPointerEvent())
			{
				AddPointerHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.Flag.IsGestureEvent())
			{
				AddGestureHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.Flag.IsKeyEvent())
			{
				AddKeyHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
			else if (routedEvent.Flag.IsFocusEvent())
			{
				AddFocusHandler(routedEvent, handlersCount, handler, handledEventsToo);
			}
		}

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddGestureHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddKeyHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);
		partial void AddFocusHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo);

		public void RemoveHandler(RoutedEvent routedEvent, object handler)
		{
			if (_eventHandlerStore.TryGetValue(routedEvent, out var handlers))
			{
				var matchingHandler = handlers.FirstOrDefault(handlerInfo => (handlerInfo.Handler as Delegate).Equals(handler as Delegate));

				if (!matchingHandler.Equals(default(RoutedEventHandlerInfo)))
				{
					handlers.Remove(matchingHandler);

					if (matchingHandler.HandledEventsToo)
					{
						UpdateSubscribedToHandledEventsToo();
					}
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
			if (routedEvent.Flag.IsPointerEvent())
			{
				RemovePointerHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.Flag.IsGestureEvent())
			{
				RemoveGestureHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.Flag.IsKeyEvent())
			{
				RemoveKeyHandler(routedEvent, remainingHandlersCount, handler);
			}
			else if (routedEvent.Flag.IsFocusEvent())
			{
				RemoveFocusHandler(routedEvent, remainingHandlersCount, handler);
			}
		}

		partial void RemovePointerHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveGestureHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveKeyHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);
		partial void RemoveFocusHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler);

		private void UpdateSubscribedToHandledEventsToo()
		{
			var subscribedToHandledEventsToo = RoutedEventFlag.None;

			foreach (var eventHandlers in _eventHandlerStore)
			{
				foreach (var handler in eventHandlers.Value)
				{
					if (handler.HandledEventsToo)
					{
						subscribedToHandledEventsToo |= eventHandlers.Key.Flag;
						break;
					}
				}
			}

			SubscribedToHandledEventsToo = subscribedToHandledEventsToo;
		}

		/// <summary>
		/// Raise a routed event
		/// </summary>
		/// <remarks>
		/// Return true if event is handled in managed code (shouldn't bubble natively)
		/// </remarks>
		internal bool RaiseEvent(RoutedEvent routedEvent, RoutedEventArgs args)
		{
			if (routedEvent.Flag == RoutedEventFlag.None)
			{
				throw new InvalidOperationException($"Flag not defined for routed event {routedEvent.Name}.");
			}

			// [3] Any local handlers?
			var anyLocalHandlers = _eventHandlerStore.TryGetValue(routedEvent, out var handlers) && handlers.Any();
			if (anyLocalHandlers)
			{
				// [4] Invoke local handlers
				foreach (var handler in handlers.ToArray())
				{
					if (!IsHandled(args) || handler.HandledEventsToo)
					{
						InvokeHandler(handler.Handler, args);
					}
				}

				// [5] Event handled by local handlers?
				if (IsHandled(args))
				{
					// [9] Any parent interested ?
					var anyParentInterested = AnyParentInterested(routedEvent);
					if (!anyParentInterested)
					{
						// [12] Event processing finished
						return true; // reported has handled in managed
					}

					// Make sure the event is marked as not bubbling natively anymore
					// --> [10]
					if (args != null)
					{
						args.CanBubbleNatively = false;
					}
				}
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

#if __IOS__ || __ANDROID__
			var parent = this.FindFirstParent<UIElement>();
#else
			var parent = this.GetParent() as UIElement;
#endif
			// [11] A parent is defined?
			if (parent == null)
			{
				return true; // [12] processing finished
			}

			// [13] Raise on parent
			return RaiseOnParent(routedEvent, args, parent);
		}

		// This method is a workaround for https://github.com/mono/mono/issues/12981
		// It can be inlined in RaiseEvent when fixed.
		private static bool RaiseOnParent(RoutedEvent routedEvent, RoutedEventArgs args, UIElement parent)
			=> parent.RaiseEvent(routedEvent, args);

		private static bool IsHandled(RoutedEventArgs args)
		{
			return args is ICancellableRoutedEventArgs cancellable && cancellable.Handled;
		}

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

		private bool AnyParentInterested(RoutedEvent routedEvent)
		{
			// [9] Any parent interested?
			var subscribedToHandledEventsToo = SubscribedToHandledEventsToo;
			var flag = routedEvent.Flag;
			return subscribedToHandledEventsToo.HasFlag(flag);
		}

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
				case KeyEventHandler keyEventHandler:
					keyEventHandler(this, (KeyRoutedEventArgs)args);
					break;
			}
		}
	}
}
