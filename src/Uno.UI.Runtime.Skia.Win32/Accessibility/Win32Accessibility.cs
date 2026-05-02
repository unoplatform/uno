#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
	private readonly ConditionalWeakTable<UIElement, Win32RawElementProvider> _providers = new();
	private readonly ConditionalWeakTable<AutomationPeer, Win32RawElementProvider> _peerProviders = new();
	private readonly HashSet<Win32RawElementProvider> _pendingStructureChanges = new();
	private bool _structureChangeFlushQueued;

	internal Win32RawElementProvider? RootProvider => _rootProvider;

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
		_providers.AddOrUpdate(rootElement, _rootProvider);
		if (rootPeer is not null)
		{
			_peerProviders.AddOrUpdate(rootPeer, _rootProvider);
		}

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
		catch (Exception ex)
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
		if (_providers.TryGetValue(element, out var existing))
		{
			return existing;
		}

		if (!IsAccessibilityEnabled)
		{
			return null;
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

		return GetOrCreateProviderForResolvedPeer(peer.ResolveProviderPeer(resolveEventsSource: true));
	}

	/// <summary>
	/// Resolves an <see cref="AutomationPeer"/> to its corresponding UIA provider.
	/// </summary>
	internal Win32RawElementProvider? GetProviderForPeer(AutomationPeer peer, bool resolveEventsSource = false)
	{
		return GetOrCreateProviderForResolvedPeer(peer.ResolveProviderPeer(resolveEventsSource));
	}

	internal Win32RawElementProvider? GetProvider(UIElement element)
	{
		if (_providers.TryGetValue(element, out var provider))
		{
			return provider;
		}

		var peer = element.GetOrCreateAutomationPeer();
		return peer is null
			? null
			: GetOrCreateProviderForResolvedPeer(peer.ResolveProviderPeer(resolveEventsSource: true));
	}

	private Win32RawElementProvider? GetOrCreateProviderForResolvedPeer(AutomationPeer resolvedPeer)
	{
		if (!resolvedPeer.TryGetProviderOwner(out var element))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] GetProviderForPeer: Could not resolve owner for {resolvedPeer.GetType().Name}");
			}
			return null;
		}

		var canonicalPeer = element.GetOrCreateAutomationPeer()?.ResolveProviderPeer(resolveEventsSource: true) ?? resolvedPeer;
		if (!canonicalPeer.TryGetProviderOwner(out element))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] GetProviderForPeer: Canonical owner resolution failed for {canonicalPeer.GetType().Name}");
			}
			return null;
		}

		// Virtual/secondary peer: resolvedPeer shares an Owner UIElement with a
		// *different* canonical peer (e.g. DataGridItemAutomationPeer whose Owner
		// is the DataGrid, while the canonical peer for that element is
		// DataGridAutomationPeer).  Returning the canonical provider here would
		// make the DataGrid appear as its own child in the UIA tree.
		// Give the virtual peer its own provider, keyed only in _peerProviders.
		if (!ReferenceEquals(resolvedPeer, canonicalPeer))
		{
			if (_peerProviders.TryGetValue(resolvedPeer, out var existingVirtual))
			{
				return existingVirtual;
			}

			var virtualProvider = new Win32RawElementProvider(element, _hwnd, isRoot: false, this, resolvedPeer);
			_peerProviders.AddOrUpdate(resolvedPeer, virtualProvider);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[UIA] Created virtual provider for {virtualProvider.DescribeElement()} (peer={resolvedPeer.GetType().Name})");
			}

			return virtualProvider;
		}

		if (_providers.TryGetValue(element, out var existingByElement)
			&& existingByElement.RepresentsPeer(canonicalPeer))
		{
			_peerProviders.AddOrUpdate(canonicalPeer, existingByElement);
			return existingByElement;
		}

		if (_peerProviders.TryGetValue(canonicalPeer, out var existingByPeer))
		{
			_providers.AddOrUpdate(element, existingByPeer);
			return existingByPeer;
		}

		var provider = new Win32RawElementProvider(element, _hwnd, isRoot: false, this, canonicalPeer);
		_providers.AddOrUpdate(element, provider);
		_peerProviders.AddOrUpdate(canonicalPeer, provider);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[UIA] Created provider for {provider.DescribeElement()} (peer={canonicalPeer.GetType().Name})");
		}

		return provider;
	}

	// ──────────────────────────────────────────────────────────────
	//  Tree management — called from router via base.Route*
	// ──────────────────────────────────────────────────────────────

	protected override void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		// Raise structure changed event on the nearest ancestor that has a provider.
		// Child providers will be lazily created when UIA navigates to them.
		var ancestorProvider = FindNearestAncestorProvider(parent);
		if (ancestorProvider is not null)
		{
			RaiseStructureChanged(ancestorProvider);
		}
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		// Clean up cached providers for the removed subtree
		CleanupProviders(child);

		// Raise structure changed event on the nearest ancestor
		var ancestorProvider = FindNearestAncestorProvider(parent);
		if (ancestorProvider is not null)
		{
			RaiseStructureChanged(ancestorProvider);
		}
	}

	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		// UIA pulls BoundingRectangle on demand, so we only need to notify
		// clients that the property has changed so they re-query it.
		if (visual is ContainerVisual containerVisual
			&& containerVisual.Owner?.Target is UIElement owner
			&& _providers.TryGetValue(owner, out var provider))
		{
			try
			{
				_ = Win32UIAutomationInterop.UiaRaiseAutomationPropertyChangedEvent(
					provider,
					Win32UIAutomationInterop.UIA_BoundingRectanglePropertyId,
					null,
					null);
			}
			catch (Exception ex)
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

		while (stack.Count > 0)
		{
			var current = stack.Pop();

			if (_providers.TryGetValue(current, out var provider))
			{
				// Clear cached peer lists so a stale provider cannot keep the
				// removed subtree alive.
				provider.InvalidateChildrenCache();
				_pendingStructureChanges.Remove(provider);

				_providers.Remove(current);
				if (provider.RepresentedPeer is { } representedPeer)
				{
					_peerProviders.Remove(representedPeer);
				}

				// Disconnect the provider from UIA so stale COM references are released.
				try
				{
					_ = Win32UIAutomationInterop.UiaDisconnectProvider(provider);
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"UiaDisconnectProvider failed for {provider.DescribeElement()}: {ex.Message}");
					}
				}
			}

			foreach (var child in current.GetChildren())
			{
				stack.Push(child);
			}
		}
	}

	private Win32RawElementProvider? FindNearestAncestorProvider(UIElement element)
	{
		UIElement? current = element;
		while (current is not null)
		{
			if (_providers.TryGetValue(current, out var provider))
			{
				return provider;
			}
			current = VisualTreeHelper.GetParent(current) as UIElement;
		}
		return _rootProvider;
	}

	private void RaiseStructureChanged(Win32RawElementProvider provider)
	{
		// Invalidate the children cache so the next navigation rebuilds the list
		provider.InvalidateChildrenCache();

		// Coalesce rapid StructureChanged events into a single deferred dispatch.
		_pendingStructureChanges.Add(provider);
		if (_structureChangeFlushQueued)
		{
			return;
		}

		_structureChangeFlushQueued = true;
		_dispatcherQueue.TryEnqueue(() =>
		{
			_structureChangeFlushQueued = false;

			// Short-circuit the flush if the window was closed while this callback
			// was queued (edge case "Window close during dispatch").
			if (IsDisposed)
			{
				_pendingStructureChanges.Clear();
				return;
			}

			foreach (var pending in _pendingStructureChanges)
			{
				RaiseStructureChangedCore(pending);
			}
			_pendingStructureChanges.Clear();
		});
	}

	private void RaiseStructureChangedCore(Win32RawElementProvider provider)
	{
		try
		{
			_ = Win32UIAutomationInterop.UiaRaiseStructureChangedEvent(
				provider,
				StructureChangeType.ChildrenInvalidated,
				provider.GetRuntimeId());
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"RaiseStructureChanged failed: {ex.Message}");
			}
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Helpers
	// ──────────────────────────────────────────────────────────────

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

		if (_peerProviders.TryGetValue(resolvedPeer, out var providerByPeer))
		{
			return providerByPeer;
		}

		if (resolvedPeer.TryGetProviderOwner(out var element) && _providers.TryGetValue(element, out var providerByElement))
		{
			return providerByElement;
		}

		return null;
	}

	// ──────────────────────────────────────────────────────────────
	//  Automation peer listener — UIA-style dispatch overrides
	// ──────────────────────────────────────────────────────────────

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

	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (!IsAccessibilityEnabled)
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
			if (eventId is AutomationEvents.AutomationFocusChanged or AutomationEvents.LiveRegionChanged)
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
					if (TryGetPeerOwner(peer, out var focusedElement))
					{
						TrackFocusedElement(focusedElement);
					}
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
					_ = Win32UIAutomationInterop.UiaRaiseStructureChangedEvent(
						provider, StructureChangeType.ChildrenInvalidated, provider.GetRuntimeId());
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
				case AutomationEvents.LiveRegionChanged:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_LiveRegionChangedEventId);
					// Also announce the live region text for reliable Narrator delivery
					var label = peer.GetName();
					if (!string.IsNullOrEmpty(label))
					{
						var liveSetting = AutomationProperties.GetLiveSetting(provider.Owner);
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
		if (!IsAccessibilityEnabled || string.IsNullOrEmpty(displayString))
		{
			return;
		}

		// Use specific provider if available, otherwise fall back to root
		IRawElementProviderSimple? target = _rootProvider;
		if (FindExistingProviderForPeer(peer, resolveEventsSource: true) is { } elementProvider)
		{
			target = elementProvider;
		}

		if (target is null)
		{
			return;
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

		// Disconnect every live cached provider scoped to this window so UIA
		// clients see a well-formed disconnect rather than a dangling HWND.
		// Uses the .NET 9+ ConditionalWeakTable IEnumerable<KeyValuePair<...>>
		// support. UiaDisconnectAllProviders is intentionally NOT used — it is
		// process-wide and would disconnect providers belonging to other windows.
		foreach (var pair in _providers)
		{
			try
			{
				_ = Win32UIAutomationInterop.UiaDisconnectProvider(pair.Value);
			}
			catch (Exception ex)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[UIA] UiaDisconnectProvider failed during dispose: {ex.Message}");
				}
			}
		}

		// Also disconnect virtual providers (keyed only in _peerProviders, not
		// _providers).  These are created for secondary peers like
		// DataGridItemAutomationPeer whose Owner UIElement is shared with the
		// element's canonical peer.
		foreach (var pair in _peerProviders)
		{
			if (!_providers.TryGetValue(pair.Value.Owner, out var elementProvider)
				|| !ReferenceEquals(elementProvider, pair.Value))
			{
				try
				{
					_ = Win32UIAutomationInterop.UiaDisconnectProvider(pair.Value);
				}
				catch (Exception ex)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"[UIA] UiaDisconnectProvider (virtual) failed during dispose: {ex.Message}");
					}
				}
			}
		}

		_providers.Clear();
		_peerProviders.Clear();
		_pendingStructureChanges.Clear();
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
