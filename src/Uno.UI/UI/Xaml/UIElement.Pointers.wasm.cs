#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
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

using PointerIdentifier = Windows.Devices.Input.PointerIdentifier;

#if HAS_UNO_WINUI
using Microsoft.UI.Input;
#else
using Windows.Devices.Input;
using Windows.UI.Input;
#endif

namespace Microsoft.UI.Xaml;

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

				case NativePointerEvent.pointerout: // No needs to check IsOver (leaving vs. bubbling), it's already done in native code
					stopPropagation = element.OnNativePointerExited(ToPointerArgs(element, args));
					break;

				case NativePointerEvent.pointerdown:
					stopPropagation = element.OnNativePointerDown(ToPointerArgs(element, args));
					break;

				case NativePointerEvent.pointerup:
					{
						var routedArgs = ToPointerArgs(element, args);
						stopPropagation = element.OnNativePointerUp(routedArgs);

						if (stopPropagation)
						{
							RootVisual.ProcessPointerUp(routedArgs, isAfterHandledUp: true);
						}

						break;
					}

				case NativePointerEvent.pointermove:
					{
						var ptArgs = ToPointerArgs(element, args);

						// We do have "implicit capture" for touch and pen on chromium browsers, they won't raise 'pointerout' once in contact,
						// this means that we need to validate the isOver to raise enter and exit like on UWP.
						var needsToCheckIsOver = ptArgs.Pointer.PointerDeviceType switch
						{
							PointerDeviceType.Touch => ptArgs.Pointer.IsInRange, // If pointer no longer in range, isOver is always false
							PointerDeviceType.Pen => ptArgs.Pointer.IsInContact, // We can ignore check when only hovering elements, browser does its job in that case
							PointerDeviceType.Mouse => PointerCapture.TryGet(ptArgs.Pointer, out _), // Browser won't raise pointerenter/out as soon has we have an active capture
							_ => true // For safety
						};
						stopPropagation = needsToCheckIsOver
							? element.OnNativePointerMoveWithOverCheck(ptArgs, isOver: ptArgs.IsPointCoordinatesOver(element))
							: element.OnNativePointerMove(ptArgs);
						break;
					}

				case NativePointerEvent.pointercancel:
					stopPropagation = element.OnNativePointerCancel(ToPointerArgs(element, args), isSwallowedBySystem: true);
					break;

				case NativePointerEvent.wheel:
					if (args.wheelDeltaX is not 0)
					{
						stopPropagation |= element.OnNativePointerWheel(ToPointerArgs(element, args, wheel: (true, args.wheelDeltaX)));
					}
					if (args.wheelDeltaY is not 0)
					{
						// Note: Web browser vertical scrolling is the opposite compared to WinUI!
						stopPropagation |= element.OnNativePointerWheel(ToPointerArgs(element, args, wheel: (false, -args.wheelDeltaY)));
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

	private static readonly Dictionary<PointerIdentifier, PointerIdentifier> _nativeToManagedPointerId = new();
	private static readonly Dictionary<PointerIdentifier, PointerIdentifier> _managedToNativePointerId = new();
	private static uint _lastUsedId;

	private static uint TransformPointerId(PointerIdentifier nativeId)
	{
		if (_nativeToManagedPointerId.TryGetValue(nativeId, out var managedId))
		{
			return managedId.Id;
		}

		managedId = new PointerIdentifier(nativeId.Type, ++_lastUsedId);
		_managedToNativePointerId[managedId] = nativeId;
		_nativeToManagedPointerId[nativeId] = managedId;

		return managedId.Id;
	}

	internal static void RemoveActivePointer(PointerIdentifier managedId)
	{
		if (_managedToNativePointerId.TryGetValue(managedId, out var nativeId))
		{
			_managedToNativePointerId.Remove(managedId);
			_nativeToManagedPointerId.Remove(nativeId);

			if (_managedToNativePointerId.Count == 0)
			{
				_lastUsedId = 0; // We reset the pointer ID only when there is no active pointer.
			}
		}
		else if (typeof(UIElement).Log().IsEnabled(LogLevel.Warning))
		{
			typeof(UIElement).Log().Warn($"Received an invalid managed pointer id {managedId}");
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
		var pointerId = TransformPointerId(new PointerIdentifier((Windows.Devices.Input.PointerDeviceType)pointerType, (uint)args.pointerId));

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
			pointerType,
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
		if (newMode is ManipulationModes.None or ManipulationModes.System)
		{
			ResetStyle("touch-action");
		}
		else
		{
			// 'none' here means that the browser is not allowed to steal pointer for it's own manipulations
			SetStyle("touch-action", "none");
		}
	}

	#region Capture
	partial void CapturePointerNative(Pointer pointer)
	{
		var command = "Uno.UI.WindowManager.current.setPointerCapture(" + HtmlId + ", " + pointer.PointerId + ");";
		WebAssemblyRuntime.InvokeJS(command);
	}

	partial void ReleasePointerNative(Pointer pointer)
	{
		var command = "Uno.UI.WindowManager.current.releasePointerCapture(" + HtmlId + ", " + pointer.PointerId + ");";
		WebAssemblyRuntime.InvokeJS(command);
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
