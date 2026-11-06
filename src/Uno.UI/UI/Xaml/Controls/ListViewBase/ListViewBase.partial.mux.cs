// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial.cpp, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      ListViewBase displays a rich, interactive collection of items.


#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBase
{
	private const uint BUDGET_MANAGER_DEFAULT_LIMIT = 40;

	// Uncomment to output ListViewBase debugging information
	//#define LVB_DEBUG

	// Initializes a new instance of the ListViewBase class.
	protected internal ListViewBase()
	{
		m_pAnchorIndex = null;
		m_dragItemsCount = 0;
		m_isDraggingOverSelf = false;
		m_scrollHostOffsetChangeAction = ScrollHostOffsetChangeAction.ScrollHostOffsetChangeAction_None;
		m_isLoadAsyncInProgress = false;
		m_maxContentWidth = 0;
		m_maxContentHeight = 0;
		m_semanticZoomCompletedFocusState = FocusState.Programmatic;
		m_isInItemReorderAfterDrop = false;
		m_focusedGroupIndex = -1;
		m_lastFocusedGroupIndex = 0;
		m_pendingAutoPanVelocity = default;
		m_currentAutoPanVelocity = default;
		m_lowestPhaseInQueue = -1;
		m_budget = BUDGET_MANAGER_DEFAULT_LIMIT;
		m_itemsAreTabStops = true;
		m_groupsAreTabStops = true;
		m_allowDrop = false;
		m_isInOnSelectionModeChanged = false;
		m_disableScrollingPlaceholders = false;
		m_bKeyDownArgsFromItem = false;
		m_itemSelectionGestureId = string.Empty;
		m_lastIncrementalVisualizationContainerCount = 0;
		m_focusableElementForModernPanelReentrancyGuard = false;
		m_firstAvailableItemForFocus = -1;
		m_firstAvailableGroupHeaderForFocus = -1;
		m_itemIndexHintForHeaderNavigation = -1;
		m_lastFocusedElementType = ElementType.Other;
		m_wasItemOrGroupHeaderFocused = false;
		m_isHolding = false;
		m_isDeferredElementFooter = false;
		m_deferredPoint = DeferPoint.ItemsHostAvailable;
		m_showsScrollingPlaceholdersLocal = true;

		m_lastReorderPosition.X = 0;
		m_lastReorderPosition.Y = 0;
		m_lastDragPosition.X = 0;
		m_lastDragPosition.Y = 0;
		m_lastDragOverPoint.X = 0;
		m_lastDragOverPoint.Y = 0;
		m_liveReorderIndices = new(-1, -1, -1);
		m_dragAcceptedOperation = global::Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;

		Initialize();
	}

	// Initialize this ListViewBase
	private void Initialize()
	{
		Loaded += OnLoaded;

		GettingFocus += OnGettingFocus;
		m_gettingFocusHandler.Disposable = Disposable.Create(() => GettingFocus -= OnGettingFocus);

		// TODO Uno: stands in for ~ListViewBase, which has no C# form.
		Unloaded += OnUnloadedReleaseResources;
	}

	// TODO Uno: ~ListViewBase has no C# form. The offset callbacks are released and the live reorder timer is stopped when the
	// control unloads. OnLoaded attaches the callbacks again.
	private void OnUnloadedReleaseResources(object sender, RoutedEventArgs args)
	{
		// stop the timer if it exists
		m_tpLiveReorderTimer?.Stop();

		DetachScrollViewerPropertyChanged();
	}

#if !HAS_UNO // TODO Uno: C# has no destructor to run on the UI thread.
	// Destroys an instance of the ListViewBase class.
	~ListViewBase()
	{
		if (m_tpScrollViewer is not null)
		{
			m_tpScrollViewer.SetDirectManipulationStateChangeHandler(null);
			m_epScrollViewerViewChangingHandler.Disposable = null;
		}

		// stop the timer if it exists
		{
			var spLiveReorderTimer = m_tpLiveReorderTimer;
			if (spLiveReorderTimer is not null)
			{
				spLiveReorderTimer.Stop();
			}
		}

		m_epLiveReorderTimerEvent.Disposable = null;

		ReleaseAnchorIndex();
		DetachScrollViewerPropertyChanged();
	}
#endif

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == SelectionModeProperty)
		{
			// Call OnSelectionModeChanged when the selection mode changes.
			OnSelectionModeChangedImpl(
				(ListViewSelectionMode)args.OldValue,
				(ListViewSelectionMode)args.NewValue);

			// Call the Selector::UpdateVisibleAndCachedItemsSelectionAndVisualState function
			// OnSelectionModeChanged will update all Selection related properties
			UpdateVisibleAndCachedItemsSelectionAndVisualState(false /* updateIsSelected */);
		}
		else if (args.Property == IsSwipeEnabledProperty)
		{
			// TODO: Enable/Disable swipe by registering or unregistering
			// with the Touch Input Engine we're listening to (it's important
			// to let the TIE know we're no listening for swipe so it will
			// let DirectManipulation pick up panning opposite our orientation)
			// NOTE: We have eliminated TIE usage, is the above work applicable to the
			//       the gesture recognizer.
		}
		else if (args.Property == IsSelectionActiveProperty)
		{
			OnIsSelectionActiveChanged();
		}
		else if (args.Property == ShowsScrollingPlaceholdersProperty)
		{
			// Recalculate local field.
			m_showsScrollingPlaceholdersLocal = (bool)GetValue(ShowsScrollingPlaceholdersProperty);
		}
		else if (args.Property == ItemsSourceProperty ||
				 args.Property == IncrementalLoadingTriggerProperty)
		{
			if (args.Property == ItemsSourceProperty)
			{
				// when the ItemsSource changes, reset the last focused index
				// so that when tabbing into the control we dont jump to the remembered index. It
				// is likely not representing the same data item
				SetLastFocusedIndex(0);
			}

			// When any property changes that affects our ItemsHost
			// we hookup to changes.
			EnsureScrollHostOffsetChangeAction();

#if !HAS_UNO // TODO Uno: telemetry only.
			ItemsControl.TraceVirtualizationEnabledByModernPanel();
#endif

			// Hookup to the ItemsRangeInfo if it exists
			InitializeDataSourceItemsRangeInfo();

			// Hookup to the SelectionInfo if it exists
			InitializeDataSourceSelectionInfo();
		}
#if !HAS_UNO // TODO Uno: telemetry only.
		else if (args.Property == ItemsPanelProperty)
		{
			ItemsControl.TraceVirtualizationEnabledByModernPanel();
		}
