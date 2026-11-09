// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial_SemanticZoomInformation.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d


#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

// TODO Uno: SemanticZoom is not implemented, so the bodies below stay in place but do not compile.
public partial class ListViewBase
{
	// Prepare the view for a zoom transition.
	public void InitializeViewChange()
	{
#if !HAS_UNO // TODO Uno: SemanticZoom is not implemented.
		SemanticZoom? spSemanticZoomOwner;

		// block scrollbars from showing during sezo operation
		if (m_tpScrollViewer is not null)
		{
			m_tpScrollViewer.BlockIndicatorsFromShowing();
		}

		// need to precalculate the focusstate our item is going to be in
		// since the sezo will lose the trigger state soon
		spSemanticZoomOwner = SemanticZoomOwner;

		if (spSemanticZoomOwner is not null)
		{
			SemanticZoom spSemanticZoomOwnerConcrete = spSemanticZoomOwner;
			m_semanticZoomCompletedFocusState = FocusState.Programmatic;

			if (spSemanticZoomOwnerConcrete.GetIsProcessingKeyboardInput() ||
				spSemanticZoomOwnerConcrete.GetIsProcessingPointerInput())
			{
				// will set focus to the destination element using either keyboard or pointer focus depending on what
				// triggered the statechange.
				m_semanticZoomCompletedFocusState =
					spSemanticZoomOwnerConcrete.GetIsProcessingKeyboardInput() ?
					FocusState.Keyboard :
					FocusState.Pointer;
			}
		}
#endif
	}

	// Cleanup the view after a zoom transition.
	public void CompleteViewChange()
	{
#if !HAS_UNO // TODO Uno: SemanticZoom is not implemented.
		// unblock scrollbars from showing during sezo operation
		if (m_tpScrollViewer is not null)
		{
			m_tpScrollViewer.ResetBlockIndicatorsFromShowing();
		}
#endif
	}

