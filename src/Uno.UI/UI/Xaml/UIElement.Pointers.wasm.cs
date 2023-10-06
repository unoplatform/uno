#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using Uno.Foundation;
using Windows.Foundation;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Uno;
using Uno.Foundation.Interop;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Core;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

using PointerIdentifierPool = Windows.Devices.Input.PointerIdentifierPool; // internal type (should be in Uno namespace)
using PointerIdentifier = Windows.Devices.Input.PointerIdentifier; // internal type (should be in Uno namespace)
using PointerIdentifierDeviceType = Windows.Devices.Input.PointerDeviceType; // PointerIdentifier always uses Windows.Devices as it does noe have access to Microsoft.UI.Input (Uno.UI assembly)
#if HAS_UNO_WINUI
using Microsoft.UI.Input;
using PointerDeviceType = Microsoft.UI.Input.PointerDeviceType;
#else
using Windows.UI.Input;
using PointerDeviceType = Windows.Devices.Input.PointerDeviceType;
#endif

namespace Microsoft.UI.Xaml;

public partial class UIElement : DependencyObject
{
	private static TSInteropMarshaller.HandleRef<NativePointerEventArgs> _pointerEventArgs;
	private static TSInteropMarshaller.HandleRef<NativePointerEventResult> _pointerEventResult;

	// Ref:
	// https://www.w3.org/TR/pointerevents/
	// https://developer.mozilla.org/en-US/docs/Web/API/PointerEvent
	// https://developer.mozilla.org/en-US/docs/Web/API/WheelEvent

	#region Native event registration handling
	private NativePointerEvent _subscribedNativeEvents;

	partial void OnGestureRecognizerInitialized(GestureRecognizer recognizer)
	{
		// When a gesture recognizer is initialized, we subscribe to pointer events in order to feed it.
		// Note: We register to Move, so it will also register Enter, Exited, Pressed, Released and Cancel.
		//		 Gesture recognizer does not needs CaptureLost nor Wheel events.
		AddPointerHandler(PointerMovedEvent, 1, default, default);
	}

	partial void AddPointerHandler(RoutedEvent routedEvent, int handlersCount, object handler, bool handledEventsToo)
	{
		if (_pointerEventArgs == null)
		{
			_pointerEventArgs = TSInteropMarshaller.Allocate<NativePointerEventArgs>("UnoStatic_Windows_UI_Xaml_UIElement:setPointerEventArgs");
			_pointerEventResult = TSInteropMarshaller.Allocate<NativePointerEventResult>("UnoStatic_Windows_UI_Xaml_UIElement:setPointerEventResult");
		}

		if (handlersCount != 1
			|| routedEvent == PointerCaptureLostEvent) // Captures are handled in managed code only
		{
			return;
		}

		var events = default(NativePointerEvent);

		// First we make sure to subscribe to the missing defaults events
		events |= ~_subscribedNativeEvents & NativePointerEvent.Defaults;

		// Add optional events
		switch (routedEvent.Flag)
		{
			case RoutedEventFlag.PointerWheelChanged when !_subscribedNativeEvents.HasFlag(NativePointerEvent.wheel):
				events |= NativePointerEvent.wheel;
				break;
		}

		if (events is not default(NativePointerEvent))
		{
			TSInteropMarshaller.InvokeJS(
				"UnoStatic_Windows_UI_Xaml_UIElement:subscribePointerEvents",
				new NativePointerSubscriptionParams
				{
					HtmlId = Handle,
					Events = (byte)events
				});

			_subscribedNativeEvents |= events;
		}
	}

	partial void RemovePointerHandler(RoutedEvent routedEvent, int remainingHandlersCount, object handler)
	{
		if (remainingHandlersCount > 0)
		{
			return;
		}

		if (!IsLoaded)
		{
			// If element is not in the visual tree its native element might also have been already destroyed,
			// in that case we just ignore the native handler removal.
			// The native handler will somehow leak, but if the control is re-used we will actually avoid the cost to re-create the handler,
			// and if the control if dropped, then native handler will also be dropped.
			// Even if not recommended, it might happen that users use the control finalizer to remove pointer events
			// (cf. WinUI's TreeViewItem.RecycleEvents() invoked by the finalizer), this check make sure to not crash in the finalizer!
			return;
		}

		if (_gestures is null
			&& CountHandler(PointerEnteredEvent) is 0
			&& CountHandler(PointerExitedEvent) is 0
			&& CountHandler(PointerPressedEvent) is 0
			&& CountHandler(PointerReleasedEvent) is 0
			&& CountHandler(PointerCanceledEvent) is 0
			&& CountHandler(PointerMovedEvent) is 0
			&& CountHandler(PointerWheelChangedEvent) is 0)
		{
			TSInteropMarshaller.InvokeJS(
				"UnoStatic_Windows_UI_Xaml_UIElement:unSubscribePointerEvents",
				new NativePointerSubscriptionParams
				{
					HtmlId = Handle,
					Events = (byte)_subscribedNativeEvents
				});

			_subscribedNativeEvents = default;
		}
	}
	#endregion

