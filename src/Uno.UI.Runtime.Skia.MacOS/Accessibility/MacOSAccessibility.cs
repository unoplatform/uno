#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
using Uno.Extensions;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// Manages the macOS VoiceOver accessibility tree for Skia-rendered Uno applications.
/// Creates a parallel native accessibility tree (UNOAccessibilityElement objects) that
/// mirrors the XAML visual tree.
///
/// Uses the shared <see cref="AriaMapper"/> to resolve attributes from
/// automation peers, with macOS-specific attribute resolution for VoiceOver.
/// </summary>
internal class MacOSAccessibility : SkiaAccessibilityBase
{
	private static MacOSAccessibility? _instance;
	private nint _windowHandle;
	private bool _accessibilityTreeInitialized;
	private bool _isCreatingAOM;
	private nint _activeModalHandle;
	private nint _modalTriggerHandle;

	internal static MacOSAccessibility Instance => _instance ??= new MacOSAccessibility();

	private MacOSAccessibility()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(MacOSAccessibility)}");
		}

		RegisterCallbacks();
	}

	internal static unsafe void Register()
	{
		_ = Instance;

		NativeUno.uno_accessibility_set_callbacks(&OnNativeInvoke, &OnNativeFocus);
		NativeUno.uno_accessibility_set_range_callbacks(&OnNativeIncrement, &OnNativeDecrement);
		NativeUno.uno_accessibility_set_expand_collapse_callback(&OnNativeExpandCollapse);
		NativeUno.uno_accessibility_set_value_callback(&OnNativeSetValue);

		MacOSWindowNative.NativeWindowReady += Instance.OnNativeWindowReady;
	}

	// Called from native code when VoiceOver triggers a press action on an element
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeInvoke(nint handle)
	{
		if (handle == nint.Zero)
		{
			return;
		}

		try
		{
			if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
			{
				// WinUI 3 pattern implementations (e.g., ButtonAutomationPeer::InvokeImpl)
				// check IsEnabled and return UIA_E_ELEMENTNOTENABLED if disabled.
				// Guard here as well so all patterns are protected uniformly.
				if (owner is Control { IsEnabled: false })
				{
					return;
				}

				var peer = owner.GetOrCreateAutomationPeer();
				if (peer is IInvokeProvider invokeProvider)
				{
					invokeProvider.Invoke();
				}
				else if (peer is IToggleProvider toggleProvider)
				{
					toggleProvider.Toggle();
				}
				else if (peer is IExpandCollapseProvider expandCollapseProvider)
				{
					if (expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.Collapsed)
					{
						expandCollapseProvider.Expand();
					}
					else
					{
						expandCollapseProvider.Collapse();
					}
				}
				else if (peer is ISelectionItemProvider selectionItemProvider)
				{
					selectionItemProvider.Select();
				}
			}
		}
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSAccessibility));
		}
	}

	// Called from native code when VoiceOver sets focus on an element
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeFocus(nint handle)
	{
		if (handle == nint.Zero)
		{
			return;
		}

		try
		{
			if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
			{
				if (owner is Control control)
				{
					control.Focus(FocusState.Programmatic);
				}
			}
		}
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSAccessibility));
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeIncrement(nint handle)
	{
		if (handle == nint.Zero)
		{
			return;
		}

		try
		{
			if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
			{
				var peer = owner.GetOrCreateAutomationPeer();
				if (peer is IRangeValueProvider rangeProvider && !rangeProvider.IsReadOnly)
				{
					var newValue = Math.Min(rangeProvider.Value + rangeProvider.SmallChange, rangeProvider.Maximum);
					rangeProvider.SetValue(newValue);
				}
			}
		}
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSAccessibility));
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeDecrement(nint handle)
	{
		if (handle == nint.Zero)
		{
			return;
		}

		try
		{
			if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
			{
				var peer = owner.GetOrCreateAutomationPeer();
				if (peer is IRangeValueProvider rangeProvider && !rangeProvider.IsReadOnly)
				{
					var newValue = Math.Max(rangeProvider.Value - rangeProvider.SmallChange, rangeProvider.Minimum);
					rangeProvider.SetValue(newValue);
				}
			}
		}
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSAccessibility));
		}
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeExpandCollapse(nint handle)
	{
		if (handle == nint.Zero)
		{
			return;
		}

		try
		{
			if (GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
			{
				var peer = owner.GetOrCreateAutomationPeer();
				if (peer is IExpandCollapseProvider expandCollapseProvider)
				{
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
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSAccessibility));
		}
	}

	// Called from native code when VoiceOver sets a text value on an element
	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
	private static void OnNativeSetValue(nint handle, nint valuePtr)
	{
		if (handle == nint.Zero || valuePtr == nint.Zero)
		{
			return;
		}

		try
		{
			var value = Marshal.PtrToStringUTF8(valuePtr);
			if (value != null &&
				GCHandle.FromIntPtr(handle).Target is ContainerVisual { Owner.Target: UIElement owner })
			{
				var peer = owner.GetOrCreateAutomationPeer();
				if (peer?.GetPattern(PatternInterface.Value) is IValueProvider valueProvider &&
					!valueProvider.IsReadOnly)
				{
					valueProvider.SetValue(value);
				}
			}
		}
		catch (Exception e)
		{
			ApplicationExtensions.RaiseRecoverableUnhandledExceptionOrLog(Application.Current, e, typeof(MacOSAccessibility));
		}
	}

	public override bool IsAccessibilityEnabled => _windowHandle != nint.Zero;

	internal void Initialize(nint windowHandle)
	{
		_windowHandle = windowHandle;
		// Reset _accessibilityTreeInitialized so TryInitializeAccessibilityTree
		// will rebuild the tree for the new window. Without this, the native
		// g_elements dict is cleared by uno_accessibility_init but the managed
		// side thinks the tree is already initialized and never repopulates it.
		_accessibilityTreeInitialized = false;
		NativeUno.uno_accessibility_init(windowHandle);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MacOSAccessibility initialized for window 0x{windowHandle:X}");
		}
	}

	private void OnNativeWindowReady(object? sender, MacOSWindowNative nativeWindow)
	{
		// Only initialize the accessibility tree for the window that owns the current
		// accessibility singleton handle. Secondary windows do not call Initialize() so
		// their handle will never match _windowHandle, and we skip them safely.
		if (nativeWindow.Handle != _windowHandle)
		{
			return;
		}

		var capturedHandle = _windowHandle;
		_ = nativeWindow.Host.RootElement?.Dispatcher.RunAsync(
			Windows.UI.Core.CoreDispatcherPriority.Low,
			() =>
			{
				// Bail out if the window handle changed while we were queued
				// (e.g. another window was created and reinitialized the singleton).
				if (_windowHandle != capturedHandle)
				{
					return;
				}

				TryInitializeAccessibilityTree(nativeWindow.Host.RootElement);
			});
	}

	private void TryInitializeAccessibilityTree(UIElement? rootElement)
	{
		if (_accessibilityTreeInitialized || !IsAccessibilityEnabled || rootElement is null)
		{
			return;
		}

		_accessibilityTreeInitialized = true;
		InitializeAccessibilityTree(rootElement);

		NativeUno.uno_accessibility_post_layout_changed();

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"Accessibility tree initialized for window 0x{_windowHandle:X}");
		}
	}

	internal void InitializeAccessibilityTree(UIElement rootElement)
	{
		if (!IsAccessibilityEnabled)
		{
			return;
		}

		_isCreatingAOM = true;
		try
		{
			CreateAOM(rootElement);
		}
		finally
		{
			_isCreatingAOM = false;
		}
		Control.OnIsFocusableChangedCallback = UpdateIsFocusable;
	}

	/// <summary>
	/// Gets the absolute position of a visual within the window by walking up
	/// the parent chain and accumulating all local offsets. This is necessary
	/// because TotalMatrix may be stale when called from property-change callbacks
	/// (the dirty flag hasn't been set yet at that point).
	/// VoiceOver needs absolute window coordinates for the accessibility frame.
	/// </summary>
	private static Vector3 GetAbsoluteOffset(Visual visual)
	{
		var x = 0f;
		var y = 0f;
		var current = visual;
		while (current is not null)
		{
			var offset = current.GetTotalOffset();
			x += offset.X;
			y += offset.Y;
			current = current.Parent;
		}
		return new Vector3(x, y, 0);
	}

	protected override void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (_accessibilityTreeInitialized && !_isCreatingAOM)
		{
			// Only process elements that are part of the live visual tree.
			// ExternalOnChildAdded fires for ANY AddChild call, including during
			// element construction before the parent is in the visual tree.
			// Adding elements prematurely corrupts the native accessibility tree
			// (e.g., incorrectly overwriting g_rootElement).
			if (!parent.IsActiveInVisualTree)
			{
				return;
			}

			try
			{
				// Skip elements with AccessibilityView=Raw, but process their children
				// under the Raw element's parent to maintain the semantic tree.
				var accessibilityView = AutomationProperties.GetAccessibilityView(child);
				if (accessibilityView == AccessibilityView.Raw)
				{
					foreach (var childChild in child.GetChildren())
					{
						OnChildAdded(parent, childChild, null);
					}
					return;
				}

				AddAccessibilityElement(parent.Visual.Handle, child, index);
				TryRegisterModalDialog(child);
				foreach (var childChild in child.GetChildren())
				{
					OnChildAdded(child, childChild, null);
				}
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Failed to add accessibility element for {child}: {e.Message}");
				}
			}
		}
	}

	protected override void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (_accessibilityTreeInitialized && !_isCreatingAOM)
		{
			// If the child had AccessibilityView=Raw when added, it was skipped
			// in the native tree but its children were reparented to the semantic
			// parent. Remove those reparented children from the native tree.
			var accessibilityView = AutomationProperties.GetAccessibilityView(child);
			if (accessibilityView == AccessibilityView.Raw)
			{
				foreach (var childChild in child.GetChildren())
				{
					OnChildRemoved(parent, childChild);
				}
				return;
			}

			// Always attempt removal — the native side handles missing elements
			// gracefully via dictionary lookup. Skipping removal when the parent
			// is no longer in the visual tree would leak elements because during
			// teardown, parents are removed before children.
			NativeUno.uno_accessibility_remove_element(parent.Visual.Handle, child.Visual.Handle);
		}
	}

	protected override void OnSizeOrOffsetChanged(Visual visual)
	{
		if (IsAccessibilityEnabled && _accessibilityTreeInitialized
			&& visual is ContainerVisual containerVisual
			&& containerVisual.Owner?.Target is UIElement owner
			&& owner.IsActiveInVisualTree)
		{
			if (!visual.IsVisible)
			{
				NativeUno.uno_accessibility_update_visibility(containerVisual.Handle, false);
			}
			else
			{
				var totalOffset = GetAbsoluteOffset(visual);
				NativeUno.uno_accessibility_update_frame(
					containerVisual.Handle,
					containerVisual.Size.X, containerVisual.Size.Y,
					totalOffset.X, totalOffset.Y);
				NativeUno.uno_accessibility_update_visibility(containerVisual.Handle, true);
			}
		}
	}

	private void UpdateIsFocusable(Control control, bool isFocusable)
	{
		if (!_accessibilityTreeInitialized)
		{
			return;
		}

		NativeUno.uno_accessibility_update_focusable(
			control.Visual.Handle,
			IsAccessibilityFocusable(control, isFocusable));
	}


	/// <summary>
	/// Creates the Accessibility Object Model (AOM) from the root element.
	/// </summary>
	internal void CreateAOM(UIElement rootElement)
	{
		Debug.Assert(IsAccessibilityEnabled);

		var rootHandle = rootElement.Visual.Handle;
		var totalOffset = GetAbsoluteOffset(rootElement.Visual);
		var peer = rootElement.GetOrCreateAutomationPeer();
		var role = peer != null ? ResolveRole(peer, rootElement) : null;
		var label = peer != null ? ResolveLabel(peer) : null;

		NativeUno.uno_accessibility_add_element(
			nint.Zero, rootHandle, -1,
			rootElement.Visual.Size.X, rootElement.Visual.Size.Y,
			totalOffset.X, totalOffset.Y,
			role, label,
			IsAccessibilityFocusable(rootElement, rootElement.IsFocusable),
			true);

		if (peer != null)
		{
			ApplyAttributes(rootHandle, peer, rootElement);
		}

		foreach (var child in rootElement.GetChildren())
		{
			BuildAccessibilityTreeRecursive(rootHandle, child);
		}
	}

	private void BuildAccessibilityTreeRecursive(nint parentHandle, UIElement child)
	{
		Debug.Assert(IsAccessibilityEnabled);

		// Skip elements with AccessibilityView=Raw - they should be completely
		// hidden from the accessibility tree (VoiceOver skips them).
		// Their children are still added under the Raw element's parent,
		// preserving the semantic tree structure.
		var accessibilityView = AutomationProperties.GetAccessibilityView(child);
		if (accessibilityView == AccessibilityView.Raw)
		{
			foreach (var childChild in child.GetChildren())
			{
				BuildAccessibilityTreeRecursive(parentHandle, childChild);
			}
			return;
		}

		var handle = child.Visual.Handle;
		AddAccessibilityElement(parentHandle, child, null);
		foreach (var childChild in child.GetChildren())
		{
			BuildAccessibilityTreeRecursive(handle, childChild);
		}
	}

	/// <summary>
	/// Adds a single accessibility element to the native macOS accessibility tree.
	/// </summary>
	private void AddAccessibilityElement(nint parentHandle, UIElement child, int? index)
	{
		var totalOffset = GetAbsoluteOffset(child.Visual);
		var peer = child.GetOrCreateAutomationPeer();
		var isFocusable = IsAccessibilityFocusable(child, child.IsFocusable);

		string? role = null;
		string? label = null;

		if (peer != null)
		{
			try
			{
				role = ResolveRole(peer, child);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"[A11y] Failed to resolve role for {child.GetType().Name} handle={child.Visual.Handle}: {e.Message}");
				}
			}

			// Log when a peer has no role — this causes the native layer to fall back
			// to NSAccessibilityGroupRole which makes VoiceOver announce "group"
			if (role == null && this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"[A11y] No role resolved for {child.GetType().Name} handle={child.Visual.Handle} peer={peer.GetType().Name} — will default to group in native layer");
			}

			try
			{
				label = ResolveLabel(peer);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Warning))
				{
					this.Log().Warn($"[A11y] Failed to resolve label for {child.GetType().Name} handle={child.Visual.Handle}: {e.Message}");
				}
			}
		}

		NativeUno.uno_accessibility_add_element(
			parentHandle, child.Visual.Handle,
			index ?? -1,
			child.Visual.Size.X, child.Visual.Size.Y,
			totalOffset.X, totalOffset.Y,
			role, label,
			isFocusable,
			child.Visual.IsVisible);

		if (peer != null)
		{
			try
			{
				ApplyAttributes(child.Visual.Handle, peer, child);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Failed to apply accessibility attributes for {child}: {e.Message}");
				}
			}
		}

		if (child is Control control)
		{
			NativeUno.uno_accessibility_update_enabled(child.Visual.Handle, control.IsEnabled);
		}

		// Fire AXLiveRegionCreated for elements with a live setting
		if (peer != null)
		{
			var liveSetting = AutomationProperties.GetLiveSetting(child);
			if (liveSetting != AutomationLiveSetting.Off)
			{
				NativeUno.uno_accessibility_post_live_region_created(child.Visual.Handle);
			}
		}
	}

	/// <summary>
	/// Applies accessibility attributes to the native element using the shared
	/// AriaAttributes from <see cref="AriaMapper"/>. All semantic resolution is
	/// centralized in the shared mapper; this method only translates to native calls.
	/// </summary>
	private static void ApplyAttributes(nint handle, AutomationPeer peer, UIElement? owner = null)
	{
		var attributes = AriaMapper.GetAriaAttributes(peer);

		// Description → accessibilityHelp (VoiceOver reads this as secondary context)
		// AriaMapper resolves FullDescription > HelpText.
		if (!string.IsNullOrEmpty(attributes.Description))
		{
			NativeUno.uno_accessibility_update_help(handle, attributes.Description);
		}

		// PlaceholderText is surfaced as the empty-field description when a Header already
		// provides the primary label. VoiceOver reads this via accessibilityPlaceholderValue.
		if (peer is FrameworkElementAutomationPeer feap)
		{
			if (feap.Owner is TextBox tb && !string.IsNullOrEmpty(tb.Header?.ToString()) && !string.IsNullOrEmpty(tb.PlaceholderText))
			{
				NativeUno.uno_accessibility_update_description(handle, tb.PlaceholderText);
			}
			else if (feap.Owner is PasswordBox pb && !string.IsNullOrEmpty(pb.Header?.ToString()) && !string.IsNullOrEmpty(pb.PlaceholderText))
			{
				NativeUno.uno_accessibility_update_description(handle, pb.PlaceholderText);
			}
		}

		if (!string.IsNullOrEmpty(attributes.RoleDescription))
		{
			NativeUno.uno_accessibility_update_role_description(handle, attributes.RoleDescription);
		}

		if (attributes.Level is > 0)
		{
			NativeUno.uno_accessibility_update_heading_level(handle, attributes.Level.Value);
		}

		if (peer.IsPassword())
		{
			NativeUno.uno_accessibility_update_is_password(handle, true);
		}

		// Non-editable ComboBox does not expose ValuePattern in WinUI, but VoiceOver still
		// expects the current selection to be surfaced as the native accessibility value.
		if (owner is ComboBox comboBox && peer.GetPattern(PatternInterface.Value) is not IValueProvider)
		{
			var selectedValue = FrameworkElement.GetStringFromObject(comboBox.SelectionBoxItem);
			if (!string.IsNullOrEmpty(selectedValue))
			{
				NativeUno.uno_accessibility_update_value(handle, selectedValue);
			}
		}

		// Read-only state for text fields (VoiceOver uses this to determine editability)
		if (peer.GetPattern(PatternInterface.Value) is IValueProvider vp)
		{
			NativeUno.uno_accessibility_update_read_only(handle, vp.IsReadOnly);
		}

		if (attributes.Required)
		{
			NativeUno.uno_accessibility_update_required(handle, true);
		}

		// Toggle state (checkbox, radio, toggle button, switch)
		if (attributes.Checked != null)
		{
			NativeUno.uno_accessibility_update_value(handle, ConvertCheckedToNativeValue(attributes.Checked));
		}

		// Range value (slider, progress bar)
		if (attributes.ValueNow != null)
		{
			NativeUno.uno_accessibility_update_has_range_value(handle, true);
			NativeUno.uno_accessibility_update_value(handle, attributes.ValueNow.Value.ToString("G", CultureInfo.InvariantCulture));
			if (attributes.ValueMin != null && attributes.ValueMax != null)
			{
				NativeUno.uno_accessibility_update_range_bounds(handle, attributes.ValueMin.Value, attributes.ValueMax.Value);
			}
		}

		// Text value from IValueProvider (TextBox text)
		if (attributes.ValueNow == null && attributes.Checked == null &&
			peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider &&
			valueProvider.Value != null)
		{
			NativeUno.uno_accessibility_update_value(handle, valueProvider.Value);
		}

		if (attributes.Expanded != null)
		{
			NativeUno.uno_accessibility_update_expand_collapse(handle, true, attributes.Expanded.Value);
		}

		if (attributes.Selected != null)
		{
			NativeUno.uno_accessibility_update_selected(handle, attributes.Selected.Value);
		}

		if (attributes.PositionInSet is > 0)
		{
			NativeUno.uno_accessibility_update_position_in_set(handle,
				attributes.PositionInSet.Value,
				attributes.SizeOfSet ?? 0);
		}

		if (!string.IsNullOrEmpty(attributes.LandmarkRole))
		{
			NativeUno.uno_accessibility_update_landmark(handle, attributes.LandmarkRole);
		}

		if (attributes.Disabled)
		{
			NativeUno.uno_accessibility_update_enabled(handle, false);
		}
	}

	/// <summary>
	/// Converts ARIA-style checked values ("true"/"false"/"mixed") to VoiceOver-style
	/// values ("1"/"0"/"mixed") for NSAccessibilityCheckBoxRole.
	/// </summary>
	private static string ConvertCheckedToNativeValue(string ariaChecked) => ariaChecked switch
	{
		"true" => "1",
		"false" => "0",
		"mixed" => "mixed",
		_ => "0",
	};

	/// <summary>
	/// Resolves the native accessibility role string for a peer, with macOS-specific
	/// overrides for controls that need different roles than the ARIA mapper provides
	/// (e.g. ToggleSwitch → "switch", Text → "text").
	/// </summary>
	protected override string? ResolveRole(AutomationPeer peer, UIElement owner)
	{
		var controlType = peer.GetAutomationControlType();

		if (owner is ContentDialog || peer.IsDialog())
		{
			return "dialog";
		}

		if (owner is ComboBoxItem or ListBoxItem or ListViewItem or GridViewItem)
		{
			return "option";
		}

		// ToggleSwitch has AutomationControlType.Button but should be "switch" for VoiceOver
		if (controlType == AutomationControlType.Button && owner is ToggleSwitch)
		{
			return "switch";
		}

		// AutomationControlType.Text is not mapped in the shared AriaMapper (no ARIA role
		// for plain text), but macOS needs "text" for NSAccessibilityStaticTextRole
		if (controlType == AutomationControlType.Text)
		{
			return "text";
		}

		// Multi-line TextBox (AcceptsReturn=true) should be "textarea" so VoiceOver
		// announces it as "text area" instead of "text field"
		if (controlType == AutomationControlType.Edit && owner is TextBox textBox && textBox.AcceptsReturn)
		{
			return "textarea";
		}

		return base.ResolveRole(peer, owner);
	}

	/// <summary>
	/// Resolves a label for an automation peer with macOS-specific fallbacks.
	/// Extends the shared mapper with Header/PlaceholderText fallbacks used by VoiceOver.
	/// </summary>
	private new static string? ResolveLabel(AutomationPeer peer)
	{
		var label = AriaMapper.ResolveLabel(peer);
		if (!string.IsNullOrEmpty(label))
		{
			return label;
		}

		if (peer is FrameworkElementAutomationPeer frameworkPeer)
		{
			var owner = frameworkPeer.Owner;

			if (owner is TextBox textBox)
			{
				var header = textBox.Header?.ToString();
				if (!string.IsNullOrEmpty(header))
				{
					return header;
				}

				if (!string.IsNullOrEmpty(textBox.PlaceholderText))
				{
					return textBox.PlaceholderText;
				}
			}
			else if (owner is PasswordBox passwordBox)
			{
				var header = passwordBox.Header?.ToString();
				if (!string.IsNullOrEmpty(header))
				{
					return header;
				}

				if (!string.IsNullOrEmpty(passwordBox.PlaceholderText))
				{
					return passwordBox.PlaceholderText;
				}
			}
			else if (owner is ComboBox comboBox)
			{
				var header = comboBox.Header?.ToString();
				if (!string.IsNullOrEmpty(header))
				{
					return header;
				}
			}
		}

		return label;
	}

	// Platform-specific property update implementations (called from SkiaAccessibilityBase routing)

	protected override void UpdateName(nint handle, AutomationPeer peer, string? label)
		=> NativeUno.uno_accessibility_update_label(handle, label);

	protected override void UpdateToggleState(nint handle, AutomationPeer peer, ToggleState newState)
	{
		var ariaChecked = AriaMapper.ConvertToggleStateToAriaChecked(newState);
		NativeUno.uno_accessibility_update_value(handle, ConvertCheckedToNativeValue(ariaChecked));
	}

	protected override void UpdateRangeValue(nint handle, AutomationPeer peer, double value)
		=> NativeUno.uno_accessibility_update_value(handle, value.ToString("G", CultureInfo.InvariantCulture));

	protected override void UpdateRangeBounds(nint handle, double min, double max)
		=> NativeUno.uno_accessibility_update_range_bounds(handle, min, max);

	protected override void UpdateTextValue(nint handle, string? value)
		=> NativeUno.uno_accessibility_update_value(handle, value);

	protected override void UpdateExpandCollapseState(nint handle, bool isExpanded)
		=> NativeUno.uno_accessibility_update_expand_collapse(handle, true, isExpanded);

	protected override void UpdateEnabled(nint handle, bool enabled)
		=> NativeUno.uno_accessibility_update_enabled(handle, enabled);

	protected override void UpdateSelected(nint handle, bool selected)
		=> NativeUno.uno_accessibility_update_selected(handle, selected);

	protected override void UpdateHelpText(nint handle, string? helpText)
		=> NativeUno.uno_accessibility_update_help(handle, helpText);

	protected override void UpdateHeadingLevel(nint handle, int level)
		=> NativeUno.uno_accessibility_update_heading_level(handle, level);

	protected override void UpdateLandmark(nint handle, string? landmarkRole)
		=> NativeUno.uno_accessibility_update_landmark(handle, landmarkRole);

	protected override void UpdateIsReadOnly(nint handle, bool isReadOnly)
		=> NativeUno.uno_accessibility_update_read_only(handle, isReadOnly);

	protected override void UpdateFocusable(nint handle, bool focusable)
		=> NativeUno.uno_accessibility_update_focusable(handle, focusable);

	protected override void UpdateIsOffscreen(nint handle, bool isOffscreen)
		=> NativeUno.uno_accessibility_update_visibility(handle, !isOffscreen);

	// Override event routing from base to add macOS-specific events
	public override void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (!_accessibilityTreeInitialized)
		{
			return;
		}

		// Let base handle common events (focus, text changes, structure)
		base.NotifyAutomationEvent(peer, eventId);

		// macOS-specific event handling
		switch (eventId)
		{
			case AutomationEvents.InvokePatternOnInvoked when TryGetPeerOwner(peer, out _):
			case AutomationEvents.SelectionItemPatternOnElementSelected when TryGetPeerOwner(peer, out _):
			case AutomationEvents.SelectionPatternOnInvalidated:
				NativeUno.uno_accessibility_post_layout_changed();
				break;

			case AutomationEvents.TextPatternOnTextSelectionChanged when TryGetPeerOwner(peer, out var textElement):
				if (textElement is TextBox textBox)
				{
					NativeUno.uno_accessibility_update_selection(
						textElement.Visual.Handle,
						textBox.SelectionStart,
						textBox.SelectionLength);
				}
				break;

			case AutomationEvents.LiveRegionChanged when TryGetPeerOwner(peer, out var liveElement):
				if (_activeModalHandle != nint.Zero && !IsDescendantOf(liveElement, _activeModalHandle))
				{
					break;
				}

				NativeUno.uno_accessibility_post_live_region_changed(liveElement.Visual.Handle);

				var label = ResolveLabel(peer);
				if (!string.IsNullOrEmpty(label))
				{
					var liveSetting = AutomationProperties.GetLiveSetting(liveElement);
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

	protected override void SetNativeFocus(nint handle)
		=> NativeUno.uno_accessibility_set_focused(handle);

	protected override void OnNativeStructureChanged()
		=> NativeUno.uno_accessibility_post_children_changed();

	protected override void AnnounceOnPlatform(string text, bool assertive)
		=> NativeUno.uno_accessibility_announce(text, assertive);

	// Modal dialog support: ContentDialog gets modal focus trapping for VoiceOver.

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

				// Remember what had focus before the dialog opened
				_modalTriggerHandle = TrackedFocusedElement?.Visual.Handle ?? nint.Zero;
				_activeModalHandle = dialog.Visual.Handle;

				// ContentDialog does not currently provide a dedicated dialog peer on Uno,
				// so ensure the native accessibility node keeps explicit dialog semantics.
				NativeUno.uno_accessibility_update_role(dialog.Visual.Handle, "dialog");

				// Mark the dialog as modal so VoiceOver restricts navigation
				NativeUno.uno_accessibility_update_modal(dialog.Visual.Handle, true);

				// Announce the dialog title so VoiceOver users know a dialog appeared
				var dialogPeer = dialog.GetOrCreateAutomationPeer();
				var dialogTitle = dialogPeer?.GetName();
				if (!string.IsNullOrEmpty(dialogTitle))
				{
					AnnounceAssertive(dialogTitle);
				}

			};

			dialog.Closed += (s, e) =>
			{
				if (!IsAccessibilityEnabled)
				{
					return;
				}

				// Remove modal flag
				NativeUno.uno_accessibility_update_modal(dialog.Visual.Handle, false);

				// Restore focus to the element that triggered the dialog,
				// or fall back to the root element if nothing was focused before.
				if (_modalTriggerHandle != nint.Zero)
				{
					NativeUno.uno_accessibility_set_focused(_modalTriggerHandle);
				}
				else
				{
					// No trigger element — try focusing the root element
					var rootElement = Microsoft.UI.Xaml.Window.CurrentSafe?.RootElement;
					if (rootElement is not null)
					{
						var firstFocusable = FindFirstFocusableChild(rootElement);
						if (firstFocusable is not null)
						{
							NativeUno.uno_accessibility_set_focused(firstFocusable.Visual.Handle);
						}
					}
				}

				_activeModalHandle = nint.Zero;
				_modalTriggerHandle = nint.Zero;

				// Clear duplicate tracking so the next announcement always goes through
				ResetAnnouncementTracking();
			};
		}
	}
}