	// Forces content to scroll until the coordinate space of the SemanticZoomLocation is
	// visible.
	public void MakeVisible(
		SemanticZoomLocation item)
	{
#if !HAS_UNO // TODO Uno: SemanticZoom is not implemented.
		object? spItem;
		SemanticZoomLocation spSemanticLocation = item;
		bool isJumpListEnabled = false;
		bool isZoomedInView = false;

		SemanticZoom? spSemanticZoomOwner;
		spSemanticZoomOwner = SemanticZoomOwner;
		if (spSemanticZoomOwner is not null)
		{
			isJumpListEnabled = true;

			// For threshold, the jumplist behavior of aligning the items in zoomed out view and zoomed in view
			// is the converged behavior
			isZoomedInView = IsZoomedInView;
		}

		spItem = spSemanticLocation.Item;
		if (spItem is not null)
		{
			ItemCollection spItems;

			// Get the index of the corresponding item
			spItems = Items;

			if (m_tpScrollViewer is not null)
			{
				bool canScrollIntoView = false;
				canScrollIntoView = CanScrollIntoView();
				if (canScrollIntoView)
				{
					DependencyObject? spContainer = null;
					uint groupItemIndex = 0;
					bool foundGroup = false;

					// this could be a group, let's try that first
					GetGroupItemIndex(spItem, out groupItemIndex, out foundGroup);

					// On the phone when exiting the JumpList, we want to force top alignment for the selected group
					ScrollIntoViewAlignment alignment = ScrollIntoViewAlignment.Default;
					if (isJumpListEnabled && isZoomedInView && foundGroup)
					{
						alignment = ScrollIntoViewAlignment.Leading;
					}

					// whether spItem is a group or an actual item, this should work
					ScrollIntoViewWithAlignment(spItem, alignment);
					UpdateLayout(); // need to flush layout to get the container into view

					// let's get the position relative to the root
					// this is how we communicated the location of the source itemcontainer

					if (foundGroup)
					{
						spContainer = HeaderFromIndex((int)groupItemIndex);
					}
					else
					{
						// fallback to expecting just a regular item
						bool found = false;
						uint index = 0;

						// Get the index of the corresponding item
						found = spItems.IndexOf(spItem, out index);

						if (found)
						{
							spContainer = ContainerFromIndex((int)index);
						}
					}

					if (spContainer is not null)
					{
						FrameworkElement? spContainerLVI;

						spContainerLVI = spContainer as FrameworkElement;

						if (spContainerLVI is not null)
						{
							GeneralTransform spTransform;
							float amountToScroll = 0;
							double width = 0;
							double height = 0;
							double viewportWidth = 0;
							double viewportHeight = 0;
							Point destinationContainerLocation = default;
							Rect destinationContainerNewLocation = default;
							bool shouldScrollHorizontally = true;
							bool shouldScrollVertically = true;
							bool readLocationFromISZL = true;
							double scrollableAmount = 0;
							SemanticZoomLocation? spSezoLocationConcrete;

							// current destination location
							spTransform = spContainerLVI.TransformToVisual(this);
							destinationContainerLocation = spTransform.TransformPoint(destinationContainerLocation);

							// If we are following jumplist behavior, we may want the item to be bottom aligned:
							//   (a) if we are in a JumpList scenario
							//   (b) and we are the ZoomedOut view
							if (isJumpListEnabled && (!isZoomedInView))
							{
								destinationContainerNewLocation.X = destinationContainerLocation.X;
								if (spSemanticLocation.GetIsBottomAlignment())
								{
									double lvwHeight = 0.0;
									double containerHeight = 0.0;

									lvwHeight = ActualHeight;
									containerHeight = spContainerLVI.ActualHeight;
									destinationContainerNewLocation.Y = (float)(lvwHeight - containerHeight);
								}
								else
								{
									destinationContainerNewLocation.Y = 0;
								}
								readLocationFromISZL = false;
							}

							if (readLocationFromISZL)
							{
								// the location where we really want this container to live
								destinationContainerNewLocation = spSemanticLocation.Bounds;
							}

							// need to take care of both horizontal and vertical based on the modes of our ScrollViewer
							scrollableAmount = m_tpScrollViewer.ScrollableHeight;
							shouldScrollVertically = scrollableAmount > 0;
							scrollableAmount = m_tpScrollViewer.ScrollableWidth;
							shouldScrollHorizontally = scrollableAmount > 0;

							viewportWidth = m_tpScrollViewer.ComputePixelViewportWidth(null, false);
							viewportHeight = m_tpScrollViewer.ComputePixelViewportHeight(null, false);

							width = spContainerLVI.ActualWidth;
							height = spContainerLVI.ActualHeight;

							while (shouldScrollHorizontally || shouldScrollVertically)
							{
								double originalOffset = 0.0f;
								// offset we wish to be at:
								amountToScroll = (float)(shouldScrollHorizontally ? destinationContainerNewLocation.X - destinationContainerLocation.X : destinationContainerNewLocation.Y - destinationContainerLocation.Y);

								// we should not scroll further then the edge of our viewport
								// the remainer will be done using a rendertransform on the view by the sezo

								// limit to the left: a container can be placed flush against the left of the viewport
								amountToScroll = (float)DoubleUtil.Max(amountToScroll, (shouldScrollHorizontally ? -1 * destinationContainerLocation.X : -1 * destinationContainerLocation.Y));

								// limit to the right: a container can be placed against the right of the viewport,taking into account its width or height so it is fully visible.
								amountToScroll = (float)DoubleUtil.Min(amountToScroll,
																			(shouldScrollHorizontally ?
																			viewportWidth - destinationContainerLocation.X - width :
																			viewportHeight - destinationContainerLocation.Y - height));
								if (shouldScrollHorizontally)
								{
									originalOffset = m_tpScrollViewer.GetPixelHorizontalOffset();
								}
								else
								{
									originalOffset = m_tpScrollViewer.GetPixelVerticalOffset();
								}

								// Note that ScrollByPixelDelta uses the 2nd argument for non-logical scrolling scenarios (say, with StackPanels), while
								// it uses the 3d argument for logical scrolling scenarios.
								// Both the 2nd and 3rd arguments need to be expressed in pixels.
								m_tpScrollViewer.ScrollByPixelDelta(
									shouldScrollHorizontally,
									originalOffset - amountToScroll,
									-1 * amountToScroll,
									false /*isDManipInput*/);

								// setup for next iteration
								if (shouldScrollHorizontally)
								{
									shouldScrollHorizontally = false;
								}
								else
								{
									shouldScrollVertically = false;
								}
							}   // end of the while loop that handles both horizontal and vertical scrolling

							spSezoLocationConcrete = spSemanticLocation;
							if (spSezoLocationConcrete is not null)
							{
								// communicate where the destination container actually ended up
								// this allows the SeZo to apply extra transforms
								UpdateLayout(); // need to flush layout to get the container in the right position
								spTransform = spContainerLVI.TransformToVisual(this);
								destinationContainerLocation.X = destinationContainerLocation.Y = 0;
								// point of the container will be relative to this listview
								destinationContainerLocation = spTransform.TransformPoint(destinationContainerLocation);
								destinationContainerNewLocation.X -= destinationContainerLocation.X;
								destinationContainerNewLocation.Y -= destinationContainerLocation.Y;
								destinationContainerNewLocation.Width = (float)width;
								destinationContainerNewLocation.Height = (float)height;
								// put the delta in the remainder field - this is the remainder that we were unable to scroll
								// we are currently in lockdown phase, so no new api is allowed.
								// ofcourse, we would like to make this public and put the remainder field in the IDL or
								// have some better mechanism to pass this value.
								spSezoLocationConcrete.Remainder = destinationContainerNewLocation;
							}
						}
					}
				}
			}
			else
			{
				m_deferredAlignment = (spSemanticLocation.GetIsBottomAlignment()
									   ? ScrollIntoViewAlignment.Default
									   : ScrollIntoViewAlignment.Leading);
				ScheduleDeferredScrollCommand((object? scrollToItem) =>
				{
					if (scrollToItem is not null && m_tpScrollViewer is not null)
					{
						Panel? spPanel;
						spPanel = ItemsPanelRoot;
						if (spPanel is not null)
						{
							ScrollIntoViewWithAlignment(scrollToItem, m_deferredAlignment);
						}
					}
				},
				spItem,
				DeferPoint.ItemsHostAvailable);
			}
		}
#endif
	}

