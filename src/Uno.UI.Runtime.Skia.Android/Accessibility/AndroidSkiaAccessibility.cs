#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Java.Lang;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Runtime.Skia.Android;

internal sealed class AndroidSkiaAccessibility : SkiaAccessibilityBase
{
	private const int MaxRecordedEventCount = 256;

	private readonly WeakReference<XamlRoot> _xamlRootRef;
	private readonly Handler _mainHandler;
	private readonly Runnable _flushInvalidationsRunnable;
	private readonly Runnable _flushRootRunnable;
	private UnoExploreByTouchHelper? _helper;

	// Per-window event log; consumed by tests via AndroidAccessibilityEventsAccessor.
	private readonly List<AccessibilityNativeEventRecord> _eventLog = new();
	private bool _recordEvents;
	private Func<XamlRoot, AccessibilityNativeEventRecord[]?>? _registeredEventsAccessor;
	private Action<XamlRoot>? _registeredClearEventsAction;

	// Handles that received property updates before their virtual ID was assigned;
	// cleared when the ID is allocated so the node gets invalidated on first query.
	private readonly HashSet<nint> _pendingDirtyHandles = new();

	// Handles batched for the next-looper-iteration InvalidateVirtualView flush.
	private HashSet<nint> _pendingInvalidationHandles = new();
	private HashSet<nint> _flushingInvalidationHandles = new();
	private bool _invalidationFlushScheduled;

	// Guards the single root-invalidation flush for a burst of structure changes.
	private bool _rootInvalidationScheduled;
	private readonly HashSet<UIElement> _pendingScrollSubscriptionRoots = new();

	internal AndroidSkiaAccessibility(XamlRoot xamlRoot)
	{
		_xamlRootRef = new WeakReference<XamlRoot>(xamlRoot);
		_mainHandler = new Handler(Looper.MainLooper!);
		_flushInvalidationsRunnable = new Runnable(FlushPendingInvalidations);
		_flushRootRunnable = new Runnable(FlushRootInvalidation);
	}

