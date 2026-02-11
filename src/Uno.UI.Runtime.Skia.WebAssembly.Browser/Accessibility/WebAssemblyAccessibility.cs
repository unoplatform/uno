#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

	private Vector3 GetVisualOffset(Visual visual)
	{
		return visual.GetTotalOffset();
	}

	private void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (IsAccessibilityEnabled)
		{
			if (AddSemanticElement(parent.Visual.Handle, child, index))
			{
				foreach (var childChild in child._children)
				{
					OnChildAdded(child, childChild, null);
				}
			}
		}
	}

	private void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (IsAccessibilityEnabled)
		{
			RemoveSemanticElement(parent.Visual.Handle, child.Visual.Handle);
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
				var totalOffset = GetVisualOffset(visual);
				NativeMethods.UpdateSemanticElementPositioning(shapeVisual.Handle, shapeVisual.Size.X, shapeVisual.Size.Y, totalOffset.X, totalOffset.Y);
			}
		}
	}

	[JSExport]
	public static void EnableAccessibility()
	{
		var @this = Instance;
		if (@this.IsAccessibilityEnabled)
		{
			if (@this.Log().IsEnabled(LogLevel.Warning))
			{
				@this.Log().LogWarning("EnableA11y is called for the second time. This shouldn't happen.");
			}

			return;
		}

		if (WebAssemblyWindowWrapper.Instance.Window?.RootElement is not { } rootElement)
		{
			if (@this.Log().IsEnabled(LogLevel.Warning))
			{
				@this.Log().LogWarning("EnableA11y is called while either Window or its RootElement is null. This shouldn't happen.");
			}

			return;
		}

		@this.IsAccessibilityEnabled = true;
		@this.CreateAOM(rootElement);
		Control.OnIsFocusableChangedCallback = @this.UpdateIsFocusable;
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
		var @this = Instance;
		if (@this.Log().IsEnabled(LogLevel.Trace))
		{
			@this.Log().Trace($"OnFocus called for handle: {handle}");
		}

		// Sync focus from semantic element to Uno element
		if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
		{
			if (owner is Control control && control.IsFocusable)
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
		NativeMethods.UpdateIsFocusable(control.Visual.Handle, IsAccessibilityFocusable(control, isFocusable));
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

		// We build an AOM (Accessibility Object Model):
		// https://wicg.github.io/aom/explainer.html
		var rootHandle = rootElement.Visual.Handle;

		var totalOffset = GetVisualOffset(rootElement.Visual);
		NativeMethods.AddRootElementToSemanticsRoot(rootHandle, rootElement.Visual.Size.X, rootElement.Visual.Size.Y, totalOffset.X, totalOffset.Y, IsAccessibilityFocusable(rootElement, rootElement.IsFocusable));
		foreach (var child in rootElement.GetChildren())
		{
			BuildSemanticsTreeRecursive(rootHandle, child);
		}
	}

	internal void BuildSemanticsTreeRecursive(IntPtr parentHandle, UIElement child)
	{
		Debug.Assert(IsAccessibilityEnabled);

		var handle = child.Visual.Handle;

		AddSemanticElement(parentHandle, child, null);
		foreach (var childChild in child.GetChildren())
		{
			BuildSemanticsTreeRecursive(handle, childChild);
		}
	}

	private bool AddSemanticElement(IntPtr parentHandle, UIElement child, int? index)
	{
		var totalOffset = GetVisualOffset(child.Visual);
		var automationPeer = child.GetOrCreateAutomationPeer();

		// Try to create type-specific semantic elements (button, slider, checkbox, etc.)
		// This provides better keyboard support and screen reader compatibility
		if (automationPeer is not null)
		{
			var created = SemanticElementFactory.CreateElement(
				automationPeer,
				child.Visual.Handle,
				totalOffset.X,
				totalOffset.Y,
				child.Visual.Size.X,
				child.Visual.Size.Y);

			if (created)
			{
				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					var elementType = AriaMapper.GetSemanticElementType(automationPeer);
					this.Log().Trace($"Created semantic element: type={elementType}, handle={child.Visual.Handle}, control={child.GetType().Name}");
				}

				return true;
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

		return NativeMethods.AddSemanticElement(parentHandle, child.Visual.Handle, index, child.Visual.Size.X, child.Visual.Size.Y, totalOffset.X, totalOffset.Y, role, automationId, IsAccessibilityFocusable(child, child.IsFocusable), ariaChecked, child.Visual.IsVisible, horizontallyScrollable, verticallyScrollable, child.GetType().Name);
	}

	private void RemoveSemanticElement(IntPtr parentHandle, IntPtr childHandle)
	{
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
			NativeMethods.UpdateAriaChecked(element.Visual.Handle, ariaChecked);
		}
		else if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, out element))
		{
			OnAutomationNameChanged(element, (string)newValue);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsEnabledProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Sync aria-disabled state when IsEnabled changes
			var isDisabled = !(bool)newValue;
			NativeMethods.UpdateDisabledState(element.Visual.Handle, isDisabled);
		}
		else if (automationProperty == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Sync aria-expanded state for comboboxes and expanders
			var expanded = (ExpandCollapseState)newValue == ExpandCollapseState.Expanded ||
			               (ExpandCollapseState)newValue == ExpandCollapseState.PartiallyExpanded;
			NativeMethods.UpdateExpandCollapseState(element.Visual.Handle, expanded);
		}
		else if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty &&
			TryGetPeerOwner(peer, out element))
		{
			// Sync aria-selected state for list items
			var selected = (bool)newValue;
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
		internal static partial void CreateButtonElement(IntPtr handle, float x, float y, float width, float height, string? label, bool disabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createSliderElement")]
		internal static partial void CreateSliderElement(IntPtr handle, float x, float y, float width, float height, double value, double min, double max, double step, string orientation);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTextBoxElement")]
		internal static partial void CreateTextBoxElement(IntPtr handle, float x, float y, float width, float height, string value, bool multiline, bool password, bool readOnly);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createCheckboxElement")]
		internal static partial void CreateCheckboxElement(IntPtr handle, float x, float y, float width, float height, string? checkedState, string? label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createRadioElement")]
		internal static partial void CreateRadioElement(IntPtr handle, float x, float y, float width, float height, bool isChecked, string? label, string? groupName);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createComboBoxElement")]
		internal static partial void CreateComboBoxElement(IntPtr handle, float x, float y, float width, float height, bool expanded, string? selectedValue);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListBoxElement")]
		internal static partial void CreateListBoxElement(IntPtr handle, float x, float y, float width, float height, bool multiselect);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListItemElement")]
		internal static partial void CreateListItemElement(IntPtr handle, float x, float y, float width, float height, bool selected, int positionInSet, int sizeOfSet);

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

		// ===== Debug Mode =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.enableDebugMode")]
		internal static partial void EnableDebugMode(bool enabled);

		// ===== Focus Management =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.focusSemanticElement")]
		internal static partial void FocusSemanticElement(IntPtr handle);
	}
}
