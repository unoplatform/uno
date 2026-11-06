// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

// MUX Reference ListViewBase_Partial.h, ListViewBase.g.h, tag winui3/release/2.5.1, commit ba3a8d59e6d

//  Abstract:
//      ListViewBase displays a rich, interactive collection of items.


#nullable enable

using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Uno.Disposables;
using Uno.UI.Helpers.Boxes;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using MovedItems = DirectUI.Components.LiveReorderHelper.MovedItems;

namespace Microsoft.UI.Xaml.Controls;

// ListViewBase displays a a rich, interactive collection of items.
public partial class ListViewBase : Selector, ITreeBuilder, IBackButtonListener, IDirectManipulationStateChangeHandler
{
	internal const string ListViewBaseName = "Microsoft.UI.Xaml.ListViewBase.ListViewBaseName";

	// Enum of the actions we can take when our scroll host's offset changes.
	private enum ScrollHostOffsetChangeAction
	{
		// No action.
		ScrollHostOffsetChangeAction_None = 0,
		// Automatic triggering of incremental loading.
		ScrollHostOffsetChangeAction_IncrementalEdgeTrigger = 1,
	}

	// Enum used for identifying tab-stoppable elements for navigation in LVB with modern panel.
	// Other is used for a control outside of LVB. It's also used for serialization.
	private enum ElementType
	{
		Other,
		Header,
		Footer,
		Item,
		GroupHeader,

		// This type is used when we want to consider Items & Group Headers together
		// when determining tab stop candidates.
		ItemOrGroupHeader,
	}

	// Represents a pan velocity. Used for edge scrolling.
	private struct PanVelocity
	{
		internal float HorizontalVelocity;
		internal float VerticalVelocity;

		internal static PanVelocity Stationary() => default;

		internal readonly bool IsStationary() => (HorizontalVelocity == 0) && (VerticalVelocity == 0);

		internal void Clear() => HorizontalVelocity = VerticalVelocity = 0;
	}

	// Represents a direction for keyboard reordering.
	internal enum KeyboardReorderDirection
	{
		KeyboardReorderDirection_ToLowerIndex,
		KeyboardReorderDirection_ToHigherIndex
	}

	// Represents the index used as an anchor during extended selection
	// mode.  The anchor is used specifically so range selection will
	// respect the user's original selected item.  For example, click an
	// item in the middle of a directory in file explorer, press
	// Shift+Down several times and note how the selection grows down,
	// but then press Shift+Up even more times and you'll see selection
	// actually flip about the anchor item and start growing upwards.
	private uint? m_pAnchorIndex;

	// The input pointer used for the current drag and drop operation,
	// if any.  During a drag, will be used to ignore input from devices
	// not involved in the drag (i.e., mouse input will be ignored when
	// touch-dragging).
	private Pointer? m_tpDragPointer;

	// If there is a drag and drop in progress, this is the number of
	// items being dragged. Outside of a drag, the value is undefined.
	private uint m_dragItemsCount;

	// A reference to the item the user is physically dragging (as
	// opposed to items in the selection, but not being directly
	// dragged).  If there is no drag and drop in progress, this field
	// is set to NULL.
	private SelectorItem? m_tpPrimaryDraggedContainer;

	// The DragDropVisual used by the current drag/drop operation.
	// This DragDropVisual will be drawing m_tpPrimaryDraggedContainer's
	// drag content (see ListViewBaseItem::GetDragVisual).
	private object? m_tpDragDropVisual;

	// Set to TRUE while we're in ReorderItemsTo. This prevents us
	// from clearing drag/drop-related fields in our ItemsCollection
	// change handler.
	private bool m_isInItemReorderAfterDrop;

	// TRUE if there is a drag and drop started by this ListViewBase and
	// the user is dragging over this ListViewBase. NOT VALID outside of
	// a drag and drop (could be TRUE or FALSE).
	private bool m_isDraggingOverSelf;

	// TRUE if any item in the ListView is in press and hold state
	private bool m_isHolding;

	// The event handler that we have attached to determine when the ScrollViewer's
	// offsets have changed. This is only set when incremental loading or prefetching
	// are enabled.
	private IDisposable? m_tpOffsetChangedHandler;

	// The item that should be in a DragOver state
	private SelectorItem? m_tpDragOverItem;

	// The current item being interacted with through pointer events. We need to know this so we
	// can forward ManipulationStarted notifications to the item, so it can update its state.
	private SelectorItem? m_tpHoldingItem;