#endif
		else if (args.Property == IsMultiSelectCheckBoxEnabledProperty)
		{
			// Call the Selector::UpdateVisibleAndCachedItemsVisualState function
			UpdateVisibleAndCachedItemsSelectionAndVisualState(false /* updateIsSelected */);
		}
		else if (args.Property == VisibilityProperty)
		{
			// If we have a pending drag operation, cancel it now.
			CancelDrag();
		}
	}

	// Called when a ListViewBaseItem is getting a new data item to display.
	protected override void PrepareContainerForItemOverride(
		DependencyObject element,
		object item)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		if (IsListViewBaseItem(element))
		{
			var spIListViewItemAsSelectorItem = (SelectorItem)element;

			{
				// Update all states at once whichever changes are made by bases classes
				// Suspends the VisualState changes in order to "batch" them at the end of the preparation
				using var suspender = new StateChangeSuspender((ListViewBaseItem)spIListViewItemAsSelectorItem);

				((ListViewBaseItem)spIListViewItemAsSelectorItem).ClearInteractionState();

				// Will call UpdateVisualState for us.
				base.PrepareContainerForItemOverride(element, item);
			}
		}
	}

	private protected override void PreProcessContentPreparation(
		DependencyObject pContainer,
		object? pItem)
	{
		if (IsListViewBaseItem(pContainer))
		{
			var spIListViewItemAsSelectorItem = (SelectorItem)pContainer;
			var spElementAsUIE = pContainer as UIElement;

			// ListViewItems are allowed to grow bigger than the available size and not be clipped.
			// we currently only allow this in ItemsStackPanel
			var spItemsHost = ItemsPanelRoot;

			bool allowNonClipping = false;
			if (spItemsHost is not null)
			{
				allowNonClipping = spItemsHost.IsNonClippingSubtree;
			}
			((ListViewBaseItem)spIListViewItemAsSelectorItem).IsNonClippingSubtree = allowNonClipping;

			// we might have been inserted into the list for deferred clear container calls.
			// the fact that we are now being prepared, means that we don't have to perform that clear call.
			// yay! that means we are going to not perform work that has quite a bit of perf overhead.
			// ---
			// we happen to know that the clear will have been pushed to the back of the vector, so optimize
			// the panning scenario by checking in reverse order

			// special case the last element (since we push_back during when we called clear and we expect the next
			// action to be this prepare).
			uint toBeClearedContainerCount = 0;
			toBeClearedContainerCount = (uint)m_toBeClearedContainers.Count;

			for (uint current = toBeClearedContainerCount - 1; toBeClearedContainerCount > 0; --current)
			{
				// go from back to front, since we're most likely in the back.
				var spCurrentContainer = m_toBeClearedContainers[(int)current];

				if (ReferenceEquals(spCurrentContainer, spElementAsUIE))
				{
					// remove from _BOTH_
					m_toBeClearedContainers.RemoveAt((int)current);
					m_toBeClearedItems.RemoveAt((int)current);

					// The current container is *not* going to be cleared and it's going to
					// be reused and reprepared right away. If it gets re-templated during
					// the preparation routine, the binding expressions in the new template
					// will fail against the old content (assuming it's set) when the
					// old data context is propagated to the new template tree. To avoid
					// that, we make sure to unset the content here.
					// Note that this operation is a no-op if the app is handling ContainerContentChanging
					// and not relying on data context propagation (which we don't recommend). In that case,
					// the content is never set and we don't run into the binding issue described above.
					var itemTemplateSelector = ItemTemplateSelector;
					if (itemTemplateSelector is not null)
					{
						bool isContainerFromTemplateRoot = spElementAsUIE is not null && spElementAsUIE.IsContainerFromTemplateRoot;

						// Do not clear content if we got the container from ItemTemplate
						if (!isContainerFromTemplateRoot)
						{
							spIListViewItemAsSelectorItem.ClearValue(ContentControl.ContentProperty);
						}
					}

					break;
				}

				if (current == 0)
				{
					// UINT
					break;
				}
			}
		}
	}

	// Called when a ListViewBaseItem is getting recycled
	protected override void ClearContainerForItemOverride(
		DependencyObject element,
		object item)
	{
		if (element is null)
		{
			throw new ArgumentNullException(nameof(element));
		}

		if (IsListViewBaseItem(element))
		{
			var spItemsHost = ItemsPanelRoot;

			// There is much perf involved with doing a clear, and usually it is going to be
			// a waste of time since we are going to immediately follow up with a prepare.
			// Perf traces have found this to be about 8 to 12% during a full virtualization pass (!!)
			// Although with other optimizations we would expect that to go down, it is unlikely to go
			// down to 0. Therefore we are deferring the impl work here to later.
			// We have decided to do this only for the new panels.

			// also, do not defer items that are UIElement. They need to be cleared straight away so that
			// they can be messed with again.
			if (spItemsHost is ICustomGeneratorItemsHost && item is not UIElement)
			{
				var spElement = element;
				var spElementUIE = spElement as UIElement;

				// raise the event, so that people can build up a concept of the recycle queue
				// ListView decides to raise the CCC event immediately (even though it is deferring the actual clearing)
				RaiseContainerContentChangingOnRecycle(spElementUIE!, item);

				// we are going to defer the clear
				m_toBeClearedContainers.Add(spElementUIE!);
				m_toBeClearedItems.Add(item);

				// note that if we are being cleared, we are not going to be in the
				// visible index, or the caches. And thus we will never be called in the
				// prepare queuing part.

				if (!m_isRegisteredForCallbacks)
				{
					var spBuildTree = DXamlCore.Current.GetBuildTreeService();
					spBuildTree.RegisterWork(this);
				}
				global::System.Diagnostics.Debug.Assert(m_isRegisteredForCallbacks);
			}
			else
			{
				base.ClearContainerForItemOverride(element, item);
			}
		}
	}

	// Causes the object to scroll into view.  If it is not visible, it is aligned
	// either at the top or bottom of the viewport.
	public void ScrollIntoView(object? item)
		=> ScrollIntoViewWithOptionalAnimation(item!, ScrollIntoViewAlignment.Default, true /*disableAnimation*/);

	// Causes the object to scroll into view.
	// Item will be always aligned based on alignment mode.
	public void ScrollIntoView(object? item, ScrollIntoViewAlignment alignment)
		=> ScrollIntoViewWithOptionalAnimation(item!, alignment, true /*disableAnimation*/);

	// Invoked by the public ListViewBase::ScrollIntoView and ListViewBase::ScrollIntoViewWithAlignment methods,
	// as well as the private IListViewBasePrivate2::ScrollIntoViewWithOptionalAnimation.
	internal void ScrollIntoViewWithOptionalAnimation(
		object item,
		ScrollIntoViewAlignment alignment,
		bool disableAnimation)
	{
		bool isHeader = false;
		bool isFooter = false;

		var spHeaderOrFooter = Header;
		isHeader = Equals(spHeaderOrFooter, item);

		if (!isHeader)
		{
			spHeaderOrFooter = Footer;
			isFooter = Equals(spHeaderOrFooter, item);
		}

		ScrollIntoViewInternal(item, isHeader, isFooter, true /*isFromPublicAPI*/, alignment, 0.0 /*offset*/, !disableAnimation /*animateIfBringIntoView*/);
	}

	// Called when a pointer moves within a ListViewBase.
	protected override void OnPointerMoved(PointerRoutedEventArgs pArgs)
	{
		if (pArgs is null)
		{
			throw new ArgumentNullException(nameof(pArgs));
		}

		base.OnPointerMoved(pArgs);

		// XAML Drag and Drop on desktop uses drag events to track drag operations
		// TODO Uno: DragDrop.GetUseCoreDragDrop does not exist, Uno never uses core drag and drop.
		const bool useCoreDragDrop = false;
		if (!useCoreDragDrop)
		{
			if (!pArgs.Handled)
			{
				bool wasGestureHandled = false;

				var spPointerPoint = pArgs.GetCurrentPoint(this);
				if (spPointerPoint is null)
				{
					throw new ArgumentNullException(nameof(spPointerPoint));
				}

				Point newPosition = spPointerPoint.Position;

				var spPointer = pArgs.Pointer;
				wasGestureHandled = OnDragMoveGesture(spPointer, newPosition);
				if (wasGestureHandled)
				{
					pArgs.Handled = true;
				}
			}
		}
	}

	// Called when the user releases a pointer over the ListViewBase.
	protected override void OnPointerReleased(PointerRoutedEventArgs pArgs)
	{
		if (pArgs is null)
		{
			throw new ArgumentNullException(nameof(pArgs));
		}

		base.OnPointerReleased(pArgs);

		// XAML Drag and Drop on desktop uses drag events to track drag operations
		// TODO Uno: DragDrop.GetUseCoreDragDrop does not exist, Uno never uses core drag and drop.
		const bool useCoreDragDrop = false;
		if (!useCoreDragDrop)
		{
			if (!pArgs.Handled)
			{
				bool wasGestureHandled = false;
				Point dropPoint = default;

				// TODO Uno: the window is reached through the XamlRoot.
				var spVisualRootAsUIE = XamlRoot?.Content;
				if (spVisualRootAsUIE is null)
				{
					return;
				}

				var spPointerPoint = pArgs.GetCurrentPoint(spVisualRootAsUIE);
				dropPoint = spPointerPoint.Position;

				var spPointer = pArgs.Pointer;
				wasGestureHandled = OnDropGesture(
					spPointer,
					dropPoint);

				if (wasGestureHandled)
				{
					pArgs.Handled = true;
				}
			}
		}
	}

	// Called when the ListViewBase or its children lose mouse capture.
	protected override void OnPointerCaptureLost(PointerRoutedEventArgs pArgs)
	{
		bool isOriginalSource = false;

		base.OnPointerCaptureLost(pArgs);

		// If we are the original source, terminate any mouse-initiated drag.
		// We must check the source because the ListViewItem (our child) will
		// relinquish mouse capture to us when a drag starts, thus causing this
		// method to be called with the child as the original source. We don't want to
		// terminate the drag at that time, hence this check.
		var spSource = pArgs.OriginalSource;
		isOriginalSource = ReferenceEquals(spSource, this);

		if (isOriginalSource)
		{
			bool wasGestureHandled = false;
			Point dropPoint = default;

			// TODO Uno: the window is reached through the XamlRoot.
			var spVisualRootAsUIE = XamlRoot?.Content;
			if (spVisualRootAsUIE is null)
			{
				return;
			}

			var spPointer = pArgs.Pointer;
			var spPointerPoint = pArgs.GetCurrentPoint(spVisualRootAsUIE);
			dropPoint = spPointerPoint.Position;

			wasGestureHandled = OnDropGesture(
				spPointer,
				dropPoint);

			if (wasGestureHandled)
			{
				pArgs.Handled = true;
			}
		}
	}

	// Apply a template to the ListViewBase.
	protected override void OnApplyTemplate()
	{
		if (m_tpScrollViewer is not null)
		{
			m_tpScrollViewer.SetDirectManipulationStateChangeHandler(null);

			// ScrollViewer is reset in Selector::OnApplyTemplate, so the property changed handler should be detached.
			DetachScrollViewerPropertyChanged();

			// Detach from the ScrollViewer's ViewChanging handler
			m_epScrollViewerViewChangingHandler.Disposable = null;
		}

		base.OnApplyTemplate();

		// Update whether we're listening for the ScrollHost's property changes
		// when the template is changed (and the ScrollHost updated)
		EnsureScrollHostOffsetChangeAction();

		// Attach scroll viewer property handler.
		AttachScrollViewerPropertyChanged();

		// Tell our ScrollHost to ignore PointerPressed as well as SemanticZoom input
		// when it's used in conjunction with a SemanticZoom
		if (m_tpScrollViewer is not null)
		{
			m_tpScrollViewer.m_templatedParentHandlesMouseButton = true;

#if !HAS_UNO // TODO Uno: DManip is inert, see NotifyStateChange.
			m_tpScrollViewer.SetDirectManipulationStateChangeHandler(this);
#endif

			global::System.Diagnostics.Debug.Assert(m_epScrollViewerViewChangingHandler.Disposable is null);

			var scrollViewer = m_tpScrollViewer;
			EventHandler<ScrollViewerViewChangingEventArgs> viewChangingHandler = (_, _) => ResetAllItemsForLiveReorder();
			scrollViewer.ViewChanging += viewChangingHandler;
			m_epScrollViewerViewChangingHandler.Disposable = Disposable.Create(() => scrollViewer.ViewChanging -= viewChangingHandler);
		}
	}

	// sets a clip the same size as we are, so that elements can portal and be clipped appropriately

	// make sure to call this after an arrange has occurred, since that is the moment the ItemsHost
	// will have updated its ItemBounds fully.
	internal void UpdateClip()
	{
		Rect bounds = default;

		// use the clip to communicate (and clip) how far elements can move for portal-ing
		// it is an indirect contract between AddDeleteThemeTransition and the elements that it
		// will portal based on the local clip set on the parent of the elements (the panel).
		var spItemsHost = ItemsPanelRoot;

		if (spItemsHost is not null && spItemsHost is not ModernCollectionBasePanel)
		{
			var spItemsHostAsUIElement = (UIElement)spItemsHost;
			var spItemsHostAsItemLookupPanel = spItemsHost as IItemLookupPanel;

			// only set a local clip if we have anything useful
			// transitions will choose differently if no clip is set
			if (spItemsHostAsItemLookupPanel is not null)
			{
				bounds = spItemsHostAsItemLookupPanel.GetItemsBounds();
				m_maxContentWidth = Math.Max(m_maxContentWidth, bounds.Width);
				m_maxContentHeight = Math.Max(m_maxContentHeight, bounds.Height);

				var spItemsHostAsOrientedPanel = spItemsHost as IOrientedPanel;
				if (spItemsHostAsOrientedPanel is not null)
				{
					// if this is an oriented panel, we can be better by expanding the bounds in the direction of
					// of scrolling. Need to do that otherwise the clip is too intrusive:
					// Imagine a wrap grid with 4 elements and a height of 3 elements (so two columns, 2nd column showing one element).
					// When we remove an element from the first column, the clip that we are getting back would immediately clip out
					// the LTE that is waiting to be portal-ed in the 2nd column
					Orientation orientation = spItemsHostAsOrientedPanel.PhysicalOrientation;
					if (orientation == Orientation.Vertical)
					{
						bounds.Height = (float)m_maxContentHeight;
					}
					else
					{
						bounds.Width = (float)m_maxContentWidth;
					}
				}

				var spClipGeometry = spItemsHostAsUIElement.Clip as RectangleGeometry;

				if (spClipGeometry is null)
				{
					RectangleGeometry spClipGeometryImpl = new();
					spItemsHostAsUIElement.Clip = spClipGeometryImpl;
					spClipGeometry = spClipGeometryImpl;
				}

				spClipGeometry.Rect = bounds;
			}
		}
	}

	protected override Size ArrangeOverride(Size arrangeSize)
	{
		// allow panel to arrange
		var returnValue = base.ArrangeOverride(arrangeSize);

		// In case of DataVirtualization, if number of of items on screen are less than availableSize,
		// this will trigger to LoadMoreItems
		if (m_scrollHostOffsetChangeAction == ScrollHostOffsetChangeAction.ScrollHostOffsetChangeAction_IncrementalEdgeTrigger)
		{
			LoadMoreItemsIfNeeded(arrangeSize);
		}

		// also executed from OnScrollHostPropertyChanged
		UpdateClip();

		return returnValue;
	}

	protected override Size MeasureOverride(Size availableSize)
	{
#if !HAS_UNO // TODO Uno: ETW telemetry only.
		// ETW Trace, we want to raise an ETW event here if we can determine that the configuration
		// does not allow virtualization to happen
		if (EventEnabledVirtualizationIsEnabledByLayoutInfo())
		{
			bool isVirtualizationActive = true;
			var spItemsPanel = ItemsPanelRoot;

			if (spItemsPanel is ModernCollectionBasePanel modernPanel)
			{
				if (availableSize.Width == float.PositiveInfinity
					|| availableSize.Height == float.PositiveInfinity)
				{
					var spLayoutStrategy = modernPanel.GetLayoutStrategy();
					Orientation orientation = spLayoutStrategy.GetVirtualizationDirection();

					// if we can't determine strategy, we can't determine true state of virtualization, just jump to cleanup
					// and not log anything.
					bool isWrappingStrategy = spLayoutStrategy.GetIsWrappingStrategy();

					float constraint = (orientation == Orientation.Horizontal)
											? (float)availableSize.Width : (float)availableSize.Height;

					// for wrapping strategy, the virtualization is active only when the availableSize is finite in both directions
					// which will never be true here in this block of code
					// otherwise, virtualization is effectively disabled if the constraint is infinite
					isVirtualizationActive = !isWrappingStrategy && constraint != float.PositiveInfinity;
				}
				var spParent = Parent;
				TraceVirtualizationIsEnabledByLayoutInfo1(
					isVirtualizationActive,
					(ulong)GetHashCode(),
					Name,
					GetType().Name,
					(spParent is not null) ? spParent.GetType().Name : "NULL"
				);
			} // else if not modern panel, we shouldn't trace it here.
		}
#endif

		return base.MeasureOverride(availableSize);
	}

	// In case of DataVirtualization, if number of of items on screen are less than availableSize,
	// this will trigger to LoadMoreItems
	private void LoadMoreItemsIfNeeded(Size finalSize)
	{
		var spItemsHost = ItemsPanelRoot;
		if (spItemsHost is not null)
		{
			var spItemsHostAsItemLookupPanel = spItemsHost as IItemLookupPanel;
			var spItemsHostAsOrientedPanel = spItemsHost as IOrientedPanel;

			if (spItemsHostAsItemLookupPanel is not null && spItemsHostAsOrientedPanel is not null)
			{
				Rect bounds = default;
				Orientation orientation = spItemsHostAsOrientedPanel.PhysicalOrientation;
				bounds = spItemsHostAsItemLookupPanel.GetItemsBounds();

				if ((orientation == Orientation.Vertical && bounds.Height <= finalSize.Height) ||
					(orientation == Orientation.Horizontal && bounds.Width <= finalSize.Width))
				{
					OnScrollHostOffsetsChanged();
				}
			}
		}
	}

	// Adds or removes a property changed handler to the ScrollHost.
	private void EnsureScrollHostOffsetChangeAction()
	{
		bool incrementalEdgeTriggerPossible = false;

		if (m_tpScrollViewer is not null)
		{
			IncrementalLoadingTrigger incrementalLoadingTrigger = IncrementalLoadingTrigger.None;

			var spItemsSource = ItemsSource;

			if (spItemsSource is not null)
			{
				// See if we could activate incremental loading edge trigger.
				incrementalLoadingTrigger = IncrementalLoadingTrigger;
				if (incrementalLoadingTrigger == IncrementalLoadingTrigger.Edge)
				{
					incrementalEdgeTriggerPossible = IncrementalLoadingAdapter.SupportsIncrementalLoading(spItemsSource);
				}
			}
		}

		// Determine which action we want to take when scrolling happens.
		m_scrollHostOffsetChangeAction = incrementalEdgeTriggerPossible ? ScrollHostOffsetChangeAction.ScrollHostOffsetChangeAction_IncrementalEdgeTrigger : ScrollHostOffsetChangeAction.ScrollHostOffsetChangeAction_None;
	}

	// Adds a property changed handler to the ScrollHost.
	// Used to call OnScrollHostOffsetsChanged for Data Virtualization.
	private void AttachScrollViewerPropertyChanged()
	{
		if (m_tpOffsetChangedHandler is null && m_tpScrollViewer is not null)
		{
			var scrollViewer = m_tpScrollViewer;

			// TODO Uno: the DP changed event source is replaced by property changed callbacks. The extents are watched as
			// well, because OnScrollHostPropertyChanged reacts to them.
			IDisposable Register(DependencyProperty property)
			{
				long token = scrollViewer.RegisterPropertyChangedCallback(property, (sender, dp) => OnScrollHostPropertyChanged(sender, dp));
				return Disposable.Create(() => scrollViewer.UnregisterPropertyChangedCallback(property, token));
			}

			m_tpOffsetChangedHandler = new CompositeDisposable(
				Register(ScrollViewer.HorizontalOffsetProperty),
				Register(ScrollViewer.VerticalOffsetProperty),
				Register(ScrollViewer.ExtentWidthProperty),
				Register(ScrollViewer.ExtentHeightProperty));
		}
	}

	// Removes the property changed handler we attached in AttachScrollViewerPropertyChanged
	// from the ScrollHost. Used for Data Virtualization.
	private void DetachScrollViewerPropertyChanged()
	{
		if (m_tpOffsetChangedHandler is not null)
		{
			m_tpOffsetChangedHandler.Dispose();
			m_tpOffsetChangedHandler = null;
		}
	}

	// ScrollHost property changed handler callback
	private void OnScrollHostPropertyChanged(
		DependencyObject pSender,
		DependencyProperty pDP)
	{
		// TODO: Just replace with a check on the property indices for speed
		if ((pDP == ScrollViewer.HorizontalOffsetProperty) ||
			(pDP == ScrollViewer.VerticalOffsetProperty))
		{
			OnScrollHostOffsetsChanged();
		}
		else
		{
			if ((pDP == ScrollViewer.ExtentWidthProperty) ||
				(pDP == ScrollViewer.ExtentHeightProperty))
			{
				// the fact that the extent has changed means that we are going to have a dirty layout pass
				// anyway. There are side effects to this ListView's arrange (calling UpdateClip) that need
				// to execute after the arrange has occurred. The cheapest way of doing that is making sure
				// we participate in the arrange walk.
				InvalidateArrange();
			}
		}
	}

	// Called when the ScrollHost's scrolling offsets changes.
	private void OnScrollHostOffsetsChanged()
	{
		ProcessDataVirtualizationScrollOffsets();
	}

	internal override Point GetDropOffsetToRoot() => m_lastReorderPosition;

	// DirectManipulationStateChangeHandler implementation
	private void NotifyStateChange(
		DMManipulationState state,
		float xCumulativeTranslation,
		float yCumulativeTranslation,
		float zCumulativeFactor,
		float xCenter,
		float yCenter,
		bool isInertial,
		bool isTouchConfigurationActivated,
		bool isBringIntoViewportConfigurationActivated)
	{
		switch (state)
		{
			case DMManipulationState.DMManipulationStarted:
				ClearHoldingState();
				break;
		}
	}

	// return string representation of Header property
	internal override string GetPlainText()
	{
		string strPlainText = "";

		var spHeader = Header;

		if (spHeader is not null)
		{
			strPlainText = GetStringFromObject(spHeader);
		}

		return strPlainText;
	}

	// Returns next focusable element in a specified direction.
	// Since Get{First,Last}FocusableElementOverride is called in different context after refactoring change 601761
	// this method needs to be called from Panel::GetFirstFocusableElementOverride in order to retain the same call order as in Win8.
	internal DependencyObject? GetFocusableElement(bool isBackward)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: GetFocusableElement. isBackward={isBackward}");
