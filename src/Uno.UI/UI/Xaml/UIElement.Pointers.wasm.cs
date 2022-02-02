#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Uno.Foundation;
using Windows.Foundation;
using Windows.UI.Xaml.Input;
using Windows.System;
using Uno;
using Uno.Foundation.Interop;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Windows.UI.Xaml;

public partial class UIElement : DependencyObject
{
	private static TSInteropMarshaller.HandleRef<NativePointerEventArgs> _pointerEventArgs;
	private static TSInteropMarshaller.HandleRef<NativePointerEventResult> _pointerEventResult;

	static partial void InitializePointersStaticPartial()
	{
		_pointerEventArgs = TSInteropMarshaller.Allocate<NativePointerEventArgs>("UnoStatic_Windows_UI_Xaml_UIElement:setPointerEventArgs");
		_pointerEventResult = TSInteropMarshaller.Allocate<NativePointerEventResult>("UnoStatic_Windows_UI_Xaml_UIElement:setPointerEventResult");
	}

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
			case RoutedEventFlag.PointerMoved when !_subscribedNativeEvents.HasFlag(NativePointerEvent.pointermove):
				events |= NativePointerEvent.pointermove;
				break;

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
	[Preserve]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static void OnNativePointerEvent()
	{
		var stopPropagation = false;
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
					var ptArgs = ToPointerArgs(element, args);
					stopPropagation = element.IsOver(ptArgs.Pointer) || element.OnNativePointerEnter(ptArgs);
					break;
				}

				case NativePointerEvent.pointerout: // No needs to check IsOver, it's already done in native code
					stopPropagation = element.OnNativePointerExited(ToPointerArgs(element, args));
					break;

				case NativePointerEvent.pointerdown:
					stopPropagation = element.OnNativePointerDown(ToPointerArgs(element, args, isInContact: true));
					break;

				case NativePointerEvent.pointerup:
					stopPropagation = element.OnNativePointerUp(ToPointerArgs(element, args, isInContact: false));
					break;

				case NativePointerEvent.pointermove:
					stopPropagation = element.OnNativePointerMove(ToPointerArgs(element, args));
					break;

				case NativePointerEvent.pointercancel:
					stopPropagation = element.OnNativePointerCancel(ToPointerArgs(element, args, isInContact: false), isSwallowedBySystem: true);
					break;

				case NativePointerEvent.wheel:
					if (args.wheelDeltaX is not 0)
					{
						stopPropagation |= element.OnNativePointerWheel(ToPointerArgs(element, args, wheel: (true, args.wheelDeltaX), isInContact: null /* maybe */));
					}
					if (args.wheelDeltaY is not 0)
					{
						// Note: Web browser vertical scrolling is the opposite compared to WinUI!
						stopPropagation |= element.OnNativePointerWheel(ToPointerArgs(element, args, wheel: (false, -args.wheelDeltaY), isInContact: null /* maybe */));
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
				Result = (byte)(stopPropagation
					? HtmlEventDispatchResult.StopPropagation
					: HtmlEventDispatchResult.Ok)
			};
		}
	}

	private static PointerRoutedEventArgs ToPointerArgs(
		UIElement snd,
		NativePointerEventArgs args,
		bool? isInContact = null,
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
		if (value == HitTestability.Visible)
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
		public string typeStr;
		public int srcHandle;
		public double timestamp;
		public double pressure;
		public double wheelDeltaX;
		public double wheelDeltaY;

		/// <inheritdoc />
		public override string ToString()
			=> $"elt={HtmlId}|evt={(NativePointerEvent)Event}|id={pointerId}|x={x}|y={x}|ctrl={ctrl}|shift={shift}|bts={buttons}|btUpdate={buttonUpdate}|tyep={typeStr}|srcId={srcHandle}|ts={timestamp}|pres={pressure}|wheelX={wheelDeltaX}|wheelY={wheelDeltaY}";
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

		// Optional pointer events
		pointermove = 1 << 5,
		wheel = 1 << 6,

		Defaults = pointerover | pointerout | pointerdown | pointerup | pointercancel,
	}
}
