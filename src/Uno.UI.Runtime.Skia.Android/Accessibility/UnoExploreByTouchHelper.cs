#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Android.OS;
using Android.Views;
using Android.Views.Accessibility;
using AndroidX.Core.View.Accessibility;
using AndroidX.CustomView.Widget;
using Java.Lang;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI;

namespace Uno.UI.Runtime.Skia.Android;

// Virtual-view accessibility bridge for Skia-on-Android.
// Enumerates the live WinUI automation-peer tree via the adapter's XamlRoot,
// assigns stable weak virtual IDs, and projects each peer's properties into
// Android AccessibilityNodeInfoCompat objects.
internal sealed class UnoExploreByTouchHelper : ExploreByTouchHelper
{
	private readonly View _host;
	private AndroidSkiaAccessibility? _adapter;
	private AccessibilityNodeProviderCompat? _innerNodeProvider;
	private AccessibilityNodeProviderCompat? _filteringNodeProvider;

	// Package name cached on first use for ViewIdResourceName qualification.
	private string? _packageName;
	private readonly int[] _hostLocationOnScreen = new int[2];

	// Persistent virtual-ID registry. Survives tree rebuilds; IDs are never reused.
	// _elementToId (CWT) is the stable identity store: kept alive as long as the
	// owner DependencyObject is alive.  _idToWeakElement and _handleToId are
	// reverse/handle maps: they are pruned at the end of every scan to contain only
	// IDs present in the current peer tree, then rehydrated when an owner reappears.

	private readonly ConditionalWeakTable<DependencyObject, VirtualIdBox> _elementToId = new();
	private readonly Dictionary<int, WeakReference<DependencyObject>> _idToWeakElement = new();
	private readonly Dictionary<nint, int> _handleToId = new();
	private int _nextId;

	private sealed class VirtualIdBox
	{
		public int Value { get; init; }
	}

	// Per-scan caches rebuilt by GetVisibleVirtualViews.
	// _orderedIds is in peer-tree order and drives GetVisibleVirtualViews output.

	private readonly List<int> _orderedIds = new();
	private readonly HashSet<int> _orderedIdSet = new();
	private readonly Dictionary<int, int> _virtualIdByNodeIndex = new();
	private readonly Dictionary<int, int> _parentVirtualIdByVirtualId = new();
	private readonly Dictionary<int, List<int>> _childrenByVirtualId = new();
	private readonly Dictionary<int, int> _rowIndexByVirtualId = new();
	private readonly Dictionary<int, Dictionary<int, AccessibilityNativeActionRequest>> _customActionsByVirtualId = new();
	private readonly Dictionary<string, string> _resourceSegmentByAutomationId = new(StringComparer.Ordinal);
	private readonly Dictionary<string, string> _automationIdByResourceSegment = new(StringComparer.Ordinal);
	private IReadOnlyList<AccessibilityPeerNode> _cachedPeerTree = Array.Empty<AccessibilityPeerNode>();
	private UIElement? _cachedPeerTreeRoot;
	private UIElement? _cachedModalElement;
	private int _cachedModalNodeIndex = -1;
	private bool _peerTreeDirty = true;
	private bool _visibleTreeBuilt;

	// Action hook delegate stored for reference-equality check in ClearAdapter.
	private Func<UIElement, AccessibilityNativeActionRequest, bool>? _registeredActionAccessor;
	private Func<int, int, bool>? _registeredRawActionAccessor;

	// Focus hook delegates.
	private Func<UIElement, bool>? _registeredFocusAccessor;
	private Func<XamlRoot, object?>? _registeredFocusedNodeAccessor;

	// Tracks the virtual ID that was most recently given native accessibility focus
	// by this process, or -1 (HostId) when none is tracked.
	// Updated in RequestNativeFocusById; cleared when the element is pruned from the tree.
	private int _nativeFocusedId = -1;
	private int _nativeKeyboardFocusedId = -1;

	// Re-entry guard shared between the XAML -> native and native -> XAML focus paths.
	// Prevents AutomationFocusChanged -> SetNativeFocus -> OnPopulateEventForVirtualView
	// -> XAML focus change -> AutomationFocusChanged infinite loops.
	private bool _settingNativeFocus;

	// Stable Android constants used for native focus requests.
	// ACTION_ACCESSIBILITY_FOCUS = 0x40 per AccessibilityNodeInfoCompat (all API levels).
	private const int ActionFocusId = 0x1;
	private const int ActionClearFocusId = 0x2;
	private const int ActionAccessibilityFocusId = 0x40;
	private const int ActionClearAccessibilityFocusId = 0x80;
	private const int FocusInput = 1;
	private const int FocusAccessibility = 2;

	// TYPE_VIEW_ACCESSIBILITY_FOCUSED = 0x00008000, used to send the focus event directly
	// when no accessibility service is active (e.g., unit-test environments).
	private const int TypeViewAccessibilityFocused = 0x00008000;

	// Android accessibility event type constants (stable per AOSP).
	private const int TypeViewClicked = 0x1;
	private const int TypeViewSelected = 0x4;
	private const int TypeViewTextChanged = 0x10;
	private const int TypeWindowStateChanged = 0x20;
	private const int TypeViewTextSelectionChanged = 0x2000;

	// Bundle argument keys (stable per the Android/AndroidX specification).
	private const string ActionArgumentSetTextCharsequenceKey = "action_argument_set_text_charsequence";
	private const string ActionArgumentProgressValueKey = "android.view.accessibility.action.ARGUMENT_PROGRESS_VALUE";

	// Cached Android action IDs avoid repeated JNI property access in the hot action path.
	// Standard actions have stable non-zero IDs; 0 indicates "not available" (no match).
	private static readonly int s_actionClickId =
		AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionClick?.Id ?? 0;
	private static readonly int s_actionExpandId =
		AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionExpand?.Id ?? 0;
	private static readonly int s_actionCollapseId =
		AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionCollapse?.Id ?? 0;
	private static readonly int s_actionScrollForwardId =
		AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollForward?.Id ?? 0;
	private static readonly int s_actionScrollBackwardId =
		AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollBackward?.Id ?? 0;
	private static readonly int s_actionSetTextId =
		AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionSetText?.Id ?? 0;
	private static readonly int s_actionDismissId =
		AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionDismiss?.Id ?? 0;

	private const int CustomActionChangeViewBase = 0x01010000;
	private const int CustomActionZoomIn = 0x01020001;
	private const int CustomActionZoomOut = 0x01020002;
	private const int CustomActionScrollIntoView = 0x01030001;
	private const int CustomActionRealize = 0x01030002;
	private const int CustomActionDockBase = 0x01040000;

	// ActionSetProgress was added in AndroidX.Core 1.0 / API 24; access defensively.
	private static readonly int s_actionSetProgressId = GetActionSetProgressId();

	public UnoExploreByTouchHelper(View host) : base(host)
	{
		_host = host;
	}

	public override AccessibilityNodeProviderCompat? GetAccessibilityNodeProvider(View? host)
	{
		var innerProvider = base.GetAccessibilityNodeProvider(host);
		if (innerProvider is null)
		{
			return null;
		}

		if (!ReferenceEquals(_innerNodeProvider, innerProvider))
		{
			_innerNodeProvider = innerProvider;
			_filteringNodeProvider = new FilteringNodeProvider(this, innerProvider);
		}

		return _filteringNodeProvider;
	}

	private sealed class FilteringNodeProvider : AccessibilityNodeProviderCompat
	{
		private readonly UnoExploreByTouchHelper _owner;
		private readonly AccessibilityNodeProviderCompat _innerProvider;

		internal FilteringNodeProvider(
			UnoExploreByTouchHelper owner,
			AccessibilityNodeProviderCompat innerProvider)
		{
			_owner = owner;
			_innerProvider = innerProvider;
		}

		public override void AddExtraDataToAccessibilityNodeInfo(
			int virtualViewId,
			AccessibilityNodeInfoCompat? info,
			string? extraDataKey,
			Bundle? arguments)
		{
			if (_owner.IsVirtualViewAvailable(virtualViewId))
			{
				_innerProvider.AddExtraDataToAccessibilityNodeInfo(
					virtualViewId,
					info,
					extraDataKey,
					arguments);
			}
		}

		public override AccessibilityNodeInfoCompat? CreateAccessibilityNodeInfo(int virtualViewId)
			=> _owner.IsVirtualViewAvailable(virtualViewId)
				? _innerProvider.CreateAccessibilityNodeInfo(virtualViewId)
				: null;

		public override IList<AccessibilityNodeInfoCompat>? FindAccessibilityNodeInfosByText(
			string? text,
			int virtualViewId)
			=> _owner.IsVirtualViewAvailable(virtualViewId)
				? _innerProvider.FindAccessibilityNodeInfosByText(text, virtualViewId)
				: Array.Empty<AccessibilityNodeInfoCompat>();

		public override AccessibilityNodeInfoCompat? FindFocus(int focus)
		{
			var focusedId = focus == FocusAccessibility
				? _owner._nativeFocusedId
				: focus == FocusInput
					? _owner._nativeKeyboardFocusedId
					: -1;
			return focusedId < 0 || _owner.IsVirtualViewAvailable(focusedId)
				? _innerProvider.FindFocus(focus)
				: null;
		}

		public override bool PerformAction(int virtualViewId, int action, Bundle? arguments)
		{
			if (!_owner.IsVirtualViewAvailable(virtualViewId) ||
				!_innerProvider.PerformAction(virtualViewId, action, arguments))
			{
				return false;
			}

			return true;
		}
	}

	private bool IsVirtualViewAvailable(int virtualViewId)
	{
		if (virtualViewId == HostId)
		{
			return true;
		}

		EnsureVisibleTreeBuilt();
		return _orderedIdSet.Contains(virtualViewId);
	}

	internal bool IsTouchExplorationEnabled
		=> _host.Context?.GetSystemService(global::Android.Content.Context.AccessibilityService) is
			AccessibilityManager { IsEnabled: true, IsTouchExplorationEnabled: true };

	internal void InvalidateAccessibilityRoot()
	{
		MarkAccessibilityTreeDirty();
		InvalidateRoot();
	}

	internal void MarkAccessibilityTreeDirty()
	{
		_peerTreeDirty = true;
		_visibleTreeBuilt = false;
		_cachedPeerTreeRoot = null;
		_cachedPeerTree = Array.Empty<AccessibilityPeerNode>();
		_cachedModalElement = null;
		_cachedModalNodeIndex = -1;
	}

	private IReadOnlyList<AccessibilityPeerNode> GetCurrentPeerTree()
	{
		var root = GetRootElement();
		if (root is null)
		{
			_cachedPeerTreeRoot = null;
			_cachedPeerTree = Array.Empty<AccessibilityPeerNode>();
			_cachedModalElement = null;
			_cachedModalNodeIndex = -1;
			_peerTreeDirty = false;
			return _cachedPeerTree;
		}

		if (_peerTreeDirty || !ReferenceEquals(root, _cachedPeerTreeRoot))
		{
			_visibleTreeBuilt = false;
			_cachedPeerTreeRoot = root;
			_cachedPeerTree = AccessibilityPeerHelper.GetPeerTree(root);
			UpdateCachedModal(_cachedPeerTree);
			RebuildAutomationIdResourceSegments(_cachedPeerTree);
			_peerTreeDirty = false;
		}

		return _cachedPeerTree;
	}

	private void UpdateCachedModal(IReadOnlyList<AccessibilityPeerNode> tree)
	{
		_cachedModalElement = null;
		_cachedModalNodeIndex = -1;
		for (var i = 0; i < tree.Count; i++)
		{
			var node = tree[i];
			if (node.Owner is UIElement owner && IsModalPeer(node.ProviderPeer))
			{
				_cachedModalElement = owner;
				_cachedModalNodeIndex = i;
			}
		}
	}

	private void RebuildAutomationIdResourceSegments(IReadOnlyList<AccessibilityPeerNode> tree)
	{
		var automationIds = tree
			.Select(node => node.Owner is UIElement owner
				? AutomationProperties.GetAutomationId(owner)
				: null)
			.Where(id => !string.IsNullOrEmpty(id))
			.Distinct(StringComparer.Ordinal)
			.Cast<string>()
			.ToArray();

		foreach (var automationId in automationIds.Where(IsValidResourceSegment))
		{
			AssignResourceSegment(automationId, automationId);
		}

		foreach (var automationId in automationIds.Where(id => !IsValidResourceSegment(id)))
		{
			AssignResourceSegment(automationId, NormalizeToResourceSegment(automationId));
		}
	}

