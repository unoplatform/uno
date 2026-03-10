using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Manages the Win32 UIAutomation provider tree for Skia-rendered Uno applications.
/// Creates a parallel UIA provider tree (IRawElementProviderFragment objects) that
/// mirrors the XAML visual tree, enabling Narrator and other screen readers.
/// Follows the same pattern as <see cref="T:Uno.UI.Runtime.Skia.MacOS.MacOSAccessibility"/>.
/// </summary>
internal class Win32Accessibility : IUnoAccessibility, IAutomationPeerListener
{
	private static Win32Accessibility? _instance;

	private nint _hwnd;
	private bool _accessibilityTreeInitialized;
	private Win32RawElementProvider? _rootProvider;
	private readonly Dictionary<UIElement, Win32RawElementProvider> _providers = new();

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
			this.Log().Debug($"Win32Accessibility initialized for window 0x{hwnd:X}");
		}

		TryInitializeAccessibilityTree(rootElement);
	}

	private void TryInitializeAccessibilityTree(UIElement? rootElement)
	{
		if (_accessibilityTreeInitialized || !IsAccessibilityEnabled || rootElement is null)
		{
			return;
		}

		_accessibilityTreeInitialized = true;
		CreateProviderTree(rootElement);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Accessibility tree initialized for window 0x{_hwnd:X} with {_providers.Count} providers");
		}
	}

	// Tree management

	private void CreateProviderTree(UIElement rootElement)
	{
		_rootProvider = new Win32RawElementProvider(
			rootElement, _hwnd, isRoot: true, this, parent: null);
		_providers[rootElement] = _rootProvider;

		foreach (var child in rootElement.GetChildren())
		{
			BuildProviderTreeRecursive(child, _rootProvider);
		}
	}

	private void BuildProviderTreeRecursive(UIElement element, Win32RawElementProvider parentProvider)
	{
		var provider = new Win32RawElementProvider(
			element, _hwnd, isRoot: false, this, parentProvider);
		_providers[element] = provider;

		foreach (var child in element.GetChildren())
		{
			BuildProviderTreeRecursive(child, provider);
		}
	}

	internal Win32RawElementProvider? GetProvider(UIElement element)
	{
		_providers.TryGetValue(element, out var provider);
		return provider;
	}

	// Callbacks from UIElementAccessibilityHelper

	private void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (!IsAccessibilityEnabled || !_accessibilityTreeInitialized)
		{
			return;
		}

		if (_providers.TryGetValue(parent, out var parentProvider))
		{
			AddProvider(child, parentProvider);

			foreach (var childChild in child.GetChildren())
			{
				OnChildAdded(child, childChild, null);
			}
		}
	}

	private void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (!IsAccessibilityEnabled || !_accessibilityTreeInitialized)
		{
			return;
		}

		RemoveProvider(child);
	}

	private void AddProvider(UIElement element, Win32RawElementProvider parentProvider)
	{
		if (_providers.ContainsKey(element))
		{
			return;
		}

		var provider = new Win32RawElementProvider(
			element, _hwnd, isRoot: false, this, parentProvider);
		_providers[element] = provider;
	}

	private void RemoveProvider(UIElement element)
	{
		// Remove children first
		foreach (var child in element.GetChildren())
		{
			RemoveProvider(child);
		}

		_providers.Remove(element);
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
	{
		if (peer is FrameworkElementAutomationPeer { Owner: { } element })
		{
			owner = element;
			return true;
		}

		if (peer is ItemAutomationPeer itemPeer)
		{
			var parent = itemPeer.GetParent();
			if (parent is FrameworkElementAutomationPeer { Owner: { } parentElement })
			{
				owner = parentElement;
				return true;
			}
		}

		owner = null;
		return false;
	}

	// IAutomationPeerListener

	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (!IsAccessibilityEnabled || !TryGetPeerOwner(peer, out var owner))
		{
			return;
		}

		if (!_providers.TryGetValue(owner, out var provider))
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
		if (!IsAccessibilityEnabled || !TryGetPeerOwner(peer, out var owner))
		{
			return;
		}

		if (!_providers.TryGetValue(owner, out var provider))
		{
			return;
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
				case AutomationEvents.StructureChanged:
					_ = Win32UIAutomationInterop.UiaRaiseStructureChangedEvent(
						provider, StructureChangeType.ChildrenInvalidated, provider.GetRuntimeId());
					break;
				case AutomationEvents.LiveRegionChanged:
					_ = Win32UIAutomationInterop.UiaRaiseAutomationEvent(
						provider, Win32UIAutomationInterop.UIA_LiveRegionChangedEventId);
					// Also announce the live region text for reliable Narrator delivery
					var label = peer.GetName();
					if (!string.IsNullOrEmpty(label))
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

	public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
	{
		if (!IsAccessibilityEnabled || string.IsNullOrEmpty(displayString))
		{
			return;
		}

		// Use specific provider if available, otherwise fall back to root
		IRawElementProviderSimple? target = _rootProvider;
		if (TryGetPeerOwner(peer, out var owner) && _providers.TryGetValue(owner, out var elementProvider))
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

		return null;
	}
}