	#region Native event dispatch
	[EditorBrowsable(EditorBrowsableState.Never)]
	[JSExport]
	internal static void OnNativePointerEvent()
	{
		var result = new HtmlEventDispatchResultHelper();
		try
		{
			_logTrace?.Trace("Receiving native pointer event.");

			var args = _pointerEventArgs.Value;
			var element = GetElementFromHandle(args.HtmlId);

			if (element is null)
			{
				if (_log.IsEnabled(LogLevel.Error))
				{
					_log.Error($"Received pointer event for '{args.HtmlId}' but element does not exists anymore.");
				}

				return;
			}

			_logTrace?.Trace($"Dispatching to {element.GetDebugName()} pointer arg: {args}.");

			switch ((NativePointerEvent)args.Event)
			{
				case NativePointerEvent.pointerover:
					{
						// On WASM we do get 'pointerover' event for sub elements,
						// so we can avoid useless work by validating if the pointer is already flagged as over element.
						// If so, we stop bubbling since our parent will do the same!
						var routedArgs = ToPointerArgs(element, args);
						if (element.IsOver(routedArgs.Pointer))
						{
							result.Add(HtmlEventDispatchResult.StopPropagation);
						}
						else
						{
							result.Add(routedArgs, element.OnNativePointerEnter(routedArgs));
						}

						break;
					}

				case NativePointerEvent.pointerout: // No needs to check IsOver (leaving vs. bubbling), it's already done in native code
					{
						var routedArgs = ToPointerArgs(element, args);
						var handled = element.OnNativePointerExited(routedArgs);
						result.Add(routedArgs, handled);

						break;
					}

				case NativePointerEvent.pointerdown:
					{
						var routedArgs = ToPointerArgs(element, args);
						var handled = element.OnNativePointerDown(routedArgs);
						result.Add(routedArgs, handled);

						break;
					}

				case NativePointerEvent.pointerup:
					{
						var routedArgs = ToPointerArgs(element, args);
						var handled = element.OnNativePointerUp(routedArgs);
						result.Add(routedArgs, handled);

						if (result.ShouldStop)
						{
							if (WinUICoreServices.Instance.MainRootVisual is not IRootElement rootElement)
							{
								rootElement = XamlRoot?.VisualTree.RootElement as IRootElement;
							}
							rootElement?.RootElementLogic.ProcessPointerUp(args, isAfterHandledUp: true);
						}

						break;
					}

				case NativePointerEvent.pointermove:
					{
						var routedArgs = ToPointerArgs(element, args);

						// We do have "implicit capture" for touch and pen on chromium browsers, they won't raise 'pointerout' once in contact,
						// this means that we need to validate the isOver to raise enter and exit like on UWP.
						var needsToCheckIsOver = routedArgs.Pointer.PointerDeviceType switch
						{
							PointerDeviceType.Touch => routedArgs.Pointer.IsInRange, // If pointer no longer in range, isOver is always false
							PointerDeviceType.Pen => routedArgs.Pointer.IsInContact, // We can ignore check when only hovering elements, browser does its job in that case
							PointerDeviceType.Mouse => PointerCapture.TryGet(routedArgs.Pointer, out _), // Browser won't raise pointerenter/out as soon has we have an active capture
							_ => true // For safety
						};
						var handled = needsToCheckIsOver
							? element.OnNativePointerMoveWithOverCheck(routedArgs, isOver: routedArgs.IsPointCoordinatesOver(element))
							: element.OnNativePointerMove(routedArgs);
						result.Add(routedArgs, handled);

						break;
					}

				case NativePointerEvent.pointercancel:
					{
						var routedArgs = ToPointerArgs(element, args);
						var handled = element.OnNativePointerCancel(routedArgs, isSwallowedBySystem: true);
						result.Add(routedArgs, handled);

						break;
					}

				case NativePointerEvent.wheel:
					if (args.wheelDeltaX is not 0)
					{
						var routedArgs = ToPointerArgs(element, args, wheel: (true, args.wheelDeltaX));
						var handled = element.OnNativePointerWheel(routedArgs);
						result.Add(routedArgs, handled);
					}

					if (args.wheelDeltaY is not 0)
					{
						// Note: Web browser vertical scrolling is the opposite compared to WinUI!
						var routedArgs = ToPointerArgs(element, args, wheel: (false, -args.wheelDeltaY));
						var handled = element.OnNativePointerWheel(routedArgs);
						result.Add(routedArgs, handled);
					}

					break;
			}
		}
		catch (Exception error)
		{
			if (_log.IsEnabled(LogLevel.Error))
			{
				_log.Error($"Failed to dispatch native pointer event: {error}");
			}
		}
		finally
		{
			_pointerEventResult.Value = new NativePointerEventResult
			{
				Result = (byte)result.Value
			};
		}
	}

