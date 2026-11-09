// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_Reorder.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DirectUI;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using MovedItem = DirectUI.Components.LiveReorderHelper.MovedItem;
using MovedItems = DirectUI.Components.LiveReorderHelper.MovedItems;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	// Bug 62849414: Reserve space in TrackerCollections before appending multiple items

	// Phone contstants for reorder mode.
	// If user moves pointer less than s_reorderConfirmationThresholdInDips device-independent pixels
	// during time s_reorderConfirmationDelayInMsec from touch-down then gesture is confirm reorderable item.
	// Otherwise it is a pan (flick).
	// (s_reorderConfirmationDelayInMsec and s_reorderConfirmationThresholdInDips are in ListViewBase.partial.h.mux.cs)

	private const int LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX = 100;
	private const int LISTVIEWBASE_EDGE_SCROLL_START_DELAY_MSEC = 50;

	// Edge scroll speed varies linearly with distance from edge.
	// At 0 px from the edge, the speed is LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED.
	// At LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX from the edge, the speed is
	// LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED.
	private const double LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED = 150.0 /* px/sec */;
	private const double LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED = 1500.0 /* px/sec */;

	// The delay time before going playing the live reorder
	private const int LISTVIEW_LIVEREORDER_TIMER = 200;
	private const int GRIDVIEW_LIVEREORDER_TIMER = 300;

	private const double ScrollViewerScrollRoundingTolerance = 0.05f;

	// If there isn't a drag and drop in progress
	// 1) fire DragItemsStart to serialize the current selection and to determine if the drag should be canceled.
	// 2) If the drag isn't canceled, kick off the drag operation.
	//  pointer -          The pointer used to start the new drag.
	//  pOriginatingItem -  The item the user performed a drag gesture on.
	//  dragStartPoint   -  the point at which the drag should start.
	//                      This should line up with the user's current Pointer location.
	// onDragStarting    -  Called when we've decided to actually start a drag, meaning that DragItemsStarting
	//                      was fired and did not request a cancel (or drag/drop is disabled but reordering is enabled).
	//                      NOTE! This doesn't mean *pWasCanceled is guaranteed to become false! We can still fail in other ways.
	// pWasCanceled      -  Set if the drag was canceled or failed in some way.
	internal bool OnDragGesture(
		Pointer pointer,
		PointerPoint pointerPoint,
		ListViewBaseItem pOriginatingItem,
		object pDragVisual,
		Point dragStartPoint,
		Action onDragStarting)
	{
		bool pWasCanceled;

		// Take a ref on the visual in case someone removes our dragged item inside DragItemsStarting or similar.
		object spDragDropVisual = pDragVisual;
		ListViewBaseItem spOriginatingItem = pOriginatingItem;

#if !HAS_UNO // TODO Uno: no core DragDrop service, Uno's DragDropManager runs the operation.
		DragDrop dragDrop = DXamlCore.GetCurrent().GetDragDrop();
#endif

		ArgumentNullException.ThrowIfNull(pOriginatingItem);
		ArgumentNullException.ThrowIfNull(pointer);
		ArgumentNullException.ThrowIfNull(pDragVisual);

		pWasCanceled = true;

		try
		{
			// Ignore extra drag gestures during a drag
			if (m_tpDragPointer == null
#if !HAS_UNO
				&& !dragDrop.GetIsDragDropInProgress()
#endif
				)
			{
				bool canDragItems = false;
				bool wasCanceled = false;
#if !HAS_UNO
				bool useWinRtDragDrop = dragDrop.GetUseCoreDragDrop();
#endif
				PointerDeviceType pointerDeviceType = PointerDeviceType.Touch;

				DataPackage spDataPackage;
				IList<object>? spItems;
#if !HAS_UNO
				IDragOperation spDragOperation;
				DragStartingEventArgs spDragStartingEventArgs;
				StartDragAsyncOperation spStartDragAsyncOperation;
#endif

				pointerDeviceType = pointer.PointerDeviceType;

				GetDraggedItems(spOriginatingItem, true /* wantItems */, out spItems, null /* pItemIndices */);

#if HAS_UNO
				// TODO Uno: Uno raises DragStarting itself and owns the DataPackage, WinUI creates the drag operation and the DragStartingEventArgs here.
				spDataPackage = m_unoDragStartingArgs!.Data;
#else
				// WinRT DragDrop API does not support on phone and only works when a CoreWindow is associated with the app which is not the case when XAML Presenter
				// is in use. In those cases, we will fallback to use our own DragDropInternal API.
				if (useWinRtDragDrop)
				{
					spDragOperation = dragDrop.CreateDragOperation();
					spDataPackage = spDragOperation.Data;

					WeakReference<ListViewBaseItem> wpItem = new(pOriginatingItem);

					spStartDragAsyncOperation = new StartDragAsyncOperation(wpItem, spDragOperation);
					spDragStartingEventArgs = new DragStartingEventArgs();
					spDragStartingEventArgs.SetOperationContext(dragStartPoint, spStartDragAsyncOperation, spDataPackage, pointerPoint);
				}
				else
				{
					spDataPackage = GetBlankDataPackage();
				}
#endif

				canDragItems = CanDragItems;
				if (canDragItems)
				{
					// Only raise DragItemsStarting if CanDragItems == TRUE.
					wasCanceled = OnDragItemsStarting(spItems!, spDataPackage);
				}

				if (!wasCanceled)
				{
#if HAS_UNO
					// TODO Uno: DragStarting was already raised by Uno on the item, so only its Cancel is read.
					wasCanceled = m_unoDragStartingArgs!.Cancel;
#else
					pOriginatingItem.RaiseDragStarting(spDragStartingEventArgs, false /* shouldStartOperation */);

					wasCanceled = spDragStartingEventArgs.Cancel;
#endif
				}

				// Only start dragging if the handler didn't cancel the drag.
				if (!wasCanceled)
				{
					bool gotCapture = true;
					m_lastDragPosition.X = 0;
					m_lastDragPosition.Y = 0;

					// Release pointer capture on the dragged item
					onDragStarting();

#if !HAS_UNO // TODO Uno: Uno's drag never captures the pointer on the ListViewBase, as the WinRT drag path does.
					if (!useWinRtDragDrop)
					{
						//Capture pointer on the listviewbase.
						gotCapture = CapturePointer(pointer);
					}
#endif

					// Someone else might have already captured this pointer.
					// Only start the drag if we actually got capture. Otherwise, cancel.
					if (gotCapture)
					{
						m_tpDragPointer = pointer;
						m_dragItemsCount = (uint)spItems!.Count;
						m_tpPrimaryDraggedContainer = spOriginatingItem;

						// Update the item's TemplateSettings.DragItemsCount with the size of our dragged items list.
						spOriginatingItem.SetDragItemsCountDisplay(m_dragItemsCount);

						NotifyDragVisualCreated(pDragVisual);
						m_isDraggingOverSelf = false;
#if !HAS_UNO // TODO Uno: no core DragDrop service.
						dragDrop.DragStart(this, spOriginatingItem, spDataPackage, pDragVisual, dragStartPoint, m_dragItemsCount, !useWinRtDragDrop /* shouldFireEvent */);
#endif

#if HAS_UNO
						// TODO Uno: Uno completes the drag through the item's DropCompleted event, WinUI through the completion of its StartDragAsyncOperation.
						WeakReference<ListViewBase> wpThis = new(this);
						TypedEventHandler<UIElement, DropCompletedEventArgs>? callbackPtr = null;

						callbackPtr = (sender, args) =>
						{
							sender.DropCompleted -= callbackPtr;

							// DragDrop::CleanupDragDrop
							spOriginatingItem.SetDragVisualCaptured(false);
							spOriginatingItem.SetDragIsGrabbed(false);

							if (wpThis.TryGetTarget(out var spThis))
							{
								spThis.OnDragCompleted(args, spItems!);

								// Calls OnDropGesture to update the state of the dragged items
								spThis.OnDropGesture(null, new Point(-1, -1));

								// TODO Uno: OnDrop's CompleteDrop clears m_tpDragPointer first, so OnDropGesture may not release the visual.
								spThis.NotifyDragVisualReleased();
							}
						};

						spOriginatingItem.DropCompleted += callbackPtr;
#else
						if (useWinRtDragDrop)
						{
							WeakReference<ListViewBase> wpThis = new(this);
							uint pointerId = 0;
							DragVisual spDragVisual;

							Action<IAsyncOperation<DataPackageOperation>, AsyncStatus> callbackPtr = (pAsyncOp, status) =>
							{
								bool isHandledIgnored = false;

								try
								{
									if (wpThis.TryGetTarget(out var spThis))
									{
										spThis.OnDragCompleted(pAsyncOp, status, spItems);

										// Calls OnDropGesture to update the state of the dragged items
										isHandledIgnored = spThis.OnDropGesture(null, new Point(-1, -1));
									}
								}
								finally
								{
									// There is some specific ListView stuff in core Drag/Drop that this attempts to
									// clean up.  However, it is possible that this event can come in after we have
									// shut down the core.  So make sure one exists before we try to clean up.
									if (DXamlCore.GetCurrent() != null)
									{
										DXamlCore.GetCurrent().GetDragDrop().CleanupDragDrop();
									}
								}
							};

							pointerId = pointer.PointerId;

							PointerDeviceType deviceType;
							deviceType = pointer.PointerDeviceType;

							bool isMouse = (deviceType == PointerDeviceType.Mouse);

							AppPolicyWindowingModel policy = AppPolicyWindowingModel_None;
							int status = AppPolicyGetWindowingModel(GetCurrentThreadEffectiveToken(), out policy);
							if (status != ERROR_SUCCESS)
							{
								throw new InvalidOperationException();
							}

							if (!isMouse && policy != AppPolicyWindowingModel_Universal)
							{
								var hmod = GetModuleHandleA("microsoft.ui.input.dll");
								if (hmod == IntPtr.Zero)
								{
									throw new InvalidOperationException();
								}
								var captureForDragAndDrop = GetProcAddress(hmod, "LiftedInputCapturePointerIdForDragAndDrop");
								if (captureForDragAndDrop != null)
								{
									int hr = captureForDragAndDrop(pointerId);

									if (hr == E_ACCESSDENIED)
									{
										// This means IXP API used for touch and pen drag and drop does not support current scenario, we should cancel in progress drag and return.
										DXamlCore.GetCurrent().GetDragDrop().CleanupDragDrop();
										return pWasCanceled;
									}
									Marshal.ThrowExceptionForHR(hr);
								}
							}

							// Capture the source ListViewBase to be able to get position updates from
							// WinRT DragDrop component. This is only for M1 when setting DragUI is
							// not supported.
							dragDrop.SetCapturedDragSource(this);

							// Start drag operation
							spDragStartingEventArgs.StartOrUpdateOperation();
							spStartDragAsyncOperation.Completed = callbackPtr;
						}
#endif

						m_isHolding = false;
						ChangeSelectorItemsVisualState(true);
						UpdateDropTargetDropEffect(false, false /*isLeaving*/);
						UpdateDragGrabbedItems(false);
						AutomationPeer.RaiseEventIfListener(spOriginatingItem, AutomationEvents.DragStart);
					}
					else
					{
						wasCanceled = true;
					}
				}

				if (wasCanceled)
				{
					// make sure the holding variable is cleared
					SetIsHolding(false /* isHolding */);
					AutomationPeer.RaiseEventIfListener(spOriginatingItem, AutomationEvents.DragCancel);
					CompleteDrop();
				}
				else
				{
					pWasCanceled = false;
				}
			}
		}
		catch (Exception)
		{
#if !HAS_UNO // TODO Uno: no core DragDrop service.
			DXamlCore.GetCurrent().GetDragDrop().CleanupDragDrop();
#endif
			throw;
		}

		return pWasCanceled;
	}

	//
	// This is fired on the drag source ListViewBase when WinRt DragDrop API is used.
	//
	// TODO Uno: takes the DropCompletedEventArgs Uno raises on the item in place of the IAsyncOperation and its status.
	private void OnDragCompleted(
		DropCompletedEventArgs pArgs,
		IList<object> pItems)
	{
		bool canDragItems = false;
		canDragItems = CanDragItems;

		if (canDragItems) // only raise DragItemsCompleted if DragItemsStarting was raised
		{
			DataPackageOperation dropResult;

			// Create the event args
			DragItemsCompletedEventArgs spArgs = new(pArgs, new ReadOnlyCollection<object>(pItems));

			dropResult = pArgs.DropResult;

			// Raise the event
			DragItemsCompleted?.Invoke(this, spArgs);

			if (dropResult == DataPackageOperation.None)
			{
				// The drag&drop operation was canceled, with an Escape keystroke or a right-mouse-button click
				// for example. Make sure DropCausesReorder's evaluation is False in the upcoming ListViewBase::OnDropGesture execution
				// so that it invokes UpdateDropTargetDropEffect and CompleteDrop.
				m_isDraggingOverSelf = false;

				// Also, make sure the items that were dragged regain their original location before m_movedItems is cleared.
				ResetAllItemsForLiveReorder();
			}
		}

		// stop the timer
		StopLiveReorderTimer();

		// reset the moved items vector
		m_movedItems.Clear();
	}

	//
	// Called by DropTargetOperation directly on the currently captured drop target ListViewBase
	// to ensure that the visual position gets updated even though the drag happens outside the
	// ListViewBase that originate the drag.
	//
	internal void UpdateDragPositionOnCapturedTarget(
		DragDropMessageType type,
		Point dragPoint)
	{
		GeneralTransform spTransformFromRoot;
		GeneralTransform spTransformToRoot;

		spTransformToRoot = TransformToVisual(null!);
		spTransformFromRoot = spTransformToRoot.Inverse;

		dragPoint = spTransformFromRoot.TransformPoint(dragPoint);

		switch (type)
		{
			case DragDropMessageType.DragEnter:
			case DragDropMessageType.DragOver:
				OnDragMoveGesture(null /*PointerId*/, dragPoint);
				break;
			case DragDropMessageType.DragLeave:
				break;
			case DragDropMessageType.Drop:
				// this drop action happens outside the current listview
				OnDropGesture(null /*PointerId*/, dragPoint);
				break;
			default:
				global::System.Diagnostics.Debug.Assert(false);
				break;
		}
	}

	// Called when the user moves the drag location.
	// If the current drag modality isn't the same as the provided modality,
	// do nothing.
	//  modality   - The input modality used to indicate the drop.
	//  dragPoint  - The point the user moved to. Relative to this ListViewBase.
	//  pIsHandled - Set to FALSE if the gesture was ignored due to input modality mismatch, set to TRUE otherwise.
	private protected bool OnDragMoveGesture(
		Pointer? pointer,
		Point dragPoint)
	{
		bool pIsHandled = false;
		bool shouldAccept = false;

		shouldAccept = ShouldAcceptDragInput(pointer);

		pIsHandled = shouldAccept;

		if (shouldAccept)
		{
#if HAS_UNO
			// TODO Uno: no DragDropVisual or core DragDrop service, Uno's DragDropManager moves the drag visual.
#else
			if (((DragDropVisual)m_tpDragDropVisual!).IsShowing())
			{
				DXamlCore.GetCurrent().GetDragDrop().DragMove(dragPoint, ref m_lastDragPosition, pointer != null /* shouldFireEvent */);
			}
			else
			{
				// Our visual is no longer showing (usually because someone removed our drag visual template
				// part from the tree). Cancel the drag.
				CancelDrag();
			}
#endif
		}

		return pIsHandled;
	}

	// Only one Pointer can drive drag and drop operations at a time.
	// This method returns TRUE if there is no current drag and drop operation,
	// or the given IPointer is the pointer currently in charge of a drag
	// and drop operation.
	private bool ShouldAcceptDragInput(
		Pointer? pointer)
	{
		bool shouldAccept = false;

		if (m_tpDragPointer != null)
		{
			if (pointer == null)
			{
				// Since only Mouse drag is supported using WinRT drag and drop API in M1,
				// if m_tpDragPointer is set, return TRUE mouse always produces the same
				// pointer id.
				shouldAccept = true;
			}
			else
			{
				shouldAccept = AreSamePointer(m_tpDragPointer, pointer);
			}
		}

		return shouldAccept;
	}

	// Returns TRUE if the given IPointers refer to the same physical device.
	private static bool AreSamePointer(
		Pointer pPointerA,
		Pointer pPointerB)
	{
		uint idPointerA = 0;
		uint idPointerB = 1;

		ArgumentNullException.ThrowIfNull(pPointerA);
		ArgumentNullException.ThrowIfNull(pPointerB);

		idPointerA = pPointerA.PointerId;
		idPointerB = pPointerB.PointerId;

		return idPointerA == idPointerB;
	}

	// Called when the user indicates the dragged item should be dropped.
	// If the current drag modality isn't the same as the provided modality,
	// do nothing.
	//  pointer    - The input pointer used to indicate the drop.
	//  pIsHandled  - Set to FALSE if the gesture was ignored due to input modality mismatch, set to TRUE otherwise.
	private protected bool OnDropGesture(
		Pointer? pointer,
		Point dropPoint)
	{
		bool pIsHandled = false;
		bool shouldAccept = false;

		shouldAccept = ShouldAcceptDragInput(pointer);

		pIsHandled = shouldAccept;

		if (shouldAccept)
		{
			Pointer? spLocalDragPointer;
			bool dropCausesReorder = false;

			// We must clear m_tpDragPointer before calling ReleasePointerCapture,
			// otherwise we risk re-entering this block (PointerCaptureLost is sync).
			// ShouldAcceptDragInput will not return TRUE if m_tpDragPointer is NULL.
			spLocalDragPointer = m_tpDragPointer;
			m_tpDragPointer = null;
			ReleasePointerCapture(spLocalDragPointer!);
			NotifyDragVisualReleased();
			global::System.Diagnostics.Debug.Assert(m_tpPrimaryDraggedContainer != null);
#if !HAS_UNO // TODO Uno: no core DragDrop service.
			DXamlCore.GetCurrent().GetDragDrop().Drop(pointer != null /* shouldFireEvent */);
#endif
			UpdateDragGrabbedItems(false);
			dropCausesReorder = DropCausesReorder();

			if (!dropCausesReorder)
			{
				UpdateDropTargetDropEffect(false, false /*isLeaving*/);
				CompleteDrop();
			}
			else if (pointer != null)
			{
				// Phone reordering mode relies on a call to ListViewBase::IsInDragDrop() to reject manipulations while D&D is in progress.
				// This method uses presence of primary dragged container to determine whether operation is in progress.
				// In most cases ListViewBase::OnDropGesture is called from either OnPointerReleased or OnPointerCaptureLost AFTER ListViewBase::OnDragLeave,
				// which sets a flag m_isDraggingOverSelf which determines whether cleanup in OnDropGesture is executed.
				// In a case when user quickly flicks an item out of ListView, the order of events is reversed and primary dragged container is not cleared.
				// This in turn will ignore all following reordering requests, as ListView will be still in dragging operation.
				// This code detects if item is dragged outside of ListView but OnDragLeave has not executed and clears primary dragged container.

				bool foundThis = false;

				// TODO Uno: the can-hit-disabled and can-hit-invisible defaults are not passed.
				foreach (UIElement spElement in VisualTreeHelper.FindElementsInHostCoordinates(
					dropPoint,
					null!))
				{
					foundThis = ReferenceEquals(this, spElement);

					if (foundThis)
					{
						break;
					}
				}

				if (!foundThis)
				{
					// Reorder hint items will be cleared in OnDragLeave.
					ClearPrimaryDragContainer();
				}
			}
		}
		else if (m_isDraggingOverSelf && m_tpDragPointer != null)
		{
			// if m_tpDragPointer is NULL, it means that we have already handled
			// the DropGesture in the upper part of this function
			// in that case, we do not want to clear the variables as the
			// drop may still be using them
			// note that the DropGesture will clear all these variables itself

			// Drop has happened on a descendant, we have to cleanup the D&D
			m_tpDragPointer = null;
			NotifyDragVisualReleased();
			ClearPrimaryDragContainer();
			SetPendingAutoPanVelocity(PanVelocity.Stationary());
			ChangeSelectorItemsVisualState(true);
			m_isDraggingOverSelf = false;
		}

		return pIsHandled;
	}

	private void ClearPrimaryDragContainer()
	{
		if (m_tpPrimaryDraggedContainer != null)
		{
			// Update our item's TemplateSettings with a zero DragItemsCount.
			((ListViewBaseItem)m_tpPrimaryDraggedContainer).SetDragItemsCountDisplay(0);
			((ListViewBaseItem)m_tpPrimaryDraggedContainer).ClearLastTouchPressedArgs();
			m_tpPrimaryDraggedContainer = null;
		}

		m_dragItemsCount = 0;
	}

	// Complete a drop gesture.
	private void CompleteDrop()
	{
		SetPendingAutoPanVelocity(PanVelocity.Stationary());

		if (m_tpPrimaryDraggedContainer != null)
		{
			ListViewBaseItem spPrimaryDraggedContainer = (ListViewBaseItem)m_tpPrimaryDraggedContainer;

			ClearPrimaryDragContainer();
			AutomationPeer.RaiseEventIfListener(spPrimaryDraggedContainer, AutomationEvents.DragComplete);
			AutomationPeer.RaiseEventIfListener(this, AutomationEvents.Dropped);
		}

		m_tpDragPointer = null;
		ChangeSelectorItemsVisualState(true);

		// stop the timer
		StopLiveReorderTimer();

		// reset live reorder variables
		m_liveReorderIndices = new(-1, -1, -1);

		// reset the moved items vector
		m_movedItems.Clear();
	}

	// Builds one or both of:
	// - a vector containing the data items that should be dragged. This is the vector that will be passed as the Items property in the DragItemsStartingEvent args.
	// - a list containing the current indexes of said items. Not that this isn't ref-counted - it's a std::list.
	private void GetDraggedItems(
		ListViewBaseItem pOriginatingItem,
		bool wantItems,
		out IList<object>? ppItems,
		List<uint>? pItemIndices)
	{
		bool isOriginatingItemSelected = false;
		int originatingItemIndex = 0;
		List<uint> selectedItemIndices = new();

		ArgumentNullException.ThrowIfNull(pOriginatingItem);

		ppItems = null;

		isOriginatingItemSelected = pOriginatingItem.IsSelected;
		originatingItemIndex = IndexFromContainer(pOriginatingItem);

		// If the item belongs to the selection, we reorder all selected items
		if (isOriginatingItemSelected)
		{
			int originatingItem;

			selectedItemIndices = m_selection.GetSelectedIndexes();

			// Find the item's index in the selected indices to be removed
			originatingItem = selectedItemIndices.IndexOf((uint)originatingItemIndex);

			if (originatingItem != -1)
			{
				// remove the dragged item from the selected indices
				// that way, we can add the first item on its own
				selectedItemIndices.RemoveAt(originatingItem);
			}
			else
			{
				// making sure the value is -1
				global::System.Diagnostics.Debug.Assert(originatingItemIndex == -1);
			}
		}

		// if the dragged item was removed after the drag has started, it won't belong to the item collection anymore
		// and originatingItemIndex will be -1
		// if not, we insert its index to the front of the array of indices
		if (originatingItemIndex != -1)
		{
			selectedItemIndices.Insert(0, (uint)originatingItemIndex);
		}

		if (wantItems)
		{
			ItemCollection spItems;
			List<object> spGeneratedItemsList;

			// create the list
			spGeneratedItemsList = new(selectedItemIndices.Count);

			spItems = Items;

			// add the dragged items
			foreach (uint itemIndex in selectedItemIndices)
			{
				object spItem;
				bool isItemOwnContainer = false;

				spItem = spItems[(int)itemIndex];

				// Do a quick check to see if the item is its own container before proceeding.
				isItemOwnContainer = IsItemItsOwnContainer(spItem);

				if (isItemOwnContainer)
				{
					// Item was a container. Add its data to the dragged items list.
					// Note that we're assuming our containers are at least IContentControls. Drag/drop doesn't work
					// with other containers, so it's reasonable to make this assumption. if the customer makes an odd
					// ICG that mixes container types, we simply won't drag items that are not IContentControls.

					ContentControl? spItemAsICC = spItem as ContentControl;

					if (spItemAsICC != null)
					{
						object spItemData;

						// Try to get the data from the container.
						spItemData = spItemAsICC.Content;

						spGeneratedItemsList.Add(spItemData);
					}
				}
				else
				{
					// Item wasn't a container. Go ahead and add to the dragged items list.
					spGeneratedItemsList.Add(spItem);
				}
			}

			ppItems = spGeneratedItemsList;
		}

		if (pItemIndices != null)
		{
			pItemIndices.Clear();
			pItemIndices.AddRange(selectedItemIndices);
		}
	}

	// Raises the DragItemsStarting event to let the handler populate
	// the DataPackage and to determine whether or not the drag
	// was canceled by the handler.
	private protected bool OnDragItemsStarting(
		IList<object> pDraggedItems,
		DataPackage pData)
	{
		bool pWasCanceled = false;

		DragItemsStartingEventArgs spArgs;

		ArgumentNullException.ThrowIfNull(pData);
		ArgumentNullException.ThrowIfNull(pDraggedItems);

		pWasCanceled = false;

		// Create the args
		// TODO Uno: the args wrap Uno's DragStartingEventArgs, whose Data is pData.
		spArgs = new DragItemsStartingEventArgs(m_unoDragStartingArgs!, pDraggedItems);

		// Raise the event
		DragItemsStarting?.Invoke(this, spArgs);

		// Return the results of the event.
		pWasCanceled = spArgs.Cancel;

		return pWasCanceled;
	}

	// Sets pResult to TRUE if
	// 1) a drag and drop operation is in progress, and
	// 2a) the given item is the item the user is physically dragging
	//    (as opposed to just part of the selection), or
	// 2b) the given item is the owning GroupItem for the item the user is physically dragging.
	// Sets pResult to FALSE otherwise.
	internal bool IsContainerDragDropOwner(
		UIElement pContainer)
	{
		bool pResult = false;

		ArgumentNullException.ThrowIfNull(pContainer);

		pResult = false;

		if (m_tpPrimaryDraggedContainer != null)
		{
			bool isPrimaryDraggedContainer = false;

			// pContainer could either be a real item, or it could be a GroupItem.
			isPrimaryDraggedContainer = ReferenceEquals(pContainer, m_tpPrimaryDraggedContainer);
			if (isPrimaryDraggedContainer)
			{
				// Normal item.
				pResult = true;
			}
			else
			{
				// Possibly group item.
				GroupItem? spGroupItem = pContainer as GroupItem;
				if (spGroupItem != null)
				{
					// See if the dragged container is within the group.
					int groupIndex = -1;
					uint groupLeftIndex = 0;
					uint groupSize = 0;
					bool foundGroup = false;

					groupIndex = spGroupItem.GetGroupIndex();

					if (groupIndex >= 0)
					{
						GetGroupInformation(
							(uint)groupIndex,
							out groupLeftIndex,
							out groupSize,
							out foundGroup);
					}

					// We expect the given GroupItem to still be in our collection.
					// If this isn't the case, let the container be recycled.
					global::System.Diagnostics.Debug.Assert(foundGroup);
					if (foundGroup)
					{
						int draggedItemIndex = 0;
						draggedItemIndex = IndexFromContainer((ListViewBaseItem)m_tpPrimaryDraggedContainer);

						// Check to see if the item is in the group.
						pResult = ((uint)draggedItemIndex >= groupLeftIndex) && ((uint)draggedItemIndex < groupLeftIndex + groupSize);
					}
				}
			}
		}

		return pResult;
	}

	// Returns TRUE if a drag and drop is in progress.
	internal bool IsInDragDrop() => m_tpPrimaryDraggedContainer != null;

	// If a drag and drop operation is in progress, returns the number of items being dragged.
	// Otherwise, returns 0.
	internal int DragItemsCount()
	{
		global::System.Diagnostics.Debug.Assert((m_dragItemsCount != 0) == IsInDragDrop());
		return (int)m_dragItemsCount;
	}

	// Called when the user drags into this ListViewBase.
	protected override void OnDragEnter(
		DragEventArgs e)
	{
		bool dropCausesReorder = false;

		ArgumentNullException.ThrowIfNull(e);

		m_isDraggingOverSelf = (m_tpPrimaryDraggedContainer != null);

		dropCausesReorder = DropCausesReorder();
		if (dropCausesReorder)
		{
			DragUIOverride spDragUIOverride;

			spDragUIOverride = e.DragUIOverride;

			// by default ListView doesn't show a glyph and a caption
			if (spDragUIOverride != null)
			{
				spDragUIOverride.IsCaptionVisible = false;
				spDragUIOverride.IsGlyphVisible = false;
			}

			// isHandled == True means that we are reordering
			e.Handled = true;
		}

		UpdateDropTargetDropEffect(false, false /*isLeaving*/);
		base.OnDragEnter(e);
		AutomationPeer.RaiseEventIfListener(this, AutomationEvents.DragEnter);
	}

	// Called when the user drags out of this ListViewBase.
	protected override void OnDragLeave(
		DragEventArgs e)
	{
		bool dropCausesReorder = false;

		ArgumentNullException.ThrowIfNull(e);

		dropCausesReorder = DropCausesReorder();
		if (dropCausesReorder)
		{
			e.Handled = true;
		}

		SetPendingAutoPanVelocity(PanVelocity.Stationary());

		base.OnDragLeave(e);

		// stop the timer
		StopLiveReorderTimer();

		// reset live reorder variables
		ResetAllItemsForLiveReorder();

		m_isDraggingOverSelf = false;
		AutomationPeer.RaiseEventIfListener(this, AutomationEvents.DragLeave);
		UpdateDropTargetDropEffect(false, true /*isLeaving*/);
	}

	// Processes a drag over event at the given position (relative to the panel).
	private bool ProcessDragOverAt(
		Point dragPoint)
	{
		bool pHandled = false;
		bool isHandled = false;

		// only play live reoder if DragOverItem is null and the accepted operation is not null
		// if the item is not null, that means we are in a folder scenario
		bool playLiveReorder = false;

		playLiveReorder = ShouldPlayLiveReorder();

		if (playLiveReorder)
		{
			Panel? spItemsHost;
			IItemLookupPanel? spItemsHostAsItemLookupPanel;

			spItemsHost = ItemsPanelRoot;
			spItemsHostAsItemLookupPanel = spItemsHost as IItemLookupPanel;

			// We need the IItemLookupPanel to do reorder and drop into folder
			if (spItemsHostAsItemLookupPanel != null)
			{
				int draggedIndex = 0;
				int insertionIndex = -1;
				int dragOverIndex = -1;
				int previousDragOverIndex = m_liveReorderIndices.draggedOverIndex;
				int itemsCount = 0;
				Primitives.ElementInfo closestElementInfo = new() { m_childIndex = -1, m_childIsHeader = false };

				closestElementInfo = spItemsHostAsItemLookupPanel.GetClosestElementInfo(dragPoint);

				// save the dragOverIndex
				dragOverIndex = closestElementInfo.m_childIndex;

				itemsCount = GetItemsCount();

				// if Reordering from within the list, this will not be null and we can get the dragged index
				if (m_tpPrimaryDraggedContainer != null)
				{
					DependencyObject spDraggedContainerAsDO;

					spDraggedContainerAsDO = m_tpPrimaryDraggedContainer;
					draggedIndex = IndexFromContainer(spDraggedContainerAsDO);
				}
				else
				{
					// If Dragging over from outside the ListView, set the draggedIndex to be the size of the elements
					draggedIndex = itemsCount;
				}

				// this is the first time we enter this code
				if (previousDragOverIndex == -1)
				{
					previousDragOverIndex = draggedIndex;
				}

				// get the index of insertion in the panel
				insertionIndex = spItemsHostAsItemLookupPanel.GetInsertionIndex(dragPoint);

				if (draggedIndex == itemsCount && insertionIndex == itemsCount - 1)
				{
					ListViewBaseItem? spLastElementAsLVBI;
					DependencyObject? spLastElementAsDO;

					spLastElementAsDO = ContainerFromIndex(insertionIndex);
					spLastElementAsLVBI = spLastElementAsDO as ListViewBaseItem;

					if (spLastElementAsLVBI != null)
					{
						bool inBottomHalf = false;
						Point dragPointRelativeToLastItem = new(0, 0);
						Orientation panelLogicalOrientation = Orientation.Vertical;

						GeneralTransform spTransform;
						UIElement? spItemsHostAsUIE = spItemsHost;

						// Get the dragpoint position relative to the item
						// First get the item's location relative to the panel
						// Then calculate the difference to get the location relative to the item
						spTransform = spLastElementAsLVBI.TransformToVisual(spItemsHostAsUIE);
						dragPointRelativeToLastItem = spTransform.TransformPoint(dragPointRelativeToLastItem);

						dragPointRelativeToLastItem.X = dragPoint.X - dragPointRelativeToLastItem.X;
						dragPointRelativeToLastItem.Y = dragPoint.Y - dragPointRelativeToLastItem.Y;

						// Get the panel's logical orientation
						// We need this to know the orientation of the folder's regions
						panelLogicalOrientation = GetPanelOrientation();

						// Check to see if the item should go into a DragOver state
						inBottomHalf = spLastElementAsLVBI.IsInBottomHalfForExternalReorder(dragPointRelativeToLastItem, panelLogicalOrientation);

						if (inBottomHalf)
						{
							insertionIndex = itemsCount;
						}
					}
				}

				if (insertionIndex == itemsCount)
				{
					dragOverIndex = itemsCount;
				}
				else
				{
					// get the drag over index
					// this function returns the right dragover index based on the direction of the drag
					dragOverIndex = MovedItems.GetDragOverIndex(dragOverIndex, insertionIndex, previousDragOverIndex);
				}

				// save all the information
				m_liveReorderIndices = new(draggedIndex, dragOverIndex, itemsCount);

				// we only want to reset/start a new timer when there is a new dragOverIndex
				// this will allow the LiveReorder to happen when the drag is happening in the Reorder zone
				// note that folder/drop-into scenario is handled in ListViewBaseItem
				if (previousDragOverIndex == draggedIndex || previousDragOverIndex != dragOverIndex)
				{
					StartLiveReorderTimer();
				}

				isHandled = true;
			}
		}

		UpdateDropTargetDropEffect(false, false /*isLeaving*/);

		// return the handled value
		pHandled = isHandled;

		return pHandled;
	}

	// Called when the user drags over this ListViewBase.
	protected override void OnDragOver(
		DragEventArgs e)
	{
		bool isHandled = false;

		ArgumentNullException.ThrowIfNull(e);

		m_dragAcceptedOperation = DataPackageOperation.None;

		// We want the developer to handle the drag over first to set the AcceptedOperation
		base.OnDragOver(e);

		m_dragAcceptedOperation = e.AcceptedOperation;

		isHandled = e.Handled;

		if (!isHandled)
		{
			Panel? spItemsHost;

			spItemsHost = ItemsPanelRoot;
			if (spItemsHost != null)
			{
				bool dropCausesReorder = false;
				PanVelocity panVelocity;
				Point dragPointRelativeToLVB = new(0, 0);
				UIElement spItemsHostAsUIE;

				spItemsHostAsUIE = spItemsHost;

				m_lastDragOverPoint = e.GetPosition(spItemsHostAsUIE);
				isHandled = ProcessDragOverAt(m_lastDragOverPoint);
				e.Handled = isHandled;

				dropCausesReorder = DropCausesReorder();

				if (dropCausesReorder)
				{
					// isHandled == True means that we are reordering
					e.AcceptedOperation = DataPackageOperation.Move;
				}

				// See what our edge scrolling action should be...
				dragPointRelativeToLVB = e.GetPosition(this);
				panVelocity = ComputeEdgeScrollVelocity(dragPointRelativeToLVB);
				// And request it.
				SetPendingAutoPanVelocity(panVelocity);
			}
		}
	}

	// Called when the user drops onto this ListViewBase.
	protected override void OnDrop(
		DragEventArgs e)
	{
		bool isHandled = false;

		ArgumentNullException.ThrowIfNull(e);

		isHandled = e.Handled;

		if (!isHandled)
		{
			bool dropCausesReorder = false;

			dropCausesReorder = DropCausesReorder();
			if (dropCausesReorder)
			{
				Panel spItemsHost;
				UIElement spItemsHostAsUIE;
				Point dropPointRelativeToPanel = new(0, 0);

				spItemsHost = ItemsPanelRoot!;
				spItemsHostAsUIE = spItemsHost;
				dropPointRelativeToPanel = e.GetPosition(spItemsHostAsUIE);
				OnReorderDrop(dropPointRelativeToPanel);

				e.AcceptedOperation = DataPackageOperation.Move;
				e.Handled = true;
			}
			else
			{
				ResetAllItemsForLiveReorder();
			}
		}

		base.OnDrop(e);

		if (m_tpDragOverItem != null)
		{
			// Reset the dragged-over item and potentially switch to the pointer-over visual state.
			((ListViewBaseItem)m_tpDragOverItem).LeaveDragOver(this, e);
		}

		CompleteDrop();
	}

	// Cancels the current drag/drop operation.
	private protected void CancelDrag()
	{
		if (IsInDragDrop())
		{
			NotifyDragVisualReleased();
#if !HAS_UNO // TODO Uno: no core DragDrop service.
			DragDrop dragDrop = DXamlCore.GetCurrent().GetDragDrop();
			if (dragDrop.GetIsDragDropInProgress())
			{
				dragDrop.DragCancel(true /* shouldFireEvent */);
			}
#endif
			UpdateDragGrabbedItems(false);
			UpdateDropTargetDropEffect(false, true /*isLeaving*/);
			CompleteDrop();
		}
	}

	// Returns TRUE if reordering is active, meaning:
	// * CanReorderItems is TRUE
	// * IsGrouping is FALSE
	// * ItemsHost is an IItemLookupPanel
	private bool IsItemReorderingActive()
	{
		bool pResult = false;

		bool canReorderItems = false;
		canReorderItems = CanReorderItems;

		// TODO Uno: TreeViewList (IsCustomReorder) runs its own reorder.
		if (canReorderItems && !IsCustomReorder)
		{
			pResult = IsNotGroupingAndHostIsLookupPanel();
		}

		return pResult;
	}

	// Returns TRUE if:
	// * IsGrouping is FALSE
	// * ItemsHost is an IItemLookupPanel
	private bool IsNotGroupingAndHostIsLookupPanel()
	{
		bool pResult = false;
		bool isGrouping = false;

		isGrouping = IsGrouping;

		if (!isGrouping)
		{
			// To match pointer reordering, only allow the action if our panel is an IItemLookupPanel,
			// even though it's technically possible to do keyboard reordering without this interface.
			Panel? spItemsHost;
			spItemsHost = ItemsPanelRoot;
			pResult = spItemsHost is IItemLookupPanel;
		}

		return pResult;
	}

	// Returns TRUE if reordering is enabled. This means CanReorderItems is TRUE,
	// AllowDrop is TRUE, we're dragging over ourselves, we're not grouping, and our ItemsHost is an IItemLookupPanel.
	private bool DropCausesReorder()
	{
		bool pResult = false;

		pResult = false;

		if (m_isDraggingOverSelf)
		{
			bool allowDrop = false;
			allowDrop = AllowDrop;
			if (allowDrop)
			{
				pResult = IsItemReorderingActive();
			}
		}

		return pResult;
	}

	// Moves the currently dragged items to the given point.
	// Depends on ItemsHost being an IItemLookupPanel.
	//  dropPoint   - The point where reordered items should be inserted, relative to this ListViewBase.
	private protected void OnReorderDrop(
		Point dropPoint)
	{
		Panel? spItemsHost;
		IItemLookupPanel? spItemsHostAsItemLookupPanel;

		spItemsHost = ItemsPanelRoot;
		spItemsHostAsItemLookupPanel = spItemsHost as IItemLookupPanel;

		// We need the IItemLookupPanel interface in order to find the reorder insertion index.
		if (spItemsHostAsItemLookupPanel != null)
		{
			// The index at which reordered items should be inserted.
			int insertIndex = -1;
			int draggedItemIndex = -1;
			int focusedIndex = GetFocusedIndex();

			// The indexes of the items we'll be reordering.
			List<uint> reorderItemIndexes = new();

			GetDraggedItems((ListViewBaseItem)m_tpPrimaryDraggedContainer!, false /* wantItems */, out _, reorderItemIndexes);

			insertIndex = GetInsertionIndexForLiveReorder();
			m_liveReorderIndices = new(-1, -1, -1);

			// insertIndex can be -1 if either the app is a Pre-Redstone app
			// or a Redstone app with the drop happening right after an edge scroll
			// but before we update the indices to play the live reorder
			if (insertIndex == -1)
			{
				// Ask our ItemsHost where the dropped items should go.
				insertIndex = spItemsHostAsItemLookupPanel.GetInsertionIndex(dropPoint);
			}

			// ReorderItemsTo expects the index list to be sorted in increasing order. Do that.
			reorderItemIndexes.Sort();

			// Get the index of item being dragged
			draggedItemIndex = IndexFromContainer((ListViewBaseItem)m_tpPrimaryDraggedContainer!);

			// Dragged item should be realized
			global::System.Diagnostics.Debug.Assert(draggedItemIndex >= 0);

			// Store the last reorder position so we can animate the dropped item into position.
			m_lastReorderPosition = m_lastDragPosition;

			// Do the reorder.
			ReorderItemsTo(
				reorderItemIndexes,
				insertIndex,
				ref draggedItemIndex,
				ref focusedIndex);

			// Force a layout so ScrollIntoView knows where to scroll to.
			UpdateLayout();

			// Check if one of the dragged items is focused.
			// Note, search is performed on index BEFORE update.
			if (focusedIndex >= 0 &&
				reorderItemIndexes.BinarySearch((uint)GetFocusedIndex()) >= 0)
			{
				// If focused item was dragged, set the focus and scroll it into view.
				// Since this code is only called for pointer input, we can set the FocusState appropriately
				SetFocusedItem(
					focusedIndex,
					true, // shouldScrollIntoView
					true, // forceFocus
					FocusState.Pointer);
			}
			else
			{
				if (draggedItemIndex >= 0)
				{
					// Otherwise scroll to the item that was being dragged.
					base.ScrollIntoView(
						(uint)draggedItemIndex,
						false, // isGroupItemIndex
						false, // isHeader
						false, // isFooter
						false, // isFromPublicAPI
						true,  // ensureContainerRealized
						false, // animateIfBringIntoView
						ScrollIntoViewAlignment.Default);
				}

				if (focusedIndex >= 0)
				{
					// ...and adjust focus to be on the right container, as indices are not updated.
					// This is a lower risk fix to an issue where focus is not adjusted after collection change.
					// A more comprehensive fix would be to update it in collection change notification in Selector.
					SetFocusedItem(focusedIndex, false);
				}
			}
		}
	}

	// Helper method for updating tracked index
	private static void UpdateTrackedIndex(
		int realItemIndex,
		int insertIndex,
		ref int pTrackedIndex)
	{
		if (pTrackedIndex >= 0)
		{
			if (pTrackedIndex == realItemIndex)
			{
				pTrackedIndex = insertIndex;
			}
			// tracked index is between source and insert index
			// Moving before to after
			else if (
				(realItemIndex < pTrackedIndex) &&
				(insertIndex >= pTrackedIndex))
			{
				pTrackedIndex--;
			}
			// Moving after to before
			else if (
				(realItemIndex > pTrackedIndex) &&
				(insertIndex <= pTrackedIndex))
			{
				pTrackedIndex++;
			}
		}
	}

	// Reorder the given list of items (given as a list of indexes) to the given index.
	// Items are inserted in their original order.
	// Optionally, pTrackedIndex can be specified. It should hold the index of an item you're
	// interested in tracking. When the function returns successfully, pTrackedIndex will be updated
	// with the new index location of the item originally at *pTrackedIndex.
	private void ReorderItemsTo(
		List<uint> pReorderItemIndexes,
		int insertIndex,
		ref int pTrackedIndex1,
		ref int pTrackedIndex2)
	{
		IList<object>? spItemSourceVector = null;
		IList? spItemSourceBindableVector = null;
		object? spItemsSource;

		ArgumentNullException.ThrowIfNull(pReorderItemIndexes);
		if (insertIndex < 0)
		{
			throw new InvalidOperationException();
		}

		// Let our ItemCollection change handler know we're doing a reorder now. This prevents the handler
		// from tearing down fields we need to complete the reorder.
		m_isInItemReorderAfterDrop = true;

		try
		{
			// Get our list of items as a mutable vector. This is usually from ItemsSource, but may be Items if ItemsSource is NULL.
			spItemsSource = ItemsSource;
			if (spItemsSource != null)
			{
				spItemSourceVector = spItemsSource as IList<object>;
				if (spItemSourceVector == null)
				{
					// IBindableVector is v-table compatible with IVector<IInspectable *>, so this reinterpret cast is safe
					spItemSourceBindableVector = spItemsSource as IList;
				}
			}
			else
			{
				ItemCollection spItems;
				spItems = Items;
				spItemSourceVector = spItems;
			}

			if (spItemSourceVector != null || spItemSourceBindableVector != null)
			{
				// We need to move the dragged items to where the user dropped,
				// while preserving the original order of the dragged items.

				// The number of items that have been reordered so far.
				// We need this to account for indexes shifting during reorder.
				int numItemsReorderedSoFar = 0;

				// Increment the Reorder Counter (used for content animations).
				OnItemsReordered((uint)pReorderItemIndexes.Count);

				// Iterate over the dragged items in reverse index order. Move items as required,
				// keeping track of the insertion index as it changes. We also account for the fact that
				// the real indexes of items referred to in pReorderItemIndexes will shift as we move items
				// around.
				for (int it = pReorderItemIndexes.Count - 1; it >= 0; --it)
				{
					int realItemIndex = (int)pReorderItemIndexes[it];

					if (realItemIndex < insertIndex)
					{
						// Our insert index will change if we move an item to the insertion index
						// from a lower-index location.
						insertIndex--;
					}
					else
					{
						// If we're reordering an item from after the insertion index,
						// we must account for the shift of indexes due to previous iterations
						// of this loop. Remember, we're going in reverse index order.
						realItemIndex += numItemsReorderedSoFar;
					}

					// Reorder implemented as remove + insert.
					// TODO/REVISIT: This can be slow if the underlying
					// data source is array-based. We can't really detect this though,
					// since the data is provided externally.
					if (insertIndex != realItemIndex)
					{
						// Whether or not the item we're reordering is selected.
						bool isCurrentReorderedItemSelected = false;

						// Not used. The index of the item we're reordering in the selected items list, if applicable.
						uint currentReorderedItemIndexInSelection = 0;

						// The item we're reordering.
						object? spCurrentItem;

						// Update tracked indices, if necessary.
						UpdateTrackedIndex(
							realItemIndex,
							insertIndex,
							ref pTrackedIndex1);

						UpdateTrackedIndex(
							realItemIndex,
							insertIndex,
							ref pTrackedIndex2);

						m_selection.Has((uint)realItemIndex, out currentReorderedItemIndexInSelection, out isCurrentReorderedItemSelected);
						spCurrentItem = spItemSourceVector != null ? spItemSourceVector[realItemIndex] : spItemSourceBindableVector![realItemIndex];
						if (spItemSourceVector != null)
						{
							spItemSourceVector.RemoveAt(realItemIndex);
							spItemSourceVector.Insert(insertIndex, spCurrentItem!);
						}
						else
						{
							spItemSourceBindableVector!.RemoveAt(realItemIndex);
							spItemSourceBindableVector.Insert(insertIndex, spCurrentItem);
						}

						// This remove-then-insert step will clear any selection on the current item.
						// If this item was selected, we need to re-select it.
						if (isCurrentReorderedItemSelected)
						{
							// call the Selector::SelectOneItemInternal
							SelectOneItemInternal(insertIndex, spCurrentItem, true /* clearOldSelection */);
						}
					}

					numItemsReorderedSoFar++;
				}
			}
		}
		finally
		{
			m_isInItemReorderAfterDrop = false;
		}
	}

	// React to a drag visual being created. The given visual is set to
	// m_tpDragDropVisual. Will result in error if there is already a drag
	// in progress.
	private void NotifyDragVisualCreated(
		object pDragVisual)
	{
		// Only one drag/drop should happen at any given time.
		global::System.Diagnostics.Debug.Assert(m_tpDragDropVisual == null);
		if (m_tpDragDropVisual != null)
		{
			throw new InvalidOperationException();
		}

		ArgumentNullException.ThrowIfNull(pDragVisual);

		m_tpDragDropVisual = pDragVisual;
	}

	// Release m_tpDragDropVisual.
	private void NotifyDragVisualReleased() => m_tpDragDropVisual = null;

	// Creates a blank IDataPackage for use in drag and drop.
	private DataPackage GetBlankDataPackage() => new DataPackage();

	// Returns TRUE if the current interaction mode requires drag and drop.
	// This is the case if CanDragItems == TRUE, or CanReorderItems == TRUE
	// unless we're grouping.
	internal bool GetIsDragEnabled()
	{
		bool pDragEnabled = false;
		bool reorderActive = false;
		bool canDragItems = false;
		bool canReorderItems = false;
		bool isGrouping = false;

		pDragEnabled = false;

		isGrouping = IsGrouping;

		canDragItems = CanDragItems;
		canReorderItems = CanReorderItems;

		// Ignore canReorderItems if we're grouping.
		reorderActive = canReorderItems && !isGrouping;
		pDragEnabled = (canDragItems || reorderActive);

		return pDragEnabled;
	}

	// Returns the edge scrolling velocity we should be performing.
	// dragPoint - The point over which the user is dragging, relative to the ListViewBase.
	private PanVelocity ComputeEdgeScrollVelocity(
		Point dragPoint)
	{
		bool isHorizontalScrollAllowed = false;
		bool isVerticalScrollAllowed = false;
		PanVelocity velocity = default;
		PanVelocity pVelocity;

		pVelocity = default;

		// See in which directions we've enabled panning.
		if (m_tpScrollViewer != null)
		{
			ScrollMode verticalScrollMode = ScrollMode.Disabled;
			ScrollMode horizontalScrollMode = ScrollMode.Disabled;

			verticalScrollMode = m_tpScrollViewer.VerticalScrollMode;
			horizontalScrollMode = m_tpScrollViewer.HorizontalScrollMode;

			isVerticalScrollAllowed = verticalScrollMode != ScrollMode.Disabled;
			isHorizontalScrollAllowed = horizontalScrollMode != ScrollMode.Disabled;
		}

		if (isHorizontalScrollAllowed)
		{
			double offset = 0.0;
			double bound = 0.0;

			offset = m_tpScrollViewer!.HorizontalOffset;

			// Try scrolling left.
			velocity.HorizontalVelocity = ComputeEdgeScrollVelocityFromEdgeDistance(dragPoint.X);
			if (velocity.IsStationary())
			{
				// Try scrolling right.
				double width = 0;
				width = ActualWidth;
				velocity.HorizontalVelocity = -ComputeEdgeScrollVelocityFromEdgeDistance(width - dragPoint.X);
				bound = m_tpScrollViewer.ScrollableWidth;
			}
			else
			{
				// We're scrolling to the left.
				// The minimum horizontal offset obtained here accounts for the presence of the
				// sentinel offset values in the left padding and/or header case. For instance,
				// with no header or padding this will return exactly 2.0.
				bound = m_tpScrollViewer.MinHorizontalOffset;
			}

			// Disallow edge scrolling if we're right up against the edge.
			if (DoubleUtil.AreWithinTolerance(bound, offset, ScrollViewerScrollRoundingTolerance))
			{
				velocity.Clear();
			}
		}

		if (isVerticalScrollAllowed && velocity.IsStationary() /* only allow vertical edge scrolling if there is no horizontal edge scrolling */)
		{
			double offset = 0.0;
			double bound = 0.0;
			offset = m_tpScrollViewer!.VerticalOffset;

			// Try scrolling up.
			velocity.VerticalVelocity = ComputeEdgeScrollVelocityFromEdgeDistance(dragPoint.Y);
			if (velocity.IsStationary())
			{
				// Try scrolling down.
				double height = 0;
				height = ActualHeight;
				velocity.VerticalVelocity = -ComputeEdgeScrollVelocityFromEdgeDistance(height - dragPoint.Y);
				bound = m_tpScrollViewer.ScrollableHeight;
			}
			else
			{
				// We're scrolling up.
				// The minimum vertical offset obtained here accounts for the presence of the
				// sentinel offset values in the top padding and/or header case. For instance,
				// with no header or padding this will return exactly 2.0.
				bound = m_tpScrollViewer.MinVerticalOffset;
			}

			// Disallow edge scrolling if we're right up against the edge.
			if (DoubleUtil.AreWithinTolerance(bound, offset, ScrollViewerScrollRoundingTolerance))
			{
				velocity.Clear();
			}
		}

		pVelocity = velocity;

		return pVelocity;
	}

	// Computes the speed of an edge scroll, given a distance from the edge.
	private float ComputeEdgeScrollVelocityFromEdgeDistance(
		double distanceFromEdge)
	{
		if (distanceFromEdge <= LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX)
		{
			// Linear velocity gradient.
			// 0 distance:                                      LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED
			// LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX distance: LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED
			return (float)(LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED -
							(distanceFromEdge / LISTVIEWBASE_EDGE_SCROLL_EDGE_WIDTH_PX) * (LISTVIEWBASE_EDGE_SCROLL_MAX_SPEED - LISTVIEWBASE_EDGE_SCROLL_MIN_SPEED));
		}
		else
		{
			return 0;
		}
	}

	// Implements the delay-start, but instant-update behavior for edge scrolling.
	// If the given velocity is zero, immediately stop edge scrolling.
	// If there isn't a currently running edge scroll, start a timer. When that timer
	// completes, call ScrollWithVelocity with the given arguments.
	// If there is a currently running edge scroll, change the velocity immediately.
	private void SetPendingAutoPanVelocity(
		PanVelocity velocity)
	{
		if (!velocity.IsStationary())
		{
			if (m_currentAutoPanVelocity.IsStationary())
			{
				// Means we need to start a timer,
				// or we're updating the pending velocity.
				m_pendingAutoPanVelocity = velocity;
				EnsureStartEdgeScrollTimer();
			}
			else
			{
				// Our velocity is changing.
				m_currentAutoPanVelocity = velocity;
				ScrollWithVelocity(m_currentAutoPanVelocity);
			}
		}
		else
		{
			DestroyStartEdgeScrollTimer();
			m_currentAutoPanVelocity.Clear();
			m_pendingAutoPanVelocity.Clear();
			ScrollWithVelocity(m_currentAutoPanVelocity);
		}
	}

	// Scroll our ScrollViewer with the given velocity.
	private void ScrollWithVelocity(
		PanVelocity velocity)
	{
		if (m_tpScrollViewer != null)
		{
			m_tpScrollViewer.SetConstantVelocities(velocity.HorizontalVelocity, velocity.VerticalVelocity);
		}
	}

	// Instantiate the edge scroll timer, if necessary, and start it.
	private void EnsureStartEdgeScrollTimer()
	{
		if (m_tpStartEdgeScrollTimer == null)
		{
			TimeSpan edgeScrollTimeSpan = TimeSpan.FromMilliseconds(LISTVIEWBASE_EDGE_SCROLL_START_DELAY_MSEC);
			DispatcherTimer spEdgeScrollTimer;

			spEdgeScrollTimer = new DispatcherTimer();
			m_tpStartEdgeScrollTimer = spEdgeScrollTimer;

			m_tpStartEdgeScrollTimer.Tick += StartEdgeScrollTimerTick;
			m_tpStartEdgeScrollTimer.Interval = edgeScrollTimeSpan;
			m_tpStartEdgeScrollTimer.Start();
		}
	}

	// Stop and releas the edge scroll timer.
	private void DestroyStartEdgeScrollTimer()
	{
		try
		{
			if (m_tpStartEdgeScrollTimer != null)
			{
				m_tpStartEdgeScrollTimer.Stop();
			}
		}
		finally
		{
			m_tpStartEdgeScrollTimer = null;
		}
	}

	// The edge scroll timer calls this function when it's time
	// to do our pending edge scroll action.
	private void StartEdgeScrollTimerTick(
		object? pUnused1,
		object? pUnused2)
	{
		DestroyStartEdgeScrollTimer();
		m_currentAutoPanVelocity = m_pendingAutoPanVelocity;
		m_pendingAutoPanVelocity.Clear();
		ScrollWithVelocity(m_currentAutoPanVelocity);
	}

	// Perform a keyboard reorder in the specified direction,
	// originating from the given focused item.
	internal void OnKeyboardReorder(
		KeyboardReorderDirection direction,
		ListViewBaseItem pFocusedItem)
	{
		bool isItemReorderingActive = false;
		Panel? spItemsHost;

		ArgumentNullException.ThrowIfNull(pFocusedItem);
		spItemsHost = ItemsPanelRoot;

		m_lastDragPosition.X = 0;
		m_lastDragPosition.Y = 0;

		isItemReorderingActive = IsItemReorderingActive();

		if (isItemReorderingActive)
		{
			// The indexes of the items we'll be reordering.
			List<uint> reorderItemIndexes = new();

			// Get the indexes to reorder.
			GetDraggedItems(pFocusedItem, false /* wantItems */, out _, reorderItemIndexes);

			if (reorderItemIndexes.Count > 0)
			{
				// The index at which reordered items should be inserted.
				int insertIndex = 0;

				// The index of the item the user is on.
				int keyboardFocusItemIndex = 0;

				// Item that will serve as the physical origin of the reordering animation.
				// If we're reordering forwards, it's the last item being reordered.
				// If we're reordering backwards, it's the first item being reordered.
				int animationOriginItemIndex = 0;

				keyboardFocusItemIndex = IndexFromContainer(pFocusedItem);

				// Sort the indexes in increasing order.
				reorderItemIndexes.Sort();

				if (direction == KeyboardReorderDirection.KeyboardReorderDirection_ToLowerIndex)
				{
					uint front = reorderItemIndexes[0];
					animationOriginItemIndex = (int)front;
					insertIndex = unchecked((int)(front - 1));
				}
				else
				{
					uint back = reorderItemIndexes[reorderItemIndexes.Count - 1];
					animationOriginItemIndex = (int)back;
					insertIndex = (int)(back + 2);
				}

				if (insertIndex >= 0)
				{
					uint itemsCount = 0;
					itemsCount = Items.Size;

					if (itemsCount >= (uint)insertIndex)
					{
						DependencyObject? spAnimationOriginItem;
						UIElement? spAnimationOriginItemAsUIE;

						// Set up the last reorder position to be the top-left of animationOriginItemIndex, so animations
						// look OK.
						spAnimationOriginItem = ContainerFromIndex(animationOriginItemIndex);
						spAnimationOriginItemAsUIE = spAnimationOriginItem as UIElement;
						if (spAnimationOriginItemAsUIE != null)
						{
							// Used to calculate the last reorder position so we can animate the dropped item into position.
							GeneralTransform spToParent;
							Point origin = new(0, 0);
							// Store the last reorder position so we can animate the dropped item into position.
							spToParent = spAnimationOriginItemAsUIE.TransformToVisual(this);
							m_lastReorderPosition = spToParent.TransformPoint(origin);
						}

						// Disable transitions.
						spItemsHost!.IsIgnoringTransitions = true;

						// Do the reorder, and get the new index of the currently focused item and reorder animation origin item.
						int unusedTrackedIndex = -1;
						ReorderItemsTo(reorderItemIndexes, insertIndex, ref keyboardFocusItemIndex, ref unusedTrackedIndex);
						// At this point, pFocusedItem and spAnimationOriginItem are possibly no longer in the tree. So we can't rely upon them.

						// Force a layout so ScrollIntoView knows where to scroll to.
						UpdateLayout();

						// Enable transitions.
						spItemsHost.IsIgnoringTransitions = false;

						// Bring the focused item into view and focus it (since it will have been unfocused by the reorder).
						SetFocusedItem(keyboardFocusItemIndex, true /* shouldScrollIntoView */, true /*forceFocus*/, FocusState.Keyboard);

						//Set up and call into Narrator to read out a reorder string.
						UpdateDropTargetDropEffect(false, false /*isLeaving*/, pFocusedItem);
						AutomationPeer.RaiseEventIfListener(this, AutomationEvents.Dropped);
					}
				}
			}
		}
	}

	private void ToggleBackKeyListenerOnPhone(bool isReorderModeEnabled)
	{
		if (DXamlCore.Current.BackButtonSupported)
		{
			if (isReorderModeEnabled)
			{
				BackButtonIntegration.RegisterListener(this);
			}
			else
			{
				BackButtonIntegration.UnregisterListener(this);
			}
		}
	}

	internal bool OnBackButtonPressed()
	{
		bool pHandled = false;

		// Disable reorder mode when the back button is pressed
		ReorderMode = ListViewReorderMode.Disabled;
		pHandled = true;

		return pHandled;
	}

	internal bool IsCurrentlyReordering()
	{
		bool isReordering = false;
		if (m_isDraggingOverSelf)
		{
			bool canReorder = false;
			canReorder = CanReorderItems;
			isReordering = canReorder;
		}
		return isReordering;
	}

	// Returns TRUE if live reordering is enabled
	// This means CanReorderItem is true and AllowDrop is true
	private bool ShouldPlayLiveReorder()
	{
		bool pResult = false;
		bool allowDrop = false;
		bool canReorder = false;
		bool isStationary = m_currentAutoPanVelocity.IsStationary(); // we're not at the edge scrolling
		bool supportLiveReorder = false;

		pResult = false;

		if (m_tpPrimaryDraggedContainer != null)
		{
			supportLiveReorder = true;
		}
		else
		{
			supportLiveReorder = (m_dragAcceptedOperation != DataPackageOperation.None);
		}

		allowDrop = AllowDrop;
		canReorder = CanReorderItems;

		// TODO Uno: TreeViewList (IsCustomReorder) runs its own reorder.
		pResult = canReorder && allowDrop && isStationary && supportLiveReorder && !IsCustomReorder;

		return pResult;
	}

	// Returns whether the passed item is the one being dragged over
	internal bool IsDragOverItem(
		SelectorItem pItem)
	{
		return ReferenceEquals(pItem, m_tpDragOverItem);
	}

	// Returns whether the ListViewBase was the source of the drag
	public bool IsDragSource()
	{
		bool pReturnValue = false;
		pReturnValue = (m_tpPrimaryDraggedContainer != null);

		return pReturnValue;
	}

	private void StartLiveReorderTimer()
	{
		TimeSpan timeSpan = default;

		// make sure we stop the current timer if it exists
		StopLiveReorderTimer();

		// make sure we have a timer
		EnsureLiveReorderTimer();

		// set the timer duration
		timeSpan = TimeSpan.FromMilliseconds(this is GridView ? GRIDVIEW_LIVEREORDER_TIMER : LISTVIEW_LIVEREORDER_TIMER);

		// set the interval
		m_tpLiveReorderTimer!.Interval = timeSpan;

		// start the timer
		m_tpLiveReorderTimer.Start();
	}

	private void EnsureLiveReorderTimer()
	{
		// if it hasn't been created yet, create it
		if (m_tpLiveReorderTimer == null)
		{
			DispatcherTimer spNewDispatcherTimer;

			spNewDispatcherTimer = new DispatcherTimer();

			// attach the handler
			void handler(object? pUnused1, object pUnused2) => LiveReorderTimerTickHandler();

			spNewDispatcherTimer.Tick += handler;
			m_epLiveReorderTimerEvent.Disposable = Disposable.Create(() => spNewDispatcherTimer.Tick -= handler);

			m_tpLiveReorderTimer = spNewDispatcherTimer;
		}
	}

	private void StopLiveReorderTimer()
	{
		if (m_tpLiveReorderTimer != null)
		{
			m_tpLiveReorderTimer.Stop();
		}
	}

	// Handler for the Tick event on m_tpLiveReorderTimer
	private void LiveReorderTimerTickHandler()
	{
		// stop the timer
		StopLiveReorderTimer();

		if (IsInLiveTree)
		{
			// We started the timer because we had a dragged item.
			global::System.Diagnostics.Debug.Assert(m_liveReorderIndices.draggedItemIndex != -1);

			List<MovedItem> newItems = new();
			List<MovedItem> newItemsToMove = new();
			List<MovedItem> oldItemsToMoveBack = new();

			Orientation panelLogicalOrientation = Orientation.Vertical;

			// Get the panel's logical orientation
			// We need this to know the orientation of the items
			panelLogicalOrientation = GetPanelOrientation();

			// get the new items
			GetNewMovedItemsForLiveReorder(newItems);

			// update the MovedItems
			m_movedItems.Update(panelLogicalOrientation == Orientation.Vertical /* isOrientationVertical */, newItems, newItemsToMove, oldItemsToMoveBack);

			// move items back to their original location
			MoveItemsForLiveReorder(false /* areNewItems */, oldItemsToMoveBack);

			// move the new items to their destination
			MoveItemsForLiveReorder(true /* areNewItems */, newItemsToMove);
		}
	}

	private void GetNewMovedItemsForLiveReorder(
		List<MovedItem> newItems)
	{
		int startIndex = m_liveReorderIndices.draggedItemIndex;
		int endIndex = m_liveReorderIndices.draggedOverIndex;
		int increment = (startIndex < endIndex) ? 1 : -1;

		newItems.Clear();

		for (int i = startIndex; i != endIndex; i += increment)
		{
			int targetIndex = i - increment;

			if (i == startIndex)
			{
				targetIndex = -1;
			}

			AddNewItemForLiveReorder(i, targetIndex, newItems);
		}

		// add the endIndex
		AddNewItemForLiveReorder(endIndex, endIndex - increment, newItems);
	}

	private void AddNewItemForLiveReorder(
		int sourceIndex,
		int targetIndex,
		List<MovedItem> newItems)
	{
		int itemsCount = m_liveReorderIndices.itemsCount;

		Rect sourceRect = default;
		Rect targetRect = default;

		// source information
		// in the case where we're dragging an element from outside the list
		// sourceIndex will be equal to the items' count
		if (sourceIndex != itemsCount)
		{
			sourceRect = GetLayoutSlot(sourceIndex);
		}

		// destination information
		// if targetIndex is -1, then this is the element being dragged
		// and we don't need to know its target location since it will be faded out
		// LiveReorderHelper requires us to set the target to -1 for the dragged item
		// and ignores the targetRect in that case
		// if targetIndex is the itemsCount then the drag happened from outside the list
		if (targetIndex != -1 && targetIndex != itemsCount)
		{
			targetRect = GetLayoutSlot(targetIndex);
		}

		// add it to the collection
		newItems.Add(new MovedItem(sourceIndex, targetIndex, sourceRect, targetRect));
	}

	private void ResetAllItemsForLiveReorder()
	{
		StopLiveReorderTimer();

		foreach (MovedItem itr in m_movedItems)
		{
			if (itr.destinationIndex != -1)
			{
				DependencyObject? spCurrentElementAsDO;
				spCurrentElementAsDO = ContainerFromIndex(itr.sourceIndex);

				if (spCurrentElementAsDO != null)
				{
					((ListViewBaseItem)spCurrentElementAsDO).Arrange(itr.sourceRect);
				}
			}
		}

		m_movedItems.Clear();
		m_liveReorderIndices = new(-1, -1, -1);
	}

	private void MoveItemsForLiveReorder(
		bool areNewItems,
		List<MovedItem> newItemsToMove)
	{
		Rect sourceRect = default;
		Rect destinationRect = default;
		DependencyObject? spCurrentElementAsDO;

		foreach (MovedItem movedItem in newItemsToMove)
		{
			spCurrentElementAsDO = ContainerFromIndex(movedItem.sourceIndex);

			if (spCurrentElementAsDO != null)
			{
				if (areNewItems)
				{
					sourceRect = movedItem.sourceRect;
					destinationRect = movedItem.destinationRect;
				}
				else
				{
					sourceRect = movedItem.destinationRect;
					destinationRect = movedItem.sourceRect;
				}

				((ListViewBaseItem)spCurrentElementAsDO).Arrange(destinationRect);
			}
		}
	}

	private Rect GetLayoutSlot(
		int itemIndex)
	{
		Rect pLayoutSlot = default;
		DependencyObject? spItemAsDO;
		spItemAsDO = ContainerFromIndex(itemIndex);

		if (spItemAsDO != null)
		{
			FrameworkElement spItemAsFE;

			spItemAsFE = (FrameworkElement)spItemAsDO;
			pLayoutSlot = LayoutInformation.GetLayoutSlot(spItemAsFE);
		}

		return pLayoutSlot;
	}

	private int GetItemsCount()
	{
		uint itemsCount = 0;
		ItemCollection spItems;

		spItems = Items;
		itemsCount = spItems.Size;

		return (int)itemsCount;
	}

	internal Orientation GetPanelOrientation()
	{
		// Get the panel's logical orientation
		// We need this to know the orientation of the folder's regions
		// TODO Uno: GetPanelOrientations does not exist, ItemsStackPanel and WrapPanel don't implement IOrientedPanel yet.
		return ItemsPanelRoot switch
		{
			ItemsStackPanel itemsStackPanel => itemsStackPanel.Orientation,
			ItemsWrapGrid itemsWrapGrid => itemsWrapGrid.Orientation,
			WrapPanel wrapPanel => wrapPanel.Orientation,
			_ => GetItemsHostOrientations().LogicalOrientation,
		};
	}

	internal void SetDragOverItem(
		ListViewBaseItem? dragOverItem)
	{
		if (dragOverItem == null)
		{
			m_tpDragOverItem = null;
		}
		else
		{
			m_tpDragOverItem = dragOverItem;
		}
	}

	private protected override Rect OverrideContainerArrangeBounds(
		int index,
		Rect suggestedBounds)
	{
		// by default, we use the suggested bounds
		Rect newBounds = suggestedBounds;

		if (IsInLiveReorder())
		{
			// search for the item in the LiveReorder movedItems array
			// if we find it, we will override the bounds that were passed
			// with the new ones for LiveReorder
			foreach (MovedItem itr in m_movedItems)
			{
				// destinationIndex == -1 means that this is the item being dragged -> do not update the bounds
				if (itr.sourceIndex == index && itr.destinationIndex != -1)
				{
					newBounds = itr.destinationRect;
					break;
				}
			}
		}

		return newBounds;
	}
}

