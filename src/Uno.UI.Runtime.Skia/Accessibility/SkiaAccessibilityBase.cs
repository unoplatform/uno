#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Shared base class for Skia platform accessibility implementations.
/// Provides common property-change routing, event routing, utility methods,
/// focus recovery, modal dialog lifecycle, and announcement debouncing.
/// Platform-specific subclasses override abstract methods for native interop.
/// </summary>
internal abstract class SkiaAccessibilityBase : IUnoAccessibility, IAutomationPeerListener
{
	// Announcement debounce/throttle constants
	private const int AnnouncementDebounceMs = 100;
	private const int PoliteThrottleMs = 500;
	private const int AssertiveThrottleMs = 200;

	private string? _pendingPoliteContent;
	private string? _pendingAssertiveContent;
	private Timer? _politeDebounceTimer;
	private Timer? _assertiveDebounceTimer;
	private long _politeThrottleTimestamp;
	private long _assertiveThrottleTimestamp;
	private string? _lastAnnouncedPoliteContent;
	private string? _lastAnnouncedAssertiveContent;

	// Focus tracking
	private UIElement? _trackedFocusedElement;

	/// <summary>
	/// Registers this instance as the accessibility implementation for the Uno framework.
	/// Wires up UIElement child-add/remove callbacks, visual change callbacks, and
	/// automation peer listener. Call from subclass constructor or initialization.
	/// </summary>
	protected void RegisterCallbacks()
	{
		AccessibilityAnnouncer.AccessibilityImpl = this;
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAddedCore;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemovedCore;
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnSizeOrOffsetChangedCore;
		AutomationPeer.AutomationPeerListener = this;
	}

	// ──────────────────────────────────────────────────────────────
	//  Abstract: Platform state
	// ──────────────────────────────────────────────────────────────

	/// <summary>Whether accessibility is currently enabled and the tree is initialized.</summary>
	public abstract bool IsAccessibilityEnabled { get; }

	// ──────────────────────────────────────────────────────────────
	//  Abstract: Tree management
	// ──────────────────────────────────────────────────────────────

	/// <summary>Called when a child is added to the visual tree. Platform handles tree updates.</summary>
	protected abstract void OnChildAdded(UIElement parent, UIElement child, int? index);

	/// <summary>Called when a child is removed from the visual tree. Platform handles tree updates.</summary>
	protected abstract void OnChildRemoved(UIElement parent, UIElement child);

	/// <summary>Called when a visual's offset or size changes. Platform handles position updates.</summary>
	protected abstract void OnSizeOrOffsetChanged(Microsoft.UI.Composition.Visual visual);

	// ──────────────────────────────────────────────────────────────
	//  Abstract: Property updates (called from shared routing)
	// ──────────────────────────────────────────────────────────────

	protected abstract void UpdateName(nint handle, AutomationPeer peer, string? label);
	protected abstract void UpdateToggleState(nint handle, AutomationPeer peer, ToggleState newState);
	protected abstract void UpdateRangeValue(nint handle, AutomationPeer peer, double value);
	protected abstract void UpdateRangeBounds(nint handle, double min, double max);
	protected abstract void UpdateTextValue(nint handle, string? value);
	protected abstract void UpdateExpandCollapseState(nint handle, bool isExpanded);
	protected abstract void UpdateEnabled(nint handle, bool enabled);
	protected abstract void UpdateSelected(nint handle, bool selected);
	protected abstract void UpdateHelpText(nint handle, string? helpText);
	protected abstract void UpdateHeadingLevel(nint handle, int level);
	protected abstract void UpdateLandmark(nint handle, string? landmarkRole);
	protected abstract void UpdateIsReadOnly(nint handle, bool isReadOnly);
	protected abstract void UpdateFocusable(nint handle, bool focusable);
	protected abstract void UpdateIsOffscreen(nint handle, bool isOffscreen);

	// ──────────────────────────────────────────────────────────────
	//  Abstract: Focus & modal
	// ──────────────────────────────────────────────────────────────

	protected abstract void SetNativeFocus(nint handle);
	protected abstract void OnNativeStructureChanged();

	// ──────────────────────────────────────────────────────────────
	//  Abstract: Announcements
	// ──────────────────────────────────────────────────────────────

	protected abstract void AnnounceOnPlatform(string text, bool assertive);

	// ──────────────────────────────────────────────────────────────
	//  Shared: Callback wrappers with guard
	// ──────────────────────────────────────────────────────────────

