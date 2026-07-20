#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
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

	private readonly object _announcementGate = new();
	private string? _pendingPoliteContent;
	private string? _pendingAssertiveContent;
	private WeakReference<UIElement>? _pendingPoliteSource;
	private WeakReference<UIElement>? _pendingAssertiveSource;
	private Timer? _politeDebounceTimer;
	private Timer? _assertiveDebounceTimer;
	private long _politeAnnouncementVersion;
	private long _assertiveAnnouncementVersion;
	private long _politeThrottleTimestamp;
	private long _assertiveThrottleTimestamp;
	private string? _lastAnnouncedPoliteContent;
	private string? _lastAnnouncedAssertiveContent;

	// Focus tracking
	private UIElement? _trackedFocusedElement;

	// Disposal state — guards pending dispatcher callbacks after the window closes.
	private volatile bool _isDisposed;

	// Tracks scroll-source elements (ScrollViewer / ScrollPresenter) we are subscribed to,
	// so descendant accessibility positions can be re-emitted when the scroll offset changes.
	// Keyed by Visual handle so removal can find the entry without holding a strong reference.
	private readonly System.Collections.Generic.Dictionary<nint, (ScrollViewer Source, EventHandler<ScrollViewerViewChangedEventArgs> Handler)> _scrollViewerSubscriptions = new();
	private readonly System.Collections.Generic.Dictionary<nint, (ScrollPresenter Source, Windows.Foundation.TypedEventHandler<ScrollPresenter, object> Handler)> _scrollPresenterSubscriptions = new();
	private readonly System.Collections.Generic.Stack<UIElement> _reemitStack = new();

	/// <summary>
	/// Whether this instance has been disposed. Pending dispatcher callbacks
	/// (e.g., structure-change coalescing, announcement flushers) must check
	/// this before running.
	/// </summary>
	protected bool IsDisposed => _isDisposed;

	// ──────────────────────────────────────────────────────────────
	//  Abstract: Platform state
	// ──────────────────────────────────────────────────────────────

	/// <summary>Whether accessibility is currently enabled and the tree is initialized.</summary>
	public abstract bool IsAccessibilityEnabled { get; }

	protected virtual bool ShouldInvalidateOnScroll => IsAccessibilityEnabled;

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
	//  Router entry points — called by AccessibilityRouter after it
	//  resolves a callback to this per-window instance.
	// ──────────────────────────────────────────────────────────────

	internal void RouteChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (_isDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

		OnChildAdded(parent, child, index);
	}

	internal void RouteChildRemoved(UIElement parent, UIElement child)
	{
		if (_isDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

		OnChildRemoved(parent, child);
	}

	// Hooks ScrollViewer / ScrollPresenter scroll events. Without this, scrolling a
	// container does not invalidate the cached bounding rectangles of its descendants
	// in the native accessibility tree, leaving screen-reader highlight rectangles at
	// pre-scroll positions on macOS and WASM. Platform OnChildAdded implementations
	// must call this for every element they add (including those pruned via
	// AccessibilityView=Raw, since their descendants still need the subscription).
	protected void TrySubscribeScrollSource(UIElement element)
	{
		if (element is ScrollViewer scrollViewer)
		{
			var handle = scrollViewer.Visual.Handle;
			if (_scrollViewerSubscriptions.ContainsKey(handle))
			{
				return;
			}

			void Handler(object? sender, ScrollViewerViewChangedEventArgs e) => OnScrollSourceChanged(scrollViewer);
			scrollViewer.ViewChanged += Handler;
			_scrollViewerSubscriptions[handle] = (scrollViewer, Handler);
		}
		else if (element is ScrollPresenter scrollPresenter)
		{
			var handle = scrollPresenter.Visual.Handle;
			if (_scrollPresenterSubscriptions.ContainsKey(handle))
			{
				return;
			}

			void Handler(ScrollPresenter sender, object e) => OnScrollSourceChanged(scrollPresenter);
			scrollPresenter.ViewChanged += Handler;
			_scrollPresenterSubscriptions[handle] = (scrollPresenter, Handler);
		}
	}

	protected void TryUnsubscribeScrollSource(UIElement element)
	{
		if (element is ScrollViewer scrollViewer)
		{
			var handle = scrollViewer.Visual.Handle;
			if (_scrollViewerSubscriptions.Remove(handle, out var subscription))
			{
				subscription.Source.ViewChanged -= subscription.Handler;
			}
		}
		else if (element is ScrollPresenter scrollPresenter)
		{
			var handle = scrollPresenter.Visual.Handle;
			if (_scrollPresenterSubscriptions.Remove(handle, out var subscription))
			{
				subscription.Source.ViewChanged -= subscription.Handler;
			}
		}
	}

	// Walks descendants of the scrolled element and re-emits OnSizeOrOffsetChanged
	// for each ContainerVisual. The platform overrides recompute positions via
	// UIElement.GetTransform, which composes ancestor scroll offsets and transforms.
	private void OnScrollSourceChanged(UIElement scrollSource)
	{
		if (_isDisposed || !ShouldInvalidateOnScroll)
		{
			return;
		}

		ReemitDescendantPositions(scrollSource);
	}

	private void ReemitDescendantPositions(UIElement element)
	{
		_reemitStack.Clear();
		foreach (var child in element.GetChildren())
		{
			_reemitStack.Push(child);
		}

		while (_reemitStack.Count > 0)
		{
			var child = _reemitStack.Pop();
			if (child.Visual is ContainerVisual childVisual)
			{
				OnSizeOrOffsetChanged(childVisual);
			}

			foreach (var descendant in child.GetChildren())
			{
				_reemitStack.Push(descendant);
			}
		}
	}

	internal void RouteVisualOffsetOrSizeChanged(Microsoft.UI.Composition.Visual visual)
	{
		if (_isDisposed || !ShouldInvalidateOnScroll)
		{
			return;
		}

		OnSizeOrOffsetChanged(visual);

		// Raise automatic property changes (IsOffscreen, IsEnabled, Name, ItemStatus)
		// so accessibility clients get notified when elements move on/off screen.
		// Use CachedAutomationPeer to avoid creating peers eagerly on every layout pass,
		// which would prevent elements from being garbage collected.
		if (visual is ContainerVisual containerVisual
			&& containerVisual.Owner?.Target is UIElement owner)
		{
			owner.CachedAutomationPeer?.RaiseAutomaticPropertyChanges(firePropertyChangedEvents: true);
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Shared: IAutomationPeerListener — Property change routing
	// ──────────────────────────────────────────────────────────────

	public virtual void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (_isDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

		NotifyPropertyChangedEventCore(peer, automationProperty, oldValue, newValue);
	}

	/// <summary>
	/// Routes property changes to platform-specific update methods.
	/// Subclasses can override to add platform-specific property handling
	/// (e.g., WASM roving tabindex on selection change) by calling base first.
	/// </summary>
	protected virtual void NotifyPropertyChangedEventCore(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		// Match WinUI/Win32: route property changes to the peer's EventsSource so that
		// ListItem/TabItem/TreeItem changes are attributed to the data peer the client sees, not
		// the raw container peer. ResolveProviderPeer returns `this` for every other peer, so this
		// is a no-op outside those three control types. (Win32 does the equivalent via
		// FindExistingProviderForPeer(peer, resolveEventsSource: true).)
		var sourcePeer = peer;
		peer = peer.ResolveProviderPeer(resolveEventsSource: true);

		if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, sourcePeer, out var element))
		{
			var label = AriaMapper.ResolveLabel(peer);
			UpdateName(element.Visual.Handle, peer, label ?? newValue as string);
		}
		else if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			UpdateToggleState(element.Visual.Handle, peer, (ToggleState)newValue);
		}
		else if (automationProperty == RangeValuePatternIdentifiers.ValueProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			if (newValue is double doubleValue)
			{
				UpdateRangeValue(element.Visual.Handle, peer, doubleValue);
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.ValueProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			UpdateTextValue(
				element.Visual.Handle,
				peer.IsPassword() ? null : newValue as string);
		}
		else if (automationProperty == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			var isExpanded = newValue is ExpandCollapseState state &&
				(state == ExpandCollapseState.Expanded || state == ExpandCollapseState.PartiallyExpanded);
			UpdateExpandCollapseState(element.Visual.Handle, isExpanded);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsEnabledProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			UpdateEnabled(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.HelpTextProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			UpdateHelpText(element.Visual.Handle, newValue as string);
		}
		else if (automationProperty == AutomationElementIdentifiers.HeadingLevelProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			var level = ConvertHeadingLevel(newValue);
			UpdateHeadingLevel(element.Visual.Handle, level);
		}
		else if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			UpdateSelected(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.LandmarkTypeProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			var landmarkType = newValue is AutomationLandmarkType lt ? lt : AutomationLandmarkType.None;
			UpdateLandmark(element.Visual.Handle, AriaMapper.GetLandmarkRole(landmarkType));
		}
		else if (automationProperty == RangeValuePatternIdentifiers.MinimumProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			if (newValue is double min &&
				peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				UpdateRangeBounds(element.Visual.Handle, min, rangeProvider.Maximum);
			}
		}
		else if (automationProperty == RangeValuePatternIdentifiers.MaximumProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			if (newValue is double max &&
				peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				UpdateRangeBounds(element.Visual.Handle, rangeProvider.Minimum, max);
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.IsReadOnlyProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			UpdateIsReadOnly(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsKeyboardFocusableProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
		{
			UpdateFocusable(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsOffscreenProperty &&
			TryGetPeerOwner(peer, sourcePeer, out element))
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
		if (_isDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

		var sourcePeer = peer;
		peer = peer.ResolveProviderPeer(resolveEventsSource: true);

		switch (eventId)
		{
			case AutomationEvents.AutomationFocusChanged when TryGetPeerOwner(peer, sourcePeer, out var focusedElement):
				SetNativeFocus(focusedElement.Visual.Handle);
				TrackFocusedElement(focusedElement);
				break;

			case AutomationEvents.TextPatternOnTextChanged:
			case AutomationEvents.TextEditTextChanged:
			case AutomationEvents.ConversionTargetChanged:
				if (TryGetPeerOwner(peer, sourcePeer, out var textElement) &&
					peer.GetPattern(PatternInterface.Value) is IValueProvider textValueProvider)
				{
					UpdateTextValue(
						textElement.Visual.Handle,
						peer.IsPassword() ? null : textValueProvider.Value);
				}
				break;

			case AutomationEvents.StructureChanged:
			case AutomationEvents.SelectionPatternOnInvalidated:
			case AutomationEvents.WindowOpened:
			case AutomationEvents.WindowClosed:
			case AutomationEvents.MenuOpened:
			case AutomationEvents.MenuClosed:
			case AutomationEvents.LayoutInvalidated:
			case AutomationEvents.AsyncContentLoaded:
				OnNativeStructureChanged();
				break;

			case AutomationEvents.SelectionItemPatternOnElementAddedToSelection:
			case AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection:
			case AutomationEvents.SelectionItemPatternOnElementSelected:
				if (TryGetPeerOwner(peer, sourcePeer, out var selectedElement) &&
					peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
				{
					UpdateSelected(selectedElement.Visual.Handle, selectionItemProvider.IsSelected);
				}
				break;

			case AutomationEvents.LiveRegionChanged:
				UIElement? liveRegionElement = null;
				if (TryGetPeerOwner(peer, sourcePeer, out liveRegionElement) &&
					IsBlockedByActiveModal(liveRegionElement))
				{
					break;
				}

				var announcement = peer.GetName();
				if (!string.IsNullOrEmpty(announcement))
				{
					if (peer.GetLiveSetting() == AutomationLiveSetting.Assertive)
					{
						AnnounceAssertive(announcement, liveRegionElement);
					}
					else if (peer.GetLiveSetting() == AutomationLiveSetting.Polite)
					{
						AnnouncePolite(announcement, liveRegionElement);
					}
				}
				break;
		}
	}

	public virtual void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
	{
		if (_isDisposed || !IsAccessibilityEnabled || string.IsNullOrEmpty(displayString))
		{
			return;
		}

		var sourcePeer = peer;
		peer = peer.ResolveProviderPeer(resolveEventsSource: true);
		UIElement? sourceElement = null;
		if (TryGetPeerOwner(peer, sourcePeer, out sourceElement) &&
			IsBlockedByActiveModal(sourceElement))
		{
			return;
		}

		var assertive = notificationProcessing == AutomationNotificationProcessing.ImportantAll ||
						notificationProcessing == AutomationNotificationProcessing.ImportantMostRecent;

		if (assertive)
		{
			AnnounceAssertive(displayString, sourceElement);
		}
		else
		{
			AnnouncePolite(displayString, sourceElement);
		}
	}

	public virtual void NotifyTextEditTextChangedEvent(AutomationPeer peer, Microsoft.UI.Xaml.Automation.AutomationTextEditChangeType changeType, System.Collections.Generic.IReadOnlyList<string> changedData)
	{
		if (_isDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

		peer = peer.ResolveProviderPeer(resolveEventsSource: true);
		if (TryGetPeerOwner(peer, out var element) &&
			peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
		{
			UpdateTextValue(
				element.Visual.Handle,
				peer.IsPassword() ? null : valueProvider.Value);
		}
	}

	public virtual void NotifyInvalidatePeer(AutomationPeer peer)
	{
		if (_isDisposed || !IsAccessibilityEnabled)
		{
			return;
		}

		// WinUI's CAutomationPeer::InvalidatePeer schedules RaiseAutomaticPropertyChanges,
		// which re-evaluates IsEnabled/IsOffscreen/Name/ItemStatus and raises PropertyChanged
		// for any that changed. Platform subclasses that maintain a provider-level children
		// cache (Win32) additionally drop it; see Win32Accessibility.NotifyInvalidatePeer.
		peer.RaiseAutomaticPropertyChanges(firePropertyChangedEvents: true);
	}

	public virtual bool ListenerExistsHelper(AutomationEvents eventId)
		=> !_isDisposed && IsAccessibilityEnabled;

	public virtual void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
		=> NotifyAutomationEvent(peer, eventId);

	// ──────────────────────────────────────────────────────────────
	//  Shared: Utility methods
	// ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Extracts the owner UIElement from an AutomationPeer.
	/// Works for both FrameworkElementAutomationPeer and ItemAutomationPeer.
	/// </summary>
	internal static bool TryGetPeerOwner(AutomationPeer peer, [NotNullWhen(true)] out UIElement? owner)
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

			// Unrealized items have no native node to update. Targeting the parent
			// collection would apply item state to the wrong accessibility element.
		}

		owner = null;
		return false;
	}

	internal static bool TryGetPeerOwner(
		AutomationPeer peer,
		AutomationPeer fallbackPeer,
		[NotNullWhen(true)] out UIElement? owner)
		=> TryGetPeerOwner(peer, out owner) ||
			(!ReferenceEquals(peer, fallbackPeer) && TryGetPeerOwner(fallbackPeer, out owner));

	/// <summary>
	/// Determines whether a UIElement should be accessibility-focusable.
	/// Checks focusability, AccessibilityView, role overrides, and peer existence.
	/// </summary>
	protected static bool IsAccessibilityFocusable(DependencyObject dependencyObject, bool isFocusable)
	{
		var hasRoleOverride = dependencyObject is UIElement roleElement &&
			!string.IsNullOrEmpty(AutomationProperties.GetRoleOverride(roleElement));

		if (!isFocusable && !hasRoleOverride)
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
		UIElement? current = element;
		while (current is not null)
		{
			if (current.Visual.Handle == ancestorHandle)
			{
				return true;
			}
			current = current.GetUIElementAdjustedParentInternal();
		}
		return false;
	}

	protected virtual bool IsBlockedByActiveModal(UIElement element)
		=> false;

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
		=> AnnouncePolite(text, source: null);

	private void AnnouncePolite(string text, UIElement? source)
	{
		lock (_announcementGate)
		{
			if (_isDisposed)
			{
				return;
			}

			_pendingPoliteContent = text;
			_pendingPoliteSource = source is null ? null : new WeakReference<UIElement>(source);
			var version = ++_politeAnnouncementVersion;
			_politeDebounceTimer?.Dispose();
			_politeDebounceTimer = new Timer(
				_ => FlushPoliteAnnouncement(version),
				null,
				AnnouncementDebounceMs,
				Timeout.Infinite);
		}
	}

	public void AnnounceAssertive(string text)
		=> AnnounceAssertive(text, source: null);

	private void AnnounceAssertive(string text, UIElement? source)
	{
		lock (_announcementGate)
		{
			if (_isDisposed)
			{
				return;
			}

			_pendingAssertiveContent = text;
			_pendingAssertiveSource = source is null ? null : new WeakReference<UIElement>(source);
			var version = ++_assertiveAnnouncementVersion;
			_assertiveDebounceTimer?.Dispose();
			_assertiveDebounceTimer = new Timer(
				_ => FlushAssertiveAnnouncement(version),
				null,
				AnnouncementDebounceMs,
				Timeout.Infinite);
		}
	}

	private void FlushPoliteAnnouncement(long version)
	{
		string? content;
		WeakReference<UIElement>? source;
		lock (_announcementGate)
		{
			if (_isDisposed || version != _politeAnnouncementVersion)
			{
				return;
			}

			content = _pendingPoliteContent;
			source = _pendingPoliteSource;
			_pendingPoliteContent = null;
			_pendingPoliteSource = null;
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
				_pendingPoliteSource = source;
				_politeDebounceTimer = new Timer(
					_ => FlushPoliteAnnouncement(version),
					null,
					remaining,
					Timeout.Infinite);
				return;
			}

			if (string.Equals(content, _lastAnnouncedPoliteContent, StringComparison.Ordinal))
			{
				// U+FEFF makes repeated text lexically distinct so the native
				// accessibility service does not suppress the announcement.
				content += "\uFEFF";
			}

			_politeThrottleTimestamp = now;
			_lastAnnouncedPoliteContent = content;
		}

		AnnounceOnPlatformIfAllowed(content, assertive: false, source);
	}

	private void FlushAssertiveAnnouncement(long version)
	{
		string? content;
		WeakReference<UIElement>? source;
		lock (_announcementGate)
		{
			if (_isDisposed || version != _assertiveAnnouncementVersion)
			{
				return;
			}

			content = _pendingAssertiveContent;
			source = _pendingAssertiveSource;
			_pendingAssertiveContent = null;
			_pendingAssertiveSource = null;
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
				_pendingAssertiveSource = source;
				_assertiveDebounceTimer = new Timer(
					_ => FlushAssertiveAnnouncement(version),
					null,
					remaining,
					Timeout.Infinite);
				return;
			}

			if (string.Equals(content, _lastAnnouncedAssertiveContent, StringComparison.Ordinal))
			{
				// U+FEFF makes repeated text lexically distinct without changing
				// the text spoken by the native accessibility service.
				content += "\uFEFF";
			}

			_assertiveThrottleTimestamp = now;
			_lastAnnouncedAssertiveContent = content;
		}

		AnnounceOnPlatformIfAllowed(content, assertive: true, source);
	}

	private void AnnounceOnPlatformIfAllowed(
		string content,
		bool assertive,
		WeakReference<UIElement>? source)
	{
		if (source is null)
		{
			AnnounceOnPlatform(content, assertive);
			return;
		}

		if (!source.TryGetTarget(out var element))
		{
			return;
		}

		void Announce()
		{
			if (!_isDisposed && !IsBlockedByActiveModal(element))
			{
				AnnounceOnPlatform(content, assertive);
			}
		}

		if (element.DispatcherQueue.HasThreadAccess)
		{
			Announce();
		}
		else
		{
			_ = element.DispatcherQueue.TryEnqueue(Announce);
		}
	}

	/// <summary>
	/// Clears the duplicate-suppression state so the next announcement always goes through.
	/// Call after modal close or other state resets.
	/// </summary>
	protected void ResetAnnouncementTracking()
	{
		lock (_announcementGate)
		{
			_lastAnnouncedPoliteContent = null;
			_lastAnnouncedAssertiveContent = null;
		}
	}

	// ──────────────────────────────────────────────────────────────
	//  Disposal — per-window lifecycle
	// ──────────────────────────────────────────────────────────────

	/// <summary>
	/// Ordered teardown for this per-window accessibility instance:
	///   1. Mark as disposed so coalesced dispatcher callbacks no-op.
	///   2. Invoke subclass platform-specific teardown (provider cleanup, native context destroy).
	///   3. Dispose debouncer timers.
	///   4. Untrack any focused element.
	/// Idempotent — calling more than once is a no-op.
	/// </summary>
	public virtual void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		try
		{
			DisposeCore();
		}
		finally
		{
			lock (_announcementGate)
			{
				_politeDebounceTimer?.Dispose();
				_politeDebounceTimer = null;
				_assertiveDebounceTimer?.Dispose();
				_assertiveDebounceTimer = null;
				_pendingPoliteContent = null;
				_pendingAssertiveContent = null;
				_pendingPoliteSource = null;
				_pendingAssertiveSource = null;
				_politeAnnouncementVersion++;
				_assertiveAnnouncementVersion++;
			}

			foreach (var subscription in _scrollViewerSubscriptions.Values)
			{
				subscription.Source.ViewChanged -= subscription.Handler;
			}
			_scrollViewerSubscriptions.Clear();

			foreach (var subscription in _scrollPresenterSubscriptions.Values)
			{
				subscription.Source.ViewChanged -= subscription.Handler;
			}
			_scrollPresenterSubscriptions.Clear();

			UntrackFocusedElement();
		}
	}

	/// <summary>
	/// Subclass hook for platform-specific disposal (Win32: provider cleanup;
	/// macOS: native context destroy). Called once after <see cref="IsDisposed"/>
	/// has been set to true. Implementations must be idempotent.
	/// </summary>
	protected abstract void DisposeCore();
}