	private static PointerRoutedEventArgs ToPointerArgs(
		UIElement snd,
		NativePointerEventArgs args,
		(bool isHorizontalWheel, double delta) wheel = default)
	{
		const int cancel = (int)NativePointerEvent.pointercancel;
		const int exitOrUp = (int)(NativePointerEvent.pointerout | NativePointerEvent.pointerup);

		var pointerType = (PointerDeviceType)args.deviceType;
		var pointerId = PointerIdentifierPool.RentManaged(new PointerIdentifier((PointerIdentifierDeviceType)pointerType, (uint)args.pointerId));

		var src = GetElementFromHandle(args.srcHandle) ?? (UIElement)snd;
		var position = new Point(args.x, args.y);
		var isInContact = args.buttons != 0;
		var isInRange = true;
		if (args.Event is cancel)
		{
			isInRange = false;
		}
		else if ((args.Event & exitOrUp) != 0)
		{
			isInRange = pointerType switch
			{
				PointerDeviceType.Mouse => true, // Mouse is always in range (unless for 'cancel' eg. if it was unplugged from computer)
				PointerDeviceType.Touch => false, // If we get a pointer out for touch, it means that pointer left the screen (pt up - a.k.a. Implicit capture)
				PointerDeviceType.Pen => args.hasRelatedTarget, // If the relatedTarget is null it means pointer left the detection range ... but only for out event!
				_ => !isInContact // Safety!
			};
		}
		var keyModifiers = VirtualKeyModifiers.None;
		if (args.ctrl) keyModifiers |= VirtualKeyModifiers.Control;
		if (args.shift) keyModifiers |= VirtualKeyModifiers.Shift;

		return new PointerRoutedEventArgs(
			args.timestamp,
			pointerId,
			position,
			isInContact,
			isInRange,
			(WindowManagerInterop.HtmlPointerButtonsState)args.buttons,
			(WindowManagerInterop.HtmlPointerButtonUpdate)args.buttonUpdate,
			keyModifiers,
			args.pressure,
			wheel,
			src);
	}
	#endregion

	partial void OnManipulationModeChanged(ManipulationModes oldMode, ManipulationModes newMode)
	{
		if (newMode == ManipulationModes.System)
		{
			ResetStyle("touch-action");
		}
		else
		{
			// 'none' here means that the browser is not allowed to steal pointer for it's own manipulations
			SetStyle("touch-action", "none");
		}
	}

	#region HitTestVisibility
	internal void UpdateHitTest()
	{
		this.CoerceValue(HitTestVisibilityProperty);
	}

	[GeneratedDependencyProperty(DefaultValue = HitTestability.Collapsed, ChangedCallback = true, CoerceCallback = true, Options = FrameworkPropertyMetadataOptions.Inherits)]
	internal static DependencyProperty HitTestVisibilityProperty { get; } = CreateHitTestVisibilityProperty();

	internal HitTestability HitTestVisibility
	{
		get => GetHitTestVisibilityValue();
		set => SetHitTestVisibilityValue(value);
	}

	/// <summary>
	/// This calculates the final hit-test visibility of an element.
	/// </summary>
	/// <returns></returns>
	private object CoerceHitTestVisibility(object baseValue)
	{
		// The HitTestVisibilityProperty is never set directly. This means that baseValue is always the result of the parent's CoerceHitTestVisibility.
		var baseHitTestVisibility = (HitTestability)baseValue;

		// If the parent is collapsed, we should be collapsed as well. This takes priority over everything else, even if we would be visible otherwise.
		if (baseHitTestVisibility == HitTestability.Collapsed)
		{
			return HitTestability.Collapsed;
		}