	// Set before raising ItemClick event, cleared afterwards
	private DependencyObject? m_spContainerBeingClicked;

	// The last location of the top-left corner of the drag visual. Only valid during a drag and drop
	// initiated via this ListViewBase. Relative to root visual.
	private Point m_lastDragPosition;

	// The last point the user performed a reorder drop will be stored here.
	// Relative to root visual.
	private Point m_lastReorderPosition;

	// The action we will take when the scroll host's offset changes.
	private ScrollHostOffsetChangeAction m_scrollHostOffsetChangeAction;

#pragma warning disable CS0414 // Only used by the SemanticZoom paths, which are inert in Uno.
	private FocusState m_semanticZoomCompletedFocusState;
#pragma warning restore CS0414

	// Edge scrolling begins after a delay. This is the velocity we will take after
	// the delay expires.
	private PanVelocity m_pendingAutoPanVelocity;

	// Our current automatic scrolling speed. See ScrollWithVelocity.
	private PanVelocity m_currentAutoPanVelocity;

	// Timer that handles the delay between hitting an edge and edge scrolling
	// kicking in.
	private DispatcherTimer? m_tpStartEdgeScrollTimer;

	// True when items are getting fetched from AcynLoadOperation for Increment Loading
	private bool m_isLoadAsyncInProgress;

	private double m_maxContentWidth;
	private double m_maxContentHeight;

	// UIAutomation Property: Text that describes what would happen if the currently dragged item were dropped over this target.
	private string? m_strDropTargetDropEffect;

	// UIAutomation Property: Array of grabbed items
	private IList<IRawElementProviderSimple>? m_tpDragGrabbedItems;

	// Last point we got from a DragOver event. Only valid during a drag/drop.
	private Point m_lastDragOverPoint;

	// Currently focused group index.
	// Can be negative. (-1) means nothing focused.
	private int m_focusedGroupIndex;

	// The index of the last focused group.
	private int m_lastFocusedGroupIndex;

	// The index of the first available item for focus (only valid during a call to ProcessTabStopInternal).
	private int m_firstAvailableItemForFocus;

	// The index of the first available group header for focus (only valid during a call to ProcessTabStopInternal).
	private int m_firstAvailableGroupHeaderForFocus;

	// The item index to use as a reference when keyboarding navigating
	// off of a group header.
	private int m_itemIndexHintForHeaderNavigation;

	private ElementType m_lastFocusedElementType;

	// Tells our KeyDown handler whether the key down args came from an item.
	private bool m_bKeyDownArgsFromItem;

	// On the phone
	// If the SemanticZoom switch has been triggered by a group tap, we want to keep it to
	// Position the jumpList correctly
#pragma warning disable CS0169 // Only used by the SemanticZoom paths, which are inert in Uno.
	private object? m_tpSZRequestingItem;
#pragma warning restore CS0169

	//
	// Deferred scrolling.
	//

	// If asked to scroll too soon, we delay the scroll until the template is applied.
	// This can happen with semantic zoom and list UI deserialization.
	private object? m_tpDeferredScrollToItem;
#pragma warning disable CS0169 // Only used in #if !HAS_UNO code.
	private ScrollIntoViewAlignment m_deferredAlignment;
#pragma warning restore CS0169

	private Action<object?>? m_deferredScrollCommand;

	private enum DeferPoint
	{
		// Defer until OnItemsHostAvailable call
		ItemsHostAvailable,
		// Defer until OnLoaded
		ListViewBaseLoaded
	}

	// The point to defer until - ItemsHostAvailable or ListViewBaseLoaded
	private DeferPoint m_deferredPoint;

	// The deferred element is a footer. We need to keep track of this to
	// load the footer. The footer is delay loaded by default.
	private bool m_isDeferredElementFooter;

	// Delay from PointerPressed to conversion form candidate to reorderable.
	private const uint s_reorderConfirmationDelayInMsec = 150;

	// If pointer travels distance smaller than this, it is considered 'noise' and will not reset
	// the conversion timer.
	private const float s_reorderConfirmationThresholdInDips = 3.0f;

	internal string m_itemSelectionGestureId = string.Empty;

	// LiveReorder
	private struct LiveReorderIndices
	{
		internal int draggedItemIndex;
		internal int draggedOverIndex;
		internal int itemsCount;

		internal LiveReorderIndices(int draggedItemIndex, int draggedOverIndex, int itemsCount)
		{
			this.draggedItemIndex = draggedItemIndex;
			this.draggedOverIndex = draggedOverIndex;
			this.itemsCount = itemsCount;
		}
	}