	private void OnChildAddedCore(UIElement parent, UIElement child, int? index)
	{
		if (IsAccessibilityEnabled)
		{
			OnChildAdded(parent, child, index);
		}
	}

	private void OnChildRemovedCore(UIElement parent, UIElement child)
	{
		if (IsAccessibilityEnabled)
		{
			OnChildRemoved(parent, child);
		}
	}

	private void OnSizeOrOffsetChangedCore(Microsoft.UI.Composition.Visual visual)
	{
		if (IsAccessibilityEnabled)
		{
			OnSizeOrOffsetChanged(visual);
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Shared: IAutomationPeerListener — Property change routing
	// ──────────────────────────────────────────────────────────────

	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		try
		{
			NotifyPropertyChangedEventCore(peer, automationProperty, oldValue, newValue);
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] NotifyPropertyChangedEvent failed: property={automationProperty} peer={peer.GetType().Name}: {ex.Message}", ex);
			}
		}
	}

	/// <summary>
	/// Routes property changes to platform-specific update methods.
	/// Subclasses can override to add platform-specific property handling
	/// (e.g., WASM roving tabindex on selection change) by calling base first.
	/// </summary>
	protected virtual void NotifyPropertyChangedEventCore(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, out var element))
		{
			var label = AriaMapper.ResolveLabel(peer);
			UpdateName(element.Visual.Handle, peer, label ?? (string)newValue);
		}
		else if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty &&
			TryGetPeerOwner(peer, out element))
		{
			UpdateToggleState(element.Visual.Handle, peer, (ToggleState)newValue);
		}
		else if (automationProperty == RangeValuePatternIdentifiers.ValueProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (newValue is double doubleValue)
			{
				UpdateRangeValue(element.Visual.Handle, peer, doubleValue);
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.ValueProperty &&
			TryGetPeerOwner(peer, out element))
		{
			UpdateTextValue(element.Visual.Handle, newValue as string);
		}
		else if (automationProperty == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var isExpanded = newValue is ExpandCollapseState state &&
				(state == ExpandCollapseState.Expanded || state == ExpandCollapseState.PartiallyExpanded);
			UpdateExpandCollapseState(element.Visual.Handle, isExpanded);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsEnabledProperty &&
			TryGetPeerOwner(peer, out element))
		{
			UpdateEnabled(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.HelpTextProperty &&
			TryGetPeerOwner(peer, out element))
		{
			UpdateHelpText(element.Visual.Handle, newValue as string);
		}
		else if (automationProperty == AutomationElementIdentifiers.HeadingLevelProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var level = ConvertHeadingLevel(newValue);
			UpdateHeadingLevel(element.Visual.Handle, level);
		}
		else if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty &&
			TryGetPeerOwner(peer, out element))
		{
			UpdateSelected(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.LandmarkTypeProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var landmarkType = newValue is AutomationLandmarkType lt ? lt : AutomationLandmarkType.None;
			UpdateLandmark(element.Visual.Handle, AriaMapper.GetLandmarkRole(landmarkType));
		}
		else if (automationProperty == RangeValuePatternIdentifiers.MinimumProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (newValue is double min &&
				peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				UpdateRangeBounds(element.Visual.Handle, min, rangeProvider.Maximum);
			}
		}
		else if (automationProperty == RangeValuePatternIdentifiers.MaximumProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (newValue is double max &&
				peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				UpdateRangeBounds(element.Visual.Handle, rangeProvider.Minimum, max);
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.IsReadOnlyProperty &&
			TryGetPeerOwner(peer, out element))
		{
			UpdateIsReadOnly(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsOffscreenProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// WinUI 3's RaiseAutomaticPropertyChanges tracks IsOffscreen and raises
			// property change events when an element moves on/off screen.
			UpdateIsOffscreen(element.Visual.Handle, newValue is true);
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Shared: IAutomationPeerListener — Automation event routing
	// ──────────────────────────────────────────────────────────────

	public virtual void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		switch (eventId)
		{
			case AutomationEvents.AutomationFocusChanged when TryGetPeerOwner(peer, out var focusedElement):
				SetNativeFocus(focusedElement.Visual.Handle);
				TrackFocusedElement(focusedElement);
				break;

			case AutomationEvents.TextEditTextChanged:
			case AutomationEvents.TextPatternOnTextChanged:
				if (TryGetPeerOwner(peer, out var textElement) &&
					peer.GetPattern(PatternInterface.Value) is IValueProvider textValueProvider)
				{
					UpdateTextValue(textElement.Visual.Handle, textValueProvider.Value);
				}
				break;

			case AutomationEvents.StructureChanged:
				OnNativeStructureChanged();
				break;
		}
	}

	public virtual void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
	{
		if (!IsAccessibilityEnabled || string.IsNullOrEmpty(displayString))
		{
			return;
		}

		var assertive = notificationProcessing == AutomationNotificationProcessing.ImportantAll ||
						notificationProcessing == AutomationNotificationProcessing.ImportantMostRecent;

		if (assertive)
		{
			AnnounceAssertive(displayString);
		}
		else
		{
			AnnouncePolite(displayString);
		}
	}

	public bool ListenerExistsHelper(AutomationEvents eventId)
		=> IsAccessibilityEnabled;

	public virtual void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		=> NotifyAutomationEvent(peer, eventId);

	// ──────────────────────────────────────────────────────────────
	//  Shared: Utility methods
	// ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Extracts the owner UIElement from an AutomationPeer.
	/// Works for both FrameworkElementAutomationPeer and ItemAutomationPeer.
	/// </summary>
	protected static bool TryGetPeerOwner(AutomationPeer peer, [NotNullWhen(true)] out UIElement? owner)
	{
		if (peer is FrameworkElementAutomationPeer { Owner: { } element })
		{
			owner = element;
			return true;
		}

		if (peer is ItemAutomationPeer itemPeer)
		{
			// First try to resolve the actual item container (e.g., the ListViewItem
			// or ComboBoxItem). This is necessary so property updates target the
			// correct native accessibility element rather than the parent container.
			var container = itemPeer.GetContainer();
			if (container is not null)
			{
				owner = container;
				return true;
			}

			// Fall back to parent's owner if container is not yet materialized
			// (e.g., virtualized items that haven't been realized).
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

	/// <summary>
	/// Determines whether a UIElement should be accessibility-focusable.
	/// Checks focusability, AccessibilityView, role overrides, and peer existence.
	/// </summary>
	protected static bool IsAccessibilityFocusable(DependencyObject dependencyObject, bool isFocusable)
	{
		var hasRoleOverride = dependencyObject is UIElement roleElement &&
			!string.IsNullOrEmpty(AutomationProperties.GetRoleOverride(roleElement));

		if (!isFocusable && !hasRoleOverride && dependencyObject is not (TextBlock or RichTextBlock))
		{
			return false;
		}

		var accessibilityView = AutomationProperties.GetAccessibilityView(dependencyObject);
		if (accessibilityView == AccessibilityView.Raw)
		{
			return false;
		}

		if (hasRoleOverride)
		{
			return true;
		}

		if ((dependencyObject as UIElement)?.GetOrCreateAutomationPeer() is null)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Finds the first focusable control within a UIElement tree (depth-first).
	/// </summary>
	protected static Control? FindFirstFocusableChild(UIElement parent)
	{
		foreach (var child in parent.GetChildren())
		{
			if (child is Control control && control.IsFocusable && control.IsEnabled && control.Visibility == Visibility.Visible)
			{
				return control;
			}

			var result = FindFirstFocusableChild(child);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}

	/// <summary>
	/// Enumerates all focusable children within a UIElement tree, collecting their handles.
	/// </summary>
	protected static void EnumerateFocusableChildren(UIElement parent, System.Collections.Generic.List<IntPtr> focusableHandles)
	{
		foreach (var child in parent.GetChildren())
		{
			if (child is Control control && control.IsFocusable && control.IsEnabled && control.Visibility == Visibility.Visible)
			{
				focusableHandles.Add(child.Visual.Handle);
			}

			EnumerateFocusableChildren(child, focusableHandles);
		}
	}

	/// <summary>
	/// Checks whether the given element is a descendant of the element with the specified handle.
	/// Used for modal-aware filtering (suppress background live regions during modal).
	/// </summary>
	protected static bool IsDescendantOf(UIElement element, nint ancestorHandle)
	{
		DependencyObject? current = element;
		while (current is not null)
		{
			if (current is UIElement uiElement && uiElement.Visual.Handle == ancestorHandle)
			{
				return true;
			}
			current = (current as FrameworkElement)?.Parent;
		}
		return false;
	}

	/// <summary>
	/// Resolves the accessibility role for a peer, with support for platform-specific overrides.
	/// Subclasses can override for platform-specific role mappings.
	/// </summary>
	protected virtual string? ResolveRole(AutomationPeer peer, UIElement owner)
		=> AriaMapper.GetAriaRole(peer.GetAutomationControlType());

	/// <summary>
	/// Resolves the accessibility label for a peer.
	/// </summary>
	protected static string? ResolveLabel(AutomationPeer peer)
		=> AriaMapper.ResolveLabel(peer);

	/// <summary>
	/// Converts an AutomationHeadingLevel enum to its integer representation (1-9, or 0 for None).
	/// </summary>
	protected static int ConvertHeadingLevel(object? value)
	{
		return value is AutomationHeadingLevel headingLevel ? headingLevel switch
		{
			AutomationHeadingLevel.Level1 => 1,
			AutomationHeadingLevel.Level2 => 2,
			AutomationHeadingLevel.Level3 => 3,
			AutomationHeadingLevel.Level4 => 4,
			AutomationHeadingLevel.Level5 => 5,
			AutomationHeadingLevel.Level6 => 6,
			AutomationHeadingLevel.Level7 => 7,
			AutomationHeadingLevel.Level8 => 8,
			AutomationHeadingLevel.Level9 => 9,
			_ => 0,
		} : 0;
	}

	// ──────────────────────────────────────────────────────────────
	//  Shared: Focus tracking and recovery
	// ──────────────────────────────────────────────────────────────

	/// <summary>Gets the currently tracked focused element.</summary>
	protected UIElement? TrackedFocusedElement => _trackedFocusedElement;

	/// <summary>
	/// Begins tracking a focused element for focus recovery.
	/// When the element is disabled or unloaded, <see cref="RecoverFocus"/> is called.
	/// </summary>
	protected void TrackFocusedElement(UIElement element)
	{
		UntrackFocusedElement();
		_trackedFocusedElement = element;

		if (element is Control control)
		{
			control.IsEnabledChanged += OnTrackedElementIsEnabledChanged;
		}

		if (element is FrameworkElement fe)
		{
			fe.Unloaded += OnTrackedElementUnloaded;
		}
	}

	/// <summary>Stops tracking the current focused element.</summary>
	protected void UntrackFocusedElement()
	{
		if (_trackedFocusedElement is null)
		{
			return;
		}

		if (_trackedFocusedElement is Control control)
		{
			control.IsEnabledChanged -= OnTrackedElementIsEnabledChanged;
		}

		if (_trackedFocusedElement is FrameworkElement fe)
		{
			fe.Unloaded -= OnTrackedElementUnloaded;
		}

		_trackedFocusedElement = null;
	}

	private void OnTrackedElementIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is false && sender is UIElement element)
		{
			// Do NOT call RecoverFocus when an element becomes disabled.
			// WinUI retains focus on disabled controls; the framework's own
			// focus management (not the accessibility layer) is responsible for
			// focus recovery. Proactively moving focus here interferes with
			// transient disable/re-enable cycles such as ButtonBase command
			// execution toggling ICommand.CanExecute.
			if (!IsElementCurrentlyFocused(element))
			{
				UntrackFocusedElement();
			}
			// If still focused, keep tracking so OnTrackedElementUnloaded
			// can handle the case where the element is later removed.
		}
	}

	private void OnTrackedElementUnloaded(object sender, RoutedEventArgs e)
	{
		if (sender is UIElement element)
		{
			// Only recover focus if the element is still the focused element.
			// During navigation, the framework handles focus transfer itself.
			if (IsElementCurrentlyFocused(element))
			{
				RecoverFocus(element);
			}
			else
			{
				UntrackFocusedElement();
			}
		}
	}

	private static bool IsElementCurrentlyFocused(UIElement element)
	{
		if (element.XamlRoot is null)
		{
			return false;
		}

		var focusedElement = FocusManager.GetFocusedElement(element.XamlRoot);
		return ReferenceEquals(focusedElement, element);
	}

	/// <summary>
	/// Finds the next valid focus target when the currently focused element
	/// becomes disabled or is removed from the visual tree.
	/// Strategy: (1) next focusable sibling, (2) previous sibling, (3) ancestor.
	/// </summary>
	protected virtual void RecoverFocus(UIElement lostElement)
	{
		UntrackFocusedElement();

		var parent = lostElement.GetParent() as UIElement;
		if (parent is not null)
		{
			var children = parent.GetChildren();
			bool foundLost = false;

			// Look for the next focusable sibling after the lost element
			foreach (var child in children)
			{
				if (child == lostElement)
				{
					foundLost = true;
					continue;
				}

				if (foundLost && child is Control nextControl && nextControl.IsFocusable && nextControl.IsEnabled)
				{
					nextControl.StartBringIntoView();
					nextControl.Focus(FocusState.Keyboard);
					return;
				}
			}

			// Try siblings before the lost element
			foreach (var child in children)
			{
				if (child == lostElement)
				{
					break;
				}

				if (child is Control prevControl && prevControl.IsFocusable && prevControl.IsEnabled)
				{
					prevControl.StartBringIntoView();
					prevControl.Focus(FocusState.Keyboard);
					return;
				}
			}
		}

		// Try nearest focusable ancestor
		var ancestor = parent;
		while (ancestor is not null)
		{
			if (ancestor is Control ancestorControl && ancestorControl.IsFocusable && ancestorControl.IsEnabled)
			{
				ancestorControl.StartBringIntoView();
				ancestorControl.Focus(FocusState.Keyboard);
				return;
			}

			ancestor = ancestor.GetParent() as UIElement;
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Shared: Announcement debounce/throttle
	// ──────────────────────────────────────────────────────────────

	public void AnnouncePolite(string text)
	{
		_pendingPoliteContent = text;
		var oldTimer = _politeDebounceTimer;
		_politeDebounceTimer = new Timer(_ => FlushPoliteAnnouncement(), null, AnnouncementDebounceMs, Timeout.Infinite);
		oldTimer?.Dispose();
	}

	public void AnnounceAssertive(string text)
	{
		_pendingAssertiveContent = text;
		var oldTimer = _assertiveDebounceTimer;
		_assertiveDebounceTimer = new Timer(_ => FlushAssertiveAnnouncement(), null, AnnouncementDebounceMs, Timeout.Infinite);
		oldTimer?.Dispose();
	}

	private void FlushPoliteAnnouncement()
	{
		var content = _pendingPoliteContent;
		_pendingPoliteContent = null;
		_politeDebounceTimer?.Dispose();
		_politeDebounceTimer = null;

		if (string.IsNullOrEmpty(content))
		{
			return;
		}

		var now = Environment.TickCount64;
		if (now - _politeThrottleTimestamp < PoliteThrottleMs)
		{
			var remaining = PoliteThrottleMs - (int)(now - _politeThrottleTimestamp);
			_pendingPoliteContent = content;
			_politeDebounceTimer = new Timer(_ => FlushPoliteAnnouncement(), null, remaining, Timeout.Infinite);
			return;
		}

		if (string.Equals(content, _lastAnnouncedPoliteContent, StringComparison.Ordinal))
		{
			return;
		}

		_politeThrottleTimestamp = now;
		_lastAnnouncedPoliteContent = content;
		AnnounceOnPlatform(content, assertive: false);
	}

	private void FlushAssertiveAnnouncement()
	{
		var content = _pendingAssertiveContent;
		_pendingAssertiveContent = null;
		_assertiveDebounceTimer?.Dispose();
		_assertiveDebounceTimer = null;

		if (string.IsNullOrEmpty(content))
		{
			return;
		}

		var now = Environment.TickCount64;
		if (now - _assertiveThrottleTimestamp < AssertiveThrottleMs)
		{
			var remaining = AssertiveThrottleMs - (int)(now - _assertiveThrottleTimestamp);
			_pendingAssertiveContent = content;
			_assertiveDebounceTimer = new Timer(_ => FlushAssertiveAnnouncement(), null, remaining, Timeout.Infinite);
			return;
		}

		if (string.Equals(content, _lastAnnouncedAssertiveContent, StringComparison.Ordinal))
		{
			return;
		}

		_assertiveThrottleTimestamp = now;
		_lastAnnouncedAssertiveContent = content;
		AnnounceOnPlatform(content, assertive: true);
	}

	/// <summary>
	/// Clears the duplicate-suppression state so the next announcement always goes through.
	/// Call after modal close or other state resets.
	/// </summary>
	protected void ResetAnnouncementTracking()
	{
		_lastAnnouncedPoliteContent = null;
		_lastAnnouncedAssertiveContent = null;
	}
}