	// When this ListViewBase is the active view and we're changing to the other
	// view, optionally provide the source and destination items.
	//
	// Two modes:
	//  1. user has tapped an item and this listview has decided to toggle a semantic zoom based on that
	//  2. user has used a DM gesture such as pinch or zoomout and SemanticZoom has decided to perform a semantic zoom
	//
	//  in the first case we can use the focused element
	//  in the second case we need to find out what the closest element is to the center point
	public void StartViewChangeFrom(
		SemanticZoomLocation source,
		SemanticZoomLocation destination)
	{
#if !HAS_UNO // TODO Uno: SemanticZoom is not implemented.
		try
		{
			object? spItem = null;
			bool isGrouping = false;
			int sourceIndex = -1;
			// the container that represents our source.
			DependencyObject? spContainer = null;

			if (source is not null)
			{
				spItem = source.Item;
				if (spItem is null)
				{
					Point zoomPoint = default;
					bool validZoomPoint = false;
					bool jumpListAlignmentBehavior = false;

					zoomPoint = source.ZoomPoint;

					validZoomPoint = zoomPoint.X != 0 || zoomPoint.Y != 0;

					// only use zoompoint if there was a gesture that got us to a zoompoint
					if (validZoomPoint)
					{
						// scenario 2: the user has zoomed with DM or used ctrl-mousewheel and that is why we are doing this viewchange
						Panel spItemsHost;
						IItemLookupPanel? spItemsHostAsItemLookupPanel;
						spItemsHost = ItemsPanelRoot;
						if (spItemsHost is null)
						{
							throw new InvalidOperationException();
						}
						spItemsHostAsItemLookupPanel = spItemsHost as IItemLookupPanel;

						Point zoomPointToFirstPanel = zoomPoint;
						GeneralTransform spTransformFromLVBtoPanel;

						spTransformFromLVBtoPanel = TransformToVisual(spItemsHost);
						zoomPointToFirstPanel = spTransformFromLVBtoPanel.TransformPoint(zoomPoint);

						if (spItemsHostAsItemLookupPanel is not null)
						{
							bool isViewGrouping = false;
							ElementInfo elementInfo = new(-1, false);

							elementInfo = spItemsHostAsItemLookupPanel.GetClosestElementInfo(zoomPointToFirstPanel);
							sourceIndex = elementInfo.m_childIndex;

							isViewGrouping = IsGrouping;
							if (elementInfo.m_childIsHeader)
							{
								global::System.Diagnostics.Debug.Assert(isViewGrouping);

								// fallback to the group. The sourceindex still points to the view property
								// instead of an actual item.

								ICollectionView? spCollectionView;
								spCollectionView = CollectionView;
								if (spCollectionView is not null)
								{
									IObservableVector<object>? spGroups;
									spGroups = spCollectionView.CollectionGroups;
									if (spGroups is not null)
									{
										IList<object> spGroupsAsVector;
										spGroupsAsVector = spGroups;
										spItem = spGroupsAsVector[sourceIndex];
									}
								}
							}

							if (isViewGrouping)
							{
								if (spItemsHost is not ModernCollectionBasePanel)
								{
									// so we now have an index that points to a group. That is not what we need, we want
									// to figure out which item was zoomed at.
									// we will have to look at this group, find the itemscontrol over there, find the panel
									// and then get a better index.
									// NOTE: this will change as we get a different grouping panel
									GroupItem? spGroupItem;

									spContainer = HeaderFromIndex(sourceIndex);
									if (spContainer is not null)
									{
										spGroupItem = spContainer as GroupItem;
									}
									else
									{
										spGroupItem = null;
									}

									if (spGroupItem is not null)
									{
										ItemsControl? spItemsControl;
										ElementInfo innerElementInfo = new(-1, false);
										spItemsControl = spGroupItem.GetTemplatedItemsControl();
										if (spItemsControl is not null)
										{
											spItemsHost = spItemsControl.ItemsPanelRoot;

											if (spItemsHost is not null)
											{
												// we found a panel. This panel might be arbitrarily positioned inside of our moco.
												// ZoomPoint is relative to the moco, it would be good to transform that point to
												// the panels coordinate space.
												Point zoomPointToNestedPanel = zoomPoint;

												spTransformFromLVBtoPanel = TransformToVisual(spItemsHost);
												zoomPointToNestedPanel = spTransformFromLVBtoPanel.TransformPoint(zoomPoint);

												// try to use the optimized IITemLookup version
												spItemsHostAsItemLookupPanel = spItemsHost as IItemLookupPanel;
												if (spItemsHostAsItemLookupPanel is not null)
												{
													innerElementInfo = spItemsHostAsItemLookupPanel.GetClosestElementInfo(zoomPointToNestedPanel);
												}
												else
												{
													// fallback to a naive and slow (full iteration) implementation of all the
													// arranged children
													Point p = new(zoomPointToNestedPanel.X, zoomPointToNestedPanel.Y);
													innerElementInfo.m_childIndex = CoreImports.CPanel_PanelGetClosestIndexSlow(spItemsHost, p);
												}

												// nice, now we have a sourceindex that belongs to another itemscontrol
												// what we need is an index into our original itemscollection.
												// Grouping does not support virtualization.
												IObservableVector<object> spObservableItems;
												IList<object>? spInnerItems;
												bool found = false;
												spObservableItems = spItemsControl.Items;
												spInnerItems = spObservableItems as IList<object>;

												if (spInnerItems is not null)
												{
													uint innerItemsCount = 0;
													uint i = 0; // will keep either sourceindex or innersourceindex
													innerItemsCount = (uint)spInnerItems.Count;

													// correct the sourceindex to point to the view
													// we are mixing up our UINT and INT's unfortunately. The IItemLookupPanel uses INT's
													// but collections use UINT. To be honest, we cannot support an INT amount of children
													// so i'm not afraid here.
													if (innerElementInfo.m_childIndex >= 0 && innerElementInfo.m_childIndex < (int)innerItemsCount)
													{
														ItemCollection spItems;
														spItems = Items;

														// use inner information
														spItem = spInnerItems[innerElementInfo.m_childIndex];
														found = spItems.IndexOf(spItem, out i);
														// assign the actual sourceIndex as it is within the outer collection
														sourceIndex = (int)(uint)i;
														// clear out the container since we should try and find a new container
														spContainer = null;
													}
													else
													{
														// fallback to the group. The sourceindex still points to the view property
														// instead of an actual item.
														ItemContainerGenerator spGenerator;
														IList<object>? spView;

														spGenerator = ItemContainerGenerator;
														spView = spGenerator.View;

														if (spView is not null)
														{
															spItem = spView[sourceIndex];
														}
													}
												}
											}
										}
									}
								}
								else
								{
									if (elementInfo.m_childIsHeader)
									{
										spContainer = HeaderFromIndex(sourceIndex);
									}
									else
									{
										spContainer = ContainerFromIndex(sourceIndex);
									}
								}
							}
						}
						else
						{
							// fallback to a naieve and slow (full iteration) implementation of all the
							// arranged children
							Point p = new(zoomPoint.X, zoomPoint.Y);
							sourceIndex = CoreImports.CPanel_PanelGetClosestIndexSlow(spItemsHost, p);
						}
					}
					else
					{
						bool isZoomedInView = false;

						// Let's check if we are in a jump list situation
						SemanticZoom? spSemanticZoomOwner;
						spSemanticZoomOwner = SemanticZoomOwner;
						if (spSemanticZoomOwner is not null)
						{
							jumpListAlignmentBehavior = true;

							// SemanticZoom checks other conditions than the apiset such as type of ZI/ZO views,
							// popup in template, etc
							isZoomedInView = IsZoomedInView;
							if (!isZoomedInView)
							{
								// We are the Zoomed Out View
								if (spSemanticZoomOwner.GetIsCancellingJumpList())
								{
									// Cancel: don't set a destination item
									return;
								}
							}
						}

						if (jumpListAlignmentBehavior && isZoomedInView)
						{
							// We are the ZoomedInView
							uint startIndex = 0;
							bool bottomAlignment = false;
							if (m_tpSZRequestingItem is not null)
							{
								// If a group has requested the change, let's take it as reference
								int groupIndex = -1;
								DependencyObject spDO;
								spDO = (DependencyObject)m_tpSZRequestingItem;
								groupIndex = IndexFromHeader(spDO, false /*excludeHiddenEmptyGroups*/);
								if (groupIndex >= 0)
								{
									startIndex = (uint)(int)groupIndex;
								}
								bottomAlignment = true;
							}
							FindFirstItem(startIndex, out sourceIndex, out spItem);
							if (destination is not null)
							{
								SemanticZoomLocation spDestination = destination;
								spDestination.SetIsBottomAlignment(bottomAlignment);
							}
						}
						else
						{
							// scenario 1: the user has selected an item and that is why we are doing this viewchange

							// this is only valid if we are in the zoomedout view (which will never have grouping)
							sourceIndex = (GetFocusedIndex() >= 0) ? GetFocusedIndex() : (int)GetLastFocusedIndex();
						}
					}


					// some of the specialized codepaths have advised on an spitem already.
					if (spItem is null)
					{
						ItemCollection spItems;
						uint itemsCount = 0;

						spItems = Items;
						itemsCount = (uint)spItems.Count;

						// the lastFocusedIndex is an INT, not UINT, so -1 does not come into play
						// which is why we make absolutely sure this is a valid index we are going to.
						if (sourceIndex >= 0 && sourceIndex < (int)itemsCount)
						{
							spItem = spItems[sourceIndex];
						}
					}

					// hopefully we found an item somehow. Fill the szl with it.
					if (spItem is not null)
					{
						// Set the focused item as the SourceItem
						source.Item = spItem;
					}

					// set the destination bounding box
					// if we had an item, this is the items container. if we are a group, this
					// is the groups container
					{
						GeneralTransform spTransform;

						// some of the specialized codepaths(grouping) have advised on a container already
						if (spContainer is null)
						{
							spContainer = ContainerFromIndex(sourceIndex);
						}


						// container surely can be null, because we're not guaranteed to get back an item
						// that was in view.
						// if the container is null, there is not much we can do.
						// If we are doing jumpList behavior then dont adjust co-ordinate systems.
						if (spContainer is not null && !jumpListAlignmentBehavior)
						{
							FrameworkElement spContainerFE;
							Point relativeToThis = default;
							Rect boundingBox = default;
							double dActualWidth = 0.0;
							double dActualHeight = 0.0;

							spContainerFE = (FrameworkElement)spContainer;

							dActualWidth = spContainerFE.ActualWidth;
							dActualHeight = spContainerFE.ActualHeight;

							spTransform = spContainerFE.TransformToVisual(this);
							relativeToThis = spTransform.TransformPoint(relativeToThis);

							boundingBox.Width = (float)dActualWidth;
							boundingBox.Height = (float)dActualHeight;
							boundingBox.X = relativeToThis.X;
							boundingBox.Y = relativeToThis.Y;
							source.Bounds = boundingBox;
						}
						else
						{
							Rect boundingBox = default;
							boundingBox.X = zoomPoint.X;
							boundingBox.Y = zoomPoint.Y;
							source.Bounds = boundingBox;
						}
					}
				}
			}

			// If our ItemsSource is grouped, automatically provide a destination of the
			// group's name (so that if the next level's items are just the names, then
			// we won't need to write any code to handle the mapping)
			isGrouping = CanSemanticZoomGroup;
			if (isGrouping)
			{
				object? spDestination;

				// Create a SemanticZoomLocation if we don't have one yet
				if (destination is not null)
				{
					// If there's no DestinationItem currently set, then will use its group
					// name (or the first item in the group if heading the other direction)
					spDestination = destination.Item;
					if (spDestination is null && spItem is not null)
					{
						bool isZoomedInView = false;

						// If we're changing from the ZoomedInView to the ZoomedOutView then we
						// need to look up a group from an item, but otherwise we'll just
						// get the first item in the group
						isZoomedInView = IsZoomedInView;
						if (isZoomedInView)
						{
							if (sourceIndex >= 0)
							{
								ICollectionViewGroup? spDestinationGroup;

								// in the normal case, spitem is set to an item, and we are going to find a group out of that now
								// however, in the case of an empty group, that is not true.
								// That codepath has already adviced on a group
								spDestinationGroup = spItem as ICollectionViewGroup;
								if (spDestinationGroup is null)
								{
									// have to dig a little bit deeper
									GetGroupFromItem((uint)sourceIndex, out spDestinationGroup, out _, out _, out _);
								}

								// use the group to set the destination item
								if (spDestinationGroup is not null)
								{
									spDestination = spDestinationGroup;
								}
							}
						}
						else
						{
							// currently in zoomedoutview, going to zoomedinview

							// Get the first item in the group as the destination
							ICollectionViewGroup? spDestinationGroup;
							spDestinationGroup = spItem as ICollectionViewGroup;

							// if that did not succeed (empty group) or if we are using sticky headers, fallback to the group
							if (spDestination is null && spDestinationGroup is not null)
							{
								spDestination = spDestinationGroup.Group;
							}
						}


						// If we've found the corresponding destination item
						if (spDestination is not null)
						{
							// Set the group key as the DestinationItem
							destination.Item = spDestination;
						}
					}
				}
			}
		}
		finally
		{
			m_tpSZRequestingItem = null;
		}
#endif
	}