	private LiveReorderIndices m_liveReorderIndices;
	private readonly MovedItems m_movedItems = new();
	private DispatcherTimer? m_tpLiveReorderTimer;
	private readonly SerialDisposable m_epLiveReorderTimerEvent = new();
	private DataPackageOperation m_dragAcceptedOperation;

	// Event registrations on the ScrollViewer.
	private readonly SerialDisposable m_epScrollViewerViewChangingHandler = new();

	#region ListViewBase_Partial.cpp members

	// GetFocusedGroupIndex and SetFocusedGroupIndex are consistently used instead of
	// m_focusedGroupIndex to make it easier to track when this field is read & written.
	private protected int GetFocusedGroupIndex() => m_focusedGroupIndex;

	private protected void SetFocusedGroupIndex(int focusedGroupIndex)
	{
		if (m_focusedGroupIndex != focusedGroupIndex)
		{
			m_focusedGroupIndex = focusedGroupIndex;
		}
	}

	// GetLastFocusedGroupIndex and SetLastFocusedGroupIndex are consistently used instead of
	// m_lastFocusedGroupIndex to make it easier to track when this field is read & written.
	private protected int GetLastFocusedGroupIndex() => m_lastFocusedGroupIndex;

	private protected void SetLastFocusedGroupIndex(int lastFocusedGroupIndex)
	{
		if (m_lastFocusedGroupIndex != lastFocusedGroupIndex)
		{
			m_lastFocusedGroupIndex = lastFocusedGroupIndex;
		}
	}

	// we keep a list of containers we have called clear on. Each time we call prepare, we remove it from this list
	// at the end of measure, we know to only execute code on containers that are left in this list.
	// if the below looks like a hack: it is. We need to keep these two vectors in sync.
	private readonly TrackerCollection<UIElement> m_toBeClearedContainers = new();
	private readonly TrackerCollection<object?> m_toBeClearedItems = new();

	// Flags indicating whether items/groups should be tab stops.
	private bool m_itemsAreTabStops;
	private bool m_groupsAreTabStops;

	// flag indicating that an item or group header was focused before. If this is false
	// that means that items/group headers never got focus so far.
	private bool m_wasItemOrGroupHeaderFocused;

	private bool m_focusableElementForModernPanelReentrancyGuard;

#pragma warning disable CS0414 // Only used in #if !HAS_UNO code.
	private bool m_allowDrop;
#pragma warning restore CS0414

	// This flag is set to true while in ListViewBase::OnSelectionModeChanged.
	// Selection might change when changing selection mode. In this case, we don't
	// want to bring the new selected item into view.
	// A typical case for this is when the ItemsSource is a CollectionViewSource and we
	// are switching from Multiple to Single selection mode. In this case, UpdateCVSynchronizationState
	// will get called and we will select the collection view's current item, but we should not bring it
	// into view.
	// Additional note: this is really only an issue if this control has focus when the selection mode changes.
	// Which is the case on phone when the user is using the (phone) AppBar since the latter don't take focus.
	private bool m_isInOnSelectionModeChanged;

	private readonly SerialDisposable m_gettingFocusHandler = new();

	private static bool IsListViewBaseItem(object? pItem) => pItem is ListViewItem or GridViewItem;

	private static bool IsListViewBaseHeaderItem(object? pItem) => pItem is ListViewHeaderItem or GridViewHeaderItem;

	#endregion

	#region ListViewBase_Partial_Selection.cpp members

	// Set the extended anchor index
	private void SetAnchorIndex(uint index) => m_pAnchorIndex = index;

	// Release the extended selection anchor index.
	private void ReleaseAnchorIndex() => m_pAnchorIndex = null;

	#endregion

	#region ListViewBase_Partial_Interaction.cpp members

	// Returns TRUE if there is a currently focused GroupItem.
	internal bool HasFocusedGroup() => GetFocusedGroupIndex() >= 0;

	#endregion

	#region ListViewBase_Partial_DataVirtualization.cpp members

	private IItemsRangeInfo? m_tpDataSourceAsItemsRangeInfo;
	private ItemIndexRange? m_tpLastPassedVisibleRange;
	private TrackerCollection<ItemIndexRange>? m_tpLastPassedTrackedRanges;

	#endregion

	#region ListViewBase_Partial_Reorder.cpp members

	// Gets the value of m_isHolding
	internal bool GetIsHolding() => m_isHolding;

	// Sets the value of m_isHolding
	internal void SetIsHolding(bool isHolding) => m_isHolding = isHolding;

