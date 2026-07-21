#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Manages the Win32 UIAutomation provider tree for Skia-rendered Uno applications.
/// Per top-level <see cref="Microsoft.UI.Xaml.Window"/>: creates UIA providers lazily
/// for elements that have automation peers so Narrator / other screen readers can
/// navigate the tree. The UIA tree follows the automation peer tree (which flattens
/// layout-only elements) rather than the raw visual tree.
/// </summary>
internal sealed class Win32Accessibility : SkiaAccessibilityBase
{
	private readonly nint _hwnd;
	private readonly DispatcherQueue _dispatcherQueue;
	private readonly Win32RawElementProvider _rootProvider;
	private readonly Win32SyntheticPaneProvider _outerPane;
	private readonly Win32SyntheticPaneProvider _innerPane;
	private readonly ConditionalWeakTable<UIElement, Win32RawElementProvider> _providers = new();
	private readonly ConditionalWeakTable<AutomationPeer, Win32RawElementProvider> _peerProviders = new();
	private readonly ConcurrentDictionary<Win32RawElementProvider, byte> _liveProviders = new(ReferenceEqualityComparer.Instance);
	private int _providerCleanupDepth;
	// Pending StructureChanged events, coalesced onto the dispatcher. We record the specific
	// add/remove/invalidate so the flush can emit the WinUI-faithful event type (ChildAdded on the
	// added element, ChildRemoved on the container with the removed child's runtime id) rather than
	// a blanket ChildrenInvalidated. See AutomationEventsHelper::StructureChangedEventInformation
	// (microsoft-ui-xaml). Kind.Invalidated is the coarse fallback for peer-initiated changes,
	// while ExplicitType preserves app-raised AutomationPeer events.
	private enum StructureChangeKind { Added, Removed, Invalidated }
	private readonly record struct PendingStructureChange(
		Win32RawElementProvider Container,
		Win32RawElementProvider? Child,
		StructureChangeKind Kind,
		int[]? ChildRuntimeId,
		StructureChangeType? ExplicitType = null,
		UIElement? Element = null);
	private readonly List<PendingStructureChange> _pendingStructureChanges = new();
	private readonly HashSet<Win32RawElementProvider> _pendingAddedProviders = new(ReferenceEqualityComparer.Instance);
	// Strong references to just-invalidated providers. Keeps their COM-callable
	// wrappers alive across the window between UiaDisconnectProvider and UIA
	// delivering the structure-changed notification, so an out-of-proc client
	// that still holds the proxy observes UIA_E_ELEMENTNOTAVAILABLE rather than a
	// severed-CCW 0x80070002. Drained once the structure-change flush completes.
	private readonly HashSet<Win32RawElementProvider> _disconnectedProviders = new(ReferenceEqualityComparer.Instance);
	private bool _structureChangeFlushQueued;
	private int _uiaClientsAreListeningFailureLogged;

	// Matches WinUI's AP_BULK_CHILDREN_LIMIT. Within one coalescing window, per container:
	// if total add+remove exceeds this, collapse to a single ChildrenInvalidated; otherwise a
	// per-type count that reaches this emits ChildrenBulkAdded/ChildrenBulkRemoved, and below it
	// individual ChildAdded/ChildRemoved.
	private const int BulkChildrenLimit = 20;

	internal Win32RawElementProvider? RootProvider => _rootProvider;

	/// <summary>
	/// The outer synthetic pane — represents WinAppSDK's DesktopChildSiteBridge
	/// in the UIA tree. Sits between the HWND root and the inner pane.
	/// </summary>
	internal Win32SyntheticPaneProvider OuterPane => _outerPane;

	/// <summary>
	/// The inner synthetic pane — represents WinAppSDK's content-island host.
	/// Its children resolve to the user's Xaml content (whatever the HWND
	/// root's child-walk would have returned before the pane synthesis).
	/// </summary>
	internal Win32SyntheticPaneProvider InnerPane => _innerPane;

	internal Win32Accessibility(nint hwnd, UIElement rootElement, DispatcherQueue dispatcherQueue)
	{
		_hwnd = hwnd;
		_dispatcherQueue = dispatcherQueue;

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[UIA] Win32Accessibility initialized for window 0x{hwnd:X}");
		}

		// Create root provider only; child providers are created lazily during navigation.
		var rootPeer = rootElement.GetOrCreateAutomationPeer()?.ResolveProviderPeer(resolveEventsSource: true);
		_rootProvider = new Win32RawElementProvider(rootElement, _hwnd, isRoot: true, this, rootPeer);
		_liveProviders.TryAdd(_rootProvider, 0);
		_providers.AddOrUpdate(rootElement, _rootProvider);
		if (rootPeer is not null)
		{
			_peerProviders.AddOrUpdate(rootPeer, _rootProvider);
		}

		// Synthesize two intermediate pane providers (outer + inner) so the UIA
		// tree matches WinAppSDK exactly: window → pane → pane → user content.
		// The outer pane sits directly under the HWND root; the inner pane sits
		// under the outer pane and forwards child queries to the root's normal
		// peer-tree walk (via GetFirstChildCore / GetLastChildCore).
		// Parents/children are resolved via delegates so the two panes can
		// reference each other without a construction-order cycle.
		_outerPane = new Win32SyntheticPaneProvider(
			hwnd: _hwnd,
			accessibility: this,
			parentResolver: () => _rootProvider,
			firstChildResolver: () => _innerPane,
			lastChildResolver: () => _innerPane,
			debugTag: "outer");