	private void Trace(string message)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"[A11y] {message}");
		}
	}

	// Always true: tree is available for TalkBack and UIAutomator regardless of
	// service state; unsolicited events are gated in AnnounceOnPlatform.
	public override bool IsAccessibilityEnabled => true;

	protected override bool ShouldInvalidateOnScroll
		=> _helper?.IsTouchExplorationEnabled == true;

	protected override bool IsBlockedByActiveModal(UIElement element)
		=> _helper?.IsBlockedByActiveModal(element) is true;

	internal UIElement? RootElement
		=> _xamlRootRef.TryGetTarget(out var root)
			? root.Content as UIElement ?? root.VisualTree.RootElement
			: null;

	internal void Configure(UnoExploreByTouchHelper helper)
	{
		if (ReferenceEquals(_helper, helper))
		{
			return;
		}

		Detach();
		_helper = helper;
		helper.Initialize(this);
		Trace("Configured Android accessibility adapter.");

		// Subscribe scroll sources already in the tree so descendant bounds stay
		// current when the user scrolls before any child-add events fire.
		if (RootElement is { } root)
		{
			SubscribeScrollSourcesInSubtree(root);
		}

		_registeredEventsAccessor = GetEventRecords;
		AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor = _registeredEventsAccessor;

		_registeredClearEventsAction = ClearEventRecords;
		AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction = _registeredClearEventsAction;
	}

	internal void Detach()
	{
		_helper?.ClearAdapter();
		_helper = null;

		// Clear batched state so a posted flush cannot target a replacement helper.
		_pendingDirtyHandles.Clear();
		_pendingInvalidationHandles.Clear();
		_flushingInvalidationHandles.Clear();
		_pendingScrollSubscriptionRoots.Clear();
		_eventLog.Clear();
		_recordEvents = false;
		_invalidationFlushScheduled = false;
		_rootInvalidationScheduled = false;

		if (AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor == _registeredEventsAccessor)
		{
			AccessibilityPeerHelper.AndroidAccessibilityEventsAccessor = null;
		}

		if (AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction == _registeredClearEventsAction)
		{
			AccessibilityPeerHelper.AndroidClearAccessibilityEventsAction = null;
		}

		_registeredEventsAccessor = null;
		_registeredClearEventsAction = null;
		Trace("Detached Android accessibility adapter.");
	}

	// Event log -----------------------------------------------------------------

	private AccessibilityNativeEventRecord[]? GetEventRecords(XamlRoot xamlRoot)
	{
		if (!_xamlRootRef.TryGetTarget(out var root) || !ReferenceEquals(root, xamlRoot))
		{
			return null;
		}

		_recordEvents = true;
		return _eventLog.ToArray();
	}

	private void ClearEventRecords(XamlRoot xamlRoot)
	{
		if (_xamlRootRef.TryGetTarget(out var r) && ReferenceEquals(r, xamlRoot))
		{
			_recordEvents = true;
			_eventLog.Clear();
		}
	}

	private void RecordEvent(
		AccessibilityNativeEventKind kind,
		string? name = null,
		string? text = null)
	{
		if (!_recordEvents)
		{
			return;
		}

		if (_eventLog.Count >= MaxRecordedEventCount)
		{
			_eventLog.RemoveAt(0);
		}

		var record = new AccessibilityNativeEventRecord(kind, name, text);
		_eventLog.Add(record);
		Trace($"Recorded native event {record.Kind}.");
	}

	// Coalesced invalidation ----------------------------------------------------

	// Called by UnoExploreByTouchHelper when a handle that was absent from the
	// virtual-ID registry is assigned an ID (either new or rehydrated after removal).
	internal void OnVirtualIdAssigned(nint handle)
	{
		Trace($"Virtual ID assigned for handle {handle}.");
		if (_pendingDirtyHandles.Remove(handle))
		{
			_pendingInvalidationHandles.Add(handle);
			ScheduleInvalidationFlush();
		}
	}

	// Queues handle for the next flush. Handles without a virtual ID are parked in
	// _pendingDirtyHandles and promoted to the flush queue by OnVirtualIdAssigned.
	private void ScheduleInvalidation(nint handle)
	{
		if (_helper is null)
		{
			return;
		}

		if (!_helper.HasVirtualId(handle))
		{
			_pendingDirtyHandles.Add(handle);
			Trace($"Deferred invalidation for unregistered handle {handle}.");
			return;
		}

		_pendingInvalidationHandles.Add(handle);
		ScheduleInvalidationFlush();
	}

	private void ScheduleInvalidationFlush()
	{
		if (_invalidationFlushScheduled)
		{
			return;
		}

		_invalidationFlushScheduled = true;
		_mainHandler.Post(_flushInvalidationsRunnable);
	}

	private void FlushPendingInvalidations()
	{
		// Return if Detach() cancelled this flush since it was posted.
		if (!_invalidationFlushScheduled || IsDisposed)
		{
			return;
		}

		_invalidationFlushScheduled = false;

		(_pendingInvalidationHandles, _flushingInvalidationHandles) =
			(_flushingInvalidationHandles, _pendingInvalidationHandles);
		_pendingInvalidationHandles.Clear();
		Trace($"Flushing {_flushingInvalidationHandles.Count} targeted invalidation(s).");

		foreach (var handle in _flushingInvalidationHandles)
		{
			if (_helper?.InvalidateForHandle(handle) is true)
			{
				RecordEvent(AccessibilityNativeEventKind.NodeInvalidated);
			}
		}

		_flushingInvalidationHandles.Clear();
	}

	// Queues one root-level invalidation for the current looper iteration.
	// Any number of structure mutations within a single synchronous burst share
	// the same flush, producing exactly one InvalidateRoot call and one
	// StructureChanged record.
	private void ScheduleRootInvalidation()
	{
		if (_rootInvalidationScheduled || _helper is null)
		{
			return;
		}

		_rootInvalidationScheduled = true;
		_mainHandler.Post(_flushRootRunnable);
	}

	private void FlushRootInvalidation()
	{
		// Return if Detach() cancelled this flush since it was posted, or if a
		// replacement Configure() has not yet re-scheduled its own flush.
		if (!_rootInvalidationScheduled || IsDisposed || _helper is null)
		{
			return;
		}

		_rootInvalidationScheduled = false;
		foreach (var root in _pendingScrollSubscriptionRoots)
		{
			if (_xamlRootRef.TryGetTarget(out var xamlRoot) &&
				ReferenceEquals(root.XamlRoot, xamlRoot))
			{
				SubscribeScrollSourcesInSubtree(root);
			}
		}
		_pendingScrollSubscriptionRoots.Clear();
		_helper.InvalidateAccessibilityRoot();
		Trace("Flushed root structure invalidation.");
		RecordEvent(AccessibilityNativeEventKind.StructureChanged);
	}

	// Tree mutations ------------------------------------------------------------

	protected override void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		Trace($"Child added at index {index?.ToString() ?? "end"}.");
		_helper?.MarkAccessibilityTreeDirty();
		_pendingScrollSubscriptionRoots.Add(child);
		ScheduleRootInvalidation();
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		Trace($"Child removed for handle {child.Visual.Handle}.");
		_helper?.MarkAccessibilityTreeDirty();
		_pendingScrollSubscriptionRoots.RemoveWhere(
			pendingRoot => IsDescendantOf(pendingRoot, child.Visual.Handle));
		UnsubscribeScrollSourcesInSubtree(child);

		// Prune immediately so TalkBack cannot query the removed subtree between
		// now and the deferred root flush.
		if (_helper is not null)
		{
			PruneHandlesForSubtree(child);
			_helper.PruneForRemovedElement(child);
		}

		ScheduleRootInvalidation();
	}

	private void PruneHandlesForSubtree(UIElement element)
	{
		var handle = element.Visual.Handle;
		_pendingDirtyHandles.Remove(handle);
		_pendingInvalidationHandles.Remove(handle);
		foreach (var child in element.GetChildren())
		{
			PruneHandlesForSubtree(child);
		}
	}

	// Walks the subtree and subscribes every ScrollViewer/ScrollPresenter so that
	// scroll-offset changes re-emit descendant positions via OnSizeOrOffsetChanged.
	// TrySubscribeScrollSource is idempotent; duplicate calls for the same source
	// are no-ops because the base guards on _scrollViewerSubscriptions.
	private void SubscribeScrollSourcesInSubtree(UIElement element)
	{
		TrySubscribeScrollSource(element);
		foreach (var child in element.GetChildren())
		{
			SubscribeScrollSourcesInSubtree(child);
		}
	}

	// Mirrors SubscribeScrollSourcesInSubtree; called when a subtree is removed.
	// Base Dispose handles any remaining subscriptions on window close.
	private void UnsubscribeScrollSourcesInSubtree(UIElement element)
	{
		TryUnsubscribeScrollSource(element);
		foreach (var child in element.GetChildren())
		{
			UnsubscribeScrollSourcesInSubtree(child);
		}
	}

	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		if (_helper is not null &&
			visual is ContainerVisual containerVisual &&
			containerVisual.Owner?.Target is UIElement element)
		{
			ScheduleInvalidation(element.Visual.Handle);
		}
	}

	// Property updates — all delegate to coalesced invalidation ----------------

	protected override void UpdateName(nint handle, AutomationPeer peer, string? label)
		=> ScheduleInvalidation(handle);

	protected override void UpdateToggleState(nint handle, AutomationPeer peer, ToggleState newState)
		=> ScheduleInvalidation(handle);

	protected override void UpdateRangeValue(nint handle, AutomationPeer peer, double value)
		=> ScheduleInvalidation(handle);

	protected override void UpdateRangeBounds(nint handle, double min, double max)
		=> ScheduleInvalidation(handle);

	protected override void UpdateTextValue(nint handle, string? value)
		=> ScheduleInvalidation(handle);

	protected override void UpdateExpandCollapseState(nint handle, bool isExpanded)
		=> ScheduleInvalidation(handle);

	protected override void UpdateEnabled(nint handle, bool enabled)
		=> ScheduleInvalidation(handle);

	protected override void UpdateSelected(nint handle, bool selected)
		=> ScheduleInvalidation(handle);

	protected override void UpdateHelpText(nint handle, string? helpText)
		=> ScheduleInvalidation(handle);

	protected override void UpdateHeadingLevel(nint handle, int level)
		=> ScheduleInvalidation(handle);

	protected override void UpdateLandmark(nint handle, string? landmarkRole)
		=> ScheduleInvalidation(handle);

	protected override void UpdateIsReadOnly(nint handle, bool isReadOnly)
		=> ScheduleInvalidation(handle);

	protected override void UpdateFocusable(nint handle, bool focusable)
		=> ScheduleInvalidation(handle);

	protected override void UpdateIsOffscreen(nint handle, bool isOffscreen)
		=> ScheduleInvalidation(handle);

	// Invoked once per property change event. Base routes known properties to
	// Update* methods; the override also covers unmatched properties (AutomationId,
	// FullDescription, etc.) by calling ScheduleInvalidation for the resolved owner.
	// NodeInvalidated is recorded in FlushPendingInvalidations so N rapid changes to
	// the same node produce exactly one native signal and one record.
	protected override void NotifyPropertyChangedEventCore(
		AutomationPeer peer,
		AutomationProperty automationProperty,
		object oldValue,
		object newValue)
	{
		Trace($"Property change routed for {automationProperty}.");
		base.NotifyPropertyChangedEventCore(peer, automationProperty, oldValue, newValue);

		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		if (TryGetPeerOwner(resolvedPeer, peer, out var owner))
		{
			ScheduleInvalidation(owner.Visual.Handle);
		}

		if (automationProperty == AutomationElementIdentifiers.IsDialogProperty ||
			automationProperty == AutomationElementIdentifiers.AutomationIdProperty)
		{
			_helper?.MarkAccessibilityTreeDirty();
			if (automationProperty == AutomationElementIdentifiers.IsDialogProperty)
			{
				ScheduleRootInvalidation();
			}
		}
	}

	// Automation events ---------------------------------------------------------

	// Overrides NotifyTextEditTextChangedEvent (the app-level TextEdit API) to
	// send TYPE_VIEW_TEXT_CHANGED and record TextChanged. This path carries the
	// change type and changed-data spans that NotifyAutomationEvent does not.
	public override void NotifyTextEditTextChangedEvent(
		AutomationPeer peer,
		AutomationTextEditChangeType changeType,
		IReadOnlyList<string> changedData)
	{
		Trace($"Text edit event routed with change type {changeType}.");
		base.NotifyTextEditTextChangedEvent(peer, changeType, changedData);

		if (IsDisposed || !IsAccessibilityEnabled || _helper is null)
		{
			return;
		}

		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		if (TryGetPeerOwner(resolvedPeer, peer, out var owner) &&
			_helper.SendTextChangedEventForHandle(owner.Visual.Handle))
		{
			RecordEvent(AccessibilityNativeEventKind.TextChanged, name: resolvedPeer.GetName());
		}
	}

	// Translates WinUI automation events into targeted Android accessibility
	// signals and records them. TextEditTextChanged is handled by
	// NotifyTextEditTextChangedEvent above; base already calls OnNativeStructureChanged
	// → InvalidateRoot for StructureChanged; duplicate signals for base-handled
	// paths are intentionally avoided.
	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		Trace($"Automation event routed: {eventId}.");
		base.NotifyAutomationEvent(peer, eventId);

		if (IsDisposed || !IsAccessibilityEnabled || _helper is null)
		{
			return;
		}

		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		TryGetPeerOwner(resolvedPeer, peer, out var owner);
		var name = resolvedPeer.GetName();

		switch (eventId)
		{
			// TextPatternOnTextChanged: generic text change (base also calls UpdateTextValue).
			// TextEditTextChanged is handled separately via NotifyTextEditTextChangedEvent.
			case AutomationEvents.TextPatternOnTextChanged:
				if (owner is not null && _helper.SendTextChangedEventForHandle(owner.Visual.Handle))
				{
					RecordEvent(AccessibilityNativeEventKind.TextChanged, name: name);
				}

				break;

			case AutomationEvents.TextPatternOnTextSelectionChanged:
				if (owner is not null && _helper.SendTextSelectionChangedEventForHandle(owner.Visual.Handle))
				{
					RecordEvent(AccessibilityNativeEventKind.SelectionChanged, name: name);
				}

				break;

			case AutomationEvents.SelectionItemPatternOnElementSelected:
			case AutomationEvents.SelectionItemPatternOnElementAddedToSelection:
			case AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection:
			case AutomationEvents.SelectionPatternOnInvalidated:
				if (owner is not null && _helper.SendSelectionChangedEventForHandle(owner.Visual.Handle))
				{
					RecordEvent(AccessibilityNativeEventKind.SelectionChanged, name: name);
				}

				break;

			// The base routes structural events through OnNativeStructureChanged.
			case AutomationEvents.StructureChanged:
			case AutomationEvents.LayoutInvalidated:
			case AutomationEvents.AsyncContentLoaded:
				break;

			// Window and overlay transitions: send TYPE_WINDOW_STATE_CHANGED.
			// SendWindowStateChangedEvent always returns true (host-view fallback).
			case AutomationEvents.WindowOpened:
			case AutomationEvents.WindowClosed:
			case AutomationEvents.MenuOpened:
			case AutomationEvents.MenuClosed:
			case AutomationEvents.ToolTipOpened:
			case AutomationEvents.ToolTipClosed:
				_helper.SendWindowStateChangedEvent(owner?.Visual.Handle ?? default);
				RecordEvent(AccessibilityNativeEventKind.WindowChanged);
				break;

			// IME conversion: the active composition target has changed its text.
			case AutomationEvents.ConversionTargetChanged:
				if (owner is not null && _helper.SendTextChangedEventForHandle(owner.Visual.Handle))
				{
					RecordEvent(AccessibilityNativeEventKind.TextChanged, name: name);
				}

				break;

			// Invoke: emit a click signal so TalkBack can announce the activation.
			case AutomationEvents.InvokePatternOnInvoked:
				if (owner is not null)
				{
					_helper.SendClickEventForHandle(owner.Visual.Handle);
				}

				break;
		}
	}

	// Focus and structure -------------------------------------------------------

	protected override void SetNativeFocus(nint handle)
	{
		Trace($"Native focus requested for handle {handle}.");
		_helper?.SetFocusForHandle(handle);
	}

	protected override void OnNativeStructureChanged()
	{
		_helper?.MarkAccessibilityTreeDirty();
		ScheduleRootInvalidation();
	}

	// Announces via View.AnnounceForAccessibility (TYPE_ANNOUNCEMENT) and records.
	// Must run on the main thread; dispatches asynchronously if called off-thread.
	protected override void AnnounceOnPlatform(string text, bool assertive)
	{
		if (_helper is null)
		{
			return;
		}

		if (!IsOnMainThread())
		{
			_mainHandler.Post(
				new Runnable(() => AnnounceOnPlatform(text, assertive)));
			return;
		}

		_helper.SendAnnouncement(text);
		RecordEvent(AccessibilityNativeEventKind.Announcement, text: text);
	}

	// Action execution ----------------------------------------------------------

	internal bool PerformActionForElement(UIElement element, AccessibilityNativeActionRequest request)
	{
		Trace($"Native action requested: {request.Action}.");
		if (IsOnMainThread())
		{
			var result = _helper?.ExecuteAction(element, request) ?? false;
			Trace($"Native action {request.Action} completed with result {result}.");
			return result;
		}

		if (IsDisposed || _helper is null)
		{
			return false;
		}

		_mainHandler.Post(new Runnable(() => _helper?.ExecuteAction(element, request)));
		Trace($"Native action {request.Action} queued for the main thread.");
		return true;
	}

	private static bool IsOnMainThread() => Looper.MyLooper() == Looper.MainLooper;

	// Disposal ------------------------------------------------------------------

	protected override void DisposeCore()
	{
		Trace("Disposing Android accessibility adapter.");
		Detach();
		_eventLog.Clear();
	}
}