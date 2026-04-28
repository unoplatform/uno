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

		try
		{
			TrySubscribeScrollSource(child);

			var isChildSemantic = IsSemanticElement(child);

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] OnChildAdded: parent={parent.GetType().Name} handle={parent.Visual.Handle} child={child.GetType().Name} handle={child.Visual.Handle} index={index?.ToString(CultureInfo.InvariantCulture) ?? "append"}");
			}

			// Detect virtualized containers for accessibility tracking
			TryRegisterVirtualizedContainer(child);
			// Detect ContentDialog for focus trapping
			TryRegisterModalDialog(child);

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
			if (child is not (ListViewBase or ItemsRepeater))
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
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (!_isAccessibilityEnabled || _isCreatingAOM)
		{
			return;
		}

		try
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] OnChildRemoved: parent={parent.GetType().Name} handle={parent.Visual.Handle} child={child.GetType().Name} handle={child.Visual.Handle}");
			}

			TryUnsubscribeScrollSource(child);
			TryUnregisterVirtualizedContainer(child);

			// Remove any children of this element first (they may be semantic even if parent isn't)
			foreach (var childChild in child.GetChildren())
			{
				OnChildRemoved(child, childChild);
			}

			// Only remove from DOM if this element was actually in the semantic tree
			var childHandle = child.Visual.Handle;
			if (_semanticParentMap.TryGetValue(childHandle, out var semanticParent))
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] OnChildRemoved: REMOVING from semantic tree child={child.GetType().Name} handle={childHandle} semanticParent={semanticParent}");
				}
				RemoveSemanticElement(semanticParent, childHandle);
				_semanticParentMap.Remove(childHandle);
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
		if (element is ItemsRepeater repeater)
		{
			var region = new VirtualizedSemanticRegion(
				repeater.Visual.Handle,
				"listbox",
				repeater.GetOrCreateAutomationPeer()?.GetName(),
				false);
			_virtualizedRegions.Add(region);

			repeater.ElementPrepared += (s, e) =>
			{
				var itemElement = e.Element;
				var itemIndex = e.Index;
				var peer = itemElement.GetOrCreateAutomationPeer();
				var label = peer?.GetName() ?? string.Empty;
				var totalCount = repeater.ItemsSourceView?.Count ?? 0;
				var offset = GetOffsetRelativeToSemanticParent(itemElement, repeater.Visual.Handle);
				region.OnItemRealized(
					itemElement.Visual.Handle,
					itemIndex,
					totalCount,
					offset.X, offset.Y,
					itemElement.Visual.Size.X, itemElement.Visual.Size.Y,
					"option", label);
			};

			repeater.ElementClearing += (s, e) =>
			{
				var itemElement = e.Element;
				var info = ItemsRepeater.GetVirtualizationInfo(itemElement);
				if (info is not null)
				{
					region.OnItemUnrealized(itemElement.Visual.Handle, info.Index);
				}
			};
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

			// ListViewBase uses ContainerContentChanging for virtualization lifecycle
			listView.ContainerContentChanging += (s, e) =>
			{
				if (!e.InRecycleQueue)
				{
					var itemElement = e.ItemContainer;
					if (itemElement is not null)
					{
						var peer = itemElement.GetOrCreateAutomationPeer();
						var label = peer?.GetName() ?? string.Empty;
						var totalCount = listView.Items?.Count ?? 0;
						var offset = GetOffsetRelativeToSemanticParent(itemElement, listView.Visual.Handle);
						region.OnItemRealized(
							itemElement.Visual.Handle,
							e.ItemIndex,
							totalCount,
							offset.X, offset.Y,
							itemElement.Visual.Size.X, itemElement.Visual.Size.Y,
							isGrid ? "row" : "option", label);
					}
				}
				else
				{
					var itemElement = e.ItemContainer;
					if (itemElement is not null)
					{
						region.OnItemUnrealized(itemElement.Visual.Handle, e.ItemIndex);
					}
				}
			};
		}
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

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM complete");
		}
	}

	/// <summary>
	/// Determines whether a UIElement should be included in the semantic accessibility tree.
	/// Elements without an automation peer, ARIA role, or automation ID are purely structural
	/// (e.g., Grid, Border, ContentPresenter) and are pruned to reduce DOM bloat.
	/// </summary>
	private static bool IsSemanticElement(UIElement element)
	{
		// Elements with AccessibilityView="Raw" are excluded from the accessibility tree entirely.
		// This matches WinUI3 behavior where Raw elements are not exposed to UIA.
		var accessibilityView = AutomationProperties.GetAccessibilityView(element);
		if (accessibilityView == AccessibilityView.Raw)
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
			return false;
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

		// Don't recurse into virtualized containers — their items are managed
		// by VirtualizedSemanticRegion via ContainerContentChanging/ElementPrepared.
		if (child is ListViewBase or ItemsRepeater)
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
				child);

			if (created)
			{
				return true;
			}

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y] AddSemanticElement: factory returned false for {child.GetType().Name} elementType={elementType} — falling through to generic path");
			}
		}

		// Fall back to generic semantic element for unsupported control types.
		// Prefer AriaMapper role (covers Image, Group, etc.) over FindHtmlRole.
		var role = (automationPeer is not null
			? AriaMapper.GetAriaRole(automationPeer.GetAutomationControlType())
			: null)
			?? AutomationProperties.FindHtmlRole(child);

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
		var landmarkType = AutomationProperties.GetLandmarkType(child);
		if (landmarkType != AutomationLandmarkType.None)
		{
			var landmarkRole = AriaMapper.GetLandmarkRole(landmarkType);
			if (!string.IsNullOrEmpty(landmarkRole))
			{
				role = landmarkRole;
			}
		}

		var automationId = AutomationProperties.GetAutomationId(child);
		var horizontallyScrollable = false;
		var verticallyScrollable = false;
		if (automationPeer is not null)
		{

			if (string.IsNullOrEmpty(automationId))
			{
				automationId = automationPeer.GetName();
			}
		}
		else if (string.IsNullOrEmpty(automationId))
		{
			// For elements without an automation peer (e.g., named StackPanel/Border groups),
			// use AutomationProperties.Name as the label
			automationId = AutomationProperties.GetName(child);
		}

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
			this.Log().Trace($"[A11y] AddSemanticElement: generic path — control={child.GetType().Name} handle={child.Visual.Handle} role='{role}' automationId='{automationId}'");
		}

		var result = NativeMethods.AddSemanticElement(parentHandle, child.Visual.Handle, index, width, height, x, y, role, automationId, IsAccessibilityFocusable(child, child.IsFocusable), ariaChecked, child.Visual.IsVisible, horizontallyScrollable, verticallyScrollable, child.GetType().Name);

		if (!result && this.Log().IsEnabled(LogLevel.Error))
		{
			this.Log().Error($"[A11y] AddSemanticElement failed for {child.GetType().Name} handle={child.Visual.Handle} — parent={parentHandle} may not exist in JS DOM");
		}

		// Apply additional ARIA attributes for generic elements (landmarks, live regions, custom role descriptions)
		if (result)
		{
			var handle = child.Visual.Handle;

			// Custom landmark → aria-roledescription
			if (landmarkType == AutomationLandmarkType.Custom)
			{
				var localizedLandmarkType = AutomationProperties.GetLocalizedLandmarkType(child);
				if (!string.IsNullOrEmpty(localizedLandmarkType))
				{
					NativeMethods.UpdateAriaRoleDescription(handle, localizedLandmarkType);
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
			// hosted inside a fallback role, custom controls) need aria-expanded / aria-keyshortcuts
			// applied post-hoc. Factory paths handle their own creation-time wiring.
			if (automationPeer is not null)
			{
				try
				{
					if (automationPeer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider)
					{
						var expanded = expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.Expanded ||
									   expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.PartiallyExpanded;
						NativeMethods.UpdateExpandCollapseState(handle, expanded);
					}
				}
				catch
				{
					// Some peers throw if queried before fully initialized. Update will arrive via property change.
				}

				var acceleratorKey = automationPeer.GetAcceleratorKey();
				var accessKey = automationPeer.GetAccessKey();
				if (!string.IsNullOrEmpty(acceleratorKey) || !string.IsNullOrEmpty(accessKey))
				{
					var keyShortcuts = string.IsNullOrEmpty(accessKey)
						? acceleratorKey
						: string.IsNullOrEmpty(acceleratorKey) ? accessKey : $"{acceleratorKey} {accessKey}";
					NativeMethods.UpdateAriaKeyShortcuts(handle, keyShortcuts);
				}
			}
		}

		return result;
	}

	private void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle)
	{
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] RemoveSemanticElement: parent={parentHandle} child={childHandle}");
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
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: ToggleState handle={element.Visual.Handle} element={element.GetType().Name} old={oldValue} new={newValue} ariaChecked={ariaChecked}");
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
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: Name handle={element.Visual.Handle} element={element.GetType().Name} old='{oldValue}' new='{newValue}'");
			}
			OnAutomationNameChanged(element, (string)newValue);

			// When the accessible name changes on a live region element, trigger
			// the announcement. In WinUI3, the OS UIA framework monitors content
			// changes on live regions automatically. We replicate that here.
			var liveSetting = peer.GetLiveSetting();
			if (liveSetting != AutomationLiveSetting.Off)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] PROP CHANGE: Name on LiveRegion — triggering announcement liveSetting={liveSetting} content='{newValue}'");
				}
				_liveRegionManager?.HandleLiveRegionChanged(peer);
			}
		}
		else if (automationProperty == AutomationElementIdentifiers.HelpTextProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: HelpText handle={element.Visual.Handle} element={element.GetType().Name} new='{newValue}'");
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
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: IsEnabled handle={element.Visual.Handle} element={element.GetType().Name} disabled={isDisabled}");
			}
			NativeMethods.UpdateDisabledState(element.Visual.Handle, isDisabled);
		}
		else if (automationProperty == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var expanded = (ExpandCollapseState)newValue == ExpandCollapseState.Expanded ||
							(ExpandCollapseState)newValue == ExpandCollapseState.PartiallyExpanded;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: ExpandCollapse handle={element.Visual.Handle} element={element.GetType().Name} expanded={expanded}");
			}
			NativeMethods.UpdateExpandCollapseState(element.Visual.Handle, expanded);
		}
		else if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var selected = (bool)newValue;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: IsSelected handle={element.Visual.Handle} element={element.GetType().Name} selected={selected}");
			}
			NativeMethods.UpdateSelectionState(element.Visual.Handle, selected);

			// Update roving tabindex: the newly selected item gets tabindex=0,
			// other group members get tabindex=-1 (for listbox options, radio groups, tabs)
			if (selected)
			{
				// Use groupHandle=0 to let TS infer the group from the element's context
				NativeMethods.UpdateRovingTabindex(IntPtr.Zero, element.Visual.Handle);

				// Update aria-activedescendant on the parent container (combobox/listbox)
				// so screen readers announce the active option without moving DOM focus
				if (peer.GetParent() is FrameworkElementAutomationPeer { Owner: { } parentOwner })
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
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] PROP CHANGE: ComboBox Value handle={element.Visual.Handle} selectedValue='{selectedValue}'");
				}
				NativeMethods.UpdateAriaLabel(element.Visual.Handle, selectedValue);
			}
			else if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
			{
				// Sync programmatic text value changes to the semantic DOM element
				// (e.g., TextBox.Text set from code-behind)
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] PROP CHANGE: Value handle={element.Visual.Handle} element={element.GetType().Name} valueLen={valueProvider.Value?.Length ?? 0}");
				}
				UpdateTextBoxValueKeepingSelection(element.Visual.Handle, valueProvider.Value, element as TextBox);
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.IsReadOnlyProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var isReadOnly = (bool)newValue;
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: IsReadOnly handle={element.Visual.Handle} element={element.GetType().Name} readOnly={isReadOnly}");
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
			// Dynamic aria-labelledby: when LabeledBy changes after creation
			var attributes = AriaMapper.GetAriaAttributes(peer);
			if (!string.IsNullOrEmpty(attributes.LabelledBy))
			{
				NativeMethods.UpdateAriaLabelledBy(element.Visual.Handle, attributes.LabelledBy);
			}
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
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] AUTOMATION EVENT: LiveRegionChanged peer={peer.GetType().Name}");
				}
				_liveRegionManager?.HandleLiveRegionChanged(peer);
				break;

			case AutomationEvents.TextEditTextChanged:
			case AutomationEvents.TextPatternOnTextChanged:
				// Sync text value changes to the semantic DOM (handles programmatic TextBox.Text updates)
				if (TryGetPeerOwner(peer, out var textElement) &&
					peer.GetPattern(PatternInterface.Value) is IValueProvider textValueProvider)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"[A11y] AUTOMATION EVENT: {eventId} handle={textElement.Visual.Handle} valueLen={textValueProvider.Value?.Length ?? 0}");
					}
					UpdateTextBoxValueKeepingSelection(textElement.Visual.Handle, textValueProvider.Value, textElement as TextBox);
				}
				break;

			case AutomationEvents.AutomationFocusChanged:
				// Route focus changes to the semantic DOM so the browser focus ring follows
				if (TryGetPeerOwner(peer, out var focusElement))
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().Debug($"[A11y] AUTOMATION EVENT: AutomationFocusChanged handle={focusElement.Visual.Handle} element={focusElement.GetType().Name}");
					}
					NativeMethods.FocusSemanticElement(focusElement.Visual.Handle);
				}
				break;

			case AutomationEvents.StructureChanged:
				// Structure changes (children added/removed) require the screen reader to
				// re-scan the accessible tree. The browser handles this automatically when
				// DOM nodes are added/removed, so no explicit notification is needed.
				// This is here for completeness and logging.
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] AUTOMATION EVENT: StructureChanged peer={peer.GetType().Name}");
				}
				break;

			case AutomationEvents.InvokePatternOnInvoked:
				// After a button invoke, screen readers may need to update state.
				// The property change notifications handle the actual state updates;
				// this event is logged for diagnostics.
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] AUTOMATION EVENT: InvokePatternOnInvoked peer={peer.GetType().Name}");
				}
				break;

			case AutomationEvents.SelectionItemPatternOnElementSelected:
				// Selection events trigger property change notifications which handle
				// the DOM state updates. Log for diagnostics.
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] AUTOMATION EVENT: SelectionItemPatternOnElementSelected peer={peer.GetType().Name}");
				}
				break;

			case AutomationEvents.SelectionPatternOnInvalidated:
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] AUTOMATION EVENT: SelectionPatternOnInvalidated peer={peer.GetType().Name}");
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
		internal static partial bool AddSemanticElement(IntPtr parentHandle, IntPtr handle, int? index, float width, float height, float x, float y, string role, string automationId, bool isFocusable, string? ariaChecked, bool isVisible, bool horizontallyScrollable, bool verticallyScrollable, string temporary);

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

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaPressed")]
		internal static partial void UpdateAriaPressed(IntPtr handle, string pressed);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaKeyShortcuts")]
		internal static partial void UpdateAriaKeyShortcuts(IntPtr handle, string keyShortcuts);

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