		_innerPane = new Win32SyntheticPaneProvider(
			hwnd: _hwnd,
			accessibility: this,
			parentResolver: () => _outerPane,
			firstChildResolver: () => _rootProvider.GetFirstChildCore(),
			lastChildResolver: () => _rootProvider.GetLastChildCore(),
			debugTag: "inner");

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info(
				$"[UIA] Root provider created: element={rootElement.GetType().Name}, " +
				$"peer={rootPeer?.GetType().Name ?? "NULL"}, " +
				$"children count={rootElement.GetChildren().Count}, " +
				$"window=0x{_hwnd:X}");
		}
	}

	public override bool IsAccessibilityEnabled => !IsDisposed && _hwnd != nint.Zero;

	public override bool ListenerExistsHelper(AutomationEvents eventId) =>
		IsAccessibilityEnabled
		&& (eventId == AutomationEvents.AutomationFocusChanged || AreUiaClientsListening());

	private bool AreUiaClientsListening()
	{
		try
		{
			return Win32UIAutomationInterop.UiaClientsAreListening();
		}
		catch (Exception error) when (
			error is DllNotFoundException
				or EntryPointNotFoundException
				or BadImageFormatException
				or TypeLoadException
				or System.Runtime.InteropServices.SEHException)
		{
			if (Interlocked.Exchange(ref _uiaClientsAreListeningFailureLogged, 1) == 0
				&& this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("[UIA] Unable to query whether UI Automation clients are listening.", error);
			}

			return false;
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Announcements — override base to dispatch at the UIA layer
	//  (base's debouncing/throttling still runs; AnnounceOnPlatform
	//  implements the actual raise.)
	// ──────────────────────────────────────────────────────────────

	protected override void AnnounceOnPlatform(string text, bool assertive)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		var processing = assertive
			? Win32UIAutomationInterop.AutomationNotificationProcessing_ImportantMostRecent
			: Win32UIAutomationInterop.AutomationNotificationProcessing_CurrentThenMostRecent;

		try
		{
			_ = Win32UIAutomationInterop.UiaRaiseNotificationEvent(
				_rootProvider,
				Win32UIAutomationInterop.AutomationNotificationKind_Other,
				processing,
				text,
				"UnoAnnouncement");
		}
		catch (Exception ex) when (
			ex is System.Runtime.InteropServices.COMException
				or DllNotFoundException
				or EntryPointNotFoundException
				or BadImageFormatException
				or TypeLoadException
				or System.Runtime.InteropServices.SEHException)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] AnnounceOnPlatform failed: {ex.Message}");
			}
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Provider management
	// ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Gets or lazily creates a UIA provider for the given element.
	/// Only creates providers for elements that have automation peers.
	/// </summary>
	internal Win32RawElementProvider? GetOrCreateProvider(UIElement element)
	{
		if (!IsAccessibilityEnabled || Volatile.Read(ref _providerCleanupDepth) > 0)
		{
			return null;
		}

		if (TryGetLiveProvider(element, out var existing))
		{
			return existing;
		}

		var peer = element.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[UIA] GetOrCreateProvider: No automation peer for {element.GetType().Name}");
			}
			return null;
		}

		var provider = GetOrCreateProviderForResolvedPeer(peer.ResolveProviderPeer(resolveEventsSource: true));
		if (provider is { IsInvalidated: false })
		{
			// PopupPanel and similar visual proxies expose a peer owned by another UIElement.
			// Keep the visual element as an alias so subtree removal can invalidate the provider.
			_providers.AddOrUpdate(element, provider);
		}

		return provider;
	}

	/// <summary>
	/// Resolves an <see cref="AutomationPeer"/> to its corresponding UIA provider.
	/// </summary>
	internal Win32RawElementProvider? GetProviderForPeer(AutomationPeer peer, bool resolveEventsSource = false)
	{
		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource);
		if (resolvedPeer.TryGetProviderOwner(out var owner)
			&& AccessibilityRouter.Resolve(owner) is Win32Accessibility accessibility
			&& !ReferenceEquals(accessibility, this))
		{
			return accessibility.GetOrCreateProviderForResolvedPeer(resolvedPeer);
		}

		return GetOrCreateProviderForResolvedPeer(resolvedPeer);
	}

	internal Win32RawElementProvider? GetProviderForRelatedElement(UIElement element)
		=> AccessibilityRouter.Resolve(element) is Win32Accessibility accessibility
			? accessibility.GetOrCreateProvider(element)
			: null;

	internal Win32RawElementProvider? GetProviderForRelatedPeer(AutomationPeer peer)
		=> GetProviderForPeer(peer);

	internal Win32RawElementProvider? GetProvider(UIElement element)
	{
		if (!IsAccessibilityEnabled || Volatile.Read(ref _providerCleanupDepth) > 0)
		{
			return null;
		}

		if (TryGetLiveProvider(element, out var provider))
		{
			return provider;
		}

		var peer = element.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			return null;
		}

		var resolvedProvider = GetOrCreateProviderForResolvedPeer(peer.ResolveProviderPeer(resolveEventsSource: true));
		if (resolvedProvider is { IsInvalidated: false })
		{
			_providers.AddOrUpdate(element, resolvedProvider);
		}

		return resolvedProvider;
	}

	private Win32RawElementProvider? GetOrCreateProviderForResolvedPeer(AutomationPeer resolvedPeer)
	{
		if (!IsAccessibilityEnabled || Volatile.Read(ref _providerCleanupDepth) > 0)
		{
			return null;
		}

		// Fast path: already have a provider keyed by this exact peer.
		if (TryGetLiveProvider(resolvedPeer, out var existingByPeer))
		{
			if (resolvedPeer.TryGetProviderOwner(out var existingOwner) && existingOwner is Popup popup)
			{
				RegisterPopupPanelAlias(popup, existingByPeer);
			}
			return existingByPeer;
		}

		if (!resolvedPeer.TryGetProviderOwner(out var element))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] GetProviderForPeer: Could not resolve owner for {resolvedPeer.GetType().Name}");
			}
			return null;
		}

		var canonicalPeer = element.GetOrCreateAutomationPeer()?.ResolveProviderPeer(resolveEventsSource: true) ?? resolvedPeer;

		if (ReferenceEquals(resolvedPeer, canonicalPeer))
		{
			// Normal path: peer is the canonical peer for its owner element.
			if (!canonicalPeer.TryGetProviderOwner(out element))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[UIA] GetProviderForPeer: Canonical owner resolution failed for {canonicalPeer.GetType().Name}");
				}
				return null;
			}

			if (TryGetLiveProvider(element, out var existingByElement)
				&& existingByElement.RepresentsPeer(canonicalPeer))
			{
				_peerProviders.AddOrUpdate(canonicalPeer, existingByElement);
				RegisterProviderOwner(element, existingByElement);
				return existingByElement;
			}

			if (TryGetLiveProvider(canonicalPeer, out existingByPeer))
			{
				RegisterProviderOwner(element, existingByPeer);
				return existingByPeer;
			}

			var provider = new Win32RawElementProvider(element, _hwnd, isRoot: false, this, canonicalPeer);
			_liveProviders.TryAdd(provider, 0);
			RegisterProviderOwner(element, provider);
			_peerProviders.AddOrUpdate(canonicalPeer, provider);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] Created provider for {provider.DescribeElement()} (peer={canonicalPeer.GetType().Name})");
			}

			return provider;
		}
		else
		{
			// Virtual peer: shares its UIElement owner with other peers (e.g.,
			// DataGridItemAutomationPeer whose Owner is the DataGrid, not the row).
			// Create a provider keyed by this specific peer. Do NOT store in
			// _providers since the element is shared with the canonical peer.
			var provider = new Win32RawElementProvider(element, _hwnd, isRoot: false, this, resolvedPeer, isVirtualPeer: true);
			_liveProviders.TryAdd(provider, 0);
			_peerProviders.AddOrUpdate(resolvedPeer, provider);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] Created virtual provider for {provider.DescribeElement()} (peer={resolvedPeer.GetType().Name})");
			}

			return provider;
		}
	}

	private void RegisterProviderOwner(UIElement owner, Win32RawElementProvider provider)
	{
		if (provider.IsInvalidated)
		{
			return;
		}

		_providers.AddOrUpdate(owner, provider);

		// PopupPanel exposes its Popup's automation peer, so UIA navigation creates the provider
		// from the Popup owner rather than from the visual panel that enters and leaves PopupRoot.
		if (owner is Popup popup)
		{
			RegisterPopupPanelAlias(popup, provider);
		}
	}

	private void RegisterPopupPanelAlias(Popup popup, Win32RawElementProvider provider)
	{
		if (!provider.IsInvalidated && popup.PopupPanel is { } popupPanel)
		{
			_providers.AddOrUpdate(popupPanel, provider);
		}
	}

	private bool TryGetLiveProvider(UIElement element, [NotNullWhen(true)] out Win32RawElementProvider? provider)
	{
		if (_providers.TryGetValue(element, out provider))
		{
			if (!provider.IsInvalidated)
			{
				return true;
			}

			_providers.Remove(element);
		}

		provider = null;
		return false;
	}

	private bool TryGetLiveProvider(AutomationPeer peer, [NotNullWhen(true)] out Win32RawElementProvider? provider)
	{
		if (_peerProviders.TryGetValue(peer, out provider))
		{
			if (!provider.IsInvalidated)
			{
				return true;
			}

			_peerProviders.Remove(peer);
		}

		provider = null;
		return false;
	}

	// ──────────────────────────────────────────────────────────────
	//  Tree management — called from router via base.Route*
	// ──────────────────────────────────────────────────────────────

	protected override void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		// A child entered the tree / the composition (PC) scene (e.g. a DataGrid cell flipped
		// Collapsed->Visible when its column scrolled into view). Faithful to WinUI: when a UIA
		// client is listening, this raises a ChildAdded event ON THE ADDED ELEMENT itself (with a
		// null runtime id) — see CUIElement::EnterPCScene -> RegisterForStructureChangedEvent(Added)
		// -> AutomationEventsHelper. A blanket ChildrenInvalidated on an ancestor (the prior
		// behaviour) is invisible to screen readers like Narrator that key off ChildAdded to
		// incorporate/announce a newly revealed element, so the revealed content was rendered but
		// never surfaced to automation.
		var ancestorProvider = FindNearestAncestorProvider(parent);

		// Drop the cached children along the path so the next navigation rebuilds from current state.
		ancestorProvider.InvalidateChildrenCache();

		// WinUI raises ChildAdded only when an element enters the PC (render) scene; a Collapsed
		// element does not (CUIElement::EnterPCScene is never reached), so skip it here — matching
		// the Collapsed skip in CollectEnteringProviders. Without this, adding a Collapsed element to
		// an active tree would materialize a provider and surface hidden content to UIA clients.
		if (child.Visibility == Visibility.Collapsed)
		{
			return;
		}

		if (!AreUiaClientsListening())
		{
			return;
		}

		if (TryCancelPendingRemoval(ancestorProvider, child))
		{
			// Removal disconnects the old provider before the batch is flushed, so the pair cannot
			// become a silent no-op. Collapse it to one refresh signal: UIA drops the stale runtime id
			// and re-queries the re-added element without observing a transient remove/add pair.
			QueueStructureChange(new PendingStructureChange(
				ancestorProvider,
				null,
				StructureChangeKind.Invalidated,
				null));
			return;
		}

		// If UIA has not materialized any provider between the changed parent and the root, there
		// is no specific accessible container to update. Keep the root-level signal coarse instead
		// of eagerly creating peers for every element added anywhere in the app.
		if (ReferenceEquals(ancestorProvider, _rootProvider))
		{
			QueueStructureChange(new PendingStructureChange(
				ancestorProvider,
				null,
				StructureChangeKind.Invalidated,
				null,
				Element: child));
			return;
		}

		// ChildAdded is associated with the added element, so we must materialize its provider.
		// Gated above on a listening client to avoid COM-wrapper churn during normal layout.
		var childProvider = GetOrCreateProvider(child);
		if (childProvider is not null)
		{
			QueueStructureChange(new PendingStructureChange(
				ancestorProvider,
				childProvider,
				StructureChangeKind.Added,
				null,
				Element: child));
			return;
		}

		// The entering element has no automation peer of its own (e.g. a WCT DataGridCell whose
		// only automation content is a flattened TextBlock descendant). Faithful to WinUI's
		// EnterPCSceneRecursive, walk into the now-visible subtree and raise ChildAdded for each
		// shallowest peer-bearing descendant — otherwise the revealed text (the cell value) would
		// be announced by no event at all. Fall back to the coarse signal only if none is found.
		var entering = new List<Win32RawElementProvider>();
		CollectEnteringProviders(child, entering);
		if (entering.Count > 0)
		{
			foreach (var provider in entering)
			{
				QueueStructureChange(new PendingStructureChange(
					ancestorProvider,
					provider,
					StructureChangeKind.Added,
					null,
					Element: child));
			}
		}
		else
		{
			QueueStructureChange(new PendingStructureChange(
				ancestorProvider,
				null,
				StructureChangeKind.Invalidated,
				null,
				Element: child));
		}
	}

	private bool TryCancelPendingRemoval(Win32RawElementProvider container, UIElement child)
	{
		for (var i = _pendingStructureChanges.Count - 1; i >= 0; i--)
		{
			var pending = _pendingStructureChanges[i];
			if (pending.ExplicitType is null
				&& ReferenceEquals(pending.Container, container)
				&& ReferenceEquals(pending.Element, child)
				&& pending.Kind is StructureChangeKind.Removed or StructureChangeKind.Invalidated)
			{
				_pendingStructureChanges.RemoveAt(i);
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Collects the shallowest peer-bearing descendant providers of an element entering the scene,
	/// traversing through peer-less (flattened) elements and skipping Collapsed branches. Mirrors the
	/// flattening of <see cref="Win32RawElementProvider"/>'s child walk / WinUI EnterPCSceneRecursive.
	/// </summary>
	private void CollectEnteringProviders(UIElement element, List<Win32RawElementProvider> result)
	{
		TraverseDescendants(
			element,
			static current => current.GetChildren(),
			child =>
			{
				if (child.Visibility == Visibility.Collapsed)
				{
					return false;
				}

				if (GetOrCreateProvider(child) is { } provider)
				{
					result.Add(provider);
					return false;
				}

				return true;
			});
	}

	private static void TraverseDescendants(
		UIElement root,
		Func<UIElement, IEnumerable<UIElement>> getChildren,
		Func<UIElement, bool> shouldDescend)
	{
		var pending = new Stack<UIElement>();
		var visited = new HashSet<UIElement>(ReferenceEqualityComparer.Instance) { root };
		var children = new List<UIElement>();

		PushChildren(root);

		while (pending.Count > 0)
		{
			var child = pending.Pop();
			if (!visited.Add(child))
			{
				continue;
			}

			if (shouldDescend(child))
			{
				PushChildren(child);
			}
		}

		void PushChildren(UIElement parent)
		{
			children.Clear();
			children.AddRange(getChildren(parent));

			// Reverse the push order so the LIFO traversal preserves visual-tree order.
			for (var i = children.Count - 1; i >= 0; i--)
			{
				pending.Push(children[i]);
			}
		}
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		// Capture the removed child's runtime id BEFORE cleanup — ChildRemoved carries it so a
		// client can drop exactly that node. Only an already-existing provider is meaningful here.
		var listening = AreUiaClientsListening();
		var existingProvider = listening ? TryGetExistingProviderForElement(child) : null;
		int[]? childRuntimeId = existingProvider?.GetRuntimeId();

		// If this child's ChildAdded is still queued (add + remove within one coalescing window), the
		// pair cancels out — WinUI's AutomationEventsHelper drops Added/Removed pairs for the same
		// (child, container) key within a batch. CleanupProviders drops the pending Added below;
		// suppress the matching Removed so a client never sees a remove for a node it was never told
		// was added.
		var cancelsPendingAdd = existingProvider is not null &&
			_pendingAddedProviders.Contains(existingProvider);

		// Clean up cached providers for the removed subtree.
		CleanupProviders(child);

		// Faithful to WinUI: a child leaving the PC scene (e.g. a cell collapsing as its column
		// scrolls off) raises ChildRemoved ON THE CONTAINER with the removed child's runtime id
		// (CUIElement::LeavePCScene -> RegisterForStructureChangedEvent(Removed)).
		var ancestorProvider = FindNearestAncestorProvider(parent);
		ancestorProvider.InvalidateChildrenCache();

		if (!listening)
		{
			if (_disconnectedProviders.Count > 0)
			{
				// No structure event will queue the deferred tombstone release.
				EnsureStructureChangeFlushQueued();
			}
			return;
		}

		if (cancelsPendingAdd)
		{
			return;
		}

		QueueStructureChange(childRuntimeId is not null
			? new PendingStructureChange(
				ancestorProvider,
				null,
				StructureChangeKind.Removed,
				childRuntimeId,
				Element: child)
			: new PendingStructureChange(
				ancestorProvider,
				null,
				StructureChangeKind.Invalidated,
				null,
				Element: child));
	}

	/// <summary>
	/// Resolves an element's already-created provider (element- or peer-keyed) without creating one.
	/// </summary>
	private Win32RawElementProvider? TryGetExistingProviderForElement(UIElement element)
	{
		if (TryGetLiveProvider(element, out var provider))
		{
			return provider;
		}

		var peer = element.CachedAutomationPeer;
		return peer is not null ? FindExistingProviderForPeer(peer, resolveEventsSource: true) : null;
	}

	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		// UIA pulls BoundingRectangle on demand, so we only need to notify
		// clients that the property has changed so they re-query it.
		if (visual is ContainerVisual containerVisual
			&& containerVisual.Owner?.Target is UIElement owner
			&& TryGetLiveProvider(owner, out var provider))
		{
			try
			{
				_ = Win32UIAutomationInterop.UiaRaiseAutomationPropertyChangedEvent(
					provider,
					Win32UIAutomationInterop.UIA_BoundingRectanglePropertyId,
					null,
					null);
			}
			catch (Exception ex) when (
				ex is System.Runtime.InteropServices.COMException
					or DllNotFoundException
					or EntryPointNotFoundException
					or BadImageFormatException
					or TypeLoadException
					or System.Runtime.InteropServices.SEHException)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Failed to raise BoundingRectangle changed event: {ex.Message}");
				}
			}
		}
	}

	private void CleanupProviders(UIElement element)
	{
		// Use an explicit stack instead of recursion to prevent StackOverflow
		// on deep visual trees when subtrees are removed.
		var stack = new Stack<UIElement>();
		stack.Push(element);
		Interlocked.Increment(ref _providerCleanupDepth);

		try
		{
			while (stack.Count > 0)
			{
				var current = stack.Pop();
				var provider = TryGetExistingProviderForElement(current);

				// Some visual elements expose a peer owned by another element. PopupPanel, for example,
				// returns its Popup's peer, so the provider is keyed by Popup rather than PopupPanel.
				if (provider is not null)
				{
					// Invalidate (mirrors WinUI's CUIAWrapper::Invalidate): clears the
					// cached children, cascades the disconnect to virtual children —
					// e.g. WCT DataGrid rows/cells not reachable via the element table —
					// disconnects from UIA, and updates the lookup tables + tombstone
					// via OnProviderInvalidated. Every subsequent call on the provider
					// then fails with UIA_E_ELEMENTNOTAVAILABLE instead of the CCW being
					// GC'd out from under a client's proxy (0x80070002).
					provider.Invalidate();
				}

				foreach (var child in current.GetChildren())
				{
					stack.Push(child);
				}
			}
		}
		finally
		{
			Interlocked.Decrement(ref _providerCleanupDepth);
		}
	}

	/// <summary>
	/// Bookkeeping invoked by <see cref="Win32RawElementProvider.Invalidate"/>:
	/// drops the provider from the lookup tables (so a re-added element is issued
	/// a fresh provider) and roots it in the tombstone set until the pending
	/// structure-change flush completes, guaranteeing the COM-callable wrapper
	/// outlives UIA's processing of the disconnect.
	/// </summary>
	internal void OnProviderInvalidated(Win32RawElementProvider provider)
	{
		_liveProviders.TryRemove(provider, out _);
		_pendingAddedProviders.Remove(provider);

		if (provider.RepresentedPeer is { } representedPeer
			&& _peerProviders.TryGetValue(representedPeer, out var byPeer)
			&& ReferenceEquals(byPeer, provider))
		{
			_peerProviders.Remove(representedPeer);
		}

		if (provider.Owner is { } owner
			&& _providers.TryGetValue(owner, out var byElement)
			&& ReferenceEquals(byElement, provider))
		{
			_providers.Remove(owner);
		}

		// Additional aliases are removed lazily when looked up. ConditionalWeakTable entries do not
		// keep their keys alive, and every lookup rejects an invalidated provider before reuse.
		_disconnectedProviders.Add(provider);
	}

	private Win32RawElementProvider FindNearestAncestorProvider(UIElement element)
	{
		UIElement? current = element;
		while (current is not null)
		{
			if (TryGetLiveProvider(current, out var provider))
			{
				return provider;
			}
			current = VisualTreeHelper.GetParent(current) as UIElement;
		}
		return _rootProvider;
	}

	/// <summary>
	/// Coarse StructureChanged (ChildrenInvalidated) for peer-initiated changes where we don't know
	/// the specific added/removed child (e.g. a peer raising AutomationEvents.StructureChanged).
	/// </summary>
	private void RaiseStructureChanged(Win32RawElementProvider provider)
	{
		// Invalidate the children cache (cascading) so the next navigation rebuilds the list.
		provider.InvalidateChildrenCache();
		QueueStructureChange(new PendingStructureChange(provider, null, StructureChangeKind.Invalidated, null));
	}

	/// <summary>
	/// Records a pending StructureChanged and schedules a coalesced flush on the dispatcher.
	/// Deferring rather than raising synchronously avoids UIA re-entering GetChildren while a peer
	/// is still computing its children (WCT's DataGridItemAutomationPeer.GetChildrenCore calls
	/// OwningRowPeer.InvalidatePeer() from inside that very call).
	/// </summary>
	private void QueueStructureChange(PendingStructureChange change)
	{
		_pendingStructureChanges.Add(change);
		if (change.Kind == StructureChangeKind.Added && change.Child is not null)
		{
			_pendingAddedProviders.Add(change.Child);
		}
		EnsureStructureChangeFlushQueued();
	}

	private void EnsureStructureChangeFlushQueued()
	{
		if (_structureChangeFlushQueued)
		{
			return;
		}

		_structureChangeFlushQueued = true;
		if (!_dispatcherQueue.TryEnqueue(() =>
		{
			_structureChangeFlushQueued = false;

			// Short-circuit the flush if the window was closed while this callback was queued.
			if (IsDisposed)
			{
				_pendingStructureChanges.Clear();
				_pendingAddedProviders.Clear();
				_disconnectedProviders.Clear();
				return;
			}

			try
			{
				FlushStructureChanges();
			}
			finally
			{
				_disconnectedProviders.Clear();
			}
		}))
		{
			_structureChangeFlushQueued = false;
			_pendingStructureChanges.Clear();
			_pendingAddedProviders.Clear();
			_disconnectedProviders.Clear();
		}
	}

	/// <summary>
	/// Emits the queued StructureChanged events, grouped per container and applying WinUI's bulk
	/// thresholding (AutomationEventsHelper::StructureChangedEventInformation::RaiseStructureChangedEvent):
	/// up to <see cref="BulkChildrenLimit"/> changes per container emit individual ChildAdded (on the
	/// added element) / ChildRemoved (on the container, carrying the child runtime id); beyond that
	/// they collapse to ChildrenBulkAdded / ChildrenBulkRemoved / ChildrenInvalidated on the container.
	/// </summary>
	private void FlushStructureChanges()
	{
		var groups = new Dictionary<Win32RawElementProvider, (List<Win32RawElementProvider> Added, List<int[]> Removed, bool Invalidated)>(ReferenceEqualityComparer.Instance);
		List<(Win32RawElementProvider Provider, StructureChangeType Type, int[]? RuntimeId)>? explicitChanges = null;

		foreach (var change in _pendingStructureChanges)
		{
			if (change.Container.IsInvalidated)
			{
				continue;
			}

			if (change.ExplicitType is { } explicitType)
			{
				(explicitChanges ??= new()).Add((change.Container, explicitType, change.ChildRuntimeId));
				continue;
			}

			if (!groups.TryGetValue(change.Container, out var group))
			{
				group = (new List<Win32RawElementProvider>(), new List<int[]>(), false);
			}

			switch (change.Kind)
			{
				case StructureChangeKind.Added when change.Child is { IsInvalidated: false }:
					group.Added.Add(change.Child);
					break;
				case StructureChangeKind.Added:
					break;
				case StructureChangeKind.Removed when change.ChildRuntimeId is not null:
					group.Removed.Add(change.ChildRuntimeId);
					break;
				default:
					group.Invalidated = true;
					break;
			}

			groups[change.Container] = group;
		}

		_pendingStructureChanges.Clear();
		_pendingAddedProviders.Clear();

		if (explicitChanges is not null)
		{
			foreach (var (provider, type, runtimeId) in explicitChanges)
			{
				RaiseStructureChangedCore(provider, type, runtimeId);
			}
		}

		foreach (var (container, group) in groups)
		{
			var total = group.Added.Count + group.Removed.Count;

			// Too many changes (or an explicit coarse request) -> a single ChildrenInvalidated.
			if (group.Invalidated || total > BulkChildrenLimit)
			{
				RaiseStructureChangedCore(container, StructureChangeType.ChildrenInvalidated, null);
				continue;
			}

			if (group.Added.Count >= BulkChildrenLimit) // deliberate >=: exactly BulkChildrenLimit → BulkAdded (total > limit → Invalidated above)
			{
				RaiseStructureChangedCore(container, StructureChangeType.ChildrenBulkAdded, null);
			}
			else
			{
				// ChildAdded is raised on the added element itself, with a null runtime id.
				foreach (var child in group.Added)
				{
					RaiseStructureChangedCore(child, StructureChangeType.ChildAdded, null);
				}
			}

			if (group.Removed.Count >= BulkChildrenLimit) // deliberate >=: exactly BulkChildrenLimit → BulkRemoved (total > limit → Invalidated above)
			{
				RaiseStructureChangedCore(container, StructureChangeType.ChildrenBulkRemoved, null);
			}
			else
			{
				// ChildRemoved is raised on the container, carrying the removed child's runtime id.
				foreach (var removedRuntimeId in group.Removed)
				{
					RaiseStructureChangedCore(container, StructureChangeType.ChildRemoved, removedRuntimeId);
				}
			}
		}
	}

	private void RaiseStructureChangedCore(Win32RawElementProvider provider, StructureChangeType type, int[]? runtimeId)
	{
		try
		{
			_ = Win32UIAutomationInterop.UiaRaiseStructureChangedEvent(provider, type, runtimeId);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"RaiseStructureChanged ({type}) failed: {ex.Message}");
			}
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Helpers
	// ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Resolves a peer's events source and returns its peer-keyed provider without
	/// creating one or falling back to an owner element's provider.
	/// </summary>
	internal Win32RawElementProvider? TryGetExistingProviderResolvingEventsSource(AutomationPeer peer)
	{
		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		return TryGetLiveProvider(resolvedPeer, out var provider) ? provider : null;
	}

	/// <summary>
	/// Looks up an existing provider for the given peer without creating one.
	/// Used by event notification methods to avoid eagerly creating providers
	/// for elements that UIA hasn't navigated to yet — creating providers in
	/// event paths registers COM callable wrappers with UIA, which hold strong
	/// references and prevent GC of the underlying UIElements.
	/// </summary>
	private Win32RawElementProvider? FindExistingProviderForPeer(AutomationPeer peer, bool resolveEventsSource = false)
	{
		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource);

		if (TryGetLiveProvider(resolvedPeer, out var providerByPeer))
		{
			return providerByPeer;
		}

		if (resolvedPeer.TryGetProviderOwner(out var element) && TryGetLiveProvider(element, out var providerByElement))
		{
			return providerByElement;
		}

		return null;
	}

	// ──────────────────────────────────────────────────────────────
	//  Automation peer listener — UIA-style dispatch overrides
	// ──────────────────────────────────────────────────────────────

	public override void NotifyStructureChangedEvent(AutomationPeer peer, AutomationStructureChangeType structureChangeType, AutomationPeer? child)
	{
		if (!IsAccessibilityEnabled || !AreUiaClientsListening())
		{
			return;
		}

		// Structure events are meaningful only for providers already exposed to UIA. Creating one
		// here would register a COM wrapper from an event path and keep the element alive.
		var provider = FindExistingProviderForPeer(peer, resolveEventsSource: true);
		if (provider is null)
		{
			return;
		}

		provider.InvalidateChildrenCache();

		int[]? runtimeId = null;
		if (structureChangeType == AutomationStructureChangeType.ChildRemoved)
		{
			runtimeId = child is not null
				? FindExistingProviderForPeer(child, resolveEventsSource: true)?.GetRuntimeId()
				: null;
			if (runtimeId is null)
			{
				structureChangeType = AutomationStructureChangeType.ChildrenInvalidated;
			}
		}

		// WinUI's RaiseStructureChangedEventImpl intentionally ignores child for ChildAdded and
		// raises the event on the calling peer with a null runtime ID. ChildRemoved alone carries
		// the removed child's runtime ID.
		var nativeType = structureChangeType switch
		{
			AutomationStructureChangeType.ChildAdded => StructureChangeType.ChildAdded,
			AutomationStructureChangeType.ChildRemoved => StructureChangeType.ChildRemoved,
			AutomationStructureChangeType.ChildrenInvalidated => StructureChangeType.ChildrenInvalidated,
			AutomationStructureChangeType.ChildrenBulkAdded => StructureChangeType.ChildrenBulkAdded,
			AutomationStructureChangeType.ChildrenBulkRemoved => StructureChangeType.ChildrenBulkRemoved,
			AutomationStructureChangeType.ChildrenReordered => StructureChangeType.ChildrenReordered,
			_ => throw new ArgumentOutOfRangeException(nameof(structureChangeType))
		};

		QueueStructureChange(new PendingStructureChange(
			provider,
			null,
			StructureChangeKind.Invalidated,
			runtimeId,
			ExplicitType: nativeType));
	}

	public override void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		var provider = FindExistingProviderForPeer(peer, resolveEventsSource: true);
		if (provider is null)
		{
			return;
		}

		var propertyId = MapAutomationPropertyToUia(automationProperty);
		if (propertyId is null)
		{
			return;
		}

		try
		{
			_ = Win32UIAutomationInterop.UiaRaiseAutomationPropertyChangedEvent(
				provider, propertyId.Value, oldValue, newValue);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"NotifyPropertyChangedEvent failed for {automationProperty}: {ex.Message}");
			}
		}
	}

	public override void NotifyInvalidatePeer(AutomationPeer peer)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		// Faithful part: re-evaluate the peer's automatic properties and raise
		// PropertyChanged for any that changed (matches WinUI's RaiseAutomaticPropertyChanges).
		base.NotifyInvalidatePeer(peer);

		// Skia-bridge part: WinUI relies on the OS UIA layer to cache and refetch
		// children, invalidating implicitly. Our Win32 provider keeps its own
		// children cache (_cachedAutomationChildren), so drop it here — cascading
		// through the UIA child links to virtual peers (e.g. WCT DataGrid rows) that
		// the element table can't reach — so the next UIA navigation rebuilds from
		// current state. This raises NO client StructureChanged event, matching WinUI:
		// InvalidatePeer never raises StructureChanged (see CCoreServices::CallbackEventListener).
		var provider = FindExistingProviderForPeer(peer, resolveEventsSource: true);
		provider?.InvalidateChildrenCache();
	}

	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		if (eventId == AutomationEvents.AutomationFocusChanged && TryGetPeerOwner(peer, out var focusedElement))
		{
			TrackFocusedElement(focusedElement);
		}

		if (!AreUiaClientsListening())
		{
			return;
		}

		// Only look up existing providers for most events — eagerly creating
		// providers registers COM callable wrappers with UIA that prevent GC.
		// For focus and live region changes, create a provider so Narrator
		// can track focus or announce live region content.
		var provider = FindExistingProviderForPeer(peer, resolveEventsSource: true);
		if (provider is null)
		{
			// Eagerly materialize a provider for events raised on an element the client has not yet
			// navigated to: focus/live-region (Narrator tracking) and LayoutInvalidated (raised on the
			// AutoSuggestBox suggestions list the moment it is populated, before UIA has walked into it).
			if (eventId is AutomationEvents.AutomationFocusChanged or AutomationEvents.LiveRegionChanged or AutomationEvents.LayoutInvalidated)
			{
				provider = GetProviderForPeer(peer, resolveEventsSource: true);
			}

			if (provider is null)
			{
				return;
			}
		}

		try
		{
			switch (eventId)
			{
				case AutomationEvents.AutomationFocusChanged:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_AutomationFocusChangedEventId);
					break;
				case AutomationEvents.InvokePatternOnInvoked:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_Invoke_InvokedEventId);
					break;
				case AutomationEvents.SelectionPatternOnInvalidated:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_Selection_InvalidatedEventId);
					break;
				case AutomationEvents.SelectionItemPatternOnElementSelected:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_SelectionItem_ElementSelectedEventId);
					break;
				case AutomationEvents.SelectionItemPatternOnElementAddedToSelection:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_SelectionItem_ElementAddedToSelectionEventId);
					break;
				case AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_SelectionItem_ElementRemovedFromSelectionEventId);
					break;
				case AutomationEvents.TextPatternOnTextChanged:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_Text_TextChangedEventId);
					break;
				case AutomationEvents.TextPatternOnTextSelectionChanged:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_Text_TextSelectionChangedEventId);
					break;
				case AutomationEvents.StructureChanged:
					// Drop the cached subtree (cascading to virtual peers) and coalesce
					// the UIA notification on the dispatcher. Deferring rather than
					// raising synchronously avoids UIA re-entering GetChildren while a
					// peer is still computing its children — WCT's
					// DataGridItemAutomationPeer.GetChildrenCore calls
					// OwningRowPeer.InvalidatePeer() from inside that very call.
					RaiseStructureChanged(provider);
					break;
				case AutomationEvents.MenuOpened:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_MenuOpenedEventId);
					break;
				case AutomationEvents.MenuClosed:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_MenuClosedEventId);
					break;
				case AutomationEvents.ToolTipOpened:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_ToolTipOpenedEventId);
					break;
				case AutomationEvents.ToolTipClosed:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_ToolTipClosedEventId);
					break;
				case AutomationEvents.WindowOpened:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_Window_WindowOpenedEventId);
					break;
				case AutomationEvents.WindowClosed:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_Window_WindowClosedEventId);
					break;
				case AutomationEvents.LayoutInvalidated:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_LayoutInvalidatedEventId);
					break;
				case AutomationEvents.LiveRegionChanged:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_LiveRegionChangedEventId);
					// Also announce the live region text for reliable Narrator delivery
					var label = peer.GetName();
					if (!string.IsNullOrEmpty(label)
						&& provider.Owner is { } owner)
					{
						var liveSetting = AutomationProperties.GetLiveSetting(owner);
						if (liveSetting == AutomationLiveSetting.Assertive)
						{
							AnnounceAssertive(label);
						}
						else
						{
							AnnouncePolite(label);
						}
					}
					break;
			}
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"NotifyAutomationEvent failed for {eventId}: {ex.Message}");
			}
		}
	}

	public override void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
	{
		if (!IsAccessibilityEnabled
			|| !AreUiaClientsListening()
			|| string.IsNullOrEmpty(displayString))
		{
			return;
		}

		// Use specific provider if available, otherwise fall back to root
		IRawElementProviderSimple target = _rootProvider;
		if (FindExistingProviderForPeer(peer, resolveEventsSource: true) is { } elementProvider)
		{
			target = elementProvider;
		}

		try
		{
			// Uno enum values match UIA values exactly, so cast directly
			_ = Win32UIAutomationInterop.UiaRaiseNotificationEvent(
				target,
				(int)notificationKind,
				(int)notificationProcessing,
				displayString,
				activityId);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"NotifyNotificationEvent failed: {ex.Message}");
			}
		}
	}

	public override void NotifyTextEditTextChangedEvent(AutomationPeer peer, Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType changeType, System.Collections.Generic.IReadOnlyList<string> changedData)
	{
		if (!IsAccessibilityEnabled
			|| !AreUiaClientsListening()
			|| changedData is null)
		{
			// WinUI rejects a null changedData (E_POINTER) without raising; mirror that (no crash).
			return;
		}

		// WinUI's CUIAWindow::UIARaiseTextEditTextChangedEvent only raises for AutoCorrect / Composition /
		// CompositionFinalized — `None` falls through the switch without raising, and any other value is
		// E_INVALIDARG. Mirror that so `None` (or an out-of-range cast) is not surfaced as a spurious event.
		switch (changeType)
		{
			case Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType.AutoCorrect:
			case Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType.Composition:
			case Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType.CompositionFinalized:
				break;
			default:
				return;
		}

		// Materialize a provider if the client has not navigated to the element yet, mirroring the
		// focus/live-region/LayoutInvalidated handling above (text services may raise this on an
		// off-screen edit control). WinUI raises on the peer's own provider or fails (no root
		// retargeting — CUIAWindow::UIARaiseTextEditTextChangedEvent), so we do not fall back to root.
		var target = FindExistingProviderForPeer(peer, resolveEventsSource: true)
			?? GetProviderForPeer(peer, resolveEventsSource: true);
		if (target is null)
		{
			return;
		}

		var dataArray = new string[changedData.Count];
		for (var i = 0; i < changedData.Count; i++)
		{
			dataArray[i] = changedData[i];
		}

		try
		{
			// Uno's AutomationTextEditChangeType values match UIA TextEditChangeType exactly.
			_ = Win32UIAutomationInterop.UiaRaiseTextEditTextChangedEvent(target, (int)changeType, dataArray);
		}
		catch (Exception ex) when (
			ex is System.Runtime.InteropServices.COMException
				or DllNotFoundException
				or EntryPointNotFoundException
				or BadImageFormatException
				or TypeLoadException
				or System.Runtime.InteropServices.SEHException)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"NotifyTextEditTextChangedEvent failed: {ex.Message}");
			}
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Abstract no-op overrides — Win32 dispatches at the UIA layer
	//  via NotifyPropertyChangedEvent / NotifyAutomationEvent, not via
	//  the per-handle UpdateXxx methods used by the macOS path.
	// ──────────────────────────────────────────────────────────────

	protected override void UpdateName(nint handle, AutomationPeer peer, string? label) { }
	protected override void UpdateToggleState(nint handle, AutomationPeer peer, ToggleState newState) { }
	protected override void UpdateRangeValue(nint handle, AutomationPeer peer, double value) { }
	protected override void UpdateRangeBounds(nint handle, double min, double max) { }
	protected override void UpdateTextValue(nint handle, string? value) { }
	protected override void UpdateExpandCollapseState(nint handle, bool isExpanded) { }
	protected override void UpdateEnabled(nint handle, bool enabled) { }
	protected override void UpdateSelected(nint handle, bool selected) { }
	protected override void UpdateHelpText(nint handle, string? helpText) { }
	protected override void UpdateHeadingLevel(nint handle, int level) { }
	protected override void UpdateLandmark(nint handle, string? landmarkRole) { }
	protected override void UpdateRoleDescription(nint handle, string? roleDescription) { }
	protected override void UpdateIsReadOnly(nint handle, bool isReadOnly) { }
	protected override void UpdateFocusable(nint handle, bool focusable) { }
	protected override void UpdateIsOffscreen(nint handle, bool isOffscreen) { }
	protected override void SetNativeFocus(nint handle) { }
	protected override void OnNativeStructureChanged() { }

	// Forwarded by Win32RawElementProvider.AdviseEventAdded/Removed — currently a no-op
	// because UIA doesn't require explicit subscription management here.
	internal void OnAdviseEventAdded(int eventId, int[]? propertyIds) { }
	internal void OnAdviseEventRemoved(int eventId, int[]? propertyIds) { }

	// ──────────────────────────────────────────────────────────────
	//  Disposal — per-window provider cleanup
	// ──────────────────────────────────────────────────────────────

	protected override void DisposeCore()
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[UIA] Win32Accessibility disposing for window 0x{_hwnd:X}");
		}

		// The explicit registry includes peer-only virtual providers even if their weak cache key
		// has already been collected. UiaDisconnectAllProviders would affect other windows.
		foreach (var provider in new List<Win32RawElementProvider>(_liveProviders.Keys))
		{
			provider.Invalidate(allowRoot: true);
		}

		// Synthetic panes are not part of _providers (they wrap no UIElement),
		// so they must be disconnected separately.
		foreach (var pane in new IRawElementProviderSimple?[] { _outerPane, _innerPane })
		{
			if (pane is null)
			{
				continue;
			}
			if (!Win32UIAutomationInterop.TryDisconnectProvider(pane, out var error)
				&& this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("[UIA] UiaDisconnectProvider failed for synthetic pane during dispose.", error);
			}
		}

		_providers.Clear();
		_peerProviders.Clear();
		_liveProviders.Clear();
		_pendingStructureChanges.Clear();
		_pendingAddedProviders.Clear();
		_disconnectedProviders.Clear();
	}

	// ──────────────────────────────────────────────────────────────
	//  Property mapping
	// ──────────────────────────────────────────────────────────────

	private static int? MapAutomationPropertyToUia(AutomationProperty property)
	{
		if (ReferenceEquals(property, AutomationElementIdentifiers.NameProperty))
		{
			return Win32UIAutomationInterop.UIA_NamePropertyId;
		}
		if (ReferenceEquals(property, TogglePatternIdentifiers.ToggleStateProperty))
		{
			return Win32UIAutomationInterop.UIA_ToggleToggleStatePropertyId;
		}
		if (ReferenceEquals(property, RangeValuePatternIdentifiers.ValueProperty))
		{
			return Win32UIAutomationInterop.UIA_RangeValueValuePropertyId;
		}
		if (ReferenceEquals(property, ValuePatternIdentifiers.ValueProperty))
		{
			return Win32UIAutomationInterop.UIA_ValueValuePropertyId;
		}
		if (ReferenceEquals(property, ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty))
		{
			return Win32UIAutomationInterop.UIA_ExpandCollapseExpandCollapseStatePropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.IsEnabledProperty))
		{
			return Win32UIAutomationInterop.UIA_IsEnabledPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.HelpTextProperty))
		{
			return Win32UIAutomationInterop.UIA_HelpTextPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.HeadingLevelProperty))
		{
			return Win32UIAutomationInterop.UIA_HeadingLevelPropertyId;
		}
		if (ReferenceEquals(property, SelectionItemPatternIdentifiers.IsSelectedProperty))
		{
			return Win32UIAutomationInterop.UIA_SelectionItemIsSelectedPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.LandmarkTypeProperty))
		{
			return Win32UIAutomationInterop.UIA_LandmarkTypePropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.LocalizedLandmarkTypeProperty))
		{
			return Win32UIAutomationInterop.UIA_LocalizedLandmarkTypePropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.LiveSettingProperty))
		{
			return Win32UIAutomationInterop.UIA_LiveSettingPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.IsOffscreenProperty))
		{
			return Win32UIAutomationInterop.UIA_IsOffscreenPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.AcceleratorKeyProperty))
		{
			return Win32UIAutomationInterop.UIA_AcceleratorKeyPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.AccessKeyProperty))
		{
			return Win32UIAutomationInterop.UIA_AccessKeyPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.ItemStatusProperty))
		{
			return Win32UIAutomationInterop.UIA_ItemStatusPropertyId;
		}
		if (ReferenceEquals(property, AutomationElementIdentifiers.ItemTypeProperty))
		{
			return Win32UIAutomationInterop.UIA_ItemTypePropertyId;
		}

		return null;
	}
}