	// Determines whether or not a SemanticZoom associated with this ListViewBase has
	// grouped data as its ZoomedInView.
	private bool CanSemanticZoomGroup
	{
		get
		{
			bool pCanGroup = false;
			bool isZoomedInView = false;

			isZoomedInView = IsZoomedInView;
			if (isZoomedInView)
			{
				// If we are the ZoomedInView, then we just need to check if we support
				// grouping
				pCanGroup = IsGrouping;
			}
			else
			{
#if !HAS_UNO // TODO Uno: SemanticZoom is not implemented.
				// Otherwise, get the ZoomedInView and see if it supports grouping
				SemanticZoom spSemanticZoomOwner;
				ISemanticZoomInformation? spJumpContent;
				ItemsControl? spItems;

				spSemanticZoomOwner = SemanticZoomOwner!;
				spJumpContent = spSemanticZoomOwner.ZoomedInView;
				if (spJumpContent is not null)
				{
					spItems = spJumpContent as ItemsControl;
					if (spItems is not null)
					{
						pCanGroup = spItems.IsGrouping;
					}
				}
#endif
			}

			return pCanGroup;
		}
	}

	// When this ListViewBase is the inactive view and we're changing to it,
	// optionally provide the source and destination items.
	public void StartViewChangeTo(
		SemanticZoomLocation source,
		SemanticZoomLocation destination)
	{
		m_disableScrollingPlaceholders = true;
	}

