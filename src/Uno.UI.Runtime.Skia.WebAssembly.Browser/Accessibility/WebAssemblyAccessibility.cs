#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Numerics;
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
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.Helpers;

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
	internal ModalFocusScope? ActiveModalScope { get; set; }
	private readonly List<VirtualizedSemanticRegion> _virtualizedRegions = new();
	private const int PreserveTextSelectionSentinel = -1;

	/// <summary>
	/// True if the handle is a currently-realized item inside a virtualized container — it has a
	/// uno-semantics-{handle} DOM node created via VirtualizedSemanticRegion (not via the normal
	/// _semanticParentMap path), so focus/membership resolution must recognize it.
	/// </summary>
	private bool IsRealizedVirtualizedItem(IntPtr handle)
	{
		foreach (var region in _virtualizedRegions)
		{
			if (region.ContainsRealizedHandle(handle))
			{
				return true;
			}
		}

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
		if (IsRealizedVirtualizedItem(handle))
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

	/// <summary>
	/// Checks whether a given handle is present in the semantic DOM tree.
	/// </summary>
	internal bool HasSemanticElement(IntPtr handle)
	{
		if (_semanticParentMap.ContainsKey(handle))
		{
			return true;
		}

		if (IsRealizedVirtualizedItem(handle))
		{
			return true;
		}

		var rootElement = WebAssemblyWindowWrapper.Instance?.Window?.RootElement;
		return rootElement is not null && rootElement.Visual.Handle == handle;
	}

	/// <summary>
	/// Maps each child handle to its semantic parent handle.
	/// This is needed because non-semantic elements (Grid, Border, ContentPresenter, etc.)
	/// are pruned from the accessibility tree, so the visual parent is not always
	/// the semantic parent. Without this, RemoveSemanticElement fails with
	/// "parent handle not found in DOM" errors.
	/// </summary>
	private readonly Dictionary<IntPtr, IntPtr> _semanticParentMap = new();
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
	private readonly List<(IntPtr Handle, AutomationPeer Peer)> _pendingLabelledBy = new();

	/// <summary>
	/// Reentrancy depth of <see cref="OnChildAdded"/>. OnChildAdded recurses through a whole subtree
	/// synchronously, so the outermost call (depth returning to 0) is the point at which every labeller
	/// in that subtree has been registered — the moment to drain <see cref="_pendingLabelledBy"/> so a
	/// following-sibling labeller resolves order-independently on the dynamic path too.
	/// </summary>
	private int _onChildAddedDepth;

	// Debounce timer infrastructure for DOM updates (FR-012: 100ms debounce)
	private const int DebounceDelayMs = 100;
	private Timer? _debounceTimer;
	private readonly Queue<AccessibilityUpdateAction> _pendingUpdates = new();
	private readonly object _updateLock = new();

	/// <summary>
	/// Represents an action to perform during a debounced DOM update.
	/// </summary>
	private abstract class AccessibilityUpdateAction
	{
		public abstract void Execute();
	}

	/// <summary>
	/// Update action for property changes on semantic elements.
	/// </summary>
	private sealed class PropertyChangeAction : AccessibilityUpdateAction
	{
		public IntPtr Handle { get; init; }
		public string PropertyName { get; init; } = string.Empty;
		public object? OldValue { get; init; }
		public object? NewValue { get; init; }
		public Action<IntPtr, object?> UpdateAction { get; init; } = null!;

		public override void Execute()
		{
			UpdateAction(Handle, NewValue);
		}
	}

	/// <summary>
	/// Queues an update action for debounced execution.
	/// </summary>
	/// <param name="action">The action to queue.</param>
	private void QueueUpdate(AccessibilityUpdateAction action)
	{
		lock (_updateLock)
		{
			_pendingUpdates.Enqueue(action);
			ResetDebounceTimer();
		}
	}

	/// <summary>
	/// Resets the debounce timer to fire after the debounce delay.
	/// </summary>
	private void ResetDebounceTimer()
	{
		_debounceTimer?.Dispose();
		_debounceTimer = new Timer(
			_ => FlushPendingUpdates(),
			null,
			DebounceDelayMs,
			Timeout.Infinite);
	}

	/// <summary>
	/// Flushes all pending updates to the DOM.
	/// </summary>
	private void FlushPendingUpdates()
	{
		List<AccessibilityUpdateAction> actionsToExecute;

		lock (_updateLock)
		{
			if (_pendingUpdates.Count == 0)
			{
				return;
			}

			actionsToExecute = new List<AccessibilityUpdateAction>(_pendingUpdates);
			_pendingUpdates.Clear();
			_debounceTimer?.Dispose();
			_debounceTimer = null;
		}

		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Flushing {actionsToExecute.Count} pending accessibility updates");
		}

		foreach (var action in actionsToExecute)
		{
			try
			{
				action.Execute();
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error($"Error executing accessibility update: {ex.Message}", ex);
				}
			}
		}
	}

	/// <summary>
	/// Immediately flushes all pending updates without waiting for debounce.
	/// Call this when accessibility is being disabled or element is being removed.
	/// </summary>
	internal void FlushUpdatesImmediately()
	{
		lock (_updateLock)
		{
			_debounceTimer?.Dispose();
			_debounceTimer = null;
		}

		FlushPendingUpdates();
	}

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

		_onChildAddedDepth++;
		try
		{
			TrySubscribeScrollSource(child);

			// FR-032/T058: a Collapsed element (and its whole subtree) is not rendered — skip both
			// emission and recursion so its descendants do not leak into the AT tree (WinUI: Collapsed
			// is absent from the UIA tree). Equivalent to !child.Visual.IsVisible.
			if (IsPrunedAsHidden(child))
			{
				_prunedHandles.Add(child.Visual.Handle);
				return;
			}

			var isChildSemantic = IsSemanticElement(child);

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] OnChildAdded: parent={parent.GetType().Name} handle={parent.Visual.Handle} child={child.GetType().Name} handle={child.Visual.Handle} index={index?.ToString(CultureInfo.InvariantCulture) ?? "append"}");
			}

			// Detect virtualized containers for accessibility tracking
			TryRegisterVirtualizedContainer(child);
			// Detect ContentDialog for focus trapping
			TryRegisterModalDialog(child);
			// Detect ComboBox dropdowns so their options form a proper role="listbox"
			TryRegisterComboBox(child);
			TryRealizeComboBoxItem(child);

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

						// FR-019/FR-022: defer aria-labelledby resolution on the dynamic path too. The
						// inline resolution inside AddSemanticElement is order-dependent — a following-
						// sibling labeller has not registered yet when this control is added — so record
						// it and re-resolve when the outermost OnChildAdded subtree completes (below).
						// The HasSemanticElement gate in ResolveLabelledByIdRef still applies at drain
						// time, so no dangling IDREF is emitted.
						if (AutomationProperties.GetLabeledBy(child) is not null
							&& child.GetOrCreateAutomationPeer() is { } labelledPeer)
						{
							_pendingLabelledBy.Add((childHandle, labelledPeer));
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
				DrainPendingLabelledBy();
			}
		}
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (!_isAccessibilityEnabled || _isCreatingAOM)
		{
			return;
		}

		try
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] OnChildRemoved: parent={parent.GetType().Name} handle={parent.Visual.Handle} child={child.GetType().Name} handle={child.Visual.Handle}");
			}

			TryUnsubscribeScrollSource(child);
			TryUnregisterVirtualizedContainer(child);
			TryUnregisterComboBox(child);
			TryUnrealizeComboBoxItem(child);

			// Remove any children of this element first (they may be semantic even if parent isn't)
			foreach (var childChild in child.GetChildren())
			{
				OnChildRemoved(child, childChild);
			}

			// Only remove from DOM if this element was actually in the semantic tree
			var childHandle = child.Visual.Handle;
			if (_semanticParentMap.TryGetValue(childHandle, out var semanticParent))
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] OnChildRemoved: REMOVING from semantic tree child={child.GetType().Name} handle={childHandle} semanticParent={semanticParent}");
				}
				RemoveSemanticElement(semanticParent, childHandle);
				_semanticParentMap.Remove(childHandle);
				_prunedHandles.Remove(childHandle);
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] OnChildRemoved failed for {child.GetType().Name}: {ex.Message}", ex);
			}
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
		foreach (var existingRegion in _virtualizedRegions)
		{
			if (existingRegion.ContainerHandle == containerHandle)
			{
				return;
			}
		}

		if (element is ItemsRepeater repeater)
		{
			var region = new VirtualizedSemanticRegion(
				repeater.Visual.Handle,
				"listbox",
				repeater.GetOrCreateAutomationPeer()?.GetName(),
				false);
			_virtualizedRegions.Add(region);

			repeater.ElementPrepared += (s, e) =>
				EmitRealizedItem(region, repeater.Visual.Handle, e.Element, e.Index, repeater.ItemsSourceView?.Count ?? 0, "option");

			repeater.ElementClearing += (s, e) =>
			{
				var info = ItemsRepeater.GetVirtualizationInfo(e.Element);
				if (info is not null)
				{
					region.OnItemUnrealized(e.Element.Visual.Handle, info.Index);
				}
			};

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
			var isGrid = element is GridView;
			var region = new VirtualizedSemanticRegion(
				listView.Visual.Handle,
				isGrid ? "grid" : "listbox",
				listView.GetOrCreateAutomationPeer()?.GetName(),
				listView.SelectionMode == ListViewSelectionMode.Multiple ||
				listView.SelectionMode == ListViewSelectionMode.Extended);
			_virtualizedRegions.Add(region);

			var itemRole = isGrid ? "row" : "option";

			listView.ContainerContentChanging += (s, e) =>
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
					region.OnItemUnrealized(itemElement.Visual.Handle, e.ItemIndex);
				}
			};

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

		var label = itemElement.GetOrCreateAutomationPeer()?.GetName() ?? string.Empty;
		var offset = GetOffsetRelativeToSemanticParent(itemElement, containerHandle);
		region.OnItemRealized(
			itemElement.Visual.Handle,
			index,
			totalCount,
			offset.X, offset.Y,
			itemElement.Visual.Size.X, itemElement.Visual.Size.Y,
			role, label);
	}

	private void TryUnregisterVirtualizedContainer(UIElement element)
	{
		if (element is ItemsRepeater or ListViewBase)
		{
			var handle = element.Visual.Handle;
			for (int i = _virtualizedRegions.Count - 1; i >= 0; i--)
			{
				if (_virtualizedRegions[i].ContainerHandle == handle)
				{
					_virtualizedRegions[i].Dispose();
					_virtualizedRegions.RemoveAt(i);
					break;
				}
			}
		}
	}

	private void TryRegisterModalDialog(UIElement element)
	{
		if (element is ContentDialog dialog)
		{
			dialog.Opened += (s, e) =>
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

			dialog.Closed += (s, e) =>
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
					return;
				}

				if (_semanticParentMap.TryGetValue(handle, out var semanticParentHandle)
					&& containerVisual.Owner?.Target is UIElement element)
				{
					// Use the full element-to-semantic-parent transform so that
					// RenderTransform, Scale, etc. are reflected in the position.
					var semanticParentElement = FindUIElementByHandle(element, semanticParentHandle);
					var localRect = new Windows.Foundation.Rect(0, 0, visual.Size.X, visual.Size.Y);
					if (semanticParentElement is not null)
					{
						var transform = UIElement.GetTransform(from: element, to: semanticParentElement);
						var transformedRect = transform.Transform(localRect);
						NativeMethods.UpdateSemanticElementPositioning(handle, (float)transformedRect.Width, (float)transformedRect.Height, (float)transformedRect.X, (float)transformedRect.Y);
					}
					else
					{
						var transform = UIElement.GetTransform(from: element, to: null);
						var transformedRect = transform.Transform(localRect);
						NativeMethods.UpdateSemanticElementPositioning(handle, (float)transformedRect.Width, (float)transformedRect.Height, (float)transformedRect.X, (float)transformedRect.Y);
					}
				}
				else
				{
					// Root element or element not in semantic map — use full transform to root
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

				// Only the named+scrollable case is upgraded here. The unnamed / non-scrollable cases are
				// already correct from the build-time gate (a bare <div> with no role), and an empty-string
				// role would be an invalid attribute — so we never write a role in those cases.
				if (AriaMapper.QualifiesAsNamedScrollRegion(peer, scrollViewer))
				{
					NativeMethods.UpdateLandmarkRole(scrollViewer.Visual.Handle, "region");
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

			if (_enableAccessibilityRetryCount < MaxEnableAccessibilityRetries)
			{
				_enableAccessibilityRetryCount++;
				if (@this.Log().IsEnabled(LogLevel.Trace))
				{
					@this.Log().Trace($"[A11y] EnableAccessibility() will retry in {EnableAccessibilityRetryDelayMs}ms (attempt {_enableAccessibilityRetryCount}/{MaxEnableAccessibilityRetries})");
				}

				var timer = new Timer(
					_ =>
					{
						if (@this.Log().IsEnabled(LogLevel.Trace))
						{
							@this.Log().Trace($"[A11y] EnableAccessibility() retry attempt {_enableAccessibilityRetryCount}");
						}
						EnableAccessibility();
					},
					null,
					EnableAccessibilityRetryDelayMs,
					Timeout.Infinite);

				return;
			}
			else
			{
				if (@this.Log().IsEnabled(LogLevel.Error))
				{
					@this.Log().Error($"[A11y] EnableAccessibility: max retries ({MaxEnableAccessibilityRetries}) exceeded; Window still not ready.");
				}

				return;
			}
		}

		// Success! Window and RootElement are now available
		_enableAccessibilityRetryCount = 0;
		if (@this.Log().IsEnabled(LogLevel.Debug))
		{
			@this.Log().Debug($"[A11y] EnableAccessibility() SUCCESS: rootElement={rootElement.GetType().Name}, children={rootElement.GetChildren().Count}");
		}

		@this._isAccessibilityEnabled = true;
		@this._isCreatingAOM = true;
		try
		{
			@this.CreateAOM(rootElement);
		}
		finally
		{
			@this._isCreatingAOM = false;
		}
		Control.OnIsFocusableChangedCallback = @this.UpdateIsFocusable;

		// Initialize subsystems
		@this._liveRegionManager = new LiveRegionManager();
		@this._focusSynchronizer = new FocusSynchronizer(@this);
		@this._focusSynchronizer.Initialize();

		// Suppress the duplicate FocusManager.FocusNative path.
		// The FocusSynchronizer handles all focus sync via FocusManager.GotFocus
		// and performs semantic tree resolution before calling into JS.
		FocusManager.SuppressNativeFocus = true;
	}

	[JSExport]
	public static void OnScroll(IntPtr handle, double horizontalOffset, double verticalOffset)
	{
		var @this = Instance;
		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
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
		}
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

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer?.GetPattern(PatternInterface.Invoke) is IInvokeProvider invokeProvider)
			{
				invokeProvider.Invoke();
			}
		}
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

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
			{
				toggleProvider.Toggle();
			}
		}
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

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer?.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValueProvider)
			{
				rangeValueProvider.SetValue(value);
			}
		}
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

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			if (owner is TextBox textBox)
			{
				var maxLength = value?.Length ?? 0;
				selectionStart = Math.Max(0, Math.Min(selectionStart, maxLength));
				selectionEnd = Math.Max(selectionStart, Math.Min(selectionEnd, maxLength));
				textBox.SetPendingSelection(selectionStart, selectionEnd - selectionStart);
			}

			var peer = owner.GetOrCreateAutomationPeer();
			if (peer?.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
			{
				valueProvider.SetValue(value);
			}
		}
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

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer?.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider)
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
		}
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

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			var peer = owner.GetOrCreateAutomationPeer();
			if (peer?.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
			{
				selectionItemProvider.Select();
			}
		}
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

		// Route through FocusSynchronizer if available (handles IsSyncing guard)
		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
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
		}
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
		DrainPendingLabelledBy();

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
		for (var i = _pendingLabelledBy.Count - 1; i >= 0; i--)
		{
			var (labelledHandle, labelledPeer) = _pendingLabelledBy[i];
			var labelledById = SemanticElementFactory.ResolveLabelledByIdRef(labelledPeer);
			if (labelledById is not null)
			{
				NativeMethods.UpdateAriaLabelledBy(labelledHandle, labelledById);
				_pendingLabelledBy.RemoveAt(i);
			}
		}
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
		// This matches WinUI3 behavior where Raw elements are not exposed to UIA.
		var accessibilityView = AutomationProperties.GetAccessibilityView(element);
		if (accessibilityView == AccessibilityView.Raw)
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
			// Keep TextBlocks with explicit accessibility properties
			if (!string.IsNullOrEmpty(AutomationProperties.GetName(element)))
			{
				return true;
			}
			if (AutomationProperties.GetLandmarkType(element) != AutomationLandmarkType.None)
			{
				return true;
			}
			if (AutomationProperties.GetLiveSetting(element) != AutomationLiveSetting.Off)
			{
				return true;
			}
			if (AutomationProperties.GetHeadingLevel(element) != AutomationHeadingLevel.None)
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
		var node = element.GetParent() as UIElement;
		for (var depth = 0; node is not null && depth < 6; depth++, node = node.GetParent() as UIElement)
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
		var isSemantic = IsSemanticElement(child);

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
			var added = AddSemanticElement(parentHandle, child, null);
			if (added)
			{
				_semanticParentMap[handle] = parentHandle;
				effectiveParent = handle; // children go under this element
			}
			else if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] AddSemanticElement returned false for {child.GetType().Name} handle={handle}");
			}
		}

		// FR-019/FR-022: defer aria-labelledby resolution. The labeller's semantic node may not be
		// registered yet at this control's create time (a following sibling or a Header/template child
		// registers after the labelled control), so record the control and re-resolve at the end of
		// CreateAOM. The HasSemanticElement gate still applies there, so no dangling IDREF is emitted.
		if (_isCreatingAOM && _semanticParentMap.ContainsKey(handle) && AutomationProperties.GetLabeledBy(child) is not null
			&& child.GetOrCreateAutomationPeer() is { } labelledPeer)
		{
			_pendingLabelledBy.Add((handle, labelledPeer));
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
		var resolvedName = automationPeer is not null
			? AriaMapper.ResolveLabel(automationPeer)
			: AutomationProperties.GetName(child);
		var hasAccessibleName = !string.IsNullOrEmpty(resolvedName);

		// Fall back to generic semantic element for unsupported control types.
		// Prefer AriaMapper role (covers Image, Group, etc.) over FindHtmlRole.
		var role = (automationPeer is not null
			? AriaMapper.GetAriaRole(automationPeer.GetAutomationControlType())
			: null)
			?? AutomationProperties.FindHtmlRole(child);

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
			var automationName = AutomationProperties.GetName(child);
			if (!string.IsNullOrEmpty(automationName))
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
		var name = resolvedName;
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
			this.Log().Trace($"[A11y] AddSemanticElement: generic path — control={child.GetType().Name} handle={child.Visual.Handle} role='{role}' name='{name}' automationId='{xamlAutomationId}'");
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

			// aria-roledescription from the AUTHORED AutomationProperties.LocalizedLandmarkType /
			// LocalizedControlType attached properties (null when unset) — NOT the peer's
			// GetLocalized*Type(), which DEFAULTS to the role name (e.g. "button") and would restate
			// the role on every named control (an ARIA anti-pattern). FR-014: roledescription is also
			// not a name substitute, so it is gated on hasAccessibleName.
			if (hasAccessibleName)
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
				catch
				{
					// Some peers throw if queried before fully initialized. Update will arrive via property change.
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
			}

			// Owner-scoped attributes sourced from AutomationProperties attached properties
			// (aria-level, aria-busy, lang). Mirrors the factory path so both surface them.
			SemanticElementFactory.ApplyOwnerScopedAriaAttributes(child, handle);
		}

		return result;
	}

	private void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"[A11y] RemoveSemanticElement: parent={parentHandle} child={childHandle}");
		}

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

	private void OnAutomationNameChanged(UIElement element, string automationId)
	{
		Debug.Assert(IsAccessibilityEnabled);
		NativeMethods.UpdateAriaLabel(element.Visual.Handle, automationId);
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

	// WASM overrides to unpin virtualized items on focus change.
	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (eventId == AutomationEvents.AutomationFocusChanged)
		{
			// When focus moves away from a virtualized item, unpin the previously-pinned
			// item so it can be recycled normally. Without this, items accumulate in the
			// semantic DOM forever once focused (memory/DOM leak).
			foreach (var region in _virtualizedRegions)
			{
				if (region.IsFocusPinned)
				{
					region.UnpinFocusedItem();
				}
			}
		}

		base.NotifyAutomationEvent(peer, eventId);
	}

	// WASM overrides the full property change routing because it has
	// platform-specific behavior (roving tabindex, activedescendant, etc.)
	// that differs from the base routing pattern.
	protected override void NotifyPropertyChangedEventCore(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
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
			var elementType = AriaMapper.GetSemanticElementType(peer, element);
			if (elementType == SemanticElementType.ToggleButton)
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
		else if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: Name handle={element.Visual.Handle} element={element.GetType().Name} old='{oldValue}' new='{newValue}'");
			}
			OnAutomationNameChanged(element, (string)newValue);

			// When the accessible name changes on a live region element, trigger
			// the announcement. In WinUI3, the OS UIA framework monitors content
			// changes on live regions automatically. We replicate that here.
			var liveSetting = peer.GetLiveSetting();
			if (liveSetting != AutomationLiveSetting.Off)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] PROP CHANGE: Name on LiveRegion — triggering announcement liveSetting={liveSetting} content='{newValue}'");
				}
				_liveRegionManager?.HandleLiveRegionChanged(peer);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.HelpTextProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] PROP CHANGE: HelpText handle={element.Visual.Handle} element={element.GetType().Name} new='{newValue}'");
			}
			NativeMethods.UpdateAriaDescription(element.Visual.Handle, (string)newValue);
		}
		else if (automationProperty == AutomationElementIdentifiers.LandmarkTypeProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Sync landmark role for VoiceOver rotor navigation
			var attributes = AriaMapper.GetAriaAttributes(peer);
			if (!string.IsNullOrEmpty(attributes.LandmarkRole))
			{
				NativeMethods.UpdateLandmarkRole(element.Visual.Handle, attributes.LandmarkRole);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.IsEnabledProperty &&
			TryGetPeerOwner(peer, out element))
		{
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
			if (element is RadioButton)
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
			if (selected)
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
				// For ComboBox, update aria-label with the selected value so
				// screen readers announce it when the ComboBox receives focus
				var selectedValue = newValue as string ?? string.Empty;
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] PROP CHANGE: ComboBox Value handle={element.Visual.Handle} selectedValue='{selectedValue}'");
				}
				NativeMethods.UpdateAriaLabel(element.Visual.Handle, selectedValue);
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
			// Sync slider value/min/max and aria-valuetext to semantic DOM element
			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValueProvider)
			{
				// Recompute aria-valuetext so VoiceOver announces the updated value
				string? valueText = null;
				if (element is Slider slider)
				{
					var headerText = slider.Header?.ToString();
					if (!string.IsNullOrEmpty(headerText))
					{
						valueText = $"{headerText}: {rangeValueProvider.Value}";
					}
				}

				NativeMethods.UpdateSliderValue(
					element.Visual.Handle,
					rangeValueProvider.Value,
					rangeValueProvider.Minimum,
					rangeValueProvider.Maximum,
					valueText);
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
			// Dynamic aria-labelledby: when LabeledBy changes after creation. Resolve the new
			// labeller → its semantic id (guarded on HasSemanticElement); clear the attribute when
			// the labeller was removed or is not semantic, so no dangling IDREF survives.
			var labelledById = SemanticElementFactory.ResolveLabelledByIdRef(peer);
			NativeMethods.UpdateAriaLabelledBy(element.Visual.Handle, labelledById ?? string.Empty);
		}
		else if (automationProperty == AutomationElementIdentifiers.DescribedByProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-describedby: when DescribedBy collection changes
			var describedByIds = SemanticElementFactory.ResolvePeerCollectionToIdList(peer.GetDescribedBy());
			if (describedByIds is not null)
			{
				NativeMethods.UpdateAriaDescribedBy(element.Visual.Handle, describedByIds);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.ControlledPeersProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-controls: when ControlledPeers collection changes
			var controlledIds = SemanticElementFactory.ResolvePeerCollectionToIdList(peer.GetControlledPeers());
			if (controlledIds is not null)
			{
				NativeMethods.UpdateAriaControls(element.Visual.Handle, controlledIds);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.FlowsToProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-flowto: when FlowsTo collection changes
			var flowsToIds = SemanticElementFactory.ResolvePeerCollectionToIdList(peer.GetFlowsTo());
			if (flowsToIds is not null)
			{
				NativeMethods.UpdateAriaFlowTo(element.Visual.Handle, flowsToIds);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.PositionInSetProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-posinset/aria-setsize: sync when position changes
			var positionInSet = peer.GetPositionInSet();
			var sizeOfSet = peer.GetSizeOfSet();
			if (positionInSet > 0 && sizeOfSet > 0)
			{
				NativeMethods.UpdatePositionInSet(element.Visual.Handle, positionInSet, sizeOfSet);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.SizeOfSetProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Dynamic aria-setsize: sync when set size changes
			var positionInSet = peer.GetPositionInSet();
			var sizeOfSet = peer.GetSizeOfSet();
			if (positionInSet > 0 && sizeOfSet > 0)
			{
				NativeMethods.UpdatePositionInSet(element.Visual.Handle, positionInSet, sizeOfSet);
			}
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
	}

	public override void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"OnAutomationEvent: eventId={eventId}, peer={peer.GetType().Name}");
		}

		switch (eventId)
		{
			case AutomationEvents.LiveRegionChanged:
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] AUTOMATION EVENT: LiveRegionChanged peer={peer.GetType().Name}");
				}
				_liveRegionManager?.HandleLiveRegionChanged(peer);
				break;

			case AutomationEvents.TextEditTextChanged:
			case AutomationEvents.TextPatternOnTextChanged:
				// Sync text value changes to the semantic DOM (handles programmatic TextBox.Text updates)
				if (TryGetPeerOwner(peer, out var textElement) &&
					peer.GetPattern(PatternInterface.Value) is IValueProvider textValueProvider)
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"[A11y] AUTOMATION EVENT: {eventId} handle={textElement.Visual.Handle} valueLen={textValueProvider.Value?.Length ?? 0}");
					}
					UpdateTextBoxValueKeepingSelection(textElement.Visual.Handle, textValueProvider.Value, textElement as TextBox);
				}
				break;

			case AutomationEvents.AutomationFocusChanged:
				// Route focus changes to the semantic DOM so the browser focus ring follows
				if (TryGetPeerOwner(peer, out var focusElement))
				{
					if (this.Log().IsEnabled(LogLevel.Trace))
					{
						this.Log().Trace($"[A11y] AUTOMATION EVENT: AutomationFocusChanged handle={focusElement.Visual.Handle} element={focusElement.GetType().Name}");
					}
					NativeMethods.FocusSemanticElement(focusElement.Visual.Handle);
				}
				break;

			case AutomationEvents.StructureChanged:
				// Structure changes (children added/removed) require the screen reader to
				// re-scan the accessible tree. The browser handles this automatically when
				// DOM nodes are added/removed, so no explicit notification is needed.
				// This is here for completeness and logging.
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] AUTOMATION EVENT: StructureChanged peer={peer.GetType().Name}");
				}
				break;

			case AutomationEvents.InvokePatternOnInvoked:
				// After a button invoke, screen readers may need to update state.
				// The property change notifications handle the actual state updates;
				// this event is logged for diagnostics.
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] AUTOMATION EVENT: InvokePatternOnInvoked peer={peer.GetType().Name}");
				}
				break;

			case AutomationEvents.SelectionItemPatternOnElementSelected:
				// Selection events trigger property change notifications which handle
				// the DOM state updates. Log for diagnostics.
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] AUTOMATION EVENT: SelectionItemPatternOnElementSelected peer={peer.GetType().Name}");
				}
				break;

			case AutomationEvents.SelectionPatternOnInvalidated:
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[A11y] AUTOMATION EVENT: SelectionPatternOnInvalidated peer={peer.GetType().Name}");
				}
				break;
		}
	}

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
		=> NativeMethods.FocusSemanticElement(handle);
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

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: TextBox owner })
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

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.addSemanticElement")]
		internal static partial bool AddSemanticElement(IntPtr parentHandle, IntPtr handle, int? index, float width, float height, float x, float y, string role, string automationId, bool isFocusable, string? ariaChecked, bool isVisible, bool horizontallyScrollable, bool verticallyScrollable, string temporary, string? xamlAutomationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.removeSemanticElement")]
		internal static partial void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabel")]
		internal static partial void UpdateAriaLabel(IntPtr handle, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaChecked")]
		internal static partial void UpdateAriaChecked(IntPtr handle, string? ariaChecked);

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

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateDisabledState")]
		internal static partial void UpdateDisabledState(IntPtr handle, bool disabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateActiveDescendant")]
		internal static partial void UpdateActiveDescendant(IntPtr containerHandle, IntPtr activeItemHandle);

		// ===== VoiceOver Enhancement Methods =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescription")]
		internal static partial void UpdateAriaDescription(IntPtr handle, string description);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateLandmarkRole")]
		internal static partial void UpdateLandmarkRole(IntPtr handle, string role);

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

		// ===== Relationship Attributes =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescribedBy")]
		internal static partial void UpdateAriaDescribedBy(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaControls")]
		internal static partial void UpdateAriaControls(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaFlowTo")]
		internal static partial void UpdateAriaFlowTo(IntPtr handle, string idList);

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
	}
}