	private void AssignResourceSegment(string automationId, string preferredSegment)
	{
		if (_resourceSegmentByAutomationId.ContainsKey(automationId))
		{
			return;
		}

		var segment = preferredSegment;
		if (_automationIdByResourceSegment.TryGetValue(segment, out var existingId) &&
			!string.Equals(existingId, automationId, StringComparison.Ordinal))
		{
			var collisionSuffix = Fnv1a32(automationId).ToString("x8");
			var collisionBase = $"{segment}_{collisionSuffix}";
			segment = collisionBase;
			var collisionIndex = 2;
			while (_automationIdByResourceSegment.ContainsKey(segment))
			{
				segment = $"{collisionBase}_{collisionIndex++}";
			}
		}

		_resourceSegmentByAutomationId[automationId] = segment;
		_automationIdByResourceSegment[segment] = automationId;
	}

	private static bool IsModalPeer(AutomationPeer peer)
	{
		if (peer.IsDialog())
		{
			return true;
		}

		var windowProvider = peer.GetPattern(PatternInterface.Window) as IWindowProvider;
		if (windowProvider is null &&
			peer is PopupAutomationPeer { Owner: Popup { IsOpen: true } } popupPeer)
		{
			windowProvider = popupPeer;
		}

		if (windowProvider is null)
		{
			return false;
		}

		try
		{
			return windowProvider.IsModal;
		}
		catch (ElementNotAvailableException)
		{
			return false;
		}
		catch (InvalidOperationException)
		{
			return false;
		}
	}

	private static int GetActionSetProgressId()
	{
		try
		{
			return AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionSetProgress?.Id ?? -1;
		}
		catch (Java.Lang.NoSuchFieldError)
		{
			return -1;
		}
		catch (Java.Lang.NoSuchMethodError)
		{
			return -1;
		}
	}

	// Adapter lifecycle ---------------------------------------------------------

	// Connects this helper to its per-window accessibility adapter and registers
	// the AccessibilityPeerHelper hooks used by the test infrastructure.
	// Idempotent when called with the same adapter instance.
	internal void Initialize(AndroidSkiaAccessibility adapter)
	{
		if (ReferenceEquals(_adapter, adapter))
		{
			return;
		}

		_adapter = adapter;
		MarkAccessibilityTreeDirty();

		// Android's current Skia host supports one XamlRoot per process, so these
		// test hooks are single-slot. AppleUIKit uses a per-root registry because
		// its host can own multiple roots.
		// Register test-access hooks so runtime tests can inspect the live native
		// tree through Uno.UI without a direct reference to this assembly.
		AccessibilityPeerHelper.AndroidAccessibilityNodeAccessor = GetNodeForElement;
		AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor = GetVirtualIdForElement;
		AccessibilityPeerHelper.AndroidAllNodesForRootAccessor = GetAllNodesForRoot;
		AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor = GetSnapshotForElement;
		AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor = GetAllSnapshotsForRoot;
		AccessibilityPeerHelper.AndroidAccessibilityDiagnosticsAccessor = GetDiagnostics;

		// The focus accessor lets callers request native accessibility focus
		// for a specific element; focused-node accessor returns a snapshot of whatever
		// virtual view currently holds native accessibility focus.
		_registeredFocusAccessor = RequestNativeFocusForElement;
		AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor = _registeredFocusAccessor;

		_registeredFocusedNodeAccessor = GetFocusedNativeNode;
		AccessibilityPeerHelper.AndroidFocusedNativeNodeAccessor = _registeredFocusedNodeAccessor;

		// Register the action accessor. The delegate points to the adapter so that
		// Main-thread dispatch is enforced transparently for all callers.
		_registeredActionAccessor = adapter.PerformActionForElement;
		AccessibilityPeerHelper.AndroidAccessibilityActionAccessor = _registeredActionAccessor;

		_registeredRawActionAccessor = PerformRawAction;
		AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor = _registeredRawActionAccessor;
	}

	// Removes the adapter reference and clears the test-access hooks.
	internal void ClearAdapter()
	{
		if (AccessibilityPeerHelper.AndroidAccessibilityNodeAccessor == GetNodeForElement)
		{
			AccessibilityPeerHelper.AndroidAccessibilityNodeAccessor = null;
			AccessibilityPeerHelper.AndroidAccessibilityVirtualIdAccessor = null;
			AccessibilityPeerHelper.AndroidAllNodesForRootAccessor = null;
			AccessibilityPeerHelper.AndroidAccessibilityNodeSnapshotAccessor = null;
			AccessibilityPeerHelper.AndroidAllNodeSnapshotsForRootAccessor = null;
			AccessibilityPeerHelper.AndroidAccessibilityDiagnosticsAccessor = null;
		}

		if (AccessibilityPeerHelper.AndroidAccessibilityActionAccessor == _registeredActionAccessor)
		{
			AccessibilityPeerHelper.AndroidAccessibilityActionAccessor = null;
		}

		if (AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor == _registeredRawActionAccessor)
		{
			AccessibilityPeerHelper.AndroidAccessibilityRawActionAccessor = null;
		}

		if (AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor == _registeredFocusAccessor)
		{
			AccessibilityPeerHelper.AndroidAccessibilityFocusAccessor = null;
		}

		if (AccessibilityPeerHelper.AndroidFocusedNativeNodeAccessor == _registeredFocusedNodeAccessor)
		{
			AccessibilityPeerHelper.AndroidFocusedNativeNodeAccessor = null;
		}

		_registeredActionAccessor = null;
		_registeredRawActionAccessor = null;
		_registeredFocusAccessor = null;
		_registeredFocusedNodeAccessor = null;
		if (_nativeFocusedId >= 0)
		{
			ClearNativeAccessibilityFocus(_nativeFocusedId);
		}
		if (_nativeKeyboardFocusedId >= 0)
		{
			ClearNativeKeyboardFocus(_nativeKeyboardFocusedId);
		}
		_customActionsByVirtualId.Clear();
		_orderedIdSet.Clear();
		_cachedPeerTree = Array.Empty<AccessibilityPeerNode>();
		_cachedPeerTreeRoot = null;
		_cachedModalElement = null;
		_cachedModalNodeIndex = -1;
		_resourceSegmentByAutomationId.Clear();
		_automationIdByResourceSegment.Clear();
		MarkAccessibilityTreeDirty();
		_adapter = null;
	}

	private bool PerformRawAction(int virtualViewId, int action)
		=> GetAccessibilityNodeProvider(_host)?.PerformAction(
			virtualViewId,
			action,
			arguments: null) is true;

	// Targeted invalidation (called by AndroidSkiaAccessibility) ---------------

	internal bool InvalidateForHandle(nint handle)
	{
		if (_handleToId.TryGetValue(handle, out var id))
		{
			InvalidateVirtualView(id);
			return true;
		}

		return false;
	}

	// Returns true when the given native-visual handle already has a stable virtual ID.
	internal bool HasVirtualId(nint handle) => _handleToId.ContainsKey(handle);

	// Eagerly removes an element's virtual-ID entries from the reverse maps so that
	// TalkBack stops advertising it before the next full tree scan.
	// The CWT (_elementToId) is intentionally NOT cleared; stable IDs survive
	// temporary removal (e.g., a control recycled in a virtualized list).
	internal void PruneForRemovedElement(UIElement element)
	{
		PruneSubtree(element);
	}

	private void PruneSubtree(UIElement element)
	{
		var handle = element.Visual.Handle;
		if (_handleToId.Remove(handle, out var id))
		{
			if (_nativeFocusedId == id)
			{
				ClearNativeAccessibilityFocus(id);
			}
			if (_nativeKeyboardFocusedId == id)
			{
				ClearNativeKeyboardFocus(id);
			}

			_idToWeakElement.Remove(id);
			_customActionsByVirtualId.Remove(id);
		}

		foreach (var child in element.GetChildren())
		{
			PruneSubtree(child);
		}
	}

	// Event sending helpers — each returns true when the native signal was sent,
	// false when the handle had no virtual ID and no signal was dispatched.

	internal bool SendTextChangedEventForHandle(nint handle)
	{
		if (_handleToId.TryGetValue(handle, out var id))
		{
			SendEventForVirtualView(id, TypeViewTextChanged);
			return true;
		}

		return false;
	}

	internal bool SendSelectionChangedEventForHandle(nint handle)
	{
		if (_handleToId.TryGetValue(handle, out var id))
		{
			SendEventForVirtualView(id, TypeViewSelected);
			return true;
		}

		return false;
	}

	internal bool SendTextSelectionChangedEventForHandle(nint handle)
	{
		if (_handleToId.TryGetValue(handle, out var id))
		{
			SendEventForVirtualView(id, TypeViewTextSelectionChanged);
			return true;
		}

		return false;
	}

	// TYPE_WINDOW_STATE_CHANGED: uses the element's virtual ID when available,
	// falling back to HostId (-1) so the signal always reaches the framework.
	// Returns true unconditionally because the host-view fallback always succeeds.
	internal bool SendWindowStateChangedEvent(nint handle)
	{
		var id = _handleToId.TryGetValue(handle, out var vid) ? vid : HostId;
		SendEventForVirtualView(id, TypeWindowStateChanged);
		return true;
	}

	internal bool SendClickEventForHandle(nint handle)
	{
		if (_handleToId.TryGetValue(handle, out var id))
		{
			SendEventForVirtualView(id, TypeViewClicked);
			return true;
		}

		return false;
	}

	// Sends an Android accessibility announcement for the host view.
	internal void SendAnnouncement(string text) => _host.AnnounceForAccessibility(text);

	internal void SetFocusForHandle(nint handle)
	{
		EnsureVisibleTreeBuilt();
		if (_handleToId.TryGetValue(handle, out var id) &&
			_orderedIdSet.Contains(id) &&
			_idToWeakElement.TryGetValue(id, out var weakElement) &&
			weakElement.TryGetTarget(out var element) &&
			element is UIElement uiElement &&
			!IsBlockedByActiveModal(uiElement))
		{
			RequestNativeFocusById(id);
		}
	}

	// Requests native accessibility focus for the given virtual view ID.
	// Uses the real AccessibilityNodeProvider/ACTION_ACCESSIBILITY_FOCUS path so that
	// the focus event travels the same route as TalkBack-initiated focus.
	// Re-entry is blocked by _settingNativeFocus to break the
	// AutomationFocusChanged -> SetNativeFocus -> OnPopulateEventForVirtualView -> XAML loop.
	private bool RequestNativeFocusById(int id)
	{
		if (_settingNativeFocus)
		{
			return false;
		}

		_settingNativeFocus = true;
		try
		{
			// Always track the intended focus, even if the provider call fails
			// (e.g., no active accessibility service in a test environment).
			_nativeFocusedId = id;

			// Primary path: route through the real provider so ExploreByTouchHelper's
			// internal mFocusedVirtualViewId is updated and the proper
			// TYPE_VIEW_ACCESSIBILITY_FOCUSED event reaches the framework.
			var provider = GetAccessibilityNodeProvider(_host);
			if (provider?.PerformAction(id, ActionAccessibilityFocusId, null) is true)
			{
				return true;
			}

			// Fallback for environments where no accessibility service is active
			// (unit tests, emulator without TalkBack): send the event directly and
			// invalidate the node so it is re-read with the correct focus state.
			SendEventForVirtualView(id, TypeViewAccessibilityFocused);
			InvalidateVirtualView(id);
			return true;
		}
		finally
		{
			_settingNativeFocus = false;
		}
	}

	private void ClearNativeAccessibilityFocus(int id)
	{
		var provider = _innerNodeProvider ?? base.GetAccessibilityNodeProvider(_host);
		provider?.PerformAction(id, ActionClearAccessibilityFocusId, arguments: null);
		if (_nativeFocusedId == id)
		{
			_nativeFocusedId = -1;
		}
	}

	private void ClearNativeKeyboardFocus(int id)
	{
		var provider = _innerNodeProvider ?? base.GetAccessibilityNodeProvider(_host);
		provider?.PerformAction(id, ActionClearFocusId, arguments: null);
		if (_nativeKeyboardFocusedId == id)
		{
			_nativeKeyboardFocusedId = -1;
		}
	}

	// Root element resolution ---------------------------------------------------

	private UIElement? GetRootElement() => _adapter?.RootElement;

	// Package name --------------------------------------------------------------

	private string GetPackageName() => _packageName ??= _host.Context?.PackageName ?? string.Empty;

	// Virtual ID allocation and reverse-map maintenance -------------------------