#endif

		bool shouldCustomizeTabNavigation = false;
		KeyboardNavigationMode navigationMode;
		GroupItem? spGroupItem = null;
		DependencyObject? spFirstFocusableResult = null;

		// ShouldCustomizeTabNavigation returns whether the ItemsPanel is a ModernCollectionBasePanel or not (shouldCustomizeTabNavigation is set to True for modern panels).
		shouldCustomizeTabNavigation = ShouldCustomizeTabNavigation();

		navigationMode = TabNavigation;

		// The modern panels may not have to use this GetFirst/GetLast mechanism for controlling tab stop navigation, but rather
		// ProcessTabStopOverride and ProcessCandidateTabStopOverride as those methods work for some scenarios involving
		// enter, in-LVB and exit transitions.
		// There are cases though requiring the following custom processing, even for modern panels and the default Once TabNavigation:
		// - ListViewBaseItem instances have nested focusable elements and the 2 overrides above are not invoked for this ListViewBase.
		// - irrespective of the TabNavigation mode, the last focused index (returned by GetLastFocusedIndex()) may not be realized
		//   anymore and may require item realization, scrolling and preparation.
		bool isUsingTabNavigationOnce = navigationMode == KeyboardNavigationMode.Once;
		bool isUsingTabNavigationCycle = navigationMode == KeyboardNavigationMode.Cycle;
		bool scrollIntoViewAndPrepareContainer = false;
		bool isGrouping = false;
		bool hasFocus = false;
		int targetFocusedIndex = -1;
		int targetGroupFocusedIndex = -1;

		isGrouping = IsGrouping;
		hasFocus = this.HasFocus();

		// For modern panels (i.e. shouldCustomizeTabNavigation == True),
		// - for TabNavigation modes Local or Cycle, the first (for isBackward==False) or last (for isBackward==True) item may not be realized,
		// - for TabNavigation mode Once, the last focused item may not be realized.
		// In those cases, that item must be realized and prepared first and selected as the focusable element.
		if (!isGrouping && shouldCustomizeTabNavigation)
		{
#if !HAS_UNO // TODO Uno: FocusManager does not track IsMovingFocusToNextTabStop / IsMovingFocusToPreviousTabStop.
			var focusManager = VisualTree.GetFocusManagerForElement(this);

			global::System.Diagnostics.Debug.Assert(focusManager is not null);

			if ((!isBackward && focusManager.IsMovingFocusToNextTabStop()) || (isBackward && focusManager.IsMovingFocusToPreviousTabStop()))
			{
				// The FocusManager is processing the Tab key to actually navigate to an element (as opposed to only accessing a tab stop).
				scrollIntoViewAndPrepareContainer = true;
			}
#endif
		}

		// If neither focused item nor focused group exists, retrieve GroupItem for last focused item, if grouping.
		// If not grouping, get container for last focused item.
		// Additional check for HasFocus is necessary because focused index, etc. are set on OnGotFocus/LostFocus and this is async,
		// so even if the indices are set they may be slightly out of sync. Check that focus is within this ListViewBase before using the indices.
		// There is still a possibility that they will not be up to date if focus was rapidly changed within the ListViewBase, but that is not an important
		// scenario.
		// Even when HasFocus is True, when TabNavigation is Cycle, the target index may have to be realized and prepared when cycling from the first to
		// the last item or vice-versa.
		if ((GetFocusedIndex() < 0 && GetFocusedGroupIndex() < 0) ||
			(isUsingTabNavigationCycle && scrollIntoViewAndPrepareContainer) ||
			!hasFocus)
		{
			if (isGrouping)
			{
				GetGroupFromItem(
					GetLastFocusedIndex(),
					out _,
					out spGroupItem,
					out targetGroupFocusedIndex,
					out _);
			}

			if (spGroupItem is null)
			{
				if (!isUsingTabNavigationOnce)
				{
					uint itemCount = 0;

					itemCount = (uint)GetItemCount();

					if (itemCount > 0)
					{
						global::System.Diagnostics.Debug.Assert(!(isUsingTabNavigationCycle && isBackward && GetFocusedIndex() == 0 && !hasFocus));
						global::System.Diagnostics.Debug.Assert(!(isUsingTabNavigationCycle && !isBackward && GetFocusedIndex() == itemCount - 1 && !hasFocus));

						if (!isUsingTabNavigationCycle ||                                                     // TabNavigation is Local, tabbing into the first item or shift-tabbing into the last one.
							(isUsingTabNavigationCycle && !hasFocus) ||                                       // TabNavigation is Cycle, hasFocus is False, tabbing into the first item or shift-tabbing into the last one.
							(isUsingTabNavigationCycle && isBackward && GetFocusedIndex() == 0) ||            // TabNavigation is Cycle, hasFocus is True, alter the default behavior when shift-tabbing from the first to the last item.
							(isUsingTabNavigationCycle && !isBackward && GetFocusedIndex() == itemCount - 1)) // TabNavigation is Cycle, hasFocus is True, alter the default behavior when tabbing from the last to the first item.
						{
							targetFocusedIndex = (!isBackward) ? 0 : (int)(itemCount - 1);
						}
					}
				}
				else
				{
					targetFocusedIndex = (int)GetLastFocusedIndex();
				}
			}
		}

		if (spGroupItem is not null)
		{
			global::System.Diagnostics.Debug.Assert(targetFocusedIndex < 0);
			spFirstFocusableResult = spGroupItem;
		}
		else if (targetFocusedIndex >= 0)
		{
			spFirstFocusableResult = ContainerFromIndex(targetFocusedIndex);

			if (scrollIntoViewAndPrepareContainer)
			{
				var itemContainerCandidate = spFirstFocusableResult;

				// Make sure the target index is realized and prepared so it can be declared focusable and tabbed into.
				// Since this method brings the target index into view, it is only invoked when the FocusManager is processing
				// the Tab key to actually navigate to an element (as opposed to only accessing a tab stop).
				spFirstFocusableResult = GetScrolledIntoViewAndPreparedContainer(targetFocusedIndex, itemContainerCandidate);
			}
		}

		return spFirstFocusableResult;
	}

	// Shared implementation for GetFirstFocusableElementOverride and GetLastFocusableElementOverride.
	private DependencyObject? GetFocusableElementForModernPanel(bool isBackward)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: GetFocusableElementForModernPanel. isBackward={isBackward}");
