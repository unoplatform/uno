#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Accessibility;
using CoreGraphics;
using Foundation;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using UIKit;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

/// <summary>
/// Per-XamlRoot accessibility adapter for Skia iOS/tvOS/macCatalyst.
/// Owns a stable registry of <see cref="UnoUIAccessibilityElement"/> objects hosted by the
/// Skia/Metal view's <c>AccessibilityElements</c> collection, and pulls all accessibility
/// property values live from the resolved automation peer on native demand.
/// </summary>
internal sealed class AppleUIKitAccessibility : SkiaAccessibilityBase
{
	private const int MaxRecordedEventCount = 256;

	// Stored so GetAllElementsForRoot can reject queries for a different window.
	private readonly XamlRoot _xamlRoot;
	private readonly WeakReference<RootViewController> _controllerRef;
	private readonly WeakReference<AppleUIKitAccessibility> _selfRef;

	// Stable node registry: Visual.Handle -> native element
	private readonly Dictionary<nint, UnoUIAccessibilityElement> _nodeElements = new();
	private UIAccessibilityElement[] _currentAccessibilityElements = Array.Empty<UIAccessibilityElement>();
	private static readonly NSString _accessibilityElementsKey = new("accessibilityElements");

	// Weak owner references for live property pull
	private readonly Dictionary<nint, WeakReference<UIElement>> _handleToOwner = new();

	private bool _rebuildPending;
	private bool _initialBuildDone;
	private bool _forceStructureNotification;
	private bool _screenChangeRequested;
	private nint _screenChangeTargetHandle;
	private List<nint> _lastOrderedHandles = new();
	private List<nint> _nextOrderedHandles = new();
	private readonly HashSet<nint> _pendingInvalidationHandles = new();
	private bool _invalidationFlushScheduled;
	private readonly List<AccessibilityNativeEventRecord> _recordedEvents = new();
	private bool _recordEvents;

	// Tracks ALL subscribed scroll sources in the current visual tree, including
	// Raw/peerless containers that are not promoted accessibility owners.
	private readonly Dictionary<nint, WeakReference<UIElement>> _allScrollSources = new();

	// Focus synchronization state
	// Guards against XAML -> native -> XAML and native -> XAML -> native loops.

	// Handle last set via SetNativeFocus (XAML -> native). Cleared when VoiceOver
	// confirms focus by firing accessibilityElementDidBecomeFocused.
	private nint _pendingNativeFocusHandle;

	// True while the native -> XAML handoff is in progress (setting XAML focus from
	// accessibilityElementDidBecomeFocused). Prevents SetNativeFocus from re-posting.
	private bool _settingXamlFocus;

	// The last handle whose native element received or was requested for focus.
	// Exposed via IOSFocusedNativeNodeAccessor.
	private nint _lastNativeFocusedHandle;

	// Modal state
	// Handle of the modal peer owner element while a modal is active.
	private nint _activeModalHandle;
	private nint _modalScopeHandle;
	private volatile bool _modalScopeDirty = true;

	private readonly List<(nint ModalHandle, nint PreviousFocusHandle)> _modalFocusStack = new();

	private sealed class AdapterRegistration
	{
		internal AdapterRegistration(AppleUIKitAccessibility adapter)
			=> Adapter = new WeakReference<AppleUIKitAccessibility>(adapter);

		internal WeakReference<AppleUIKitAccessibility> Adapter { get; }
	}

	private static readonly ConditionalWeakTable<XamlRoot, AdapterRegistration> _adapterRegistry = new();

	private static readonly object _registryLock = new();

	private static volatile bool _staticDispatchersInstalled;

	/// <summary>
	/// Installs process-stable static dispatchers on the global IOS* hooks once.
	/// Each dispatcher resolves the target adapter from the registry and delegates.
	/// Safe to call from every constructor; subsequent calls are no-ops.
	/// </summary>
	private static void EnsureStaticDispatchers()
	{
		if (_staticDispatchersInstalled)
		{
			return;
		}

		lock (_registryLock)
		{
			if (_staticDispatchersInstalled)
			{
				return;
			}

			AccessibilityPeerHelper.IOSAccessibilityElementAccessor =
				element => FindAdapterForElement(element)?.GetElementForOwner(element);

			AccessibilityPeerHelper.IOSAccessibilityElementCountAccessor =
				root => FindAdapterForRoot(root)?._nodeElements.Count ?? 0;

			AccessibilityPeerHelper.IOSAllElementsForRootAccessor =
				root => FindAdapterForRoot(root)?.GetAllElementsForRoot(root);

			AccessibilityPeerHelper.IOSAccessibilityNodeSnapshotAccessor =
				element => FindAdapterForElement(element)?.GetSnapshotForOwner(element);

			AccessibilityPeerHelper.IOSAllNodeSnapshotsForRootAccessor =
				root => FindAdapterForRoot(root)?.GetAllSnapshotsForRoot(root);

			AccessibilityPeerHelper.IOSAccessibilityActionAccessor =
				(element, request) =>
					FindAdapterForElement(element)?.ExecuteAction(element, request) ?? false;

			AccessibilityPeerHelper.IOSAccessibilityFocusAccessor =
				element => FindAdapterForElement(element)?.RequestNativeFocus(element) ?? false;

			AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor =
				root => FindAdapterForRoot(root)?.GetFocusedNativeNode(root);

			AccessibilityPeerHelper.IOSAccessibilityEventsAccessor =
				root => FindAdapterForRoot(root)?.GetEventsForRoot(root);

			AccessibilityPeerHelper.IOSClearAccessibilityEventsAction =
				root => FindAdapterForRoot(root)?.ClearEventsForRoot(root);

			_staticDispatchersInstalled = true;
		}
	}

	private static void RegisterAdapter(XamlRoot root, AppleUIKitAccessibility adapter)
	{
		lock (_registryLock)
		{
			_adapterRegistry.Remove(root);
			_adapterRegistry.Add(root, new AdapterRegistration(adapter));
		}
	}

	private static void UnregisterAdapter(XamlRoot root, AppleUIKitAccessibility adapter)
	{
		lock (_registryLock)
		{
			if (_adapterRegistry.TryGetValue(root, out var registration) &&
				registration.Adapter.TryGetTarget(out var existing) &&
				ReferenceEquals(existing, adapter))
			{
				_adapterRegistry.Remove(root);
			}
		}
	}

	/// <summary>Resolves the live adapter for <paramref name="root"/>, or null.</summary>
	private static AppleUIKitAccessibility? FindAdapterForRoot(XamlRoot? root)
	{
		if (root is null)
		{
			return null;
		}

		if (_adapterRegistry.TryGetValue(root, out var registration) &&
			registration.Adapter.TryGetTarget(out var adapter) &&
			!adapter.IsDisposed)
		{
			return adapter;
		}

		return null;
	}

	/// <summary>
	/// Resolves the adapter for the XamlRoot that contains <paramref name="element"/>.
	/// Returns null when the element has no associated root or the root has no adapter.
	/// </summary>
	private static AppleUIKitAccessibility? FindAdapterForElement(UIElement? element)
		=> FindAdapterForRoot(element?.XamlRoot);

	internal AppleUIKitAccessibility(XamlRoot xamlRoot, RootViewController viewController)
	{
		_xamlRoot = xamlRoot;
		_controllerRef = new WeakReference<RootViewController>(viewController);
		_selfRef = new WeakReference<AppleUIKitAccessibility>(this);

		// Install process-stable static dispatchers on the first created root, then
		// register this adapter so dispatchers can route queries to the right instance.
		EnsureStaticDispatchers();
		RegisterAdapter(_xamlRoot, this);
		Trace("Configured AppleUIKit accessibility adapter.");

		// Schedule an initial tree build in case XAML content already exists before the
		// router's OnChildAdded callbacks start arriving (e.g., Window re-activation path).
		ScheduleRebuild();
	}

	// IAccessibilityOwner

	public override bool IsAccessibilityEnabled => true;

	protected override bool ShouldInvalidateOnScroll
		=> UIAccessibility.IsVoiceOverRunning || UIAccessibility.IsSwitchControlRunning;

	protected override bool IsBlockedByActiveModal(UIElement element)
	{
		var modalHandle = GetCurrentModalScopeHandle();
		return modalHandle != 0 && !IsDescendantOf(element, modalHandle);
	}