	internal bool IsInLiveReorder() => m_liveReorderIndices.draggedItemIndex != -1;

	private int GetInsertionIndexForLiveReorder()
	{
		int draggedIndex = m_liveReorderIndices.draggedItemIndex;
		int insertIndex = m_liveReorderIndices.draggedOverIndex;

		if (draggedIndex < insertIndex)
		{
			++insertIndex;
		}

		// make sure we don't go out of range
		if (insertIndex > m_liveReorderIndices.itemsCount)
		{
			insertIndex = m_liveReorderIndices.itemsCount;
		}

		return insertIndex;
	}

	#endregion

	#region ListViewBase_Partial_ContainerPhase.cpp members

	// the lowest phase container we have in the queue. -1 means nothing is in the queue
	private long m_lowestPhaseInQueue;

	// budget that we have to do other work, measured since the last tick
	// once we get stl 11, we should convert to std::chrono
	private uint m_budget;

	// the last known count of containers in the incremental visualization queue. this value is used when logging
	// our telemetry event in the case where BuildTreeImpl cannot perform work on the busy uiThread.
	private uint m_lastIncrementalVisualizationContainerCount;

	// Locally cached value, so we can avoid more expensive property system lookups.
	private bool m_showsScrollingPlaceholdersLocal;

	// In some situations (typically when closing JumpList), we do not want scrolling PlaceHolders
	private bool m_disableScrollingPlaceholders;

	#endregion

	#region ListViewBase_Partial_Serialization.cpp members

	private enum ListSerializationFormatVersions
	{
		ListSerializationFormatVersions_PhoneBlue = 1
	}

	private struct ListSerializationData
	{
		internal ListSerializationFormatVersions version;
		internal ElementType elementType;
		internal double viewportOffset;
		internal string? serializationKey;
	}

	#endregion

	#region ListViewBase.g.h

	private bool m_isRegisteredForCallbacks;

	internal bool IsRegisteredForCallbacks { get => m_isRegisteredForCallbacks; set => m_isRegisteredForCallbacks = value; }

	public event TypedEventHandler<ListViewBase, ChoosingGroupHeaderContainerEventArgs>? ChoosingGroupHeaderContainer;
	public event TypedEventHandler<ListViewBase, ChoosingItemContainerEventArgs>? ChoosingItemContainer;
	public event TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs>? ContainerContentChanging;
	public event TypedEventHandler<ListViewBase, DragItemsCompletedEventArgs>? DragItemsCompleted;
	public event DragItemsStartingEventHandler? DragItemsStarting;
	public event ItemClickEventHandler? ItemClick;

	// Header, HeaderTemplate, Footer, FooterTemplate, SelectionMode, IsItemClickEnabled, DataFetchSize,
	// IncrementalLoadingThreshold and IncrementalLoadingTrigger come from DependencyPropertyMixinGenerator.

	public bool CanDragItems
	{
		get => (bool)GetValue(CanDragItemsProperty);
		set => SetValue(CanDragItemsProperty, value);
	}

