// MUX Reference dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp
//
// NOTE: ToolTipServiceMetadata + static fields live in ToolTipService.partial.h.mux.cs
// (port of ToolTipService_Partial.h). This file ports method bodies in the order
// they appear in ToolTipService_Partial.cpp.

#if __SKIA__

#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

public partial class ToolTipService
{
#pragma warning disable IDE0051 // Remove unused private members (placeholder until later phases)
#pragma warning disable IDE0060 // Remove unused parameter (placeholder)

	// MUX Reference: ToolTipService_Partial.cpp ToolTipServiceMetadata constructor (line 28).
	// Phase 6 will activate the PowerSettingRegisterNotification display-state hook.
	// For now the metadata is created lazily on first access via GetToolTipServiceMetadata.

	private static ToolTipServiceMetadata? s_toolTipServiceMetadata;

	internal static ToolTipServiceMetadata GetToolTipServiceMetadata()
	{
		s_toolTipServiceMetadata ??= new ToolTipServiceMetadata();
		return s_toolTipServiceMetadata;
	}

	// MUX Reference: ToolTipService_Partial.cpp RegisterToolTip (line 110).
	internal static void RegisterToolTip(
		DependencyObject pOwner,
		FrameworkElement pContainer,
		object pToolTipAsIInspectable,
		bool isKeyboardAcceleratorToolTip)
	{
		global::System.Diagnostics.Debug.Assert(pOwner is not null, "ToolTip must have an owner");
		global::System.Diagnostics.Debug.Assert(pContainer is not null, "ToolTip must have an container");
		global::System.Diagnostics.Debug.Assert(pToolTipAsIInspectable is not null, "ToolTip can not be null");

		bool inputEventsAlreadyHookedUp = false;

		ToolTip? spToolTipObject;
		if (isKeyboardAcceleratorToolTip)
		{
			spToolTipObject = GetKeyboardAcceleratorToolTipObject(pOwner);
		}
		else
		{
			spToolTipObject = GetToolTipReference(pOwner);
		}

		if (spToolTipObject is not null)
		{
			inputEventsAlreadyHookedUp = spToolTipObject.m_bInputEventsHookedUp;
		}

		// Set the tooltip before applying the delegates, otherwise the owner
		// will try to call into the tool tip services.
		var spIToolTip = ConvertToToolTip(pToolTipAsIInspectable);

		var pToolTipNoRef = spIToolTip;
		pToolTipNoRef.SetOwner(pOwner);
		pToolTipNoRef.SetContainer(pContainer);

		if (isKeyboardAcceleratorToolTip)
		{
			SetKeyboardAcceleratorToolTipObject(pOwner, pToolTipNoRef);
		}
		else
		{
			SetToolTipReference(pOwner, pToolTipNoRef);
		}

		// If the owner is also the container, then we'll want to attach pointer events,
		// since nothing will be already listening to pointer events for us.
		if (ReferenceEquals(pOwner, pContainer) && !inputEventsAlreadyHookedUp)
		{
			var pOwnerAsFENoRef = (FrameworkElement)pOwner;

			pOwnerAsFENoRef.PointerEntered += OnOwnerPointerEntered;
			pToolTipNoRef.m_ownerPointerEnteredToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerEntered -= OnOwnerPointerEntered);

			pOwnerAsFENoRef.PointerExited += OnOwnerPointerExitedOrLostOrCanceled;
			pToolTipNoRef.m_ownerPointerExitedToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerExited -= OnOwnerPointerExitedOrLostOrCanceled);

