// MUX Reference dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp, tag 5f9e85113
// Contains ported portions of dxaml\xcp\dxaml\lib\ToolTipService_Partial.cpp
//
// NOTE: ToolTipServiceMetadata + static fields live in ToolTipService.partial.h.mux.cs
// (port of ToolTipService_Partial.h). This file ports method bodies in the order
// they appear in ToolTipService_Partial.cpp.

#if __SKIA__

#nullable enable

using System;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

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

		// TODO Uno (Phase 6): port StartSafeZoneCheckTimer.
#if false
		IFC(StartSafeZoneCheckTimer(pToolTipServiceMetadataNoRef));
#endif
	}

	// MUX Reference: ToolTipService_Partial.cpp CloseAutomaticToolTip (line 496).
	private static void CloseAutomaticToolTip(object? pUnused1, object? pUnused2)
	{
		var pToolTipServiceMetadataNoRef = GetToolTipServiceMetadata();

		if (pToolTipServiceMetadataNoRef.m_tpCloseTimer is not null)
		{
			pToolTipServiceMetadataNoRef.m_tpCloseTimer.Stop();
		}

		// TODO Uno (Phase 6): SafeZoneCheckTimer (DispatcherQueueTimer) not yet wired.
#if false
		if (pToolTipServiceMetadataNoRef->m_tpSafeZoneCheckTimer)
		{
			IFC(pToolTipServiceMetadataNoRef->m_tpSafeZoneCheckTimer->Stop());
		}
#endif

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
	// Phase 4 closeout / Phase 6 will port the OnRootVisualPointerMoved + OnRootVisualSizeChanged
	// handler attachment. For now this is a no-op stub so OpenPopup compiles.
	internal static void EnsureHandlersAttachedToRootElement(XamlRoot? visualTree)
	{
		// TODO Uno: Phase 4 closeout will port EnsureHandlersAttachedToRootElement.
	}

	// MUX Reference: ToolTipService_Partial.cpp AddToNestedOwners (line 979).
	// Phase 4 closeout will port the full nested-owner list management. For now this stub
	// is sufficient because tooltips don't nest in the common Slider / Button case.
	private static void AddToNestedOwners(DependencyObject pOwner)
	{
		// TODO Uno: Phase 4 closeout will port AddToNestedOwners faithfully.
	}

	// MUX Reference: ToolTipService_Partial.cpp RemoveFromNestedOwners (line 1116).
	private static void RemoveFromNestedOwners(DependencyObject pOwner)
	{
		// TODO Uno: Phase 4 closeout will port RemoveFromNestedOwners faithfully.
	}

	// MUX Reference: ToolTipService_Partial.cpp PurgeInvalidNestedOwners (line 1177).
	internal static void PurgeInvalidNestedOwners()
	{
		// TODO Uno: Phase 4 closeout will port PurgeInvalidNestedOwners faithfully.
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