#endif

		bool shouldCustomizeTabNavigation = false;
		DependencyObject? ppFocusable = null;

		shouldCustomizeTabNavigation = ShouldCustomizeTabNavigation();

		if (shouldCustomizeTabNavigation)
		{
			// Takes care of the initial case where focused element is still NULL and
			// ProcessTabOverride has not been called.
			var spFocusedElement = XamlRoot is { } xamlRoot ? FocusManager.GetFocusedElement(xamlRoot) as DependencyObject : null;

			if (spFocusedElement is null && !m_focusableElementForModernPanelReentrancyGuard)
			{
				// there is a reentrancy issue since ListView calls back into focus manager and
				// he will call back into the ListView if it happens to be that the ListView is hosted in a popup
				// and there are no other focusable elements in the scene
				m_focusableElementForModernPanelReentrancyGuard = true;

				try
				{
					ProcessTabStopInternal(
						null, // pFocusedElement
						null, // pCandidateElement
						isBackward,
						false,   // didCycleFocusAtRootVisualScope,
						out _,
						out ppFocusable);
				}
				finally
				{
					m_focusableElementForModernPanelReentrancyGuard = false;
				}
			}
		}

		return ppFocusable;
	}

	// Gets first element that should take focus after Tab.
	internal override DependencyObject? GetFirstFocusableElementOverride()
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: GetFirstFocusableElementOverride.");
#endif

		return GetFocusableElementForModernPanel(false);
	}

	// Gets last element that should take focus after Shift+Tab.
	internal override DependencyObject? GetLastFocusableElementOverride()
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: GetLastFocusableElementOverride.");
#endif

		return GetFocusableElementForModernPanel(true);
	}

	// Needs override in Win8 RTM and higher to preserve focus on focused item when Focus() is called on a non
	// focusable ListViewBase. See explanation under quirked code for more details.
	internal override bool FocusImpl(FocusState value)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: FocusImpl. focusState={value}");
#endif

		bool isFocusable = false;
		int targetFocusedIndex = -1;
		int targetGroupFocusedIndex = -1;
		bool hasFocus = false;
		bool focused = false;

		// Win8 RTM and higher - ListViewBase handles focus through a FocusManager callback instead of
		// having items transfer focus after they get focused. This enables TabNavigation.Cycle and programmatic
		// focus to work, but for the case where an item within ListViewBase already has focus and then Focus() is
		// called on ListViewBase, we need to go to the item which had focus assuming ListViewBase is not focusable.

		// Check if ListViewBase is focusable - if it is, it can take focus itself and there is no need to transfer
		// focus elsewhere. However, it may not be Focusable, e.g. IsTabStop=False, in which case focus should go to
		// a focused or focusable child.
		isFocusable = IsFocusable;

		if (!isFocusable)
		{
			// Next check to see if the ListViewBase currently has focus on an item/group using HasFocus and focused indices.
			// If nothing is currently focused, we'll fall back to the FocusManager::GetFirstFocusableElement path which will
			// return the previously focused item or group. However, if an item within ListViewBase is currently
			// focused the GetFirstFocusableElement callback will not return anything, since it mainly handles the case
			// where focus is coming from outside. This is necessary because FocusManager calls GetFirst/LastFocusable child both for
			// tabbing and programmatic focus and doesn't have any specific overrides for tabs, which is what ListViewBase really requires.
			hasFocus = this.HasFocus();
			if (hasFocus)
			{
				if (GetFocusedIndex() >= 0)
				{
					targetFocusedIndex = GetFocusedIndex();
				}
				else if (GetFocusedGroupIndex() >= 0)
				{
					targetGroupFocusedIndex = GetFocusedGroupIndex();
				}
			}

			if (targetFocusedIndex >= 0 ||
				targetGroupFocusedIndex >= 0)
			{
				DependencyObject? spContainer;

				if (targetFocusedIndex >= 0)
				{
					spContainer = ContainerFromIndex(targetFocusedIndex);
				}
				else
				{
					spContainer = HeaderFromIndex(targetGroupFocusedIndex);
				}

				if (spContainer is not null)
				{
					focused = ((FrameworkElement)spContainer).FocusImpl(value);
				}
			}
		}

		if (!focused)
		{
			focused = base.FocusImpl(value);
		}

		return focused;
	}

	// UIElement override for getting next tab stop on path from focused element to root.
	internal override TabStopProcessingResult ProcessTabStopOverride(
		DependencyObject? pFocusedElement,
		DependencyObject? pCandidateTabStopElement,
		bool isBackward,
		bool didCycleFocusAtRootVisualScope)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: ProcessTabStopOverride. isBackward={isBackward}");
#endif

		ProcessTabStopInternal(
				pFocusedElement,
				pCandidateTabStopElement,
				isBackward,
				didCycleFocusAtRootVisualScope,
				out var pIsTabStopOverridden,
				out var ppNewTabStop);

		return new TabStopProcessingResult(pIsTabStopOverridden, ppNewTabStop);
	}

	// UIElement override for getting next tab stop on path from focus candidate element to root.
	internal override TabStopProcessingResult ProcessCandidateTabStopOverride(
		DependencyObject? pFocusedElement,
		DependencyObject pCandidateTabStopElement,
		DependencyObject? pOverriddenCandidateTabStopElement,
		bool isBackward)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: ProcessCandidateTabStopOverride. isBackward={isBackward}");
