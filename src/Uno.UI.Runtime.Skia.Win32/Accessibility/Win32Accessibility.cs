using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Manages the Win32 UIAutomation provider tree for Skia-rendered Uno applications.
/// Creates UIA providers lazily for elements that have automation peers, enabling
/// Narrator and other screen readers. The UIA tree follows the automation peer tree
/// (which flattens layout-only elements) rather than the raw visual tree.
/// </summary>
internal class Win32Accessibility : IUnoAccessibility, IAutomationPeerListener
{
	private static Win32Accessibility? _instance;

	private nint _hwnd;
	private bool _accessibilityTreeInitialized;
	private Win32RawElementProvider? _rootProvider;
	private readonly ConditionalWeakTable<UIElement, Win32RawElementProvider> _providers = new();
	private readonly ConditionalWeakTable<AutomationPeer, Win32RawElementProvider> _peerProviders = new();
	private DispatcherQueue? _dispatcherQueue;
	private readonly HashSet<Win32RawElementProvider> _pendingStructureChanges = new();

	internal static Win32Accessibility Instance => _instance ??= new Win32Accessibility();

	internal Win32RawElementProvider? RootProvider => _rootProvider;

	private Win32Accessibility()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(Win32Accessibility)}");
		}

		AccessibilityAnnouncer.AccessibilityImpl = this;
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnSizeOrOffsetChanged;
		AutomationPeer.AutomationPeerListener = this;
	}

	internal static void Register()
	{
		// Force singleton creation, which registers all callbacks
		_ = Instance;
	}

	// IUnoAccessibility

	public bool IsAccessibilityEnabled => _hwnd != nint.Zero;

	public void AnnouncePolite(string text)
	{
		if (!IsAccessibilityEnabled || _rootProvider is null)
		{
			return;
		}

		_ = Win32UIAutomationInterop.UiaRaiseNotificationEvent(
			_rootProvider,
			Win32UIAutomationInterop.AutomationNotificationKind_Other,
			Win32UIAutomationInterop.AutomationNotificationProcessing_CurrentThenMostRecent,
			text,
			"UnoAnnouncement");
	}

	public void AnnounceAssertive(string text)
	{
		if (!IsAccessibilityEnabled || _rootProvider is null)
		{
			return;
		}

		_ = Win32UIAutomationInterop.UiaRaiseNotificationEvent(
			_rootProvider,
			Win32UIAutomationInterop.AutomationNotificationKind_Other,
			Win32UIAutomationInterop.AutomationNotificationProcessing_ImportantMostRecent,
			text,
			"UnoAnnouncement");
	}

	// Initialization

	internal void Initialize(nint hwnd, UIElement rootElement)
	{
		_hwnd = hwnd;

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
		_dispatcherQueue = rootElement.DispatcherQueue;
		_accessibilityTreeInitialized = true;

		if (this.Log().IsEnabled(LogLevel.Information))
		{
			this.Log().Info(
				$"[UIA] Root provider created: element={rootElement.GetType().Name}, " +
				$"peer={rootPeer?.GetType().Name ?? "NULL"}, " +
				$"children count={rootElement.GetChildren().Count}, " +
				$"window=0x{_hwnd:X}");
		}

		// Dump the initial automation tree at Info level for diagnostics
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			DumpAutomationTree(rootElement, 0);
		}
	}

	private void DumpAutomationTree(UIElement element, int depth)
	{
		if (depth > 6)
		{
			return;
		}

		var indent = new string(' ', depth * 2);
		var peer = element.GetOrCreateAutomationPeer();
		var peerType = peer?.GetType().Name ?? "no-peer";
		string peerName;
		try { peerName = peer?.GetName() ?? ""; }
		catch { peerName = "<error>"; }
		var controlType = peer != null ? peer.GetAutomationControlType().ToString() : "none";
		var automationId = AutomationProperties.GetAutomationId(element);
		var automationName = AutomationProperties.GetName(element);

		this.Log().Debug(
			$"[UIA] {indent}{element.GetType().Name}" +
			$" peer={peerType}" +
			$" name=\"{peerName}\"" +
			$" type={controlType}" +
			$" a11yId={automationId}" +
			$" a11yName={automationName}" +
			$" vis={element.Visibility}");

		foreach (var child in element.GetChildren())
		{
			DumpAutomationTree(child, depth + 1);
		}
	}

	// Provider management

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

		if (!_accessibilityTreeInitialized || !IsAccessibilityEnabled)
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
	/// When <paramref name="resolveEventsSource"/> is true, the peer's EventsSource
	/// is resolved first (matching WinUI3 CUIAWrapper behavior where navigation
	/// substitutes the EventsSource peer before creating a wrapper).
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

	// Callbacks from UIElementAccessibilityHelper

	private void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (!IsAccessibilityEnabled || !_accessibilityTreeInitialized)
		{
			return;
		}

		// Raise structure changed event on the nearest ancestor that has a provider.
		// Child providers will be lazily created when UIA navigates to them.
		var ancestorProvider = FindNearestAncestorProvider(parent);
		if (ancestorProvider is not null)
		{
			RaiseStructureChanged(ancestorProvider);
		}
	}

	private void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (!IsAccessibilityEnabled || !_accessibilityTreeInitialized)
		{
			return;
		}

		// Clean up cached providers for the removed subtree
		CleanupProviders(child);

		// Raise structure changed event on the nearest ancestor
		var ancestorProvider = FindNearestAncestorProvider(parent);
		if (ancestorProvider is not null)
		{
			RaiseStructureChanged(ancestorProvider);
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
		// Each affected provider is tracked so we fire per-subtree events rather
		// than always invalidating the entire root, which reduces the scope of
		// UIA re-validation during steady-state interaction. During startup the
		// batch still avoids hundreds of synchronous round-trips.
		if (_dispatcherQueue is not null)
		{
			var wasEmpty = _pendingStructureChanges.Count == 0;
			_pendingStructureChanges.Add(provider);

			if (wasEmpty)
			{
				_dispatcherQueue.TryEnqueue(() =>
				{
					foreach (var pending in _pendingStructureChanges)
					{
						RaiseStructureChangedCore(pending);
					}
					_pendingStructureChanges.Clear();
				});
			}
			return;
		}

		RaiseStructureChangedCore(provider);
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

	// Callback from VisualAccessibilityHelper

	private void OnSizeOrOffsetChanged(Visual visual)
	{
		if (!IsAccessibilityEnabled || !_accessibilityTreeInitialized)
		{
			return;
		}

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

	// Helpers

	internal static bool TryGetPeerOwner(AutomationPeer peer, [NotNullWhen(true)] out UIElement? owner)
		=> peer.TryGetProviderOwner(out owner);

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

	// IAutomationPeerListener

	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
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

	public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
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

	public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
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

	public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		NotifyAutomationEvent(peer, eventId);
	}

	public bool ListenerExistsHelper(AutomationEvents eventId)
		=> IsAccessibilityEnabled;

	public void OnAdviseEventAdded(int eventId, int[]? propertyIds)
	{
	}

	public void OnAdviseEventRemoved(int eventId, int[]? propertyIds)
	{
	}

	// Property mapping

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