	// Complete the change to the other view when this ListViewBase was
	// the active view.
	public void CompleteViewChangeFrom(
		SemanticZoomLocation source,
		SemanticZoomLocation destination)
	{
	}

	// Complete the change to make this ListViewBase the active view.
	public void CompleteViewChangeTo(
		SemanticZoomLocation source,
		SemanticZoomLocation destination)
	{
		try
		{
#if !HAS_UNO // TODO Uno: SemanticZoom is not implemented.
			object? spItem;
			bool hasFocus = false;
			SemanticZoom? spSemanticZoomOwner;
			bool shouldTakeFocus = true;

			spSemanticZoomOwner = SemanticZoomOwner;
			if (spSemanticZoomOwner is not null)
			{
				shouldTakeFocus = spSemanticZoomOwner.HasFocus();
			}

			// We only want to focus the destination item (or any item in the destination view) if and
			// only if the semantic zoom has focus at this time.
			if (shouldTakeFocus)
			{
				spItem = destination.Item;
				if (spItem is not null)
				{
					ItemCollection spItems;

					bool found = false;
					uint index = 0;

					// Get the index of the corresponding item
					spItems = Items;
					found = spItems.IndexOf(spItem, out index);

					if (found)
					{
						SetFocusedItem((int)index,
										   /*shouldScrollIntoView*/ false,
										   /*forceFocus*/ true,
										   m_semanticZoomCompletedFocusState);
					}
					else
					{
						bool isGrouping = false;

						isGrouping = IsGrouping;
						if (isGrouping)
						{
							bool foundGroup = false;
							uint groupIndex = 0;

							GetGroupItemIndex(spItem, out groupIndex, out foundGroup);
							if (foundGroup)
							{
								// Our destination is a group.
								int groupCount = 0;
								int delta = 0;
								int firstSelectableElementIndex = -1;

								groupCount = GetGroupCount();
								while (((int)groupIndex + delta) < groupCount && !found)
								{
									FindFirstSelectableItemInGroup(groupIndex + (uint)delta, true /* from beginning*/,
																   -1/* start index */, out firstSelectableElementIndex, out found);
									delta++;
								}

								if (!found)
								{
									// Search the previous groups
									delta = -1;
									while (((int)groupIndex + delta) >= 0 && !found)
									{
										FindFirstSelectableItemInGroup(groupIndex + (uint)delta, true /* from beginning*/,
																	   -1/* start index */, out firstSelectableElementIndex, out found);
										delta--;
									}
								}

								if (found)
								{
									SetFocusedItem(firstSelectableElementIndex,
													   /*shouldScrollIntoView*/ false,
													   /*forceFocus*/ true,
													   m_semanticZoomCompletedFocusState);
								}
							}
						}
					}
				}

				hasFocus = HasFocus();
				if (!hasFocus)
				{
					SetFocusedItem((int)GetLastFocusedIndex(),
										   /*shouldScrollIntoView*/ true,
										   /*forceFocus*/ true,
										   m_semanticZoomCompletedFocusState);

					hasFocus = HasFocus();
					if (!hasFocus)
					{
						// If still we could not focus something that makes sense, focus the  destination view itself
						// for the sake of not leaving the focus on the source view.
						hasFocus = Focus(m_semanticZoomCompletedFocusState);
					}
				}
			}
#endif
		}
		finally
		{
			m_disableScrollingPlaceholders = false;
		}
	}

