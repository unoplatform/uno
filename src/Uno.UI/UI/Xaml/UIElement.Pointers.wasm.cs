using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uno.Foundation;
using Windows.Devices.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.System;
using Windows.UI.Input;
using Uno.UI;
using Uno.UI.Xaml;

namespace Windows.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		// Ref:
		// https://www.w3.org/TR/pointerevents/
		// https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent
		// https://developer.mozilla.org/en-US/docs/Web/API/WheelEvent

		#region Native event registration handling
		partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer)
		{
			// When a gesture recognizer is initialized, we subscribe to pointer events in order to feed it.
			// Note: We register to Move, so it will also register Enter, Exited, Pressed, Released and Cancel.
			//		 Gesture recognizer does not needs CaptureLost nor Wheel events.
			AddPointerHandler(PointerMovedEvent, 1, default, default);
		}

		partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
		{
			if (handlersCount != 1 || _registeredRoutedEvents.HasFlag(routedEvent.Flag))
			{
				return;
			}

			// In order to ensure valid pressed and over state, we ** must ** subscribe to all the related events
			// before subscribing to other pointer events.
			if (!_registeredRoutedEvents.HasFlag(RoutedEventFlag.PointerEntered))
			{
				WindowManagerInterop.RegisterPointerEventsOnView(HtmlId);

				_registeredRoutedEvents |=
					  RoutedEventFlag.PointerEntered
					| RoutedEventFlag.PointerExited
					| RoutedEventFlag.PointerPressed
					| RoutedEventFlag.PointerReleased
					| RoutedEventFlag.PointerCanceled;

				// Note: we use 'pointerenter' and 'pointerleave' which are not bubbling natively
				//		 as on UWP, even if the event are RoutedEvents, PointerEntered and PointerExited
				//		 are routed only in some particular cases (entering at once on multiple controls),
				//		 it's easier to handle this in managed code.
				RegisterEventHandler("pointerenter", (RawEventHandler)DispatchNativePointerEnter);
				RegisterEventHandler("pointerleave", (RawEventHandler)DispatchNativePointerLeave);
				RegisterEventHandler("pointerdown", (RawEventHandler)DispatchNativePointerDown);
				RegisterEventHandler("pointerup", (RawEventHandler)DispatchNativePointerUp);
				RegisterEventHandler("pointercancel", (RawEventHandler)DispatchNativePointerCancel); //https://www.w3.org/TR/pointerevents/#the-pointercancel-event
			}

			switch (routedEvent.Flag)
			{
				case RoutedEventFlag.PointerEntered:
				case RoutedEventFlag.PointerExited:
				case RoutedEventFlag.PointerPressed:
				case RoutedEventFlag.PointerReleased:
				case RoutedEventFlag.PointerCanceled:
					// All event above are automatically subscribed
					break;

				case RoutedEventFlag.PointerMoved:
					_registeredRoutedEvents |= RoutedEventFlag.PointerMoved;
					RegisterEventHandler(
						"pointermove",
						handler: (RawEventHandler)DispatchNativePointerMove,
						onCapturePhase: false,
						eventExtractor: HtmlEventExtractor.PointerEventExtractor
					);
					break;

				case RoutedEventFlag.PointerCaptureLost:
					// No native registration: Captures are handled in managed code only
					_registeredRoutedEvents |= routedEvent.Flag;
					break;

				case RoutedEventFlag.PointerWheelChanged:
					_registeredRoutedEvents |= RoutedEventFlag.PointerMoved;
					RegisterEventHandler(
						"wheel",
						handler: (RawEventHandler)DispatchNativePointerWheel,
						onCapturePhase: false,
						eventExtractor: HtmlEventExtractor.PointerEventExtractor
					);
					break;

				default:
					Application.Current.RaiseRecoverableUnhandledException(new NotImplementedException($"Pointer event {routedEvent.Name} is not supported on this platform"));
					break;
			}
		}
		#endregion

		#region Native event dispatch
		// Note for Enter and Leave:
		//	canBubble: true is actually not true.
		//	When we subscribe to pointer enter in a window, we don't receive pointer enter for each sub-views!
		//	But the web-browser will actually behave like WinUI for pointerenter and pointerleave, so here by setting it to true,
		//	we just ensure that the managed code won't try to bubble it by its own.
		//	However, if the event is Handled in managed, it will then bubble while it should not! https://github.com/unoplatform/uno/issues/3007
		private static bool DispatchNativePointerEnter(UIElement target, string eventPayload)
			=> TryParse(eventPayload, out var args) && target.OnNativePointerEnter(ToPointerArgs(target, args, isInContact: false));

		private static bool DispatchNativePointerLeave(UIElement target, string eventPayload)
			=> TryParse(eventPayload, out var args) && target.OnNativePointerExited(ToPointerArgs(target, args, isInContact: false));

		private static bool DispatchNativePointerDown(UIElement target, string eventPayload)
			=> TryParse(eventPayload, out var args) && target.OnNativePointerDown(ToPointerArgs(target, args, isInContact: true));

		private static bool DispatchNativePointerUp(UIElement target, string eventPayload)
			=> TryParse(eventPayload, out var args) && target.OnNativePointerUp(ToPointerArgs(target, args, isInContact: true));

		private static bool DispatchNativePointerMove(UIElement target, string eventPayload)
			=> TryParse(eventPayload, out var args) && target.OnNativePointerMove(ToPointerArgs(target, args, isInContact: true));

		private static bool DispatchNativePointerCancel(UIElement target, string eventPayload)
			=> TryParse(eventPayload, out var args) && target.OnNativePointerCancel(ToPointerArgs(target, args, isInContact: false), isSwallowedBySystem: true);

		private static bool DispatchNativePointerWheel(UIElement target, string eventPayload)
		{
			if (TryParse(eventPayload, out var args))
			{
				// We might have a scroll along 2 directions at once (touch pad).
				// As WinUI does support scrolling only along one direction at a time, we have to raise 2 managed events.

				var handled = false;
				if (args.wheelDeltaX != 0)
				{
					handled |= target.OnNativePointerWheel(ToPointerArgs(target, args, wheel: (true, args.wheelDeltaX), isInContact: null /* maybe */));
				}
				if (args.wheelDeltaY != 0)
				{
					// Note: Web browser vertical scrolling is the opposite compared to WinUI!
					handled |= target.OnNativePointerWheel(ToPointerArgs(target, args, wheel: (false, -args.wheelDeltaY), isInContact: null /* maybe */));
				}
				return handled;
			}
			else
			{
				return false;
			}
		}

		private static bool TryParse(string eventPayload, out NativePointerEventArgs args)
		{
			var parts = eventPayload?.Split(';');
			if (parts?.Length != 13)
			{
				args = default;
				return false;
			}

			args = new NativePointerEventArgs { 
				pointerId = double.Parse(parts[0], CultureInfo.InvariantCulture), // On Safari for iOS, the ID might be negative!
				x = double.Parse(parts[1], CultureInfo.InvariantCulture),
				y = double.Parse(parts[2], CultureInfo.InvariantCulture),
				ctrl = parts[3] == "1",
				shift = parts[4] == "1",
				buttons = int.Parse(parts[5], CultureInfo.InvariantCulture),
				buttonUpdate = int.Parse(parts[6], CultureInfo.InvariantCulture),
				typeStr = parts[7],
				srcHandle = int.Parse(parts[8], CultureInfo.InvariantCulture),
				timestamp = double.Parse(parts[9], CultureInfo.InvariantCulture),
				pressure = double.Parse(parts[10], CultureInfo.InvariantCulture),
				wheelDeltaX = double.Parse(parts[11], CultureInfo.InvariantCulture),
				wheelDeltaY = double.Parse(parts[12], CultureInfo.InvariantCulture),
			};
			return true;
		}

		private static PointerRoutedEventArgs ToPointerArgs(
			UIElement snd,
			NativePointerEventArgs args,
			bool? isInContact,
			(bool isHorizontalWheel, double delta) wheel = default)
		{
			var pointerId = (uint)args.pointerId;
			var src = GetElementFromHandle(args.srcHandle) ?? (UIElement)snd;
			var position = new Point(args.x, args.y);
			var pointerType = ConvertPointerTypeString(args.typeStr);
			var keyModifiers = VirtualKeyModifiers.None;
			if (args.ctrl) keyModifiers |= VirtualKeyModifiers.Control;
			if (args.shift) keyModifiers |= VirtualKeyModifiers.Shift;

			return new PointerRoutedEventArgs(
				args.timestamp,
				pointerId,
				pointerType,
				position,
				isInContact ?? ((UIElement)snd).IsPressed(pointerId),
				(WindowManagerInterop.HtmlPointerButtonsState)args.buttons,
				(WindowManagerInterop.HtmlPointerButtonUpdate)args.buttonUpdate,
				keyModifiers,
				args.pressure,
				wheel,
				src);
		}

		private static PointerDeviceType ConvertPointerTypeString(string typeStr)
		{
			PointerDeviceType type;
			switch (typeStr.ToUpper())
			{
				case "MOUSE":
				default:
					type = PointerDeviceType.Mouse;
					break;
				// Note: As of 2019-11-28, once pen pressed events pressed/move/released are reported as TOUCH on Firefox
				//		 https://bugzilla.mozilla.org/show_bug.cgi?id=1449660
				case "PEN":
					type = PointerDeviceType.Pen;
					break;
				case "TOUCH":
					type = PointerDeviceType.Touch;
					break;
			}

			return type;
		}
		#endregion

		#region Capture
		partial void OnManipulationModeChanged(ManipulationModes _, ManipulationModes newMode)
			=> SetStyle("touch-action", newMode == ManipulationModes.None ? "none" : "auto");

		partial void CapturePointerNative(Pointer pointer)
		{
			var command = "Uno.UI.WindowManager.current.setPointerCapture(" + HtmlId + ", " + pointer.PointerId + ");";
			WebAssemblyRuntime.InvokeJS(command);

			if (pointer.PointerDeviceType != PointerDeviceType.Mouse)
			{
				SetStyle("touch-action", "none");
			}
		}

		partial void ReleasePointerNative(Pointer pointer)
		{
			var command = "Uno.UI.WindowManager.current.releasePointerCapture(" + HtmlId + ", " + pointer.PointerId + ");";
			WebAssemblyRuntime.InvokeJS(command);

			if (pointer.PointerDeviceType != PointerDeviceType.Mouse && ManipulationMode != ManipulationModes.None)
			{
				SetStyle("touch-action", "auto");
			}
		}
		#endregion

		#region HitTestVisibility
		internal void UpdateHitTest()
		{
			this.CoerceValue(HitTestVisibilityProperty);
		}

		private protected enum HitTestVisibility
		{
			/// <summary>
			/// The element and its children can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This occurs when IsHitTestVisible="False", IsEnabled="False", or Visibility="Collapsed".
			/// </remarks>
			Collapsed,

			/// <summary>
			/// The element can't be targeted by hit-testing.
			/// </summary>
			/// <remarks>
			/// This usually occurs if an element doesn't have a Background/Fill.
			/// </remarks>
			Invisible,

			/// <summary>
			/// The element can be targeted by hit-testing.
			/// </summary>
			Visible,
		}

		/// <summary>
		/// Represents the final calculated hit-test visibility of the element.
		/// </summary>
		/// <remarks>
		/// This property should never be directly set, and its value should always be calculated through coercion (see <see cref="CoerceHitTestVisibility(DependencyObject, object, bool)"/>.
		/// </remarks>
		private static DependencyProperty HitTestVisibilityProperty { get ; } =
			DependencyProperty.Register(
				"HitTestVisibility",
				typeof(HitTestVisibility),
				typeof(UIElement),
				new FrameworkPropertyMetadata(
					HitTestVisibility.Visible,
					FrameworkPropertyMetadataOptions.Inherits,
					coerceValueCallback: (s, e) => CoerceHitTestVisibility(s, e),
					propertyChangedCallback: (s, e) => OnHitTestVisibilityChanged(s, e)
				)
			);

		/// <summary>
		/// This calculates the final hit-test visibility of an element.
		/// </summary>
		/// <returns></returns>
		private static object CoerceHitTestVisibility(DependencyObject dependencyObject, object baseValue)
		{
			var element = (UIElement)dependencyObject;

			// The HitTestVisibilityProperty is never set directly. This means that baseValue is always the result of the parent's CoerceHitTestVisibility.
			var baseHitTestVisibility = (HitTestVisibility)baseValue;

			// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
			if (baseHitTestVisibility == HitTestVisibility.Collapsed)
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
			if (!element.IsHitTestVisible || element.Visibility != Visibility.Visible || !element.IsEnabledOverride())
			{
				return HitTestVisibility.Collapsed;
			}

			// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
			if (!element.IsViewHit())
			{
				return HitTestVisibility.Invisible;
			}

			// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.
			return HitTestVisibility.Visible;
		}

		private static void OnHitTestVisibilityChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is UIElement element
				&& args.OldValue is HitTestVisibility oldValue
				&& args.NewValue is HitTestVisibility newValue)
			{
				element.OnHitTestVisibilityChanged(oldValue, newValue);
			}
		}

		private protected virtual void OnHitTestVisibilityChanged(HitTestVisibility oldValue, HitTestVisibility newValue)
		{
			if (newValue == HitTestVisibility.Visible)
			{
				// By default, elements have 'pointer-event' set to 'auto' (see Uno.UI.css .uno-uielement class).
				// This means that they can be the target of hit-testing and will raise pointer events when interacted with.
				// This is aligned with HitTestVisibilityProperty's default value of Visible.
				WindowManagerInterop.SetPointerEvents(HtmlId, true);
			}
			else
			{
				// If HitTestVisibilityProperty is calculated to Invisible or Collapsed,
				// we don't want to be the target of hit-testing and raise any pointer events.
				// This is done by setting 'pointer-events' to 'none'.
				WindowManagerInterop.SetPointerEvents(HtmlId, false);
			}

			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMProperties();
			}
		}
		#endregion

		// TODO: This should be marshaled instead of being parsed! https://github.com/unoplatform/uno/issues/2116
		private struct NativePointerEventArgs
		{
			public double pointerId; // Warning: This is a Number in JS, and it might be negative on safari for iOS
			public double x;
			public double y;
			public bool ctrl;
			public bool shift;
			public int buttons;
			public int buttonUpdate;
			public string typeStr;
			public int srcHandle;
			public double timestamp;
			public double pressure;
			public double wheelDeltaX;
			public double wheelDeltaY;
		}
	}
}