			pOwnerAsFENoRef.PointerCaptureLost += OnOwnerPointerExitedOrLostOrCanceled;
			pToolTipNoRef.m_ownerPointerCaptureLostToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerCaptureLost -= OnOwnerPointerExitedOrLostOrCanceled);

			pOwnerAsFENoRef.PointerCanceled += OnOwnerPointerExitedOrLostOrCanceled;
			pToolTipNoRef.m_ownerPointerCanceledToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.PointerCanceled -= OnOwnerPointerExitedOrLostOrCanceled);

			pOwnerAsFENoRef.GotFocus += OnOwnerGotFocus;
			pToolTipNoRef.m_ownerGotFocusToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.GotFocus -= OnOwnerGotFocus);

			pOwnerAsFENoRef.LostFocus += OnOwnerLostFocus;
			pToolTipNoRef.m_ownerLostFocusToken.Disposable = global::Uno.Disposables.Disposable.Create(
				() => pOwnerAsFENoRef.LostFocus -= OnOwnerLostFocus);

			// Uno-specific UX: a click on a Button-based owner closes the tooltip.
			// WinUI doesn't subscribe here — it relies on hit-testing the popup. Until
			// safe-zone (Phase 6) and Popup hit-test forwarding are wired on Skia, the
			// existing Uno behavior matches the cross-platform ToolTipService.cs path.
			if (pOwnerAsFENoRef is ButtonBase buttonBaseOwner)
			{
				PointerEventHandler pointerPressedHandler = OnOwnerPointerPressed;
				buttonBaseOwner.AddHandler(UIElement.PointerPressedEvent, pointerPressedHandler, handledEventsToo: true);
				pToolTipNoRef.m_ownerPointerPressedToken.Disposable = global::Uno.Disposables.Disposable.Create(
					() => buttonBaseOwner.RemoveHandler(UIElement.PointerPressedEvent, pointerPressedHandler));
			}

			pToolTipNoRef.m_bInputEventsHookedUp = true;
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp UnregisterToolTip (line 251).
	internal static void UnregisterToolTip(
		DependencyObject pOwner,
		FrameworkElement pContainer,
		bool isKeyboardAcceleratorToolTip)
	{
		global::System.Diagnostics.Debug.Assert(pOwner is not null, "owner element is required");
		global::System.Diagnostics.Debug.Assert(pContainer is not null, "container element is required");

		ToolTip? spToolTipObject;
		if (isKeyboardAcceleratorToolTip)
		{
			spToolTipObject = GetKeyboardAcceleratorToolTipObject(pOwner);
		}
		else
		{
			spToolTipObject = GetToolTipReference(pOwner);
		}

		var spToolTipObjectConcrete = spToolTipObject;
		global::System.Diagnostics.Debug.Assert(spToolTipObjectConcrete is not null);
		if (spToolTipObjectConcrete is null)
		{
			return;
		}

		if (spToolTipObjectConcrete.m_bInputEventsHookedUp)
		{
			spToolTipObjectConcrete.m_ownerPointerEnteredToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerPointerExitedToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerPointerCaptureLostToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerPointerCanceledToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerGotFocusToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerLostFocusToken.Disposable = null;
			spToolTipObjectConcrete.m_ownerPointerPressedToken.Disposable = null;
		}

		spToolTipObjectConcrete.SetOwner(null);
		spToolTipObjectConcrete.SetContainer(null);

		// Close the ToolTip if it's open, or cancel it from opening if it's in the process of opening
		OnOwnerLeaveInternal(pOwner);

		var toolTipProperty = isKeyboardAcceleratorToolTip
			? KeyboardAcceleratorToolTipObjectProperty
			: ToolTipReferenceProperty;
		pOwner.ClearValue(toolTipProperty);
	}

	// MUX Reference: ToolTipService_Partial.cpp GetActualToolTipObjectStatic (line 302).
	// Renamed to GetActualToolTipObject to match Uno call sites that already exist
	// (DXamlTestHooks.cs etc.).
	internal static ToolTip? GetActualToolTipObject(DependencyObject element)
	{
		// Try to get the actual public tooltip object
		var toolTip = GetToolTipReference(element);

		// If public tooltip doesn't exist, then look for keyboard accelerator tooltip.
		if (toolTip is null)
		{
			toolTip = GetKeyboardAcceleratorToolTipObject(element);
		}

		return toolTip;
	}

	// MUX Reference: ToolTipService_Partial.cpp OnSafeZoneCheck (line 348).
	// ToolTip will not be closed until Pointer moves out of safe zone.
	// A timer is started when ToolTip is open, and then check if pointer is in the safe zone periodically.
	// Because Keyboard also opens ToolTip, s_pointerPointWhenSafeZoneTimerStart is used to determine if pointer
	// is moved or not after.
	private static void OnSafeZoneCheck()
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		// When screen is off, stop the scheduled timer to avoid battery drain like BUG 1735672 and BUG 1735672
		if (!pToolTipServiceMetadataNoRef.m_displayOn && pToolTipServiceMetadataNoRef.m_tpSafeZoneCheckTimer is not null)
		{
			pToolTipServiceMetadataNoRef.m_tpSafeZoneCheckTimer.Stop();
			CancelAutomaticToolTip();
			return;
		}

		if (pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is { } tooltip)
		{
			// We don't not use PointerPointStatic get_Position here, since it is based on the current window.
			// This prevents us from getting the right position in out of window ToolTip cases.
			// We call GetCursorPos() to get the screen based position instead of each window based position.
			// Uno: Win32 GetCursorPos has no direct Skia equivalent. Use TryGetLastPointerPosition
			// (PointerRoutedEventArgs.LastPointerEvent) instead. Forfeits screen-physical coordinate
			// semantics but acceptable since the new port runs in-window.
			if (TryGetLastPointerPosition(out var pointerPosition))
			{
				var startPosition = s_pointerPointWhenSafeZoneTimerStart;
				if (Math.Abs(startPosition.X - pointerPosition.X) < 0.1 && Math.Abs(startPosition.Y - pointerPosition.Y) < 0.1)
				{
					// Pointer not moved, avoid to dismiss keyboard opened ToolTip
					return;
				}

				tooltip.HandlePointInSafeZone(pointerPosition);
			}
		}
	}

	// Uno-specific helper: read the last pointer position from
	// `Microsoft.UI.Xaml.Input.PointerRoutedEventArgs.LastPointerEvent` as a stand-in for the
	// Win32 `GetCursorPos` used by the C++ safe-zone timer. Returns false if no pointer has
	// been seen yet (e.g. the tooltip was opened by keyboard focus before any pointer move).
	private static bool TryGetLastPointerPosition(out Point position)
	{
		var lastPointer = Microsoft.UI.Xaml.Input.PointerRoutedEventArgs.LastPointerEvent;
		var pointerPoint = lastPointer?.GetCurrentPoint(null);
		if (pointerPoint is null)
		{
			position = default;
			return false;
		}

		position = pointerPoint.Position;
		return true;
	}

	// MUX Reference: ToolTipService_Partial.cpp StartSafeZoneCheckTimer (line 384).
	private static void StartSafeZoneCheckTimer(ToolTipServiceMetadata pToolTipServiceMetadataNoRef)
	{
		if (pToolTipServiceMetadataNoRef.m_tpSafeZoneCheckTimer is null)
		{
			var interval = TimeSpan.FromTicks(ToolTipServiceConstants.s_safeZoneCheckTimerDuration);

			var dispatcherQueue = global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
			var dispatcherQueueTimer = dispatcherQueue.CreateTimer();

			dispatcherQueueTimer.Tick += (sender, args) => OnSafeZoneCheck();
			dispatcherQueueTimer.Interval = interval;

			pToolTipServiceMetadataNoRef.SetSafeZoneTimer(dispatcherQueueTimer);
		}

		pToolTipServiceMetadataNoRef.m_tpSafeZoneCheckTimer!.Start();

		// Uno: Win32 GetCursorPos. On Skia capture the last pointer position via the
		// LastPointerEvent (TryGetLastPointerPosition helper). This is what subsequent
		// OnSafeZoneCheck ticks compare against.
		_ = TryGetLastPointerPosition(out s_pointerPointWhenSafeZoneTimerStart);
	}

	// MUX Reference: ToolTipService_Partial.cpp OpenAutomaticToolTip (line 423).
	private static void OpenAutomaticToolTip(object? pUnused1, object? pUnused2)
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_tpOpenTimer is not null)
		{
			pToolTipServiceMetadataNoRef.m_tpOpenTimer.Stop();
		}

		global::System.Diagnostics.Debug.Assert(pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is null);

		// ToolTipService does not open ToolTips automatically on Xbox.
		// TODO Uno (Phase 6): port XboxUtility::IsOnXbox check.
#if false
		if (XboxUtility::IsOnXbox())
		{
			goto Cleanup;
		}