	public static DependencyProperty CanDragItemsProperty { get; } =
		DependencyProperty.Register(nameof(CanDragItems), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.False));

	public bool CanReorderItems
	{
		get => (bool)GetValue(CanReorderItemsProperty);
		set => SetValue(CanReorderItemsProperty, value);
	}

	public static DependencyProperty CanReorderItemsProperty { get; } =
		DependencyProperty.Register(nameof(CanReorderItems), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.False));

	// TODO Uno: IsValueCreatedOnDemand: WinUI creates the collection on first read.
	public TransitionCollection? FooterTransitions
	{
		get => (TransitionCollection?)GetValue(FooterTransitionsProperty);
		set => SetValue(FooterTransitionsProperty, value);
	}

	public static DependencyProperty FooterTransitionsProperty { get; } =
		DependencyProperty.Register(nameof(FooterTransitions), typeof(TransitionCollection), typeof(ListViewBase), new FrameworkPropertyMetadata(default(TransitionCollection)));

	// TODO Uno: IsValueCreatedOnDemand: WinUI creates the collection on first read.
	public TransitionCollection? HeaderTransitions
	{
		get => (TransitionCollection?)GetValue(HeaderTransitionsProperty);
		set => SetValue(HeaderTransitionsProperty, value);
	}

	public static DependencyProperty HeaderTransitionsProperty { get; } =
		DependencyProperty.Register(nameof(HeaderTransitions), typeof(TransitionCollection), typeof(ListViewBase), new FrameworkPropertyMetadata(default(TransitionCollection)));

	public bool IsActiveView
	{
		get => (bool)GetValue(IsActiveViewProperty);
		set => SetValue(IsActiveViewProperty, value);
	}

	public static DependencyProperty IsActiveViewProperty { get; } =
		DependencyProperty.Register(nameof(IsActiveView), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.False));

	public bool IsMultiSelectCheckBoxEnabled
	{
		get => (bool)GetValue(IsMultiSelectCheckBoxEnabledProperty);
		set => SetValue(IsMultiSelectCheckBoxEnabledProperty, value);
	}

	public static DependencyProperty IsMultiSelectCheckBoxEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsMultiSelectCheckBoxEnabled), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.True));

	public bool IsSwipeEnabled
	{
		get => (bool)GetValue(IsSwipeEnabledProperty);
		set => SetValue(IsSwipeEnabledProperty, value);
	}

	public static DependencyProperty IsSwipeEnabledProperty { get; } =
		DependencyProperty.Register(nameof(IsSwipeEnabled), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.False));

	public bool IsZoomedInView
	{
		get => (bool)GetValue(IsZoomedInViewProperty);
		set => SetValue(IsZoomedInViewProperty, value);
	}

	public static DependencyProperty IsZoomedInViewProperty { get; } =
		DependencyProperty.Register(nameof(IsZoomedInView), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.False));

	public ListViewReorderMode ReorderMode
	{
		get => (ListViewReorderMode)GetValue(ReorderModeProperty);
		set => SetValue(ReorderModeProperty, value);
	}

	public static DependencyProperty ReorderModeProperty { get; } =
		DependencyProperty.Register(nameof(ReorderMode), typeof(ListViewReorderMode), typeof(ListViewBase), new FrameworkPropertyMetadata(default(ListViewReorderMode)));

	public SemanticZoom? SemanticZoomOwner
	{
		get => (SemanticZoom?)GetValue(SemanticZoomOwnerProperty);
		set => SetValue(SemanticZoomOwnerProperty, value);
	}

	public static DependencyProperty SemanticZoomOwnerProperty { get; } =
		DependencyProperty.Register(nameof(SemanticZoomOwner), typeof(SemanticZoom), typeof(ListViewBase), new FrameworkPropertyMetadata(default(SemanticZoom)));

	// The getter reads the local field, as ListViewBase::get_ShowsScrollingPlaceholders does.
	public bool ShowsScrollingPlaceholders
	{
		get => m_showsScrollingPlaceholdersLocal;
		set => SetValue(ShowsScrollingPlaceholdersProperty, value);
	}

	// TODO Uno: the default is not in the source drop. True matches the constructor's m_showsScrollingPlaceholdersLocal.
	public static DependencyProperty ShowsScrollingPlaceholdersProperty { get; } =
		DependencyProperty.Register(nameof(ShowsScrollingPlaceholders), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.True));

	public bool SingleSelectionFollowsFocus
	{
		get => (bool)GetValue(SingleSelectionFollowsFocusProperty);
		set => SetValue(SingleSelectionFollowsFocusProperty, value);
	}

	public static DependencyProperty SingleSelectionFollowsFocusProperty { get; } =
		DependencyProperty.Register(nameof(SingleSelectionFollowsFocus), typeof(bool), typeof(ListViewBase), new FrameworkPropertyMetadata(BoolBoxes.True));

	bool ITreeBuilder.IsBuildTreeSuspended => IsBuildTreeSuspended;
	bool ITreeBuilder.IsRegisteredForCallbacks { get => IsRegisteredForCallbacks; set => IsRegisteredForCallbacks = value; }
	bool ITreeBuilder.BuildTree() => BuildTree();
	void ITreeBuilder.ShutDownDeferredWork() => ShutDownDeferredWork();

	bool IBackButtonListener.OnBackButtonPressed() => OnBackButtonPressed();

	void IDirectManipulationStateChangeHandler.NotifyStateChange(
		DMManipulationState state,
		float xCumulativeTranslation,
		float yCumulativeTranslation,
		float zCumulativeFactor,
		float xCenter,
		float yCenter,
		bool isInertial,
		bool isTouchConfigurationActivated,
		bool isBringIntoViewportConfigurationActivated)
		=> NotifyStateChange(state, xCumulativeTranslation, yCumulativeTranslation, zCumulativeFactor, xCenter, yCenter, isInertial, isTouchConfigurationActivated, isBringIntoViewportConfigurationActivated);

	#endregion
}