	private int GetOrCreateVirtualId(DependencyObject element)
	{
		if (_elementToId.TryGetValue(element, out var box))
		{
			// Rehydrate the reverse and handle maps in case this element had been
			// pruned from them during a previous scan where it was off-tree.
			if (!_idToWeakElement.ContainsKey(box.Value))
			{
				_idToWeakElement[box.Value] = new WeakReference<DependencyObject>(element);
			}

			if (element is UIElement uiElement)
			{
				var handle = uiElement.Visual.Handle;
				bool wasAbsent = !_handleToId.ContainsKey(handle);
				_handleToId[handle] = box.Value;
				if (wasAbsent)
				{
					_adapter?.OnVirtualIdAssigned(handle);
				}
			}

				return box.Value;
		}

		var id = Interlocked.Increment(ref _nextId);
		var newBox = new VirtualIdBox { Value = id };
		_elementToId.Add(element, newBox);
		_idToWeakElement[id] = new WeakReference<DependencyObject>(element);

		if (element is UIElement ui)
		{
			_handleToId[ui.Visual.Handle] = id;
			_adapter?.OnVirtualIdAssigned(ui.Visual.Handle);
		}

		return id;
	}

	private bool TryGetElement(int virtualViewId, [NotNullWhen(true)] out DependencyObject? element)
	{
		if (_idToWeakElement.TryGetValue(virtualViewId, out var weakRef) &&
			weakRef.TryGetTarget(out element))
		{
			return true;
		}

		element = null;
		return false;
	}

	private bool TryGetVisibleElement(int virtualViewId, [NotNullWhen(true)] out DependencyObject? element)
	{
		EnsureVisibleTreeBuilt();
		if (_orderedIdSet.Contains(virtualViewId))
		{
			return TryGetElement(virtualViewId, out element);
		}

		element = null;
		return false;
	}

	private void EnsureVisibleTreeBuilt()
	{
		if (!_visibleTreeBuilt)
		{
			GetVisibleVirtualViews(new List<Integer>());
		}
	}

	// Registry pruning after each scan ------------------------------------------
	// Removes from _idToWeakElement and _handleToId every entry whose ID is either
	// GC-dead or was absent from the just-built peer tree.  The CWT (_elementToId)
	// is intentionally untouched so stable IDs survive temporary off-tree periods.
	// Also clears _nativeFocusedId when its element is pruned (stale-focus recovery).

	private void PruneRegistryToCurrentTree(HashSet<int> currentIds)
	{
		List<int>? toRemove = null;
		foreach (var (id, weakRef) in _idToWeakElement)
		{
			if (!weakRef.TryGetTarget(out _) || !currentIds.Contains(id))
			{
				(toRemove ??= new List<int>()).Add(id);
			}
		}

		if (toRemove is not null)
		{
			// If the natively-focused node is being pruned, clear the tracked ID
			// so GetFocusedNativeNode returns null rather than a stale snapshot.
			if (_nativeFocusedId >= 0 && toRemove.Contains(_nativeFocusedId))
			{
				ClearNativeAccessibilityFocus(_nativeFocusedId);
			}
			if (_nativeKeyboardFocusedId >= 0 && toRemove.Contains(_nativeKeyboardFocusedId))
			{
				ClearNativeKeyboardFocus(_nativeKeyboardFocusedId);
			}

			foreach (var id in toRemove)
			{
				_idToWeakElement.Remove(id);
				_customActionsByVirtualId.Remove(id);
			}
		}

		List<nint>? staleHandles = null;
		foreach (var (handle, id) in _handleToId)
		{
			if (!_idToWeakElement.ContainsKey(id))
			{
				(staleHandles ??= new List<nint>()).Add(handle);
			}
		}

		if (staleHandles is not null)
		{
			foreach (var handle in staleHandles)
			{
				_handleToId.Remove(handle);
			}
		}
	}

	// ExploreByTouchHelper overrides --------------------------------------------

	// Intercepts accessibility events sent for virtual views so we can track when
	// native accessibility focus moves to a virtual view (either from our own
	// SetFocusForHandle call or from an external TalkBack gesture).
	// When focus arrives from outside (TalkBack explore-by-touch), we synchronise
	// XAML keyboard focus if the element is keyboard-focusable, using the
	// _settingNativeFocus guard to break the potential
	// XAML-focus-changed -> SetNativeFocus -> OnPopulateEventForVirtualView loop.
	protected override void OnPopulateEventForVirtualView(int virtualViewId, AccessibilityEvent e)
	{
		base.OnPopulateEventForVirtualView(virtualViewId, e);

		if ((int)e.EventType != TypeViewAccessibilityFocused)
		{
			return;
		}

		if (!TryGetVisibleElement(virtualViewId, out var depObj) || depObj is not UIElement uiElement)
		{
			return;
		}

		_nativeFocusedId = virtualViewId;

		// If _settingNativeFocus is set, this event originated from our own
		// RequestNativeFocusById call; no need to sync XAML focus back.
		if (_settingNativeFocus)
		{
			return;
		}

		// Post to the main thread to avoid re-entrant calls within the event-send
		// stack frame, which can happen when this is triggered synchronously from
		// provider.PerformAction inside RequestNativeFocusById.
		new Handler(Looper.MainLooper!).Post(new Runnable(() =>
		{
			if (_settingNativeFocus)
			{
				return;
			}

			_settingNativeFocus = true;
			try
			{
				if (uiElement is Control { IsFocusable: true, IsEnabled: true } control
					&& control.Visibility == Visibility.Visible)
				{
					control.Focus(FocusState.Pointer);
				}
			}
			finally
			{
				_settingNativeFocus = false;
			}
		}));
	}

	protected override int GetVirtualViewAt(float x, float y)
	{
		var rootElement = GetRootElement();
		if (rootElement is null)
		{
			return HostId;
		}

		var logicalPoint = new Windows.Foundation.Point(x, y).PhysicalToLogicalPixels();
		var (element, _) = VisualTreeHelper.HitTest(logicalPoint, rootElement.XamlRoot?.VisualTree.RootElement);
		if (element is null)
		{
			return HostId;
		}

		DependencyObject? current = element;
		while (current is not null)
		{
			if (current is UIElement uiElement)
			{
				var accessibilityView = AutomationProperties.GetAccessibilityView(uiElement);
				if (accessibilityView != AccessibilityView.Raw)
				{
					var peer = uiElement.GetOrCreateAutomationPeer();
					if (peer is not null && (peer.IsControlElement() || peer.IsContentElement()))
					{
						return GetOrCreateVirtualId(uiElement);
					}
				}
			}

			current = (current as UIElement)?.GetUIElementAdjustedParentInternal();
		}

		return HostId;
	}

	protected override void OnVirtualViewKeyboardFocusChanged(int virtualViewId, bool hasFocus)
	{
		base.OnVirtualViewKeyboardFocusChanged(virtualViewId, hasFocus);
		if (!hasFocus)
		{
			if (_nativeKeyboardFocusedId == virtualViewId)
			{
				_nativeKeyboardFocusedId = -1;
			}
			return;
		}

		if (_settingNativeFocus ||
			!TryGetVisibleElement(virtualViewId, out var element) ||
			element is not Control { IsFocusable: true, IsEnabled: true } control)
		{
			ClearNativeKeyboardFocus(virtualViewId);
			return;
		}

		_nativeKeyboardFocusedId = virtualViewId;
		_settingNativeFocus = true;
		try
		{
			control.Focus(FocusState.Keyboard);
		}
		finally
		{
			_settingNativeFocus = false;
		}
	}

	protected override void GetVisibleVirtualViews(IList<Integer>? virtualViewIds)
	{
		if (virtualViewIds is null)
		{
			return;
		}

		var tree = GetCurrentPeerTree();
		if (_cachedPeerTreeRoot is null)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning("[A11y] No root element for accessibility tree.");
			}