#endif

		bool pIsCandidateTabStopOverridden = false;
		DependencyObject? ppNewTabStop = null;

		bool isAncestor = pFocusedElement is not null && this.IsAncestorOf(pFocusedElement);

		if (!isAncestor)
		{
			KeyboardNavigationMode navigationMode = TabNavigation;

			if (navigationMode == KeyboardNavigationMode.Once)
			{
				// For KeyboardNavigationMode == Once, tab or shift-tab into the previously visited item, if any.
				// Or first/last selectable item otherwise.
				ProcessTabStopInternal(
					null, // pFocusedElement
					null, // pCandidateElement
					isBackward,
					false,   // didCycleFocusAtRootVisualScope,
					out pIsCandidateTabStopOverridden,
					out ppNewTabStop);
			}
			else
			{
				// Attempt to find a tab stop in this priority order: the header if present,
				// the group headers and items, finally the footer if present. That order is
				// reversed when shift-tabbing.
				ElementType newElementType = ElementType.Other;
				DependencyObject? nextElement;

				TryNextElementType(
					ElementType.Other /*initialElementType*/,
					isBackward ? ElementType.Footer : ElementType.Header /*candidateElementType*/,
					isBackward,
					out newElementType,
					out nextElement);

				if (nextElement is null)
				{
					TryNextElementType(
						ElementType.Other /*initialElementType*/,
						ElementType.ItemOrGroupHeader /*candidateElementType*/,
						isBackward,
						out newElementType,
						out nextElement);
				}

				if (nextElement is null)
				{
					TryNextElementType(
						ElementType.Other /*initialElementType*/,
						isBackward ? ElementType.Header : ElementType.Footer /*candidateElementType*/,
						isBackward,
						out newElementType,
						out nextElement);
				}

				if (nextElement is not null)
				{
					// When a tab stop element is found, try to locate its first or last inner tab stop
					// when tabbing or shift-tabbing respectively.
					DependencyObject? tabStopNative;

					if (isBackward)
					{
						tabStopNative = FocusManager.FindLastFocusableElement(nextElement);
					}
					else
					{
						tabStopNative = FocusManager.FindFirstFocusableElement(nextElement);
					}

					if (tabStopNative is not null)
					{
						ppNewTabStop = tabStopNative;
					}
					else
					{
						// Fall back to using the element itself when no inner tab stop was found.
						ppNewTabStop = nextElement;
					}

					pIsCandidateTabStopOverridden = true;
				}
			}
		}

		return new TabStopProcessingResult(pIsCandidateTabStopOverridden, ppNewTabStop);
	}

	// Ensure itemIndex is within valid range for group with index of groupIndex.  If forceSet is TRUE, then
	// set pNewItemIndex to the first item index (atBeginning = TRUE) or the last.
	private uint ValidateItemIndexForGroup(
		uint itemIndex,
		uint groupIndex,
		bool atBeginning,
		bool forceSet)
	{
		uint groupStartIndex = 0;
		uint groupSize = 0;
		bool isValid = false;

		uint pNewItemIndex = itemIndex;

		GetGroupInformation(
			groupIndex,
			out groupStartIndex,
			out groupSize,
			out isValid);

		if (isValid &&
			groupSize > 0)
		{
			if (forceSet ||
				itemIndex < groupStartIndex ||
				itemIndex >= (groupStartIndex + groupSize))
			{
				if (atBeginning)
				{
					pNewItemIndex = groupStartIndex;
				}
				else
				{
					pNewItemIndex = groupStartIndex + groupSize - 1;
				}
			}
		}

		return pNewItemIndex;
	}

	// Test if there are any elements in group.
	private bool GroupHasElements(uint groupIndex)
	{
		int groupCount = 0;

#if DEBUG
		bool isGrouping = IsGrouping;
		global::System.Diagnostics.Debug.Assert(isGrouping);
#endif

		bool pGroupHasElements = false;

		groupCount = GetGroupCount();

		if (groupCount > 0)
		{
			uint groupStartIndex = 0;
			uint groupSize = 0;
			bool isValid = false;

			GetGroupInformation(
				groupIndex,
				out groupStartIndex,
				out groupSize,
				out isValid);

			pGroupHasElements = (isValid && groupSize > 0);
		}

		return pGroupHasElements;
	}

	// Test if there are empty group headers at the beginning or at the end.
	private bool HasBoundaryEmptyGroup(bool atBeginning)
	{
		bool isGrouping = false;
		int groupCount = 0;

		bool pHasBoundaryEmptyGroup = false;

		isGrouping = IsGrouping;

		if (isGrouping)
		{
			groupCount = GetGroupCount();

			if (groupCount > 0)
			{
				int groupIndex = 0;

				if (atBeginning)
				{
					GetGroupFromItem(
						0,
						out _,
						out _,
						out groupIndex,
						out _);

					pHasBoundaryEmptyGroup = (groupIndex > 0);
				}
				else
				{
					uint itemCount = 0;

					itemCount = (uint)GetItemCount();

					if (itemCount > 0)
					{
						GetGroupFromItem(
							itemCount - 1,
							out _,
							out _,
							out groupIndex,
							out _);

						pHasBoundaryEmptyGroup = (groupIndex + 1 < groupCount);
					}
				}
			}
		}

		return pHasBoundaryEmptyGroup;
	}

	// Walk up visual tree from pChild towards this LVB, looking for the last element which is an
	// item, group header item, header or footer.
	private void IdentifyParentElement(
		DependencyObject? pChild,
		out ElementType pParentElementType,
		out DependencyObject? pParent)
	{
		var spCurrent = pChild;
		DependencyObject? spLastFoundParent = null;
		ContentControl? spHeaderContainer = null;
		ContentControl? spFooterContainer = null;

		pParentElementType = ElementType.Other;
		pParent = null;

		var spItemsPresenter = ItemsPresenter;

		if (spItemsPresenter is not null)
		{
			spHeaderContainer = spItemsPresenter.HeaderContentControl;
			spFooterContainer = spItemsPresenter.FooterContentControl;
		}

		while (spCurrent is not null)
		{
			bool found = false;
			object pCurrentAsInspectable = spCurrent;

			found = ReferenceEquals(this, pCurrentAsInspectable);

			// We are interested in the closest element to the LVB, therefore keep searching
			// until we are at the root of hierarchy or parent LVB.

			if (found)
			{
				break;
			}

			// Since most likely the number of items is the greatest, test it first.
			if (IsListViewBaseItem(pCurrentAsInspectable) || IsListViewBaseHeaderItem(pCurrentAsInspectable))
			{
				spLastFoundParent = spCurrent;
				pParentElementType = ElementType.ItemOrGroupHeader;
			}
			else
			{
				// Is it a header?
				found = ReferenceEquals(spHeaderContainer, pCurrentAsInspectable);

				if (found)
				{
					spLastFoundParent = spCurrent;
					pParentElementType = ElementType.Header;
				}
				else
				{
					// Maybe a footer?
					found = ReferenceEquals(spFooterContainer, pCurrentAsInspectable);

					if (found)
					{
						spLastFoundParent = spCurrent;
						pParentElementType = ElementType.Footer;
					}
				}
			}

			{
				// Advance to parent.
				var spParent = VisualTreeHelper.GetParent(spCurrent);
				spCurrent = spParent;
			}
		}

		pParent = spLastFoundParent;
	}

	// Updates flags which determine whether items and group headers are visited during tab navigation.
	// In order to avoid potential realization of all elements, only one item/group header are checked.
	// The assumption is that all elements of one type will have the same tab stop value.
	// Violating this assumption will have the following consequences:
	// * prototype is tab-stoppable but there is an element which is not - tabbing will get stuck on
	//   last element which was tab-stoppable.
	// * prototype is not tab-stoppable but there is element which is - the element will not be reached.
	// For the purpose of tab navigation, the following properties are taken into account to make a determination
	// whether item/group header should be visited:
	// It's not a Control, but visible UIElement OR
	// it's a Control which is visible, enabled and is either a tab stop or one of its children is a tab stop.
	private void UpdateTabStopFlags()
	{
		bool isGrouping = false;
		uint itemCount = 0;

		global::System.Diagnostics.Debug.Assert(m_firstAvailableItemForFocus == -1);
		global::System.Diagnostics.Debug.Assert(m_firstAvailableGroupHeaderForFocus == -1);

		m_itemsAreTabStops = false;
		m_groupsAreTabStops = false;

		m_firstAvailableItemForFocus = 0;
		m_firstAvailableGroupHeaderForFocus = 0;

		isGrouping = IsGrouping;
		itemCount = (uint)GetItemCount();

		if (itemCount > 0)
		{
			int itemCacheStart = -1;
			int itemCacheEnd = -1;

			GetCacheStartAndEnd(
				false /*isForGroupIndexes*/,
				out itemCacheStart,
				out itemCacheEnd);

			if (itemCacheStart >= 0)
			{
				// loop across the cached containers to find the first item that can focused
				while (!m_itemsAreTabStops && itemCacheStart <= itemCacheEnd)
				{
					var spContainer = ContainerFromIndex(itemCacheStart);

					if (spContainer is not null)
					{
						m_itemsAreTabStops = IsFocusableHelper(spContainer);
					}
					++itemCacheStart;
				}

				// if we were able to find an item to set focus on
				// we set the index for the first available item for focus
				if (m_itemsAreTabStops)
				{
					// itemCacheStart is incremented in the loop above hence we need to decrement it by 1
					m_firstAvailableItemForFocus = itemCacheStart - 1;
				}
			}
		}

		if (isGrouping)
		{
			int groupCount = 0;

			groupCount = GetGroupCount();

			if (groupCount > 0)
			{
				int groupCacheStart = -1;
				int groupCacheEnd = -1;

				GetCacheStartAndEnd(
					true /*isForGroupIndexes*/,
					out groupCacheStart,
					out groupCacheEnd);

				// loop across the cached header containers to find the first group header that can be focused
				while (!m_groupsAreTabStops && groupCacheStart <= groupCacheEnd)
				{
					var spHeader = HeaderFromIndex(groupCacheStart);

					if (spHeader is not null)
					{
						m_groupsAreTabStops = IsFocusableHelper(spHeader);
					}
					++groupCacheStart;
				}

				// if we were able to find a group header to set focus on
				// we set the index for the first available group header for focus
				if (m_groupsAreTabStops)
				{
					// groupCacheStart is incremented in the loop above hence we need to decrement it by 1
					m_firstAvailableGroupHeaderForFocus = groupCacheStart - 1;
				}
			}
		}
	}

	// Given a starting element type transition states until the next tab stop is found.
	private DependencyObject? GetNextTabStopForElementType(
		ElementType elementType,
		bool isBackward)
	{
		DependencyObject? spNextElement = null;
		ElementType candidateElementType = elementType;
		List<ElementType> visitedStates = new();

		do
		{
			// Sanity check to avoid infinite loop in case if the state machine can't determine next tab stop.
			if (visitedStates.Contains(candidateElementType))
			{
				global::System.Diagnostics.Debug.Assert(false, "State already visited");
				break;
			}

			visitedStates.Add(candidateElementType);

			TryNextElementType(
				elementType,
				candidateElementType,
				isBackward,
				out candidateElementType,
				out spNextElement);
		}
		while (spNextElement is null &&
			candidateElementType != ElementType.Other);

		return spNextElement;
	}

	// Get next tab stop for currently focused element type.
	private DependencyObject? GetNextTabStop(
		ElementType elementType,
		bool isBackward)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: GetNextTabStop. elementType={elementType}, isBackward={isBackward}");