	// Find the first non emptyGoup of the underlying collection
	private void FindFirstItem(
		uint targetGroupIndex,
		out int pItemIndex,
		out object? ppItem)
	{
		pItemIndex = -1;
		ppItem = null;

#if !HAS_UNO // TODO Uno: ItemsControl has no CollectionView.
		ICollectionView? spCollectionView;
		spCollectionView = CollectionView;
		if (spCollectionView is not null)
		{
			IObservableVector<object>? spGroups;
			spGroups = spCollectionView.CollectionGroups;
			if (spGroups is not null)
			{
				uint itemIndex = 0;
				uint size = 0;
				IList<object> spGroupsAsVector;
				spGroupsAsVector = spGroups;
				size = (uint)spGroupsAsVector.Count;
				for (uint groupIndex = 0; groupIndex < size; ++groupIndex)
				{
					object spCurrent;
					ICollectionViewGroup spCurrentGroup;
					IObservableVector<object> spCurrentGroupItems;
					IList<object> spCurrentGroupItemsAsV;
					uint groupSize = 0;

					spCurrent = spGroupsAsVector[(int)groupIndex];
					spCurrentGroup = (ICollectionViewGroup)spCurrent;
					spCurrentGroupItems = spCurrentGroup.GroupItems;
					spCurrentGroupItemsAsV = spCurrentGroupItems;
					groupSize = (uint)spCurrentGroupItemsAsV.Count;

					if (groupSize > 0)
					{
						if (groupIndex >= targetGroupIndex)
						{
							// We stop at the first non empty group after (including) targetGroupIndex
							ppItem = spCurrentGroupItemsAsV[0];
							pItemIndex = (int)itemIndex;
							break;
						}
						else
						{
							itemIndex += groupSize;
						}
					}
				}
			}
		}
#endif
	}
}

