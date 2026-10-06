// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference ListViewBaseItem_Partial.cpp, tag winui3/release/2.5.1

#nullable enable

using System;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.UI;
using Uno.UI.Xaml;
using Windows.Foundation;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBaseItem
{
	// TODO Uno: Original C++ destructor cleanup (stop m_tpTouchTimer, detach its Tick, UnregisterNoHintVisualStateHandlers).
	// Uno does not support cleanup via finalizers; LeaveImpl stops the timer.

	// Get the parent ListViewBase of this item.  Note that we retrieve the parent
	// via GetParentSelector whose relationship is setup in PrepareContainer and
	// torn down in ClearContainer so it's only valid within that range.  This will
	// obviously be NULL if the parent ItemsControl does not implement
	// IListViewBase.
	internal ListViewBase? GetParentListView() => Selector as ListViewBase;

	// MUX Reference ListViewBaseItem_Partial.cpp, lines 146-171 (Initialize): WinUI subscribes to Loaded; Uno overrides OnLoaded.

	// MUX Reference ListViewBaseItem_Partial.cpp, lines 173-180
	// Uno-specific: called by ListViewBase.OnItemContainerDragStarting when Uno's touch drag starts the item drag.
	internal void OnTouchDragStarted()
	{
		// On a touch drag, we don't get Holding Completed event. So, clearing the m_isHolding flag & destroying holding visual here.
		m_isHolding = false;
		DestroyHoldingVisual();

		// TODO Uno: the rest of OnTouchDragStarted / StartDragIfEnabled (LBI:182-234) - AutomaticDragHelper and DragDropVisual not ported.
	}

	// Called when the element leaves the tree.
	internal override void LeaveImpl(LeaveParams @params)
	{
		base.LeaveImpl(@params);

		StopTouchTimer();
	}

	// Called when the ListViewBaseItem has been constructed and added to the visual
	// tree.
	private protected override void OnLoaded()
	{
		base.OnLoaded();

		ChangeVisualState(false);
	}

	// Apply a template to the ListViewBaseItem.
	protected override void OnApplyTemplate()
	{
		// TODO Uno: UnregisterNoHintVisualStateHandlers - hint storyboards not ported.

		base.OnApplyTemplate();

		// TODO Uno: RegisterNoHintVisualStateHandlers - live for Grid-rooted templates (e.g. ListViewItemExpanded).

		// Grab the ContentContainer template part.
		m_tpContentContainer = GetTemplateChild(LISTVIEWBASEITEM_CONTENT_CONTAINER_PART_NAME) as UIElement;

		m_tpCheckboxContainer = GetTemplateChild(LISTVIEWBASEITEM_CHECKBOX_CONTAINER_PART_NAME) as UIElement;

		// Set up the chrome, if we have it.
		{
			var pChrome = GetItemChrome();

			// WinUI quirk: linking runs before SetGridViewItemChrome, which unlinks the old chrome even when it is the same presenter.
			pChrome?.SetChromedListViewBaseItem(this);

			// Update the child (refs get taken care of).
			SetGridViewItemChrome(pChrome);
		}

		// Sync the logical and visual states of the control
		ChangeVisualState(false);
	}

	// TODO Uno: AttachTransitionCompleted / RegisterNoHintVisualStateHandlers / UnregisterNoHintVisualStateHandlers (LBI:313-405)
	// are live for Grid-rooted templates (e.g. ListViewItemExpanded) but not ported.

	// Called when the user presses a pointer down over the ListViewBaseItem.
	protected override void OnPointerPressed(PointerRoutedEventArgs pArgs)
	{
		bool isVisualStateChangePending = false;

		// Uno-specific: read Handled before the base call; WinUI reads it after (SelectorItem sets it unconditionally).
		bool isHandled = pArgs.Handled;

		base.OnPointerPressed(pArgs);

		// TODO: We may want to move this functionality into an OnPointerPressed/Released
		// handler.  It would be weird if mouse raised ItemClick for
		// OnMouseLeftButtonDown but touch only raised ItemClick for "PointerReleased"
		// (depending on whether we get an "OnPointerPressed" call or just an
		// "OnTap/OnHold" call for the self-reveal functionality we need to
		// implement for Swipe).

		if (!isHandled)
		{
			var spPointer = pArgs.Pointer;
			var pointerDeviceType = spPointer.PointerDeviceType;
			var spListView = GetParentListView();
			var spPointerPoint = pArgs.GetCurrentPoint(this);

			// TODO: Ignore the PointerPressed if we've already "captured" input
			// from another device (via InteractionManager)

			// Check if this is a mouse button down or a finger down.
			if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
			{
				// Mouse button down.
				var spPointerProperties = spPointerPoint.Properties;
				bool isLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;
				bool isRightButtonPressed = spPointerProperties.IsRightButtonPressed;

				// If the left mouse button was the one pressed...
				if (!m_isLeftButtonPressed && isLeftButtonPressed)
				{
					// Start listening for a mouse drag gesture if dragging is
					// enabled on our parent.
					m_isLeftButtonPressed = true;
					isVisualStateChangePending = true;
					isHandled = true;

					// TODO Uno: EnqueuePendingTapInteractionForPointerId - click stays on SelectorItem.

					// TODO Uno: BeginCheckingForMouseDrag (LBI:469-484) - mouse drag threshold not ported.
				}

				// If the right mouse button was the one pressed...
				if (!m_isRightButtonPressed && isRightButtonPressed)
				{
					m_isRightButtonPressed = true;
					isVisualStateChangePending = true;

					//With the new changes to the pen input model we want to initaite drag and drops when the pen barrel button is pressed.
					//This is presented to us in the for of isRightButtonPressed.
					if (pointerDeviceType == PointerDeviceType.Pen)
					{
						// Start listening for a mouse drag gesture if dragging is
						// enabled on our parent.
						m_isRightButtonPressed = true;
						isVisualStateChangePending = true;

						// TODO Uno: EnqueuePendingTapInteractionForPointerId + BeginCheckingForMouseDrag (LBI:502-519).
					}
				}
			}
			else if (pointerDeviceType == PointerDeviceType.Touch)
			{
				bool shouldSwipeContinue = true;

				isHandled = true;
				// TODO Uno: EnqueuePendingTapInteractionForPointerId - click stays on SelectorItem.

				// Clear any pointer over visuals.
				isVisualStateChangePending |= m_shouldEnterPointerOver;
				m_shouldEnterPointerOver = false;

				m_lastTouchDownPosition = spPointerPoint.Position;

				if (m_tpLastTouchPressedArgs is not null)
				{
					var spSwipePointer = m_tpLastTouchPressedArgs.Pointer;

					shouldSwipeContinue = spSwipePointer.PointerId == spPointer.PointerId;
				}

				// Touch down.
				m_tpLastTouchPressedArgs = pArgs;

				if (shouldSwipeContinue)
				{
					// On phone the checkbox is built into the ListViewBaseItem as a Path.
					// When that checkbox is pressed the phone PointerDown Tilt animation
					// must only apply to it. We determine using the RoutedEvent's OriginalSource
					// if the PointerPressed event occurred from within the Checkbox templatepart here.
					// TODO Uno: IsOverCheckbox (phone-only template part) not ported.
					bool isOverCheckbox = false;

					if (isOverCheckbox)
					{
						isVisualStateChangePending |= !m_inCheckboxPressedForTouch;
						m_inCheckboxPressedForTouch = true;

						isVisualStateChangePending |= m_inPressedForTouch;
						m_inPressedForTouch = false;
					}
					else
					{
						// start the touch pressed timer
						// this timer delays going into the Pressed state
						StartTouchTimer(TouchTimerState.PressedTimer);

						isVisualStateChangePending |= m_inCheckboxPressedForTouch;
						m_inCheckboxPressedForTouch = false;
					}

					// TODO Uno: IsDraggable + AutomaticDragHelper setup (LBI:574-605) not ported.
				}
				else
				{
					isVisualStateChangePending |= m_inPressedForTouch;
					isVisualStateChangePending |= m_inCheckboxPressedForTouch;
					m_inPressedForTouch = false;
					m_inCheckboxPressedForTouch = false;
				}
			}

			// Uno-specific: never clear the Handled flag SelectorItem set.
			if (isHandled)
			{
				pArgs.Handled = true;
			}
		}

		if (isVisualStateChangePending)
		{
			ChangeVisualState(true);
		}
	}

	// Called when the user holds a pointer down over the
	// ListViewBaseItem (Holding gesture).
	protected override void OnHolding(HoldingRoutedEventArgs pArgs)
	{
		bool wasHolding = m_isHolding;

		try
		{
			m_isHolding = false;

			base.OnHolding(pArgs);

			var pointerDeviceType = pArgs.PointerDeviceType;

			if (pointerDeviceType == PointerDeviceType.Touch)
			{
				var holdingState = pArgs.HoldingState;
				m_isHolding = (holdingState == HoldingState.Started);

				// TODO Uno: AutomaticDragHelper.HandleHoldingEventArgs - no AutomaticDragHelper.

				// Holding gesture will show drag visual
				{
					var spListView = GetParentListView();

					if (spListView is not null)
					{
						var canDragItems = spListView.CanDragItems;
						var canReorderItems = spListView.CanReorderItems;

						if (canDragItems || canReorderItems)
						{
							if (m_isHolding)
							{
								spListView.SetIsHolding(true);
								spListView.SetHoldingItem(this);

								// Show the drag visual LTE for item on which press and hold detected.
								// We don't need the LTE to escape clip for reordering, since scaling up is only needed for dragging.
								if (!canReorderItems)
								{
									CreateHoldingVisual();
									TransformHoldingVisual(pArgs);
								}
							}
							else
							{
								spListView.SetIsHolding(false);
								spListView.ClearHoldingItem(this);
								DestroyHoldingVisual();
							}

							spListView.ChangeSelectorItemsVisualState(true);
						}
					}
				}
			}
		}
		finally
		{
			if (wasHolding != m_isHolding)
			{
				ChangeVisualState(true);
			}
		}
	}

	// Called when the user releases a pointer over the ListViewBaseItem.
	protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
	{
		bool isVisualStateChangePending = false;

		// Clearing the holding visual on pointer released.
		m_isHolding = false;
		DestroyHoldingVisual();

		base.OnPointerReleased(pArgs);

		// TODO: Ignore the PointerReleased if we've already "captured" input
		// from another device (via InteractionManager)

		bool isHandled = pArgs.Handled;

		var spPointer = pArgs.Pointer;
		var pointerDeviceType = spPointer.PointerDeviceType;

		var gestureFollowing = pArgs.GestureFollowing;
		bool rightTappedPending = gestureFollowing == GestureModes.RightTapped;

		// TODO Uno: selection gesture trace id (m_itemSelectionGestureId) not ported.

		// Check if this is a mouse button up
		if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
		{
			var spPointerPoint = pArgs.GetCurrentPoint(this);
			var spPointerProperties = spPointerPoint.Properties;
			bool isLeftButtonPressed = spPointerProperties.IsLeftButtonPressed;
			bool isRightButtonPressed = spPointerProperties.IsRightButtonPressed;

			// if the mouse left button was the one released...
			if (m_isLeftButtonPressed && !isLeftButtonPressed)
			{
				m_isLeftButtonPressed = false;
				isVisualStateChangePending = true;

				if (!isHandled && !rightTappedPending)
				{
					// TODO Uno: DoPendingTapInteractionForPointerId - click/selection stay on SelectorItem.
				}
			}

			// if the mouse right button was the one released...
			if (m_isRightButtonPressed && !isRightButtonPressed)
			{
				m_isRightButtonPressed = false;
				isVisualStateChangePending = true;
			}

			// Terminate any mouse drag gesture tracking.
			// TODO Uno: StopCheckingForMouseDrag - mouse drag threshold not ported.
		}
		else if (pointerDeviceType == PointerDeviceType.Touch)
		{
			// no need for a quirk here since m_touchTimerState is only set to
			// PressedTimer in Threshold and beyond
			if (m_touchTimerState == TouchTimerState.PressedTimer)
			{
				m_inPressedForTouch = true;

				// start the early release timer
				StartTouchTimer(TouchTimerState.ReleasedTimer);
			}
			else
			{
				// Touch up.
				m_inPressedForTouch = false;
			}

			m_inCheckboxPressedForTouch = false;

			m_tpLastTouchPressedArgs = null;

			if (!isHandled &&
				GestureModes.Tapped == gestureFollowing)
			{
				// TODO Uno: DoPendingTapInteractionForPointerId - click/selection stay on SelectorItem.
			}

			// TODO Uno: ReleasePointerCapture(spPointer) - its CaptureLost would cancel SelectorItem's click, which is raised after this handler.

			// TODO Uno: AutomaticDragHelper.HandlePointerReleasedEventArgs not ported.

			isVisualStateChangePending = true;
		}

		pArgs.Handled = isHandled;

		// TODO Uno: CancelPendingTapInteractionForPointerId - no pending tap list.

		if (isVisualStateChangePending)
		{
			ChangeVisualState(true);
		}
	}

	// TODO Uno: EnqueuePendingTapInteractionForPointerId / DoPendingTapInteractionForPointerId /
	// CancelPendingTapInteractionForPointerId (LBI:858-903) - click/selection stay on SelectorItem.

	// Called when a pointer enters a ListViewBaseItem.
	protected override void OnPointerEntered(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerEntered(pArgs);

		// Only update m_shouldEnterPointerOver if the pointer type isn't touch
		var pointerDeviceType = pArgs.Pointer.PointerDeviceType;
		if (pointerDeviceType != PointerDeviceType.Touch)
		{
			m_shouldEnterPointerOver = true;
			ChangeVisualState(true);
		}
	}

	protected override void OnPointerCanceled(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerCanceled(pArgs);

		// TODO Uno: CancelPendingTapInteractionForPointerId - no pending tap list.
	}

	// Called when a pointer leaves a ListViewBaseItem.
	protected override void OnPointerExited(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerExited(pArgs);

		var pointerDeviceType = pArgs.Pointer.PointerDeviceType;

		// TODO Uno: CancelPendingTapInteractionForPointerId - no pending tap list.

		// Clear pointer over regardless of input method
		m_shouldEnterPointerOver = false;

		if (pointerDeviceType != PointerDeviceType.Touch)
		{
			m_isLeftButtonPressed = false;
			m_isRightButtonPressed = false;
			ChangeVisualState(true);
		}
		else
		{
			// no need for a quirk here since m_touchTimerState is only set to
			// a value other than NoTimer in Threshold and beyond (with touch input)
			// if the value is not NoTimer, it means that it could either be Pressed or Released
			// in the case of Pressed, it means that the user started the press and quickly exited the item
			// -> we start the ReleasedTimer to show a PressedState
			// in the case of Released, we do nothing, m_inPressedForTouch is already true
			// -> we wait for the released timer to expire and set it back to false
			if (m_touchTimerState == TouchTimerState.NoTimer)
			{
				m_inPressedForTouch = false;
			}
			else if (m_touchTimerState == TouchTimerState.PressedTimer)
			{
				m_inPressedForTouch = true;

				// start the early release timer
				StartTouchTimer(TouchTimerState.ReleasedTimer);
			}

			// Touch up.
			m_tpLastTouchPressedArgs = null;
			m_inCheckboxPressedForTouch = false;
			ChangeVisualState(true);
		}
	}

	// Called when a pointer moves within a ListViewBaseItem.
	protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)
	{
		base.OnPointerMoved(pArgs);

		bool isHandled = pArgs.Handled;
		var pointerDeviceType = pArgs.Pointer.PointerDeviceType;
		if (!isHandled)
		{
			// Our behavior is different between mouse and touch.
			// It's up to us to detect mouse drag gestures - if we
			// detect one here, tell our parent to start a drag drop.
			if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
			{
				// Desired behavior when a user touches a container that currently
				// has a mouse hover visual is to clear the hover visual, then re-instate
				// the hover visual only when the mouse is moved.
				// We clear m_shouldEnterPointerOver when the item is touched. We re-instate
				// it here.
				if (!m_shouldEnterPointerOver && !m_inPressedForTouch)
				{
					m_shouldEnterPointerOver = true;
					ChangeVisualState(true);
				}

				// TODO Uno: mouse drag detection (LBI:1041-1092: IsInExclusiveInteraction, ShouldStartMouseDrag, TryStartDrag) not ported.
			}
		}
	}

	// MUX Reference ListViewBaseItem_Partial.cpp, lines 1064-1070
	// Uno-specific: runs when Uno's own drag gesture starts the item drag (ListViewBase.OnItemContainerDragStarting).
	internal void OnDragGestureStarting()
	{
		// TODO: We don't get a MouseLeave on this
		// element after a drag and drop.  Manually reset the mouse over
		// state as a workaround. Note that this isn't quite right yet (user
		// might drop really close to the drag start point). Should be fixed
		// during stabilization.
		m_shouldEnterPointerOver = false;
		ChangeVisualState(true);
	}

	// Called when the ListViewBaseItem or its children lose pointer capture.
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
	{
		bool isVisualStateChangePending = false;

		base.OnPointerCaptureLost(pArgs);

		// TODO Uno: CancelPendingTapInteractionForPointerId - no pending tap list.

		var pointerDeviceType = pArgs.Pointer.PointerDeviceType;
		if (pointerDeviceType == PointerDeviceType.Mouse || pointerDeviceType == PointerDeviceType.Pen)
		{
			// We're not necessarily going to get a PointerReleased on capture lost, so reset this flag here.
			if (m_isLeftButtonPressed || m_isRightButtonPressed || m_shouldEnterPointerOver)
			{
				m_isLeftButtonPressed = false;
				m_isRightButtonPressed = false;
				m_shouldEnterPointerOver = false;
				isVisualStateChangePending = true;
			}

		}
		else if (pointerDeviceType == PointerDeviceType.Touch)
		{
			// stop the touch timer
			StopTouchTimer();

			if (m_inPressedForTouch || m_inCheckboxPressedForTouch || m_shouldEnterPointerOver)
			{
				m_inPressedForTouch = false;
				m_inCheckboxPressedForTouch = false;
				m_shouldEnterPointerOver = false;
				isVisualStateChangePending = true;
			}

			// TODO Uno: AutomaticDragHelper.HandlePointerCaptureLostEventArgs not ported.
			m_tpLastTouchPressedArgs = null;
		}

		if (isVisualStateChangePending &&
			!IsProcessingElementEnterLeave)
		{
			ChangeVisualState(true);
		}
	}

	// TODO Uno: DoTapInteraction (LBI:1163-1196) - OnItemPrimary/SecondaryInteractionGesture not ported;
	// click/selection stay on SelectorItem (_canRaiseClickOnPointerRelease, Selector.OnItemClicked).

	// Called when a pointer makes a right-tap gesture on a ListViewBaseItem.
	protected override void OnRightTapped(RightTappedRoutedEventArgs pArgs)
	{
		base.OnRightTapped(pArgs);

		bool isHandled = pArgs.Handled;
		var pointerDeviceType = pArgs.PointerDeviceType;

		// Only do interaction for right click, not press-and-hold (the latter will be done in PointerReleased).
		if (!isHandled && pointerDeviceType != PointerDeviceType.Touch)
		{
			var spListView = GetParentListView();

			if (spListView is not null)
			{
				// TODO Uno: ListViewBase.FocusItem(FocusState.Programmatic, this) not ported.
				pArgs.Handled = isHandled;
			}
		}

		// Touch up.
		m_inPressedForTouch = false;
		m_inCheckboxPressedForTouch = false;
		ChangeVisualState(true);
	}

	// Called when the value of the IsEnabled property changes.
	private protected override void OnIsEnabledChanged(IsEnabledChangedEventArgs pArgs)
	{
		base.OnIsEnabledChanged(pArgs);

		var isEnabled = IsEnabled;
		if (!isEnabled)
		{
			ClearStateFlags();
		}
		ChangeVisualState(true);
	}

	// MUX Reference ListViewBaseItem_Partial.cpp, lines 1252-1271 (OnContextRequestedImpl): ported in SelectorItem.

	// Called when the ListViewBaseItem receives focus.
	protected override void OnGotFocus(RoutedEventArgs pArgs)
	{
		bool notifyParent = false;
		var focusState = FocusState.Unfocused;

		base.OnGotFocus(pArgs);

		var hasFocus = HasFocus();
		var spOriginalSource = pArgs.OriginalSource;
		if (spOriginalSource is not null)
		{
			if (spOriginalSource is UIElement spFocusedElement)
			{
				focusState = spFocusedElement.FocusState;
			}
			notifyParent = (FocusState.Keyboard == focusState ||
							FocusState.Programmatic == focusState);
			// In mouse and touch we should pass notifyParent as false so the listviewbaseitem does not grab the focus itself.
		}
		FocusChanged(hasFocus, notifyParent);
	}

	// Called when the ListViewBaseItem loses focus.
	protected override void OnLostFocus(RoutedEventArgs pArgs)
	{
		base.OnLostFocus(pArgs);

		// clear the KeyboardPressed boolean
		m_isKeyboardPressed = false;

		var hasFocus = HasFocus();
		// Original source is always the listviewbaseitem or one of the controls in it.
		FocusChanged(hasFocus, true);
	}

	// Update our parent ListView when focus changes so it can manage the currently
	// focused item.
	private void FocusChanged(
		bool hasFocus,
		bool notifyParent)
	{
		var spListView = GetParentListView();

		// Win 8 RTM and higher - focus transfer is handled through FocusManager callback.
		// Just inform the parent selector that we got focus.
		if (spListView is not null)
		{
			// TODO Uno: ListViewBase.ItemFocused/ItemUnfocused not ported; SelectorItem.OnGotFocus sets FocusedIndexContainerItem.
		}

		ChangeVisualState(true);
	}

	// TODO Uno: EnsureDragDropVisual (LBI:1358-1379) - DragDropVisual not ported.

	// Create a holding visual LTE that will make the ListViewItem bigger
	private void CreateHoldingVisual()
	{
		// TODO Uno: holding LTE (LayoutTransitionElement_Create over GetDragVisual) not ported.
	}

	internal void ClearHoldingState()
	{
		DestroyHoldingVisual();
		m_isHolding = false;
		ChangeVisualState(true);
	}

	// Destroy the holding visual LTE
	private void DestroyHoldingVisual()
	{
		// TODO Uno: holding LTE (LayoutTransitionElement_Destroy) not ported.
	}

	// Holding visual will be transformed to size of 1.05, opacity 0.8, and positioned at the same place as the original ListViewItem
	private void TransformHoldingVisual(HoldingRoutedEventArgs pArgs)
	{
		// TODO Uno: holding LTE transform (scale s_holdingVisualScale, opacity s_holdingVisualOpacity) not ported.
	}

	// Sets the value to display as the dragged items count.
	internal virtual void SetDragItemsCountDisplay(
		uint dragItemsCount)
	{
		var pChrome = GetGridViewItemChromeNoRef();

		if (pChrome is not null)
		{
			pChrome.SetDragItemsCount(dragItemsCount);
		}
	}

	// Change to the correct visual state for the ListViewItem using
	// an existing VisualStateManagerBatchContext
	private protected override void ChangeVisualStateWithContext(
		// true to use transitions when updating the visual state, false
		// to snap directly to the new visual state.
		bool bUseTransitions)
	{
		// TODO Uno: recycled containers leave the tree, so states applied while unloaded must not animate.
		if (!IsLoaded)
		{
			bUseTransitions = false;
		}

		base.ChangeVisualStateWithContext(bUseTransitions);

		// new VisualState changing code for the new style
		ChangeVisualStateWithContextNewStyle(bUseTransitions);
	}

	// TODO Uno: OnNoSelectionHintStoryboardCompleted / OnNoReorderHintStoryboardCompleted / UpdateTopmost /
	// MakeTopmost / ClearTopmost (LBI:1516-1595) are live for Grid-rooted templates (e.g. ListViewItemExpanded) but not ported.

	// TODO Uno: GetCurrentTransitionContext / IsCollectionMutatingFast / GetDropOffsetToRoot (LBI:1597-1931) not ported.

	// Clears flags related to ongoing pointer input in response to a manipulation starting
	// or container recycle.
	// Updates the VisualState as appropriate.
	// Uno-specific: hides UIElement.ClearPointerState, which resets the routed-pointer bookkeeping instead.
	internal new void ClearPointerState()
	{
		// stop the touch timer
		StopTouchTimer();

		m_inPressedForTouch = false;
		m_inCheckboxPressedForTouch = false;
		m_shouldEnterPointerOver = false;
		m_isLeftButtonPressed = false;
		m_isRightButtonPressed = false;
		// TODO Uno: m_pendingTapPointerIDs.clear() - no pending tap list.

		UpdateVisualState(true);
	}

	// Clears all state related to user interaction. Used to prepare a ListViewBaseItem
	// for a new data item.
	internal void ClearInteractionState()
	{
		ClearPointerState();
		m_isCheckingForMouseDrag = false;
		m_inReorderHintState = false;
		m_inSelectionHintState = false;
		m_isTopmost = false;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == VisibilityProperty)
		{
			// WinUI quirk: Control.OnPropertyChanged2 already called OnVisibilityChanged, so it runs twice.
			OnVisibilityChanged();
		}
		else if (args.Property == BorderThicknessProperty)
		{
			var pChrome = GetItemChrome();

			if (pChrome is not null)
			{
				pChrome.InvalidateMeasure();
				pChrome.InvalidateRender();
			}
		}
		else if (args.Property == BorderBrushProperty || args.Property == BackgroundProperty)
		{
			var pChrome = GetItemChrome();

			if (pChrome is not null)
			{
				pChrome.InvalidateRender();
			}
		}
	}

	// Update the visual states when the Visibility property is changed.
	private protected override void OnVisibilityChanged()
	{
		var visibility = Visibility;
		if (Visibility.Visible != visibility)
		{
			ClearStateFlags();
		}

		UpdateVisualState();
	}

	// Clear flags relating to the visual state.  Called when IsEnabled is set to FALSE
	// or when Visibility is set to Hidden or Collapsed.
	private void ClearStateFlags()
	{
		// stop the touch timer
		StopTouchTimer();

		// Clear m_shouldEnterPointerOver because we'll no longer receive mouse input events
		// (including the MouseLeave event) once we've been disabled.
		m_shouldEnterPointerOver = false;
		m_inPressedForTouch = false;
		m_inCheckboxPressedForTouch = false;
		m_isLeftButtonPressed = false;
		m_isRightButtonPressed = false;
		m_isCheckingForMouseDrag = false;
		m_isKeyboardPressed = false;
	}

	// Handles when a key is pressed down on the ListViewBaseItem.
	protected override void OnKeyDown(KeyRoutedEventArgs pArgs)
	{
		var spListView = GetParentListView();

		// Ignore already handled events
		base.OnKeyDown(pArgs);
		bool isHandled = pArgs.Handled;

		if (!isHandled && spListView is not null && spListView.IsInExclusiveInteraction())
		{
			// During an exclusive interaction (drag/drop), disable all keyboard interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			isHandled = true;
		}

		if (!isHandled)
		{
			// get the released key
			var originalKey = pArgs.OriginalKey;

			switch (originalKey)
			{
				// we should go into the pressed visual state
				case VirtualKey.GamepadA:
					{
						SetIsKeyboardPressed(true /* isKeyboardPressed */);
						isHandled = true;
						break;
					}

				case VirtualKey.Left:
				case VirtualKey.Right:
				case VirtualKey.Up:
				case VirtualKey.Down:
					{
						// TODO Uno: Alt+Shift+Arrow keyboard reorder (LBI:2100-2130, OnKeyboardReorder) not ported.
						break;
					}
			}
		}

		// Inform ListView that the keydown came from an Item. If it came from a header, or blank area
		// instead of an item ListView will just forward the event back to the ScrollView.
		if (!isHandled && spListView is not null)
		{
			// TODO Uno: ListViewBase.SetHandleKeyDownArgsFromItem not ported.
		}

		if (isHandled)
		{
			pArgs.Handled = true;
		}
	}

	// Handles when a key is released on the ListViewBaseItem.
	protected override void OnKeyUp(KeyRoutedEventArgs pArgs)
	{
		var spListView = GetParentListView();

		// Ignore already handled events
		base.OnKeyUp(pArgs);
		bool isHandled = pArgs.Handled;

		if (!isHandled && spListView is not null && spListView.IsInExclusiveInteraction())
		{
			// During an exclusive interaction (drag/drop), disable all keyboard interaction.
			// Handle the event, since other controls don't have the necessary context around drag/drop interactions.
			isHandled = true;
		}

		// get the released key
		var originalKey = pArgs.OriginalKey;

		// This code needs to always execute to prevent getting stuck in Pressed state.
		try
		{
			if (!isHandled && m_isKeyboardPressed)
			{
				switch (originalKey)
				{
					// Gamepads use Gamepad A as both the Primary and Secondary interaction
					// Note that OnPrimaryInteractionGesture will do invoke if possible and if not
					// will fallback to selection.
					case VirtualKey.GamepadA:
						{
							if (spListView is not null)
							{
								// TODO Uno: ListViewBase.OnItemPrimaryInteractionGesture(this, isKeyboardInput: true) not ported.
							}

							break;
						}
				}
			}

			if (isHandled)
			{
				pArgs.Handled = true;
			}
		}
		finally
		{
			switch (originalKey)
			{
				// Gamepads use Gamepad A as both the Primary and Secondary interaction
				// gesture depending on the current state of the control
				case VirtualKey.GamepadA:
					{
						SetIsKeyboardPressed(false /* isKeyboardPressed */);
						break;
					}
			}
		}
	}

	// TODO Uno: OnKeyboardReorder / QueryFor / Get+SetDragIsGrabbed / GoToStateWithFallback / GetLogicalParentForAPProtected (LBI:2217-2352) not ported here.

	// MUX Reference ListViewBaseItem_Partial.cpp, lines 2354-2381
	// Uno-specific: a type test instead of WinUI's exact chrome type index, so presenter subclasses (e.g. RevealListViewItemPresenter) get the chrome as in WinUI.
	private ListViewBaseItemPresenter? GetItemChrome() => GetFirstChildNoAddRef() as ListViewBaseItemPresenter;

	// TODO Uno: GetOrCreateDragDropVisual / ClearDragDropVisual (LBI:2383-2398) not ported here.

	private void ChangeVisualStateWithContextNewStyle(
		// true to use transitions when updating the visual state, false
		// to snap directly to the new visual state.
		bool bUseTransitions)
	{
		ListViewBaseItemVisualStatesCriteria criteria = new();

		criteria.isEnabled = IsEnabled;
		criteria.isSelected = IsSelected;
		criteria.focusState = FocusState;

		// Pressed state should be handled whether it's mouse or touch
		// m_inCheckboxPressedForTouch is not used because it is part of the 8.1 template
		criteria.isPressed = m_isLeftButtonPressed || m_isRightButtonPressed || m_inPressedForTouch || m_isKeyboardPressed;
		criteria.isPointerOver = m_shouldEnterPointerOver;
		criteria.isDragVisualCaptured = m_dragVisualCaptured;

#if HAS_UNO
		// Uno-specific: FeatureConfiguration opt-out of the pointer-over states.
		criteria.isPointerOver &= FeatureConfiguration.SelectorItem.UseOverStates;
#endif

		var spListView = GetParentListView();
		if (spListView is not null)
		{
			criteria.isDragging = spListView.IsInDragDrop();
			criteria.isDraggedOver = spListView.IsDragOverItem(this);
			criteria.dragItemsCount = spListView.DragItemsCount();
			criteria.isItemDragPrimary = spListView.IsContainerDragDropOwner(this);

			// Holding gesture will show drag visual
			criteria.canDrag = spListView.CanDragItems;
			criteria.canReorder = spListView.CanReorderItems;
			if (spListView.GetIsHolding())
			{
				criteria.isHolding = true;
				if (m_isHolding)
				{
					criteria.isItemDragPrimary = true;
				}
			}

			criteria.isMultiSelect = spListView.IsMultiSelectCheckBoxEnabled;

			criteria.isIndicatorSelect = ListViewBaseItemPresenter.IsRoundedListViewBaseItemChromeEnabledStatic();

			var selectionMode = spListView.SelectionMode;

			// if the ListView selection mode is None, we should appear as not Selected
			criteria.isSelected &= (selectionMode != ListViewSelectionMode.None);

			// Read-only mode
			{
				var isItemClickEnabled = spListView.IsItemClickEnabled;

				if (selectionMode == ListViewSelectionMode.None && !isItemClickEnabled)
				{
					criteria.isPressed = false;
					criteria.isPointerOver = false;
				}
			}

			if (criteria.isMultiSelect || criteria.isIndicatorSelect)
			{
				var selectionMode2 = spListView.SelectionMode;

				if (criteria.isMultiSelect)
				{
					criteria.isMultiSelect &= (selectionMode2 == ListViewSelectionMode.Multiple);
				}

				if (criteria.isIndicatorSelect)
				{
					criteria.isIndicatorSelect &= (selectionMode == ListViewSelectionMode.Single || selectionMode == ListViewSelectionMode.Extended);
				}
			}

			criteria.isInsideListView = true;
		}

		// get all valid visual states
		var validVisualStates = VisualStatesHelper.GetValidVisualStatesListViewBaseItem(criteria);

		foreach (var visualState in validVisualStates)
		{
			GoToState(bUseTransitions, visualState);
		}
	}

	// returns true if we captured the pointer for the drag visual
	internal void SetDragVisualCaptured(
		bool dragVisualCaptured)
	{
		m_dragVisualCaptured = dragVisualCaptured;

		// make sure we go to the right state
		ChangeVisualState(true);
	}

	// Sets the state of the isKeyboardPressed boolean
	private void SetIsKeyboardPressed(
		bool isKeyboardPressed)
	{
		if (m_isKeyboardPressed != isKeyboardPressed)
		{
			m_isKeyboardPressed = isKeyboardPressed;

			// make sure we go to the right state
			ChangeVisualState(true);
		}
	}

	private void StartTouchTimer(
		TouchTimerState touchTimerState)
	{
		TimeSpan timeSpan = default;

		// make sure we stop the current timer if it exists
		StopTouchTimer();

		// make sure we have a timer
		var touchTimer = EnsureTouchTimer();

		// set the state
		m_touchTimerState = touchTimerState;

		switch (m_touchTimerState)
		{
			case TouchTimerState.PressedTimer:
				timeSpan = TimeSpan.FromMilliseconds(LISTVIEWBASEITEM_TOUCH_PRESSED_DELAY);
				break;

			case TouchTimerState.ReleasedTimer:
				timeSpan = TimeSpan.FromMilliseconds(LISTVIEWBASEITEM_TOUCH_RELEASED_TIMER);
				break;
		}

		// set the interval
		touchTimer.Interval = timeSpan;

		// start the timer
		touchTimer.Start();
	}

	private DispatcherTimer EnsureTouchTimer()
	{
		// if it hasn't been created yet, create it
		if (m_tpTouchTimer is null)
		{
			DispatcherTimer spNewDispatcherTimer = new();

			// attach the handler
			spNewDispatcherTimer.Tick += (_, _) => TouchTimerTickHandler();

			m_tpTouchTimer = spNewDispatcherTimer;
		}

		return m_tpTouchTimer;
	}

	private void StopTouchTimer()
	{
		m_tpTouchTimer?.Stop();

		m_touchTimerState = TouchTimerState.NoTimer;
	}

	// Handler for the Tick event on m_tpTouchTimer
	private void TouchTimerTickHandler()
	{
		switch (m_touchTimerState)
		{
			case TouchTimerState.PressedTimer:
				m_inPressedForTouch = true;
				break;

			case TouchTimerState.ReleasedTimer:
				m_inPressedForTouch = false;
				break;
		}

		StopTouchTimer();
		ChangeVisualState(true);
	}

	// Returns TRUE if the item should play the DragOver visual state
	// this is true if AllowDrop is true
	// and if the dragPoint is in the center area of the item
	// 20/60/20 for LVI
	// 30/40/30 for GVI
	private bool IsDragOver(
		Point dragPoint,
		Orientation panelOrientation)
	{
		// Represents the outside margin percentage
		// In case of ListViewItem, margins are 20/60/20
		// In case of GridViewItem, margins are 30/40/30
		double startMargin = this is GridViewItem ? GRIDVIEWITEM_DRAGOVER_OUTSIDE_MARGIN : LISTVIEWITEM_DRAGOVER_OUTSIDE_MARGIN;
		// Represents the outside margin percentage added to the inside percentage
		// In case of ListViewItem, this will be 0.2 + 0.6 = 0.8 or (1 - 0.2)
		// In case of GridViewItem, this will be 0.3 + 0.4 = 0.7 or (1 - 0.3)
		double endMargin = 1.0 - startMargin;
		double size = 0;
		double location = 0;

		var pResult = false;

		// depending on the orientation of the panel, we look at either the height or width
		if (panelOrientation == Orientation.Vertical)
		{
			location = dragPoint.Y;
			size = ActualHeight;
		}
		else
		{
			location = dragPoint.X;
			size = ActualWidth;
		}

		if (location > startMargin * size && location < endMargin * size)
		{
			pResult = true;
		}

		return pResult;
	}

	// Called when the user drags over this ListViewBaseItem
	protected override void OnDragOver(DragEventArgs args)
	{
		var spListView = GetParentListView();

		// TODO Uno: Uno's live reorder lays the dragged container out under the pointer, where WinUI has an empty gap; don't swallow the reorder DragOver.
		if (spListView is not null && !spListView.IsLiveReorderPrimaryContainer(this, args))
		{
			bool isDragOver = false;
			Point dragPointRelativeToLVBI = default;
			Orientation panelOrientation = Orientation.Vertical;

			dragPointRelativeToLVBI = args.GetPosition(this);
			panelOrientation = spListView.GetPanelOrientation();

			isDragOver = IsDragOver(dragPointRelativeToLVBI, panelOrientation);

			if (isDragOver)
			{
				spListView.SetDragOverItem(this);
				args.Handled = true;
			}
			else
			{
				spListView.SetDragOverItem(null);
			}

			SetIsDragOver(isDragOver);
		}
	}

	// Called when the user drags out of this ListViewBaseItem
	protected override void OnDragLeave(DragEventArgs args) => LeaveDragOver(GetParentListView(), null);

	// Declares this item as no longer being dragged over and instead uses the
	// pointer-over visual state if the args' pointer is within its boundaries.
	internal void LeaveDragOver(
		ListViewBase? listViewBase,
		DragEventArgs? args)
	{
		if (listViewBase is not null)
		{
			listViewBase.SetDragOverItem(null);
		}

		SetIsDragOver(false /*isDragOver*/);

		if (args is not null)
		{
			var dragPointRelativeToLVBI = args.GetPosition(this);
			var width = ActualWidth;
			var height = ActualHeight;

			if (dragPointRelativeToLVBI.X >= 0.0 && dragPointRelativeToLVBI.X <= width &&
				dragPointRelativeToLVBI.Y >= 0.0 && dragPointRelativeToLVBI.Y <= height)
			{
				// Pointer is within item's boundaries. Use the PointerOver visual state.
				m_shouldEnterPointerOver = true;
				// No need to call ChangeVisualState as SetIsDragOver above already did so.
			}
		}
	}

	// Sets the state of the isDragOver boolean
	private void SetIsDragOver(
		bool isDragOver)
	{
		if (m_isDragOver != isDragOver)
		{
			m_isDragOver = isDragOver;
			ChangeVisualState(true);
		}
	}

	// TODO Uno: IsInBottomHalfForExternalReorder (LBI:2746-2783) - external reorder not ported.
}