		// If we're not locally hit-test visible, visible, or enabled, we should be collapsed. Our children will be collapsed as well.
		// SvgElements are an exception here since they won't be loaded.
		if (!(IsLoaded || HtmlTagIsSvg) || !IsHitTestVisible || Visibility != Visibility.Visible || !IsEnabledOverride())
		{
			return HitTestability.Collapsed;
		}

		// Special case for external html element, we are always considering them as hit testable.
		if (HtmlTagIsExternallyDefined && !FeatureConfiguration.FrameworkElement.UseLegacyHitTest)
		{
			return HitTestability.Visible;
		}

		// If we're not collapsed or invisible, we can be targeted by hit-testing. This means that we can be the source of pointer events.		
		if (IsViewHit())
		{
			return HitTestability.Visible;
		}

		// If we're not hit (usually means we don't have a Background/Fill), we're invisible. Our children will be visible or not, depending on their state.
		return HitTestability.Invisible;
	}

	private protected virtual void OnHitTestVisibilityChanged(HitTestability oldValue, HitTestability newValue)
	{
		ApplyHitTestVisibility(newValue);
	}

	private void ApplyHitTestVisibility(HitTestability value)
	{
		// By default, elements have 'pointer-event' set to 'none' (see Uno.UI.css .uno-uielement class)
		// which is aligned with HitTestVisibilityProperty's default value of Visible.
		// If HitTestVisibilityProperty is calculated to Invisible or Collapsed,
		// we don't want to be the target of hit-testing and raise any pointer events.
		// This is done by setting 'pointer-events' to 'none'.
		// However setting it to 'none' will allow pointer event to pass through the element (a.k.a. Invisible)

		WindowManagerInterop.SetPointerEvents(HtmlId, value is HitTestability.Visible);

		if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
		{
			UpdateDOMProperties();
		}
	}

	internal void SetHitTestVisibilityForRoot()
	{
		// Root element must be visible to hit testing, regardless of the other properties values.
		// The default value of HitTestVisibility is collapsed to avoid spending time coercing to a
		// Collapsed.
		HitTestVisibility = HitTestability.Visible;
	}

	internal void ClearHitTestVisibilityForRoot()
	{
		this.ClearValue(HitTestVisibilityProperty);
	}

	#endregion

	[TSInteropMessage(Marshaller = CodeGeneration.Disabled, UnMarshaller = CodeGeneration.Enabled)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct NativePointerSubscriptionParams
	{
		public IntPtr HtmlId;

		public byte Events; // One or multiple NativePointerEvent
	}

	[TSInteropMessage(Marshaller = CodeGeneration.Enabled, UnMarshaller = CodeGeneration.Disabled)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct NativePointerEventArgs
	{
		public IntPtr HtmlId;

		public byte Event; // ONE of NativePointerEvent

		public double pointerId; // Warning: This is a Number in JS, and it might be negative on safari for iOS
		public double x;
		public double y;
		public bool ctrl;
		public bool shift;
		public int buttons;
		public int buttonUpdate;
		public int deviceType;
		public int srcHandle;
		public double timestamp;
		public double pressure;
		public double wheelDeltaX;
		public double wheelDeltaY;
		public bool hasRelatedTarget;

		/// <inheritdoc />
		public override string ToString()
			=> $"elt={HtmlId}|evt={(NativePointerEvent)Event}|id={pointerId}|x={x}|y={x}|ctrl={ctrl}|shift={shift}|bts={buttons}|btUpdate={buttonUpdate}|type={(PointerDeviceType)deviceType}|srcId={srcHandle}|ts={timestamp}|pres={pressure}|wheelX={wheelDeltaX}|wheelY={wheelDeltaY}|relTarget={hasRelatedTarget}";
	}

	[TSInteropMessage(Marshaller = CodeGeneration.Disabled, UnMarshaller = CodeGeneration.Enabled)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	private struct NativePointerEventResult
	{
		public byte Result; // HtmlEventDispatchResult
	}

	[Flags]
	private enum NativePointerEvent : byte
	{
		// WARNING: This enum has a corresponding version in TypeScript!

		// Minimal default pointer required to maintain state
		pointerover = 1,
		pointerout = 1 << 1,
		pointerdown = 1 << 2,
		pointerup = 1 << 3,
		pointercancel = 1 << 4,
		pointermove = 1 << 5, // Required since when pt is captured, isOver will be maintained by the moveWithOverCheck

		// Optional pointer events
		wheel = 1 << 6,

		Defaults = pointerover | pointerout | pointerdown | pointerup | pointercancel | pointermove,
	}
}