#endif

		uint showDurationSeconds;
		// TODO Uno: SystemParametersInfo SPI_GETMESSAGEDURATION is Win32-only. Phase 6 polish
		// can wire platform-specific APIs; for now we use the WinUI fallback constant.
		showDurationSeconds = (uint)ToolTipServiceConstants.DEFAULT_SHOW_DURATION_SECONDS;
		var showDurationTimeSpan = TimeSpan.FromSeconds(showDurationSeconds);

		// If m_tpOwner or m_tpContainer is null, we received a Tick when the timer was already stopped.
		// Can't just check if timer was stopped because we sometimes call this directly.
		if (pToolTipServiceMetadataNoRef.m_tpOwner is null || pToolTipServiceMetadataNoRef.m_tpContainer is null)
		{
			return;
		}

		if (!pToolTipServiceMetadataNoRef.m_tpContainer.IsLoaded)
		{
			return;
		}

		var spToolTipObject = GetActualToolTipObject(pToolTipServiceMetadataNoRef.m_tpOwner);
		global::System.Diagnostics.Debug.Assert(spToolTipObject is not null, "ToolTip must have been registered");
		if (spToolTipObject is null)
		{
			return;
		}

		spToolTipObject.m_inputMode = s_lastEnterInputMode;

		s_bOpeningAutomaticToolTip = true;
		try
		{
			spToolTipObject.IsOpen = true;
		}
		finally
		{
			s_bOpeningAutomaticToolTip = false;
		}

		StartSafeZoneCheckTimer(pToolTipServiceMetadataNoRef);
	}

	// MUX Reference: ToolTipService_Partial.cpp CloseAutomaticToolTip (line 496).
	private static void CloseAutomaticToolTip(object? pUnused1, object? pUnused2)
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_tpCloseTimer is not null)
		{
			pToolTipServiceMetadataNoRef.m_tpCloseTimer.Stop();
		}

		if (pToolTipServiceMetadataNoRef.m_tpSafeZoneCheckTimer is not null)
		{
			pToolTipServiceMetadataNoRef.m_tpSafeZoneCheckTimer.Stop();
		}

		if (pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is not null)
		{
			global::System.Diagnostics.Debug.Assert(pToolTipServiceMetadataNoRef.m_tpCurrentPopup is not null);
			pToolTipServiceMetadataNoRef.m_tpCurrentToolTip.IsOpen = false;

			s_lastToolTipOpenedTime = Environment.TickCount;
		}

		if (pToolTipServiceMetadataNoRef.m_lastToolTipOwnerInSafeZone is not null)
		{
			var owner = pToolTipServiceMetadataNoRef.m_lastToolTipOwnerInSafeZone.Target as DependencyObject;
			pToolTipServiceMetadataNoRef.m_lastToolTipOwnerInSafeZone = null;
			if (owner is not null)
			{
				RemoveFromNestedOwners(owner);
			}
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp ConvertToToolTip (line 537).
	// Wraps a content object in a ToolTip if it isn't already one. Also handles the case
	// where the object is already parented to a ToolTip (returns that parent).
	private static ToolTip ConvertToToolTip(object objectIn)
	{
		if (objectIn is not ToolTip toolTip)
		{
			if (objectIn is FrameworkElement)
			{
				var objectInParent = (objectIn as DependencyObject)?.GetParent();
				if (objectInParent is ToolTip parentToolTip)
				{
					return parentToolTip;
				}
			}

			toolTip = new ToolTip
			{
				Content = objectIn
			};
		}

		return toolTip;
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerEnterInternal (line 583).
	private static void OnOwnerEnterInternal(object pSender, object? pSource, AutomaticToolTipInputMode mode)
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		bool isSenderLastEnterSource =
			pToolTipServiceMetadataNoRef.m_tpLastEnterSource is not null &&
			ReferenceEquals(pToolTipServiceMetadataNoRef.m_tpLastEnterSource, pSource);
		if (pToolTipServiceMetadataNoRef.m_tpLastEnterSource is not null && isSenderLastEnterSource)
		{
			// ToolTipService had processed this event once before, when it fired on the child
			// skip it now
			return;
		}

		if (pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is not null)
		{
			var spSenderAsIDO = pSender as DependencyObject;
			ToolTip? spToolTipObject = null;
			if (spSenderAsIDO is not null)
			{
				spToolTipObject = GetActualToolTipObject(spSenderAsIDO);
			}

			if (!ReferenceEquals(spToolTipObject, pToolTipServiceMetadataNoRef.m_tpCurrentToolTip))
			{
				// first close the previous ToolTip if entering nested elements with tooltips
				CloseAutomaticToolTip(null, null);
			}
			else
			{
				// reentering the same element
				return;
			}
		}

		// CStaticLock equivalent: not needed in C# managed model.
		{
			var spSenderAsDO = pSender as DependencyObject;
			if (spSenderAsDO is null)
			{
				return;
			}

			var spContainer = GetContainerFromOwner(spSenderAsDO);

			pToolTipServiceMetadataNoRef.SetOwner(spSenderAsDO);
			pToolTipServiceMetadataNoRef.SetContainer(spContainer);
			pToolTipServiceMetadataNoRef.SetLastEnterSource(pSource);
		}

		global::System.Diagnostics.Debug.Assert(pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is null);

		// open the ToolTip after the InitialShowDelay interval expires
		if (pToolTipServiceMetadataNoRef.m_tpOpenTimer is null)
		{
			var spNewDispatcherTimer = new DispatcherTimer();
			pToolTipServiceMetadataNoRef.SetOpenTimer(spNewDispatcherTimer);

			pToolTipServiceMetadataNoRef.m_tpOpenTimer!.Tick += OpenAutomaticToolTip;
		}

		s_lastEnterInputMode = mode;

		bool useReshowTimer = (Environment.TickCount - s_lastToolTipOpenedTime) < ToolTipServiceConstants.BETWEEN_SHOW_DELAY_MS;
		var initialShowDelayTimeSpan = GetInitialShowDelay(mode, useReshowTimer);
		pToolTipServiceMetadataNoRef.m_tpOpenTimer!.Interval = initialShowDelayTimeSpan;
		pToolTipServiceMetadataNoRef.m_tpOpenTimer!.Start();
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerLeaveInternal (line 670).
	// Used to handle MouseLeave on a ToolTip's owner FrameworkElement.
	private static void OnOwnerLeaveInternal(object pSender)
	{
		var spSenderAsDO = pSender as DependencyObject;
		global::System.Diagnostics.Debug.Assert(spSenderAsDO is not null);
		if (spSenderAsDO is null)
		{
			return;
		}

		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		bool areEqual = ReferenceEquals(spSenderAsDO, pToolTipServiceMetadataNoRef.m_tpOwner);
		if (areEqual)
		{
			// No need to call RemoveFromNestedOwners() since CancelAutomaticToolTip calls it.
			CancelAutomaticToolTip();
		}
		else
		{
			RemoveFromNestedOwners(spSenderAsDO);
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp CancelAutomaticToolTip (line 703).
	// If there is an automatic ToolTip in the process of opening, stop it from opening.
	// If one is already open, close it.
	// Clear any state associated with the current automatic ToolTip and its owner.
	internal static void CancelAutomaticToolTip()
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is null)
		{
			// ToolTip had not been opened yet

			// There are some strange cases where the owner will get a leave but not an enter.
			// The _openTimer is initialized in the enter, so we need to make sure it is there
			// before we try to stop it.

			if (pToolTipServiceMetadataNoRef.m_tpOpenTimer is not null)
			{
				pToolTipServiceMetadataNoRef.m_tpOpenTimer.Stop();
			}
		}
		else
		{
			CloseAutomaticToolTip(null, null);
		}

		// CStaticLock equivalent: not needed in C# managed model.
		if (pToolTipServiceMetadataNoRef.m_tpOwner is not null)
		{
			RemoveFromNestedOwners(pToolTipServiceMetadataNoRef.m_tpOwner);
			pToolTipServiceMetadataNoRef.m_tpOwner = null;
		}
		pToolTipServiceMetadataNoRef.m_tpContainer = null;
		pToolTipServiceMetadataNoRef.m_tpLastEnterSource = null;
	}

	// MUX Reference: ToolTipService_Partial.cpp EnsureHandlersAttachedToRootElement (line 744).
	internal static void EnsureHandlersAttachedToRootElement(XamlRoot? visualTree)
	{
		if (visualTree?.Content is not UIElement rootElement)
		{
			return;
		}

		var toolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (!toolTipServiceMetadataNoRef.m_rootElementsWithHandlersNoRef.Contains(rootElement))
		{
			// These event handlers are never detached, but they will not leak the root element. They take references on the core
			// CUIElement. The DXaml layer's peer DirectUI::UIElement has a separate ref count, and when the DXaml peer is released,
			// it calls into the core to detach all event handlers. See DirectUI::DependencyObject::DisconnectFrameworkPeerCore's
			// loop over its m_pEventMap.

			rootElement.PointerMoved += OnRootVisualPointerMoved;

			if (rootElement is FrameworkElement rootFrameworkElement)
			{
				rootFrameworkElement.SizeChanged += OnRootVisualSizeChanged;
			}

			toolTipServiceMetadataNoRef.m_rootElementsWithHandlersNoRef.Add(rootElement);
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp OnPublicRootRemoved (line 794).
	internal static void OnPublicRootRemoved(UIElement publicRoot)
	{
		// This function can be called in a shutdown path after the ToolTipService has already been destroyed.
		// No need to (re)create the ToolTipService here since we only get it to remove handlers.
		var toolTipServiceMetadataNoRef = s_toolTipServiceMetadata;

		if (toolTipServiceMetadataNoRef is not null)
		{
			toolTipServiceMetadataNoRef.m_rootElementsWithHandlersNoRef.Remove(publicRoot);
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp GetToolTipOwnersBoundary (line 820).
	internal static Rect GetToolTipOwnersBoundary(DependencyObject ownerDO)
	{
		Rect bounds;
		if (ownerDO is UIElement owner)
		{
			bounds = GetGlobalBoundsLogical(owner);
		}
		else if (ownerDO is Documents.TextElement textElement)
		{
			// TODO Uno (Phase 6 polish): port CCoreServices::GetTextElementBoundingRect equivalent.
			// For now use the containing FrameworkElement's bounds.
			var fe = textElement.GetContainingFrameworkElement();
			if (fe is null)
			{
				return default;
			}
			bounds = GetGlobalBoundsLogical(fe);
		}
		else
		{
			return default;
		}

		// Validate non-NaN / non-Infinity bounds.
		if (double.IsInfinity(bounds.Left) || double.IsNaN(bounds.Left) ||
			double.IsInfinity(bounds.Top) || double.IsNaN(bounds.Top) ||
			double.IsInfinity(bounds.Right) || double.IsNaN(bounds.Right) ||
			double.IsInfinity(bounds.Bottom) || double.IsNaN(bounds.Bottom))
		{
			return default;
		}

		return bounds;
	}

	// MUX Reference: ToolTipService_Partial.cpp HandleToolTipSafeZone (line 862).
	internal static void HandleToolTipSafeZone(Point point, UIElement toolTip, DependencyObject ownerDO)
	{
		// On WindowsCore, because message event queue, even if ToolTip is unhooked from CoreWindow during the same PointerMove event handling,
		// CoreWindows.PointerMove event is still be received by ToolTip, and it makes the current ToolTip doesn't match with ToolTip from the event.
		// so we should only handle the tooltip if it's the current ToolTip.
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		var current = pToolTipServiceMetadataNoRef.m_tpCurrentToolTip;
		if (current is null || !ReferenceEquals(current, toolTip))
		{
			return;
		}

		// owner bounds in global space
		Rect ownerBounds = GetToolTipOwnersBoundary(ownerDO);

		// tooltip bounds in global space
		Rect toolTipBounds = GetGlobalBoundsLogical(toolTip);

		// outside of safe zone, close ToolTip
		if (!IsToolTipInSafeZone(point, ownerBounds, toolTipBounds))
		{
			CancelAutomaticToolTip();
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp CloseToolTipInternal (line 895).
	internal static void CloseToolTipInternal(KeyRoutedEventArgs? pIKeyRoutedEventArgs)
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_tpOpenTimer is null)
		{
			return;
		}

		// close the opened ToolTip or cancel mouse hover
		if (pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is null)
		{
			pToolTipServiceMetadataNoRef.m_tpOpenTimer.Stop();
			return;
		}

		if (pIKeyRoutedEventArgs is not null)
		{
			var key = pIKeyRoutedEventArgs.Key;
			if (IsSpecialKey(key))
			{
				return;
			}
		}

		CloseAutomaticToolTip(null, null);
	}

	// MUX Reference: ToolTipService_Partial.cpp IsSpecialKey (line 933).
	internal static bool IsSpecialKey(global::Windows.System.VirtualKey key)
	{
		switch (key)
		{
			case global::Windows.System.VirtualKey.Menu:
			case global::Windows.System.VirtualKey.Back:
			case global::Windows.System.VirtualKey.Delete:
			case global::Windows.System.VirtualKey.Down:
			case global::Windows.System.VirtualKey.End:
			case global::Windows.System.VirtualKey.Home:
			case global::Windows.System.VirtualKey.Insert:
			case global::Windows.System.VirtualKey.Left:
			case global::Windows.System.VirtualKey.PageDown:
			case global::Windows.System.VirtualKey.PageUp:
			case global::Windows.System.VirtualKey.Right:
			case global::Windows.System.VirtualKey.Space:
			case global::Windows.System.VirtualKey.Up:
				return true;
			default:
				return false;
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp GetOwner (line 960).
	internal static DependencyObject? GetOwner()
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();
		return pToolTipServiceMetadataNoRef.m_tpOwner;
	}

	// MUX Reference: ToolTipService_Partial.cpp AddToNestedOwners (line 979).
	// Add current owner to list of nested owners, sorted by ancestry. The highest ancestor
	// is at the end of the list.
	internal static void AddToNestedOwners(DependencyObject pOwner)
	{
		if (pOwner is null)
		{
			return;
		}

		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_isRemovingFromNestedOwners ||
			pToolTipServiceMetadataNoRef.m_isPurgingInvalidNestedOwners)
		{
			pToolTipServiceMetadataNoRef.m_objectsToAdd.Add(new WeakReference(pOwner));
			return;
		}

		try
		{
			pToolTipServiceMetadataNoRef.m_isAddingToNestedOwners = true;
			pToolTipServiceMetadataNoRef.EnsureNestedOwnersInstance();

			LinkedListNode<WeakReference>? insertionIndex = pToolTipServiceMetadataNoRef.m_nestedOwners!.First;

			// Don't add if already in the list
			for (var it = pToolTipServiceMetadataNoRef.m_nestedOwners.First; it is not null; it = it.Next)
			{
				var spCurrentDO = it.Value.Target as DependencyObject;
				if (spCurrentDO is not null && ReferenceEquals(pOwner, spCurrentDO))
				{
					return;
				}
			}

			// Add to list, which is increasingly sorted by ancestry
			for (var it = pToolTipServiceMetadataNoRef.m_nestedOwners.First; it is not null; it = it.Next)
			{
				var spCurrentDO = it.Value.Target as DependencyObject;
				if (spCurrentDO is not null)
				{
					var spContainer = GetContainerFromOwner(pOwner);
					var spCurrentContainer = GetContainerFromOwner(spCurrentDO);

					if (spCurrentContainer is not null && spContainer is not null &&
						spCurrentContainer.IsAncestorOf(spContainer))
					{
						// Found insertion point
						insertionIndex = it;
						break;
					}
				}
			}

			var newRef = new WeakReference(pOwner);
			if (insertionIndex is not null)
			{
				pToolTipServiceMetadataNoRef.m_nestedOwners.AddBefore(insertionIndex, new LinkedListNode<WeakReference>(newRef));
			}
			else
			{
				pToolTipServiceMetadataNoRef.m_nestedOwners.AddLast(newRef);
			}
			pToolTipServiceMetadataNoRef.m_isAddingToNestedOwners = false;
			RunPendingOwnerListOperations(pToolTipServiceMetadataNoRef);
		}
		finally
		{
			pToolTipServiceMetadataNoRef.m_isAddingToNestedOwners = false;
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp IsToolTipInSafeZone (line 1062).
	// The point is in safe zone if the point is in bounds of owner or tooltip, or if it's within the bounds of the convex
	// hull created by the bounds of the owner plus tooltip.
	internal static bool IsToolTipInSafeZone(Point point, Rect ownerBounds, Rect toolTipBounds)
	{
		if (IsPointInRect(point, ownerBounds) || IsPointInRect(point, toolTipBounds))
		{
			return true;
		}

		Point[] polygonPoints = new[]
		{
			new Point(ownerBounds.Left, ownerBounds.Top),
			new Point(ownerBounds.Left, ownerBounds.Bottom),
			new Point(ownerBounds.Right, ownerBounds.Bottom),
			new Point(ownerBounds.Right, ownerBounds.Top),
			new Point(toolTipBounds.Left, toolTipBounds.Top),
			new Point(toolTipBounds.Left, toolTipBounds.Bottom),
			new Point(toolTipBounds.Right, toolTipBounds.Bottom),
			new Point(toolTipBounds.Right, toolTipBounds.Top),
		};

		// It's ok to pass in the same point buffer for input and output; ComputeConvexHull
		// returns the hull in-place.
		var hull = ComputeConvexHull(polygonPoints);

		var testPoint = new Point(point.X, point.Y);
		return IsPointInsidePolygon(testPoint, hull);
	}

	// Andrew's monotone chain convex-hull algorithm. Returns the hull points in
	// counter-clockwise order. C++ uses the engine's ComputeConvexHull<XPOINTF>; this is
	// the equivalent in pure C#.
	private static Point[] ComputeConvexHull(Point[] points)
	{
		int n = points.Length;
		if (n < 3)
		{
			return (Point[])points.Clone();
		}

		// Sort by X, then Y.
		var sorted = (Point[])points.Clone();
		global::System.Array.Sort(sorted, (a, b) =>
		{
			int cmp = a.X.CompareTo(b.X);
			return cmp != 0 ? cmp : a.Y.CompareTo(b.Y);
		});

		var hull = new Point[2 * n];
		int k = 0;

		// Build lower hull.
		for (int i = 0; i < n; i++)
		{
			while (k >= 2 && Cross(hull[k - 2], hull[k - 1], sorted[i]) <= 0)
			{
				k--;
			}
			hull[k++] = sorted[i];
		}

		// Build upper hull.
		int t = k + 1;
		for (int i = n - 2; i >= 0; i--)
		{
			while (k >= t && Cross(hull[k - 2], hull[k - 1], sorted[i]) <= 0)
			{
				k--;
			}
			hull[k++] = sorted[i];
		}

		var result = new Point[k - 1];
		global::System.Array.Copy(hull, result, k - 1);
		return result;
	}

	// 2D cross product of OA and OB vectors.
	private static double Cross(Point O, Point A, Point B)
	{
		return (A.X - O.X) * (B.Y - O.Y) - (A.Y - O.Y) * (B.X - O.X);
	}

	// Ray-casting point-in-polygon test.
	private static bool IsPointInsidePolygon(Point point, Point[] polygon)
	{
		int count = polygon.Length;
		if (count < 3)
		{
			return false;
		}

		bool inside = false;
		for (int i = 0, j = count - 1; i < count; j = i++)
		{
			if (((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y)) &&
				(point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / (polygon[j].Y - polygon[i].Y) + polygon[i].X))
			{
				inside = !inside;
			}
		}

		return inside;
	}

	// MUX Reference: ToolTipService_Partial.cpp IsPointInRect (line 1101).
	internal static bool IsPointInRect(Point point, Rect rect)
	{
		return (point.Y >= rect.Top && point.Y <= rect.Bottom && point.X >= rect.Left && point.X <= rect.Right);
	}

	// MUX Reference: ToolTipService_Partial.cpp GetGlobalBoundsLogical (line 1108).
	internal static Rect GetGlobalBoundsLogical(UIElement element)
	{
		// C++ delegates to CUIElement::GetGlobalBoundsLogical; the Uno equivalent is
		// TransformToVisual(null) of the element's local bounds.
		var local = new Rect(0, 0, (element as FrameworkElement)?.ActualWidth ?? 0, (element as FrameworkElement)?.ActualHeight ?? 0);
		return element.TransformToVisual(null).TransformBounds(local);
	}

	// MUX Reference: ToolTipService_Partial.cpp RemoveFromNestedOwners (line 1116).
	// If a nested owner has been removed from the visual tree or made invisible, remove it
	// from the list, because it can no longer display tooltips.
	internal static void RemoveFromNestedOwners(DependencyObject pOwner)
	{
		global::System.Diagnostics.Debug.Assert(pOwner is not null);
		if (pOwner is null)
		{
			return;
		}

		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_isAddingToNestedOwners ||
			pToolTipServiceMetadataNoRef.m_isPurgingInvalidNestedOwners)
		{
			pToolTipServiceMetadataNoRef.m_objectsToRemove.Add(new WeakReference(pOwner));
			return;
		}

		try
		{
			pToolTipServiceMetadataNoRef.m_isRemovingFromNestedOwners = true;

			// Remove from list of nested owners
			if (pToolTipServiceMetadataNoRef.m_nestedOwners is not null)
			{
				var it = pToolTipServiceMetadataNoRef.m_nestedOwners.First;
				while (it is not null)
				{
					var spCurrentDO = it.Value.Target as DependencyObject;
					if (spCurrentDO is not null)
					{
						if (ReferenceEquals(pOwner, spCurrentDO))
						{
							pToolTipServiceMetadataNoRef.DeleteElementFromNestedOwners(it);
							break;
						}
					}

					it = it.Next;
				}
			}

			pToolTipServiceMetadataNoRef.m_isRemovingFromNestedOwners = false;
			RunPendingOwnerListOperations(pToolTipServiceMetadataNoRef);
		}
		finally
		{
			pToolTipServiceMetadataNoRef.m_isRemovingFromNestedOwners = false;
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp PurgeInvalidNestedOwners (line 1177).
	internal static void PurgeInvalidNestedOwners()
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		try
		{
			bool isListOperationInProgress =
				pToolTipServiceMetadataNoRef.m_isAddingToNestedOwners ||
				pToolTipServiceMetadataNoRef.m_isRemovingFromNestedOwners;

			pToolTipServiceMetadataNoRef.m_isPurgingInvalidNestedOwners = true;

			if (pToolTipServiceMetadataNoRef.m_nestedOwners is null)
			{
				return;
			}

			var it = pToolTipServiceMetadataNoRef.m_nestedOwners.First;
			while (it is not null)
			{
				var spCurrentDO = it.Value.Target as DependencyObject;

				bool shouldErase = false;

				if (spCurrentDO is null)
				{
					shouldErase = true;
				}
				else
				{
					var spCurrentContainer = GetContainerFromOwner(spCurrentDO);
					bool bIsHitTestVisible = spCurrentContainer?.IsHitTestVisible ?? false;

					shouldErase = !bIsHitTestVisible || spCurrentContainer is null || !spCurrentContainer.IsLoaded;
				}

				if (shouldErase)
				{
					if (isListOperationInProgress)
					{
						pToolTipServiceMetadataNoRef.m_objectsToRemove.Add(it.Value);
						it = it.Next;
					}
					else
					{
						it = pToolTipServiceMetadataNoRef.DeleteElementFromNestedOwners(it);
					}
				}
				else
				{
					it = it.Next;
				}
			}

			if (!isListOperationInProgress)
			{
				pToolTipServiceMetadataNoRef.m_isPurgingInvalidNestedOwners = false;
				RunPendingOwnerListOperations(pToolTipServiceMetadataNoRef);
			}
		}
		finally
		{
			pToolTipServiceMetadataNoRef.m_isPurgingInvalidNestedOwners = false;
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp RunPendingOwnerListOperations (line 1255).
	private static void RunPendingOwnerListOperations(ToolTipServiceMetadata pToolTipServiceMetadataNoRef)
	{
		while (pToolTipServiceMetadataNoRef.m_objectsToAdd.Count > 0)
		{
			// Cache the list so we don't modify it while iterating over it.
			var objectsToAdd = new global::System.Collections.Generic.List<WeakReference>(pToolTipServiceMetadataNoRef.m_objectsToAdd);
			pToolTipServiceMetadataNoRef.m_objectsToAdd.Clear();

			foreach (var objectToAdd in objectsToAdd)
			{
				var objectToAddDO = objectToAdd.Target as DependencyObject;
				if (objectToAddDO is not null)
				{
					AddToNestedOwners(objectToAddDO);
				}
			}
		}

		while (pToolTipServiceMetadataNoRef.m_objectsToRemove.Count > 0)
		{
			// Cache the list so we don't modify it while iterating over it.
			var objectsToRemove = new global::System.Collections.Generic.List<WeakReference>(pToolTipServiceMetadataNoRef.m_objectsToRemove);
			pToolTipServiceMetadataNoRef.m_objectsToRemove.Clear();

			foreach (var objectToRemove in objectsToRemove)
			{
				var objectToRemoveDO = objectToRemove.Target as DependencyObject;
				if (objectToRemoveDO is not null)
				{
					RemoveFromNestedOwners(objectToRemoveDO);
				}
			}
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp GetFirstNestedOwner (line 1317).
	internal static DependencyObject? GetFirstNestedOwner()
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_nestedOwners is not null)
		{
			var it = pToolTipServiceMetadataNoRef.m_nestedOwners.First;
			while (it is not null)
			{
				var spCurrentAsDO = it.Value.Target as DependencyObject;
				if (spCurrentAsDO is not null)
				{
					return spCurrentAsDO;
				}

				it = it.Next;
			}
		}

		return null;
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerPointerEntered (line 1354).
	private static void OnOwnerPointerEntered(object sender, Input.PointerRoutedEventArgs e)
	{
		var spSenderAsDO = sender as DependencyObject;
		if (spSenderAsDO is null)
		{
			return;
		}

		var spToolTipObject = GetActualToolTipObject(spSenderAsDO);
		if (spToolTipObject is null)
		{
			return;
		}

		bool isAlreadyOpen = spToolTipObject.IsOpen;

		if (!isAlreadyOpen)
		{
			var spPointerPoint = e.GetCurrentPoint(null);
			if (spPointerPoint is null)
			{
				return;
			}

			var pointerDeviceType = spPointerPoint.PointerDeviceType;
			bool isInPointerMode = pointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch;

			s_lastPointerEnteredPoint = spPointerPoint.Position;

			// Add to list of nested owners
			AddToNestedOwners(spSenderAsDO);
			var spOriginalSource = e.OriginalSource;

			OnOwnerEnterInternal(
				spSenderAsDO,
				spOriginalSource,
				isInPointerMode ? AutomaticToolTipInputMode.Touch : AutomaticToolTipInputMode.Mouse);
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerPointerExitedOrLostOrCanceled (line 1423).
	// Used to handle PointerExited, PointerCaptureLost, and PointerCanceled on a ToolTip's owner FrameworkElement.
	private static void OnOwnerPointerExitedOrLostOrCanceled(object sender, Input.PointerRoutedEventArgs e)
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		// Opened ToolTip will be kept and will be closed when Pointer is out side of safe zone
		// Not opened ToolTip will be cancelled
		if (pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is null)
		{
			// Cancel the ToolTip if it had not been opened yet
			if (pToolTipServiceMetadataNoRef.m_tpOpenTimer is not null)
			{
				CancelAutomaticToolTip();
			}
		}
		else
		{
			var owner = sender as DependencyObject;

			if (owner is not null)
			{
				pToolTipServiceMetadataNoRef.m_lastToolTipOwnerInSafeZone = new WeakReference(owner);
			}

			// TODO Uno (Phase 6): WinUI keeps the ToolTip open and waits for the safe-zone
			// timer to determine whether the pointer has truly left the owner+tooltip
			// convex hull. Until SafeZoneCheckTimer (DispatcherQueueTimer) is wired on
			// Skia, close immediately so PointerExited / PointerCanceled actually dismiss
			// the tooltip (matches the cross-platform Uno UX expected by Given_ToolTip
			// runtime tests).
			CancelAutomaticToolTip();
		}
	}

	// Uno-specific UX (no direct C++ counterpart): a click on a Button-based owner
	// dismisses the open tooltip. Mirrors the cross-platform behavior in
	// ToolTipService.cs (gated #if !__SKIA__).
	private static void OnOwnerPointerPressed(object sender, Input.PointerRoutedEventArgs e)
	{
		if (sender is FrameworkElement owner && GetActualToolTipObject(owner) is { } toolTip)
		{
			if (e.GetCurrentPoint(owner).Properties.IsLeftButtonPressed)
			{
				toolTip.IsOpen = false;
			}
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp OnRootVisualPointerMoved (line 1483).
	private static void OnRootVisualPointerMoved(object sender, Input.PointerRoutedEventArgs e)
	{
		// If the pointer is over a nested owner, and there is no current
		// owner, notify the next nested owner that it is current owner.
		// This supports the pointer coming back to an ancestor after
		// it enters and leaves a nested descendant element. Although
		// the pointer never left the ancestor in this scenario, the ancestor
		// needs to re-open its tooltip, because the pointer came back to the
		// ancestor from the nested descendant.
		var spOwner = GetOwner();
		if (spOwner is null)
		{
			var spPointerPoint = e.GetCurrentPoint(null);
			if (spPointerPoint is null)
			{
				return;
			}
			var position = spPointerPoint.Position;

			var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

			if (sender is UIElement rootElement)
			{
				var elementsAtPosition = Microsoft.UI.Xaml.Media.VisualTreeHelper.FindElementsInHostCoordinates(position, rootElement);

				PurgeInvalidNestedOwners();

				if (pToolTipServiceMetadataNoRef.m_nestedOwners is not null)
				{
					var nestedOwnersIterator = pToolTipServiceMetadataNoRef.m_nestedOwners.First;
					while (nestedOwnersIterator is not null)
					{
						var nestedOwner = nestedOwnersIterator.Value.Target as DependencyObject;
						if (nestedOwner is null)
						{
							var next = pToolTipServiceMetadataNoRef.DeleteElementFromNestedOwners(nestedOwnersIterator);
							nestedOwnersIterator = next;
							continue;
						}

						bool ownerIsAtPosition = false;
						foreach (var elementAtPosition in elementsAtPosition)
						{
							if (ReferenceEquals(elementAtPosition, nestedOwner))
							{
								ownerIsAtPosition = true;
								break;
							}
						}

						if (!ownerIsAtPosition)
						{
							var next = pToolTipServiceMetadataNoRef.DeleteElementFromNestedOwners(nestedOwnersIterator);
							nestedOwnersIterator = next;
							continue;
						}

						var pointerDeviceType = spPointerPoint.PointerDeviceType;
						bool isInPointerMode = pointerDeviceType == Microsoft.UI.Input.PointerDeviceType.Touch;

						var spContainer = GetContainerFromOwner(nestedOwner);

						OnOwnerEnterInternal(
							nestedOwner,
							spContainer,
							isInPointerMode ? AutomaticToolTipInputMode.Touch : AutomaticToolTipInputMode.Mouse);

						break;
					}
				}
			}
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp OnRootVisualSizeChanged (line 1590).
	// Used to handle SizeChanged on the application root visual FrameworkElement.
	private static void OnRootVisualSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_tpCurrentToolTip is { } currentToolTip)
		{
			currentToolTip.OnRootVisualSizeChanged();
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp OnToolTipChanged (line 1608).
	// The cross-platform OnToolTipChanged in ToolTipService.mux.cs (gated #if !__SKIA__)
	// dispatches the same way.
	private static void OnToolTipChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		if (sender is FrameworkElement senderAsFe)
		{
			bool isKeyboardAcceleratorToolTip = args.Property == KeyboardAcceleratorToolTipProperty;
			if (args.OldValue is not UnsetValue && args.OldValue is not null)
			{
				UnregisterToolTip(sender, senderAsFe, isKeyboardAcceleratorToolTip);
			}

			if (args.NewValue is { } toolTip)
			{
				RegisterToolTip(sender, senderAsFe, toolTip, isKeyboardAcceleratorToolTip);
			}
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerGotFocus (line 1635).
	private static void OnOwnerGotFocus(object sender, RoutedEventArgs e)
	{
		var spSenderAsDO = sender as DependencyObject;
		if (spSenderAsDO is null)
		{
			return;
		}

		var spToolTipObject = GetActualToolTipObject(spSenderAsDO);
		if (spToolTipObject is null)
		{
			return;
		}

		bool isAlreadyOpen = spToolTipObject.IsOpen;

		if (!isAlreadyOpen)
		{
			var contentRoot = global::Uno.UI.Xaml.Core.VisualTree.GetContentRootForElement(spSenderAsDO);
			var focusState = contentRoot?.FocusManager.GetRealFocusStateForFocusedElement();

			// If the source of a programmatic focus was UIA, we should show the tooltip:
			bool shouldShowToolTip = (focusState == FocusState.Keyboard) ||
									 (focusState == FocusState.Programmatic
										&& contentRoot?.InputManager?.GetWasUIAFocusSetSinceLastInput() == true);

			if (shouldShowToolTip)
			{
				var spOriginalSource = e.OriginalSource;
				OnOwnerEnterInternal(spSenderAsDO, spOriginalSource, AutomaticToolTipInputMode.Keyboard);
			}
		}
	}

	// MUX Reference: ToolTipService_Partial.cpp OnOwnerLostFocus (line 1696).
	// Used to handle LostFocus on a ToolTip's owner FrameworkElement.
	private static void OnOwnerLostFocus(object sender, RoutedEventArgs e)
	{
		OnOwnerLeaveInternal(sender);
	}

	// MUX Reference: ToolTipService_Partial.cpp GetInitialShowDelay (line 1744).
	// For a given input mode, returns the initial delay before the ToolTip shows according to spec.
	//
	// There are normal and reshow timers.  The normal timer is used when first opening a ToolTip.
	// The reshow timer is used when a previous ToolTip has been shown within BETWEEN_SHOW_DELAY_MS
	// of invoking this one.
	//
	//          Touch   Mouse   Keyboard
	//  --------------------------------
	//  Normal     1x      2x         2x
	//  Reshow      0    1.5x         2x
	//
	//  where x = SPI_GETMOUSEHOVERTIME (400 ms by default)
	private static TimeSpan GetInitialShowDelay(AutomaticToolTipInputMode mode, bool isReshow)
	{
		// TODO Uno: SystemParametersInfo SPI_GETMOUSEHOVERTIME is Win32-only. Phase 6 polish
		// can wire platform-specific APIs; for now we use the WinUI fallback constant.
		long ulSPIGetMouseHoverTimeMS = ToolTipServiceConstants.DEFAULT_SPI_GETMOUSEHOVERTIME;

		long ulSPIGetMouseHoverTimeTicks = ulSPIGetMouseHoverTimeMS * ToolTipServiceConstants.TICKS_PER_MILLISECOND;

		switch (mode)
		{
			case AutomaticToolTipInputMode.Touch:
				ulSPIGetMouseHoverTimeTicks *= isReshow ? 0 : 1;
				break;
			case AutomaticToolTipInputMode.Mouse:
				ulSPIGetMouseHoverTimeTicks *= (long)(isReshow ? 1.5 : 2);
				break;
			case AutomaticToolTipInputMode.Keyboard:
				ulSPIGetMouseHoverTimeTicks *= 2;
				break;
		}

		return TimeSpan.FromTicks(ulSPIGetMouseHoverTimeTicks);
	}

	// MUX Reference: ToolTipService_Partial.cpp GetContainerFromOwner (line 1790).
	private static FrameworkElement? GetContainerFromOwner(DependencyObject owner)
	{
		var toolTipObject = GetActualToolTipObject(owner);

		if (toolTipObject is not null)
		{
			return toolTipObject.GetContainer();
		}

		return null;
	}

	private static void OnPlacementChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
	{
		// TODO Uno: Phase 3 closeout will mirror the cross-platform Placement-update behavior
		// (propagate to the registered ToolTip's Placement).
	}

#pragma warning restore IDE0060
#pragma warning restore IDE0051
}

#endif // __SKIA__