	private void Trace(string message)
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"[A11y] {message}");
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

		if (_recordedEvents.Count >= MaxRecordedEventCount)
		{
			_recordedEvents.RemoveAt(0);
		}

		var record = new AccessibilityNativeEventRecord(kind, name, text);
		_recordedEvents.Add(record);
		Trace($"Recorded native event {record.Kind}.");
	}

	// Narrow test access

	private object? GetElementForOwner(UIElement element)
	{
		if (IsBlockedByActiveModal(element))
		{
			return null;
		}

		_nodeElements.TryGetValue(element.Visual.Handle, out var el);
		return el;
	}

	/// <summary>
	/// Invokes the real native focus-notification path for the given element.
	/// Posts UIAccessibilityPostNotification.ScreenChanged to the stable element.
	/// Returns false if the element has no registered native node.
	/// Registered as <see cref="AccessibilityPeerHelper.IOSAccessibilityFocusAccessor"/>.
	/// </summary>
	private bool RequestNativeFocus(UIElement element)
	{
		if (IsBlockedByActiveModal(element))
		{
			return false;
		}

		var handle = element.Visual.Handle;
		if (!_nodeElements.TryGetValue(handle, out var el))
		{
			return false;
		}

		_pendingNativeFocusHandle = handle;
		_lastNativeFocusedHandle = handle;
		var captured = el;
		PostOnMain(() =>
			UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, captured));
		return true;
	}

	/// <summary>
	/// Returns the native UIAccessibilityElement (as object?) for the last focused node
	/// on the given XamlRoot, or null if none. Used by tests without UIKit references.
	/// Registered as <see cref="AccessibilityPeerHelper.IOSFocusedNativeNodeAccessor"/>.
	/// </summary>
	private object? GetFocusedNativeNode(XamlRoot xamlRoot)
	{
		if (!ReferenceEquals(xamlRoot, _xamlRoot))
		{
			return null;
		}

		if (_lastNativeFocusedHandle == 0 ||
			!_nodeElements.TryGetValue(_lastNativeFocusedHandle, out var el))
		{
			return null;
		}

		return el;
	}

	private object[]? GetAllElementsForRoot(XamlRoot xamlRoot)
	{
		// Reject queries for a different window's root.
		if (!ReferenceEquals(xamlRoot, _xamlRoot))
		{
			return null;
		}

		return _currentAccessibilityElements.Length > 0
			? _currentAccessibilityElements.Cast<object>().ToArray()
			: null;
	}

	private AccessibilityNativeNodeSnapshot? GetSnapshotForOwner(UIElement element)
	{
		if (IsBlockedByActiveModal(element))
		{
			return null;
		}

		return _nodeElements.TryGetValue(element.Visual.Handle, out var accessibilityElement)
			? CreateSnapshot(accessibilityElement)
			: null;
	}

	private AccessibilityNativeNodeSnapshot[]? GetAllSnapshotsForRoot(XamlRoot xamlRoot)
	{
		if (!ReferenceEquals(xamlRoot, _xamlRoot))
		{
			return null;
		}

		return _currentAccessibilityElements
			.OfType<UnoUIAccessibilityElement>()
			.Select(CreateSnapshot)
			.ToArray();
	}

	private void SetAccessibilityElements(
		UnoSKMetalView metalView,
		IReadOnlyList<UIAccessibilityElement>? elements)
	{
		if (elements is not { Count: > 0 })
		{
			if (_currentAccessibilityElements.Length == 0)
			{
				return;
			}

			_currentAccessibilityElements = Array.Empty<UIAccessibilityElement>();
			metalView.SetValueForKey(null!, _accessibilityElementsKey);
			return;
		}

		if (_currentAccessibilityElements.Length == elements.Count)
		{
			var unchanged = true;
			for (var i = 0; i < elements.Count; i++)
			{
				if (!ReferenceEquals(_currentAccessibilityElements[i], elements[i]))
				{
					unchanged = false;
					break;
				}
			}

			if (unchanged)
			{
				return;
			}
		}

		_currentAccessibilityElements = elements.ToArray();
		using var array = NSArray.FromNSObjects(_currentAccessibilityElements);
		metalView.SetValueForKey(array, _accessibilityElementsKey);
	}

	// Initial build hook called from NativeWindowWrapper.ShowCore

	/// <summary>
	/// Schedules an accessibility tree build once the window's root content is loaded.
	/// Called from <see cref="NativeWindowWrapper.ShowCore"/> after the root
	/// FrameworkElement raises its <c>Loaded</c> event, ensuring the tree is built even
	/// when no child-add callbacks arrive (e.g., static content set before adapter creation).
	/// </summary>
	internal void TriggerInitialBuild() => ScheduleRebuild();

	// Tree management

	protected override void OnChildAdded(UIElement parent, UIElement child, int? index)
		=> ScheduleRebuild();

	protected override void OnChildRemoved(UIElement parent, UIElement child)
		=> ScheduleRebuild();

	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		if (visual is ContainerVisual { Owner.Target: UIElement owner })
		{
			InvalidateElement(owner.Visual.Handle);
		}
	}

	private void ScheduleRebuild()
	{
		_modalScopeDirty = true;

		// Prevent scheduling after the adapter has been disposed or a rebuild is already pending.
		if (_rebuildPending || IsDisposed)
		{
			return;
		}

		_rebuildPending = true;
		Trace("Queued accessibility tree rebuild.");
		NativeDispatcher.Main.Enqueue(RebuildTree);
	}

	private void RebuildTree()
	{
		_rebuildPending = false;

		if (IsDisposed)
		{
			return;
		}

		if (!_controllerRef.TryGetTarget(out var controller))
		{
			return;
		}

		var metalView = controller.SkCanvasView;
		if (metalView is null)
		{
			return;
		}

		var root = controller.RootElement;
		if (root is null)
		{
			var hadNodes = _lastOrderedHandles.Count > 0;

			// Clear stale state when root is removed; _allScrollSources is the authoritative
			// source for scroll subscriptions so we unsubscribe from it directly.
			foreach (var (_, weakEl) in _allScrollSources)
			{
				if (weakEl.TryGetTarget(out var scrollEl))
				{
					TryUnsubscribeScrollSource(scrollEl);
				}
			}

			_allScrollSources.Clear();
			_nodeElements.Clear();
			_handleToOwner.Clear();
			_lastOrderedHandles.Clear();
			_nextOrderedHandles.Clear();
			_pendingInvalidationHandles.Clear();
			_forceStructureNotification = false;
			_screenChangeRequested = false;
			_screenChangeTargetHandle = 0;
			_activeModalHandle = 0;
			_modalScopeHandle = 0;
			_modalScopeDirty = true;
			_modalFocusStack.Clear();
			_pendingNativeFocusHandle = 0;
			_lastNativeFocusedHandle = 0;
			metalView.IsAccessibilityElement = false;
			SetAccessibilityElements(metalView, null);
			if (hadNodes)
			{
				RecordEvent(AccessibilityNativeEventKind.StructureChanged);
				UIAccessibility.PostNotification(
					UIAccessibilityPostNotification.LayoutChanged,
					null);
			}
			return;
		}

		var forceStructureNotification = _forceStructureNotification;
		_forceStructureNotification = false;
		var screenChangeRequested = _screenChangeRequested;
		var screenChangeTargetHandle = _screenChangeTargetHandle;
		_screenChangeRequested = false;
		_screenChangeTargetHandle = 0;

		var compareOrder = _initialBuildDone;
		_initialBuildDone = true;

		var nodes = AccessibilityPeerHelper.GetPeerTree(root);
		Trace($"Rebuilding accessibility tree from {nodes.Count} peer node(s).");
		var newHandles = new HashSet<nint>(nodes.Count);
		var orderedElements = new List<UIAccessibilityElement>(nodes.Count);

		// Build handle-to-node-index mapping for modal-subtree filtering.
		var handleToNodeIndex = new Dictionary<nint, int>(nodes.Count);

		for (int i = 0; i < nodes.Count; i++)
		{
			var node = nodes[i];

			// Ownerless item peers are unrealized and have no native node yet.
			// the shared peer tree no longer borrows the parent ItemsControl handle
			// for virtual items without a realized container. Skip these nodes so we
			// never create or retain a native element with the parent's handle.
			// The element will be added on the next rebuild once its container is realized.
			var owner = node.Owner;

			if (owner is null)
			{
				continue;
			}

			var handle = owner.Visual.Handle;
			newHandles.Add(handle);
			handleToNodeIndex.TryAdd(handle, i);

			if (!_nodeElements.TryGetValue(handle, out var el))
			{
				el = new UnoUIAccessibilityElement(metalView, handle, _selfRef);
				_nodeElements[handle] = el;
				_handleToOwner[handle] = new WeakReference<UIElement>(owner);
			}

			el.InvalidateCachedAccessibilityData();
			orderedElements.Add(el);
		}

		// Remove stale nodes no longer in the promoted tree.
		var toRemove = new List<nint>();
		foreach (var handle in _nodeElements.Keys)
		{
			if (!newHandles.Contains(handle))
			{
				toRemove.Add(handle);
			}
		}

		foreach (var handle in toRemove)
		{
			_nodeElements.Remove(handle);
			_handleToOwner.Remove(handle);
		}

		// If the tracked focus handle was removed from the registry, clear it.
		if (_lastNativeFocusedHandle != 0 && !_nodeElements.ContainsKey(_lastNativeFocusedHandle))
		{
			_lastNativeFocusedHandle = 0;
		}

		// Subscribe scroll sources for the full visual tree (including Raw/peerless containers)
		// and unsubscribe any that are no longer present.
		RefreshScrollSources(root);

		// The Metal view is a container, not an element itself.
		metalView.IsAccessibilityElement = false;

		// Detect add, remove, or reorder by comparing the ordered handle sequence.
		_nextOrderedHandles.Clear();
		foreach (var el in orderedElements)
		{
			_nextOrderedHandles.Add(((UnoUIAccessibilityElement)el).NodeId);
		}

		var structureChanged = forceStructureNotification ||
			(compareOrder &&
				!_nextOrderedHandles.SequenceEqual(_lastOrderedHandles));

		if (structureChanged)
		{
			RecordEvent(AccessibilityNativeEventKind.StructureChanged);
		}

		(_lastOrderedHandles, _nextOrderedHandles) = (_nextOrderedHandles, _lastOrderedHandles);

		// Apply modal filtering if an active modal is present in the tree.
		var modalOwner = FindActiveModalOwner(nodes, out int modalNodeIndex);
		var currentModalHandle = modalOwner?.Visual.Handle ?? 0;
		_modalScopeHandle = currentModalHandle;
		_modalScopeDirty = false;
		var screenChangeHandled = false;

		if (currentModalHandle != 0)
		{
			var isNewModal = _activeModalHandle != currentModalHandle;
			nint transitionFocusHandle = 0;
			if (isNewModal)
			{
				if (_nodeElements.TryGetValue(_activeModalHandle, out var previousModalElement))
				{
					previousModalElement.IsModalContainer = false;
				}

				var existingIndex = _modalFocusStack.FindLastIndex(
					entry => entry.ModalHandle == currentModalHandle);
				if (existingIndex >= 0)
				{
					for (var index = _modalFocusStack.Count - 1; index > existingIndex; index--)
					{
						transitionFocusHandle = _modalFocusStack[index].PreviousFocusHandle;
						_modalFocusStack.RemoveAt(index);
					}
				}
				else
				{
					while (_modalFocusStack.Count > 0 &&
						!_nodeElements.ContainsKey(_modalFocusStack[^1].ModalHandle))
					{
						_modalFocusStack.RemoveAt(_modalFocusStack.Count - 1);
					}

					_modalFocusStack.Add((currentModalHandle, _lastNativeFocusedHandle));
				}

				_activeModalHandle = currentModalHandle;
			}

			// Filter to only the modal's peer subtree.
			var modalElements = FilterToModalSubtree(
				nodes, modalNodeIndex, orderedElements, handleToNodeIndex);

			// Mark the modal owner element so VoiceOver excludes background peers.
			if (_nodeElements.TryGetValue(currentModalHandle, out var modalEl))
			{
				modalEl.IsModalContainer = true;
			}

			SetAccessibilityElements(metalView, modalElements);

			if (isNewModal && modalElements.Count > 0)
			{
				var target = transitionFocusHandle != 0 &&
					_nodeElements.TryGetValue(transitionFocusHandle, out var transitionFocusElement)
						? transitionFocusElement
						: modalElements[0];
				PostOnMain(() =>
					UIAccessibility.PostNotification(
						UIAccessibilityPostNotification.ScreenChanged, target));
				screenChangeHandled = true;
			}
			else if (!isNewModal && structureChanged)
			{
				PostOnMain(() =>
				{
					if (!IsDisposed)
					{
						// null: structure updated within active modal; do not move focus.
						UIAccessibility.PostNotification(
							UIAccessibilityPostNotification.LayoutChanged, null);
					}
				});
			}
		}
		else if (_activeModalHandle != 0)
		{
			// The modal just closed; clear modal state and restore the full element list.
			if (_nodeElements.TryGetValue(_activeModalHandle, out var prevModalEl))
			{
				prevModalEl.IsModalContainer = false;
			}

			_activeModalHandle = 0;
			SetAccessibilityElements(metalView, orderedElements);

			var restoreHandle = _modalFocusStack.Count > 0
				? _modalFocusStack[0].PreviousFocusHandle
				: 0;
			_modalFocusStack.Clear();

			if (restoreHandle != 0 && _nodeElements.TryGetValue(restoreHandle, out var restoreEl))
			{
				var captured = restoreEl;
				PostOnMain(() =>
					UIAccessibility.PostNotification(
						UIAccessibilityPostNotification.ScreenChanged, captured));
				screenChangeHandled = true;
			}
			else if (orderedElements.Count > 0)
			{
				var firstEl = orderedElements[0];
				PostOnMain(() =>
					UIAccessibility.PostNotification(
						UIAccessibilityPostNotification.ScreenChanged, firstEl));
				screenChangeHandled = true;
			}

			ResetAnnouncementTracking();
		}
		else
		{
			// Normal path with no active modal.
			SetAccessibilityElements(metalView, orderedElements);

			if (structureChanged)
			{
				PostOnMain(() =>
				{
					if (!IsDisposed)
					{
						// null: structure updated; do not move VoiceOver focus.
						UIAccessibility.PostNotification(
							UIAccessibilityPostNotification.LayoutChanged, null);
					}
				});
			}
		}

		if (screenChangeRequested && !screenChangeHandled)
		{
			UIAccessibilityElement? target = null;
			if (screenChangeTargetHandle != 0 &&
				_nodeElements.TryGetValue(screenChangeTargetHandle, out var requestedTarget))
			{
				target = requestedTarget;
			}

			if (target is null &&
				_lastNativeFocusedHandle != 0 &&
				_nodeElements.TryGetValue(_lastNativeFocusedHandle, out var focusedTarget))
			{
				target = focusedTarget;
			}

			target ??= orderedElements.FirstOrDefault();
			PostOnMain(() =>
			{
				if (!IsDisposed)
				{
					UIAccessibility.PostNotification(
						UIAccessibilityPostNotification.ScreenChanged,
						target);
				}
			});
		}
	}

	// Modal helpers

	/// <summary>
	/// Finds the first peer in the node list that acts as an active modal container.
	/// Detection uses both the exposed Window pattern and direct IWindowProvider cast so
	/// that ContentDialog popups (IsLightDismissEnabled=false, no pattern exposed) are
	/// also matched.
	/// </summary>
	private static UIElement? FindActiveModalOwner(
		IReadOnlyList<AccessibilityPeerNode> nodes,
		out int modalNodeIndex)
	{
		UIElement? modalOwner = null;
		modalNodeIndex = -1;

		for (int i = 0; i < nodes.Count; i++)
		{
			var peer = nodes[i].ProviderPeer;
			if (nodes[i].Owner is not { } owner)
			{
				continue;
			}

			if (peer.IsDialog())
			{
				modalOwner = owner;
				modalNodeIndex = i;
				continue;
			}

			// Primary: Window pattern (respects ShouldExposeWindowPattern gating).
			var wp = peer.GetPattern(PatternInterface.Window) as IWindowProvider;

			// Secondary: direct cast for Window-typed peers whose pattern is gated off
			// (e.g., Popup with IsLightDismissEnabled=false used by ContentDialog).
			if (wp is null &&
				peer is PopupAutomationPeer { Owner: Popup { IsOpen: true } } popupPeer)
			{
				wp = popupPeer;
			}

			if (wp is not null)
			{
				bool isModal;
				try
				{
					isModal = wp.IsModal;
				}
				catch (ElementNotAvailableException)
				{
					isModal = false;
				}
				catch (InvalidOperationException)
				{
					isModal = false;
				}

				if (isModal)
				{
					modalOwner = owner;
					modalNodeIndex = i;
				}
			}
		}

		return modalOwner;
	}

	private nint GetCurrentModalScopeHandle()
	{
		if (!_modalScopeDirty)
		{
			return _modalScopeHandle;
		}

		var root = _xamlRoot.Content as UIElement ?? _xamlRoot.VisualTree.RootElement;
		var modalOwner = root is null
			? null
			: FindActiveModalOwner(AccessibilityPeerHelper.GetPeerTree(root), out _);
		_modalScopeHandle = modalOwner?.Visual.Handle ?? 0;
		_modalScopeDirty = false;
		return _modalScopeHandle;
	}

	/// <summary>
	/// Returns the subset of <paramref name="allElements"/> whose peer nodes are the modal
	/// peer itself or one of its descendants (determined via the <see cref="AccessibilityPeerNode.ParentIndex"/>
	/// chain, which follows DFS parent-before-child order).
	/// </summary>
	private static List<UIAccessibilityElement> FilterToModalSubtree(
		IReadOnlyList<AccessibilityPeerNode> nodes,
		int modalNodeIndex,
		List<UIAccessibilityElement> allElements,
		Dictionary<nint, int> handleToNodeIndex)
	{
		// Single forward pass is sufficient because parents always precede children
		// in DFS order, so once a node is in modalIndices its children appear later.
		var modalIndices = new HashSet<int> { modalNodeIndex };
		for (int i = modalNodeIndex + 1; i < nodes.Count; i++)
		{
			if (nodes[i].ParentIndex.HasValue &&
				modalIndices.Contains(nodes[i].ParentIndex!.Value))
			{
				modalIndices.Add(i);
			}
		}

		var result = new List<UIAccessibilityElement>(allElements.Count);
		foreach (var el in allElements)
		{
			var nodeId = ((UnoUIAccessibilityElement)el).NodeId;
			if (handleToNodeIndex.TryGetValue(nodeId, out var nodeIdx) &&
				modalIndices.Contains(nodeIdx))
			{
				result.Add(el);
			}
		}

		return result;
	}

	// Pull property methods called by UnoUIAccessibilityElement

	internal string? GetLabel(nint handle)
	{
		var peer = ResolvePeer(handle);
		return peer is null ? null : ResolveLabel(peer);
	}

	internal string? GetHint(nint handle)
	{
		var peer = ResolvePeer(handle);
		if (peer is null)
		{
			return null;
		}

		List<string>? parts = null;
		var fullDescription = peer.GetFullDescription();
		if (!string.IsNullOrEmpty(fullDescription))
		{
			(parts ??= new()).Add(fullDescription);
		}
		else if (peer.GetHelpText() is { Length: > 0 } helpText)
		{
			(parts ??= new()).Add(helpText);
		}

		if (_handleToOwner.TryGetValue(handle, out var ownerReference) &&
			ownerReference.TryGetTarget(out var owner))
		{
			if (owner.GetValue(AutomationProperties.DescribedByProperty) is
				IEnumerable<DependencyObject> describedBy)
			{
				foreach (var candidate in describedBy)
				{
					if (candidate is not UIElement descriptionElement)
					{
						continue;
					}

					var description = ReferenceEquals(descriptionElement.XamlRoot, _xamlRoot) &&
						_nodeElements.ContainsKey(descriptionElement.Visual.Handle) &&
						descriptionElement.GetOrCreateAutomationPeer() is { } descriptionPeer
						? ResolveLabel(descriptionPeer)
						: null;
					if (!string.IsNullOrEmpty(description) &&
						(parts is null || !parts.Contains(description)))
					{
						(parts ??= new()).Add(description);
					}
				}
			}
		}

		return parts is { Count: > 0 } ? string.Join(". ", parts) : null;
	}

	internal string? GetValue(nint handle)
	{
		var peer = ResolvePeer(handle);
		if (peer is null)
		{
			return null;
		}

		if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggle)
		{
			return toggle.ToggleState switch
			{
				ToggleState.On => "1",
				ToggleState.Off => "0",
				_ => "mixed",
			};
		}

		if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider range)
		{
			return range.Value.ToString(CultureInfo.CurrentCulture);
		}

		if (peer.GetPattern(PatternInterface.Value) is IValueProvider value)
		{
			// Never expose password text across the accessibility boundary.
			return peer.IsPassword() ? null : value.Value;
		}

		if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selection)
		{
			return IsSelected(handle, peer, selection) ? "1" : "0";
		}

		return peer.GetItemStatus() is { Length: > 0 } itemStatus
			? itemStatus
			: null;
	}

	internal UIAccessibilityTrait GetTraits(nint handle)
	{
		var peer = ResolvePeer(handle);
		if (peer is null)
		{
			return UIAccessibilityTrait.None;
		}

		var isSelected = peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selection &&
			IsSelected(handle, peer, selection);
		return BuildTraits(peer, isSelected);
	}

	private bool IsSelected(
		nint handle,
		AutomationPeer peer,
		ISelectionItemProvider selection)
		=> peer.IsEnabled()
			? selection.IsSelected
			: _handleToOwner.TryGetValue(handle, out var weakOwner) &&
				weakOwner.TryGetTarget(out var owner) &&
				owner is SelectorItem { IsSelected: true };

	internal string? GetIdentifier(nint handle)
	{
		// Read the AutomationId directly from the owner element, bypassing EventsSource
		// resolution. EventsSource can redirect to a data peer whose GetAutomationId()
		// returns a synthetic or data-object ID, not the developer-set container ID that
		// XCUITest and automation clients expect for element lookup.
		//
		// Returns null when the property is unset (default is "") or explicitly empty.
		// Whitespace-only IDs are passed through unchanged, matching WinUI string
		// semantics: the framework does not normalize or trim AutomationId values.
		if (!_handleToOwner.TryGetValue(handle, out var weakRef) ||
			!weakRef.TryGetTarget(out var owner))
		{
			return null;
		}

		var id = AutomationProperties.GetAutomationId(owner);
		return string.IsNullOrEmpty(id) ? null : id;
	}

	internal CGRect GetFrameInContainerSpace(nint handle)
	{
		if (!_handleToOwner.TryGetValue(handle, out var weakRef) ||
			!weakRef.TryGetTarget(out var element))
		{
			return CGRect.Empty;
		}

		try
		{
			if (ResolvePeer(handle)?.GetBoundingRectangle() is { } peerBounds &&
				HasFiniteBounds(peerBounds))
			{
				return new CGRect(
					peerBounds.X,
					peerBounds.Y,
					peerBounds.Width,
					peerBounds.Height);
			}

			var transform = element.TransformToVisual(null);
			var bounds = transform.TransformBounds(
				new Windows.Foundation.Rect(
					0,
					0,
					element.Visual.Size.X,
					element.Visual.Size.Y));
			return new CGRect(bounds.X, bounds.Y, bounds.Width, bounds.Height);
		}
		catch (InvalidOperationException ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] GetFrameInContainerSpace failed: {ex.Message}");
			}

			return CGRect.Empty;
		}
	}

	private static bool HasFiniteBounds(Windows.Foundation.Rect bounds)
		=> double.IsFinite(bounds.X)
			&& double.IsFinite(bounds.Y)
			&& double.IsFinite(bounds.Width)
			&& double.IsFinite(bounds.Height);

	internal bool Activate(nint handle)
	{
		var peer = ResolvePeer(handle);
		if (peer is null)
		{
			return false;
		}

		return peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider
			? AccessibilityPeerHelper.TryToggleSelection(peer)
			: AccessibilityPeerHelper.TryInvokeDefaultAction(peer);
	}

	internal bool Increment(nint handle)
	{
		var peer = ResolvePeer(handle);
		return peer is not null && AccessibilityPeerHelper.TryIncrement(peer);
	}

	internal bool Decrement(nint handle)
	{
		var peer = ResolvePeer(handle);
		return peer is not null && AccessibilityPeerHelper.TryDecrement(peer);
	}

	internal bool Scroll(nint handle, UIAccessibilityScrollDirection direction)
	{
		var peer = ResolvePeer(handle);
		if (peer is null ||
			peer.GetPattern(PatternInterface.Scroll) is not IScrollProvider provider)
		{
			return false;
		}

		var forward = direction is
			UIAccessibilityScrollDirection.Right or
			UIAccessibilityScrollDirection.Down or
			UIAccessibilityScrollDirection.Next;
		var amount = forward ? ScrollAmount.SmallIncrement : ScrollAmount.SmallDecrement;

		if (direction is UIAccessibilityScrollDirection.Right or UIAccessibilityScrollDirection.Left)
		{
			return provider.HorizontallyScrollable &&
				AccessibilityPeerHelper.TryScroll(peer, amount, ScrollAmount.NoAmount);
		}

		if (provider.VerticallyScrollable)
		{
			return AccessibilityPeerHelper.TryScroll(peer, ScrollAmount.NoAmount, amount);
		}

		return provider.HorizontallyScrollable &&
			AccessibilityPeerHelper.TryScroll(peer, amount, ScrollAmount.NoAmount);
	}

	internal bool PerformEscape(nint handle)
	{
		var peer = ResolvePeer(handle);
		return peer is not null && AccessibilityPeerHelper.TryClose(peer);
	}

	/// <summary>
	/// Returns the currently-valid custom actions for a native element.
	/// Advertises only actions that are applicable given the peer's current state.
	/// </summary>
	internal UIAccessibilityCustomAction[]? GetCustomActions(nint handle)
	{
		var peer = ResolvePeer(handle);
		if (peer is null)
		{
			return null;
		}

		if (!peer.IsEnabled())
		{
			return null;
		}

		var list = new List<UIAccessibilityCustomAction>();
		var weakSelf = _selfRef;

		if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider ec)
		{
			if (ec.ExpandCollapseState is ExpandCollapseState.Collapsed)
			{
				list.Add(CreateCustomAction(
					Localize("Expand"),
					_ =>
					{
						if (!weakSelf.TryGetTarget(out var self)) return false;
						var p = self.ResolvePeer(handle);
						return p is not null && AccessibilityPeerHelper.TryExpand(p);
					}));
			}
			else if (ec.ExpandCollapseState is ExpandCollapseState.Expanded)
			{
				list.Add(CreateCustomAction(
					Localize("Collapse"),
					_ =>
					{
						if (!weakSelf.TryGetTarget(out var self)) return false;
						var p = self.ResolvePeer(handle);
						return p is not null && AccessibilityPeerHelper.TryCollapse(p);
					}));
			}
		}

		if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItem)
		{
			var selectionContainer = peer.GetParent();
			var canSelectMultiple =
				selectionContainer?.GetPattern(PatternInterface.Selection) is ISelectionProvider
				{
					CanSelectMultiple: true,
				};
			var label = canSelectMultiple && selectionItem.IsSelected
				? Localize("Deselect")
				: Localize("Select");
			list.Add(CreateCustomAction(
				label,
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					var p = self.ResolvePeer(handle);
					return p is not null && AccessibilityPeerHelper.TryToggleSelection(p);
				}));
		}

		if (peer.GetPattern(PatternInterface.Scroll) is IScrollProvider scrollProvider &&
			(scrollProvider.HorizontallyScrollable || scrollProvider.VerticallyScrollable))
		{
			list.Add(CreateCustomAction(
				Localize("Scroll Forward"),
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					return self.Scroll(handle, UIAccessibilityScrollDirection.Down);
				}));
			list.Add(CreateCustomAction(
				Localize("Scroll Backward"),
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					return self.Scroll(handle, UIAccessibilityScrollDirection.Up);
				}));
		}

		if (peer.GetPattern(PatternInterface.ScrollItem) is IScrollItemProvider)
		{
			list.Add(CreateCustomAction(
				Localize("Scroll Into View"),
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					var p = self.ResolvePeer(handle);
					return p is not null && AccessibilityPeerHelper.TryScrollIntoView(p);
				}));
		}

		if (peer.GetPattern(PatternInterface.VirtualizedItem) is IVirtualizedItemProvider)
		{
			list.Add(CreateCustomAction(
				Localize("Realize"),
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					var p = self.ResolvePeer(handle);
					return p is not null && AccessibilityPeerHelper.TryRealize(p);
				}));
		}

		if (peer.GetPattern(PatternInterface.Window) is IWindowProvider wp)
		{
			list.Add(CreateCustomAction(
				Localize("Dismiss"),
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					var p = self.ResolvePeer(handle);
					return p is not null && AccessibilityPeerHelper.TryClose(p);
				}));

			// Maximize / Minimize / Restore — advertised only when the provider says the
			// operation is supported and the window is not already in that state.
			var visualState = wp.VisualState;
			var canMax = wp.Maximizable;
			var canMin = wp.Minimizable;

			if (canMax && visualState != WindowVisualState.Maximized)
			{
				list.Add(CreateCustomAction(
					Localize("Maximize"),
					_ =>
					{
						if (!weakSelf.TryGetTarget(out var self)) return false;
						var p = self.ResolvePeer(handle);
						return p is not null &&
							AccessibilityPeerHelper.TrySetWindowVisualState(p, WindowVisualState.Maximized);
					}));
			}

			if (canMin && visualState != WindowVisualState.Minimized)
			{
				list.Add(CreateCustomAction(
					Localize("Minimize"),
					_ =>
					{
						if (!weakSelf.TryGetTarget(out var self)) return false;
						var p = self.ResolvePeer(handle);
						return p is not null &&
							AccessibilityPeerHelper.TrySetWindowVisualState(p, WindowVisualState.Minimized);
					}));
			}

			if (visualState is WindowVisualState.Maximized or WindowVisualState.Minimized)
			{
				list.Add(CreateCustomAction(
					Localize("Restore"),
					_ =>
					{
						if (!weakSelf.TryGetTarget(out var self)) return false;
						var p = self.ResolvePeer(handle);
						return p is not null &&
							AccessibilityPeerHelper.TrySetWindowVisualState(p, WindowVisualState.Normal);
					}));
			}
		}

		// MultipleView: one action per supported view when there is more than one choice.
		if (peer.GetPattern(PatternInterface.MultipleView) is IMultipleViewProvider mvp)
		{
			var supportedViews = mvp.GetSupportedViews() ?? Array.Empty<int>();
			var currentView = mvp.CurrentView;

			if (supportedViews.Length > 1)
			{
				foreach (var viewId in supportedViews)
				{
					if (viewId == currentView)
					{
						continue;
					}

					var capturedId = viewId;
					var viewLabel = mvp.GetViewName(viewId);

					if (string.IsNullOrEmpty(viewLabel))
					{
						viewLabel = $"View {viewId}";
					}

					list.Add(CreateCustomAction(
						Localize(viewLabel),
						_ =>
						{
							if (!weakSelf.TryGetTarget(out var self)) return false;
							var p = self.ResolvePeer(handle);
							return p is not null && AccessibilityPeerHelper.TryChangeView(p, capturedId);
						}));
				}
			}
		}

		// Transform2 zoom: Zoom In / Zoom Out when CanZoom. Requires no caller-supplied
		// coordinate, so these are suitable VoiceOver custom actions.
		if (peer.GetPattern(PatternInterface.Transform2) is ITransformProvider2 t2 && t2.CanZoom)
		{
			list.Add(CreateCustomAction(
				Localize("Zoom In"),
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					var p = self.ResolvePeer(handle);
					return p is not null && AccessibilityPeerHelper.TryZoomByUnit(p, ZoomUnit.SmallIncrement);
				}));
			list.Add(CreateCustomAction(
				Localize("Zoom Out"),
				_ =>
				{
					if (!weakSelf.TryGetTarget(out var self)) return false;
					var p = self.ResolvePeer(handle);
					return p is not null && AccessibilityPeerHelper.TryZoomByUnit(p, ZoomUnit.SmallDecrement);
				}));
		}

		// Dock: one action per non-current position. Uses fixed localized labels because
		// dock positions are a closed set.
		if (peer.GetPattern(PatternInterface.Dock) is IDockProvider dockProvider)
		{
			var currentPos = dockProvider.DockPosition;

			foreach (var pos in new[]
				{ DockPosition.Top, DockPosition.Left, DockPosition.Bottom,
				  DockPosition.Right, DockPosition.Fill, DockPosition.None })
			{
				if (pos == currentPos) continue;
				var capturedPos = pos;
				var posLabel = pos switch
				{
					DockPosition.Top    => "Dock to Top",
					DockPosition.Left   => "Dock to Left",
					DockPosition.Bottom => "Dock to Bottom",
					DockPosition.Right  => "Dock to Right",
					DockPosition.Fill   => "Dock to Fill",
					DockPosition.None   => "Undock",
					_                   => $"Dock {pos}",
				};
				list.Add(CreateCustomAction(
					Localize(posLabel),
					_ =>
					{
						if (!weakSelf.TryGetTarget(out var self)) return false;
						var p = self.ResolvePeer(handle);
						return p is not null && AccessibilityPeerHelper.TrySetDockPosition(p, capturedPos);
					}));
			}
		}

		// Move, Resize, Rotate require caller-supplied coordinates; they cannot be
		// expressed as parameterless VoiceOver custom actions. They are supported
		// only through the test/native action hook (ExecuteAction). No VoiceOver
		// custom actions are added here for those patterns.

		return list.Count > 0 ? list.ToArray() : null;
	}

	// Action test hook calls through the real UIAccessibilityElement override/
	// custom-action path so test assertions exercise the live UIKit dispatch chain.
	private bool ExecuteAction(UIElement element, AccessibilityNativeActionRequest request)
	{
		Trace($"Native action requested: {request.Action}.");
		if (!NativeDispatcher.Main.HasThreadAccess)
		{
			if (IsDisposed)
			{
				return false;
			}

			// UIKit calls and dictionary access must only occur on the main thread.
			NativeDispatcher.Main.Enqueue(() =>
			{
				if (!IsDisposed)
				{
					ExecuteAction(element, request);
				}
			});
			Trace($"Native action {request.Action} queued for the main thread.");
			return true;
		}

		if (IsBlockedByActiveModal(element))
		{
			Trace($"Rejected native action {request.Action} outside the active modal.");
			return false;
		}

		if (!_nodeElements.TryGetValue(element.Visual.Handle, out var el))
		{
			Trace($"Rejected native action {request.Action} for stale handle {element.Visual.Handle}.");
			return false;
		}

		switch (request.Action)
		{
			case AccessibilityNativeAction.Activate:
				return el.AccessibilityActivate();

			case AccessibilityNativeAction.Increment:
			{
				// Translate disabled/read-only to native failure before calling the UIKit path.
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled())
				{
					return false;
				}

				if (peer.GetPattern(PatternInterface.RangeValue) is not IRangeValueProvider rangeInc ||
					rangeInc.IsReadOnly)
				{
					return false;
				}

				el.AccessibilityIncrement();
				return true;
			}

			case AccessibilityNativeAction.Decrement:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled())
				{
					return false;
				}

				if (peer.GetPattern(PatternInterface.RangeValue) is not IRangeValueProvider rangeDec ||
					rangeDec.IsReadOnly)
				{
					return false;
				}

				el.AccessibilityDecrement();
				return true;
			}

			case AccessibilityNativeAction.SetValue:
			{
				var peer = ResolvePeer(el.NodeId);
				return peer is not null &&
					peer.IsEnabled() &&
					request.Text is not null &&
					AccessibilityPeerHelper.TrySetValue(peer, request.Text);
			}

			case AccessibilityNativeAction.ScrollForward:
				return el.AccessibilityScroll(UIAccessibilityScrollDirection.Down);

			case AccessibilityNativeAction.ScrollBackward:
				return el.AccessibilityScroll(UIAccessibilityScrollDirection.Up);

			case AccessibilityNativeAction.Dismiss:
				return el.AccessibilityPerformEscape();

			case AccessibilityNativeAction.ChangeView:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				if (!double.IsFinite(request.Number) || request.Number != Math.Truncate(request.Number)) return false;
				return AccessibilityPeerHelper.TryChangeView(peer, (int)request.Number);
			}

			case AccessibilityNativeAction.ZoomIn:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				return AccessibilityPeerHelper.TryZoomByUnit(peer, ZoomUnit.SmallIncrement);
			}

			case AccessibilityNativeAction.ZoomOut:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				return AccessibilityPeerHelper.TryZoomByUnit(peer, ZoomUnit.SmallDecrement);
			}

			case AccessibilityNativeAction.Zoom:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				return AccessibilityPeerHelper.TryZoom(peer, request.Number);
			}

			case AccessibilityNativeAction.SetDockPosition:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				if (!double.IsFinite(request.Number) || request.Number != Math.Truncate(request.Number)) return false;
				var posInt = (int)request.Number;
				if (posInt < (int)DockPosition.Top || posInt > (int)DockPosition.None) return false;
				return AccessibilityPeerHelper.TrySetDockPosition(peer, (DockPosition)posInt);
			}

			case AccessibilityNativeAction.SetWindowVisualState:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				if (!double.IsFinite(request.Number) || request.Number != Math.Truncate(request.Number)) return false;
				var stateInt = (int)request.Number;
				if (stateInt < (int)WindowVisualState.Normal || stateInt > (int)WindowVisualState.Minimized) return false;
				return AccessibilityPeerHelper.TrySetWindowVisualState(peer, (WindowVisualState)stateInt);
			}

			case AccessibilityNativeAction.Move:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				return AccessibilityPeerHelper.TryMove(peer, request.Number, request.Number2);
			}

			case AccessibilityNativeAction.Resize:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				return AccessibilityPeerHelper.TryResize(peer, request.Number, request.Number2);
			}

			case AccessibilityNativeAction.Rotate:
			{
				var peer = ResolvePeer(el.NodeId);
				if (peer is null || !peer.IsEnabled()) return false;
				return AccessibilityPeerHelper.TryRotate(peer, request.Number);
			}

			default:
				return InvokeMatchingCustomAction(el, request);
		}
	}

	private static bool InvokeMatchingCustomAction(
		UnoUIAccessibilityElement el,
		AccessibilityNativeActionRequest request)
	{
		var actions = el.AccessibilityCustomActions;
		if (actions is null)
		{
			return false;
		}

		var actionKey = request.Action switch
		{
			AccessibilityNativeAction.Expand => "Expand",
			AccessibilityNativeAction.Collapse => "Collapse",
			AccessibilityNativeAction.ScrollForward => "Scroll Forward",
			AccessibilityNativeAction.ScrollBackward => "Scroll Backward",
			AccessibilityNativeAction.ScrollIntoView => "Scroll Into View",
			AccessibilityNativeAction.Realize => "Realize",
			_ => null,
		};

		if (actionKey is null)
		{
			return false;
		}

		var actionName = Localize(actionKey);
		foreach (var action in actions)
		{
			if (action.Name == actionName)
			{
				return action.ActionHandler?.Invoke(action) ?? false;
			}
		}

		return false;
	}

	// Abstract property updates invalidate the native node so VoiceOver re-queries.

	protected override void UpdateName(nint handle, AutomationPeer peer, string? label)
		=> InvalidateElement(handle);

	protected override void UpdateToggleState(nint handle, AutomationPeer peer, ToggleState newState)
		=> InvalidateElement(handle);

	protected override void UpdateRangeValue(nint handle, AutomationPeer peer, double value)
		=> InvalidateElement(handle);

	protected override void UpdateRangeBounds(nint handle, double min, double max)
		=> InvalidateElement(handle);

	protected override void UpdateTextValue(nint handle, string? value)
		=> InvalidateElement(handle);

	protected override void UpdateExpandCollapseState(nint handle, bool isExpanded)
		=> InvalidateElement(handle);

	protected override void UpdateEnabled(nint handle, bool enabled)
		=> InvalidateElement(handle);

	protected override void UpdateSelected(nint handle, bool selected)
		=> InvalidateElement(handle);

	protected override void UpdateHelpText(nint handle, string? helpText)
		=> InvalidateElement(handle);

	protected override void UpdateHeadingLevel(nint handle, int level)
		=> InvalidateElement(handle);

	protected override void UpdateLandmark(nint handle, string? landmarkRole)
		=> InvalidateElement(handle);

	protected override void UpdateIsReadOnly(nint handle, bool isReadOnly)
		=> InvalidateElement(handle);

	protected override void UpdateFocusable(nint handle, bool focusable)
		=> InvalidateElement(handle);

	protected override void UpdateIsOffscreen(nint handle, bool isOffscreen)
		=> InvalidateElement(handle);

	protected override void SetNativeFocus(nint handle)
	{
		Trace($"Native focus requested for handle {handle}.");
		// Re-entry guard: if this call originated from us setting XAML focus in
		// OnNativeElementFocused; updating the tracking handle is enough, so do not
		// re-post the ScreenChanged notification and restart the loop.
		if (_settingXamlFocus)
		{
			_lastNativeFocusedHandle = handle;
			return;
		}

		if (_nodeElements.TryGetValue(handle, out var el))
		{
			_pendingNativeFocusHandle = handle;
			_lastNativeFocusedHandle = handle;
			var captured = el;
			PostOnMain(() =>
				UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, captured));
		}
	}

	protected override void OnNativeStructureChanged()
	{
		_modalScopeDirty = true;
		PostOnMain(() =>
		{
			if (!IsDisposed)
			{
				_forceStructureNotification = true;
				ScheduleRebuild();
			}
		});
	}

	private void RequestScreenChange(nint targetHandle)
		=> PostOnMain(() =>
		{
			if (!IsDisposed)
			{
				_screenChangeRequested = true;
				_screenChangeTargetHandle = targetHandle;
				ScheduleRebuild();
			}
		});

	// Native focus callbacks

	/// <summary>
	/// Called by <see cref="UnoUIAccessibilityElement.AccessibilityElementDidBecomeFocused"/>
	/// when VoiceOver moves focus to a native element. Updates tracking state and
	/// optionally sets XAML focus when the receiving peer is keyboard-focusable.
	/// The re-entry guard prevents a XAML -> native -> XAML loop.
	/// </summary>
	internal void OnNativeElementFocused(nint nodeId)
	{
		Trace($"Native focus received for handle {nodeId}.");
		_lastNativeFocusedHandle = nodeId;

		// If the focus confirmation matches the handle we programmatically set via
		// SetNativeFocus, this is the expected round-trip from the XAML side; do not
		// feed it back to XAML and loop.
		if (_pendingNativeFocusHandle == nodeId && nodeId != 0)
		{
			_pendingNativeFocusHandle = 0;
			return;
		}

		_pendingNativeFocusHandle = 0;

		// Native -> XAML direction: set XAML keyboard focus if the peer supports it.
		// Only Control subclasses accept programmatic focus in Uno.
		if (!_handleToOwner.TryGetValue(nodeId, out var weakRef) ||
			!weakRef.TryGetTarget(out var element) ||
			element is not Control control)
		{
			return;
		}

		var peer = element.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			return;
		}

		var providerPeer = AccessibilityPeerHelper.ResolveProviderPeer(peer);
		if (!providerPeer.IsKeyboardFocusable())
		{
			return;
		}

		// Guard the XAML focus call so SetNativeFocus does not re-post ScreenChanged.
		_settingXamlFocus = true;
		try
		{
			control.Focus(FocusState.Keyboard);
		}
		finally
		{
			_settingXamlFocus = false;
		}
	}

	/// <summary>
	/// Called by <see cref="UnoUIAccessibilityElement.AccessibilityElementDidLoseFocus"/>
	/// when VoiceOver moves focus away from a native element. No immediate action is taken;
	/// XAML focus management handles recovery when elements are removed or disabled.
	/// </summary>
	internal void OnNativeElementLostFocus(nint nodeId)
	{
		// No action needed here. Removal/disable recovery is handled by the base class
		// TrackFocusedElement/RecoverFocus mechanism in SkiaAccessibilityBase.
	}

	protected override void AnnounceOnPlatform(string text, bool assertive)
	{
		// Invoked from debounce timers on thread-pool threads; PostNotification requires main thread.
		var capturedText = text;
		PostOnMain(() =>
		{
			if (IsDisposed) return;
			using var content = new NSString(capturedText);
			UIAccessibility.PostNotification(UIAccessibilityPostNotification.Announcement, content);
			RecordEvent(AccessibilityNativeEventKind.Announcement, text: capturedText);
		});
	}

	protected override void DisposeCore()
	{
		Trace("Disposing AppleUIKit accessibility adapter.");
		// Remove this root from the per-root registry. Static dispatchers remain
		// installed for any other live roots; do not null them out.
		UnregisterAdapter(_xamlRoot, this);

		_pendingInvalidationHandles.Clear();
		_invalidationFlushScheduled = false;
		_forceStructureNotification = false;
		_screenChangeRequested = false;
		_screenChangeTargetHandle = 0;
		_lastOrderedHandles.Clear();
		_nextOrderedHandles.Clear();
		_modalFocusStack.Clear();
		_allScrollSources.Clear();
		_recordedEvents.Clear();
		_recordEvents = false;
		_nodeElements.Clear();
		_handleToOwner.Clear();

		if (_controllerRef.TryGetTarget(out var controller) &&
			controller.SkCanvasView is { } metalView)
		{
			metalView.InvokeOnMainThread(() => SetAccessibilityElements(metalView, null));
		}
	}

	// Helpers

	/// <summary>
	/// Posts a UIKit notification invalidation. Dispatches to the main thread when the
	/// caller is on a background thread (render thread, timer callback, etc.).
	/// </summary>
	private static void PostOnMain(Action action)
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			action();
		}
		else
		{
			NativeDispatcher.Main.Enqueue(action);
		}
	}

	/// <summary>
	/// Walks the full visual tree, subscribing every ScrollViewer/ScrollPresenter found,
	/// then unsubscribes any sources that were present in the previous tree but are gone now.
	/// This ensures Raw/peerless scroll containers still invalidate accessible descendant bounds.
	/// </summary>
	private void RefreshScrollSources(UIElement root)
	{
		var newSources = new Dictionary<nint, WeakReference<UIElement>>();
		CollectScrollSources(root, newSources);

		foreach (var (handle, weakEl) in _allScrollSources)
		{
			if (!newSources.ContainsKey(handle) && weakEl.TryGetTarget(out var stale))
			{
				TryUnsubscribeScrollSource(stale);
			}
		}

		_allScrollSources.Clear();
		foreach (var (handle, weakRef) in newSources)
		{
			_allScrollSources[handle] = weakRef;
		}
	}

	private void CollectScrollSources(UIElement element, Dictionary<nint, WeakReference<UIElement>> found)
	{
		if (element is ScrollViewer or ScrollPresenter)
		{
			var handle = element.Visual.Handle;
			if (found.TryAdd(handle, new WeakReference<UIElement>(element)))
			{
				TrySubscribeScrollSource(element);
			}
		}

		foreach (var child in element.GetChildren())
		{
			CollectScrollSources(child, found);
		}
	}

	private void InvalidateElement(nint handle)
	{
		if (NativeDispatcher.Main.HasThreadAccess)
		{
			// All dictionary/set access on the main thread only.
			if (!_nodeElements.TryGetValue(handle, out var element)) return;
			element.InvalidateCachedAccessibilityData();
			_pendingInvalidationHandles.Add(handle);
			Trace($"Queued targeted invalidation for handle {handle}.");
			if (!_invalidationFlushScheduled)
			{
				_invalidationFlushScheduled = true;
				NativeDispatcher.Main.Enqueue(FlushPendingInvalidations);
			}
		}
		else
		{
			// Marshal unconditionally; no shared state read off the main thread.
			NativeDispatcher.Main.Enqueue(() => InvalidateElement(handle));
		}
	}

	private void FlushPendingInvalidations()
	{
		_invalidationFlushScheduled = false;
		if (IsDisposed || _pendingInvalidationHandles.Count == 0)
		{
			_pendingInvalidationHandles.Clear();
			return;
		}

		_pendingInvalidationHandles.Clear();
		Trace("Flushing coalesced targeted invalidations.");

		// One LayoutChanged(null) per dispatch-cycle burst. Passing a non-null element
		// argument would move VoiceOver focus to it, which is wrong for property/bounds
		// invalidations. One NodeInvalidated record covers the entire flush.
		UIAccessibility.PostNotification(UIAccessibilityPostNotification.LayoutChanged, null);
		RecordEvent(AccessibilityNativeEventKind.NodeInvalidated);
	}

	private AccessibilityNativeEventRecord[]? GetEventsForRoot(XamlRoot xamlRoot)
	{
		if (!ReferenceEquals(xamlRoot, _xamlRoot))
		{
			return null;
		}

		_recordEvents = true;
		return _recordedEvents.ToArray();
	}

	private void ClearEventsForRoot(XamlRoot xamlRoot)
	{
		if (ReferenceEquals(xamlRoot, _xamlRoot))
		{
			_recordEvents = true;
			_recordedEvents.Clear();
		}
	}

	/// <summary>
	/// Ensures every property change invalidates the native element, including properties
	/// not in the base mapping. The coalesced flush records the native invalidation.
	/// </summary>
	protected override void NotifyPropertyChangedEventCore(
		AutomationPeer peer,
		AutomationProperty automationProperty,
		object oldValue,
		object newValue)
	{
		Trace($"Property change routed for {automationProperty}.");
		base.NotifyPropertyChangedEventCore(peer, automationProperty, oldValue, newValue);

		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		if (!TryGetPeerOwner(resolvedPeer, out var owner))
		{
			TryGetPeerOwner(peer, out owner);
		}

		if (owner is null || !_nodeElements.ContainsKey(owner.Visual.Handle))
		{
			return;
		}

		// Ensures invalidation for properties not in the base mapping.
		// Duplicate adds to the pending set are no-ops.
		InvalidateElement(owner.Visual.Handle);

		if (automationProperty == AutomationElementIdentifiers.IsDialogProperty)
		{
			PostOnMain(() =>
			{
				if (!IsDisposed)
				{
					_forceStructureNotification = true;
					ScheduleRebuild();
				}
			});
		}
	}

	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (IsDisposed || !IsAccessibilityEnabled) return;
		Trace($"Automation event routed: {eventId}.");
		base.NotifyAutomationEvent(peer, eventId);

		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource: true);

		switch (eventId)
		{
			case AutomationEvents.TextPatternOnTextChanged:
			case AutomationEvents.TextEditTextChanged:
			case AutomationEvents.ConversionTargetChanged:
				// Base updated the text value; record that a text change occurred.
				RecordEvent(AccessibilityNativeEventKind.TextChanged);
				break;

			case AutomationEvents.SelectionItemPatternOnElementSelected:
			case AutomationEvents.SelectionItemPatternOnElementAddedToSelection:
			case AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection:
				RecordEvent(AccessibilityNativeEventKind.SelectionChanged);
				break;

			case AutomationEvents.WindowOpened:
			case AutomationEvents.MenuOpened:
				RecordEvent(AccessibilityNativeEventKind.WindowChanged);
				RequestScreenChange(
					TryGetPeerOwner(resolvedPeer, peer, out var openedOwner)
						? openedOwner.Visual.Handle
						: 0);
				break;

			case AutomationEvents.WindowClosed:
			case AutomationEvents.MenuClosed:
				RecordEvent(AccessibilityNativeEventKind.WindowChanged);
				RequestScreenChange(0);
				break;

			case AutomationEvents.TextPatternOnTextSelectionChanged:
				RecordEvent(AccessibilityNativeEventKind.SelectionChanged);
				if (TryGetPeerOwner(resolvedPeer, peer, out var textOwner))
				{
					InvalidateElement(textOwner.Visual.Handle);
				}
				break;

			case AutomationEvents.SelectionPatternOnInvalidated:
				RecordEvent(AccessibilityNativeEventKind.SelectionChanged);
				break;

			case AutomationEvents.ToolTipOpened:
			case AutomationEvents.ToolTipClosed:
				// Base has no handler for tooltip events; invalidate the element directly.
				RecordEvent(AccessibilityNativeEventKind.WindowChanged);
				if (TryGetPeerOwner(resolvedPeer, peer, out var tipOwner))
				{
					InvalidateElement(tipOwner.Visual.Handle);
				}

				break;

			// StructureChanged, LayoutInvalidated, AsyncContentLoaded, SelectionPatternOnInvalidated:
			// base schedules a rebuild; StructureChanged is recorded in RebuildTree when the
			// rebuild detects an actual change in element order or membership.
			// LiveRegionChanged: base announces with correct assertiveness via AnnounceOnPlatform.
		}
	}

	public override void NotifyTextEditTextChangedEvent(
		AutomationPeer peer,
		AutomationTextEditChangeType changeType,
		System.Collections.Generic.IReadOnlyList<string> changedData)
	{
		if (IsDisposed || !IsAccessibilityEnabled) return;
		base.NotifyTextEditTextChangedEvent(peer, changeType, changedData);

		// Base called UpdateTextValue; record the text-edit change and ensure invalidation.
		var resolvedPeer = peer.ResolveProviderPeer(resolveEventsSource: true);
		if (TryGetPeerOwner(resolvedPeer, peer, out var owner))
		{
			InvalidateElement(owner.Visual.Handle);
		}

		RecordEvent(AccessibilityNativeEventKind.TextChanged);
	}

	private AutomationPeer? ResolvePeer(nint handle)
	{
		if (!_handleToOwner.TryGetValue(handle, out var weakRef) ||
			!weakRef.TryGetTarget(out var element))
		{
			return null;
		}

		if (IsBlockedByActiveModal(element))
		{
			return null;
		}

		var peer = element.GetOrCreateAutomationPeer();
		if (peer is null)
		{
			return null;
		}

		return AccessibilityPeerHelper.ResolveProviderPeer(peer);
	}

	private AccessibilityNativeNodeSnapshot CreateSnapshot(UnoUIAccessibilityElement element)
	{
		var traits = (UIAccessibilityTrait)element.AccessibilityTraits;
		var nativeTraits = AccessibilityNativeTraits.None;

		if ((traits & UIAccessibilityTrait.Button) != 0)
		{
			nativeTraits |= AccessibilityNativeTraits.Button;
		}
		if ((traits & UIAccessibilityTrait.StaticText) != 0)
		{
			nativeTraits |= AccessibilityNativeTraits.StaticText;
		}
		if ((traits & UIAccessibilityTrait.NotEnabled) != 0)
		{
			nativeTraits |= AccessibilityNativeTraits.NotEnabled;
		}
		if ((traits & UIAccessibilityTrait.Adjustable) != 0)
		{
			nativeTraits |= AccessibilityNativeTraits.Adjustable;
		}
		if ((traits & UIAccessibilityTrait.Link) != 0)
		{
			nativeTraits |= AccessibilityNativeTraits.Link;
		}
		if ((traits & UIAccessibilityTrait.Image) != 0)
		{
			nativeTraits |= AccessibilityNativeTraits.Image;
		}
		if ((traits & UIAccessibilityTrait.Header) != 0)
		{
			nativeTraits |= AccessibilityNativeTraits.Header;
		}

		var peer = ResolvePeer(element.NodeId);
		var value = element.AccessibilityValue;
		var frame = element.AccessibilityFrameInContainerSpace;
		var toggleProvider = peer?.GetPattern(PatternInterface.Toggle) as IToggleProvider;
		var checkable = toggleProvider is not null;
		bool? isChecked = toggleProvider?.ToggleState switch
		{
			ToggleState.On => true,
			ToggleState.Off => false,
			_ => null,
		};

		UIElement? owner = null;
		if (_handleToOwner.TryGetValue(element.NodeId, out var ownerReference))
		{
			ownerReference.TryGetTarget(out owner);
		}

		AccessibilityNativeNodeDetails? details = null;
		if (peer is not null)
		{
			details = BuildDetails(peer, owner);
		}

		return new AccessibilityNativeNodeSnapshot(
			element,
			element.AccessibilityLabel,
			peer?.GetAutomationControlType().ToString(),
			element.AccessibilityHint,
			value,
			element.AccessibilityIdentifier,
			(nativeTraits & AccessibilityNativeTraits.NotEnabled) == 0,
			(nativeTraits & AccessibilityNativeTraits.Header) != 0,
			peer?.IsPassword() is true,
			checkable,
			isChecked,
			nativeTraits,
			new Windows.Foundation.Rect(frame.X, frame.Y, frame.Width, frame.Height),
			details);
	}

	private AccessibilityNativeNodeDetails? BuildDetails(AutomationPeer peer, UIElement? owner)
	{
		var actions = BuildSupportedActions(peer);
		var range = BuildRange(peer);
		var textState = BuildTextState(peer, owner);
		var scroll = BuildScroll(peer);
		var collection = BuildCollection(peer);
		var collectionItem = BuildCollectionItem(peer);
		var hierarchy = BuildHierarchy(peer, owner);
		var relations = owner is not null ? BuildRelations(owner) : null;
		var fallbacks = AccessibilityPeerHelper.GetFallbackDetails(peer);

		string? itemStatus = peer.GetItemStatus() is { Length: > 0 } s ? s : null;
		string? itemType = peer.GetItemType() is { Length: > 0 } t ? t : null;
		string? localizedControlType = peer.GetLocalizedControlType() is { Length: > 0 } lct ? lct : null;
		string? fullDescription = peer.GetFullDescription() is { Length: > 0 } fd ? fd : null;
		bool? isRequired = null;
		bool? isDataValid = null;
		int? culture = null;
		AutomationLandmarkType? landmarkType = null;
		string? localizedLandmarkType = null;

		if (owner is not null)
		{
			// IsRequiredForForm: only include when explicitly true (default is false).
			if (AutomationProperties.GetIsRequiredForForm(owner))
			{
				isRequired = true;
			}

			// IsDataValidForForm: only include when explicitly false (default is true).
			if (!AutomationProperties.GetIsDataValidForForm(owner))
			{
				isDataValid = false;
			}

			var lcid = AutomationProperties.GetCulture(owner);
			if (lcid != 0)
			{
				culture = lcid;
			}
		}

		var lt = peer.GetLandmarkType();
		if (lt != AutomationLandmarkType.None)
		{
			landmarkType = lt;
			var ll = peer.GetLocalizedLandmarkType();
			if (!string.IsNullOrEmpty(ll))
			{
				localizedLandmarkType = ll;
			}
		}

		bool hasContent = actions.Count > 0 || range is not null || textState is not null
			|| scroll is not null || collection is not null || collectionItem is not null
			|| hierarchy is not null || relations is not null || fallbacks is not null
			|| itemStatus is not null || itemType is not null || localizedControlType is not null
			|| fullDescription is not null || isRequired.HasValue || isDataValid.HasValue
			|| culture.HasValue || landmarkType.HasValue;

		if (!hasContent)
		{
			return null;
		}

		return new AccessibilityNativeNodeDetails(
			supportedActions: actions.Count > 0 ? actions : null,
			range: range,
			textState: textState,
			scroll: scroll,
			collection: collection,
			collectionItem: collectionItem,
			hierarchy: hierarchy,
			relations: relations,
			itemStatus: itemStatus,
			itemType: itemType,
			localizedControlType: localizedControlType,
			fullDescription: fullDescription,
			isRequiredForForm: isRequired,
			isDataValidForForm: isDataValid,
			culture: culture,
			landmarkType: landmarkType,
			localizedLandmarkType: localizedLandmarkType,
			fallbacks: fallbacks);
	}

	private static AccessibilityNativeRangeDetails? BuildRange(AutomationPeer peer)
	{
		if (peer.GetPattern(PatternInterface.RangeValue) is not IRangeValueProvider rv)
		{
			return null;
		}

		return new AccessibilityNativeRangeDetails(
			value: rv.Value,
			minimum: rv.Minimum,
			maximum: rv.Maximum,
			smallChange: rv.SmallChange,
			largeChange: rv.LargeChange,
			isReadOnly: rv.IsReadOnly || !peer.IsEnabled(),
			orientation: peer.GetOrientation());
	}

	private static AccessibilityNativeScrollDetails? BuildScroll(AutomationPeer peer)
	{
		if (peer.GetPattern(PatternInterface.Scroll) is not IScrollProvider sp)
		{
			return null;
		}

		return new AccessibilityNativeScrollDetails(
			isHorizontallyScrollable: sp.HorizontallyScrollable,
			isVerticallyScrollable: sp.VerticallyScrollable,
			horizontalScrollPercent: sp.HorizontalScrollPercent,
			verticalScrollPercent: sp.VerticalScrollPercent,
			horizontalViewSize: sp.HorizontalViewSize,
			verticalViewSize: sp.VerticalViewSize);
	}

	private static AccessibilityNativeTextStateDetails? BuildTextState(AutomationPeer peer, UIElement? owner)
	{
		var controlType = peer.GetAutomationControlType();
		if (controlType != AutomationControlType.Edit &&
			controlType != AutomationControlType.Document)
		{
			return null;
		}

		var valueProvider = peer.GetPattern(PatternInterface.Value) as IValueProvider;
		bool isReadOnly = !peer.IsEnabled() || valueProvider?.IsReadOnly != false;

		// Multiline from owner type; TextBox peers don't override GetOrientationCore.
		bool isMultiline = owner is Microsoft.UI.Xaml.Controls.TextBox tb && tb.AcceptsReturn
			|| owner is Microsoft.UI.Xaml.Controls.RichEditBox;

		bool hasTextSelection = peer.GetPattern(PatternInterface.Text) is ITextProvider;

		return new AccessibilityNativeTextStateDetails(
			isEditable: !isReadOnly,
			isReadOnly: isReadOnly,
			isMultiline: isMultiline,
			hasTextSelection: hasTextSelection);
	}

	private static AccessibilityNativeCollectionDetails? BuildCollection(AutomationPeer peer)
	{
		// IGridProvider carries explicit row/column counts.
		if (peer.GetPattern(PatternInterface.Grid) is IGridProvider grid)
		{
			var sel = peer.GetPattern(PatternInterface.Selection) as ISelectionProvider;
			return new AccessibilityNativeCollectionDetails(
				rowCount: grid.RowCount,
				columnCount: grid.ColumnCount,
				canSelectMultiple: sel?.CanSelectMultiple ?? false,
				isSelectionRequired: sel?.IsSelectionRequired ?? false);
		}

		// ISelectionProvider without grid: derive item count from peer children.
		if (peer.GetPattern(PatternInterface.Selection) is ISelectionProvider selOnly)
		{
			var children = peer.GetChildren();
			int rowCount = children?.Count ?? 0;
			return new AccessibilityNativeCollectionDetails(
				rowCount: rowCount,
				columnCount: 1,
				canSelectMultiple: selOnly.CanSelectMultiple,
				isSelectionRequired: selOnly.IsSelectionRequired);
		}

		return null;
	}

	private static AccessibilityNativeCollectionItemDetails? BuildCollectionItem(AutomationPeer peer)
	{
		if (peer.GetPattern(PatternInterface.GridItem) is not IGridItemProvider gip)
		{
			return null;
		}

		return new AccessibilityNativeCollectionItemDetails(
			row: gip.Row,
			column: gip.Column,
			rowSpan: gip.RowSpan,
			columnSpan: gip.ColumnSpan);
	}

	private static AccessibilityNativeHierarchyDetails? BuildHierarchy(AutomationPeer peer, UIElement? owner)
	{
		if (owner is null)
		{
			return null;
		}

		int level = peer.GetLevel();
		int position = peer.GetPositionInSet();
		int size = peer.GetSizeOfSet();

		if (level < 0 && position < 0 && size < 0)
		{
			return null;
		}

		return new AccessibilityNativeHierarchyDetails(
			positionInSet: position > 0 ? position : 0,
			sizeOfSet: size > 0 ? size : 0,
			level: level > 0 ? level : 0);
	}

	private AccessibilityNativeRelationDetails? BuildRelations(UIElement owner)
	{
		var labeledByIds = BuildIdList(AsEnumerable(AutomationProperties.GetLabeledBy(owner)));
		var describedByIds = BuildIdList(
			(owner.GetValue(AutomationProperties.DescribedByProperty) as IEnumerable<DependencyObject>)
				?.OfType<UIElement>()
			?? Array.Empty<UIElement>());
		var controlledPeerIds = BuildIdList(
			owner.GetValue(AutomationProperties.ControlledPeersProperty) as IEnumerable<UIElement>
			?? Array.Empty<UIElement>());
		var flowsToIds = BuildIdList(
			(owner.GetValue(AutomationProperties.FlowsToProperty) as IEnumerable<DependencyObject>)
				?.OfType<UIElement>()
			?? Array.Empty<UIElement>());
		var flowsFromIds = BuildIdList(
			(owner.GetValue(AutomationProperties.FlowsFromProperty) as IEnumerable<DependencyObject>)
				?.OfType<UIElement>()
			?? Array.Empty<UIElement>());

		List<string>? annotationTypeNames = null;
		var annotations =
			owner.GetValue(AutomationProperties.AnnotationsProperty) as IEnumerable<AutomationAnnotation>;
		if (annotations is not null)
		{
			annotationTypeNames = new List<string>();
			foreach (var ann in annotations)
			{
				annotationTypeNames.Add(ann.Type.ToString());
			}

			if (annotationTypeNames.Count == 0)
			{
				annotationTypeNames = null;
			}
		}

		if (labeledByIds is null && describedByIds is null && controlledPeerIds is null
			&& flowsToIds is null && flowsFromIds is null && annotationTypeNames is null)
		{
			return null;
		}

		return new AccessibilityNativeRelationDetails(
			labeledByIds: labeledByIds,
			describedByIds: describedByIds,
			controlledPeerIds: controlledPeerIds,
			flowsFromIds: flowsFromIds,
			flowsToIds: flowsToIds,
			annotationTypeNames: annotationTypeNames);
	}

	private List<string>? BuildIdList(IEnumerable<UIElement> elements)
	{
		List<string>? ids = null;
		foreach (var el in elements)
		{
			if (!ReferenceEquals(el.XamlRoot, _xamlRoot) ||
				!_nodeElements.ContainsKey(el.Visual.Handle))
			{
				continue;
			}

			var id = AutomationProperties.GetAutomationId(el);
			if (!string.IsNullOrEmpty(id))
			{
				(ids ??= new List<string>()).Add(id);
			}
		}

		return ids;
	}

	// Wraps a single nullable element as a zero-or-one-element sequence.
	private static IEnumerable<UIElement> AsEnumerable(UIElement? element)
		=> element is not null ? new[] { element } : Array.Empty<UIElement>();

	private static List<AccessibilityNativeAction> BuildSupportedActions(AutomationPeer peer)
	{
		var actions = new List<AccessibilityNativeAction>();

		if (peer.IsEnabled() &&
			(peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider
				|| peer.GetPattern(PatternInterface.Toggle) is IToggleProvider
				|| peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider))
		{
			actions.Add(AccessibilityNativeAction.Activate);
		}

		if (peer.IsEnabled() &&
			peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rv &&
			!rv.IsReadOnly)
		{
			actions.Add(AccessibilityNativeAction.Increment);
			actions.Add(AccessibilityNativeAction.Decrement);
		}

		if (peer.IsEnabled() &&
			peer.GetPattern(PatternInterface.Value) is IValueProvider { IsReadOnly: false })
		{
			actions.Add(AccessibilityNativeAction.SetValue);
		}

		if (peer.GetPattern(PatternInterface.Scroll) is IScrollProvider sp
			&& (sp.HorizontallyScrollable || sp.VerticallyScrollable))
		{
			actions.Add(AccessibilityNativeAction.ScrollForward);
			actions.Add(AccessibilityNativeAction.ScrollBackward);
		}

		if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider ec)
		{
			if (ec.ExpandCollapseState == ExpandCollapseState.Collapsed)
			{
				actions.Add(AccessibilityNativeAction.Expand);
			}
			else if (ec.ExpandCollapseState == ExpandCollapseState.Expanded)
			{
				actions.Add(AccessibilityNativeAction.Collapse);
			}
		}

		if (peer.GetPattern(PatternInterface.ScrollItem) is IScrollItemProvider)
		{
			actions.Add(AccessibilityNativeAction.ScrollIntoView);
		}

		if (peer.GetPattern(PatternInterface.VirtualizedItem) is IVirtualizedItemProvider)
		{
			actions.Add(AccessibilityNativeAction.Realize);
		}

		if (peer.GetPattern(PatternInterface.Window) is IWindowProvider)
		{
			actions.Add(AccessibilityNativeAction.Dismiss);
		}

		// MultipleView: ChangeView is supported when there is more than one view to switch to.
		if (peer.IsEnabled() &&
			peer.GetPattern(PatternInterface.MultipleView) is IMultipleViewProvider mvpActions)
		{
			var views = mvpActions.GetSupportedViews() ?? Array.Empty<int>();
			if (views.Length > 1)
			{
				actions.Add(AccessibilityNativeAction.ChangeView);
			}
		}

		// Transform2: zoom actions when CanZoom; absolute Zoom via hook.
		if (peer.IsEnabled() &&
			peer.GetPattern(PatternInterface.Transform2) is ITransformProvider2 t2Actions &&
			t2Actions.CanZoom)
		{
			actions.Add(AccessibilityNativeAction.ZoomIn);
			actions.Add(AccessibilityNativeAction.ZoomOut);
		}

		// Dock: advertised when the provider is present and the element is enabled.
		if (peer.IsEnabled() && peer.GetPattern(PatternInterface.Dock) is IDockProvider)
		{
			actions.Add(AccessibilityNativeAction.SetDockPosition);
		}

		// Window visual state: advertised for elements that carry IWindowProvider.
		if (peer.IsEnabled() && peer.GetPattern(PatternInterface.Window) is IWindowProvider)
		{
			actions.Add(AccessibilityNativeAction.SetWindowVisualState);
		}

		return actions;
	}

	internal string? GetLanguage(nint handle)
	{
		if (!_handleToOwner.TryGetValue(handle, out var weakRef) ||
			!weakRef.TryGetTarget(out var owner))
		{
			return null;
		}

		var lcid = AutomationProperties.GetCulture(owner);
		if (lcid == 0)
		{
			return null;
		}

		try
		{
			return new System.Globalization.CultureInfo(lcid).Name;
		}
		catch (CultureNotFoundException ex)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y] Ignoring invalid AutomationProperties.Culture LCID {lcid}: {ex.Message}");
			}

			return null;
		}
	}

	private static string Localize(string key)
		=> NSBundle.MainBundle.GetLocalizedString(key, key, null).ToString();

	private static UIAccessibilityCustomAction CreateCustomAction(
		string name,
		Func<UIAccessibilityCustomAction, bool> handler)
		=> new(name, handler);

	internal AXCustomContent[]? GetCustomContent(nint handle)
	{
		var peer = ResolvePeer(handle);
		if (peer is null ||
			!_handleToOwner.TryGetValue(handle, out var weakReference) ||
			!weakReference.TryGetTarget(out var owner))
		{
			return null;
		}

		var content = new List<AXCustomContent>();

		void Add(string label, string? value, AXCustomContentImportance importance = AXCustomContentImportance.Default)
		{
			if (string.IsNullOrEmpty(value))
			{
				return;
			}

			var item = AXCustomContent.Create(
				Localize(label),
				value);
			item.Importance = importance;
			content.Add(item);
		}

		Add("Item Type", peer.GetItemType());
		Add("Control Type", peer.GetLocalizedControlType());

		if (peer.GetPattern(PatternInterface.GridItem) is IGridItemProvider gridItemProvider)
		{
			Add("Row", (gridItemProvider.Row + 1).ToString(CultureInfo.CurrentCulture));
			Add("Column", (gridItemProvider.Column + 1).ToString(CultureInfo.CurrentCulture));
			if (gridItemProvider.RowSpan > 1)
			{
				Add("Row Span", gridItemProvider.RowSpan.ToString(CultureInfo.CurrentCulture));
			}
			if (gridItemProvider.ColumnSpan > 1)
			{
				Add("Column Span", gridItemProvider.ColumnSpan.ToString(CultureInfo.CurrentCulture));
			}
		}

		if (AutomationProperties.GetIsRequiredForForm(owner))
		{
			Add("Required", Localize("Yes"));
		}

		if (!AutomationProperties.GetIsDataValidForForm(owner))
		{
			Add(
				"Validation",
				Localize("Invalid"),
				AXCustomContentImportance.High);
		}

		var level = peer.GetLevel();
		if (level > 0)
		{
			Add("Level", level.ToString(CultureInfo.CurrentCulture));
		}

		var position = peer.GetPositionInSet();
		var size = peer.GetSizeOfSet();
		if (position > 0)
		{
			var value = size > 0
				? string.Format(CultureInfo.CurrentCulture, "{0} of {1}", position, size)
				: position.ToString(CultureInfo.CurrentCulture);
			Add("Position", value);
		}

		if (peer.GetLandmarkType() != AutomationLandmarkType.None)
		{
			Add(
				"Landmark",
				peer.GetLocalizedLandmarkType() is { Length: > 0 } localizedLandmark
					? localizedLandmark
					: peer.GetLandmarkType().ToString());
		}

		if (owner.GetValue(AutomationProperties.AnnotationsProperty) is
			IEnumerable<AutomationAnnotation> annotations)
		{
			foreach (var annotation in annotations)
			{
				Add("Annotation", annotation.Type.ToString());
			}
		}

		return content.Count > 0 ? content.ToArray() : null;
	}

	private static UIAccessibilityTrait BuildTraits(AutomationPeer peer, bool isSelected)
	{
		var traits = UIAccessibilityTrait.None;

		if (!peer.IsEnabled())
		{
			traits |= UIAccessibilityTrait.NotEnabled;
		}

		if (peer.GetHeadingLevel() != AutomationHeadingLevel.None)
		{
			traits |= UIAccessibilityTrait.Header;
		}

		// Adjustable only for writable range; read-only progress bars must not advertise this.
		if (peer.IsEnabled() &&
			peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rv &&
			!rv.IsReadOnly)
		{
			traits |= UIAccessibilityTrait.Adjustable;
		}

		// Selected item: UIKit reads this on focused elements to announce selection state.
		if (isSelected)
		{
			traits |= UIAccessibilityTrait.Selected;
		}

		switch (peer.GetAutomationControlType())
		{
			case AutomationControlType.Button:
			case AutomationControlType.CheckBox:
			case AutomationControlType.RadioButton:
				traits |= UIAccessibilityTrait.Button;
				break;

			case AutomationControlType.Hyperlink:
				traits |= UIAccessibilityTrait.Link;
				break;

			case AutomationControlType.Image:
				traits |= UIAccessibilityTrait.Image;
				break;

			case AutomationControlType.Text:
				traits |= UIAccessibilityTrait.StaticText;
				break;

			case AutomationControlType.Header:
				traits |= UIAccessibilityTrait.Header;
				break;
		}

		return traits;
	}
}