#endif

		KeyboardNavigationMode navigationMode;

		navigationMode = TabNavigation;

		var spNextElement = GetNextTabStopForElementType(
			elementType,
			isBackward);

		// If did not find tab stop and navigation mode is Cycle, start over as if we are tabbing from outside element.
		if (spNextElement is null &&
			navigationMode == KeyboardNavigationMode.Cycle)
		{
			spNextElement = GetNextTabStopForElementType(
				ElementType.Other,
				isBackward);
		}

		// Go to the last focusable element within next tab stop if we are shift-tabbing.
		if (isBackward &&
			spNextElement is not null)
		{
			var pNextElementFromBack = FocusManager.FindLastFocusableElement(spNextElement);

			if (pNextElementFromBack is not null)
			{
				spNextElement = pNextElementFromBack;
			}
		}

		return spNextElement;
	}

	// State machine for determining next tab stop.
	// Given the current state information it tests if the transition to candidate state is a valid one
	// and if so, it returns a container for the next tab stop.  If the candidate is not accepted, it
	// advances the state.
	// initialElementType - type of element currently having focus
	// candidateElementType - state being tested
	// isBackward - direction
	// pNewElementType - new state transition
	// ppNextElement - next tab stop
	private void TryNextElementType(
		ElementType initialElementType,
		ElementType candidateElementType,
		bool isBackward,
		out ElementType pNewElementType,
		out DependencyObject? ppNextElement)
	{
		KeyboardNavigationMode navigationMode;
		DependencyObject? spNextElement = null;
		bool isGrouping = false;
		uint itemCount = 0;
		int groupCount = 0;

		pNewElementType = candidateElementType;
		ppNextElement = null;

		navigationMode = TabNavigation;
		isGrouping = IsGrouping;
		itemCount = (uint)GetItemCount();

		if (isGrouping)
		{
			groupCount = GetGroupCount();
		}

		bool supportsItemsAsTabStops = itemCount != 0 && m_itemsAreTabStops;
		bool supportsGroupHeadersAsTabStops = isGrouping && groupCount != 0 && m_groupsAreTabStops;

		switch (candidateElementType)
		{
			case ElementType.Header:
			case ElementType.Footer:
				{
					if (initialElementType == candidateElementType)
					{
						// Go to the next element type if this one already had focus.
						pNewElementType = GetElementTypeTransition(candidateElementType, isBackward);
					}
					else
					{
						// If not, get appropriate content control and if it is selectable, return it to caller.
						bool isHeader = (candidateElementType == ElementType.Header);

						var focusableFooterContainer = GetFocusableHeaderOrFooterContainer(isHeader);

						if (focusableFooterContainer is not null)
						{
							ScrollIntoView(
								0,
								false /*isGroupItemIndex*/,
								isHeader  /*isHeader*/,
								!isHeader /*isFooter*/,
								false /*isFromPublicAPI*/,
								true  /*ensureContainerRealized*/,
								false /*animateIfBringIntoView*/,
								ScrollIntoViewAlignment.Default);

							spNextElement = focusableFooterContainer;
						}

						if (spNextElement is null)
						{
							// If container cannot be returned, advance the state.
							pNewElementType = GetElementTypeTransition(candidateElementType, isBackward);
						}
					}

					if (navigationMode == KeyboardNavigationMode.Local ||
						navigationMode == KeyboardNavigationMode.Cycle)
					{
						// Per spec: for local/cycle navigation modes iterate over all elements disregarding last focused element.
						// When entering from outside of items/groups, if the NavMode is not Once, reset the group and item indices
						// and the last focused element type.
						if (!isBackward)
						{
							SetLastFocusedGroupIndex(0);
							SetLastFocusedIndex(0);

							// Forward navigation with local/cycle should try to hit group headers first,
							// otherwise try items.
							if (supportsGroupHeadersAsTabStops)
							{
								m_lastFocusedElementType = ElementType.GroupHeader;
							}
							else if (supportsItemsAsTabStops)
							{
								m_lastFocusedElementType = ElementType.Item;
							}
						}
						else
						{
							if (itemCount > 0)
							{
								SetLastFocusedIndex(itemCount - 1);
							}

							if (isGrouping &&
								groupCount > 0)
							{
								SetLastFocusedGroupIndex(groupCount - 1);
							}

							// Backward navigation with local/cycle should try to hit items first,
							// otherwise try group headers.
							if (supportsItemsAsTabStops)
							{
								m_lastFocusedElementType = ElementType.Item;
							}
							else if (supportsGroupHeadersAsTabStops)
							{
								m_lastFocusedElementType = ElementType.GroupHeader;
							}
						}

						if (isGrouping &&
							pNewElementType == ElementType.Item &&
							!isBackward &&
							(initialElementType == ElementType.Header || initialElementType == ElementType.Other))
						{
							// If there are any empty group headers at the beginning, in Local or Cycle mode visit them first.
							bool hasBoundaryEmptyGroup = false;

							hasBoundaryEmptyGroup = HasBoundaryEmptyGroup(
								true);

							if (hasBoundaryEmptyGroup &&
								m_groupsAreTabStops)
							{
								pNewElementType = ElementType.GroupHeader;
							}
						}
					}
				}
				break;

			case ElementType.ItemOrGroupHeader:
				{
					if ((!supportsItemsAsTabStops && !supportsGroupHeadersAsTabStops)
						|| (initialElementType == ElementType.ItemOrGroupHeader && navigationMode == KeyboardNavigationMode.Once))
					{
						// If neither items nor group headers are supported as tab stops, or an item/group header already has focus and NavMove==Once,
						// then advance to the next state.
						pNewElementType = GetElementTypeTransition(ElementType.ItemOrGroupHeader, isBackward);
					}
					else
					{
						int newFocusIndex = -1;
						bool isGroupHeader = false;

						if (initialElementType != ElementType.ItemOrGroupHeader)
						{
							// The first time we tab into a list, either try to restore focus to the last element that had
							// focus within the list or focus either the first group header (if available) or the
							// first item (unless it's a bottoms up list, in which case we focus the last item).

							bool isGettingFocusForFirstTime = true;

							if (m_lastFocusedElementType != ElementType.Other)
							{
								// The control has previously had focus on either an item or group header, so we
								// should restore it.

								isGroupHeader = (m_lastFocusedElementType == ElementType.GroupHeader);
								newFocusIndex = (isGroupHeader ? GetLastFocusedGroupIndex() : (int)GetLastFocusedIndex());
								if (newFocusIndex >= 0 && newFocusIndex < (isGroupHeader ? groupCount : (int)itemCount))
								{
									// Last focused element was an item that is currently available in the data.
									isGettingFocusForFirstTime = false;
								}
							}

							if (isGettingFocusForFirstTime)
							{
								// This is the first time the control is getting focus, so focus the first group
								// header (if supported), otherwise the first item, except the case of bottoms-up
								// lists, where we will instead focus the last item.

								bool isBottomsUp = false;

								var panel = ItemsPanelRoot;
								var itemsStackPanel = panel as ItemsStackPanel;
								if (itemsStackPanel is not null)
								{
									var itemsUpdatingScrollMode = itemsStackPanel.ItemsUpdatingScrollMode;
									isBottomsUp = (itemsUpdatingScrollMode == ItemsUpdatingScrollMode.KeepLastItemInView);
								}

								if (isBottomsUp)
								{
									newFocusIndex = (int)itemCount - 1;
								}
								else
								{
									isGroupHeader = supportsGroupHeadersAsTabStops;
									newFocusIndex = (isGroupHeader ? m_firstAvailableGroupHeaderForFocus : m_firstAvailableItemForFocus);
								}
							}
						}
						else
						{
							// For subsequent tab navigation, while on a group header or item, just advance in order through
							// the list, similar to using arrow keys.

							if (m_lastFocusedElementType == ElementType.Item)
							{
								global::System.Diagnostics.Debug.Assert(supportsItemsAsTabStops);

								FindNextTabStopCandidateFromItem(
									(int)GetLastFocusedIndex(),
									isBackward,
									supportsGroupHeadersAsTabStops,
									out newFocusIndex,
									out isGroupHeader);
							}
							else if (m_lastFocusedElementType == ElementType.GroupHeader
								&& (!isBackward || GetLastFocusedGroupIndex() > 0))
							{
								global::System.Diagnostics.Debug.Assert(supportsGroupHeadersAsTabStops);

								uint newGroupStartIndex = 0;
								uint newGroupSize = 0;
								bool isValid = false;

								GetGroupInformation(
									(uint)((isBackward) ? GetLastFocusedGroupIndex() - 1 : GetLastFocusedGroupIndex()),
									out newGroupStartIndex,
									out newGroupSize,
									out isValid);

								if (isValid)
								{
									if (isBackward)
									{
										if (newGroupSize > 0)
										{
											// Focus the last item in the new group.
											newFocusIndex = (int)(newGroupStartIndex + newGroupSize - 1);
										}
										else
										{
											// The previous group is empty, so focus its header instead.
											newFocusIndex = GetLastFocusedGroupIndex() - 1;
											isGroupHeader = true;
										}
									}
									else
									{
										if (newGroupSize > 0)
										{
											// Focus the first item in the current group.
											newFocusIndex = (int)newGroupStartIndex;
										}
										else if (GetLastFocusedGroupIndex() < (groupCount - 1))
										{
											// The current group is empty, so focus the next group's header instead.
											newFocusIndex = GetLastFocusedGroupIndex() + 1;
											isGroupHeader = true;
										}
									}
								}
							}
						}

						if (newFocusIndex >= 0 && newFocusIndex < (isGroupHeader ? groupCount : (int)itemCount))
						{
							ScrollIntoView(
								(uint)newFocusIndex,
								isGroupHeader ? true : false /*isGroupItemIndex*/,
								false /*isHeader*/,
								false /*isFooter*/,
								false /*isFromPublicAPI*/,
								true  /*ensureContainerRealized*/,
								false /*animateIfBringIntoView*/,
								ScrollIntoViewAlignment.Default);

							if (isGroupHeader)
							{
								spNextElement = HeaderFromIndex(newFocusIndex);
							}
							else
							{
								DependencyObject? container;

								int itemCacheStart = -1;
								int itemCacheEnd = -1;
								int currentIndex = -1;

								GetCacheStartAndEnd(
									false /*isForGroupIndexes*/,
									out itemCacheStart,
									out itemCacheEnd);

								currentIndex = newFocusIndex < itemCacheStart ? itemCacheStart : newFocusIndex;
								currentIndex = newFocusIndex > itemCacheEnd ? itemCacheEnd : newFocusIndex;

								// Make sure our candidate item is selectable.  If not, keep trying until we find
								// one that is.  We'll do this within the currently realized items to prevent going
								// through the entire list and if we fail to find a selectable item or group header
								// within that set, we'll just end up advancing to the next element type.
								while (currentIndex >= itemCacheStart && currentIndex <= itemCacheEnd)
								{
									container = ContainerFromIndex(currentIndex);
									if (container is not null)
									{
										bool isFocusable = false;

										isFocusable = IsFocusableHelper(container);

										if (isFocusable)
										{
											spNextElement = container;
											break;
										}
									}

									FindNextTabStopCandidateFromItem(
										currentIndex,
										isBackward,
										(isGrouping && groupCount > 0 && m_groupsAreTabStops),
										out currentIndex,
										out isGroupHeader);

									if (isGroupHeader)
									{
										spNextElement = HeaderFromIndex(currentIndex);
										break;
									}
								}
							}
						}

						// If we failed to find a focus candidate, then advance the state.
						if (spNextElement is null)
						{
							pNewElementType = GetElementTypeTransition(ElementType.ItemOrGroupHeader, isBackward);
						}
					}
				}
				break;

			case ElementType.Other:
				pNewElementType = GetElementTypeTransition(ElementType.Other, isBackward);
				break;
		}

		ppNextElement = spNextElement;
	}

	private void FindNextTabStopCandidateFromItem(
		int referenceItemIndex,
		bool isBackwards,
		bool supportGroupHeaders,
		out int focusCandidateIndex,
		out bool isCandidateAGroupHeader)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: FindNextTabStopCandidateFromItem. referenceItemIndex={referenceItemIndex}, isBackward={isBackwards}, supportGroupHeaders={supportGroupHeaders}");
#endif

		uint itemCount = 0;
		itemCount = (uint)GetItemCount();

		// Our reference index is expected to be valid.
		global::System.Diagnostics.Debug.Assert(referenceItemIndex >= 0 && referenceItemIndex < (int)itemCount);

		// Advance item index.
		int newFocusIndex = (isBackwards) ? referenceItemIndex - 1 : referenceItemIndex + 1;
		bool isGroupHeader = false;

		if (supportGroupHeaders)
		{
			int groupCount = 0;
			groupCount = GetGroupCount();

			int referenceItemGroupIndex = 0;
			GetGroupFromItem(
				(uint)referenceItemIndex,
				out _,
				out _,
				out referenceItemGroupIndex,
				out _);

			if (newFocusIndex < 0)
			{
				// If navigating past the first item, focus this item's group header instead.
				newFocusIndex = referenceItemGroupIndex;
				isGroupHeader = true;
			}
			else if (newFocusIndex >= (int)itemCount)
			{
				// If navigating past the last item, see if there are empty group headers at the end that
				// we can focus.
				bool hasEmptyGroupAtEnd = false;
				hasEmptyGroupAtEnd = HasBoundaryEmptyGroup(true);
				if (hasEmptyGroupAtEnd && referenceItemGroupIndex < (groupCount - 1))
				{
					newFocusIndex = referenceItemGroupIndex + 1;
					isGroupHeader = true;
				}
			}
			else
			{
				// If we're navigating past a group boundary, focus the new group header instead.
				int newFocusedItemGroupIndex = 0;
				GetGroupFromItem(
					(uint)newFocusIndex,
					out _,
					out _,
					out newFocusedItemGroupIndex,
					out _);

				if (referenceItemGroupIndex != newFocusedItemGroupIndex)
				{
					// If we are crossing a group boundary, then just advance the group header when moving
					// forward by 1 to account for empty groups between the last focused item's group
					// and the suggested focus item's group.
					newFocusIndex = isBackwards ? referenceItemGroupIndex : referenceItemGroupIndex + 1;
					isGroupHeader = true;
				}
			}
		}

		focusCandidateIndex = newFocusIndex;
		isCandidateAGroupHeader = isGroupHeader;
	}

	// Helper for getting next focusable control outside of LVB.
	private DependencyObject? GetNextFocusablePeer(bool isBackward)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: GetNextFocusablePeer. isBackward={isBackward}");
