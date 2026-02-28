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
using Uno.Foundation.Logging;
using Uno.Helpers;

namespace Uno.UI.Runtime.Skia;

internal partial class WebAssemblyAccessibility : IUnoAccessibility, IAutomationPeerListener
{
	private static readonly Lazy<WebAssemblyAccessibility> _instance = new Lazy<WebAssemblyAccessibility>(() => new());

	internal static WebAssemblyAccessibility Instance => _instance.Value;

	public WebAssemblyAccessibility()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(WebAssemblyAccessibility)}");
		}

		AccessibilityAnnouncer.AccessibilityImpl = this;
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnSizeOrOffsetChanged;
		AutomationPeer.AutomationPeerListener = this;
	}

	public bool IsAccessibilityEnabled { get; private set; }

	// Subsystem managers (initialized during accessibility activation)
	private LiveRegionManager? _liveRegionManager;
	private FocusSynchronizer? _focusSynchronizer;
	internal ModalFocusScope? ActiveModalScope { get; set; }
	private readonly List<VirtualizedSemanticRegion> _virtualizedRegions = new();

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

	private void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (IsAccessibilityEnabled)
		{
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
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] OnChildAdded: semanticParent found={semanticParent} for child={child.GetType().Name} handle={child.Visual.Handle}");
			}

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

			// Always recurse into children — if this element was skipped,
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

	private void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (IsAccessibilityEnabled)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] OnChildRemoved: parent={parent.GetType().Name} handle={parent.Visual.Handle} child={child.GetType().Name} handle={child.Visual.Handle}");
			}

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
				}
			};
		}
	}

	private static void EnumerateFocusableChildren(UIElement parent, List<IntPtr> focusableHandles)
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

	private void OnSizeOrOffsetChanged(Visual visual)
	{
		// TODO: transformations (e.g, RenderTransform) are not yet handled :/
		if (IsAccessibilityEnabled && visual is ShapeVisual shapeVisual)
		{
			if (!visual.IsVisible)
			{
				NativeMethods.HideSemanticElement(shapeVisual.Handle);
			}
			else
			{
				var handle = shapeVisual.Handle;
				if (_semanticParentMap.TryGetValue(handle, out var semanticParentHandle)
					&& (visual as ContainerVisual)?.Owner?.Target is UIElement element)
				{
					var totalOffset = GetOffsetRelativeToSemanticParent(element, semanticParentHandle);
					NativeMethods.UpdateSemanticElementPositioning(handle, shapeVisual.Size.X, shapeVisual.Size.Y, totalOffset.X, totalOffset.Y);
				}
				else
				{
					// Root element or element not in semantic map — use local offset
					var totalOffset = visual.GetTotalOffset();
					NativeMethods.UpdateSemanticElementPositioning(handle, shapeVisual.Size.X, shapeVisual.Size.Y, totalOffset.X, totalOffset.Y);
				}
			}
		}
	}

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
				@this.Log().LogWarning("EnableA11y is called for the second time. This shouldn't happen.");
			}

			return;
		}

		var window = WebAssemblyWindowWrapper.Instance.Window;
		var rootElement = window?.RootElement;

		if (rootElement is null)
		{
			if (@this.Log().IsEnabled(LogLevel.Error))
			{
				@this.Log().Error($"[A11y] EnableAccessibility() ERROR: Window={window?.GetType().Name ?? "null"}, RootElement={rootElement?.GetType().Name ?? "null"}");
			}

			// Retry with delay if we haven't exceeded max retries
			if (_enableAccessibilityRetryCount < MaxEnableAccessibilityRetries)
			{
				_enableAccessibilityRetryCount++;
				if (@this.Log().IsEnabled(LogLevel.Debug))
				{
					@this.Log().Debug($"[A11y] EnableAccessibility() will retry in {EnableAccessibilityRetryDelayMs}ms (attempt {_enableAccessibilityRetryCount}/{MaxEnableAccessibilityRetries})");
				}

				var timer = new Timer(
					_ =>
					{
						if (@this.Log().IsEnabled(LogLevel.Debug))
						{
							@this.Log().Debug($"[A11y] EnableAccessibility() retry attempt {_enableAccessibilityRetryCount}");
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
					@this.Log().Error($"[A11y] EnableAccessibility() ERROR: Max retries ({MaxEnableAccessibilityRetries}) exceeded. Window still not ready.");
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

		@this.IsAccessibilityEnabled = true;
		@this.CreateAOM(rootElement);
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
			// TODO: We shouldn't check individual scrollers.
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
			@this.Log().Trace($"OnTextInput called for handle: {handle}, value length: {value?.Length ?? 0}");
		}

		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
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

	private static bool IsAccessibilityFocusable(DependencyObject dependencyObject, bool isFocusable)
	{
		// We'll consider TextBlock and RichTextBlock as accessibility focusable, even if they are not focusable.
		// Screen readers should read them.
		if (!isFocusable && dependencyObject is not (TextBlock or RichTextBlock))
		{
			return false;
		}

		var accessibilityView = AutomationProperties.GetAccessibilityView(dependencyObject);
		if (accessibilityView == AccessibilityView.Raw)
		{
			return false;
		}

		// TODO: Adjust when TextElement's automation peers are supported.
		if ((dependencyObject as UIElement)?.GetOrCreateAutomationPeer() is null)
		{
			return false;
		}

		return true;
	}

	internal void CreateAOM(UIElement rootElement)
	{
		Debug.Assert(IsAccessibilityEnabled);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM called: rootElement={rootElement.GetType().Name}, handle={rootElement.Visual.Handle}");
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM: rootElement={rootElement.GetType().Name}, handle={rootElement.Visual.Handle}, size={rootElement.Visual.Size.X}x{rootElement.Visual.Size.Y}");
		}

		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		var rootHandle = rootElement.Visual.Handle;

		// Root element is placed directly under uno-semantics-root — use its local offset
		var rootOffset = rootElement.Visual.GetTotalOffset();
		NativeMethods.AddRootElementToSemanticsRoot(rootHandle, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, rootOffset.X, rootOffset.Y, IsAccessibilityFocusable(rootElement, rootElement.IsFocusable));

		// Set role="application" on the root so VoiceOver uses app interaction mode
		// instead of document-style page navigation
		NativeMethods.UpdateLandmarkRole(rootHandle, "application");

		var topLevelChildren = rootElement.GetChildren().ToList();
		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM: found {topLevelChildren.Count} top-level children");
		}

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
			this.Log().Debug($"[A11y] CreateAOM complete: semantic tree construction finished");
		}

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] CreateAOM: semantic tree construction complete");
		}
	}

	/// <summary>
	/// Determines whether a UIElement should be included in the semantic accessibility tree.
	/// Elements without an automation peer, ARIA role, or automation ID are purely structural
	/// (e.g., Grid, Border, ContentPresenter) and are pruned to reduce DOM bloat.
	/// </summary>
	private static bool IsSemanticElement(UIElement element)
	{
		// Elements with an automation peer are always semantic
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
		if (_semanticParentMap.ContainsKey(handle) || IsSemanticElement(visualParent))
		{
			return handle;
		}

		// Walk up searching for a semantic ancestor
		if (_semanticParentMap.TryGetValue(handle, out var mappedParent))
		{
			return mappedParent;
		}

		// Fallback: walk the visual tree up
		var parent = visualParent.GetParent() as UIElement;
		while (parent is not null)
		{
			var parentHandle = parent.Visual.Handle;
			if (_semanticParentMap.ContainsKey(parentHandle) || IsSemanticElement(parent))
			{
				return parentHandle;
			}
			parent = parent.GetParent() as UIElement;
		}

		// Ultimate fallback: use the visual parent handle
		return handle;
	}

	internal void BuildSemanticsTreeRecursive(IntPtr parentHandle, UIElement child, int depth = 0)
	{
		Debug.Assert(IsAccessibilityEnabled);

		var handle = child.Visual.Handle;
		var indent = new string(' ', depth * 2);
		var isSemantic = IsSemanticElement(child);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			var peer = child.GetOrCreateAutomationPeer();
			var peerName = peer?.GetName();
			var peerType = peer?.GetAutomationControlType().ToString() ?? "(no peer)";
			this.Log().Debug($"[A11y]{indent} depth={depth} type={child.GetType().Name} handle={handle} controlType={peerType} name='{peerName}' semantic={isSemantic}");
		}

		// Determine the effective parent for children of this element
		var effectiveParent = parentHandle;

		if (isSemantic)
		{
			var added = AddSemanticElement(parentHandle, child, null);
			if (added)
			{
				_semanticParentMap[handle] = parentHandle;
				effectiveParent = handle; // children go under this element
			}
			else if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn($"[A11y]{indent} AddSemanticElement returned false for {child.GetType().Name} handle={handle}");
			}
		}
		else
		{
			// Non-semantic element: skip it, children will be parented to the same parentHandle
			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[A11y]{indent} PRUNED {child.GetType().Name} handle={handle} — not semantic, reparenting children to parent={parentHandle}");
			}
		}

		// Always recurse into children
		foreach (var childChild in child.GetChildren())
		{
			BuildSemanticsTreeRecursive(effectiveParent, childChild, depth + 1);
		}
	}

	private bool AddSemanticElement(IntPtr parentHandle, UIElement child, int? index)
	{
		var totalOffset = GetOffsetRelativeToSemanticParent(child, parentHandle);
		var automationPeer = child.GetOrCreateAutomationPeer();


		if (automationPeer is null && this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] AddSemanticElement: no AutomationPeer for {child.GetType().Name} handle={child.Visual.Handle} — will use generic fallback");
		}

		// Try to create type-specific semantic elements (button, slider, checkbox, etc.)
		// This provides better keyboard support and screen reader compatibility
		if (automationPeer is not null)
		{
			var elementType = AriaMapper.GetSemanticElementType(automationPeer);
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				var label = automationPeer.GetName();
				this.Log().Debug($"[A11y] AddSemanticElement: factory dispatch — control={child.GetType().Name} handle={child.Visual.Handle} elementType={elementType} label='{label}' parent={parentHandle} index={index?.ToString(CultureInfo.InvariantCulture) ?? "append"}");
			}

			var created = SemanticElementFactory.CreateElement(
				automationPeer,
				child.Visual.Handle,
				parentHandle,
				index,
				totalOffset.X,
				totalOffset.Y,
				child.Visual.Size.X,
				child.Visual.Size.Y);

			if (created)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"[A11y] AddSemanticElement: OK — created {elementType} for {child.GetType().Name} handle={child.Visual.Handle}");
				}

				return true;
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] AddSemanticElement: factory returned false for {child.GetType().Name} elementType={elementType} — falling through to generic AddSemanticElement path");
			}
		}

		// Fall back to generic semantic element for unsupported control types
		var role = AutomationProperties.FindHtmlRole(child);
		var automationId = AutomationProperties.GetAutomationId(child);
		var horizontallyScrollable = false;
		var verticallyScrollable = false;
		if (automationPeer is not null)
		{
			// TODO: Verify if this is the right behavior.
			if (string.IsNullOrEmpty(automationId))
			{
				automationId = automationPeer.GetName();
			}
		}

		if (automationPeer is IScrollProvider scrollProvider)
		{
			//horizontallyScrollable = scrollProvider.HorizontallyScrollable;
			//verticallyScrollable = scrollProvider.VerticallyScrollable;
			horizontallyScrollable = true;
			verticallyScrollable = true;
		}
		else if (child.IsScrollPort)
		{
			// Workaround: ScrollViewerAutomationPeer isn't implemented.
			//var extentWidth = sv.ExtentWidth;
			//var viewportWidth = sv.ViewportWidth;
			//var minHorizontalOffset = sv.MinHorizontalOffset;
			//horizontallyScrollable = DoubleUtil.GreaterThan(extentWidth, viewportWidth + minHorizontalOffset);

			//var extentHeight = sv.ExtentHeight;
			//var viewportHeight = sv.ViewportHeight;
			//var minVerticalOffset = sv.MinVerticalOffset;
			//verticallyScrollable = DoubleUtil.GreaterThan(extentHeight, viewportHeight + minVerticalOffset);
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
		// TODO: aria-valuenow, aria-valuemin, aria-valuemax for Slider

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"[A11y] AddSemanticElement using generic path: role='{role}' automationId='{automationId}'");
			this.Log().Debug($"[A11y] AddSemanticElement: generic path — control={child.GetType().Name} handle={child.Visual.Handle} role='{role}' automationId='{automationId}' focusable={IsAccessibilityFocusable(child, child.IsFocusable)} visible={child.Visual.IsVisible} hScroll={horizontallyScrollable} vScroll={verticallyScrollable}");
		}

		var result = NativeMethods.AddSemanticElement(parentHandle, child.Visual.Handle, index, child.Visual.Size.X, child.Visual.Size.Y, totalOffset.X, totalOffset.Y, role, automationId, IsAccessibilityFocusable(child, child.IsFocusable), ariaChecked, child.Visual.IsVisible, horizontallyScrollable, verticallyScrollable, child.GetType().Name);

		if (!result)
		{
			if (this.Log().IsEnabled(LogLevel.Error))
			{
				this.Log().Error($"[A11y] AddSemanticElement ERROR: NativeMethods.AddSemanticElement returned false for {child.GetType().Name} handle={child.Visual.Handle} — parent={parentHandle} may not exist in JS DOM");
			}
		}

		if (!result && this.Log().IsEnabled(LogLevel.Warning))
		{
			this.Log().Warn($"[A11y] AddSemanticElement: NativeMethods.AddSemanticElement returned false for {child.GetType().Name} handle={child.Visual.Handle} — parent handle {parentHandle} not found in JS DOM");
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

	public void AnnouncePolite(string text)
		=> NativeMethods.AnnouncePolite(text);

	public void AnnounceAssertive(string text)
		=> NativeMethods.AnnounceAssertive(text);

	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty &&
			TryGetPeerOwner(peer, out var element))
		{
			var ariaChecked = ConvertToAriaChecked((ToggleState)newValue);
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: ToggleState handle={element.Visual.Handle} element={element.GetType().Name} old={oldValue} new={newValue} ariaChecked={ariaChecked}");
			}
			NativeMethods.UpdateAriaChecked(element.Visual.Handle, ariaChecked);
		}
		else if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] PROP CHANGE: Name handle={element.Visual.Handle} element={element.GetType().Name} old='{oldValue}' new='{newValue}'");
			}
			OnAutomationNameChanged(element, (string)newValue);
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
		}
		else if ((automationProperty == RangeValuePatternIdentifiers.ValueProperty ||
			automationProperty == RangeValuePatternIdentifiers.MinimumProperty ||
			automationProperty == RangeValuePatternIdentifiers.MaximumProperty) &&
			TryGetPeerOwner(peer, out element))
		{
			// Sync slider value/min/max to semantic DOM element
			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValueProvider)
			{
				NativeMethods.UpdateSliderValue(
					element.Visual.Handle,
					rangeValueProvider.Value,
					rangeValueProvider.Minimum,
					rangeValueProvider.Maximum);
			}
		}
		else if ((automationProperty == ScrollPatternIdentifiers.HorizontalScrollPercentProperty ||
			automationProperty == ScrollPatternIdentifiers.VerticalScrollPercentProperty) &&
			TryGetPeerOwner(peer, out element) && element is ScrollViewer { Presenter: { } presenter } sv)
		{
			NativeMethods.UpdateNativeScrollOffsets(presenter.Visual.Handle, sv.HorizontalOffset, sv.VerticalOffset);
		}
	}

	public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

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
		}
	}

	public bool ListenerExistsHelper(AutomationEvents eventId)
		=> IsAccessibilityEnabled;

	private static bool TryGetPeerOwner(AutomationPeer peer, [NotNullWhen(true)] out UIElement? owner)
	{
		if (peer is FrameworkElementAutomationPeer { Owner: { } element })
		{
			owner = element;
			return true;
		}

		owner = null;
		return false;
	}

	// TODO (DOTI): Added with macOS automation, maybe won't be needed for wasm
	public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId) => throw new NotImplementedException();
	public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId) => throw new NotImplementedException();

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
		internal static partial void CreateTextBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string value, bool multiline, bool password, bool readOnly);

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
		internal static partial void UpdateSliderValue(IntPtr handle, double value, double min, double max);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateTextBoxValue")]
		internal static partial void UpdateTextBoxValue(IntPtr handle, string value, int selectionStart, int selectionEnd);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateExpandCollapseState")]
		internal static partial void UpdateExpandCollapseState(IntPtr handle, bool expanded);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateSelectionState")]
		internal static partial void UpdateSelectionState(IntPtr handle, bool selected);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateDisabledState")]
		internal static partial void UpdateDisabledState(IntPtr handle, bool disabled);

		// ===== VoiceOver Enhancement Methods =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescription")]
		internal static partial void UpdateAriaDescription(IntPtr handle, string description);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateLandmarkRole")]
		internal static partial void UpdateLandmarkRole(IntPtr handle, string role);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaRoleDescription")]
		internal static partial void UpdateAriaRoleDescription(IntPtr handle, string roleDescription);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createHeadingElement")]
		internal static partial void CreateHeadingElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, int level, string? label);

		// ===== Debug Mode =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.enableDebugMode")]
		internal static partial void EnableDebugMode(bool enabled);

		// ===== Focus Management =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.focusSemanticElement")]
		internal static partial void FocusSemanticElement(IntPtr handle);
	}
}