			return;
		}

		if (_visibleTreeBuilt)
		{
			foreach (var id in _orderedIds)
			{
				virtualViewIds.Add(Integer.ValueOf(id));
			}

			return;
		}

		var modalNodeIndices = GetActiveModalNodeIndices(tree);

		_orderedIds.Clear();
		_orderedIdSet.Clear();
		_virtualIdByNodeIndex.Clear();
		_parentVirtualIdByVirtualId.Clear();
		_childrenByVirtualId.Clear();
		_rowIndexByVirtualId.Clear();

		// Assign stable virtual IDs to nodes that have a realized container (Owner != null).
		// Unrealized ItemAutomationPeer nodes (container not yet materialized) carry
		// Owner=null in the shared peer tree; they are intentionally skipped here so
		// no virtual ID is created and no parent-handle identity is borrowed.
		// PruneRegistryToCurrentTree below removes IDs for items that were realized on
		// a previous scan but are now unrealized; the CWT (_elementToId) retains the
		// stable ID so the same container element gets the same ID when re-realized.
		for (var i = 0; i < tree.Count; i++)
		{
			if (modalNodeIndices is not null && !modalNodeIndices.Contains(i))
			{
				continue;
			}

			var node = tree[i];
			if (node.Owner is DependencyObject owner)
			{
				var id = GetOrCreateVirtualId(owner);
				_orderedIds.Add(id);
				_orderedIdSet.Add(id);
				_virtualIdByNodeIndex[i] = id;
			}
			// node.Owner == null: unrealized virtual item — skip this slot entirely.
			// FindNearestMappedParentId walks past gaps, so realized siblings of an
			// unrealized item are still correctly nested under the container peer.
		}

		// Build parent/children maps now that all IDs are assigned.
		for (var i = 0; i < tree.Count; i++)
		{
			if (modalNodeIndices is not null && !modalNodeIndices.Contains(i))
			{
				continue;
			}

			var node = tree[i];
			if (!_virtualIdByNodeIndex.TryGetValue(i, out var id))
			{
				continue;
			}

			var parentId = FindNearestMappedParentId(tree, node.ParentIndex);
			_parentVirtualIdByVirtualId[id] = parentId;

			if (parentId != HostId)
			{
				if (!_childrenByVirtualId.TryGetValue(parentId, out var childList))
				{
					childList = new List<int>();
					_childrenByVirtualId[parentId] = childList;
				}

				_rowIndexByVirtualId[id] = childList.Count;
				childList.Add(id);
			}
		}

		if (_nativeFocusedId >= 0 && !_orderedIdSet.Contains(_nativeFocusedId))
		{
			ClearNativeAccessibilityFocus(_nativeFocusedId);
		}
		if (_nativeKeyboardFocusedId >= 0 && !_orderedIdSet.Contains(_nativeKeyboardFocusedId))
		{
			ClearNativeKeyboardFocus(_nativeKeyboardFocusedId);
		}

		// Prune IDs for elements no longer in the realized tree (de-realized containers
		// whose slot now carries Owner=null are absent from _orderedIds and therefore
		// removed here; the CWT entry stays so re-realization recovers the same ID).
		PruneRegistryToCurrentTree(_orderedIdSet);
		_visibleTreeBuilt = true;

		// Return IDs in peer-tree order.
		foreach (var id in _orderedIds)
		{
			virtualViewIds.Add(Integer.ValueOf(id));
		}
	}

	private int FindNearestMappedParentId(
		IReadOnlyList<AccessibilityPeerNode> tree,
		int? parentIndex)
	{
		while (parentIndex is { } index)
		{
			if (_virtualIdByNodeIndex.TryGetValue(index, out var parentId))
			{
				return parentId;
			}

			parentIndex = tree[index].ParentIndex;
		}

		return HostId;
	}

	protected override bool OnPerformActionForVirtualView(int virtualViewId, int action, Bundle? arguments)
	{
		if (!TryGetVisibleElement(virtualViewId, out var element) ||
			element is not UIElement uiElement)
		{
			return false;
		}

		var peer = uiElement.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			return false;
		}

		// Click / default activation: toggle > select > invoke (in priority order).
		if (action == s_actionClickId)
		{
			return AccessibilityPeerHelper.TryToggle(peer)
				|| AccessibilityPeerHelper.TryToggleSelection(peer)
				|| AccessibilityPeerHelper.TryInvokeDefaultAction(peer);
		}

		// Expand/collapse is advertised selectively by OnPopulateNodeForVirtualView.
		if (action == s_actionExpandId)
		{
			return AccessibilityPeerHelper.TryExpand(peer);
		}

		if (action == s_actionCollapseId)
		{
			return AccessibilityPeerHelper.TryCollapse(peer);
		}

		// Scroll forward / increment: range controls (Slider) take priority over scrollers.
		if (action == s_actionScrollForwardId)
		{
			var resolved = AccessibilityPeerHelper.ResolveProviderPeer(peer);
			if (resolved.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider)
			{
				return AccessibilityPeerHelper.TryIncrement(peer);
			}

			return TryScroll(peer, forward: true);
		}

		// Scroll backward / decrement.
		if (action == s_actionScrollBackwardId)
		{
			var resolved = AccessibilityPeerHelper.ResolveProviderPeer(peer);
			if (resolved.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider)
			{
				return AccessibilityPeerHelper.TryDecrement(peer);
			}

			return TryScroll(peer, forward: false);
		}

		// Set text (editable controls: TextBox, ComboBox edit, etc.)
		if (action == s_actionSetTextId)
		{
			var text = GetSetTextArgument(arguments);
			return text is not null && AccessibilityPeerHelper.TrySetValue(peer, text);
		}

		// Set progress / range value (Slider, ProgressBar, custom range controls).
		if (s_actionSetProgressId >= 0 && action == s_actionSetProgressId)
		{
			if (arguments?.ContainsKey(ActionArgumentProgressValueKey) == true)
			{
				var value = (double)arguments.GetFloat(ActionArgumentProgressValueKey);
				return AccessibilityPeerHelper.TrySetRangeValue(peer, value);
			}

			return false;
		}

		// Dismiss (windows, dialogs, flyouts).
		if (action == s_actionDismissId)
		{
			return AccessibilityPeerHelper.TryClose(peer);
		}

		if (_customActionsByVirtualId.TryGetValue(virtualViewId, out var customActions) &&
			customActions.TryGetValue(action, out var request))
		{
			return ExecuteAdvancedAction(peer, request);
		}

		return false;
	}

	private static bool TryScroll(AutomationPeer peer, bool forward)
	{
		var resolved = AccessibilityPeerHelper.ResolveProviderPeer(peer);
		if (resolved.GetPattern(PatternInterface.Scroll) is not IScrollProvider provider)
		{
			return false;
		}

		var amount = forward ? ScrollAmount.SmallIncrement : ScrollAmount.SmallDecrement;
		if (provider.VerticallyScrollable)
		{
			return AccessibilityPeerHelper.TryScroll(peer, ScrollAmount.NoAmount, amount);
		}

		return provider.HorizontallyScrollable &&
			AccessibilityPeerHelper.TryScroll(peer, amount, ScrollAmount.NoAmount);
	}

	private static bool ExecuteAdvancedAction(
		AutomationPeer peer,
		AccessibilityNativeActionRequest request)
	{
		if (!AccessibilityPeerHelper.ResolveProviderPeer(peer).IsEnabled())
		{
			return false;
		}

		return request.Action switch
		{
			AccessibilityNativeAction.ChangeView
				when double.IsFinite(request.Number) &&
					request.Number == System.Math.Truncate(request.Number)
				=> AccessibilityPeerHelper.TryChangeView(peer, (int)request.Number),
			AccessibilityNativeAction.ZoomIn
				=> AccessibilityPeerHelper.TryZoomByUnit(peer, ZoomUnit.SmallIncrement),
			AccessibilityNativeAction.ZoomOut
				=> AccessibilityPeerHelper.TryZoomByUnit(peer, ZoomUnit.SmallDecrement),
			AccessibilityNativeAction.Zoom
				=> AccessibilityPeerHelper.TryZoom(peer, request.Number),
			AccessibilityNativeAction.SetDockPosition
				when double.IsFinite(request.Number) &&
					request.Number == System.Math.Truncate(request.Number) &&
					request.Number is >= (int)DockPosition.Top and <= (int)DockPosition.None
				=> AccessibilityPeerHelper.TrySetDockPosition(peer, (DockPosition)(int)request.Number),
			AccessibilityNativeAction.SetWindowVisualState
				when double.IsFinite(request.Number) &&
					request.Number == System.Math.Truncate(request.Number) &&
					request.Number is >= (int)WindowVisualState.Normal and <= (int)WindowVisualState.Minimized
				=> AccessibilityPeerHelper.TrySetWindowVisualState(peer, (WindowVisualState)(int)request.Number),
			AccessibilityNativeAction.Move
				=> AccessibilityPeerHelper.TryMove(peer, request.Number, request.Number2),
			AccessibilityNativeAction.Resize
				=> AccessibilityPeerHelper.TryResize(peer, request.Number, request.Number2),
			AccessibilityNativeAction.Rotate
				=> AccessibilityPeerHelper.TryRotate(peer, request.Number),
			AccessibilityNativeAction.ScrollIntoView
				=> AccessibilityPeerHelper.TryScrollIntoView(peer),
			AccessibilityNativeAction.Realize
				=> AccessibilityPeerHelper.TryRealize(peer),
			_ => false,
		};
	}

	protected override void OnPopulateNodeForVirtualView(int virtualViewId, AccessibilityNodeInfoCompat node)
	{
		if (!TryGetVisibleElement(virtualViewId, out var element) ||
			element is not UIElement uiElement)
		{
			node.ContentDescription = "";
			node.Enabled = false;
			node.ClassName = "android.view.View";
			node.VisibleToUser = false;
			// ExploreByTouchHelper still requires parent bounds even though Android deprecated them.
#pragma warning disable CS0618
			node.SetBoundsInParent(new global::Android.Graphics.Rect(0, 0, 1, 1));
#pragma warning restore CS0618
			node.SetBoundsInScreen(new global::Android.Graphics.Rect(0, 0, 1, 1));
			return;
		}

		var peer = uiElement.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			node.ContentDescription = "";
			node.Enabled = false;
			node.ClassName = "android.view.View";
			node.VisibleToUser = false;
#pragma warning disable CS0618
			node.SetBoundsInParent(new global::Android.Graphics.Rect(0, 0, 1, 1));
#pragma warning restore CS0618
			node.SetBoundsInScreen(new global::Android.Graphics.Rect(0, 0, 1, 1));
			return;
		}

		var effectivePeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		var logicalRect = GetLogicalBounds(uiElement, effectivePeer);
		var physicalRect = logicalRect.LogicalToPhysicalPixels();
		var parentPhysicalRect = physicalRect;
		if (_parentVirtualIdByVirtualId.TryGetValue(virtualViewId, out var boundsParentId) &&
			boundsParentId != HostId &&
			TryGetElement(boundsParentId, out var parentElement) &&
			parentElement is UIElement parentUiElement)
		{
			var parentPeer = parentUiElement
				.GetOrCreateAutomationPeer()
				?.ResolveProviderPeer(resolveEventsSource: true);
			var parentLogicalRect = parentPeer is null
				? GetElementLogicalBounds(parentUiElement)
				: GetLogicalBounds(parentUiElement, parentPeer);
			parentPhysicalRect = new Windows.Foundation.Rect(
					logicalRect.X - parentLogicalRect.X,
					logicalRect.Y - parentLogicalRect.Y,
					logicalRect.Width,
					logicalRect.Height)
				.LogicalToPhysicalPixels();
		}

#pragma warning disable CS0618
		node.SetBoundsInParent(new global::Android.Graphics.Rect(
			(int)parentPhysicalRect.Left,
			(int)parentPhysicalRect.Top,
			(int)parentPhysicalRect.Right,
			(int)parentPhysicalRect.Bottom));