#endif

		DependencyObject? nextFocusableElement;

		var focusManager = VisualTree.GetFocusManagerForElement(this);

		if (isBackward)
		{
			nextFocusableElement = focusManager?.GetPreviousTabStop(this);
		}
		else
		{
			nextFocusableElement = focusManager?.GetNextTabStop(this, true);
		}

		if (nextFocusableElement is null)
		{
#if !HAS_UNO // TODO Uno: ApplicationBarService has no GetTopAndBottomAppBars / GetFirstFocusableElementFromAppBars.
			ApplicationBarService? applicationBarService = null;

			if (XamlRoot is { } xamlRoot)
			{
				applicationBarService = xamlRoot.GetApplicationBarService();
			}

			// Process TabStop if the application bar service is available.
			if (applicationBarService is not null)
			{
				// We look for all AppBars (not just open ones) since they can be in minimal state and receive focus
				applicationBarService.GetTopAndBottomAppBars(out var spTopAppBar, out var spBottomAppBar);

				nextFocusableElement = applicationBarService.GetFirstFocusableElementFromAppBars(
						spTopAppBar,
						spBottomAppBar,
						isBackward ? AppBarTabPriority.Top : AppBarTabPriority.Bottom,
						isBackward ? true : false);
			}
#endif
		}

		return nextFocusableElement;
	}

	// If there is a pending scroll into view in the panel, get the target element
	// of that scroll into view
	private DependencyObject? GetFocusCandidateFromPanel()
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: GetFocusCandidateFromPanel.");
#endif

		int itemIndex = -1;
		int groupIndex = -1;
		UIElement? spFocusElement;
		DependencyObject? ppFocusCandidate = null;

		var spItemsPanel = ItemsPanelRoot;

		global::System.Diagnostics.Debug.Assert(spItemsPanel is ModernCollectionBasePanel);

		((ModernCollectionBasePanel)spItemsPanel).GetFocusCandidate(out itemIndex, out groupIndex, out spFocusElement);

		if (itemIndex != -1 || groupIndex != -1)
		{
			bool isGrouping = false;

			// panel provided a valid container to focus. use that.
			ppFocusCandidate = spFocusElement;

			isGrouping = IsGrouping;
			if (isGrouping)
			{
				if (itemIndex != -1)
				{
					GetGroupFromItem(
						(uint)itemIndex,
						out _,
						out _ /* GroupItem */,
						out groupIndex,
						out _ /* IndexInGroup */);
					SetLastFocusedGroupIndex(groupIndex);
					SetLastFocusedIndex((uint)itemIndex);
				}
				else
				{
					global::System.Diagnostics.Debug.Assert(groupIndex != -1);
					SetLastFocusedGroupIndex(groupIndex);
				}
			}
			else
			{
				global::System.Diagnostics.Debug.Assert(groupIndex == -1);
				SetLastFocusedIndex((uint)itemIndex);
			}
		}

		return ppFocusCandidate;
	}

	// Determines the next tab stop given currently focused element and default focus candidate.
	private void ProcessTabStopInternal(
		DependencyObject? pCurrentFocus,
		DependencyObject? pFocusCandidate,
		bool isBackward,
		bool didCycleFocusAtRootVisualScope,
		out bool pHandled,
		out DependencyObject? ppTabStop)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: ProcessTabStopInternal. isBackward={isBackward}");
