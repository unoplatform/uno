#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Uno.UI.Dispatching;
using Windows.Foundation;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyAccessibility : SkiaAccessibilityBase
{
	private static readonly Lazy<WebAssemblyAccessibility> _instance = new Lazy<WebAssemblyAccessibility>(() => new());

	internal static WebAssemblyAccessibility Instance => _instance.Value;

	public WebAssemblyAccessibility()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(WebAssemblyAccessibility)}");
		}

		// WebAssembly is a single-window runtime (one browser tab); the Skia-Desktop
		// AccessibilityRouter is not used here. Wire the framework's single-slot
		// accessibility registrations directly to this singleton.
		AccessibilityAnnouncer.AccessibilityImpl = this;
		UIElementAccessibilityHelper.ExternalOnChildAdded = (parent, child, index) => RouteChildAdded(parent, child, index);
		UIElementAccessibilityHelper.ExternalOnChildRemoved = (parent, child) => RouteChildRemoved(parent, child);
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = visual => RouteVisualOffsetOrSizeChanged(visual);
		AutomationPeer.AutomationPeerListener = this;
	}

	protected override void DisposeCore()
	{
		// WebAssembly runs in a single browser tab; disposal is not part of the
		// per-window lifecycle exercised by the Skia-Desktop router. No-op so the
		// base-class lifecycle contract holds.
	}

	private bool _isAccessibilityEnabled;
	private bool _isCreatingAOM;
	private IntPtr _rootElementHandle;
	public override bool IsAccessibilityEnabled => _isAccessibilityEnabled;

	// Subsystem managers (initialized during accessibility activation)
	private LiveRegionManager? _liveRegionManager;
	private FocusSynchronizer? _focusSynchronizer;
	private UIElement? _focusSearchRoot;
	private bool _suppressDeparture;
	internal ModalFocusScope? ActiveModalScope { get; set; }
	private readonly Dictionary<IntPtr, VirtualizedRegionRegistration> _virtualizedRegions = new();
	private const int PreserveTextSelectionSentinel = -1;

	/// <summary>
	/// True if the handle is a currently-realized item inside a virtualized container — it has a
	/// uno-semantics-{handle} DOM node created via VirtualizedSemanticRegion (not via the normal
	/// _semanticParentMap path), so focus/membership resolution must recognize it.
	/// </summary>
	private bool IsRealizedVirtualizedItem(IntPtr handle)
	{
		foreach (var registration in _virtualizedRegions.Values)
		{
			if (registration.Region.ContainsRealizedHandle(handle))
			{
				return true;
			}
		}

		return false;
	}

	private bool TryGetVirtualizedSemanticParent(IntPtr handle, out IntPtr parentHandle)
	{
		foreach (var registration in _virtualizedRegions.Values)
		{
			if (registration.Region.ContainsRealizedHandle(handle))
			{
				parentHandle = registration.Region.ContainerHandle;
				return true;
			}
		}

		foreach (var region in _comboBoxListBoxes.Values)
		{
			if (region.ContainsRealizedHandle(handle))
			{
				parentHandle = region.ContainerHandle;
				return true;
			}
		}

		parentHandle = IntPtr.Zero;
		return false;
	}

	/// <summary>
	/// Resolves a UIElement to the nearest handle that exists in the semantic DOM tree.
	/// If the element itself is in the semantic tree, returns its handle.
	/// Otherwise, walks up the visual tree to find the nearest semantic ancestor.
	/// Returns IntPtr.Zero if no semantic element can be found.
	/// </summary>
	internal IntPtr ResolveToSemanticHandle(UIElement element)
	{
		var handle = element.Visual.Handle;

		// Check if this element is directly in the semantic tree
		if (_semanticParentMap.ContainsKey(handle))
		{
			return handle;
		}

		// A realized virtualized item (NavigationViewItem / ListViewItem) has its own
		// uno-semantics-{handle} DOM node created via VirtualizedSemanticRegion, tracked there rather
		// than in _semanticParentMap. Resolve focus to the item itself so XAML focus moves DOM focus
		// onto it instead of walking up to the container ancestor.
		if (IsRealizedVirtualizedItem(handle) || IsRealizedComboBoxItem(handle))
		{
			return handle;
		}

		// Check if this is the root element (it won't be in _semanticParentMap
		// because it's added via AddRootElementToSemanticsRoot, not AddSemanticElement)
		var rootElement = WebAssemblyWindowWrapper.Instance?.Window?.RootElement;
		if (rootElement is not null && rootElement.Visual.Handle == handle)
		{
			return handle;
		}

		// Walk up the visual tree to find the nearest semantic ancestor
		var parent = element.GetParent() as UIElement;
		while (parent is not null)
		{
			var parentHandle = parent.Visual.Handle;
			if (_semanticParentMap.ContainsKey(parentHandle))
			{
				return parentHandle;
			}

			if (rootElement is not null && parentHandle == rootElement.Visual.Handle)
			{
				return parentHandle;
			}

			parent = parent.GetParent() as UIElement;
		}

		return IntPtr.Zero;
	}

	internal bool IsSemanticDescendantOf(IntPtr handle, IntPtr ancestorHandle)
	{
		for (var current = handle; _semanticParentMap.TryGetValue(current, out var parent); current = parent)
		{
			if (parent == ancestorHandle)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Checks whether a given handle is present in the semantic DOM tree.
	/// </summary>
	internal bool HasSemanticElement(IntPtr handle)
	{
		if (_semanticParentMap.ContainsKey(handle))
		{
			return true;
		}

		if (IsRealizedVirtualizedItem(handle) || IsRealizedComboBoxItem(handle))
		{
			return true;
		}

		var rootElement = WebAssemblyWindowWrapper.Instance?.Window?.RootElement;
		return rootElement is not null && rootElement.Visual.Handle == handle;
	}

	private bool TryGetLiveSemanticOwner(IntPtr handle, [NotNullWhen(true)] out UIElement? owner)
	{
		owner = null;
		if (!_isAccessibilityEnabled || handle == IntPtr.Zero || !HasSemanticElement(handle))
		{
			return false;
		}

		try
		{
			if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement candidate } &&
				candidate.Visual.Handle == handle)
			{
				owner = candidate;
				return true;
			}
		}
		catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Rejected invalid semantic handle {handle}: {ex.Message}");
			}
		}

		return false;
	}

	private static bool IsAutomationActionEnabled(AutomationPeer peer)
		=> peer.IsEnabled() && AriaMapper.GetContainingDataGridPeer(peer)?.IsEnabled() != false;

	/// <summary>
	/// Maps each child handle to its semantic parent handle.
	/// This is needed because non-semantic elements (Grid, Border, ContentPresenter, etc.)
	/// are pruned from the accessibility tree, so the visual parent is not always
	/// the semantic parent. Without this, RemoveSemanticElement fails with
	/// "parent handle not found in DOM" errors.
	/// </summary>
	private readonly Dictionary<IntPtr, IntPtr> _semanticParentMap = new();
	private readonly Dictionary<IntPtr, (FrameworkElement Element, TypedEventHandler<FrameworkElement, DataContextChangedEventArgs> Handler)> _dataGridRowSubscriptions = new();
	private readonly Dictionary<IntPtr, (AutomationPeer GridPeer, object Item)> _dataGridRealizedItems = new();
	private readonly Dictionary<IntPtr, (FrameworkElement Element, EventHandler<object> Handler)> _dataGridLayoutSubscriptions = new();
	private readonly Dictionary<IntPtr, (UIElement Owner, AutomationPeer Peer)> _dataGridSummarySubscriptions = new();
	private Timer? _dataGridSummaryPollTimer;
	private int _dataGridSummaryPollGeneration;
	private readonly Dictionary<ContentDialog, (TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs> Opened, TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs> Closed)> _modalDialogSubscriptions = new();
	private readonly Dictionary<IntPtr, int> _dataGridProviderFingerprints = new();
	private static readonly ConditionalWeakTable<Type, DataGridItemPeerFactory> _dataGridItemPeerFactories = new();
	private readonly Dictionary<IntPtr, long> _dataGridLastFingerprintCheckTicks = new();
	private readonly Dictionary<IntPtr, Timer> _dataGridFingerprintThrottleTimers = new();
	private readonly Dictionary<IntPtr, int> _dataGridProviderSummaryFingerprints = new();
	private readonly HashSet<IntPtr> _scheduledDataGridFingerprintChecks = new();
	private readonly HashSet<IntPtr> _scheduledDataGridSummaryChecks = new();
	private const int DataGridFingerprintCheckIntervalMs = 100;
	private const int DataGridSummaryCheckIntervalMs = 2000;
	private readonly HashSet<IntPtr> _scheduledDataGridRefreshes = new();
	private readonly HashSet<IntPtr> _pendingFullDataGridRefreshes = new();
	private readonly Dictionary<IntPtr, HashSet<UIElement>> _pendingDataGridRowRefreshes = new();
	private readonly Dictionary<IntPtr, SemanticElementType> _dataGridHeaderSemanticTypes = new();
	/// <summary>
	/// Handles of elements pruned from the AOM because they were Visibility=Collapsed at build/add
	/// time (T058). When such an element later becomes visible, OnSizeOrOffsetChanged re-emits it —
	/// no other post-build path creates a node and there is no show-counterpart to hide.
	/// </summary>
	private readonly HashSet<IntPtr> _prunedHandles = new();
	/// <summary>
	/// Controls carrying AutomationProperties.LabeledBy whose aria-labelledby IDREF is resolved AFTER
	/// the surrounding subtree exists (FR-019/FR-022). The inline create-time resolution is
	/// order-dependent — the labeller's node may not be registered yet when the labelled control is
	/// built (following sibling / Header child) — so it is re-resolved by a deferred drain once every
	/// labeller is present: at the end of CreateAOM for the initial build, and at the end of the
	/// outermost OnChildAdded call for panels loaded after accessibility is already enabled.
	/// </summary>
	private readonly Dictionary<IntPtr, AutomationPeer> _pendingLabelledBy = new();
	private readonly Dictionary<IntPtr, AutomationPeer> _pendingRelationships = new();
	private readonly Dictionary<IntPtr, AutomationPeer> _labelledBySources = new();
	private readonly Dictionary<IntPtr, AutomationPeer> _relationshipSources = new();
	private readonly Dictionary<IntPtr, HashSet<IntPtr>> _flowsFromSourcesByTarget = new();
	private readonly Dictionary<IntPtr, HashSet<IntPtr>> _inverseFlowTargetsBySource = new();
	private bool _relationshipRefreshScheduled;
	private bool _relationshipFullRefreshPending;
	private int _relationshipRefreshGeneration;
	private int _onChildRemovedDepth;
	private bool _refreshRelationshipsAfterRemoval;

	/// <summary>
	/// Reentrancy depth of <see cref="OnChildAdded"/>. OnChildAdded recurses through a whole subtree
	/// synchronously, so the outermost call (depth returning to 0) is the point at which every labeller
	/// in that subtree has been registered — the moment to drain <see cref="_pendingLabelledBy"/> so a
	/// following-sibling labeller resolves order-independently on the dynamic path too.
	/// </summary>
	private int _onChildAddedDepth;

	/// <summary>
	/// Calculates the cumulative visual offset from a UIElement up to (but not including)
	/// the element whose Visual.Handle matches <paramref name="semanticParentHandle"/>.
	/// This accounts for intermediate non-semantic elements that were pruned from the
	/// accessibility tree, whose offsets would otherwise be lost.
	/// </summary>
	private static Vector3 GetOffsetRelativeToSemanticParent(UIElement element, IntPtr semanticParentHandle)
	{
		var offset = element.Visual.GetTotalOffset();

		var parent = element.GetParent() as UIElement;
		while (parent is not null && parent.Visual.Handle != semanticParentHandle)
		{
			offset += parent.Visual.GetTotalOffset();
			parent = parent.GetParent() as UIElement;
		}

		return offset;
	}

	/// <summary>
	/// Walks up from <paramref name="from"/> to find the ancestor UIElement whose
	/// Visual.Handle equals <paramref name="handle"/>.
	/// </summary>
	private static UIElement? FindUIElementByHandle(UIElement from, IntPtr handle)
	{
		var current = from.GetParent() as UIElement;
		while (current is not null)
		{
			if (current.Visual.Handle == handle)
			{
				return current;
			}
			current = current.GetParent() as UIElement;
		}
		return null;
	}

	protected override void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (!_isAccessibilityEnabled || _isCreatingAOM)
		{
			return;
		}

		// Controls commonly assemble their visual subtree before attaching it to the active XAML
		// root. Emitting those detached children would classify them without their final ancestry and
		// orphan them under the semantic root; the later attached-tree walk would then skip them as
		// duplicates. Wait for the owning subtree to be connected, then emit it through normal recursion.
		if (!IsConnectedToSemanticTree(parent))
		{
			return;
		}

		_onChildAddedDepth++;
		try
		{
			TrySubscribeScrollSource(child);

			// FR-032/T058: a Collapsed element (and its whole subtree) is not rendered — skip both
			// emission and recursion so its descendants do not leak into the AT tree (WinUI: Collapsed
			// is absent from the UIA tree). Equivalent to !child.Visual.IsVisible.
			if (IsPrunedAsHidden(child) || HasPrunedAncestor(parent))
			{
				_prunedHandles.Add(child.Visual.Handle);
				return;
			}

			var isChildSemantic = IsSemanticElement(child);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] OnChildAdded: parent={parent.GetType().Name} handle={parent.Visual.Handle} child={child.GetType().Name} handle={child.Visual.Handle} index={index?.ToString(CultureInfo.InvariantCulture) ?? "append"}");
			}

			// Detect ContentDialog for focus trapping
			TryRegisterModalDialog(child);
			// Detect ComboBox dropdowns so their options form a proper role="listbox"
			TryRegisterComboBox(child);
			TryRealizeComboBoxItem(child);
			EnsureDataGridHeaderParent(parent);

			// Find the nearest semantic ancestor for this child
			var semanticParent = FindSemanticParent(parent);

			if (isChildSemantic)
			{
				// Guard against duplicate additions: ExternalOnChildAdded fires for
				// each child as it's added to the visual tree, but the recursion below
				// also visits children. Without this check, elements at depth D get
				// processed D times, creating duplicate DOM nodes and corrupting
				// the _semanticParentMap (which causes removeChild to throw when
				// the recorded parent doesn't match the actual DOM parent).
				var childHandle = child.Visual.Handle;
				if (!_semanticParentMap.ContainsKey(childHandle))
				{
					if (AddSemanticElement(semanticParent, child, index))
					{
						_semanticParentMap[childHandle] = semanticParent;
						InitializeInverseFlows(child);
						TrackDataGridHeaderSemanticType(child);
						TrySubscribeDataGridProviderSnapshot(child);
						TrySubscribeDataGridRow(child);

						if (child.GetOrCreateAutomationPeer() is { } relationshipPeer)
						{
							ApplyOrDeferLabelledBy(childHandle, relationshipPeer);
							ApplyOrDeferRelationshipAttributes(childHandle, relationshipPeer);
						}
					}
					else
					{
						if (this.Log().IsEnabled(LogLevel.Warning))
						{
							this.Log().Warn($"[A11y] OnChildAdded: AddSemanticElement failed for {child.GetType().Name} handle={child.Visual.Handle}");
						}
					}
				}
			}

			// Register and backfill virtualized containers only after their semantic container exists.
			// This matches the activation-time build order and prevents the ordinary factory from
			// replacing a newly initialized listbox and its realized option subtree.
			TryRegisterVirtualizedContainer(child);
			TryRealizeListViewItem(child);
			TryQueueContainingDataGridRefresh(child);

			// Don't recurse into virtualized containers — their items are managed
			// by VirtualizedSemanticRegion via ContainerContentChanging/ElementPrepared.
			// ComboBox dropdown items are realized as role="option" by the listbox region; recursing
			// would also emit each item's content TextBlock as a standalone <p> (duplicate).
			if (child is not ComboBoxItem &&
				(child is not (ListViewBase or ItemsRepeater) || !isChildSemantic))
			{
				// Recurse into children — if this element was skipped,
				// its children will be parented to the nearest semantic ancestor.
				// The _semanticParentMap guard above prevents duplicate additions
				// when the same element is visited via both ExternalOnChildAdded
				// (fired per-child by UIElement) and this recursion.
				foreach (var childChild in child._children)
				{
					OnChildAdded(child, childChild, null);
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] OnChildAdded failed for {child.GetType().Name}: {ex.Message}", ex);
			}
		}
		finally
		{
			// Outermost call complete: the whole added subtree (and any following-sibling labellers
			// within it) is now registered, so re-resolve the deferred aria-labelledby IDREFs. Mirrors
			// the CreateAOM drain, making the dynamic path order-independent (FR-019/FR-022).
			if (--_onChildAddedDepth == 0)
			{
				QueueRelationshipRefresh();
			}
		}
	}

	private bool IsConnectedToSemanticTree(UIElement element)
	{
		for (var current = element; current is not null; current = current.GetParent() as UIElement)
		{
			var handle = current.Visual.Handle;
			if (handle == _rootElementHandle || _semanticParentMap.ContainsKey(handle))
			{
				return true;
			}
		}

		return false;
	}

	private bool HasPrunedAncestor(UIElement element)
	{
		for (var current = element; current is not null; current = current.GetParent() as UIElement)
		{
			if (IsPrunedAsHidden(current))
			{
				return true;
			}

			var handle = current.Visual.Handle;
			if (handle == _rootElementHandle || _semanticParentMap.ContainsKey(handle))
			{
				return false;
			}
		}

		return false;
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (!_isAccessibilityEnabled || _isCreatingAOM)
		{
			return;
		}

		_onChildRemovedDepth++;
		try
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] OnChildRemoved: parent={parent.GetType().Name} handle={parent.Visual.Handle} child={child.GetType().Name} handle={child.Visual.Handle}");
			}

			TryUnsubscribeScrollSource(child);
			TryUnregisterVirtualizedContainer(child);
			TryUnregisterModalDialog(child);
			TryUnregisterComboBox(child);
			TryUnrealizeComboBoxItem(child);
			TryUnsubscribeDataGridRow(child);
			TryUnsubscribeDataGridProviderSnapshot(child);

			// Remove any children of this element first (they may be semantic even if parent isn't)
			foreach (var childChild in child.GetChildren())
			{
				OnChildRemoved(child, childChild);
			}

			// Only remove from DOM if this element was actually in the semantic tree
			var childHandle = child.Visual.Handle;
			_prunedHandles.Remove(childHandle);
			if (_semanticParentMap.TryGetValue(childHandle, out var semanticParent))
			{
				AutomationPeer? containingDataGridPeer = null;
				try
				{
					containingDataGridPeer = child.GetOrCreateAutomationPeer() is { } childPeer
						? AriaMapper.GetContainingDataGridPeer(childPeer)
						: null;
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"[A11y] Skipped DataGrid refresh lookup while removing {child.GetType().Name}: {ex.Message}");
					}
				}
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] OnChildRemoved: REMOVING from semantic tree child={child.GetType().Name} handle={childHandle} semanticParent={semanticParent}");
				}
				RemoveSemanticElement(semanticParent, childHandle);
				_semanticParentMap.Remove(childHandle);
				_pendingRelationships.Remove(childHandle);
				_pendingLabelledBy.Remove(childHandle);
				_labelledBySources.Remove(childHandle);
				_relationshipSources.Remove(childHandle);
				_dataGridHeaderSemanticTypes.Remove(childHandle);
				_refreshRelationshipsAfterRemoval = true;
				if (containingDataGridPeer is not null)
				{
					QueueDataGridRefresh(containingDataGridPeer);
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] OnChildRemoved failed for {child.GetType().Name}: {ex.Message}", ex);
			}
		}
		finally
		{
			if (--_onChildRemovedDepth == 0)
			{
				try
				{
					DemoteEmptyDataGridHeaderParent(parent);
					if (_refreshRelationshipsAfterRemoval)
					{
						_refreshRelationshipsAfterRemoval = false;
						QueueRelationshipRefresh(refreshResolved: true);
					}
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(LogLevel.Error))
					{
						this.Log().Error($"[A11y] Post-removal accessibility refresh failed: {ex.Message}", ex);
					}
				}
			}
		}
	}

	private void EnsureDataGridHeaderParent(UIElement parent)
	{
		var handle = parent.Visual.Handle;
		if (_semanticParentMap.ContainsKey(handle) ||
			parent.GetOrCreateAutomationPeer() is not { } peer ||
			AriaMapper.GetSemanticElementType(peer, parent) is not SemanticElementType.GridRow)
		{
			return;
		}

		var visualParent = parent.GetParent() as UIElement;
		if (visualParent is null)
		{
			return;
		}

		var semanticParent = FindSemanticParent(visualParent);
		if (AddSemanticElement(semanticParent, parent, 0))
		{
			_semanticParentMap[handle] = semanticParent;
			InitializeInverseFlows(parent);
			TrackDataGridHeaderSemanticType(parent, peer);
			TrySubscribeDataGridProviderSnapshot(parent, peer);
			TrySubscribeDataGridRow(parent);
			ApplyOrDeferLabelledBy(handle, peer);
			ApplyOrDeferRelationshipAttributes(handle, peer);
		}
	}

	private void DemoteEmptyDataGridHeaderParent(UIElement parent)
	{
		var handle = parent.Visual.Handle;
		if (_semanticParentMap.TryGetValue(handle, out var semanticParent) &&
			AutomationProperties.GetAccessibilityView(parent) == AccessibilityView.Raw &&
			!IsRequiredDataGridHeaderStructure(parent))
		{
			RemoveSemanticElement(semanticParent, handle);
			_semanticParentMap.Remove(handle);
			_pendingRelationships.Remove(handle);
			_pendingLabelledBy.Remove(handle);
			_labelledBySources.Remove(handle);
			_relationshipSources.Remove(handle);
			_dataGridHeaderSemanticTypes.Remove(handle);
			_refreshRelationshipsAfterRemoval = true;
		}
	}

	private void TryRegisterVirtualizedContainer(UIElement element)
	{
		if (element is not (ItemsRepeater or ListViewBase))
		{
			return;
		}

		// FR-031: a decorative (AccessibilityView=Raw) container — e.g. RadioButtons' InnerRepeater —
		// must NOT be emitted as a listbox/grid region. The walkers recurse into it so its non-decorative
		// items still emit via the normal path.
		if (!IsSemanticElement(element))
		{
			return;
		}

		// Idempotent: a container may be registered both at AOM-build time (BuildSemanticsTreeRecursive)
		// and via the dynamic OnChildAdded path. Registering twice would create duplicate regions/
		// subscriptions and double-emit items.
		var containerHandle = element.Visual.Handle;
		if (_virtualizedRegions.ContainsKey(containerHandle))
		{
			return;
		}

		if (element is ItemsRepeater repeater)
		{
			var region = new VirtualizedSemanticRegion(
				repeater.Visual.Handle,
				"listbox",
				repeater.GetOrCreateAutomationPeer()?.GetName(),
				false);
			TypedEventHandler<ItemsRepeater, ItemsRepeaterElementPreparedEventArgs> prepared = (s, e) =>
				EmitRealizedItem(region, repeater.Visual.Handle, e.Element, e.Index, repeater.ItemsSourceView?.Count ?? 0, "option");
			TypedEventHandler<ItemsRepeater, ItemsRepeaterElementClearingEventArgs> clearing = (s, e) =>
			{
				var info = ItemsRepeater.GetVirtualizationInfo(e.Element);
				if (info is not null)
				{
					if (region.OnItemUnrealized(e.Element.Visual.Handle, info.Index))
					{
						RemoveFlowsFromTarget(e.Element.Visual.Handle);
						RemoveRelationshipSource(e.Element.Visual.Handle);
						QueueRelationshipRefresh(refreshResolved: true);
					}
				}
			};
			TypedEventHandler<ItemsRepeater, ItemsRepeaterElementIndexChangedEventArgs> indexChanged = (s, e) =>
			{
				var totalCount = repeater.ItemsSourceView?.Count ?? 0;
				region.OnItemIndexChanged(e.Element.Visual.Handle, e.OldIndex, e.NewIndex, totalCount);
				region.UpdateItemCount(totalCount);
			};
			System.Collections.Specialized.NotifyCollectionChangedEventHandler collectionChanged = (_, _) =>
				region.UpdateItemCount(repeater.ItemsSourceView?.Count ?? 0);
			ItemsSourceView? subscribedItemsSourceView = null;
			void UpdateItemsSourceSubscription()
			{
				if (subscribedItemsSourceView is not null)
				{
					subscribedItemsSourceView.CollectionChanged -= collectionChanged;
				}
				subscribedItemsSourceView = repeater.ItemsSourceView;
				if (subscribedItemsSourceView is not null)
				{
					subscribedItemsSourceView.CollectionChanged += collectionChanged;
				}
				region.UpdateItemCount(subscribedItemsSourceView?.Count ?? 0);
			}
			repeater.ElementPrepared += prepared;
			repeater.ElementClearing += clearing;
			repeater.ElementIndexChanged += indexChanged;
			var itemsSourceChangedToken = repeater.RegisterPropertyChangedCallback(
				ItemsRepeater.ItemsSourceProperty,
				(_, _) => UpdateItemsSourceSubscription());
			UpdateItemsSourceSubscription();
			_virtualizedRegions.Add(containerHandle, new(region, () =>
			{
				repeater.ElementPrepared -= prepared;
				repeater.ElementClearing -= clearing;
				repeater.ElementIndexChanged -= indexChanged;
				repeater.UnregisterPropertyChangedCallback(ItemsRepeater.ItemsSourceProperty, itemsSourceChangedToken);
				if (subscribedItemsSourceView is not null)
				{
					subscribedItemsSourceView.CollectionChanged -= collectionChanged;
				}
			}));

			// Backfill items realized before this container was registered (the AOM-build / Enable-
			// Accessibility-after-load flow); ElementPrepared only fires for FUTURE realizations.
			var totalCount = repeater.ItemsSourceView?.Count ?? 0;
			foreach (var itemElement in repeater.Children)
			{
				var info = ItemsRepeater.GetVirtualizationInfo(itemElement);
				if (info is not null && info.IsRealized)
				{
					EmitRealizedItem(region, repeater.Visual.Handle, itemElement, info.Index, totalCount, "option");
				}
			}
		}
		else if (element is ListViewBase listView)
		{
			var region = new VirtualizedSemanticRegion(
				listView.Visual.Handle,
				"listbox",
				listView.GetOrCreateAutomationPeer()?.GetName(),
				listView.SelectionMode == ListViewSelectionMode.Multiple ||
				listView.SelectionMode == ListViewSelectionMode.Extended);
			const string itemRole = "option";

			TypedEventHandler<ListViewBase, ContainerContentChangingEventArgs> contentChanging = (s, e) =>
			{
				if (!e.InRecycleQueue)
				{
					if (e.ItemContainer is { } itemElement)
					{
						EmitRealizedItem(region, listView.Visual.Handle, itemElement, e.ItemIndex, listView.Items?.Count ?? 0, itemRole);
					}
				}
				else if (e.ItemContainer is { } itemElement)
				{
					if (region.OnItemUnrealized(itemElement.Visual.Handle, e.ItemIndex))
					{
						RemoveFlowsFromTarget(itemElement.Visual.Handle);
						RemoveRelationshipSource(itemElement.Visual.Handle);
						QueueRelationshipRefresh(refreshResolved: true);
					}
				}
			};
			listView.ContainerContentChanging += contentChanging;
			EventHandler<ListViewBase.UnoContainerClearingEventArgs> containerClearing = (_, args) =>
			{
				if (region.OnItemUnrealized(args.Container.Visual.Handle, args.Index))
				{
					RemoveFlowsFromTarget(args.Container.Visual.Handle);
					RemoveRelationshipSource(args.Container.Visual.Handle);
					QueueRelationshipRefresh(refreshResolved: true);
				}
			};
			listView.UnoContainerClearing += containerClearing;
			EventHandler itemsChanged = (_, _) =>
			{
				var count = listView.Items?.Count ?? 0;
				region.UpdateMultiselectable(
					listView.SelectionMode == ListViewSelectionMode.Multiple ||
					listView.SelectionMode == ListViewSelectionMode.Extended);
				var removedHandles = region.ResynchronizeItems(
					listView.MaterializedContainers.OfType<UIElement>()
						.Select(container => (container.Visual.Handle, listView.IndexFromContainer(container)))
						.Where(item => item.Item2 >= 0),
					count);
				CleanupVirtualizedHandles(removedHandles);
			};
			listView.UnoItemsChangedForAccessibility += itemsChanged;
			_virtualizedRegions.Add(containerHandle, new(region, () =>
			{
				listView.ContainerContentChanging -= contentChanging;
				listView.UnoContainerClearing -= containerClearing;
				listView.UnoItemsChangedForAccessibility -= itemsChanged;
			}));

			// Backfill already-materialized containers (the Enable-Accessibility-after-load flow).
			var totalCount = listView.Items?.Count ?? 0;
			foreach (var container in listView.MaterializedContainers.OfType<UIElement>())
			{
				var index = listView.IndexFromContainer(container);
				if (index >= 0)
				{
					EmitRealizedItem(region, listView.Visual.Handle, container, index, totalCount, itemRole);
				}
			}
		}
	}

	/// <summary>
	/// Emits a single realized virtualized item into its region — shared by the live
	/// ElementPrepared/ContainerContentChanging handlers and the build-time backfill.
	/// </summary>
	private void EmitRealizedItem(VirtualizedSemanticRegion region, IntPtr containerHandle, UIElement itemElement, int index, int totalCount, string role)
	{
		// FR-031: a realized container may be a decorative/non-semantic element rather than a real
		// destination — e.g. NavigationView hosts NavigationViewItemSeparator and
		// NavigationViewItemHeader (both AccessibilityView=Raw) in the same menu ItemsRepeater as its
		// NavigationViewItems. Emitting those as role="option" exposes decorative clutter to AT
		// (A11y Inspector WARN). Skip anything IsSemanticElement prunes (Raw short-circuit, structural,
		// absorbed TextBlock), matching the membership rule the rest of the AOM walk already enforces.
		if (!IsSemanticElement(itemElement))
		{
			return;
		}

		var itemPeer = itemElement.GetOrCreateAutomationPeer();
		var label = itemPeer?.GetName() ?? string.Empty;
		var selected = itemElement is SelectorItem selectorItem
			? selectorItem.IsSelected
			: TryGetVirtualizedItemSelection(itemPeer);
		var disabled = itemPeer?.IsEnabled() != true;
		var focusable = itemPeer is not null && IsAccessibilityFocusable(itemElement, itemElement.IsFocusable);
		var offset = GetOffsetRelativeToSemanticParent(itemElement, containerHandle);
		var removedHandle = region.OnItemRealized(
			itemElement.Visual.Handle,
			index,
			totalCount,
			offset.X, offset.Y,
			itemElement.Visual.Size.X, itemElement.Visual.Size.Y,
			role, label,
			selected, disabled, focusable);
		CleanupVirtualizedHandle(removedHandle);
		ApplyVirtualizedAutomationId(itemElement);
		InitializeInverseFlows(itemElement);
		if (itemPeer is not null)
		{
			ApplyOrDeferLabelledBy(itemElement.Visual.Handle, itemPeer);
			ApplyOrDeferRelationshipAttributes(itemElement.Visual.Handle, itemPeer);
		}
		QueueRelationshipRefresh();
	}

	private static void ApplyVirtualizedAutomationId(UIElement itemElement)
		=> SemanticElementFactory.SetXamlAutomationId(
			itemElement.Visual.Handle,
			AutomationProperties.GetAutomationId(itemElement) ?? string.Empty);

	private void CleanupVirtualizedHandles(IEnumerable<IntPtr> handles)
	{
		var removedAny = false;
		foreach (var handle in handles)
		{
			if (handle == IntPtr.Zero)
			{
				continue;
			}

			CleanupVirtualizedHandle(handle);
			removedAny = true;
		}

		if (removedAny)
		{
			QueueRelationshipRefresh(refreshResolved: true);
		}
	}

	private void CleanupVirtualizedHandle(IntPtr handle)
	{
		if (handle == IntPtr.Zero)
		{
			return;
		}

		RemoveFlowsFromTarget(handle);
		RemoveRelationshipSource(handle);
	}

	private void TryRealizeListViewItem(UIElement element)
	{
		if (element is not SelectorItem item ||
			ItemsControl.ItemsControlFromItemContainer(item) is not ListViewBase listView ||
			!_virtualizedRegions.TryGetValue(listView.Visual.Handle, out var registration))
		{
			return;
		}

		var index = listView.IndexFromContainer(item);
		if (index >= 0)
		{
			EmitRealizedItem(
				registration.Region,
				listView.Visual.Handle,
				item,
				index,
				listView.Items?.Count ?? 0,
				"option");
		}
	}

	private static bool TryGetVirtualizedItemSelection(AutomationPeer? peer)
		=> peer is not null &&
			AriaMapper.GetPatternOrEventsSource(peer, PatternInterface.SelectionItem) is ISelectionItemProvider { IsSelected: true };

	private void RemoveRelationshipSource(IntPtr handle)
	{
		_pendingRelationships.Remove(handle);
		_pendingLabelledBy.Remove(handle);
		_labelledBySources.Remove(handle);
		_relationshipSources.Remove(handle);
	}

	private void TryUnregisterVirtualizedContainer(UIElement element)
	{
		if (element is (ItemsRepeater or ListViewBase) &&
			_virtualizedRegions.Remove(element.Visual.Handle, out var registration))
		{
			registration.Dispose();
		}
	}

	private sealed class VirtualizedRegionRegistration : IDisposable
	{
		private Action? _unsubscribe;

		public VirtualizedRegionRegistration(VirtualizedSemanticRegion region, Action unsubscribe)
		{
			Region = region;
			_unsubscribe = unsubscribe;
		}

		public VirtualizedSemanticRegion Region { get; }

		public void Dispose()
		{
			foreach (var handle in Region.GetRealizedHandles())
			{
				Instance.RemoveRelationshipSource(handle);
			}
			_unsubscribe?.Invoke();
			_unsubscribe = null;
			Region.Dispose();
		}
	}

	private void TryRegisterModalDialog(UIElement element)
	{
		if (element is ContentDialog dialog && !_modalDialogSubscriptions.ContainsKey(dialog))
		{
			TypedEventHandler<ContentDialog, ContentDialogOpenedEventArgs> opened = (s, e) =>
			{
				if (!IsAccessibilityEnabled)
				{
					return;
				}

				// Save trigger element (currently focused element before dialog opens)
				var triggerHandle = _focusSynchronizer?.CurrentFocusedHandle ?? IntPtr.Zero;

				// Enumerate focusable children within the dialog
				var focusableChildren = new List<IntPtr>();
				EnumerateFocusableChildren(dialog, focusableChildren);

				// Create and activate the modal focus scope
				var scope = new ModalFocusScope(dialog.Visual.Handle, triggerHandle, focusableChildren);
				scope.Activate(ActiveModalScope);
				ActiveModalScope = scope;

				// Notify LiveRegionManager so it suppresses background live region updates
				if (_liveRegionManager is { } lrm)
				{
					lrm.ActiveModalHandle = dialog.Visual.Handle;
				}

				// Announce the dialog title for screen readers
				var dialogPeer = dialog.GetOrCreateAutomationPeer();
				var dialogTitle = dialogPeer?.GetName();
				if (!string.IsNullOrEmpty(dialogTitle))
				{
					NativeMethods.AnnounceAssertive(dialogTitle);
				}
			};

			TypedEventHandler<ContentDialog, ContentDialogClosedEventArgs> closed = (s, e) =>
			{
				if (!IsAccessibilityEnabled || ActiveModalScope is null)
				{
					return;
				}

				if (ActiveModalScope.ModalHandle == dialog.Visual.Handle)
				{
					var parentScope = ActiveModalScope.ParentScope;
					ActiveModalScope.Deactivate();
					ActiveModalScope = parentScope;

					// Update LiveRegionManager: restore parent modal or clear
					if (_liveRegionManager is { } lrm)
					{
						lrm.ActiveModalHandle = parentScope?.ModalHandle ?? IntPtr.Zero;
					}
				}
			};
			dialog.Opened += opened;
			dialog.Closed += closed;
			_modalDialogSubscriptions.Add(dialog, (opened, closed));
		}
	}

	private void TryUnregisterModalDialog(UIElement element)
	{
		if (element is ContentDialog dialog && _modalDialogSubscriptions.Remove(dialog, out var subscription))
		{
			dialog.Opened -= subscription.Opened;
			dialog.Closed -= subscription.Closed;
		}
	}

	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		if (IsAccessibilityEnabled && visual is ContainerVisual containerVisual)
		{
			// Only use Visual.IsVisible (maps to Visibility.Collapsed) for hidden detection.
			// We intentionally do NOT call peer.IsOffscreen() here because
			// UIElement.GetGlobalBoundsWithOptions is currently an unimplemented stub
			// that always returns empty Rect, causing IsOffscreen() to return true
			// for every element with a non-null automation peer. This prevents
			// UpdateSemanticElementPositioning from ever being called after navigation,
			// leaving all elements at (0,0,0,0) hidden.
			var isHidden = !visual.IsVisible;

			if (isHidden)
			{
				NativeMethods.HideSemanticElement(containerVisual.Handle);
			}
			else
			{
				var handle = containerVisual.Handle;

				// FR-013/FR-014: a ScrollViewer's region eligibility depends on its scrollability,
				// which is only known once its content extent has been computed during layout. When the
				// AOM node was built via OnChildAdded (before layout), the ScrollViewer was not yet
				// scrollable, so the region role was dropped (and a named ScrollViewer fell back to
				// "group"). Re-evaluate the region gate now that a size/offset change has settled the
				// layout, upgrading the node to role=region once it is genuinely scrollable and named.
				if (containerVisual.Owner?.Target is UIElement changedElement)
				{
					TryUpdateScrollRegionRole(changedElement);
				}

				// T058: a previously-Collapsed element pruned at build/add time has no semantic node; now
				// that it is visible again, re-emit it (and its now-visible subtree). No other post-build
				// path creates a node (there is no show-counterpart to HideSemanticElement).
				if (_prunedHandles.Remove(handle) && containerVisual.Owner?.Target is UIElement shownElement)
				{
					var shownParent = shownElement.GetParent() as UIElement;
					var shownParentHandle = shownParent is not null ? FindSemanticParent(shownParent) : _rootElementHandle;
					BuildSemanticsTreeRecursive(shownParentHandle, shownElement);
					QueueRelationshipRefresh();
					return;
				}

				if (containerVisual.Owner?.Target is UIElement element &&
					TryGetVirtualizedSemanticParent(handle, out var virtualizedParentHandle))
				{
					var offset = GetOffsetRelativeToSemanticParent(element, virtualizedParentHandle);
					NativeMethods.UpdateSemanticElementPositioning(handle, visual.Size.X, visual.Size.Y, offset.X, offset.Y);
				}
				else if (_semanticParentMap.TryGetValue(handle, out var semanticParentHandle)
					&& containerVisual.Owner?.Target is UIElement mappedElement)
				{
					// Use the full element-to-semantic-parent transform so that
					// RenderTransform, Scale, etc. are reflected in the position.
					var semanticParentElement = FindUIElementByHandle(mappedElement, semanticParentHandle);
					var localRect = new Windows.Foundation.Rect(0, 0, visual.Size.X, visual.Size.Y);
					if (semanticParentElement is not null)
					{
						var transform = UIElement.GetTransform(from: mappedElement, to: semanticParentElement);
						var transformedRect = transform.Transform(localRect);
						NativeMethods.UpdateSemanticElementPositioning(handle, (float)transformedRect.Width, (float)transformedRect.Height, (float)transformedRect.X, (float)transformedRect.Y);
					}
					else
					{
						var transform = UIElement.GetTransform(from: mappedElement, to: null);
						var transformedRect = transform.Transform(localRect);
						NativeMethods.UpdateSemanticElementPositioning(handle, (float)transformedRect.Width, (float)transformedRect.Height, (float)transformedRect.X, (float)transformedRect.Y);
					}
				}
				else if (HasSemanticElement(handle))
				{
					// Root semantic element — use full transform to root.
					if (containerVisual.Owner?.Target is UIElement rootElement)
					{
						var transform = UIElement.GetTransform(from: rootElement, to: null);
						var localRect = new Windows.Foundation.Rect(0, 0, visual.Size.X, visual.Size.Y);
						var transformedRect = transform.Transform(localRect);
						NativeMethods.UpdateSemanticElementPositioning(handle, (float)transformedRect.Width, (float)transformedRect.Height, (float)transformedRect.X, (float)transformedRect.Y);
					}
					else
					{
						var totalOffset = visual.GetTotalOffset();
						NativeMethods.UpdateSemanticElementPositioning(handle, visual.Size.X, visual.Size.Y, totalOffset.X, totalOffset.Y);
					}
				}
			}
		}
	}

	/// <summary>
	/// Re-evaluates the <c>role=region</c> gate (FR-013/FR-014) for the <see cref="ScrollViewer"/> that
	/// owns or is the nearest semantic ancestor of <paramref name="changedElement"/>, after a layout
	/// change. The gate is layout-dependent (scrollability is only known once the content extent is
	/// computed), but the AOM node may have been built via OnChildAdded before layout — at which point a
	/// scrollable, named ScrollViewer is mis-emitted as <c>role=group</c>. This brings the live DOM role
	/// in line with the current scrollable+named state once layout has settled.
	/// </summary>
	private void TryUpdateScrollRegionRole(UIElement changedElement)
	{
		// Find the ScrollViewer that owns this layout change (itself or the nearest ancestor that has a
		// semantic node). Content growth fires for descendants, so a bounded ancestor walk is needed.
		var current = changedElement;
		while (current is not null)
		{
			if (current is ScrollViewer scrollViewer)
			{
				if (!HasSemanticElement(scrollViewer.Visual.Handle))
				{
					return;
				}

				var peer = scrollViewer.GetOrCreateAutomationPeer();

				if (peer is not null)
				{
					UpdateNameDependentRole(scrollViewer, peer);
				}

				return;
			}

			current = current.GetParent() as UIElement;
		}
	}

	/// <summary>
	/// Called from TypeScript during Accessibility.setup() to check whether the developer
	/// has opted in to auto-enabling accessibility (bypassing the "Enable Accessibility" button).
	/// </summary>
	[JSExport]
	public static bool IsAutoEnableAccessibility()
		=> FeatureConfiguration.AutomationPeer.AutoEnableAccessibility;

	// Retry state for EnableAccessibility if Window isn't ready
	private static int _enableAccessibilityRetryCount;
	private static Timer? _enableAccessibilityRetryTimer;
	private static int _enableAccessibilityRetryGeneration;
	private static readonly int MaxEnableAccessibilityRetries = 20; // ~2 seconds with 100ms delay
	private static readonly int EnableAccessibilityRetryDelayMs = 100;

	[JSExport]
	public static void EnableAccessibility()
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Debug))
		{
			@this.Log().Debug("[A11y] EnableAccessibility() called");
		}

		if (@this.IsAccessibilityEnabled)
		{
			if (@this.Log().IsEnabled(LogLevel.Warning))
			{
				@this.Log().Warn("[A11y] EnableAccessibility() called for the second time. Returning early.");
			}

			return;
		}

		var window = WebAssemblyWindowWrapper.Instance.Window;
		var rootElement = window?.RootElement;

		if (rootElement is null)
		{
			// Window not yet attached is normal during early boot; retried below.
			if (@this.Log().IsEnabled(LogLevel.Debug))
			{
				@this.Log().Debug($"[A11y] EnableAccessibility deferred: Window={window?.GetType().Name ?? "null"}, RootElement=null");
			}

			if (_enableAccessibilityRetryTimer is not null)
			{
				return;
			}

			if (_enableAccessibilityRetryCount >= MaxEnableAccessibilityRetries)
			{
				if (@this.Log().IsEnabled(LogLevel.Error))
				{
					@this.Log().Error($"[A11y] EnableAccessibility: max retries ({MaxEnableAccessibilityRetries}) exceeded; Window still not ready.");
				}
				CancelEnableAccessibilityRetry();
				NativeMethods.OnAccessibilityActivationFailed();
				return;
			}

			_enableAccessibilityRetryCount++;
			if (@this.Log().IsEnabled(LogLevel.Trace))
			{
				@this.Log().Trace($"[A11y] EnableAccessibility() will retry in {EnableAccessibilityRetryDelayMs}ms (attempt {_enableAccessibilityRetryCount}/{MaxEnableAccessibilityRetries})");
			}

			var retryGeneration = ++_enableAccessibilityRetryGeneration;
			_enableAccessibilityRetryTimer = new Timer(
				_ => NativeDispatcher.Main.Enqueue(() =>
				{
					if (retryGeneration != _enableAccessibilityRetryGeneration)
					{
						return;
					}

					_enableAccessibilityRetryTimer?.Dispose();
					_enableAccessibilityRetryTimer = null;
					if (@this.Log().IsEnabled(LogLevel.Trace))
					{
						@this.Log().Trace($"[A11y] EnableAccessibility() retry attempt {_enableAccessibilityRetryCount}");
					}

					try
					{
						EnableAccessibility();
					}
					catch (Exception ex)
					{
						@this.Log().Error("[A11y] EnableAccessibility retry failed.", ex);
						CancelEnableAccessibilityRetry();
						NativeMethods.OnAccessibilityActivationFailed();
					}
				}),
				null,
				EnableAccessibilityRetryDelayMs,
				Timeout.Infinite);

			return;
		}

		// Success! Window and RootElement are now available
		CancelEnableAccessibilityRetry();
		if (@this.Log().IsEnabled(LogLevel.Debug))
		{
			@this.Log().Debug($"[A11y] EnableAccessibility() SUCCESS: rootElement={rootElement.GetType().Name}, children={rootElement.GetChildren().Count}");
		}

		@this._isAccessibilityEnabled = true;
		@this._isCreatingAOM = true;
		try
		{
			@this.CreateAOM(rootElement);
			@this._isCreatingAOM = false;
			Control.OnIsFocusableChangedCallback = @this.UpdateIsFocusable;

			@this._liveRegionManager = new LiveRegionManager();
			@this._focusSynchronizer = new FocusSynchronizer(@this);
			@this._focusSynchronizer.Initialize();

			FocusManager.SuppressNativeFocus = true;
			@this._focusSearchRoot = rootElement;
			NativeMethods.InstallFocusSentinels();

			var focusManager = global::Uno.UI.Xaml.Core.VisualTree.GetFocusManagerForElement(rootElement);
			if (focusManager is not null)
			{
				focusManager.FocusObserver.FocusController.FocusDeparting -= @this.OnFocusDeparting;
				focusManager.FocusObserver.FocusController.FocusDeparting += @this.OnFocusDeparting;
			}

			NativeMethods.OnAccessibilityActivationSucceeded();
		}
		catch (Exception ex)
		{
			var rollbackFailures = @this.ResetFailedAccessibilityActivation();
			if (@this.Log().IsEnabled(LogLevel.Error))
			{
				@this.Log().Error($"[A11y] Accessibility activation failed and was rolled back: {ex.Message}", ex);
				foreach (var (step, error) in rollbackFailures)
				{
					@this.Log().Error($"[A11y] Accessibility activation rollback step '{step}' failed.", error);
				}
			}
			return;
		}
		finally
		{
			@this._isCreatingAOM = false;
		}
	}

	private List<(string Step, Exception Error)> ResetFailedAccessibilityActivation()
	{
		var failures = new List<(string, Exception)>();
		_isAccessibilityEnabled = false;
		TryRollbackCleanup(failures, "cancel activation retry", CancelEnableAccessibilityRetry);
		Control.OnIsFocusableChangedCallback = null;
		TryRollbackCleanup(failures, "restore native focus", () => FocusManager.SuppressNativeFocus = false);
		TryRollbackCleanup(failures, "uninitialize focus synchronizer", () => _focusSynchronizer?.Uninitialize());
		_focusSynchronizer = null;
		TryRollbackCleanup(failures, "clear live regions", () => _liveRegionManager?.ClearPending());
		_liveRegionManager = null;
		TryRollbackCleanup(failures, "unsubscribe focus departure", () =>
		{
			if (_focusSearchRoot is { } focusSearchRoot &&
				global::Uno.UI.Xaml.Core.VisualTree.GetFocusManagerForElement(focusSearchRoot) is { } focusManager)
			{
				focusManager.FocusObserver.FocusController.FocusDeparting -= OnFocusDeparting;
			}
		});
		_focusSearchRoot = null;
		TryRollbackCleanup(failures, "deactivate modal scope", () => ActiveModalScope?.Deactivate());
		ActiveModalScope = null;

		foreach (var registration in _virtualizedRegions.Values.ToArray())
		{
			TryRollbackCleanup(failures, "dispose virtualized region", registration.Dispose);
		}
		_virtualizedRegions.Clear();
		TryRollbackCleanup(failures, "reset ComboBox tracking", ResetComboBoxTracking);
		foreach (var subscription in _modalDialogSubscriptions)
		{
			TryRollbackCleanup(failures, "unsubscribe modal dialog", () =>
			{
				subscription.Key.Opened -= subscription.Value.Opened;
				subscription.Key.Closed -= subscription.Value.Closed;
			});
		}
		_modalDialogSubscriptions.Clear();
		foreach (var subscription in _dataGridRowSubscriptions.Values)
		{
			TryRollbackCleanup(failures, "unsubscribe DataGrid row", () =>
			{
				subscription.Element.DataContextChanged -= subscription.Handler;
				ReleaseDataGridRowBinding(subscription.Element);
			});
		}
		_dataGridRowSubscriptions.Clear();
		_dataGridRealizedItems.Clear();
		foreach (var subscription in _dataGridLayoutSubscriptions.Values)
		{
			TryRollbackCleanup(
				failures,
				"unsubscribe DataGrid layout",
				() => subscription.Element.LayoutUpdated -= subscription.Handler);
		}
		_dataGridLayoutSubscriptions.Clear();
		TryRollbackCleanup(failures, "reset DataGrid summary tracking", ResetDataGridSummaryPolling);
		foreach (var timer in _dataGridFingerprintThrottleTimers.Values)
		{
			TryRollbackCleanup(failures, "dispose DataGrid throttle timer", timer.Dispose);
		}
		_dataGridFingerprintThrottleTimers.Clear();
		TryRollbackCleanup(failures, "reset scroll subscriptions", ResetScrollSourceSubscriptions);

		_rootElementHandle = IntPtr.Zero;
		_semanticParentMap.Clear();
		_prunedHandles.Clear();
		_pendingLabelledBy.Clear();
		_pendingRelationships.Clear();
		_labelledBySources.Clear();
		_relationshipSources.Clear();
		_flowsFromSourcesByTarget.Clear();
		_inverseFlowTargetsBySource.Clear();
		_relationshipRefreshScheduled = false;
		_relationshipFullRefreshPending = false;
		_relationshipRefreshGeneration++;
		_dataGridHeaderSemanticTypes.Clear();
		_dataGridProviderFingerprints.Clear();
		_dataGridLastFingerprintCheckTicks.Clear();
		_dataGridProviderSummaryFingerprints.Clear();
		_scheduledDataGridFingerprintChecks.Clear();
		_scheduledDataGridSummaryChecks.Clear();
		_scheduledDataGridRefreshes.Clear();
		_pendingFullDataGridRefreshes.Clear();
		_pendingDataGridRowRefreshes.Clear();
		TryRollbackCleanup(failures, "clear semantic tree", NativeMethods.ClearSemanticTree);
		TryRollbackCleanup(failures, "remove focus sentinels", NativeMethods.RemoveFocusSentinels);
		TryRollbackCleanup(failures, "notify activation failure", NativeMethods.OnAccessibilityActivationFailed);
		return failures;
	}

	private static void TryRollbackCleanup(
		List<(string Step, Exception Error)> failures,
		string step,
		Action cleanup)
	{
		try
		{
			cleanup();
		}
		catch (Exception ex)
		{
			failures.Add((step, ex));
		}
	}

	private static void CancelEnableAccessibilityRetry()
	{
		_enableAccessibilityRetryGeneration++;
		_enableAccessibilityRetryTimer?.Dispose();
		_enableAccessibilityRetryTimer = null;
		_enableAccessibilityRetryCount = 0;
	}

	[JSExport]
	public static void OnScroll(IntPtr handle, double horizontalOffset, double verticalOffset)
	{
		var @this = Instance;
		@this.ExecuteAutomationAction(handle, "scroll", owner =>
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer is null || !IsAutomationActionEnabled(peer))
			{
				return;
			}
			// // TODO (DOTI): We shouldn't check individual scrollers.
			// Instead, we should scroll using automation peers once they are implemented correctly for SCP and ScrollPresenter
			if (owner is ScrollContentPresenter scp)
			{
				scp.Set(horizontalOffset, verticalOffset);
			}
			else if (owner is ScrollPresenter sp)
			{
				sp.ScrollTo(horizontalOffset, verticalOffset);
			}
		});
	}

	private void ExecuteAutomationAction(IntPtr handle, string action, Action<UIElement> execute)
	{
		if (!TryGetLiveSemanticOwner(handle, out var owner))
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Ignored stale accessibility {action} for semantic handle {handle}; its owner is no longer live.");
			}
			return;
		}

		execute(owner);
	}

	/// <summary>
	/// Called when a button element is invoked (clicked, Enter pressed, or Space pressed).
	/// Routes to the IInvokeProvider.Invoke() method on the automation peer.
	/// </summary>
	[JSExport]
	public static void OnInvoke(IntPtr handle)
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnInvoke called for handle: {handle}");
		}

		@this.ExecuteAutomationAction(handle, "invoke", owner =>
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer is not null && IsAutomationActionEnabled(peer) &&
				peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProvider)
			{
				invokeProvider.Invoke();
				if (peer.GetAutomationControlType() is AutomationControlType.HeaderItem &&
					AriaMapper.GetContainingDataGridPeer(peer) is not null)
				{
					@this.QueueDataGridRefresh(peer);
				}
			}
		});
	}

	/// <summary>
	/// Called when a toggle element (checkbox, radio button) is toggled.
	/// Routes to the IToggleProvider.Toggle() method on the automation peer.
	/// </summary>
	[JSExport]
	public static void OnToggle(IntPtr handle)
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnToggle called for handle: {handle}");
		}

		@this.ExecuteAutomationAction(handle, "toggle", owner =>
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer is not null && IsAutomationActionEnabled(peer) &&
				peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
			{
				toggleProvider.Toggle();
			}
		});
	}

	/// <summary>
	/// Called when a slider's value changes.
	/// Routes to the IRangeValueProvider.SetValue() method on the automation peer.
	/// </summary>
	[JSExport]
	public static void OnRangeValueChange(IntPtr handle, double value)
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnRangeValueChange called for handle: {handle}, value: {value}");
		}

		@this.ExecuteAutomationAction(handle, "range value", owner =>
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer is not null && IsAutomationActionEnabled(peer) &&
				peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider { IsReadOnly: false } rangeValueProvider)
			{
				rangeValueProvider.SetValue(value);
			}
		});
	}

	/// <summary>
	/// Called when text is input in a text box.
	/// Routes to the IValueProvider.SetValue() method on the automation peer.
	/// </summary>
	[JSExport]
	public static void OnTextInput(IntPtr handle, string value, int selectionStart, int selectionEnd)
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnTextInput called for handle: {handle}, value length: {value?.Length ?? 0}, selection: {selectionStart}-{selectionEnd}");
		}

		@this.ExecuteAutomationAction(handle, "text input", owner =>
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer is null || !IsAutomationActionEnabled(peer) ||
				peer.GetPattern(PatternInterface.Value) is not IValueProvider { IsReadOnly: false } valueProvider)
			{
				return;
			}

			if (owner is TextBox textBox)
			{
				var maxLength = value?.Length ?? 0;
				selectionStart = Math.Max(0, Math.Min(selectionStart, maxLength));
				selectionEnd = Math.Max(selectionStart, Math.Min(selectionEnd, maxLength));
				textBox.SetPendingSelection(selectionStart, selectionEnd - selectionStart);
			}

			valueProvider.SetValue(value);
		});
	}

	/// <summary>
	/// Called when a combobox or expander is expanded/collapsed.
	/// Routes to the IExpandCollapseProvider.Expand() or Collapse() method on the automation peer.
	/// </summary>
	[JSExport]
	public static void OnExpandCollapse(IntPtr handle)
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnExpandCollapse called for handle: {handle}");
		}

		@this.ExecuteAutomationAction(handle, "expand/collapse", owner =>
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer is not null && IsAutomationActionEnabled(peer) &&
				peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider)
			{
				// Toggle the expand/collapse state
				if (expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.Collapsed)
				{
					expandCollapseProvider.Expand();
				}
				else
				{
					expandCollapseProvider.Collapse();
				}
			}
		});
	}

	/// <summary>
	/// Called when a list item is selected.
	/// Routes to the ISelectionItemProvider.Select() method on the automation peer.
	/// </summary>
	[JSExport]
	public static void OnSelection(IntPtr handle)
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnSelection called for handle: {handle}");
		}

		@this.ExecuteAutomationAction(handle, "selection", owner =>
		{
			var peer = owner.GetOrCreateAutomationPeer()
				?? throw new InvalidOperationException($"Semantic option {handle} has no automation peer.");
			if (!IsAutomationActionEnabled(peer))
			{
				return;
			}

			var selectionItemProvider = AriaMapper.GetPatternOrEventsSource(peer, PatternInterface.SelectionItem) as ISelectionItemProvider
				?? throw new InvalidOperationException($"Semantic option {handle} does not expose SelectionItem.");
			if (selectionItemProvider is ComboBoxItemDataAutomationPeer comboBoxItemDataPeer)
			{
				comboBoxItemDataPeer.Select();
			}
			else
			{
				selectionItemProvider.Select();
			}
		});
	}

	/// <summary>
	/// Called when a semantic element receives focus from the browser.
	/// Used to synchronize focus between the semantic DOM and the Uno visual tree.
	/// </summary>
	[JSExport]
	public static void OnFocus(IntPtr handle)
	{
		var instance = Instance;
		if (instance.Log().IsEnabled(LogLevel.Trace))
		{
			instance.Log().Trace($"OnFocus called for handle: {handle}");
		}

		instance.ExecuteAutomationAction(handle, "focus", owner =>
		{
			foreach (var registration in instance._virtualizedRegions.Values)
			{
				if (registration.Region.ContainsRealizedHandle(handle))
				{
					var removedHandle = registration.Region.PinFocusedItem(handle);
					if (removedHandle != IntPtr.Zero)
					{
						instance.CleanupVirtualizedHandle(removedHandle);
						instance.QueueRelationshipRefresh(refreshResolved: true);
					}
					break;
				}
			}

			if (owner is TextBox)
			{
				BrowserInvisibleTextBoxViewExtension.DetachNativeInputPreservingFocus();
			}

			if (instance._focusSynchronizer is { } synchronizer)
			{
				synchronizer.OnBrowserFocus(handle, owner);
			}
			else if (owner is Control control && control.IsFocusable)
			{
				control.Focus(FocusState.Keyboard);
			}
		});
	}

	/// <summary>
	/// Called when a semantic element loses focus in the browser.
	/// Used to synchronize focus between the semantic DOM and the Uno visual tree.
	/// </summary>
	[JSExport]
	public static void OnBlur(IntPtr handle)
	{
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnBlur called for handle: {handle}");
		}

		// Focus leaving the semantic element is handled by the browser focus system.
		// No explicit action needed here - the Uno FocusManager handles focus transitions.
	}


	[JSExport]
	public static void OnFocusSentinel(bool isStart)
	{
		var @this = Instance;
		try
		{
			var root = @this._focusSearchRoot;
			if (root is null)
			{
				return;
			}

			var candidate = isStart
				? FocusManager.FindFirstFocusableElement(root)
				: FocusManager.FindLastFocusableElement(root);

			var leaf = candidate is UIElement candidateElement
				? @this.ResolveEntrySemanticLeaf(candidateElement, isStart)
				: null;

			if (leaf is not Control { IsFocusable: true } control)
			{
				return;
			}

			@this._suppressDeparture = true;
			try
			{
				control.Focus(FocusState.Keyboard);

				// Force the DOM sync: when control was already the XAML-focused element,
				// GotFocus does not fire and DOM focus would stay stranded on the sentinel.
				var semanticHandle = @this.ResolveToSemanticHandle(control);
				if (semanticHandle != IntPtr.Zero)
				{
					NativeMethods.FocusSemanticElement(semanticHandle);
				}
			}
			finally
			{
				@this._suppressDeparture = false;
			}
		}
		catch (Exception ex)
		{
			if (@this.Log().IsEnabled(LogLevel.Warning))
			{
				@this.Log().Warn($"[A11y] Ignored failed focus-sentinel action: {ex.Message}");
			}
		}
	}

	// First/last descendant-or-self owning a focusable semantic element, skipping
	// focusable containers (e.g. a navigation SplitView) that have none of their own.
	private UIElement? ResolveEntrySemanticLeaf(UIElement root, bool first)
	{
		if (HasOwnFocusableSemanticElement(root))
		{
			return root;
		}

		var children = root.GetChildren();
		var ordered = first ? children : children.Reverse();
		foreach (var child in ordered)
		{
			if (child is UIElement childElement
				&& ResolveEntrySemanticLeaf(childElement, first) is { } found)
			{
				return found;
			}
		}

		return null;
	}

	private bool HasOwnFocusableSemanticElement(UIElement element)
		=> HasSemanticElement(element.Visual.Handle)
			&& IsAccessibilityFocusable(element, (element as Control)?.IsFocusable ?? element.IsFocusable);

	private void OnFocusDeparting(object sender, object args)
	{
		if (_suppressDeparture)
		{
			return;
		}

		NativeMethods.FocusDepartureSentinel(BrowserKeyboardInputSource.LastTabWasForward);
	}

	private void UpdateIsFocusable(Control control, bool isFocusable)
	{
		// Only update focusability for elements that are in the semantic DOM tree.
		// Many controls fire IsFocusable changes but were pruned from the semantic
		// tree, so calling into JS would be a no-op (element not found).
		var handle = control.Visual.Handle;
		if (HasSemanticElement(handle))
		{
			NativeMethods.UpdateIsFocusable(handle, IsAccessibilityFocusable(control, isFocusable));
		}
	}

	private void UpdateRoleOverride(UIElement element, string? role)
	{
		if (!_isAccessibilityEnabled || !HasSemanticElement(element.Visual.Handle))
		{
			return;
		}

		var peer = element.GetOrCreateAutomationPeer();
		var roleOverride = NormalizeRoleOverrideForHost(element, peer, role);
		ApplyRoleOverride(element, peer, roleOverride);
	}

	private void ApplyRoleOverride(UIElement element, AutomationPeer? peer, string? roleOverride)
	{
		var effectiveRole = roleOverride ?? ResolveDefaultSemanticRole(element, peer);
		var primaryRole = GetPrimaryRole(effectiveRole);
		NativeMethods.UpdateRoleOverride(element.Visual.Handle, effectiveRole ?? string.Empty, roleOverride is not null);

		var label = peer is not null ? AriaMapper.ResolveLabel(peer) : AutomationProperties.GetName(element);
		var roleProhibitsNaming = RoleProhibitsNaming(primaryRole);
		NativeMethods.UpdateAriaLabel(element.Visual.Handle, roleProhibitsNaming ? string.Empty : label ?? string.Empty);
		if (roleProhibitsNaming)
		{
			_labelledBySources.Remove(element.Visual.Handle);
			_pendingLabelledBy.Remove(element.Visual.Handle);
			NativeMethods.UpdateAriaLabelledBy(element.Visual.Handle, string.Empty);
		}
		else if (peer is not null)
		{
			ApplyOrDeferLabelledBy(element.Visual.Handle, peer);
		}
		UpdateAuthoredRoleDescription(element, primaryRole, label);
		NativeMethods.UpdateAriaLevel(element.Visual.Handle, ResolveAriaLevel(element, peer));
		SynchronizeRoleDependentState(element, peer, effectiveRole, roleOverride is not null);
	}

	private static bool RoleProhibitsNaming(string? role)
		=> role is "caption" or "code" or "deletion" or "emphasis" or "generic" or "insertion" or
			"none" or "paragraph" or "presentation" or "strong" or "subscript" or "superscript";

	private static bool ElementRoleProhibitsNaming(UIElement element, AutomationPeer? peer)
	{
		var roleOverride = NormalizeRoleOverrideForHost(element, peer, AutomationProperties.GetRoleOverride(element));
		return RoleProhibitsNaming(GetPrimaryRole(roleOverride ?? ResolveDefaultSemanticRole(element, peer)));
	}

	private void UpdateNameDependentRole(UIElement element, AutomationPeer peer)
	{
		if (element is not ScrollViewer &&
			AutomationProperties.GetLandmarkType(element) == AutomationLandmarkType.None)
		{
			return;
		}

		if (NormalizeRoleOverrideForHost(element, peer, AutomationProperties.GetRoleOverride(element)) is not null)
		{
			return;
		}

		NativeMethods.UpdateLandmarkRole(
			element.Visual.Handle,
			ResolveDefaultSemanticRole(element, peer) ?? string.Empty);
	}

	private static void UpdateAuthoredRoleDescription(UIElement element, string? primaryRole, string? label)
	{
		var roleDescription = RoleProhibitsNaming(primaryRole) || string.IsNullOrEmpty(label)
			? null
			: AutomationProperties.GetLandmarkType(element) != AutomationLandmarkType.None
				? AutomationProperties.GetLocalizedLandmarkType(element)
				: AutomationProperties.GetLocalizedControlType(element);
		NativeMethods.UpdateAriaRoleDescription(element.Visual.Handle, roleDescription ?? string.Empty);
	}

	private static int ResolveAriaLevel(UIElement element, AutomationPeer? peer)
	{
		var level = AutomationProperties.GetLevel(element);
		if (level > 0)
		{
			return level;
		}

		var headingLevel = peer?.GetHeadingLevel() ?? AutomationHeadingLevel.None;
		return headingLevel == AutomationHeadingLevel.None ? 0 : (int)headingLevel;
	}

	private static string? NormalizeRoleOverrideForHost(UIElement element, AutomationPeer? peer, string? role)
	{
		var normalized = AriaMapper.NormalizeAriaRole(role);
		if (normalized is null || peer is null)
		{
			return normalized;
		}

		var elementType = AriaMapper.GetSemanticElementType(peer, element);
		var compatibleRoles = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Where(candidate => IsRoleCompatibleWithHost(elementType, peer, candidate))
			.ToArray();
		return compatibleRoles.Length > 0 ? string.Join(' ', compatibleRoles) : null;
	}

	private static bool IsRoleCompatibleWithHost(SemanticElementType elementType, AutomationPeer peer, string role)
	{
		return elementType switch
		{
			SemanticElementType.Button or SemanticElementType.ToggleButton or SemanticElementType.Switch =>
				IsButtonHostRoleCompatible(peer, role),
			SemanticElementType.Checkbox => role is "checkbox" or "menuitemcheckbox" or "option" or "switch" or "button",
			SemanticElementType.RadioButton => role is "radio" or "menuitemradio",
			SemanticElementType.Slider => role == "slider",
			SemanticElementType.TextBox => role is "textbox" or "combobox" or "searchbox" or "spinbutton",
			SemanticElementType.TextArea => role == "textbox",
			SemanticElementType.Password => false,
			SemanticElementType.Heading => role is "heading" or "none" or "presentation" or "tab",
			_ => true,
		};
	}

	private static bool IsButtonHostRoleCompatible(AutomationPeer peer, string role)
	{
		if (role is not ("button" or "checkbox" or "combobox" or "gridcell" or "link" or "menuitem" or
			"menuitemcheckbox" or "menuitemradio" or "option" or "radio" or "separator" or "slider" or
			"switch" or "tab" or "treeitem"))
		{
			return false;
		}

		if (role is "checkbox" or "menuitemcheckbox" or "menuitemradio" or "radio" or "switch")
		{
			return peer.GetPattern(PatternInterface.Toggle) is IToggleProvider ||
				AriaMapper.GetPatternOrEventsSource(peer, PatternInterface.SelectionItem) is ISelectionItemProvider;
		}
		if (role == "slider")
		{
			return peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider;
		}
		if (role == "combobox")
		{
			return peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider;
		}

		return true;
	}

	private static string? GetPrimaryRole(string? role)
		=> string.IsNullOrWhiteSpace(role) ? null : role.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0];

	private static bool SupportsAriaSetPosition(UIElement element, AutomationPeer peer)
		=> GetPrimaryRole(NormalizeRoleOverrideForHost(element, peer, AutomationProperties.GetRoleOverride(element)) ?? ResolveDefaultSemanticRole(element, peer))
			is "article" or "listitem" or "menuitem" or "menuitemcheckbox" or "menuitemradio" or "option" or "radio" or "row" or "tab" or "treeitem";

	private void SynchronizeRoleDependentState(UIElement element, AutomationPeer? peer, string? effectiveRole, bool isOverride)
	{
		if (peer is null)
		{
			return;
		}

		var elementType = AriaMapper.GetSemanticElementType(peer, element);
		if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
		{
			var state = AriaMapper.ConvertToggleStateToAriaChecked(toggleProvider.ToggleState);
			if (isOverride)
			{
				var primaryRole = GetPrimaryRole(effectiveRole);
				var attribute = primaryRole is "checkbox" or "menuitemcheckbox" or "menuitemradio" or "option" or "radio" or "switch" or "treeitem"
					? "aria-checked"
					: primaryRole == "button"
						? "aria-pressed"
						: string.Empty;
				NativeMethods.UpdateRoleOverrideToggleState(element.Visual.Handle, attribute, state);
			}
			else if (elementType == SemanticElementType.ToggleButton)
			{
				NativeMethods.UpdateAriaPressed(element.Visual.Handle, state);
			}
			else
			{
				NativeMethods.UpdateAriaChecked(element.Visual.Handle, state);
			}
		}

		if (elementType == SemanticElementType.ComboBox && element is ComboBox comboBox &&
			(!isOverride || GetPrimaryRole(effectiveRole) == "combobox"))
		{
			var expanded = peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider &&
				expandCollapseProvider.ExpandCollapseState is ExpandCollapseState.Expanded or ExpandCollapseState.PartiallyExpanded;
			NativeMethods.UpdateExpandCollapseState(element.Visual.Handle, expanded);
			NativeMethods.UpdateAriaHasPopup(element.Visual.Handle, "listbox");
			NativeMethods.UpdateComboBoxValue(
				element.Visual.Handle,
				SemanticElementFactory.ResolveComboBoxValue(peer, comboBox) ?? string.Empty);
		}
	}

	private static string? ResolveDefaultSemanticRole(UIElement element, AutomationPeer? peer)
	{
		if (peer?.IsDialog() == true)
		{
			return "dialog";
		}

		var landmarkType = AutomationProperties.GetLandmarkType(element);
		if (landmarkType != AutomationLandmarkType.None)
		{
			var landmarkRole = AriaMapper.GetLandmarkRole(landmarkType);
			var name = peer is not null ? AriaMapper.ResolveAuthoredLabel(peer) : AutomationProperties.GetName(element);
			if (landmarkRole is not ("region" or "form") || !string.IsNullOrEmpty(name))
			{
				return landmarkRole;
			}
		}

		if (peer is null)
		{
			return string.IsNullOrEmpty(AutomationProperties.GetName(element)) ? null : "group";
		}

		var semanticType = AriaMapper.GetSemanticElementType(peer, element);
		var role = semanticType switch
		{
			SemanticElementType.Switch => "switch",
			SemanticElementType.ComboBox => "combobox",
			SemanticElementType.ListBox => "listbox",
			SemanticElementType.ListItem => "option",
			SemanticElementType.TabList => "tablist",
			SemanticElementType.Tab => "tab",
			SemanticElementType.Tree => "tree",
			SemanticElementType.TreeItem => "treeitem",
			SemanticElementType.Grid => "grid",
			SemanticElementType.GridRow => "row",
			SemanticElementType.GridCell => "gridcell",
			SemanticElementType.ColumnHeader => "columnheader",
			SemanticElementType.RowHeader => "rowheader",
			SemanticElementType.Menu => "menu",
			SemanticElementType.MenuItem => "menuitem",
			SemanticElementType.Button or SemanticElementType.ToggleButton or SemanticElementType.Checkbox or
				SemanticElementType.RadioButton or SemanticElementType.Slider or SemanticElementType.TextBox or
				SemanticElementType.TextArea or SemanticElementType.Password or SemanticElementType.Link or
				SemanticElementType.Heading or SemanticElementType.Text => null,
			_ => AriaMapper.GetAriaRole(peer.GetAutomationControlType()),
		};

		if (role == "region" && !AriaMapper.QualifiesAsNamedScrollRegion(peer, element))
		{
			role = null;
		}
		var canUseContentDerivedGroupName =
			element is not ScrollViewer &&
			AutomationProperties.GetLandmarkType(element) == AutomationLandmarkType.None;
		return role ?? (semanticType == SemanticElementType.Generic &&
			canUseContentDerivedGroupName &&
			!string.IsNullOrEmpty(AriaMapper.ResolveLabel(peer))
			? "group"
			: null);
	}

	internal void CreateAOM(UIElement rootElement)
	{
		Debug.Assert(IsAccessibilityEnabled);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM: rootElement={rootElement.GetType().Name}, handle={rootElement.Visual.Handle}, size={rootElement.Visual.Size.X}x{rootElement.Visual.Size.Y}");
		}

		TrySubscribeScrollSource(rootElement);

		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		var rootHandle = rootElement.Visual.Handle;
		_rootElementHandle = rootHandle;

		// Root element is placed directly under uno-semantics-root — use its local offset
		var rootOffset = rootElement.Visual.GetTotalOffset();
		NativeMethods.AddRootElementToSemanticsRoot(rootHandle, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, rootOffset.X, rootOffset.Y, IsAccessibilityFocusable(rootElement, rootElement.IsFocusable));

		// Set role="application" on the root so VoiceOver uses app interaction mode
		// instead of document-style page navigation
		NativeMethods.UpdateLandmarkRole(rootHandle, "application");
		InitializeInverseFlows(rootElement);

		var topLevelChildren = rootElement.GetChildren().ToList();
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM: building tree for {topLevelChildren.Count} top-level children of {rootElement.GetType().Name}");
		}
		foreach (var child in topLevelChildren)
		{
			BuildSemanticsTreeRecursive(rootHandle, child, depth: 1);
		}

		// FR-019/FR-022: now that the full AOM exists, every labeller with a semantic node is
		// registered. Re-resolve the deferred aria-labelledby IDREFs so emission is order-independent
		// (covers labellers built after the labelled control). HasSemanticElement still gates each one.
		RefreshRelationshipSources();

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM complete");
		}
	}

	/// <summary>
	/// Re-resolves every deferred aria-labelledby IDREF now that more of the tree exists, then clears
	/// the queue. Shared by the CreateAOM drain and the OnChildAdded drain so both paths are
	/// order-independent (a labeller built after the labelled control still resolves). Each entry is
	/// gated by <see cref="SemanticElementFactory.ResolveLabelledByIdRef"/>, which only emits when the
	/// labeller actually has a semantic node — so a dangling IDREF is never written (FR-019/FR-022).
	/// </summary>
	private void DrainPendingLabelledBy()
	{
		if (_pendingLabelledBy.Count == 0)
		{
			return;
		}

		// Re-resolve each deferred entry; emit + drop the ones whose labeller now has a semantic node,
		// and KEEP the rest. OnChildAdded fires per-element, so a following-sibling labeller may not be
		// registered when its labelled control drains — keeping the entry lets it resolve on the
		// labeller's own (later) drain. ResolveLabelledByIdRef's HasSemanticElement gate still applies.
		foreach (var (labelledHandle, labelledPeer) in _pendingLabelledBy.ToArray())
		{
			if (TryGetPeerOwner(labelledPeer, out var labelledElement) &&
				ElementRoleProhibitsNaming(labelledElement, labelledPeer))
			{
				NativeMethods.UpdateAriaLabelledBy(labelledHandle, string.Empty);
				_pendingLabelledBy.Remove(labelledHandle);
				_labelledBySources.Remove(labelledHandle);
				continue;
			}

			var labelledById = SemanticElementFactory.ResolveLabelledByIdRef(labelledPeer);
			if (labelledById is not null)
			{
				NativeMethods.UpdateAriaLabelledBy(labelledHandle, labelledById);
				_pendingLabelledBy.Remove(labelledHandle);
			}
		}
	}

	private void ApplyOrDeferLabelledBy(IntPtr handle, AutomationPeer peer)
	{
		if (!HasSemanticElement(handle))
		{
			RemoveRelationshipSource(handle);
			return;
		}

		if (TryGetPeerOwner(peer, out var element) && ElementRoleProhibitsNaming(element, peer))
		{
			_labelledBySources.Remove(handle);
			_pendingLabelledBy.Remove(handle);
			NativeMethods.UpdateAriaLabelledBy(handle, string.Empty);
			return;
		}

		try
		{
			if (AriaMapper.ResolveLabelledByElement(peer) is null)
			{
				_labelledBySources.Remove(handle);
				_pendingLabelledBy.Remove(handle);
				NativeMethods.UpdateAriaLabelledBy(handle, string.Empty);
				return;
			}

			_labelledBySources[handle] = peer;
			var labelledById = SemanticElementFactory.ResolveLabelledByIdRef(peer);
			NativeMethods.UpdateAriaLabelledBy(handle, labelledById ?? string.Empty);
			_pendingLabelledBy.Remove(handle);
			if (labelledById is null)
			{
				_pendingLabelledBy[handle] = peer;
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Failed to apply aria-labelledby for handle={handle}: {ex.Message}");
			}
		}
	}

	private void ApplyOrDeferRelationshipAttributes(IntPtr handle, AutomationPeer peer)
	{
		if (!HasSemanticElement(handle))
		{
			RemoveRelationshipSource(handle);
			return;
		}

		try
		{
			var allResolved = SemanticElementFactory.ApplyRelationshipAttributes(peer, handle, out var hasRelationships);
			if (!hasRelationships)
			{
				_relationshipSources.Remove(handle);
				_pendingRelationships.Remove(handle);
				return;
			}

			_relationshipSources[handle] = peer;
			if (!allResolved)
			{
				_pendingRelationships[handle] = peer;
			}
			else if (allResolved)
			{
				_pendingRelationships.Remove(handle);
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Failed to apply relationship attributes for handle={handle}: {ex.Message}");
			}
		}
	}

	private void InitializeInverseFlows(UIElement element)
	{
		RefreshFlowsFromTarget(element);
		ApplyInverseFlowsToForSource(element.Visual.Handle);
	}

	private void RefreshFlowsFromTarget(UIElement target)
	{
		var targetHandle = target.Visual.Handle;
		if (!HasSemanticElement(targetHandle))
		{
			RemoveFlowsFromTarget(targetHandle);
			return;
		}

		var newSources = new HashSet<IntPtr>();
		if (AutomationProperties.TryGetFlowsFrom(target) is { } authoredSources)
		{
			foreach (var source in authoredSources.OfType<UIElement>())
			{
				if (source.Visual.Handle != IntPtr.Zero)
				{
					newSources.Add(source.Visual.Handle);
				}
			}
		}

		try
		{
			if (target.GetOrCreateAutomationPeer() is { } targetPeer)
			{
				foreach (var sourcePeer in targetPeer.GetFlowsFrom() ?? Enumerable.Empty<AutomationPeer>())
				{
					if (sourcePeer is FrameworkElementAutomationPeer { Owner: UIElement source } && source.Visual.Handle != IntPtr.Zero)
					{
						newSources.Add(source.Visual.Handle);
					}
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Failed to resolve FlowsFrom for handle={targetHandle}: {ex.Message}");
			}
		}

		var previousSources = _flowsFromSourcesByTarget.TryGetValue(targetHandle, out var existing)
			? existing
			: new HashSet<IntPtr>();
		var affectedSources = new HashSet<IntPtr>(previousSources);
		affectedSources.UnionWith(newSources);

		foreach (var sourceHandle in previousSources.Except(newSources))
		{
			if (_inverseFlowTargetsBySource.TryGetValue(sourceHandle, out var targets))
			{
				targets.Remove(targetHandle);
				if (targets.Count == 0)
				{
					_inverseFlowTargetsBySource.Remove(sourceHandle);
				}
			}
		}

		foreach (var sourceHandle in newSources.Except(previousSources))
		{
			if (!_inverseFlowTargetsBySource.TryGetValue(sourceHandle, out var targets))
			{
				targets = new HashSet<IntPtr>();
				_inverseFlowTargetsBySource[sourceHandle] = targets;
			}
			targets.Add(targetHandle);
		}

		if (newSources.Count == 0)
		{
			_flowsFromSourcesByTarget.Remove(targetHandle);
		}
		else
		{
			_flowsFromSourcesByTarget[targetHandle] = newSources;
		}

		foreach (var sourceHandle in affectedSources)
		{
			ApplyInverseFlowsToForSource(sourceHandle);
		}
	}

	private void RemoveFlowsFromTarget(IntPtr targetHandle)
	{
		if (_flowsFromSourcesByTarget.Remove(targetHandle, out var sources))
		{
			foreach (var sourceHandle in sources)
			{
				if (_inverseFlowTargetsBySource.TryGetValue(sourceHandle, out var targets))
				{
					targets.Remove(targetHandle);
					if (targets.Count == 0)
					{
						_inverseFlowTargetsBySource.Remove(sourceHandle);
					}
				}
				ApplyInverseFlowsToForSource(sourceHandle);
			}
		}

	}

	private void ApplyInverseFlowsToForSource(IntPtr sourceHandle)
	{
		if (!HasSemanticElement(sourceHandle))
		{
			return;
		}

		var idList = _inverseFlowTargetsBySource.TryGetValue(sourceHandle, out var targets)
			? string.Join(' ', targets.Where(HasSemanticElement).Select(static handle => $"uno-semantics-{handle}"))
			: string.Empty;
		NativeMethods.UpdateInverseAriaFlowTo(sourceHandle, idList);
	}

	private void DrainPendingRelationships()
	{
		foreach (var (handle, peer) in _pendingRelationships.ToArray())
		{
			if (!HasSemanticElement(handle))
			{
				_pendingRelationships.Remove(handle);
				continue;
			}

			try
			{
				var allResolved = SemanticElementFactory.ApplyRelationshipAttributes(peer, handle, out var hasRelationships);
				if (hasRelationships)
				{
					_relationshipSources[handle] = peer;
				}
				else
				{
					_relationshipSources.Remove(handle);
				}

				if (allResolved || !hasRelationships)
				{
					_pendingRelationships.Remove(handle);
				}
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"[A11y] Deferred relationship update failed for handle={handle}: {ex.Message}");
				}
			}
		}
	}

	private void RefreshRelationshipSources(bool refreshResolved = true)
	{
		if (refreshResolved)
		{
			foreach (var (handle, peer) in _labelledBySources.ToArray())
			{
				if (HasSemanticElement(handle))
				{
					ApplyOrDeferLabelledBy(handle, peer);
				}
				else
				{
					_labelledBySources.Remove(handle);
					_pendingLabelledBy.Remove(handle);
				}
			}

			foreach (var (handle, peer) in _relationshipSources.ToArray())
			{
				if (HasSemanticElement(handle))
				{
					ApplyOrDeferRelationshipAttributes(handle, peer);
				}
				else
				{
					_relationshipSources.Remove(handle);
					_pendingRelationships.Remove(handle);
				}
			}
		}

		DrainPendingLabelledBy();
		DrainPendingRelationships();
	}

	private void QueueRelationshipRefresh(bool refreshResolved = false)
	{
		_relationshipFullRefreshPending |= refreshResolved;
		if (_isCreatingAOM || _relationshipRefreshScheduled)
		{
			return;
		}

		_relationshipRefreshScheduled = true;
		var generation = _relationshipRefreshGeneration;
		NativeDispatcher.Main.Enqueue(() =>
		{
			if (generation != _relationshipRefreshGeneration)
			{
				return;
			}

			_relationshipRefreshScheduled = false;
			var fullRefresh = _relationshipFullRefreshPending;
			_relationshipFullRefreshPending = false;
			if (_isAccessibilityEnabled)
			{
				RefreshRelationshipSources(fullRefresh);
			}
		});
	}

	/// <summary>
	/// FR-032/T058: a Collapsed element (and its entire subtree) is not rendered and must not be
	/// exposed to assistive technology — matching WinUI (Collapsed is absent from the UIA tree) and
	/// the framework's own render walk (which skips {IsVisible:false} subtrees). Equivalent to
	/// !element.Visual.IsVisible (set only from Arrange's Visibility==Collapsed branch), but read
	/// from the Visibility DP so it also prunes a Collapsed element that has not yet been arranged.
	/// </summary>
	private static bool IsPrunedAsHidden(UIElement element)
		=> element.Visibility == Visibility.Collapsed;

	/// <summary>
	/// Determines whether a UIElement should be included in the semantic accessibility tree.
	/// Elements without an automation peer, ARIA role, or automation ID are purely structural
	/// (e.g., Grid, Border, ContentPresenter) and are pruned to reduce DOM bloat.
	/// </summary>
	private bool IsSemanticElement(UIElement element)
	{
		// Elements with AccessibilityView="Raw" are excluded from the accessibility tree entirely.
		// DataGrid is the exception: Toolkit marks its header presenter/header cells Raw even though
		// ARIA requires the presenter row and table-validated headers for a conforming grid tree.
		var accessibilityView = AutomationProperties.GetAccessibilityView(element);
		if (accessibilityView == AccessibilityView.Raw && !IsRequiredDataGridHeaderStructure(element))
		{
			return false;
		}

		// ComboBox dropdown items are surfaced as role="option" under a dedicated role="listbox"
		// region (see TryRealizeComboBoxItem). Emitting them through the generic path would orphan
		// them under the Popup's role="dialog", which the browser invalidates (the option resolves
		// to "paragraph"). Skip them here so the listbox region is their sole owner.
		if (element is ComboBoxItem)
		{
			return false;
		}

		// The ComboBox dropdown Popup is a structureless role="dialog" wrapper; its only meaningful
		// content (the options) lives in the listbox region. Suppress the empty dialog node so screen
		// readers don't announce a contentless dialog.
		// Matched via the ComboBox's GetPopup() — a Popup template part does not reliably carry
		// TemplatedParent, so suppress by identity against tracked ComboBoxes (IsComboBoxDropdownPopup).
		if (element is Popup comboBoxPopup && IsComboBoxDropdownPopup(comboBoxPopup))
		{
			return false;
		}

		// TextBlock and RichTextBlock are static text elements that contribute their
		// text content to parent elements via AriaMapper.ResolveLabel(). Including
		// them as separate semantic elements creates:
		// - Nested focusable elements inside buttons/list items (WCAG 4.1.2 violation)
		// - Invalid role="label" announcements (VoiceOver reads as "group")
		// - DOM bloat (122+ extra elements in typical pages)
		// Skip them unless they have explicit accessibility properties set
		// (Name, LandmarkType, LiveSetting, HeadingLevel).
		if (element is TextBlock or RichTextBlock or RichTextBlockOverflow)
		{
			if (AriaMapper.RequiresGenericTextSemantics(element))
			{
				return true;
			}
			return IsStandaloneBodyText(element);
		}

		// Elements with an automation peer are semantic.
		var peer = element.GetOrCreateAutomationPeer();
		if (peer is not null)
		{
			return true;
		}

		// Elements with an explicit ARIA role override are semantic
		var role = AutomationProperties.FindHtmlRole(element);
		if (!string.IsNullOrEmpty(role))
		{
			return true;
		}

		// Elements with an automationId are semantic (used for testing/identification)
		var automationId = AutomationProperties.GetAutomationId(element);
		if (!string.IsNullOrEmpty(automationId))
		{
			return true;
		}

		// Containers with an explicit AutomationProperties.Name act as accessible groups.
		// This matches WinUI3 behavior where named containers create UIA groups.
		var automationName = AutomationProperties.GetName(element);
		if (!string.IsNullOrEmpty(automationName))
		{
			return true;
		}

		// Elements with a LandmarkType (Navigation, Main, Search, etc.) are semantic.
		// In WinUI3, landmarks create UIA landmark regions for screen reader rotor navigation.
		var landmarkType = AutomationProperties.GetLandmarkType(element);
		if (landmarkType != AutomationLandmarkType.None)
		{
			return true;
		}

		// Elements with a LiveSetting (Polite/Assertive) are semantic.
		// They need to be in the DOM tree so live region announcements work.
		var liveSetting = AutomationProperties.GetLiveSetting(element);
		if (liveSetting != AutomationLiveSetting.Off)
		{
			return true;
		}

		// Scroll ports need semantic nodes for scroll interaction
		if (element.IsScrollPort)
		{
			return true;
		}

		// Everything else (Grid, Border, ContentPresenter, StackPanel, etc.) is structural
		return false;
	}

	private static bool IsRequiredDataGridHeaderStructure(UIElement element)
	{
		if (element.GetOrCreateAutomationPeer() is not { } peer)
		{
			return false;
		}

		var semanticType = AriaMapper.GetSemanticElementType(peer, element);
		return semanticType is SemanticElementType.ColumnHeader or SemanticElementType.RowHeader ||
			semanticType is SemanticElementType.GridRow &&
				peer.GetAutomationControlType() is AutomationControlType.Header;
	}

	/// <summary>
	/// FR-015: a plain TextBlock is exposed as standalone body text only when its text is not
	/// already carried by an ancestor control's accessible name (else it would be announced twice).
	/// </summary>
	private static bool IsStandaloneBodyText(UIElement element)
	{
		// RichTextBlockOverflow is the paired-display target of a primary RichTextBlock — never standalone.
		if (element is RichTextBlockOverflow)
		{
			return false;
		}

		// Only TextBlock exposes reliable plain text here; RichTextBlock has no GetPlainText source
		// so it stays pruned (documented FR-015 limitation).
		var text = (element as TextBlock)?.Text;
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		// A TextBlock inside a ComboBox carries text that is already conveyed by a richer role, so
		// emitting it as a standalone <p> would duplicate the announcement. ExternalOnChildAdded fires
		// per-element (not only via recursion), so the ComboBoxItem recursion-stop alone cannot prune
		// these — gate on the visual ancestor chain instead:
		//  - under a ComboBoxItem: the dropdown option's label is carried by its role="option" in the
		//    listbox region (TryRealizeComboBoxItem).
		//  - under a ComboBox (but no intervening ComboBoxItem): the head faceplate's selected value is
		//    conveyed by the combobox role/value (aria-activedescendant / the head's name).
		// An ImplicitTextBlock is the auto-generated text of a presenting control's string content
		// (a ComboBoxItem option, the combobox faceplate, a Button caption, …), so its text is always
		// conveyed by that control — never standalone body text. The visual-parent walk below misses
		// popup-hosted content (managed GetParent does not traverse the popup host), so gate on type.
		if (element is ImplicitTextBlock)
		{
			return false;
		}

		if (HasComboBoxOrComboBoxItemAncestor(element))
		{
			return false;
		}

		return !IsAbsorbedByAncestorName(element, text);
	}

	/// <summary>
	/// True when the visual ancestor chain of <paramref name="element"/> includes a ComboBoxItem
	/// (dropdown option) or a ComboBox (head faceplate). Such text is already conveyed by the
	/// listbox option / combobox role, so a plain descendant TextBlock must not be re-emitted as a
	/// standalone &lt;p&gt;.
	/// </summary>
	private static bool HasComboBoxOrComboBoxItemAncestor(UIElement element)
	{
		var node = element.GetParent() as UIElement;
		while (node is not null)
		{
			if (node is ComboBoxItem or ComboBox)
			{
				return true;
			}

			node = node.GetParent() as UIElement;
		}

		return false;
	}

	private static bool IsAbsorbedByAncestorName(UIElement element, string ownText)
	{
		// Walk ancestors looking for the nearest one that names itself from this element.
		// Termination is normally governed by the first peer-bearing ancestor with a resolved
		// accessible name (see the peer.ResolveLabel branch below), which always answers
		// "absorbed" or "not absorbed" and stops the walk. The depth cap is a defensive
		// runaway guard for malformed trees with no named ancestor at all -- 16 is well
		// past any realistic XAML nesting depth for a control's own labelling chain.
		var node = element.GetParent() as UIElement;
		for (var depth = 0; node is not null && depth < 16; depth++, node = node.GetParent() as UIElement)
		{
			// Identity: a ContentControl whose Content IS this element (or whose string content matches)
			// names itself from it (AriaMapper.ResolveLabel / FR-033), so the text is already announced.
			if (node is ContentControl contentControl)
			{
				if (ReferenceEquals(contentControl.Content, element))
				{
					return true;
				}
				if (contentControl.Content is string s && string.Equals(s, ownText, StringComparison.Ordinal))
				{
					return true;
				}
			}

			// First peer-bearing ancestor with a resolved name decides: equal to this text => absorbed;
			// named from something else => this text is not its label and remains standalone.
			if (node.GetOrCreateAutomationPeer() is { } peer)
			{
				var name = AriaMapper.ResolveLabel(peer);
				if (!string.IsNullOrEmpty(name))
				{
					return string.Equals(name, ownText, StringComparison.Ordinal);
				}
			}
		}

		return false;
	}

	/// <summary>
	/// Finds the nearest semantic ancestor handle for a given visual parent.
	/// Walks up the visual tree until it finds an element that was added to
	/// the semantic tree (tracked in _semanticParentMap) or is itself semantic.
	/// </summary>
	private IntPtr FindSemanticParent(UIElement visualParent)
	{
		var handle = visualParent.Visual.Handle;

		// If the visual parent is itself in the semantic tree, use it
		if (_semanticParentMap.ContainsKey(handle))
		{
			return handle;
		}

		// Fallback: walk the visual tree up to find the nearest semantic ancestor
		// that actually exists in the DOM (i.e., in _semanticParentMap or the root element).
		var parent = visualParent.GetParent() as UIElement;
		while (parent is not null)
		{
			var parentHandle = parent.Visual.Handle;
			if (_semanticParentMap.ContainsKey(parentHandle) || parentHandle == _rootElementHandle)
			{
				return parentHandle;
			}
			parent = parent.GetParent() as UIElement;
		}

		// Ultimate fallback: use the root element handle
		return _rootElementHandle;
	}

	internal void BuildSemanticsTreeRecursive(IntPtr parentHandle, UIElement child, int depth = 0)
	{
		Debug.Assert(IsAccessibilityEnabled);

		TrySubscribeScrollSource(child);
		// Subscribe ComboBoxes encountered during the initial walk, and realize any options
		// for a dropdown that is already open when accessibility is enabled.
		TryRegisterComboBox(child);
		TryRealizeComboBoxItem(child);

		// FR-032/T058: a Collapsed element (and its whole subtree) is not rendered — skip both
		// emission and recursion so its descendants do not leak into the AT tree (WinUI: Collapsed
		// is absent from the UIA tree). Equivalent to !child.Visual.IsVisible.
		if (IsPrunedAsHidden(child))
		{
			_prunedHandles.Add(child.Visual.Handle);
			return;
		}

		var handle = child.Visual.Handle;
		var isSemantic = false;
		try
		{
			isSemantic = IsSemanticElement(child);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Skipped unavailable peer metadata for {child.GetType().Name}: {ex.Message}");
			}
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			var peer = child.GetOrCreateAutomationPeer();
			var peerType = peer?.GetAutomationControlType().ToString() ?? "(no peer)";
			this.Log().Trace($"[A11y] BuildTree: depth={depth} type={child.GetType().Name} handle={handle} controlType={peerType} semantic={isSemantic}");
		}

		// Determine the effective parent for children of this element
		var effectiveParent = parentHandle;

		if (isSemantic && !_semanticParentMap.ContainsKey(handle))
		{
			var added = false;
			try
			{
				added = AddSemanticElement(parentHandle, child, null);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"[A11y] Skipped semantic node {child.GetType().Name} handle={handle}: {ex.Message}");
				}
			}
			if (added)
			{
				_semanticParentMap[handle] = parentHandle;
				InitializeInverseFlows(child);
				TrackDataGridHeaderSemanticType(child);
				TrySubscribeDataGridProviderSnapshot(child);
				TrySubscribeDataGridRow(child);
				effectiveParent = handle; // children go under this element
			}
			else if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] AddSemanticElement returned false for {child.GetType().Name} handle={handle}");
			}
		}

		if (_semanticParentMap.ContainsKey(handle) && child.GetOrCreateAutomationPeer() is { } relationshipPeer)
		{
			ApplyOrDeferLabelledBy(handle, relationshipPeer);
			ApplyOrDeferRelationshipAttributes(handle, relationshipPeer);
		}

		// Register virtualized containers (and backfill their already-realized items) at AOM-build
		// time. OnChildAdded is suppressed during the initial build (_isCreatingAOM guard), so without
		// this a NavigationView/list already realized at Enable-Accessibility time would never emit its
		// items — ElementPrepared only fires for future realizations (T057/FR-031).
		TryRegisterVirtualizedContainer(child);

		// Don't recurse into virtualized containers — their items are managed
		// by VirtualizedSemanticRegion via ContainerContentChanging/ElementPrepared.
		if (child is (ListViewBase or ItemsRepeater) && isSemantic)
		{
			return;
		}

		// ComboBox dropdown items are realized as role="option" by the listbox region
		// (TryRealizeComboBoxItem above); don't recurse, or each item's content TextBlock would
		// also emit as a standalone <p> alongside its option.
		if (child is ComboBoxItem)
		{
			return;
		}

		// Always recurse into children
		foreach (var childChild in child.GetChildren())
		{
			BuildSemanticsTreeRecursive(effectiveParent, childChild, depth + 1);
		}
	}

	private bool AddSemanticElement(IntPtr parentHandle, UIElement child, int? index)
	{
		// Use UIElement.GetTransform for position calculation — this accounts for
		// RenderTransform, Scale, etc. and matches the update path in OnSizeOrOffsetChanged.
		// Falling back to manual offset accumulation only when the semantic parent element
		// is not found (e.g., root element).
		float x, y, width, height;
		var localRect = new Windows.Foundation.Rect(0, 0, child.Visual.Size.X, child.Visual.Size.Y);
		var semanticParentElement = FindUIElementByHandle(child, parentHandle);
		if (semanticParentElement is not null)
		{
			var transform = UIElement.GetTransform(from: child, to: semanticParentElement);
			var transformedRect = transform.Transform(localRect);
			x = (float)transformedRect.X;
			y = (float)transformedRect.Y;
			width = (float)transformedRect.Width;
			height = (float)transformedRect.Height;
		}
		else
		{
			var totalOffset = GetOffsetRelativeToSemanticParent(child, parentHandle);
			x = totalOffset.X;
			y = totalOffset.Y;
			width = child.Visual.Size.X;
			height = child.Visual.Size.Y;
		}

		var automationPeer = child.GetOrCreateAutomationPeer();
		var roleOverride = NormalizeRoleOverrideForHost(child, automationPeer, AutomationProperties.GetRoleOverride(child));

		// Try to create type-specific semantic elements (button, slider, checkbox, etc.)
		// This provides better keyboard support and screen reader compatibility
		if (automationPeer is not null)
		{
			var elementType = AriaMapper.GetSemanticElementType(automationPeer, child);
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] AddSemanticElement: factory dispatch — control={child.GetType().Name} handle={child.Visual.Handle} elementType={elementType} parent={parentHandle}");
			}

			var created = SemanticElementFactory.CreateElement(
				automationPeer,
				child.Visual.Handle,
				parentHandle,
				index,
				x,
				y,
				width,
				height,
				child,
				IsAccessibilityFocusable(child, child.IsFocusable));

			if (created)
			{
				if (roleOverride is not null)
				{
					ApplyRoleOverride(child, automationPeer, roleOverride);
				}
				return true;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] AddSemanticElement: factory returned false for {child.GetType().Name} elementType={elementType} — falling through to generic path");
			}
		}

		// The accessible name (aria-label) must come ONLY from the resolved name (ResolveLabel),
		// never the raw GetName() or a descendant-text dump. It is also the gate for landmark/region
		// emission (FR-014): an unlabeled landmark/region MUST NOT be emitted.
		var requiresAuthoredName =
			child is ScrollViewer ||
			AutomationProperties.GetLandmarkType(child) != AutomationLandmarkType.None;
		var resolvedName = automationPeer is not null
			? requiresAuthoredName
				? AriaMapper.ResolveAuthoredLabel(automationPeer)
				: AriaMapper.ResolveLabel(automationPeer)
			: AutomationProperties.GetName(child);
		var hasAccessibleName = !string.IsNullOrEmpty(resolvedName);

		// Build the intrinsic role first. An explicit override is applied transactionally after
		// creation so clearing it can restore the native/default role and state.
		var role = ResolveDefaultSemanticRole(child, automationPeer);

		// FR-013/FR-014: a ScrollViewer (control type Pane → "region") only earns role=region when it
		// is actually scrollable AND named. A non-scrollable or unnamed ScrollViewer must NOT become an
		// (unlabeled) landmark — drop the region role so it renders as a plain structural <div>.
		if (string.Equals(role, "region", StringComparison.Ordinal) &&
			!AriaMapper.QualifiesAsNamedScrollRegion(automationPeer, child))
		{
			role = null;
		}

		// Containers with AutomationProperties.Name but no peer/role act as accessible groups.
		// This matches WinUI3 where named containers create UIA Group elements.
		if (string.IsNullOrEmpty(role))
		{
			if (hasAccessibleName)
			{
				role = "group";
			}
		}

		// Elements with a LandmarkType get the corresponding ARIA landmark role.
		// This overrides any other role since landmarks are a higher-level semantic.
		// FR-014: region/form landmarks are only exposed when named (an unnamed region/form is not a
		// landmark; axe "region must have a name"). main/navigation/search are top-level landmarks
		// identified by role alone and keep their role even when unnamed.
		var landmarkType = AutomationProperties.GetLandmarkType(child);
		if (landmarkType != AutomationLandmarkType.None)
		{
			var landmarkRole = AriaMapper.GetLandmarkRole(landmarkType);
			if (!string.IsNullOrEmpty(landmarkRole)
				&& (landmarkRole is not ("region" or "form") || hasAccessibleName))
			{
				role = landmarkRole;
			}
		}

		// The accessible name (aria-label) comes ONLY from the resolved name (ResolveLabel).
		// AutomationId is surfaced separately as the xamlautomationid attribute and
		// must never leak into aria-label.
		var name = role == "generic" ? null : resolvedName;
		var xamlAutomationId = AutomationProperties.GetAutomationId(child);
		var horizontallyScrollable = false;
		var verticallyScrollable = false;

		if (automationPeer is IScrollProvider scrollProvider)
		{
			horizontallyScrollable = scrollProvider.HorizontallyScrollable;
			verticallyScrollable = scrollProvider.VerticallyScrollable;
		}
		else if (child.IsScrollPort)
		{
			// Fallback for scroll ports without a ScrollViewerAutomationPeer
			horizontallyScrollable = true;
			verticallyScrollable = true;
		}

		string? ariaChecked = null;
		if (child is CheckBox checkBox)
		{
			ariaChecked = ConvertToAriaChecked(checkBox.IsChecked);
		}
		else if (child is RadioButton radioButton)
		{
			ariaChecked = ConvertToAriaChecked(radioButton.IsChecked);
		}
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"[A11y] AddSemanticElement: generic path — control={child.GetType().Name} handle={child.Visual.Handle} role='{role}' nameLength={name?.Length ?? 0} automationIdLength={xamlAutomationId?.Length ?? 0}");
		}

		var result = NativeMethods.AddSemanticElement(parentHandle, child.Visual.Handle, index, width, height, x, y, role ?? string.Empty, name ?? string.Empty, IsAccessibilityFocusable(child, child.IsFocusable), ariaChecked, child.Visual.IsVisible, horizontallyScrollable, verticallyScrollable, child.GetType().Name, xamlAutomationId);

		if (!result && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"[A11y] AddSemanticElement failed for {child.GetType().Name} handle={child.Visual.Handle} — parent={parentHandle} may not exist in JS DOM");
		}

		// Apply additional ARIA attributes for generic elements (landmarks, live regions, custom role descriptions)
		if (result)
		{
			var handle = child.Visual.Handle;
			if (automationPeer is not null)
			{
				var capabilities = AriaMapper.GetPatternCapabilities(automationPeer);
				var action = capabilities.CanInvoke ? "invoke"
					: capabilities.CanToggle ? "toggle"
					: capabilities.CanExpandCollapse ? "expandCollapse"
					: capabilities.CanSelect ? "selection"
					: string.Empty;
				NativeMethods.ConfigureSemanticAction(handle, action);
			}

			// aria-roledescription from the AUTHORED AutomationProperties.LocalizedLandmarkType /
			// LocalizedControlType attached properties (null when unset) — NOT the peer's
			// GetLocalized*Type(), which DEFAULTS to the role name (e.g. "button") and would restate
			// the role on every named control (an ARIA anti-pattern). FR-014: roledescription is also
			// not a name substitute, so it is gated on hasAccessibleName.
			if (hasAccessibleName && role != "generic")
			{
				var roleDescription = landmarkType != AutomationLandmarkType.None
					? AutomationProperties.GetLocalizedLandmarkType(child)
					: null;
				if (string.IsNullOrEmpty(roleDescription))
				{
					roleDescription = AutomationProperties.GetLocalizedControlType(child);
				}

				if (!string.IsNullOrEmpty(roleDescription))
				{
					NativeMethods.UpdateAriaRoleDescription(handle, roleDescription);
				}
			}

			// Live regions → aria-live attribute on the element itself
			var childLiveSetting = AutomationProperties.GetLiveSetting(child);
			if (childLiveSetting != AutomationLiveSetting.Off)
			{
				var ariaLive = childLiveSetting == AutomationLiveSetting.Assertive ? "assertive" : "polite";
				NativeMethods.UpdateAriaLive(handle, ariaLive);
			}

			if (automationPeer?.IsDialog() == true)
			{
				NativeMethods.UpdateAriaModal(handle, true);
			}

			// Generic elements that still expose ExpandCollapse / shortcut keys (e.g. Expander
			// hosted inside a fallback role, custom controls) need aria-expanded / aria-haspopup /
			// aria-keyshortcuts / accesskey applied post-hoc. Factory paths handle their own
			// creation-time wiring.
			if (automationPeer is not null)
			{
				try
				{
					if (automationPeer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider)
					{
						var expanded = expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.Expanded ||
									   expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.PartiallyExpanded;
						NativeMethods.UpdateExpandCollapseState(handle, expanded);

						// aria-haspopup from the C# value (FR-028): the popup kind follows the control
						// type, mirroring AriaMapper.GetAriaAttributes.
						var controlType = automationPeer.GetAutomationControlType();
						var hasPopup = controlType switch
						{
							AutomationControlType.ComboBox => "listbox",
							AutomationControlType.Menu or AutomationControlType.MenuItem => "menu",
							_ => null,
						};
						if (!string.IsNullOrEmpty(hasPopup))
						{
							NativeMethods.UpdateAriaHasPopup(handle, hasPopup);
						}
					}
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(LogLevel.Warning))
					{
						this.Log().Warn($"[A11y] Failed to apply ExpandCollapse metadata for handle={handle}: {ex.Message}");
					}
				}

				// aria-keyshortcuts from AcceleratorKey only; AccessKey maps to the HTML accesskey
				// attribute, never conflated into aria-keyshortcuts (FR-028).
				var acceleratorKey = automationPeer.GetAcceleratorKey();
				if (!string.IsNullOrEmpty(acceleratorKey))
				{
					NativeMethods.UpdateAriaKeyShortcuts(handle, acceleratorKey);
				}

				var accessKey = automationPeer.GetAccessKey();
				if (!string.IsNullOrEmpty(accessKey))
				{
					NativeMethods.SetAccessKey(handle, accessKey);
				}

				// aria-labelledby from AutomationProperties.LabeledBy, mirroring the factory path.
				// Only emitted when the labeller has a semantic node (no dangling IDREF — FR-019/FR-022).
				var labelledById = SemanticElementFactory.ResolveLabelledByIdRef(automationPeer);
				if (labelledById is not null)
				{
					NativeMethods.UpdateAriaLabelledBy(handle, labelledById);
				}

				ApplyGenericFallbackAttributes(child, automationPeer, handle);
			}

			// Owner-scoped attributes sourced from AutomationProperties attached properties
			// (aria-level, aria-busy, lang). Mirrors the factory path so both surface them.
			SemanticElementFactory.ApplyOwnerScopedAriaAttributes(child, handle);
			if (roleOverride is not null)
			{
				ApplyRoleOverride(child, automationPeer, roleOverride);
			}
		}

		return result;
	}

	private void TrySubscribeDataGridRow(UIElement element)
	{
		if (element is not FrameworkElement frameworkElement ||
			_dataGridRowSubscriptions.ContainsKey(element.Visual.Handle) ||
			element.GetOrCreateAutomationPeer() is not { } peer ||
			peer.GetAutomationControlType() is not AutomationControlType.DataItem ||
			AriaMapper.GetContainingDataGridPeer(peer) is not { } gridPeer)
		{
			return;
		}
		BindRealizedDataGridItemPeer(gridPeer, frameworkElement, peer);

		TypedEventHandler<FrameworkElement, DataContextChangedEventArgs> handler = (sender, _) =>
		{
			try
			{
				if (sender.GetOrCreateAutomationPeer() is { } rowPeer &&
					AriaMapper.GetContainingDataGridPeer(rowPeer) is { } gridPeer)
				{
					BindRealizedDataGridItemPeer(gridPeer, sender, rowPeer);
					QueueDataGridRowRefresh(gridPeer, sender);
				}
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"[A11y] Ignored DataGrid row recycle refresh for handle={sender.Visual.Handle}: {ex.Message}");
				}
			}
		};

		frameworkElement.DataContextChanged += handler;
		_dataGridRowSubscriptions[element.Visual.Handle] = (frameworkElement, handler);
	}

	private void BindRealizedDataGridItemPeer(AutomationPeer gridPeer, FrameworkElement row, AutomationPeer rowPeer)
	{
		if (row.DataContext is not { } item || GetDataGridItemPeerFactory(gridPeer.GetType()) is not { Factory: { } factory } peerFactory)
		{
			return;
		}

		try
		{
			if (factory.Invoke(gridPeer, [item]) is AutomationPeer itemPeer)
			{
				var handle = row.Visual.Handle;
				var hasPrevious = _dataGridRealizedItems.TryGetValue(handle, out var previous);
				_dataGridRealizedItems[handle] = (gridPeer, item);
				rowPeer.EventsSource = itemPeer;
				if (hasPrevious &&
					(!ReferenceEquals(previous.GridPeer, gridPeer) || !EqualityComparer<object>.Default.Equals(previous.Item, item)))
				{
					TryEvictDataGridItemPeer(previous.GridPeer, previous.Item);
				}
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] DataGrid realized-row peer binding failed for handle={row.Visual.Handle}: {ex.Message}");
			}
		}
	}

	[UnconditionalSuppressMessage("Trimming", "IL2075", Justification = "The Toolkit DataGrid calls GetOrCreateItemPeer internally, so its implementation remains reachable in trimmed applications.")]
	private static DataGridItemPeerFactory GetDataGridItemPeerFactory(Type peerType)
		=> _dataGridItemPeerFactories.GetValue(peerType, static type => new(type));

	private sealed class DataGridItemPeerFactory
	{
		public DataGridItemPeerFactory(Type peerType)
		{
			Factory = peerType.GetMethod(
				"GetOrCreateItemPeer",
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
				binder: null,
				types: [typeof(object)],
				modifiers: null);
			ItemPeers = peerType.GetField("_itemPeers", BindingFlags.Instance | BindingFlags.NonPublic);
			Item = Factory?.ReturnType.GetField("_item", BindingFlags.Instance | BindingFlags.NonPublic);
		}

		public MethodInfo? Factory { get; }
		public FieldInfo? ItemPeers { get; }
		public FieldInfo? Item { get; }
	}

	private void TryEvictDataGridItemPeer(AutomationPeer gridPeer, object item)
	{
		var peerFactory = GetDataGridItemPeerFactory(gridPeer.GetType());
		if (peerFactory.ItemPeers?.GetValue(gridPeer) is not IDictionary itemPeers || !itemPeers.Contains(item))
		{
			return;
		}

		foreach (var realized in _dataGridRealizedItems.Values)
		{
			if (ReferenceEquals(realized.GridPeer, gridPeer) && EqualityComparer<object>.Default.Equals(realized.Item, item))
			{
				return;
			}
		}

		try
		{
			if (itemPeers[item] is AutomationPeer itemPeer &&
				AriaMapper.GetPatternOrEventsSource(itemPeer, PatternInterface.SelectionItem) is ISelectionItemProvider { IsSelected: true })
			{
				return;
			}
		}
		catch
		{
			// Retain peers whose selection state is temporarily unavailable.
			return;
		}

		itemPeers.Remove(item);
	}

	private void TryEvictUnrealizedDataGridEventPeer(AutomationPeer peer)
	{
		if (AriaMapper.GetContainingDataGridPeer(peer) is not { } gridPeer)
		{
			return;
		}

		var peerFactory = GetDataGridItemPeerFactory(gridPeer.GetType());
		if (peerFactory.Item is { } itemField &&
			itemField.DeclaringType?.IsInstanceOfType(peer) == true &&
			itemField.GetValue(peer) is { } item)
		{
			TryEvictDataGridItemPeer(gridPeer, item);
		}
	}

	private void ReleaseDataGridRowBinding(FrameworkElement row)
	{
		if (row.GetOrCreateAutomationPeer() is { } rowPeer)
		{
			rowPeer.EventsSource = null;
		}

		if (_dataGridRealizedItems.Remove(row.Visual.Handle, out var binding))
		{
			TryEvictDataGridItemPeer(binding.GridPeer, binding.Item);
		}
	}

	private void TrySubscribeDataGridProviderSnapshot(UIElement element, AutomationPeer? peer = null)
	{
		var gridPeer = peer ?? element.GetOrCreateAutomationPeer();
		if (element is not FrameworkElement frameworkElement ||
			_dataGridLayoutSubscriptions.ContainsKey(element.Visual.Handle) ||
			gridPeer?.GetAutomationControlType() is not AutomationControlType.DataGrid)
		{
			return;
		}

		EventHandler<object> handler = (_, _) => QueueThrottledDataGridProviderFingerprintCheck(frameworkElement, gridPeer);
		frameworkElement.LayoutUpdated += handler;
		_dataGridLayoutSubscriptions[element.Visual.Handle] = (frameworkElement, handler);
		_dataGridSummarySubscriptions[element.Visual.Handle] = (frameworkElement, gridPeer);
		EnsureDataGridSummaryPolling();
		QueueDataGridProviderFingerprintCheck(frameworkElement, gridPeer);
		QueueDataGridProviderSummaryCheck(frameworkElement, gridPeer);
	}

	private void TryUnsubscribeDataGridProviderSnapshot(UIElement element)
	{
		var handle = element.Visual.Handle;
		if (_dataGridLayoutSubscriptions.Remove(handle, out var subscription))
		{
			subscription.Element.LayoutUpdated -= subscription.Handler;
		}
		_dataGridSummarySubscriptions.Remove(handle);
		if (_dataGridSummarySubscriptions.Count == 0)
		{
			ResetDataGridSummaryPolling();
		}
		_dataGridProviderFingerprints.Remove(handle);
		_dataGridLastFingerprintCheckTicks.Remove(handle);
		if (_dataGridFingerprintThrottleTimers.Remove(handle, out var timer))
		{
			timer.Dispose();
		}
		_dataGridProviderSummaryFingerprints.Remove(handle);
		_scheduledDataGridFingerprintChecks.Remove(handle);
		_scheduledDataGridSummaryChecks.Remove(handle);
	}

	private void EnsureDataGridSummaryPolling()
	{
		if (_dataGridSummaryPollTimer is not null)
		{
			return;
		}

		var generation = _dataGridSummaryPollGeneration;
		_dataGridSummaryPollTimer = new Timer(
			_ => NativeDispatcher.Main.Enqueue(() =>
			{
				if (generation != _dataGridSummaryPollGeneration || !_isAccessibilityEnabled)
				{
					return;
				}

				foreach (var (handle, subscription) in _dataGridSummarySubscriptions.ToArray())
				{
					if (HasSemanticElement(handle))
					{
						QueueDataGridProviderSummaryCheck(subscription.Owner, subscription.Peer);
					}
				}
			}),
			null,
			DataGridSummaryCheckIntervalMs,
			DataGridSummaryCheckIntervalMs);
	}

	private void ResetDataGridSummaryPolling()
	{
		_dataGridSummaryPollGeneration++;
		_dataGridSummaryPollTimer?.Dispose();
		_dataGridSummaryPollTimer = null;
		_dataGridSummarySubscriptions.Clear();
	}

	private void QueueDataGridProviderSummaryCheck(UIElement gridOwner, AutomationPeer gridPeer)
	{
		var gridHandle = gridOwner.Visual.Handle;
		if (!_scheduledDataGridSummaryChecks.Add(gridHandle))
		{
			return;
		}

		NativeDispatcher.Main.Enqueue(() =>
		{
			_scheduledDataGridSummaryChecks.Remove(gridHandle);
			if (!_isAccessibilityEnabled || !HasSemanticElement(gridHandle))
			{
				return;
			}

			try
			{
				var fingerprint = ComputeDataGridProviderSummaryFingerprint(gridPeer);
				if (!_dataGridProviderSummaryFingerprints.TryGetValue(gridHandle, out var previous) || previous != fingerprint)
				{
					_dataGridProviderSummaryFingerprints[gridHandle] = fingerprint;
					QueueDataGridRefresh(gridPeer);
				}
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"[A11y] DataGrid provider summary failed for handle={gridHandle}: {ex.Message}");
				}
			}

		});
	}

	private static int ComputeDataGridProviderSummaryFingerprint(AutomationPeer gridPeer)
	{
		var hash = new HashCode();
		if (gridPeer.GetPattern(PatternInterface.Grid) is IGridProvider gridProvider)
		{
			hash.Add(gridProvider.RowCount);
			hash.Add(gridProvider.ColumnCount);
		}
		if (gridPeer.GetPattern(PatternInterface.Selection) is ISelectionProvider selectionProvider)
		{
			hash.Add(selectionProvider.CanSelectMultiple);
		}
		if (gridPeer.GetPattern(PatternInterface.Table) is ITableProvider tableProvider)
		{
			AppendProviderSummary(tableProvider.GetColumnHeaders(), includeSort: true, ref hash);
			AppendProviderSummary(tableProvider.GetRowHeaders(), includeSort: false, ref hash);
		}
		return hash.ToHashCode();
	}

	private static void AppendProviderSummary(IRawElementProviderSimple[]? providers, bool includeSort, ref HashCode hash)
	{
		hash.Add(providers?.Length ?? 0);
		if (providers is null)
		{
			return;
		}

		foreach (var provider in providers)
		{
			var peer = provider?.AutomationPeer;
			hash.Add(peer);
			if (peer is not null)
			{
				hash.Add(peer.GetName());
				hash.Add(peer.IsEnabled());
				if (includeSort)
				{
					hash.Add(peer.GetItemStatus());
					hash.Add(peer.GetHelpText());
					hash.Add(peer.GetFullDescription());
				}
			}
		}
	}

	private void QueueDataGridProviderFingerprintCheck(UIElement gridOwner, AutomationPeer gridPeer)
	{
		var gridHandle = gridOwner.Visual.Handle;
		if (!_scheduledDataGridFingerprintChecks.Add(gridHandle))
		{
			return;
		}

		NativeDispatcher.Main.Enqueue(() =>
		{
			_scheduledDataGridFingerprintChecks.Remove(gridHandle);
			if (!_isAccessibilityEnabled || !HasSemanticElement(gridHandle))
			{
				return;
			}

			try
			{
				var fingerprint = ComputeDataGridProviderFingerprint(gridOwner);
				if (!_dataGridProviderFingerprints.TryGetValue(gridHandle, out var previous) || previous != fingerprint)
				{
					_dataGridProviderFingerprints[gridHandle] = fingerprint;
					QueueDataGridRefresh(gridPeer);
				}
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"[A11y] DataGrid provider fingerprint failed for handle={gridHandle}: {ex.Message}");
				}
			}

			QueueDataGridProviderSummaryCheck(gridOwner, gridPeer);
		});
	}

	private void QueueThrottledDataGridProviderFingerprintCheck(UIElement gridOwner, AutomationPeer gridPeer)
	{
		var gridHandle = gridOwner.Visual.Handle;
		var now = Environment.TickCount64;
		var elapsed = _dataGridLastFingerprintCheckTicks.TryGetValue(gridHandle, out var previous)
			? now - previous
			: DataGridFingerprintCheckIntervalMs;
		if (elapsed >= DataGridFingerprintCheckIntervalMs)
		{
			_dataGridLastFingerprintCheckTicks[gridHandle] = now;
			QueueDataGridProviderFingerprintCheck(gridOwner, gridPeer);
			return;
		}

		if (_dataGridFingerprintThrottleTimers.ContainsKey(gridHandle))
		{
			return;
		}

		Timer? timer = null;
		timer = new Timer(
			_ => NativeDispatcher.Main.Enqueue(() =>
			{
				if (!_dataGridFingerprintThrottleTimers.TryGetValue(gridHandle, out var current) ||
					!ReferenceEquals(current, timer))
				{
					return;
				}

				_dataGridFingerprintThrottleTimers.Remove(gridHandle);
				current.Dispose();
				if (!_isAccessibilityEnabled || !HasSemanticElement(gridHandle))
				{
					return;
				}

				_dataGridLastFingerprintCheckTicks[gridHandle] = Environment.TickCount64;
				QueueDataGridProviderFingerprintCheck(gridOwner, gridPeer);
			}),
			null,
			DataGridFingerprintCheckIntervalMs - (int)elapsed,
			Timeout.Infinite);
		_dataGridFingerprintThrottleTimers.Add(gridHandle, timer);
	}

	private static int ComputeDataGridProviderFingerprint(UIElement gridOwner)
	{
		var hash = new HashCode();
		AppendDataGridProviderFingerprint(gridOwner, ref hash);
		return hash.ToHashCode();
	}

	private static void AppendDataGridProviderFingerprint(UIElement element, ref HashCode hash)
	{
		try
		{
			if (element.GetOrCreateAutomationPeer() is { } peer)
			{
				hash.Add(element.Visual.Handle);
				hash.Add((int)AriaMapper.GetSemanticElementType(peer, element));
				hash.Add(peer.IsEnabled());
				hash.Add(peer.GetName());

				if (peer.GetPattern(PatternInterface.Grid) is IGridProvider gridProvider)
				{
					hash.Add(gridProvider.RowCount);
					hash.Add(gridProvider.ColumnCount);
				}
				if (peer.GetPattern(PatternInterface.Selection) is ISelectionProvider selectionProvider)
				{
					hash.Add(selectionProvider.CanSelectMultiple);
				}
				if (peer.GetPattern(PatternInterface.Table) is ITableProvider tableProvider)
				{
					AppendProviderHandles(tableProvider.GetColumnHeaders(), ref hash);
					AppendProviderHandles(tableProvider.GetRowHeaders(), ref hash);
				}
				if (peer.GetPattern(PatternInterface.GridItem) is IGridItemProvider gridItemProvider)
				{
					hash.Add(gridItemProvider.Row);
					hash.Add(gridItemProvider.Column);
					hash.Add(gridItemProvider.RowSpan);
					hash.Add(gridItemProvider.ColumnSpan);
				}
				if (AriaMapper.GetPatternOrEventsSource(peer, PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
				{
					hash.Add(selectionItemProvider.IsSelected);
				}
				if (peer.GetAutomationControlType() is AutomationControlType.HeaderItem)
				{
					hash.Add(peer.GetItemStatus());
					hash.Add(peer.GetHelpText());
					hash.Add(peer.GetFullDescription());
				}
			}
		}
		catch (Exception ex)
		{
			hash.Add(ex.GetType().FullName);
		}

		foreach (var child in element.GetChildren())
		{
			if (Instance._semanticParentMap.ContainsKey(child.Visual.Handle) || child.GetChildren().Count > 0)
			{
				AppendDataGridProviderFingerprint(child, ref hash);
			}
		}
	}

	private static void AppendProviderHandles(IRawElementProviderSimple[]? providers, ref HashCode hash)
	{
		hash.Add(providers?.Length ?? 0);
		if (providers is not null)
		{
			foreach (var provider in providers)
			{
				hash.Add(provider?.AutomationPeer);
			}
		}
	}

	private void TryUnsubscribeDataGridRow(UIElement element)
	{
		if (_dataGridRowSubscriptions.Remove(element.Visual.Handle, out var subscription))
		{
			subscription.Element.DataContextChanged -= subscription.Handler;
			ReleaseDataGridRowBinding(subscription.Element);
		}
	}

	private void TryQueueContainingDataGridRefresh(UIElement element)
	{
		if (element.GetOrCreateAutomationPeer() is { } peer &&
			AriaMapper.GetContainingDataGridPeer(peer) is { } gridPeer)
		{
			QueueDataGridRefresh(gridPeer);
		}
	}

	private void QueueDataGridRefresh(AutomationPeer peer)
		=> QueueDataGridRefreshCore(peer, row: null);

	private void QueueDataGridRowRefresh(AutomationPeer peer, UIElement row)
		=> QueueDataGridRefreshCore(peer, row);

	private void QueueDataGridRefreshCore(AutomationPeer peer, UIElement? row)
	{
		var gridPeer = peer.GetAutomationControlType() is AutomationControlType.DataGrid
			? peer
			: AriaMapper.GetContainingDataGridPeer(peer);
		if (gridPeer is not FrameworkElementAutomationPeer { Owner: { } gridOwner })
		{
			return;
		}

		var gridHandle = gridOwner.Visual.Handle;
		if (row is null)
		{
			_pendingFullDataGridRefreshes.Add(gridHandle);
			_pendingDataGridRowRefreshes.Remove(gridHandle);
		}
		else if (!_pendingFullDataGridRefreshes.Contains(gridHandle))
		{
			if (!_pendingDataGridRowRefreshes.TryGetValue(gridHandle, out var rows))
			{
				rows = new HashSet<UIElement>();
				_pendingDataGridRowRefreshes.Add(gridHandle, rows);
			}
			rows.Add(row);
		}

		if (!_scheduledDataGridRefreshes.Add(gridHandle))
		{
			return;
		}

		NativeDispatcher.Main.Enqueue(() =>
		{
			try
			{
				_scheduledDataGridRefreshes.Remove(gridHandle);
				var refreshFullGrid = _pendingFullDataGridRefreshes.Remove(gridHandle);
				_pendingDataGridRowRefreshes.Remove(gridHandle, out var dirtyRows);
				if (!_isAccessibilityEnabled || !HasSemanticElement(gridHandle))
				{
					return;
				}

				if (refreshFullGrid)
				{
					RefreshRealizedDataGrid(gridOwner);
				}
				else if (dirtyRows is not null)
				{
					foreach (var dirtyRow in dirtyRows)
					{
						if (_semanticParentMap.ContainsKey(dirtyRow.Visual.Handle))
						{
							RefreshRealizedDataGridDescendant(dirtyRow);
						}
					}
				}
			}
			catch (Exception ex)
			{
				_scheduledDataGridRefreshes.Remove(gridHandle);
				_pendingFullDataGridRefreshes.Remove(gridHandle);
				_pendingDataGridRowRefreshes.Remove(gridHandle);
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"[A11y] DataGrid refresh failed for handle={gridHandle}: {ex.Message}", ex);
				}
			}
		});
	}

	private void RefreshRealizedDataGrid(UIElement gridOwner)
	{
		ReconcileDataGridHeaderStructure(gridOwner);
		RefreshDataGridElement(gridOwner);
		foreach (var child in gridOwner.GetChildren())
		{
			RefreshRealizedDataGridDescendant(child);
		}
	}

	private void ReconcileDataGridHeaderStructure(UIElement gridOwner)
	{
		foreach (var child in gridOwner.GetChildren())
		{
			ReconcileDataGridHeaderDescendant(child);
		}
	}

	private void ReconcileDataGridHeaderDescendant(UIElement element)
	{
		var peer = element.GetOrCreateAutomationPeer();
		if (peer is not null)
		{
			var controlType = peer.GetAutomationControlType();
			var expectedType = AriaMapper.GetSemanticElementType(peer, element);

			if (controlType is AutomationControlType.Header)
			{
				if (expectedType is SemanticElementType.GridRow)
				{
					EnsureDataGridHeaderParent(element);
				}
				else if (AutomationProperties.GetAccessibilityView(element) == AccessibilityView.Raw &&
					_dataGridHeaderSemanticTypes.TryGetValue(element.Visual.Handle, out var currentType) &&
					currentType is SemanticElementType.GridRow)
				{
					RemoveSemanticSubtree(element);
				}
			}
			else if (controlType is AutomationControlType.HeaderItem)
			{
				var handle = element.Visual.Handle;
				var isRequiredHeader = expectedType is SemanticElementType.ColumnHeader or SemanticElementType.RowHeader;
				var visualParent = element.GetParent() as UIElement;
				var semanticParent = visualParent is null ? _rootElementHandle : FindSemanticParent(visualParent);
				var hasCurrentType = _dataGridHeaderSemanticTypes.TryGetValue(handle, out var currentType);
				var needsRecreation = isRequiredHeader &&
					(!_semanticParentMap.TryGetValue(handle, out var currentParent) ||
					 currentParent != semanticParent || !hasCurrentType || currentType != expectedType);

				if (needsRecreation)
				{
					RemoveSemanticSubtree(element);
					if (AddSemanticElement(semanticParent, element, null))
					{
						_semanticParentMap[handle] = semanticParent;
						InitializeInverseFlows(element);
						_dataGridHeaderSemanticTypes[handle] = expectedType;
						ApplyOrDeferLabelledBy(handle, peer);
						ApplyOrDeferRelationshipAttributes(handle, peer);
					}
				}
				else if (!isRequiredHeader && hasCurrentType &&
					currentType is SemanticElementType.ColumnHeader or SemanticElementType.RowHeader)
				{
					RemoveSemanticSubtree(element);
				}
			}
		}

		foreach (var child in element.GetChildren())
		{
			ReconcileDataGridHeaderDescendant(child);
		}
	}

	private void RemoveSemanticSubtree(UIElement element)
	{
		foreach (var child in element.GetChildren())
		{
			RemoveSemanticSubtree(child);
		}

		var handle = element.Visual.Handle;
		if (_semanticParentMap.Remove(handle, out var semanticParent))
		{
			RemoveSemanticElement(semanticParent, handle);
		}
		_pendingRelationships.Remove(handle);
		_pendingLabelledBy.Remove(handle);
		_labelledBySources.Remove(handle);
		_relationshipSources.Remove(handle);
		_dataGridHeaderSemanticTypes.Remove(handle);
	}

	private void TrackDataGridHeaderSemanticType(UIElement element, AutomationPeer? peer = null)
	{
		peer ??= element.GetOrCreateAutomationPeer();
		if (peer?.GetAutomationControlType() is AutomationControlType.Header or AutomationControlType.HeaderItem)
		{
			_dataGridHeaderSemanticTypes[element.Visual.Handle] = AriaMapper.GetSemanticElementType(peer, element);
		}
	}

	private void RefreshRealizedDataGridDescendant(UIElement element)
	{
		RefreshDataGridElement(element);
		foreach (var child in element.GetChildren())
		{
			RefreshRealizedDataGridDescendant(child);
		}
	}

	private void RefreshDataGridElement(UIElement element)
	{
		if (!_semanticParentMap.ContainsKey(element.Visual.Handle) ||
			element.GetOrCreateAutomationPeer() is not { } peer)
		{
			return;
		}

		try
		{
			SemanticElementFactory.RefreshGridMetadata(peer, element);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Failed to refresh DataGrid peer {peer.GetType().Name}: {ex.Message}");
			}
		}
	}

	private void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"[A11y] RemoveSemanticElement: parent={parentHandle} child={childHandle}");
		}

		RemoveFlowsFromTarget(childHandle);
		NativeMethods.RemoveSemanticElement(parentHandle, childHandle);
	}

	private static string? ConvertToAriaChecked(ToggleState isChecked)
	{
		return isChecked switch
		{
			ToggleState.On => "true",
			ToggleState.Off => "false",
			ToggleState.Indeterminate => "mixed",
			_ => null,
		};
	}

	private static string? ConvertToAriaChecked(bool? isChecked)
	{
		return isChecked switch
		{
			true => "true",
			false => "false",
			null => "mixed",
		};
	}

	private static void ApplyGenericFallbackAttributes(UIElement element, AutomationPeer peer, IntPtr handle)
	{
		var attributes = AriaMapper.GetAriaAttributes(peer);

		if (!string.IsNullOrEmpty(attributes.Description))
		{
			NativeMethods.UpdateAriaDescription(handle, attributes.Description);
		}

		if (attributes.Required)
		{
			NativeMethods.UpdateAriaRequired(handle, true);
		}

		if (attributes.Invalid)
		{
			NativeMethods.UpdateAriaInvalid(handle, true);
		}

		if (attributes.Disabled)
		{
			NativeMethods.UpdateDisabledState(handle, true);
		}

		if (attributes.Selected.HasValue)
		{
			NativeMethods.UpdateSelectionState(handle, attributes.Selected.Value);
		}

		if (attributes.PositionInSet is > 0 && attributes.SizeOfSet is > 0)
		{
			NativeMethods.UpdatePositionInSet(handle, SupportsAriaSetPosition(element, peer) ? attributes.PositionInSet.Value : 0, attributes.SizeOfSet.Value);
		}

		if (attributes.MultiSelectable == true)
		{
			NativeMethods.UpdateAriaAttribute(handle, "aria-multiselectable", "true");
		}

		ApplyGenericRangeAttributes(handle, element, attributes);
	}

	private static void ApplyGenericRangeAttributes(IntPtr handle, UIElement element, AriaAttributes attributes)
	{
		UpdateGenericAriaAttribute(handle, "aria-valuenow", attributes.ValueNow);
		UpdateGenericAriaAttribute(handle, "aria-valuemin", attributes.ValueMin);
		UpdateGenericAriaAttribute(handle, "aria-valuemax", attributes.ValueMax);
		NativeMethods.UpdateAriaAttribute(handle, "aria-valuetext", string.IsNullOrEmpty(attributes.ValueText) ? null : attributes.ValueText);

		if (element is ScrollBar scrollBar)
		{
			NativeMethods.UpdateAriaAttribute(handle, "aria-orientation", scrollBar.Orientation == Orientation.Vertical ? "vertical" : "horizontal");
		}
	}

	private static void UpdateGenericAriaAttribute(IntPtr handle, string attribute, double? value)
	{
		NativeMethods.UpdateAriaAttribute(handle, attribute, value?.ToString(CultureInfo.InvariantCulture));
	}

	private void OnAutomationNameChanged(UIElement element, string name)
	{
		Debug.Assert(IsAccessibilityEnabled);
		NativeMethods.UpdateAriaLabel(
			element.Visual.Handle,
			ElementRoleProhibitsNaming(element, element.GetOrCreateAutomationPeer()) ? string.Empty : name);
	}

	private static void UpdateColumnHeaderSortAndDescription(AutomationPeer peer, UIElement element)
	{
		var metadata = SemanticElementFactory.ResolveGridSortMetadata(peer);
		NativeMethods.UpdateColumnHeaderSort(element.Visual.Handle, metadata.Direction);
		NativeMethods.UpdateAriaDescription(element.Visual.Handle, metadata.Description ?? string.Empty);
	}

	protected override void AnnounceOnPlatform(string text, bool assertive)
	{
		if (assertive)
		{
			NativeMethods.AnnounceAssertive(text);
		}
		else
		{
			NativeMethods.AnnouncePolite(text);
		}
	}

	public override void NotifyInvalidatePeer(AutomationPeer peer)
	{
		base.NotifyInvalidatePeer(peer);
		if (TryGetPeerOwner(peer, out var element))
		{
			UpdateRoleOverride(element, AutomationProperties.GetRoleOverride(element));
		}
	}

	// WASM overrides to unpin virtualized items on focus change.
	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"NotifyAutomationEvent: eventId={eventId}, peer={peer.GetType().Name}");
		}

		if (eventId == AutomationEvents.AutomationFocusChanged)
		{
			var focusedHandle = IntPtr.Zero;
			if (TryGetPeerOwner(peer, out var focusedElement) && HasSemanticElement(focusedElement.Visual.Handle))
			{
				focusedHandle = focusedElement.Visual.Handle;
				_focusSynchronizer?.OnAutomationFocusChanged(focusedElement.Visual.Handle);
			}

			// When focus moves away from a virtualized item, release only prior pins. Keep
			// the region containing the newly focused handle pinned until the next move.
			foreach (var registration in _virtualizedRegions.Values)
			{
				var region = registration.Region;
				if (region.IsFocusPinned && region.PinnedHandle != focusedHandle)
				{
					var removedHandle = region.UnpinFocusedItem();
					if (removedHandle != IntPtr.Zero)
					{
						CleanupVirtualizedHandle(removedHandle);
						QueueRelationshipRefresh(refreshResolved: true);
					}
				}
			}
		}

		switch (eventId)
		{
			case AutomationEvents.LiveRegionChanged:
				_liveRegionManager?.HandleLiveRegionChanged(peer);
				break;

			case AutomationEvents.StructureChanged:
				if (peer.GetAutomationControlType() is AutomationControlType.DataGrid ||
					AriaMapper.GetContainingDataGridPeer(peer) is not null)
				{
					QueueDataGridRefresh(peer);
				}
				break;

			case AutomationEvents.InvokePatternOnInvoked:
				if (peer.GetAutomationControlType() is AutomationControlType.HeaderItem &&
					AriaMapper.GetContainingDataGridPeer(peer) is not null)
				{
					QueueDataGridRefresh(peer);
				}
				break;

			case AutomationEvents.SelectionItemPatternOnElementAddedToSelection:
			case AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection:
			case AutomationEvents.SelectionItemPatternOnElementSelected:
				QueueDataGridRefresh(peer);
				TryEvictUnrealizedDataGridEventPeer(peer);
				break;
			case AutomationEvents.SelectionPatternOnInvalidated:
				QueueDataGridRefresh(peer);
				break;
		}

		base.NotifyAutomationEvent(peer, eventId);
	}

	// WASM overrides the full property change routing because it has
	// platform-specific behavior (roving tabindex, activedescendant, etc.)
	// that differs from the base routing pattern.
	protected override void NotifyPropertyChangedEventCore(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		peer = peer.ResolveProviderPeer(resolveEventsSource: true);

		if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty &&
			TryGetPeerOwner(peer, out var element))
		{
			var ariaChecked = ConvertToAriaChecked((ToggleState)newValue);
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: ToggleState handle={element.Visual.Handle} element={element.GetType().Name} old={oldValue} new={newValue} ariaChecked={ariaChecked}");
			}

			// ToggleButton uses aria-pressed, ToggleSwitch uses role="switch" + aria-checked,
			// CheckBox/RadioButton use native checked property + aria-checked
			var roleOverride = NormalizeRoleOverrideForHost(element, peer, AutomationProperties.GetRoleOverride(element));
			var elementType = AriaMapper.GetSemanticElementType(peer, element);
			if (roleOverride is not null)
			{
				var primaryRole = GetPrimaryRole(roleOverride);
				var attribute = primaryRole is "checkbox" or "menuitemcheckbox" or "menuitemradio" or "option" or "radio" or "switch" or "treeitem"
					? "aria-checked"
					: primaryRole == "button"
						? "aria-pressed"
						: string.Empty;
				NativeMethods.UpdateRoleOverrideToggleState(element.Visual.Handle, attribute, ariaChecked ?? "false");
			}
			else if (elementType == SemanticElementType.ToggleButton)
			{
				NativeMethods.UpdateAriaPressed(element.Visual.Handle, ariaChecked ?? "false");
			}
			else
			{
				NativeMethods.UpdateAriaChecked(element.Visual.Handle, ariaChecked);

				// Update roving tabindex for radio buttons: the checked radio gets tabindex=0
				if (elementType == SemanticElementType.RadioButton && (ToggleState)newValue == ToggleState.On)
				{
					NativeMethods.UpdateRovingTabindex(IntPtr.Zero, element.Visual.Handle);
				}
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.AutomationIdProperty &&
			TryGetPeerOwner(peer, out element))
		{
			SemanticElementFactory.SetXamlAutomationId(
				element.Visual.Handle,
				AutomationProperties.GetAutomationId(element) ?? string.Empty);
		}
		else if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: Name handle={element.Visual.Handle} element={element.GetType().Name} oldLength={(oldValue as string)?.Length ?? 0} newLength={(newValue as string)?.Length ?? 0}");
			}
			OnAutomationNameChanged(element, (string)newValue);
			UpdateNameDependentRole(element, peer);
			if (element is TextBox textBox)
			{
				NativeMethods.UpdateTextBoxPlaceholder(element.Visual.Handle, textBox.PlaceholderText ?? string.Empty);
			}
			var roleOverride = NormalizeRoleOverrideForHost(element, peer, AutomationProperties.GetRoleOverride(element));
			var effectiveRole = roleOverride ?? ResolveDefaultSemanticRole(element, peer);
			UpdateAuthoredRoleDescription(element, GetPrimaryRole(effectiveRole), AriaMapper.ResolveLabel(peer));

			// When the accessible name changes on a live region element, trigger
			// the announcement. In WinUI3, the OS UIA framework monitors content
			// changes on live regions automatically. We replicate that here.
			var liveSetting = peer.GetLiveSetting();
			if (liveSetting != AutomationLiveSetting.Off)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] PROP CHANGE: Name on LiveRegion — triggering announcement liveSetting={liveSetting} contentLength={(newValue as string)?.Length ?? 0}");
				}
				_liveRegionManager?.HandleLiveRegionChanged(peer);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.HelpTextProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: HelpText handle={element.Visual.Handle} element={element.GetType().Name} contentLength={(newValue as string)?.Length ?? 0}");
			}
			if (AriaMapper.GetSemanticElementType(peer, element) is SemanticElementType.ColumnHeader &&
				AriaMapper.GetContainingDataGridPeer(peer) is not null)
			{
				UpdateColumnHeaderSortAndDescription(peer, element);
			}
			else
			{
				var description = peer.GetFullDescription();
				NativeMethods.UpdateAriaDescription(element.Visual.Handle, string.IsNullOrEmpty(description) ? peer.GetHelpText() : description);
			}

			if (element is TextBox textBox)
			{
				NativeMethods.UpdateTextBoxPlaceholder(element.Visual.Handle, textBox.PlaceholderText ?? string.Empty);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.FullDescriptionProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (AriaMapper.GetSemanticElementType(peer, element) is SemanticElementType.ColumnHeader &&
				AriaMapper.GetContainingDataGridPeer(peer) is not null)
			{
				UpdateColumnHeaderSortAndDescription(peer, element);
			}
			else
			{
				var description = peer.GetFullDescription();
				NativeMethods.UpdateAriaDescription(element.Visual.Handle, string.IsNullOrEmpty(description) ? peer.GetHelpText() : description);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.LandmarkTypeProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var attributes = AriaMapper.GetAriaAttributes(peer);
			var role = ResolveDefaultSemanticRole(element, peer);
			if (AutomationProperties.GetLandmarkType(element) == AutomationLandmarkType.None &&
				AutomationProperties.GetAccessibilityView(element) == AccessibilityView.Raw)
			{
				role = null;
			}

			NativeMethods.UpdateLandmarkRole(element.Visual.Handle, role ?? string.Empty);
			NativeMethods.UpdateAriaRoleDescription(element.Visual.Handle, attributes.RoleDescription ?? string.Empty);
		}
		else if (automationProperty == AutomationElementIdentifiers.LocalizedLandmarkTypeProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var roleDescription = AriaMapper.GetAriaAttributes(peer).RoleDescription;
			NativeMethods.UpdateAriaRoleDescription(element.Visual.Handle, roleDescription ?? string.Empty);
		}
		else if (automationProperty == AutomationElementIdentifiers.LocalizedControlTypeProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var roleOverride = NormalizeRoleOverrideForHost(element, peer, AutomationProperties.GetRoleOverride(element));
			var effectiveRole = roleOverride ?? ResolveDefaultSemanticRole(element, peer);
			UpdateAuthoredRoleDescription(element, GetPrimaryRole(effectiveRole), AriaMapper.ResolveLabel(peer));
		}
		else if (automationProperty == AutomationElementIdentifiers.LiveSettingProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var liveSetting = peer.GetLiveSetting();
			var ariaLive = liveSetting == AutomationLiveSetting.Off
				? string.Empty
				: liveSetting == AutomationLiveSetting.Assertive ? "assertive" : "polite";
			NativeMethods.UpdateAriaLive(element.Visual.Handle, ariaLive);
		}
		else if (automationProperty == AutomationElementIdentifiers.LevelProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeMethods.UpdateAriaLevel(element.Visual.Handle, ResolveAriaLevel(element, peer));
		}
		else if (automationProperty == AutomationElementIdentifiers.CultureProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeMethods.UpdateLang(element.Visual.Handle, SemanticElementFactory.ResolveLanguage(AutomationProperties.GetCulture(element)));
		}
		else if (automationProperty == AutomationElementIdentifiers.AcceleratorKeyProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeMethods.UpdateAriaKeyShortcuts(element.Visual.Handle, peer.GetAcceleratorKey() ?? string.Empty);
		}
		else if (automationProperty == AutomationElementIdentifiers.AccessKeyProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeMethods.SetAccessKey(element.Visual.Handle, peer.GetAccessKey() ?? string.Empty);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsDialogProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var roleOverride = NormalizeRoleOverrideForHost(element, peer, AutomationProperties.GetRoleOverride(element));
			var effectiveRole = roleOverride ?? ResolveDefaultSemanticRole(element, peer);
			NativeMethods.UpdateLandmarkRole(element.Visual.Handle, effectiveRole ?? string.Empty);
			NativeMethods.UpdateAriaModal(element.Visual.Handle, peer.IsDialog());
			UpdateAuthoredRoleDescription(element, GetPrimaryRole(effectiveRole), AriaMapper.ResolveLabel(peer));
		}
		else if (automationProperty == AutomationElementIdentifiers.ItemStatusProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (AriaMapper.GetSemanticElementType(peer, element) is SemanticElementType.ColumnHeader &&
				AriaMapper.GetContainingDataGridPeer(peer) is not null)
			{
				UpdateColumnHeaderSortAndDescription(peer, element);
			}
			else
			{
				NativeMethods.UpdateAriaBusy(element.Visual.Handle, SemanticElementFactory.IsBusyStatus(peer.GetItemStatus()));
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.IsEnabledProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (element.GetOrCreateAutomationPeer()?.GetAutomationControlType() is AutomationControlType.DataGrid &&
				peer.GetAutomationControlType() is AutomationControlType.DataItem)
			{
				QueueDataGridRefresh(peer);
				return;
			}

			var isDisabled = !(bool)newValue;
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: IsEnabled handle={element.Visual.Handle} element={element.GetType().Name} disabled={isDisabled}");
			}
			NativeMethods.UpdateDisabledState(element.Visual.Handle, isDisabled);
		}
		else if (automationProperty == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var expanded = (ExpandCollapseState)newValue == ExpandCollapseState.Expanded ||
							(ExpandCollapseState)newValue == ExpandCollapseState.PartiallyExpanded;
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: ExpandCollapse handle={element.Visual.Handle} element={element.GetType().Name} expanded={expanded}");
			}
			NativeMethods.UpdateExpandCollapseState(element.Visual.Handle, expanded);
		}
		else if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var selected = (bool)newValue;
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: IsSelected handle={element.Visual.Handle} element={element.GetType().Name} selected={selected}");
			}
			if (element.GetOrCreateAutomationPeer()?.GetAutomationControlType() is AutomationControlType.DataGrid &&
				peer.GetAutomationControlType() is AutomationControlType.DataItem)
			{
				QueueDataGridRefresh(peer);
				return;
			}
			else if (element is RadioButton)
			{
				// RadioButton is a native <input type="radio">; reflect selection as the native
				// checked state (UpdateAriaChecked sets element.checked). aria-selected is invalid on role="radio".
				NativeMethods.UpdateAriaChecked(element.Visual.Handle, selected ? "true" : "false");
			}
			else
			{
				NativeMethods.UpdateSelectionState(element.Visual.Handle, selected);
			}

			// Update roving tabindex: the newly selected item gets tabindex=0,
			// other group members get tabindex=-1 (for listbox options, radio groups, tabs)
			if (selected && AriaMapper.GetContainingDataGridPeer(peer) is null)
			{
				// Use groupHandle=0 to let TS infer the group from the element's context
				NativeMethods.UpdateRovingTabindex(IntPtr.Zero, element.Visual.Handle);

				// Update aria-activedescendant on the parent container (combobox/listbox)
				// so screen readers announce the active option without moving DOM focus.
				// A ComboBox option lives in a separate listbox subtree, so the relationship
				// must be expressed on the combobox head (which carries the matching
				// aria-controls), not on the option's automation parent.
				if (element is ComboBoxItem comboBoxItem &&
					ItemsControl.ItemsControlFromItemContainer(comboBoxItem) is ComboBox ownerComboBox)
				{
					NativeMethods.UpdateActiveDescendant(ownerComboBox.Visual.Handle, element.Visual.Handle);
				}
				else if (peer.GetParent() is FrameworkElementAutomationPeer { Owner: { } parentOwner })
				{
					NativeMethods.UpdateActiveDescendant(parentOwner.Visual.Handle, element.Visual.Handle);
				}
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.ValueProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (element is ComboBox)
			{
				// Don't overwrite aria-label with the selected value -- that destroys the
				// control's accessible name (FR-020). The selection itself is already announced
				// via aria-activedescendant -> the ComboBoxItem option, whose own text the
				// screen reader reads alongside the head's name. If we ever need to reflect
				// the selected text on the head itself (editable-combobox UX), aria-valuetext
				// is the right attribute; aria-label is not.
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] PROP CHANGE: ComboBox Value handle={element.Visual.Handle} (no aria-label update; activedescendant carries selection)");
				}
			}
			else if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
			{
				// Sync programmatic text value changes to the semantic DOM element
				// (e.g., TextBox.Text set from code-behind)
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] PROP CHANGE: Value handle={element.Visual.Handle} element={element.GetType().Name} valueLen={valueProvider.Value?.Length ?? 0}");
				}
				UpdateTextBoxValueKeepingSelection(element.Visual.Handle, valueProvider.Value, element as TextBox);
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.IsReadOnlyProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var isReadOnly = (bool)newValue;
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: IsReadOnly handle={element.Visual.Handle} element={element.GetType().Name} readOnly={isReadOnly}");
			}
			NativeMethods.UpdateTextBoxReadOnly(element.Visual.Handle, isReadOnly);
		}
		else if ((automationProperty == RangeValuePatternIdentifiers.ValueProperty ||
			automationProperty == RangeValuePatternIdentifiers.MinimumProperty ||
			automationProperty == RangeValuePatternIdentifiers.MaximumProperty) &&
			TryGetPeerOwner(peer, out element))
		{
			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValueProvider)
			{
				if (element is Slider slider)
				{
					// Recompute aria-valuetext so VoiceOver announces the updated value.
					string? valueText = null;
					var headerText = slider.Header?.ToString();
					if (!string.IsNullOrEmpty(headerText))
					{
						valueText = $"{headerText}: {rangeValueProvider.Value}";
					}

					NativeMethods.UpdateSliderValue(
						element.Visual.Handle,
						rangeValueProvider.Value,
						rangeValueProvider.Minimum,
						rangeValueProvider.Maximum,
						valueText);
				}
				else
				{
					ApplyGenericRangeAttributes(element.Visual.Handle, element, AriaMapper.GetAriaAttributes(peer));
				}
			}
		}
		else if ((automationProperty == ScrollPatternIdentifiers.HorizontalScrollPercentProperty ||
			automationProperty == ScrollPatternIdentifiers.VerticalScrollPercentProperty) &&
			TryGetPeerOwner(peer, out element) && element is ScrollViewer { Presenter: { } presenter } sv)
		{
			NativeMethods.UpdateNativeScrollOffsets(presenter.Visual.Handle, sv.HorizontalOffset, sv.VerticalOffset);
		}
		else if (automationProperty == AutomationElementIdentifiers.LabeledByProperty &&
			TryGetPeerOwner(peer, out element))
		{
			ApplyOrDeferLabelledBy(element.Visual.Handle, peer);
			OnAutomationNameChanged(element, AriaMapper.ResolveLabel(peer) ?? string.Empty);
			UpdateNameDependentRole(element, peer);
		}
		else if (automationProperty == AutomationElementIdentifiers.DescribedByProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-describedby: when DescribedBy collection changes
			ApplyOrDeferRelationshipAttributes(element.Visual.Handle, peer);
		}
		else if (automationProperty == AutomationElementIdentifiers.ControlledPeersProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-controls: when ControlledPeers collection changes
			ApplyOrDeferRelationshipAttributes(element.Visual.Handle, peer);
		}
		else if (automationProperty == AutomationElementIdentifiers.FlowsToProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-flowto: when FlowsTo collection changes
			ApplyOrDeferRelationshipAttributes(element.Visual.Handle, peer);
		}
		else if (automationProperty == AutomationElementIdentifiers.FlowsFromProperty &&
			TryGetPeerOwner(peer, out element))
		{
			RefreshFlowsFromTarget(element);
		}
		else if (automationProperty == AutomationElementIdentifiers.PositionInSetProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-posinset/aria-setsize: sync when position changes
			var positionInSet = peer.GetPositionInSet();
			var sizeOfSet = peer.GetSizeOfSet();
			NativeMethods.UpdatePositionInSet(element.Visual.Handle, SupportsAriaSetPosition(element, peer) ? positionInSet : 0, sizeOfSet);
		}
		else if (automationProperty == AutomationElementIdentifiers.SizeOfSetProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-setsize: sync when set size changes
			var positionInSet = peer.GetPositionInSet();
			var sizeOfSet = peer.GetSizeOfSet();
			NativeMethods.UpdatePositionInSet(element.Visual.Handle, SupportsAriaSetPosition(element, peer) ? positionInSet : 0, sizeOfSet);
		}
		else if (automationProperty == AutomationElementIdentifiers.HeadingLevelProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// FR-011: live-sync aria-level on HeadingLevel change. The <hN> tag is fixed at
			// creation (clamped to <h6>), but aria-level carries the true level (1-9), so a
			// runtime change to level 7-9 is reflected without re-creating the element.
			var level = ConvertHeadingLevel(newValue);
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: HeadingLevel handle={element.Visual.Handle} element={element.GetType().Name} level={level}");
			}
			UpdateHeadingLevel(element.Visual.Handle, level);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsDataValidForFormProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// FR-023: live-sync aria-invalid on IsDataValidForForm change (inverted polarity —
			// false means invalid). The attribute is removed when the field becomes valid again.
			var invalid = !(bool)newValue;
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: IsDataValidForForm handle={element.Visual.Handle} element={element.GetType().Name} invalid={invalid}");
			}
			NativeMethods.UpdateAriaInvalid(element.Visual.Handle, invalid);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsRequiredForFormProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeMethods.UpdateAriaRequired(element.Visual.Handle, (bool)newValue);
		}
	}

	public override void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		=> NotifyAutomationEvent(peer, eventId);

	// Abstract implementations for SkiaAccessibilityBase
	// WASM handles all property routing in the overridden NotifyPropertyChangedEventCore,
	// so these abstract methods are not called directly but must be implemented.
	protected override void UpdateName(nint handle, AutomationPeer peer, string? label)
		=> NativeMethods.UpdateAriaLabel(handle, label ?? string.Empty);
	protected override void UpdateToggleState(nint handle, AutomationPeer peer, ToggleState newState)
		=> NativeMethods.UpdateAriaChecked(handle, AriaMapper.ConvertToggleStateToAriaChecked(newState));
	protected override void UpdateRangeValue(nint handle, AutomationPeer peer, double value)
	{
		// Full range value updates are handled in NotifyPropertyChangedEventCore
		// (which also computes aria-valuetext). This fallback ensures correctness
		// if the base routing is ever invoked directly.
		if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
		{
			NativeMethods.UpdateSliderValue(handle, value, rangeProvider.Minimum, rangeProvider.Maximum, null);
		}
	}
	protected override void UpdateRangeBounds(nint handle, double min, double max)
		=> NativeMethods.UpdateSliderValue(handle, double.NaN, min, max, null);
	protected override void UpdateTextValue(nint handle, string? value)
		=> UpdateTextBoxValueKeepingSelection(handle, value);
	protected override void UpdateExpandCollapseState(nint handle, bool isExpanded)
		=> NativeMethods.UpdateExpandCollapseState(handle, isExpanded);
	protected override void UpdateEnabled(nint handle, bool enabled)
		=> NativeMethods.UpdateDisabledState(handle, !enabled);
	protected override void UpdateSelected(nint handle, bool selected)
		=> NativeMethods.UpdateSelectionState(handle, selected);
	protected override void UpdateHelpText(nint handle, string? helpText)
		=> NativeMethods.UpdateAriaDescription(handle, helpText ?? string.Empty);
	protected override void UpdateHeadingLevel(nint handle, int level)
		=> NativeMethods.UpdateAriaLevel(handle, level);
	protected override void UpdateLandmark(nint handle, string? landmarkRole)
	{
		if (!string.IsNullOrEmpty(landmarkRole))
		{
			NativeMethods.UpdateLandmarkRole(handle, landmarkRole);
		}
	}
	protected override void UpdateIsReadOnly(nint handle, bool isReadOnly)
		=> NativeMethods.UpdateTextBoxReadOnly(handle, isReadOnly);
	protected override void UpdateFocusable(nint handle, bool focusable)
		=> NativeMethods.UpdateIsFocusable(handle, focusable);
	protected override void UpdateIsOffscreen(nint handle, bool isOffscreen)
	{
		// When going offscreen, hide the element. When coming back onscreen,
		// OnSizeOrOffsetChanged will restore positioning and visibility.
		if (isOffscreen)
		{
			NativeMethods.HideSemanticElement(handle);
		}
	}
	protected override void SetNativeFocus(nint handle)
	{
		if (HasSemanticElement(handle))
		{
			NativeMethods.FocusSemanticElement(handle);
		}
	}
	protected override void OnNativeStructureChanged() { }

	internal void SyncTextBoxValueAndSelection(TextBox textBox)
	{
		if (!_isAccessibilityEnabled || !HasSemanticElement(textBox.Visual.Handle))
		{
			return;
		}

		UpdateTextBoxValueKeepingSelection(textBox.Visual.Handle, textBox.Text, textBox);
	}

	private static void UpdateTextBoxValueKeepingSelection(IntPtr handle, string? value, TextBox? textBox = null)
	{
		textBox ??= TryGetTextBoxForHandle(handle, out var resolvedTextBox) ? resolvedTextBox : null;
		var normalizedValue = value ?? textBox?.Text ?? string.Empty;

		if (TryGetTextSelection(textBox, normalizedValue.Length, out var selectionStart, out var selectionEnd))
		{
			NativeMethods.UpdateTextBoxValue(handle, normalizedValue, selectionStart, selectionEnd);
			return;
		}

		UpdateTextBoxValuePreservingSelection(handle, normalizedValue);
	}

	private static void UpdateTextBoxValuePreservingSelection(IntPtr handle, string value)
		=> NativeMethods.UpdateTextBoxValue(handle, value ?? string.Empty, PreserveTextSelectionSentinel, PreserveTextSelectionSentinel);

	private static bool TryGetTextBoxForHandle(IntPtr handle, [NotNullWhen(true)] out TextBox? textBox)
	{
		textBox = null;

		if (handle == IntPtr.Zero)
		{
			return false;
		}

		if (Instance.TryGetLiveSemanticOwner(handle, out var element) && element is TextBox owner)
		{
			textBox = owner;
			return true;
		}

		return false;
	}

	private static bool TryGetTextSelection(TextBox? textBox, int maxLength, out int selectionStart, out int selectionEnd)
	{
		selectionStart = PreserveTextSelectionSentinel;
		selectionEnd = PreserveTextSelectionSentinel;

		if (textBox is null)
		{
			return false;
		}

		selectionStart = Math.Max(0, Math.Min(textBox.SelectionStart, maxLength));
		selectionEnd = Math.Max(selectionStart, Math.Min(textBox.SelectionStart + textBox.SelectionLength, maxLength));
		return true;
	}

	private static partial class NativeMethods
	{
		// ===== Existing Methods =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.addRootElementToSemanticsRoot")]
		internal static partial void AddRootElementToSemanticsRoot(IntPtr rootHandle, float width, float height, float x, float y, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.clearSemanticTree")]
		internal static partial void ClearSemanticTree();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.removeFocusSentinels")]
		internal static partial void RemoveFocusSentinels();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.onAccessibilityActivationFailed")]
		internal static partial void OnAccessibilityActivationFailed();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.onAccessibilityActivationSucceeded")]
		internal static partial void OnAccessibilityActivationSucceeded();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.addSemanticElement")]
		internal static partial bool AddSemanticElement(IntPtr parentHandle, IntPtr handle, int? index, float width, float height, float x, float y, string role, string automationId, bool isFocusable, string? ariaChecked, bool isVisible, bool horizontallyScrollable, bool verticallyScrollable, string temporary, string? xamlAutomationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.configureSemanticAction")]
		internal static partial void ConfigureSemanticAction(IntPtr handle, string action);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.removeSemanticElement")]
		internal static partial void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabel")]
		internal static partial void UpdateAriaLabel(IntPtr handle, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaChecked")]
		internal static partial void UpdateAriaChecked(IntPtr handle, string? ariaChecked);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaAttribute")]
		internal static partial void UpdateAriaAttribute(IntPtr handle, string attribute, string? value);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateNativeScrollOffsets")]
		internal static partial void UpdateNativeScrollOffsets(IntPtr handle, double horizontalOffset, double verticalOffset);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateSemanticElementPositioning")]
		internal static partial void UpdateSemanticElementPositioning(IntPtr handle, float width, float height, float x, float y);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateIsFocusable")]
		internal static partial void UpdateIsFocusable(IntPtr handle, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.hideSemanticElement")]
		internal static partial void HideSemanticElement(IntPtr handle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.announcePolite")]
		internal static partial void AnnouncePolite(string text);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.announceAssertive")]
		internal static partial void AnnounceAssertive(string text);

		// ===== New Type-Specific Element Creation Methods =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createButtonElement")]
		internal static partial void CreateButtonElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool disabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createSliderElement")]
		internal static partial void CreateSliderElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, double value, double min, double max, double step, string orientation, string? valueText);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTextBoxElement")]
		internal static partial void CreateTextBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string value, bool multiline, bool password, bool readOnly, int selectionStart, int selectionEnd);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createCheckboxElement")]
		internal static partial void CreateCheckboxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? checkedState, string? label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createRadioElement")]
		internal static partial void CreateRadioElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool isChecked, string? label, string? groupName);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createComboBoxElement")]
		internal static partial void CreateComboBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool expanded, string? selectedValue);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListBoxElement")]
		internal static partial void CreateListBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool multiselect);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListItemElement")]
		internal static partial void CreateListItemElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool selected, int positionInSet, int sizeOfSet);

		// ===== New State Update Methods =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateSliderValue")]
		internal static partial void UpdateSliderValue(IntPtr handle, double value, double min, double max, string? valueText);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateTextBoxValue")]
		internal static partial void UpdateTextBoxValue(IntPtr handle, string value, int selectionStart, int selectionEnd);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateTextBoxReadOnly")]
		internal static partial void UpdateTextBoxReadOnly(IntPtr handle, bool isReadOnly);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateTextBoxPlaceholder")]
		internal static partial void UpdateTextBoxPlaceholder(IntPtr handle, string placeholder);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateExpandCollapseState")]
		internal static partial void UpdateExpandCollapseState(IntPtr handle, bool expanded);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateSelectionState")]
		internal static partial void UpdateSelectionState(IntPtr handle, bool selected);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateColumnHeaderSort")]
		internal static partial void UpdateColumnHeaderSort(IntPtr handle, string? sort);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateDisabledState")]
		internal static partial void UpdateDisabledState(IntPtr handle, bool disabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateActiveDescendant")]
		internal static partial void UpdateActiveDescendant(IntPtr containerHandle, IntPtr activeItemHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateComboBoxValue")]
		internal static partial void UpdateComboBoxValue(IntPtr handle, string selectedValue);

		// ===== VoiceOver Enhancement Methods =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescription")]
		internal static partial void UpdateAriaDescription(IntPtr handle, string description);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateLandmarkRole")]
		internal static partial void UpdateLandmarkRole(IntPtr handle, string role);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateRoleOverride")]
		internal static partial void UpdateRoleOverride(IntPtr handle, string role, bool isOverride);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateRoleOverrideToggleState")]
		internal static partial void UpdateRoleOverrideToggleState(IntPtr handle, string attribute, string state);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaRoleDescription")]
		internal static partial void UpdateAriaRoleDescription(IntPtr handle, string roleDescription);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLevel")]
		internal static partial void UpdateAriaLevel(IntPtr handle, int level);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createHeadingElement")]
		internal static partial void CreateHeadingElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, int level, string? label);

		// ===== Toggle Button / Switch Element Creation =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createToggleButtonElement")]
		internal static partial void CreateToggleButtonElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, string pressed, bool disabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createSwitchElement")]
		internal static partial void CreateSwitchElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, string isOn, bool disabled);

		// ===== Additional ARIA Attribute Updates =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updatePositionInSet")]
		internal static partial void UpdatePositionInSet(IntPtr handle, int positionInSet, int sizeOfSet);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaRequired")]
		internal static partial void UpdateAriaRequired(IntPtr handle, bool required);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaInvalid")]
		internal static partial void UpdateAriaInvalid(IntPtr handle, bool invalid);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaPressed")]
		internal static partial void UpdateAriaPressed(IntPtr handle, string pressed);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaKeyShortcuts")]
		internal static partial void UpdateAriaKeyShortcuts(IntPtr handle, string keyShortcuts);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaHasPopup")]
		internal static partial void UpdateAriaHasPopup(IntPtr handle, string hasPopup);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.setAccessKey")]
		internal static partial void SetAccessKey(IntPtr handle, string accessKey);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLive")]
		internal static partial void UpdateAriaLive(IntPtr handle, string ariaLive);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaModal")]
		internal static partial void UpdateAriaModal(IntPtr handle, bool modal);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaBusy")]
		internal static partial void UpdateAriaBusy(IntPtr handle, bool busy);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateLang")]
		internal static partial void UpdateLang(IntPtr handle, string lang);

		// ===== Relationship Attributes =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescribedBy")]
		internal static partial void UpdateAriaDescribedBy(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaControls")]
		internal static partial void UpdateAriaControls(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateRuntimeAriaControls")]
		internal static partial void UpdateRuntimeAriaControls(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaFlowTo")]
		internal static partial void UpdateAriaFlowTo(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateInverseAriaFlowTo")]
		internal static partial void UpdateInverseAriaFlowTo(IntPtr handle, string idList);

		// ===== Relationship Updates =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabelledBy")]
		internal static partial void UpdateAriaLabelledBy(IntPtr handle, string idList);

		// ===== Roving Tabindex =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateRovingTabindex")]
		internal static partial void UpdateRovingTabindex(IntPtr groupHandle, IntPtr activeHandle);

		// ===== Debug Mode =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.enableDebugMode")]
		internal static partial void EnableDebugMode(bool enabled);

		// ===== Focus Management =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.focusSemanticElement")]
		internal static partial void FocusSemanticElement(IntPtr handle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.installFocusSentinels")]
		internal static partial void InstallFocusSentinels();

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.focusDepartureSentinel")]
		internal static partial void FocusDepartureSentinel(bool isForward);
	}
}