#pragma warning restore CS0618

		_host.GetLocationOnScreen(_hostLocationOnScreen);

		node.SetBoundsInScreen(new global::Android.Graphics.Rect(
			(int)physicalRect.Left + _hostLocationOnScreen[0],
			(int)physicalRect.Top + _hostLocationOnScreen[1],
			(int)physicalRect.Right + _hostLocationOnScreen[0],
			(int)physicalRect.Bottom + _hostLocationOnScreen[1]));

		var controlType = effectivePeer.GetAutomationControlType();
		var isPassword = effectivePeer.IsPassword();
		bool isEnabled = effectivePeer.IsEnabled();
		var invokeProvider =
			effectivePeer.GetPattern(PatternInterface.Invoke) as IInvokeProvider ??
			effectivePeer as IInvokeProvider;
		var toggleProvider =
			effectivePeer.GetPattern(PatternInterface.Toggle) as IToggleProvider ??
			effectivePeer as IToggleProvider;
		var selectionItemProvider =
			effectivePeer.GetPattern(PatternInterface.SelectionItem) as ISelectionItemProvider ??
			effectivePeer as ISelectionItemProvider;

		// Core semantics (no password text leakage).
		node.ContentDescription = effectivePeer.GetName() ?? "";
		node.Enabled = isEnabled;
		node.Password = isPassword;
		node.Heading = effectivePeer.GetHeadingLevel() != AutomationHeadingLevel.None;
		node.HintText = effectivePeer.GetHelpText() ?? "";
		node.Focusable = effectivePeer.IsKeyboardFocusable();
		node.Focused =
			effectivePeer.HasKeyboardFocus() ||
			_nativeKeyboardFocusedId == virtualViewId;

		// AutomationId: normalize to a valid Android resource name segment, expose the
		// original unmodified value in UniqueId and Extras for machine-readable lookup.
		var automationId = AutomationProperties.GetAutomationId(uiElement);
		if (!string.IsNullOrEmpty(automationId))
		{
			var segment = _resourceSegmentByAutomationId.TryGetValue(automationId, out var mappedSegment)
				? mappedSegment
				: NormalizeToResourceSegment(automationId);
			node.ViewIdResourceName = $"{GetPackageName()}:id/{segment}";
			node.UniqueId = automationId;
			node.Extras?.PutString("uno.a11y.automationid", automationId);
		}

		// Activation — suppressed when disabled.
		var isClickable = isEnabled
			&& (invokeProvider is not null ||
				toggleProvider is not null ||
				selectionItemProvider is not null);
		if (isClickable)
		{
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionClick);
		}

		node.Clickable = isClickable;

		if (toggleProvider is not null)
		{
			var checkState = toggleProvider.ToggleState switch
			{
				ToggleState.On => 1,
				ToggleState.Indeterminate => 2,
				_ => 0,
			};
			if (!AccessibilityNodeInfoCompatJni.SetChecked(node, checkState) && checkState == 2)
			{
				node.Extras?.PutInt("uno.a11y.checked_state", checkState);
			}
			node.Checkable = true;
		}

		// Selection state — always visible, not an action.
		if (selectionItemProvider is not null)
		{
			node.Selected = isEnabled
				? selectionItemProvider.IsSelected
				: uiElement is SelectorItem { IsSelected: true };
		}

		// ExpandCollapse — suppressed when disabled.
		if (isEnabled && effectivePeer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandProvider)
		{
			var ecState = expandProvider.ExpandCollapseState;
			if (ecState is ExpandCollapseState.Collapsed or ExpandCollapseState.PartiallyExpanded)
			{
				node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionExpand);
			}

			if (ecState is ExpandCollapseState.Expanded or ExpandCollapseState.PartiallyExpanded)
			{
				node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionCollapse);
			}
		}

		// Range controls.
		if (effectivePeer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
		{
			SetRangeInfo(node, rangeProvider);

			if (!rangeProvider.IsReadOnly && isEnabled)
			{
				node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollForward);
				node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollBackward);

				if (s_actionSetProgressId >= 0
					&& AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionSetProgress is { } setProgressAction)
				{
					node.AddAction(setProgressAction);
				}
			}
		}
		else if (isEnabled
			&& effectivePeer.GetPattern(PatternInterface.Scroll) is IScrollProvider scrollProvider
			&& (scrollProvider.HorizontallyScrollable || scrollProvider.VerticallyScrollable))
		{
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollForward);
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionScrollBackward);
		}

		// Text.
		SetTextInfo(node, effectivePeer, controlType, isPassword, isEnabled);

		// Collection / collection-item.
		SetCollectionInfo(node, effectivePeer, controlType);
		SetCollectionItemInfo(node, effectivePeer, controlType, uiElement, virtualViewId);

		// Dismiss — suppressed when disabled.
		if (isEnabled && effectivePeer.GetPattern(PatternInterface.Window) is not null)
		{
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionDismiss);
		}

		AddCustomActions(node, virtualViewId, effectivePeer, isEnabled);

		// Class name for native role inference.
		node.ClassName = controlType switch
		{
			AutomationControlType.Button => "android.widget.Button",
			AutomationControlType.CheckBox => "android.widget.CheckBox",
			AutomationControlType.ComboBox => "android.widget.Spinner",
			AutomationControlType.DataGrid => "android.widget.GridView",
			AutomationControlType.DataItem => "android.view.View",
			AutomationControlType.Edit => "android.widget.EditText",
			AutomationControlType.Group => "android.view.ViewGroup",
			AutomationControlType.Header => "android.view.ViewGroup",
			AutomationControlType.HeaderItem => "android.view.View",
			AutomationControlType.Image => "android.widget.ImageView",
			AutomationControlType.List => "android.widget.ListView",
			AutomationControlType.ListItem => "android.view.View",
			AutomationControlType.Menu => "android.widget.LinearLayout",
			AutomationControlType.MenuItem => "android.widget.TextView",
			AutomationControlType.Pane => "android.widget.FrameLayout",
			AutomationControlType.ProgressBar => "android.widget.ProgressBar",
			AutomationControlType.RadioButton => "android.widget.RadioButton",
			AutomationControlType.ScrollBar => "android.widget.ScrollView",
			AutomationControlType.Slider => "android.widget.SeekBar",
			AutomationControlType.Tab => "android.widget.TabWidget",
			AutomationControlType.TabItem => "android.view.View",
			AutomationControlType.Text => "android.widget.TextView",
			AutomationControlType.Tree => "android.widget.ExpandableListView",
			AutomationControlType.TreeItem => "android.view.View",
			_ => "android.view.View",
		};

		// Parent / children.
		if (_parentVirtualIdByVirtualId.TryGetValue(virtualViewId, out var parentId) &&
			parentId != HostId)
		{
			node.SetParent(_host, parentId);
		}

		if (_childrenByVirtualId.TryGetValue(virtualViewId, out var children))
		{
			foreach (var childId in children)
			{
				node.AddChild(_host, childId);
			}
		}

		// Relations are applied after parent/child maps are populated.
		SetRelations(node, effectivePeer, uiElement);

		// Additional metadata.
		SetMetadata(node, effectivePeer, uiElement);
		ApplyCulture(node, uiElement);
	}

	private static Windows.Foundation.Rect GetLogicalBounds(UIElement element, AutomationPeer peer)
	{
		var peerBounds = peer.GetBoundingRectangle();
		return HasFiniteBounds(peerBounds)
			? peerBounds
			: GetElementLogicalBounds(element);
	}

	private static Windows.Foundation.Rect GetElementLogicalBounds(UIElement element)
	{
		var transform = UIElement.GetTransform(from: element, to: null);
		return transform.Transform(
			new Windows.Foundation.Rect(
				default,
				new Windows.Foundation.Size(element.Visual.Size.X, element.Visual.Size.Y)));
	}

	private static bool HasFiniteBounds(Windows.Foundation.Rect bounds)
		=> double.IsFinite(bounds.X)
			&& double.IsFinite(bounds.Y)
			&& double.IsFinite(bounds.Width)
			&& double.IsFinite(bounds.Height);

	private void AddCustomActions(
		AccessibilityNodeInfoCompat node,
		int virtualViewId,
		AutomationPeer peer,
		bool isEnabled)
	{
		_customActionsByVirtualId.Remove(virtualViewId);
		if (!isEnabled)
		{
			return;
		}

		Dictionary<int, AccessibilityNativeActionRequest>? actions = null;

		void Add(int id, string label, AccessibilityNativeActionRequest request)
		{
			node.AddAction(new AccessibilityNodeInfoCompat.AccessibilityActionCompat(id, label));
			(actions ??= new())[id] = request;
		}

		if (peer.GetPattern(PatternInterface.MultipleView) is IMultipleViewProvider multipleView)
		{
			var supportedViews = multipleView.GetSupportedViews() ?? Array.Empty<int>();
			var currentView = multipleView.CurrentView;
			for (var index = 0; index < supportedViews.Length && index < 0x1000; index++)
			{
				var viewId = supportedViews[index];
				if (viewId == currentView)
				{
					continue;
				}

				var label = multipleView.GetViewName(viewId);
				if (string.IsNullOrEmpty(label))
				{
					label = $"View {viewId}";
				}

				Add(
					CustomActionChangeViewBase + index,
					label,
					new AccessibilityNativeActionRequest(
						AccessibilityNativeAction.ChangeView,
						number: viewId));
			}
		}

		if (peer.GetPattern(PatternInterface.Transform2) is ITransformProvider2 transform2 &&
			transform2.CanZoom)
		{
			Add(
				CustomActionZoomIn,
				"Zoom in",
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.ZoomIn));
			Add(
				CustomActionZoomOut,
				"Zoom out",
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.ZoomOut));
		}

		if (peer.GetPattern(PatternInterface.ScrollItem) is IScrollItemProvider)
		{
			Add(
				CustomActionScrollIntoView,
				"Scroll into view",
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.ScrollIntoView));
		}

		if (peer.GetPattern(PatternInterface.VirtualizedItem) is IVirtualizedItemProvider)
		{
			Add(
				CustomActionRealize,
				"Realize",
				new AccessibilityNativeActionRequest(AccessibilityNativeAction.Realize));
		}

		if (peer.GetPattern(PatternInterface.Dock) is IDockProvider dockProvider)
		{
			var currentPosition = dockProvider.DockPosition;
			foreach (var position in System.Enum.GetValues<DockPosition>())
			{
				if (position == currentPosition)
				{
					continue;
				}

				Add(
					CustomActionDockBase + (int)position,
					position == DockPosition.None ? "Undock" : $"Dock {position}",
					new AccessibilityNativeActionRequest(
						AccessibilityNativeAction.SetDockPosition,
						number: (int)position));
			}
		}

		if (actions is not null)
		{
			_customActionsByVirtualId[virtualViewId] = actions;
		}
	}

	// Range info for Slider, ProgressBar, and custom range providers.
	// typeInt: 0 = INT, 1 = FLOAT, 2 = PERCENT (Android RangeInfo type constants).
	private static void SetRangeInfo(AccessibilityNodeInfoCompat node, IRangeValueProvider rangeProvider)
	{
		// Use FLOAT type for all range controls to avoid TalkBack normalising the range.
		const int TypeFloat = 1;
		node.RangeInfo = new AccessibilityNodeInfoCompat.RangeInfoCompat(
			TypeFloat,
			(float)rangeProvider.Minimum,
			(float)rangeProvider.Maximum,
			(float)rangeProvider.Value);
	}

	// Text node properties.
	private static void SetTextInfo(
		AccessibilityNodeInfoCompat node,
		AutomationPeer effectivePeer,
		AutomationControlType controlType,
		bool isPassword,
		bool isEnabled)
	{
		var isEdit = controlType is AutomationControlType.Edit or AutomationControlType.Document
			|| (effectivePeer is FrameworkElementAutomationPeer { Owner: RichEditBox });

		var valueProvider = effectivePeer.GetPattern(PatternInterface.Value) as IValueProvider;

		// isReadOnly is true when disabled or when the provider reports read-only.
		bool isReadOnly = !isEnabled || valueProvider?.IsReadOnly != false;
		bool isEditable = isEdit && !isReadOnly;

		if (valueProvider is not null && !isPassword)
		{
			node.Text = valueProvider.Value;
		}

		node.Editable = isEditable;

		if (isEditable)
		{
			node.AddAction(AccessibilityNodeInfoCompat.AccessibilityActionCompat.ActionSetText);
		}

		if (isEdit)
		{
			node.MultiLine = effectivePeer is FrameworkElementAutomationPeer { Owner: { } editOwner }
				&& editOwner is TextBox tb ? tb.AcceptsReturn
				: effectivePeer is FrameworkElementAutomationPeer { Owner: RichEditBox };
		}
	}

	private static void SetCollectionInfo(
		AccessibilityNodeInfoCompat node,
		AutomationPeer effectivePeer,
		AutomationControlType controlType)
	{
		var isContainer = controlType is AutomationControlType.List
			or AutomationControlType.DataGrid
			or AutomationControlType.Tree
			or AutomationControlType.Tab
			or AutomationControlType.Menu;

		if (!isContainer)
		{
			return;
		}

		bool hierarchical = controlType == AutomationControlType.Tree;
		var selectionMode =
			effectivePeer.GetPattern(PatternInterface.Selection) is ISelectionProvider selectionProvider
				? selectionProvider.CanSelectMultiple ? 2 : 1
				: 0;

		if (effectivePeer.GetPattern(PatternInterface.Grid) is IGridProvider gridProvider)
		{
			node.SetCollectionInfo(AccessibilityNodeInfoCompat.CollectionInfoCompat.Obtain(
				gridProvider.RowCount, gridProvider.ColumnCount, hierarchical, selectionMode));
		}
		else
		{
			// Use the total peer-child count (includes unrealized/ownerless items) so
			// TalkBack announces the full list size, not just the realized window.
			int rowCount = effectivePeer.GetChildren()?.Count ?? 0;
			node.SetCollectionInfo(AccessibilityNodeInfoCompat.CollectionInfoCompat.Obtain(
				rowCount, 1, hierarchical, selectionMode));
		}
	}

	private void SetCollectionItemInfo(
		AccessibilityNodeInfoCompat node,
		AutomationPeer effectivePeer,
		AutomationControlType controlType,
		UIElement uiElement,
		int virtualViewId)
	{
		bool isHeading = controlType == AutomationControlType.HeaderItem;
		if (effectivePeer.GetPattern(PatternInterface.GridItem) is IGridItemProvider gridItemProvider)
		{
			node.SetCollectionItemInfo(
				AccessibilityNodeInfoCompat.CollectionItemInfoCompat.Obtain(
					gridItemProvider.Row,
					gridItemProvider.RowSpan,
					gridItemProvider.Column,
					gridItemProvider.ColumnSpan,
					isHeading));
			return;
		}

		var isItem = controlType is AutomationControlType.ListItem
			or AutomationControlType.DataItem
			or AutomationControlType.TreeItem
			or AutomationControlType.TabItem
			or AutomationControlType.MenuItem
			or AutomationControlType.HeaderItem;

		if (!isItem)
		{
			return;
		}

		int posInSet = effectivePeer.GetPositionInSet();
		if (posInSet > 0)
		{
			// 1-based PositionInSet → 0-based row; never negative because sentinel is -1.
			node.SetCollectionItemInfo(
				AccessibilityNodeInfoCompat.CollectionItemInfoCompat.Obtain(
					posInSet - 1, 1, 0, 1, isHeading));
			return;
		}

		// No PositionInSet: derive row from the realized siblings already assigned
		// virtual IDs in _childrenByVirtualId (excludes ownerless/unrealized peers).
		// This gives a dense 0-based index within the visible window, which is the
		// best available position for a collection without explicit PositionInSet.
		int derivedRow = GetRowIndexFromParentChildrenList(virtualViewId);
		if (derivedRow >= 0)
		{
			node.SetCollectionItemInfo(
				AccessibilityNodeInfoCompat.CollectionItemInfoCompat.Obtain(
					derivedRow, 1, 0, 1, isHeading));
		}
		// derivedRow == -1: parent not realized or item not in siblings list — omit.
	}

	private int GetRowIndexFromParentChildrenList(int virtualViewId)
		=> _rowIndexByVirtualId.TryGetValue(virtualViewId, out var rowIndex)
			? rowIndex
			: -1;

	// Relations: LabeledBy is native, DescribedBy contributes to hint, and flows define traversal.
	private void SetRelations(
		AccessibilityNodeInfoCompat node,
		AutomationPeer effectivePeer,
		UIElement uiElement)
	{
		if (effectivePeer.GetLabeledBy() is FrameworkElementAutomationPeer labeledByPeer &&
			IsSameRoot(uiElement, labeledByPeer.Owner) &&
			_elementToId.TryGetValue(labeledByPeer.Owner, out var labeledByBox) &&
			_orderedIdSet.Contains(labeledByBox.Value))
		{
			node.SetLabeledBy(_host, labeledByBox.Value);
		}

		// DescribedBy: targets are UIElement/DependencyObject — resolve each to its name via peer.
		var describedByRaw = uiElement.GetValue(AutomationProperties.DescribedByProperty)
			as IList<DependencyObject>;
		if (describedByRaw is { Count: > 0 })
		{
			var descriptions = new System.Text.StringBuilder(node.HintText ?? "");
			foreach (var target in describedByRaw)
			{
				var desc = target is UIElement targetElement &&
					IsSameRoot(uiElement, targetElement) &&
					_elementToId.TryGetValue(targetElement, out var targetBox) &&
					_orderedIdSet.Contains(targetBox.Value)
						? targetElement.GetOrCreateAutomationPeer()?.GetName()
						: null;
				if (!string.IsNullOrEmpty(desc))
				{
					if (descriptions.Length > 0)
					{
						descriptions.Append(". ");
					}

					descriptions.Append(desc);
				}
			}

			if (descriptions.Length > 0)
			{
				node.HintText = descriptions.ToString();
			}
		}

		// FlowsTo: read from DP (no lazy allocation); filter to same-root live targets.
		var flowsToRaw = uiElement.GetValue(AutomationProperties.FlowsToProperty)
			as IList<DependencyObject>;
		if (flowsToRaw is { Count: > 0 })
		{
			foreach (var target in flowsToRaw)
			{
				if (target is UIElement targetEl &&
					IsSameRoot(uiElement, targetEl) &&
					_elementToId.TryGetValue(targetEl, out var targetBox) &&
					_orderedIdSet.Contains(targetBox.Value))
				{
					node.SetTraversalBefore(_host, targetBox.Value);
					break;
				}
			}
		}

		// FlowsFrom: read from DP; filter to same-root live sources.
		var flowsFromRaw = uiElement.GetValue(AutomationProperties.FlowsFromProperty)
			as IList<DependencyObject>;
		if (flowsFromRaw is { Count: > 0 })
		{
			foreach (var source in flowsFromRaw)
			{
				if (source is UIElement sourceEl &&
					IsSameRoot(uiElement, sourceEl) &&
					_elementToId.TryGetValue(sourceEl, out var sourceBox) &&
					_orderedIdSet.Contains(sourceBox.Value))
				{
					node.SetTraversalAfter(_host, sourceBox.Value);
					break;
				}
			}
		}
	}

	// Role, state, landmark, and form metadata.
	private static void SetMetadata(
		AccessibilityNodeInfoCompat node,
		AutomationPeer effectivePeer,
		UIElement uiElement)
	{
		// LocalizedControlType → role description (overrides class-name-derived role).
		var localizedType = effectivePeer.GetLocalizedControlType();
		if (!string.IsNullOrEmpty(localizedType))
		{
			node.RoleDescription = localizedType;
		}

		// ItemStatus → state description (e.g. "loading", "sorted ascending").
		var itemStatus = AutomationProperties.GetItemStatus(uiElement);
		if (!string.IsNullOrEmpty(itemStatus))
		{
			node.StateDescription = string.IsNullOrEmpty(node.StateDescription)
				? itemStatus
				: $"{node.StateDescription}, {itemStatus}";
		}

		// FullDescription belongs in the hint/description slot alongside HelpText and DescribedBy.
		// HintText was already set from HelpText; append FullDescription when present.
		var fullDesc = AutomationProperties.GetFullDescription(uiElement);
		if (!string.IsNullOrEmpty(fullDesc))
		{
			var existing = node.HintText ?? "";
			node.HintText = string.IsNullOrEmpty(existing) ? fullDesc : $"{existing}. {fullDesc}";
		}

		// Heading level semantics: node.Heading carries the boolean; exact level is
		// Details-only (no unlocalized "hN" state text emitted to the native node).

		// Form validation: use the real ContentInvalid API; no extras needed.
		bool dataValid = AutomationProperties.GetIsDataValidForForm(uiElement);
		if (!dataValid)
		{
			node.ContentInvalid = true;
			node.Error =
				NullIfEmpty(fullDesc)
				?? NullIfEmpty(effectivePeer.GetHelpText())
				?? NullIfEmpty(itemStatus)
				?? "Invalid";
		}

		// Required: no standard AccessibilityNodeInfoCompat API — fall back to Extras bundle.
		bool required = AutomationProperties.GetIsRequiredForForm(uiElement);
		if (required)
		{
			var extras = node.Extras ?? new global::Android.OS.Bundle();
			extras.PutBoolean("androidx.view.accessibility.required", true);
		}

		var localizedLandmark = effectivePeer.GetLocalizedLandmarkType();
		if (!string.IsNullOrEmpty(localizedLandmark))
		{
			node.PaneTitle = localizedLandmark;
		}
	}

	private void ApplyCulture(AccessibilityNodeInfoCompat node, UIElement element)
	{
		var lcid = AutomationProperties.GetCulture(element);
		if (lcid == 0)
		{
			return;
		}

		CultureInfo culture;
		try
		{
			culture = CultureInfo.GetCultureInfo(lcid);
		}
		catch (CultureNotFoundException ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning(
					$"[A11y] Ignoring invalid AutomationProperties.Culture LCID {lcid}: {ex.Message}");
			}

			return;
		}

		var locale = Java.Util.Locale.ForLanguageTag(culture.Name);
		if (node.Text is { Length: > 0 } text)
		{
			var formattedText = new global::Android.Text.SpannableString(text);
			formattedText.SetSpan(
				new global::Android.Text.Style.LocaleSpan(locale),
				0,
				text.Length,
				global::Android.Text.SpanTypes.ExclusiveExclusive);
			node.TextFormatted = formattedText;
		}

		if (node.ContentDescription is { Length: > 0 } description)
		{
			var formattedDescription = new global::Android.Text.SpannableString(description);
			formattedDescription.SetSpan(
				new global::Android.Text.Style.LocaleSpan(locale),
				0,
				description.Length,
				global::Android.Text.SpanTypes.ExclusiveExclusive);
			node.ContentDescriptionFormatted = formattedDescription;
		}
	}

	private static bool IsSameRoot(UIElement a, UIElement b)
		=> a.XamlRoot is { } ra && b.XamlRoot is { } rb && ReferenceEquals(ra, rb);

	private static int ConvertHeadingLevel(AutomationHeadingLevel level)
		=> level switch
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
		};

	// AccessibilityPeerHelper hook implementations ------------------------------
	// These private methods back the static hooks registered in Initialize so the
	// test infrastructure can inspect the live native tree without a direct
	// reference to this assembly.

	// Returns the real AccessibilityNodeInfoCompat for element (does a full scan
	// to ensure per-scan caches are current before querying the provider).
	private object? GetNodeForElement(UIElement element)
	{
		var ids = new List<Integer>();
		GetVisibleVirtualViews(ids);

		if (!_elementToId.TryGetValue(element, out var box))
		{
			return null;
		}

		if (!_orderedIdSet.Contains(box.Value))
		{
			return null;
		}

		var provider = GetAccessibilityNodeProvider(_host);
		return provider?.CreateAccessibilityNodeInfo(box.Value);
	}

	// Returns the stable virtual ID for element, triggering a scan if needed.
	private int? GetVirtualIdForElement(UIElement element)
	{
		var ids = new List<Integer>();
		GetVisibleVirtualViews(ids);

		return _elementToId.TryGetValue(element, out var box) &&
			_orderedIdSet.Contains(box.Value)
				? box.Value
				: null;
	}

	// Returns all real AccessibilityNodeInfoCompat objects for the given XamlRoot
	// in peer-tree order, or null if the root is not ours.
	private object[]? GetAllNodesForRoot(XamlRoot xamlRoot)
	{
		if (_adapter is null)
		{
			return null;
		}

		// Only serve requests for our adapter's XamlRoot.
		var adapterRootXamlRoot = _adapter.RootElement?.XamlRoot;
		if (adapterRootXamlRoot is null || !ReferenceEquals(adapterRootXamlRoot, xamlRoot))
		{
			return null;
		}

		var ids = new List<Integer>();
		GetVisibleVirtualViews(ids);

		var provider = GetAccessibilityNodeProvider(_host);
		if (provider is null)
		{
			return null;
		}

		var result = new List<object>(_orderedIds.Count);
		foreach (var id in _orderedIds)
		{
			var node = provider.CreateAccessibilityNodeInfo(id);
			if (node is not null)
			{
				result.Add(node);
			}
		}

		return result.Count > 0 ? result.ToArray() : null;
	}

	private AccessibilityNativeNodeSnapshot? GetSnapshotForElement(UIElement element)
	{
		if (GetNodeForElement(element) is not AccessibilityNodeInfoCompat node)
		{
			return null;
		}

		var peer = element.GetOrCreateAutomationPeer();
		var effectivePeer = peer?.ResolveProviderPeer(resolveEventsSource: true);
		var controlType = effectivePeer?.GetAutomationControlType() ?? AutomationControlType.Custom;
		int? vid = _elementToId.TryGetValue(element, out var box) && _orderedIdSet.Contains(box.Value)
			? box.Value
			: null;
		var details = effectivePeer is not null
			? BuildNodeDetails(effectivePeer, element, controlType, vid)
			: null;
		return CreateSnapshot(node, details, GetCheckedState(effectivePeer));
	}

	private AccessibilityNativeNodeSnapshot[]? GetAllSnapshotsForRoot(XamlRoot xamlRoot)
	{
		if (_adapter is null)
		{
			return null;
		}

		var adapterRootXamlRoot = _adapter.RootElement?.XamlRoot;
		if (adapterRootXamlRoot is null || !ReferenceEquals(adapterRootXamlRoot, xamlRoot))
		{
			return null;
		}

		var ids = new List<Integer>();
		GetVisibleVirtualViews(ids);

		var provider = GetAccessibilityNodeProvider(_host);
		if (provider is null)
		{
			return null;
		}

		var result = new List<AccessibilityNativeNodeSnapshot>(_orderedIds.Count);
		foreach (var id in _orderedIds)
		{
			if (provider.CreateAccessibilityNodeInfo(id) is not AccessibilityNodeInfoCompat node)
			{
				continue;
			}

			AccessibilityNativeNodeDetails? details = null;
			AutomationPeer? snapshotPeer = null;
			if (_idToWeakElement.TryGetValue(id, out var weakRef) &&
				weakRef.TryGetTarget(out var dep) &&
				dep is UIElement owner)
			{
				var details2Peer = owner.GetOrCreateAutomationPeer();
				var effectivePeer2 = details2Peer?.ResolveProviderPeer(resolveEventsSource: true);
				snapshotPeer = effectivePeer2;
				var controlType2 = effectivePeer2?.GetAutomationControlType() ?? AutomationControlType.Custom;
				details = effectivePeer2 is not null
					? BuildNodeDetails(effectivePeer2, owner, controlType2, id)
					: null;
			}

			result.Add(CreateSnapshot(node, details, GetCheckedState(snapshotPeer)));
		}

		return result.Count > 0 ? result.ToArray() : null;
	}

	private string GetDiagnostics(XamlRoot xamlRoot)
	{
		var root = GetRootElement();
		if (root is null)
		{
			return "root=null";
		}

		var rootPeer = root.GetOrCreateAutomationPeer();
		var nodes = GetCurrentPeerTree();
		var ownerTypes = string.Join(",", nodes
			.Select(node => node.Owner?.GetType().Name ?? "<ownerless>")
			.Take(20));

		return $"rootMatch={ReferenceEquals(root.XamlRoot, xamlRoot)};" +
			$"root={root.GetType().FullName};" +
			$"rootPeer={rootPeer?.GetType().FullName ?? "<null>"};" +
			$"visualChildren={root.GetChildren().Count};" +
			$"peerNodes={nodes.Count};" +
			$"owners={ownerTypes}";
	}

	// Focus accessor and focused-node accessor hook implementations

	// Backing method for AndroidAccessibilityFocusAccessor.
	// Looks up the stable virtual ID for element, applies modal filtering, then
	// requests native accessibility focus via the real provider path.
	// Returns true when the request was dispatched (focus tracking is updated
	// regardless of whether a TalkBack session is active).
	private bool RequestNativeFocusForElement(UIElement element)
	{
		// Ensure the per-scan caches are current before looking up the ID.
		var ids = new List<Integer>();
		GetVisibleVirtualViews(ids);

		if (!_elementToId.TryGetValue(element, out var box) || !_orderedIdSet.Contains(box.Value))
		{
			return false;
		}

		// Modal filtering: if there is an active dialog/modal peer subtree, only
		// elements within that subtree should receive accessibility focus.
		if (IsBlockedByActiveModal(element))
		{
			return false;
		}

		return RequestNativeFocusById(box.Value);
	}

	// Backing method for AndroidFocusedNativeNodeAccessor.
	// Returns an AccessibilityNativeNodeSnapshot for the virtual view that currently
	// holds native accessibility focus, or null when no valid focus exists.
	// Uses _nativeFocusedId (set by RequestNativeFocusById) as the primary source so
	// the method works in test environments where no TalkBack session is active.
	private object? GetFocusedNativeNode(XamlRoot xamlRoot)
	{
		if (_adapter?.RootElement?.XamlRoot is not { } rootXamlRoot ||
			!ReferenceEquals(rootXamlRoot, xamlRoot))
		{
			return null;
		}

		if (_nativeFocusedId < 0)
		{
			return null;
		}

		// Trigger a fresh scan so _orderedIds is current; this also prunes stale
		// focus IDs via PruneRegistryToCurrentTree if the element has been removed.
		var ids = new List<Integer>();
		GetVisibleVirtualViews(ids);

		// After pruning, _nativeFocusedId may have been cleared.
		if (_nativeFocusedId < 0 || !_orderedIdSet.Contains(_nativeFocusedId))
		{
			return null;
		}

		// Primary path: create a snapshot from the real AccessibilityNodeInfoCompat.
		var provider = GetAccessibilityNodeProvider(_host);
		if (provider?.CreateAccessibilityNodeInfo(_nativeFocusedId) is AccessibilityNodeInfoCompat node)
		{
			UIElement? owner = null;
			if (_idToWeakElement.TryGetValue(_nativeFocusedId, out var weakRefFocus) &&
				weakRefFocus.TryGetTarget(out var dep) && dep is UIElement ownerElement)
			{
				owner = ownerElement;
			}

			var details = BuildDetailsFromElement(owner);
			var peer = owner?.GetOrCreateAutomationPeer()?.ResolveProviderPeer(resolveEventsSource: true);
			return CreateSnapshot(node, details, GetCheckedState(peer));
		}

		// Fallback: provider not available (no accessibility service); build the
		// snapshot directly from the element's peer properties.
		if (_idToWeakElement.TryGetValue(_nativeFocusedId, out var weakRef) &&
			weakRef.TryGetTarget(out var element) &&
			element is UIElement uiElement)
		{
			return GetSnapshotForElement(uiElement);
		}

		return null;
	}

	// Modal filtering helpers

	private HashSet<int>? GetActiveModalNodeIndices(
		IReadOnlyList<AccessibilityPeerNode> tree)
	{
		if (_cachedModalNodeIndex < 0)
		{
			return null;
		}

		var allowed = new HashSet<int> { _cachedModalNodeIndex };
		for (var i = _cachedModalNodeIndex + 1; i < tree.Count; i++)
		{
			var parentIndex = tree[i].ParentIndex;
			while (parentIndex is { } parent)
			{
				if (parent == _cachedModalNodeIndex)
				{
					allowed.Add(i);
					break;
				}

				parentIndex = tree[parent].ParentIndex;
			}
		}

		return allowed;
	}

	// Returns true when there is an active modal and element is NOT within it.
	// Never blocks elements inside the modal, and never blocks the modal root itself.
	internal bool IsBlockedByActiveModal(UIElement element)
	{
		var tree = GetCurrentPeerTree();
		if (_cachedModalElement is null || _cachedModalNodeIndex < 0)
		{
			return false;
		}

		for (var i = 0; i < tree.Count; i++)
		{
			if (!ReferenceEquals(tree[i].Owner, element))
			{
				continue;
			}

			int? current = i;
			while (current is { } index)
			{
				if (index == _cachedModalNodeIndex)
				{
					return false;
				}

				current = tree[index].ParentIndex;
			}

			return true;
		}

		return !IsDescendantOfElement(element, _cachedModalElement);
	}

	// Walks the visual parent chain to check whether element is a descendant of ancestor.
	private static bool IsDescendantOfElement(UIElement element, UIElement ancestor)
	{
		DependencyObject? current = element;
		while (current is not null)
		{
			if (ReferenceEquals(current, ancestor))
			{
				return true;
			}

			current = (current as UIElement)?.GetUIElementAdjustedParentInternal();
		}

		return false;
	}

	private static AccessibilityNativeNodeSnapshot CreateSnapshot(
		AccessibilityNodeInfoCompat node,
		AccessibilityNativeNodeDetails? details = null,
		bool? checkedState = null)
	{
		var nativeBounds = new global::Android.Graphics.Rect();
		node.GetBoundsInScreen(nativeBounds);

		var className = node.ClassName?.ToString();
		var traits = AccessibilityNativeTraits.None;
		traits |= className switch
		{
			"android.widget.Button" => AccessibilityNativeTraits.Button,
			"android.widget.TextView" => AccessibilityNativeTraits.StaticText,
			"android.widget.SeekBar" => AccessibilityNativeTraits.Adjustable,
			"android.widget.ImageView" => AccessibilityNativeTraits.Image,
			_ => AccessibilityNativeTraits.None,
		};

		if (!node.Enabled)
		{
			traits |= AccessibilityNativeTraits.NotEnabled;
		}

		if (node.Heading)
		{
			traits |= AccessibilityNativeTraits.Header;
		}

		// Never surface password text through the snapshot value.
		var value = node.Password ? null : node.Text?.ToString();

		return new AccessibilityNativeNodeSnapshot(
			node,
			node.ContentDescription?.ToString(),
			className,
			node.HintText?.ToString(),
			value,
			node.UniqueId,
			node.Enabled,
			node.Heading,
			node.Password,
			node.Checkable,
			node.Checkable ? checkedState : null,
			traits,
			new Windows.Foundation.Rect(
				nativeBounds.Left,
				nativeBounds.Top,
				nativeBounds.Width(),
				nativeBounds.Height()),
			details,
			nativeAutomationId: node.ViewIdResourceName,
			stateDescription: node.StateDescription?.ToString(),
			nativeRoleDescription: node.RoleDescription?.ToString());
	}

	private static bool? GetCheckedState(AutomationPeer? peer)
		=> peer?.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider
			? toggleProvider.ToggleState switch
			{
				ToggleState.On => true,
				ToggleState.Off => false,
				_ => null,
			}
			: null;

	private AccessibilityNativeNodeDetails? BuildNodeDetails(
		AutomationPeer effectivePeer,
		UIElement uiElement,
		AutomationControlType controlType,
		int? virtualViewId = null)
	{
		bool enabled = effectivePeer.IsEnabled();
		var supportedActions = new List<AccessibilityNativeAction>();

		if (enabled && effectivePeer is IInvokeProvider or IToggleProvider or ISelectionItemProvider)
		{
			supportedActions.Add(AccessibilityNativeAction.Activate);
		}

		if (enabled && effectivePeer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandProv)
		{
			var ecState = expandProv.ExpandCollapseState;
			if (ecState is ExpandCollapseState.Collapsed or ExpandCollapseState.PartiallyExpanded)
			{
				supportedActions.Add(AccessibilityNativeAction.Expand);
			}

			if (ecState is ExpandCollapseState.Expanded or ExpandCollapseState.PartiallyExpanded)
			{
				supportedActions.Add(AccessibilityNativeAction.Collapse);
			}
		}

		AccessibilityNativeRangeDetails? range = null;
		if (effectivePeer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rv)
		{
			bool rangeReadOnly = rv.IsReadOnly || !effectivePeer.IsEnabled();
			if (!rangeReadOnly)
			{
				supportedActions.Add(AccessibilityNativeAction.Increment);
				supportedActions.Add(AccessibilityNativeAction.Decrement);
				supportedActions.Add(AccessibilityNativeAction.SetRangeValue);
			}

			range = new AccessibilityNativeRangeDetails(
				rv.Value, rv.Minimum, rv.Maximum,
				rv.SmallChange, rv.LargeChange,
				rangeReadOnly, effectivePeer.GetOrientation());
		}
		else if (enabled
			&& effectivePeer.GetPattern(PatternInterface.Scroll) is IScrollProvider sp
			&& (sp.HorizontallyScrollable || sp.VerticallyScrollable))
		{
			supportedActions.Add(AccessibilityNativeAction.ScrollForward);
			supportedActions.Add(AccessibilityNativeAction.ScrollBackward);
		}

		var isEdit = controlType is AutomationControlType.Edit or AutomationControlType.Document
			|| (effectivePeer is FrameworkElementAutomationPeer { Owner: RichEditBox });

		AccessibilityNativeTextStateDetails? textState = null;
		if (isEdit)
		{
			var valueProvider = effectivePeer.GetPattern(PatternInterface.Value) as IValueProvider;
			bool isReadOnly = !enabled || valueProvider?.IsReadOnly != false;
			bool isEditable = !isReadOnly;
			bool isMultiline = effectivePeer is FrameworkElementAutomationPeer { Owner: { } editOwner }
				&& editOwner is TextBox tbD ? tbD.AcceptsReturn
				: effectivePeer is FrameworkElementAutomationPeer { Owner: RichEditBox };
			bool hasTextSelection = effectivePeer.GetPattern(PatternInterface.Text) is ITextProvider;

			if (isEditable)
			{
				supportedActions.Add(AccessibilityNativeAction.SetValue);
			}

			textState = new AccessibilityNativeTextStateDetails(isEditable, isReadOnly, isMultiline, hasTextSelection);
		}

		AccessibilityNativeScrollDetails? scroll = null;
		if (effectivePeer.GetPattern(PatternInterface.Scroll) is IScrollProvider scrollProvider)
		{
			scroll = new AccessibilityNativeScrollDetails(
				scrollProvider.HorizontallyScrollable,
				scrollProvider.VerticallyScrollable,
				scrollProvider.HorizontalScrollPercent,
				scrollProvider.VerticalScrollPercent,
				scrollProvider.HorizontalViewSize,
				scrollProvider.VerticalViewSize);
		}

		AccessibilityNativeCollectionDetails? collection = null;
		var isContainer = controlType is AutomationControlType.List
			or AutomationControlType.DataGrid
			or AutomationControlType.Tree
			or AutomationControlType.Tab
			or AutomationControlType.Menu;
		if (isContainer)
		{
			bool canSelectMultiple = false;
			bool isSelectionRequired = false;
			if (effectivePeer.GetPattern(PatternInterface.Selection) is ISelectionProvider selProv)
			{
				canSelectMultiple = selProv.CanSelectMultiple;
				isSelectionRequired = selProv.IsSelectionRequired;
			}

			if (effectivePeer.GetPattern(PatternInterface.Grid) is IGridProvider gridProv)
			{
				collection = new AccessibilityNativeCollectionDetails(
					gridProv.RowCount, gridProv.ColumnCount,
					canSelectMultiple, isSelectionRequired);
			}
			else
			{
				int rowCount = effectivePeer.GetChildren()?.Count ?? 0;
				collection = new AccessibilityNativeCollectionDetails(
					rowCount, 1, canSelectMultiple, isSelectionRequired);
			}
		}

		AccessibilityNativeCollectionItemDetails? collectionItem = null;
		if (effectivePeer.GetPattern(PatternInterface.GridItem) is IGridItemProvider gridItemProvider)
		{
			collectionItem = new AccessibilityNativeCollectionItemDetails(
				gridItemProvider.Row,
				gridItemProvider.Column,
				gridItemProvider.RowSpan,
				gridItemProvider.ColumnSpan);
		}

		var isItem = controlType is AutomationControlType.ListItem
			or AutomationControlType.DataItem
			or AutomationControlType.TreeItem
			or AutomationControlType.TabItem
			or AutomationControlType.MenuItem
			or AutomationControlType.HeaderItem;
		if (collectionItem is null && isItem)
		{
			// PositionInSet default is -1 (unset); only use when > 0 to avoid row -2.
			int posInSet = effectivePeer.GetPositionInSet();
			if (posInSet > 0)
			{
				collectionItem = new AccessibilityNativeCollectionItemDetails(posInSet - 1, 0, 1, 1);
			}
			else if (virtualViewId.HasValue)
			{
				// Fallback: index among realized siblings (excludes ownerless peers).
				int derivedRow = GetRowIndexFromParentChildrenList(virtualViewId.Value);
				if (derivedRow >= 0)
				{
					collectionItem = new AccessibilityNativeCollectionItemDetails(derivedRow, 0, 1, 1);
				}
				// derivedRow == -1: item not yet in realized tree — omit CollectionItem.
			}
		}

		int positionInSet = effectivePeer.GetPositionInSet();
		int sizeOfSet = effectivePeer.GetSizeOfSet();
		int level = effectivePeer.GetLevel();
		AccessibilityNativeHierarchyDetails? hierarchy =
			(positionInSet > 0 || sizeOfSet > 0 || level > 0)
				? new AccessibilityNativeHierarchyDetails(
					positionInSet > 0 ? positionInSet : 0,
					sizeOfSet > 0 ? sizeOfSet : 0,
					level > 0 ? level : 0)
				: null;

		AccessibilityNativeRelationDetails? relations = BuildRelationDetails(effectivePeer, uiElement);
		var fallbacks = AccessibilityPeerHelper.GetFallbackDetails(effectivePeer);

		if (enabled && effectivePeer.GetPattern(PatternInterface.Window) is not null)
		{
			supportedActions.Add(AccessibilityNativeAction.Dismiss);
		}

		if (enabled &&
			effectivePeer.GetPattern(PatternInterface.MultipleView) is IMultipleViewProvider multipleView &&
			(multipleView.GetSupportedViews()?.Length ?? 0) > 1)
		{
			supportedActions.Add(AccessibilityNativeAction.ChangeView);
		}

		if (enabled &&
			effectivePeer.GetPattern(PatternInterface.Transform2) is ITransformProvider2 transform2 &&
			transform2.CanZoom)
		{
			supportedActions.Add(AccessibilityNativeAction.ZoomIn);
			supportedActions.Add(AccessibilityNativeAction.ZoomOut);
		}

		if (enabled && effectivePeer.GetPattern(PatternInterface.ScrollItem) is IScrollItemProvider)
		{
			supportedActions.Add(AccessibilityNativeAction.ScrollIntoView);
		}

		if (enabled && effectivePeer.GetPattern(PatternInterface.VirtualizedItem) is IVirtualizedItemProvider)
		{
			supportedActions.Add(AccessibilityNativeAction.Realize);
		}

		if (enabled && effectivePeer.GetPattern(PatternInterface.Dock) is IDockProvider)
		{
			supportedActions.Add(AccessibilityNativeAction.SetDockPosition);
		}

		if (supportedActions.Count == 0
			&& range is null && textState is null && scroll is null
			&& collection is null && collectionItem is null
			&& hierarchy is null && relations is null && fallbacks is null)
		{
			int cultureCheck = AutomationProperties.GetCulture(uiElement);
			bool required = AutomationProperties.GetIsRequiredForForm(uiElement);
			bool dataValid = AutomationProperties.GetIsDataValidForForm(uiElement);
			var localizedControlType = NullIfEmpty(effectivePeer.GetLocalizedControlType());
			bool hasScalar =
				!string.IsNullOrEmpty(AutomationProperties.GetItemStatus(uiElement))
				|| !string.IsNullOrEmpty(AutomationProperties.GetItemType(uiElement))
				|| localizedControlType is not null
				|| !string.IsNullOrEmpty(AutomationProperties.GetFullDescription(uiElement))
				|| required || !dataValid || cultureCheck != 0
				|| effectivePeer.GetLandmarkType() != AutomationLandmarkType.None
				|| !string.IsNullOrEmpty(effectivePeer.GetLocalizedLandmarkType());
			if (!hasScalar)
			{
				return null;
			}
		}

		return new AccessibilityNativeNodeDetails(
			supportedActions: supportedActions,
			range: range,
			textState: textState,
			scroll: scroll,
			collection: collection,
			collectionItem: collectionItem,
			hierarchy: hierarchy,
			relations: relations,
			itemStatus: NullIfEmpty(AutomationProperties.GetItemStatus(uiElement)),
			itemType: NullIfEmpty(AutomationProperties.GetItemType(uiElement)),
			localizedControlType: NullIfEmpty(effectivePeer.GetLocalizedControlType()),
			fullDescription: NullIfEmpty(AutomationProperties.GetFullDescription(uiElement)),
			isRequiredForForm: AutomationProperties.GetIsRequiredForForm(uiElement) ? true : null,
			isDataValidForForm: !AutomationProperties.GetIsDataValidForForm(uiElement) ? false : null,
			culture: AutomationProperties.GetCulture(uiElement) is var lcid && lcid != 0 ? lcid : null,
			landmarkType: effectivePeer.GetLandmarkType() is var lt && lt != AutomationLandmarkType.None ? lt : null,
			localizedLandmarkType: NullIfEmpty(effectivePeer.GetLocalizedLandmarkType()),
			fallbacks: fallbacks);
	}

	private AccessibilityNativeNodeDetails? BuildDetailsFromElement(UIElement? owner)
	{
		if (owner is null)
		{
			return null;
		}

		var peer = owner.GetOrCreateAutomationPeer();
		var effectivePeer = peer?.ResolveProviderPeer(resolveEventsSource: true);
		if (effectivePeer is null)
		{
			return null;
		}

		return BuildNodeDetails(effectivePeer, owner, effectivePeer.GetAutomationControlType());
	}

	private static AccessibilityNativeRelationDetails? BuildRelationDetails(
		AutomationPeer effectivePeer,
		UIElement uiElement)
	{
		var labeledByIds = GetPeerIds(effectivePeer.GetLabeledBy());

		// Use GetValue to avoid allocating an empty live collection when the DP is not set.
		var describedByIds = uiElement.GetValue(AutomationProperties.DescribedByProperty)
			is IList<DependencyObject> { Count: > 0 } describedByList
			? GetDependencyObjectIds(describedByList)
			: null;

		var controlledPeerIds = uiElement.GetValue(AutomationProperties.ControlledPeersProperty)
			is IList<UIElement> { Count: > 0 } controlledList
			? GetUIElementIds(controlledList)
			: null;

		// FlowsTo/From: read from DP to avoid lazy peer allocation; filter to same-root targets.
		var flowsToIds = GetSameRootDependencyObjectIds(
			uiElement,
			uiElement.GetValue(AutomationProperties.FlowsToProperty) as IList<DependencyObject>);
		var flowsFromIds = GetSameRootDependencyObjectIds(
			uiElement,
			uiElement.GetValue(AutomationProperties.FlowsFromProperty) as IList<DependencyObject>);

		// Annotations: type names from DP without lazy collection creation.
		IReadOnlyList<string>? annotationTypeNames = null;
		if (uiElement.GetValue(AutomationProperties.AnnotationsProperty)
			is IList<AutomationAnnotation> { Count: > 0 } annotations)
		{
			var names = new List<string>(annotations.Count);
			foreach (var a in annotations)
			{
				names.Add(a.Type.ToString());
			}

			annotationTypeNames = names;
		}

		if (labeledByIds is null && describedByIds is null && controlledPeerIds is null
			&& flowsToIds is null && flowsFromIds is null && annotationTypeNames is null)
		{
			return null;
		}

		return new AccessibilityNativeRelationDetails(
			labeledByIds, describedByIds, controlledPeerIds,
			flowsFromIds, flowsToIds, annotationTypeNames);
	}

	// Returns the AutomationId of a single peer, or null if absent or unstable.
	private static IReadOnlyList<string>? GetPeerIds(AutomationPeer? peer)
	{
		if (peer is null)
		{
			return null;
		}

		var id = peer is FrameworkElementAutomationPeer fep
			? NullIfEmpty(AutomationProperties.GetAutomationId(fep.Owner))
			: null;
		return id is not null ? [id] : null;
	}

	private static IReadOnlyList<string>? GetSameRootDependencyObjectIds(
		UIElement source,
		IList<DependencyObject>? elements)
	{
		if (elements is null or { Count: 0 })
		{
			return null;
		}

		var ids = new List<string>(elements.Count);
		foreach (var el in elements)
		{
			if (el is not UIElement target || !IsSameRoot(source, target))
			{
				continue;
			}

			var id = NullIfEmpty(AutomationProperties.GetAutomationId(target));
			if (id is not null)
			{
				ids.Add(id);
			}
		}

		return ids.Count > 0 ? ids : null;
	}

	private static IReadOnlyList<string>? GetDependencyObjectIds(
		IList<DependencyObject>? elements)
	{
		if (elements is null or { Count: 0 })
		{
			return null;
		}

		var ids = new List<string>(elements.Count);
		foreach (var el in elements)
		{
			var id = NullIfEmpty(AutomationProperties.GetAutomationId(el));
			if (id is not null)
			{
				ids.Add(id);
			}
		}

		return ids.Count > 0 ? ids : null;
	}

	private static IReadOnlyList<string>? GetUIElementIds(IList<UIElement>? elements)
	{
		if (elements is null or { Count: 0 })
		{
			return null;
		}

		var ids = new List<string>(elements.Count);
		foreach (var el in elements)
		{
			var id = NullIfEmpty(AutomationProperties.GetAutomationId(el));
			if (id is not null)
			{
				ids.Add(id);
			}
		}

		return ids.Count > 0 ? ids : null;
	}

	private static string? NullIfEmpty(string? s) => string.IsNullOrEmpty(s) ? null : s;

	// Returns the resource-name segment for the given AutomationId.
	// If the id is already a valid Android resource segment ([A-Za-z_][A-Za-z0-9_.]*),
	// it is returned unchanged so that existing UIAutomator selectors continue to work.
	// Otherwise every invalid character is replaced with '_', the first character is
	// forced to a letter or '_', and a 4-hex-digit FNV-1a suffix is appended so that
	// distinct originals that normalize to the same segment remain distinguishable.
	internal static string NormalizeToResourceSegment(string id)
	{
		if (IsValidResourceSegment(id))
		{
			return id;
		}

		var sb = new System.Text.StringBuilder(id.Length + 6);
		for (int i = 0; i < id.Length; i++)
		{
			char c = id[i];
			if (i == 0)
			{
				if (char.IsAsciiLetter(c) || c == '_')
				{
					sb.Append(c);
				}
				else
				{
					sb.Append('_');
					// Keep the digit so "123abc" → "_123abc_hash", not "_abc_hash".
					if (char.IsAsciiDigit(c))
					{
						sb.Append(c);
					}
				}
			}
			else
			{
				sb.Append(char.IsAsciiLetterOrDigit(c) || c == '_' || c == '.' ? c : '_');
			}
		}

		if (sb.Length == 0)
		{
			sb.Append('_');
		}

		sb.Append('_');
		sb.Append((Fnv1a32(id) & 0xFFFFu).ToString("x4"));
		return sb.ToString();
	}

	private static bool IsValidResourceSegment(string id)
	{
		if (id.Length == 0)
		{
			return false;
		}

		char first = id[0];
		if (!char.IsAsciiLetter(first) && first != '_')
		{
			return false;
		}

		for (int i = 1; i < id.Length; i++)
		{
			char c = id[i];
			if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != '.')
			{
				return false;
			}
		}

		return true;
	}

	private static uint Fnv1a32(string s)
	{
		uint hash = 2166136261u;
		foreach (char c in s)
		{
			hash ^= (byte)(c & 0xFF);
			hash *= 16777619u;
			hash ^= (byte)(c >> 8);
			hash *= 16777619u;
		}

		return hash;
	}

	// Action hook implementation -----------------------------------------------

	// Executes a platform-neutral action request against the element by mapping it
	// to the corresponding Android action ID and routing it through the real
	// AccessibilityNodeProvider path (OnPerformActionForVirtualView).
	// Assumes the caller is already on the main thread; dispatch is the
	// responsibility of AndroidSkiaAccessibility.PerformActionForElement.
	internal bool ExecuteAction(UIElement element, AccessibilityNativeActionRequest request)
	{
		var ids = new List<Integer>();
		GetVisibleVirtualViews(ids);

		if (!_elementToId.TryGetValue(element, out var box) || !_orderedIdSet.Contains(box.Value))
		{
			return false;
		}

		if (request.Action is
			AccessibilityNativeAction.ChangeView or
			AccessibilityNativeAction.ZoomIn or
			AccessibilityNativeAction.ZoomOut or
			AccessibilityNativeAction.Zoom or
			AccessibilityNativeAction.SetDockPosition or
			AccessibilityNativeAction.SetWindowVisualState or
			AccessibilityNativeAction.Move or
			AccessibilityNativeAction.Resize or
			AccessibilityNativeAction.Rotate or
			AccessibilityNativeAction.ScrollIntoView or
			AccessibilityNativeAction.Realize)
		{
			return element.GetOrCreateAutomationPeer() is { } peer &&
				ExecuteAdvancedAction(peer, request);
		}

		var (actionId, bundle) = MapRequestToAndroidAction(request);
		if (actionId <= 0)
		{
			return false;
		}

		// Route through the real provider so the action path matches TalkBack exactly.
		var provider = GetAccessibilityNodeProvider(_host);
		if (provider is not null)
		{
			return provider.PerformAction(box.Value, actionId, bundle);
		}

		// Fallback: call directly when the provider isn't reachable (e.g. no active service).
		return OnPerformActionForVirtualView(box.Value, actionId, bundle);
	}

	private static (int actionId, Bundle? bundle) MapRequestToAndroidAction(
		AccessibilityNativeActionRequest request)
	{
		switch (request.Action)
		{
			case AccessibilityNativeAction.Activate:
				return (s_actionClickId, null);

			case AccessibilityNativeAction.Expand:
				return (s_actionExpandId, null);

			case AccessibilityNativeAction.Collapse:
				return (s_actionCollapseId, null);

			// Increment and ScrollForward both use the same Android ID; OnPerformAction
			// disambiguates by inspecting the peer's provider patterns.
			case AccessibilityNativeAction.Increment:
			case AccessibilityNativeAction.ScrollForward:
				return (s_actionScrollForwardId, null);

			case AccessibilityNativeAction.Decrement:
			case AccessibilityNativeAction.ScrollBackward:
				return (s_actionScrollBackwardId, null);

			case AccessibilityNativeAction.SetValue when request.Text is not null:
			{
				var bundle = new Bundle();
				bundle.PutString(ActionArgumentSetTextCharsequenceKey, request.Text);
				return (s_actionSetTextId, bundle);
			}

			case AccessibilityNativeAction.SetRangeValue when s_actionSetProgressId >= 0:
			{
				var bundle = new Bundle();
				bundle.PutFloat(ActionArgumentProgressValueKey, (float)request.Number);
				return (s_actionSetProgressId, bundle);
			}

			case AccessibilityNativeAction.Dismiss:
				return (s_actionDismissId, null);

			// ScrollIntoView and Realize have no standard Android action IDs.
			default:
				return (0, null);
		}
	}

	private static string? GetSetTextArgument(Bundle? bundle)
	{
		if (bundle is null)
		{
			return null;
		}

		// TalkBack sends text as a CharSequence; the test accessor uses PutString.
		// Try CharSequence first for TalkBack compatibility, then fall back to string.
		return bundle.GetCharSequence(ActionArgumentSetTextCharsequenceKey)?.ToString()
			?? bundle.GetString(ActionArgumentSetTextCharsequenceKey);
	}
}