#endif

		bool isUsingModernPanel = false;
		bool isUsingOrientedVirtualizingPanel = false;
		bool isUsingFocusableHeader = false;
		bool isUsingFocusableFooter = false;
		bool isUsingLocalOrCycleTabNavigation = false;
		bool isTabIntoOrientedVirtualizingPanel = false;
		DependencyObject? spCurrentParent;
		DependencyObject? spCandidateParent;
		ElementType currentParentType = ElementType.Other;
		ElementType candidateParentType = ElementType.Other;

		ppTabStop = null;
		pHandled = false;

		try
		{
			bool shouldCustomizeTabNavigation = false;

			shouldCustomizeTabNavigation = ShouldCustomizeTabNavigation();

			isUsingModernPanel = shouldCustomizeTabNavigation;

			if (!isUsingModernPanel)
			{
#if !HAS_UNO // TODO Uno: IOrientedVirtualizingPanel (the VirtualizingStackPanel family) does not exist.
				var itemsPanel = ItemsPanelRoot;

				isUsingOrientedVirtualizingPanel = itemsPanel is IOrientedVirtualizingPanel;
#endif

				if (!isUsingOrientedVirtualizingPanel && !isBackward)
				{
					var focusableFooterContainer = GetFocusableHeaderOrFooterContainer(false /*isForHeader*/);

					isUsingFocusableFooter = focusableFooterContainer is not null;
				}
				else if (isBackward)
				{
					var focusableFooterContainer = GetFocusableHeaderOrFooterContainer(true /*isForHeader*/);

					isUsingFocusableHeader = focusableFooterContainer is not null;
				}

				// isTabIntoOrientedVirtualizingPanel is set to True when tabbing into a VirtualizingStackPanel to ensure its potential
				// focusable Header is brought into view because OrientedVirtualizingPanel::MakeVisible is not supported.
				isTabIntoOrientedVirtualizingPanel = isUsingOrientedVirtualizingPanel && !isBackward && pCurrentFocus is null && pFocusCandidate is null;

				KeyboardNavigationMode navigationMode = KeyboardNavigationMode.Once;

				navigationMode = TabNavigation;

				isUsingLocalOrCycleTabNavigation = navigationMode != KeyboardNavigationMode.Once;
			}

			IdentifyParentElement(
				pCurrentFocus,
				out currentParentType,
				out spCurrentParent);

			IdentifyParentElement(
				pFocusCandidate,
				out candidateParentType,
				out spCandidateParent);

			if (isUsingModernPanel ||                                                                                                                                 // #1
				isTabIntoOrientedVirtualizingPanel ||                                                                                                                 // #2
				isUsingLocalOrCycleTabNavigation ||                                                                                                                   // #3
				(currentParentType == ElementType.Header && candidateParentType == ElementType.Other && !isBackward) ||                                               // #4
				(isUsingFocusableHeader && currentParentType == ElementType.ItemOrGroupHeader && candidateParentType == ElementType.Other) ||                         // #5
				(isUsingFocusableFooter && pCurrentFocus is not null && pFocusCandidate is not null && currentParentType == ElementType.ItemOrGroupHeader && candidateParentType == ElementType.Other) ||             // #6
				(!isUsingOrientedVirtualizingPanel && pCurrentFocus is not null && pFocusCandidate is not null && currentParentType == ElementType.Footer && candidateParentType == ElementType.Other && isBackward)) // #7
			{
				// This custom handling is not only needed for modern panels in general (#1), but also with a VirtualizingStackPanel or StackPanel:
				// - when tabbing into a VirtualizingStackPanel because OrientedVirtualizingPanel::MakeVisible is not supported and the focused Header would not be brought into view. (#2)
				// - when navigationMode is 'Cycle' and tabbing to loop from the last item to the first, or shift-tabbing from the first to the last. (#3)
				// - when navigationMode is 'Local' and tabbing or shift-tabbing out of the control, while the last or first item is focused respectively. (#3)
				// - when navigationMode is 'Once' and tabbing from the Header to a focusable item, otherwise the items are not reachable with the keyboard. (#4)
				// - when navigationMode is 'Once' and shift-tabbing from a focusable item to the Header, otherwise the Header is not reachable with the keyboard. (#5)
				// - when navigationMode is 'Once' and tabbing from a focusable item to the Footer in the StackPanel, otherwise the Footer is not reachable with the keyboard. (#6)
				// - when navigationMode is 'Once' and shift-tabbing from the Footer to a focusable item in the StackPanel, otherwise the items are not reachable with the keyboard. (#7)
				bool doProcessTabStopOverride = true;

				if (spCurrentParent is not null || spCandidateParent is not null)
				{
					// In certain scenarios with TabNavigation == Once, we can get pCurrentFocus == pFocusCandidate.
					// For these cases continue to process the override.
					if (!ReferenceEquals(pCurrentFocus, pFocusCandidate))
					{
						// For cases where the current focused element and the candidate element both have the same
						// parent (sibling focusable elements within the header/footer, or within a group header, or
						// within an item), We don't generally want to continue processing the tab stop override because
						// we'd interfere with normal tabbing behavior within those sub-trees, such as just tabbing
						// through the elements or tab cycling within the sub-tree.
						// However, if focus has wrapped around at the root level, such as when the ListView is set to
						// TabNavigation == Once and it is the root focusable control in the tree, then continue processing
						// the tab stop override; we don't want to mistakenly identify this situation as normal cycling
						// within current focused element's parent's sub-tree (see bug #10607771).
						if (ReferenceEquals(spCurrentParent, spCandidateParent) && !didCycleFocusAtRootVisualScope)
						{
							doProcessTabStopOverride = false;
						}
					}
				}

				if (doProcessTabStopOverride)
				{
					DependencyObject? spNextTabStop = null;

					UpdateTabStopFlags();

					if (isUsingModernPanel && !m_wasItemOrGroupHeaderFocused)
					{
						// modern panel and nothing is focused, check if the modern panel has an item
						// it wants to focus to. This can happen if there is a pending scroll into view.
						spNextTabStop = GetFocusCandidateFromPanel();
					}

					if (spNextTabStop is null)
					{
						spNextTabStop = GetNextTabStop(
							currentParentType,
							isBackward);
					}

					if (spNextTabStop is null)
					{
						// No tab stop suggestions, then go outside of control to next/previous.
						spNextTabStop = GetNextFocusablePeer(
							isBackward);
					}

					// Only provide tab stop suggestion if there is one and it's inside ListViewBase.
					// This is to fix cases like TFS #1535996 whereby the focus manager will find the roll-over focus candidate but this control
					// would override the tab stop to NULL thus causing the roll over to not occur.
					if (spNextTabStop is not null ||
						(candidateParentType != ElementType.Other && spCandidateParent is not null))
					{
						ppTabStop = spNextTabStop;
						pHandled = true;
						m_wasItemOrGroupHeaderFocused = true;
					}
				}
			}
		}
		finally
		{
			m_firstAvailableItemForFocus = -1;
			m_firstAvailableGroupHeaderForFocus = -1;
		}
	}

	// Get the next state from current one in the specified direction.
	// The order for isBackward = FALSE is: Other -> Header-> Item -> GroupHeader -> Footer -> Other
	private static ElementType GetElementTypeTransition(
		ElementType current,
		bool isBackward)
	{
		ElementType next = ElementType.Other;

		switch (current)
		{
			case ElementType.Other:
				next = (isBackward) ? ElementType.Footer : ElementType.Header;
				break;

			case ElementType.Header:
				next = (isBackward) ? ElementType.Other : ElementType.ItemOrGroupHeader;
				break;

			case ElementType.Item:
				next = (isBackward) ? ElementType.Header : ElementType.GroupHeader;
				break;

			case ElementType.GroupHeader:
				next = (isBackward) ? ElementType.Item : ElementType.Footer;
				break;

			case ElementType.ItemOrGroupHeader:
				next = (isBackward) ? ElementType.Header : ElementType.Footer;
				break;

			case ElementType.Footer:
				next = (isBackward) ? ElementType.ItemOrGroupHeader : ElementType.Other;
				break;
		}

		return next;
	}

	// For certain situations (such as drag/drop), the host may hang onto a container for an extended
	// period of time. That particular container shouldn't ever be recycled as long as it's being used.
	// This method asks whether or not the given container is eligible for recycling.
	private protected override bool CanRecycleContainer(DependencyObject pContainer)
	{
		bool isDraggedContainer = false;

		// We shouldn't ever recycle the dragged container.
		isDraggedContainer = ReferenceEquals(m_tpPrimaryDraggedContainer, pContainer);
		return !isDraggedContainer;
	}

	private protected override DependencyObject? SuggestContainerForContainerFromItemLookup()
		// yay, i might know better
		=> m_spContainerBeingClicked;

	// Sets up a link from group header to parent LVB used to relay calls.  Used only for modern panels.
	private protected override void PrepareGroupContainer(
		DependencyObject pContainer,
		ICollectionViewGroup? pGroup)
	{
		base.PrepareGroupContainer(pContainer, pGroup);

		if (IsListViewBaseHeaderItem(pContainer))
		{
			((ListViewBaseHeaderItem)pContainer).SetParent(this);
		}
	}

	// Clears a link from group header to parent LVB used to relay calls.  Used only for modern panels.
	private protected override void ClearGroupContainerForGroup(
		DependencyObject pContainer,
		ICollectionViewGroup? pGroup)
	{
		base.ClearGroupContainerForGroup(pContainer, pGroup);

		if (IsListViewBaseHeaderItem(pContainer))
		{
			((ListViewBaseHeaderItem)pContainer).SetParent(null);
		}
	}

	// Called when the ListViewBase has been constructed and added to the visual
	// tree.
	private void OnLoaded(
		object pSender,
		RoutedEventArgs pArgs)
	{
		RegisterIfWorkPending();

		// TODO Uno: the scroll viewer hooks are released on unload, see OnUnloadedReleaseResources.
		AttachScrollViewerPropertyChanged();

		if (m_deferredPoint == DeferPoint.ListViewBaseLoaded)
		{
			ExecuteDeferredScrollCommand();
		}
	}

	// Schedules a deferred scroll command.
	private void ScheduleDeferredScrollCommand(
		Action<object?> command,
		object? pScrollToItem,
		DeferPoint deferUntil)
	{
		// It only makes sense to schedule the most recent
		// scroll command because that's what decide where we
		// are going to end up in the list anyways.
		// We make sure, however, that we clear all the tracker
		// pointers related to deferred scrolling once we execute
		// the command.

		m_tpDeferredScrollToItem = pScrollToItem;
		m_deferredScrollCommand = command;

		m_deferredPoint = deferUntil;
	}

	// Executes a deferred scroll command (if any) and clears it.
	private void ExecuteDeferredScrollCommand()
	{
		try
		{
			if (m_deferredScrollCommand is not null)
			{
				m_deferredScrollCommand(m_tpDeferredScrollToItem);
				m_isDeferredElementFooter = false;
			}
		}
		finally
		{
			// Make sure we clear any 'global' state related
			// to deferred scrolling.
			m_tpDeferredScrollToItem = null;
			m_deferredScrollCommand = null;
		}
	}

	private protected override void OnItemsHostAvailable()
	{
		base.OnItemsHostAvailable();

		// ItemsControl_ItemsHost case of OnPropertyChanged2: ItemsHost has no DependencyProperty in Uno.
		{
			// When any property changes that affects our ItemsHost
			// we hookup to changes.
			EnsureScrollHostOffsetChangeAction();

#if !HAS_UNO // TODO Uno: telemetry only.
			ItemsControl.TraceVirtualizationEnabledByModernPanel();
#endif

			// Hookup to the ItemsRangeInfo if it exists
			InitializeDataSourceItemsRangeInfo();

			// Hookup to the SelectionInfo if it exists
			InitializeDataSourceSelectionInfo();
		}

		// if we have a pending deferred command and the command wants to
		// scroll to the footer, don't delay load the footer
		if (m_isDeferredElementFooter)
		{
			var spItemsPresenter = ItemsPresenter;

			if (spItemsPresenter is not null)
			{
				spItemsPresenter.LoadFooter(false /* updateLayout */);
			}
		}

		if (m_deferredPoint == DeferPoint.ItemsHostAvailable)
		{
			ExecuteDeferredScrollCommand();
		}
	}

	internal override bool IsDraggableOrPannable()
	{
		bool dragEnabled = GetIsDragEnabled();

		return dragEnabled;
	}

	private void OnGettingFocus(UIElement sender, GettingFocusEventArgs args)
	{
#if LVB_DEBUG
		this.Log().Debug($"LVB({(this is GridView ? "GV" : "LV")})[{GetHashCode():X}]: OnGettingFocus.");
#endif

		// Don't take any action if focus is being set programmatically.  We only care to take
		// action when it's set in response to some interaction like tab, arrow keys, or
		// gamepad navigation.
		var focusNavigationDirection = args.Direction;
		if (focusNavigationDirection == FocusNavigationDirection.None)
		{
			return;
		}

		if (focusNavigationDirection == FocusNavigationDirection.Next ||
			focusNavigationDirection == FocusNavigationDirection.Previous)
		{
			var navigationMode = KeyboardNavigationMode.Once;

			navigationMode = TabNavigation;

			if (navigationMode != KeyboardNavigationMode.Once)
			{
				// When tabbing or shift-tabbing into the ListViewBase while TabNavigation is 'Local'
				// or 'Cycle', do not give focus to the latest focused element.
				// The first or last focusable element is getting focus instead (either the header,
				// a group header, an item, or the footer).
				return;
			}
		}

		var selectionMode = SelectionMode;

		int focusIndex = -1;

		// If SingleSelectionFollowsFocus is true and we have a selected item, then re-direct
		// focus to the selected item if focus is coming from the outside, or is coming from the
		// header or footer, and is targeting an item or group header.
		bool singleSelectionFollowsFocus = SingleSelectionFollowsFocus;
		if (singleSelectionFollowsFocus && selectionMode == ListViewSelectionMode.Single)
		{
			int selectedIndex = -1;

			selectedIndex = SelectedIndex;

			focusIndex = selectedIndex;
		}
		else if (focusNavigationDirection == FocusNavigationDirection.Next ||
				 focusNavigationDirection == FocusNavigationDirection.Previous)
		{
			focusIndex = (int)GetLastFocusedIndex();
		}

		if (focusIndex != -1)
		{
			DependencyObject? elementParent;

			var newFocusedElement = args.NewFocusedElement;

			var oldFocusedElement = args.OldFocusedElement;

			ElementType newFocusedParentElementType = ElementType.Other;
			IdentifyParentElement(newFocusedElement, out newFocusedParentElementType, out elementParent);
			bool isFocusMovingOntoItemOrGroupHeader = (newFocusedParentElementType == ElementType.Item || newFocusedParentElementType == ElementType.GroupHeader || newFocusedParentElementType == ElementType.ItemOrGroupHeader);

			bool isAncestor = false;
			isAncestor = oldFocusedElement is not null && this.IsAncestorOf(oldFocusedElement);
			bool isFocusComingFromOutside = !isAncestor;

			// If the old element is a child of this ListViewBase element, then see if it's a child
			// of either the header or footer objects.
			bool isFocusComingFromHeaderOrFooter = false;
			if (isAncestor)
			{
				ElementType oldFocusedParentElementType = ElementType.Other;
				IdentifyParentElement(oldFocusedElement, out oldFocusedParentElementType, out elementParent);
				isFocusComingFromHeaderOrFooter = (oldFocusedParentElementType == ElementType.Header || oldFocusedParentElementType == ElementType.Footer);
			}

			if ((isFocusComingFromOutside || isFocusComingFromHeaderOrFooter) && isFocusMovingOntoItemOrGroupHeader)
			{
				bool isFocusable = false;

				var itemMapping = (ItemsPanelRoot as ICustomGeneratorItemsHost)?.GetItemContainerMapping();
				isFocusable = IsItemFocusable(itemMapping!, (uint)focusIndex, true /*unrealizedItemsAssumedFocusable*/);

				if (isFocusable)
				{
					// Make sure the focused index is clear so that when we scroll to the focused item
					// and end up realizing items we won't try to set focus to any of them because setting
					// focus on an element within a GettingFocus handler is not allowed and would return an
					// error.
					// This will get set back to the focused item index when it receives focus, as a result
					// of this GettingFocus handler, in SelectorItem::ItemFocused.
					SetFocusedIndex(-1);

					ScrollIntoView(
						(uint)focusIndex,
						false /*isGroupItemIndex*/,
						false /*isHeader*/,
						false /*isFooter*/,
						false /*isFromPublicAPI*/,
						true  /*ensureContainerRealized*/,
						false /*animateIfBringIntoView*/,
						ScrollIntoViewAlignment.Default);

					var focusIndexContainer = ContainerFromIndex(focusIndex);
					isAncestor = newFocusedElement is not null && focusIndexContainer.IsAncestorOf(newFocusedElement);

					// When isAncestor is True, focus is about to move to a child of focusIndexContainer. No need to alter this imminent focus change.
					// In particular, setting focus to the container instead of its child would break the tab order when Shift-Tabbing into the ListViewBase.
					if (!isAncestor)
					{
						bool focusMoved = false;

						focusMoved = args.TrySetNewFocusedElement(focusIndexContainer);
						args.Handled = focusMoved;
					}
				}
			}
		}
	}

	[global::Uno.NotImplemented]
	public ConnectedAnimation PrepareConnectedAnimation(string key, object item, string elementName)
	{
#if !HAS_UNO // TODO Uno: ConnectedAnimationService is out of scope.
		UIElement? animationElement = null;
		{
			var containerElement = ContainerFromItem(item) as ContentControl;

			if (containerElement is not null)
			{
				var contentElement = containerElement.ContentTemplateRoot;

				if (contentElement is not null)
				{
					var contentRoot = (FrameworkElement)contentElement;

					var animationElementInspectable = contentRoot.FindName(elementName);

					if (animationElementInspectable is not null)
					{
						animationElement = (UIElement)animationElementInspectable;
					}
				}
			}
		}

		if (animationElement is not null)
		{
			var service = ConnectedAnimationService.GetForCurrentView();

			return service.PrepareToAnimate(key, animationElement);
		}
		// We were unable to find the element.
		throw new ArgumentException();
#else
		throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException("Microsoft.UI.Xaml.Controls.ListViewBase", "PrepareConnectedAnimation");
#endif
	}

	[global::Uno.NotImplemented]
	public IAsyncOperation<bool> TryStartConnectedAnimationAsync(ConnectedAnimation animation, object item, string elementName)
	{
#if !HAS_UNO // TODO Uno: TryStartConnectedAnimationOperation and ConnectedAnimationService are out of scope.
		TryStartConnectedAnimationOperation tryStartOperation = new();
		tryStartOperation.InitAndStart(this, animation, item, elementName);
		return tryStartOperation;
#else
		throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException("Microsoft.UI.Xaml.Controls.ListViewBase", "TryStartConnectedAnimationAsync");
#endif
	}

	internal bool IsScrollable()
	{
		bool scrollable = false;
		if (m_tpScrollViewer is not null)
		{
			bool horizontallyScrollable = false;
			bool verticallyScrollable = false;
			m_tpScrollViewer.IsContentScrollable(
				false, /* ignoreScrollMode */
				false, /* ignoreScrollBarVisibility */
				out horizontallyScrollable,
				out verticallyScrollable);

			scrollable = horizontallyScrollable || verticallyScrollable;
		}

		return scrollable;
	}

	protected override AutomationPeer OnCreateAutomationPeer() => new ListViewBaseAutomationPeer(this);
}

