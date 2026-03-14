#nullable enable

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
/// mirrors the XAML visual tree, similar to:
/// - Flutter's AccessibilityBridgeMac (Chromium AXTree → NSAccessibility)
/// - Uno WASM's WebAssemblyAccessibility (parallel DOM tree with ARIA attributes)
///
/// Uses the shared <see cref="AriaMapper"/> to resolve ARIA attributes from
/// automation peers, with macOS-specific attribute resolution for VoiceOver.
/// </summary>
internal class MacOSAccessibility : IUnoAccessibility, IAutomationPeerListener
{
	private static MacOSAccessibility? _instance;
	private nint _windowHandle;
	private bool _accessibilityTreeInitialized;

	internal static MacOSAccessibility Instance => _instance ??= new MacOSAccessibility();

	private MacOSAccessibility()
	{
		if (this.Log().IsEnabled(LogLevel.Trace))
		{
			this.Log().Trace($"Initializing {nameof(MacOSAccessibility)}");
		}

		AccessibilityAnnouncer.AccessibilityImpl = this;
		UIElementAccessibilityHelper.ExternalOnChildAdded = OnChildAdded;
		UIElementAccessibilityHelper.ExternalOnChildRemoved = OnChildRemoved;
		VisualAccessibilityHelper.ExternalOnVisualOffsetOrSizeChanged = OnSizeOrOffsetChanged;
		AutomationPeer.AutomationPeerListener = this;
	}

	internal static unsafe void Register()
	{
		_ = Instance;

		NativeUno.uno_accessibility_set_callbacks(&OnNativeInvoke, &OnNativeFocus);
		NativeUno.uno_accessibility_set_range_callbacks(&OnNativeIncrement, &OnNativeDecrement);
		NativeUno.uno_accessibility_set_expand_collapse_callback(&OnNativeExpandCollapse);

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

	public bool IsAccessibilityEnabled => _windowHandle != nint.Zero;

	internal void Initialize(nint windowHandle)
	{
		_windowHandle = windowHandle;
		NativeUno.uno_accessibility_init(windowHandle);

		if (this.Log().IsEnabled(LogLevel.Debug))
		{
			this.Log().Debug($"MacOSAccessibility initialized for window 0x{windowHandle:X}");
		}
	}

	private void OnNativeWindowReady(object? sender, MacOSWindowNative nativeWindow) =>
		_ = nativeWindow.Host.RootElement?.Dispatcher.RunAsync(
			Windows.UI.Core.CoreDispatcherPriority.Low,
			() => TryInitializeAccessibilityTree(nativeWindow.Host.RootElement));

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

		CreateAOM(rootElement);
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

	private void OnChildAdded(UIElement parent, UIElement child, int? index)
	{
		if (IsAccessibilityEnabled && _accessibilityTreeInitialized)
		{
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

	private void OnChildRemoved(UIElement parent, UIElement child)
	{
		if (IsAccessibilityEnabled && _accessibilityTreeInitialized)
		{
			NativeUno.uno_accessibility_remove_element(parent.Visual.Handle, child.Visual.Handle);
		}
	}

	private void OnSizeOrOffsetChanged(Visual visual)
	{
		if (IsAccessibilityEnabled && _accessibilityTreeInitialized
			&& visual is ContainerVisual containerVisual
			&& containerVisual.Owner?.Target is UIElement)
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

	private static bool IsAccessibilityFocusable(DependencyObject dependencyObject, bool isFocusable)
	{
		if (!isFocusable && dependencyObject is not (TextBlock or RichTextBlock))
		{
			return false;
		}

		var accessibilityView = AutomationProperties.GetAccessibilityView(dependencyObject);
		if (accessibilityView == AccessibilityView.Raw)
		{
			return false;
		}

		if ((dependencyObject as UIElement)?.GetOrCreateAutomationPeer() is null)
		{
			return false;
		}

		return true;
	}

	/// <summary>
	/// Creates the Accessibility Object Model (AOM) from the root element.
	/// This mirrors the WASM implementation's CreateAOM approach.
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

		// Apply attributes to the native element
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
			// Still process children, but parent them to the Raw element's parent
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
	/// Adds a single accessibility element. This is the macOS equivalent of
	/// the WASM AddSemanticElement method.
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
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Failed to resolve role for {child}: {e.Message}");
				}
			}

			try
			{
				label = ResolveLabel(peer);
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Failed to resolve label for {child}: {e.Message}");
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

		// Apply attributes to the native element
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

		// Set initial enabled state
		if (child is Control control)
		{
			NativeUno.uno_accessibility_update_enabled(child.Visual.Handle, control.IsEnabled);
		}

		// Fire AXLiveRegionCreated for live region elements (matching Flutter's
		// AccessibilityBridgeMac LIVE_REGION_CREATED notification)
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
	/// Applies accessibility attributes to the native element, combining shared
	/// AriaMapper attributes with macOS-specific resolution (VoiceOver needs).
	/// </summary>
	private static void ApplyAttributes(nint handle, AutomationPeer peer, UIElement? owner = null)
	{
		var attributes = AriaMapper.GetAriaAttributes(peer);

		// FullDescription → accessibilityHelp (VoiceOver reads this as secondary context).
		// HelpText → accessibilityHelp (fallback when no FullDescription).
		// These are separate from the short description/placeholder.
		var fullDescription = peer.GetFullDescription();
		var helpText = peer.GetHelpText();
		if (!string.IsNullOrEmpty(fullDescription))
		{
			NativeUno.uno_accessibility_update_help(handle, fullDescription);
		}
		else if (!string.IsNullOrEmpty(helpText))
		{
			NativeUno.uno_accessibility_update_help(handle, helpText);
		}

		// PlaceholderText as description when Header is set (macOS-specific label enrichment).
		// VoiceOver reads this as additional context for the text field.
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

		// Role description (custom landmark description)
		if (!string.IsNullOrEmpty(attributes.RoleDescription))
		{
			NativeUno.uno_accessibility_update_role_description(handle, attributes.RoleDescription);
		}

		// Heading level
		if (attributes.Level is > 0)
		{
			NativeUno.uno_accessibility_update_heading_level(handle, attributes.Level.Value);
		}

		// Password (resolved directly from peer)
		if (peer.IsPassword())
		{
			NativeUno.uno_accessibility_update_is_password(handle, true);
		}

		// Required (resolved directly from peer)
		if (peer.IsRequiredForForm())
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

		// Expand/Collapse
		if (attributes.Expanded != null)
		{
			NativeUno.uno_accessibility_update_expand_collapse(handle, true, attributes.Expanded.Value);
		}

		// Selected
		if (attributes.Selected != null)
		{
			NativeUno.uno_accessibility_update_selected(handle, attributes.Selected.Value);
		}

		// Position in set
		if (attributes.PositionInSet is > 0)
		{
			NativeUno.uno_accessibility_update_position_in_set(handle,
				attributes.PositionInSet.Value,
				attributes.SizeOfSet ?? 0);
		}

		// Landmark
		if (!string.IsNullOrEmpty(attributes.LandmarkRole))
		{
			NativeUno.uno_accessibility_update_landmark(handle, attributes.LandmarkRole);
		}

		// Disabled state
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
	private static string? ResolveRole(AutomationPeer peer, UIElement owner)
	{
		var controlType = peer.GetAutomationControlType();

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

		return AriaMapper.GetAriaRole(controlType);
	}

	/// <summary>
	/// Resolves a label for an automation peer with macOS-specific fallbacks.
	/// Extends the shared AriaMapper.ResolveLabel with TextBox/PasswordBox Header
	/// and PlaceholderText, and ComboBox Header fallbacks for VoiceOver.
	/// </summary>
	private static string? ResolveLabel(AutomationPeer peer)
	{
		var label = AriaMapper.ResolveLabel(peer);
		if (!string.IsNullOrEmpty(label))
		{
			return label;
		}

		// macOS-specific: TextBox/PasswordBox/ComboBox header/placeholder fallbacks
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

	// IUnoAccessibility

	public void AnnouncePolite(string text)
		=> NativeUno.uno_accessibility_announce(text, assertive: false);

	public void AnnounceAssertive(string text)
		=> NativeUno.uno_accessibility_announce(text, assertive: true);

	// IAutomationPeerListener

	public void NotifyPropertyChangedEvent(AutomationPeer peer, AutomationProperty automationProperty, object oldValue, object newValue)
	{
		if (!IsAccessibilityEnabled || !_accessibilityTreeInitialized)
		{
			return;
		}

		if (automationProperty == AutomationElementIdentifiers.NameProperty &&
			TryGetPeerOwner(peer, out var element))
		{
			var label = ResolveLabel(peer);
			NativeUno.uno_accessibility_update_label(element.Visual.Handle, label ?? (string)newValue);
		}
		else if (automationProperty == TogglePatternIdentifiers.ToggleStateProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var ariaChecked = AriaMapper.ConvertToggleStateToAriaChecked((ToggleState)newValue);
			NativeUno.uno_accessibility_update_value(element.Visual.Handle, ConvertCheckedToNativeValue(ariaChecked));
		}
		else if (automationProperty == RangeValuePatternIdentifiers.ValueProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (newValue is double doubleValue)
			{
				NativeUno.uno_accessibility_update_value(element.Visual.Handle, doubleValue.ToString("G", CultureInfo.InvariantCulture));
			}
		}
		else if (automationProperty == ValuePatternIdentifiers.ValueProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeUno.uno_accessibility_update_value(element.Visual.Handle, newValue as string);
		}
		else if (automationProperty == ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var isExpanded = newValue is ExpandCollapseState state && state == ExpandCollapseState.Expanded;
			NativeUno.uno_accessibility_update_expand_collapse(element.Visual.Handle, true, isExpanded);
		}
		else if (automationProperty == AutomationElementIdentifiers.IsEnabledProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeUno.uno_accessibility_update_enabled(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.HelpTextProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeUno.uno_accessibility_update_help(element.Visual.Handle, newValue as string);
		}
		else if (automationProperty == AutomationElementIdentifiers.HeadingLevelProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var level = newValue is AutomationHeadingLevel headingLevel ? headingLevel switch
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
			NativeUno.uno_accessibility_update_heading_level(element.Visual.Handle, level);
		}
		else if (automationProperty == SelectionItemPatternIdentifiers.IsSelectedProperty &&
			TryGetPeerOwner(peer, out element))
		{
			NativeUno.uno_accessibility_update_selected(element.Visual.Handle, newValue is true);
		}
		else if (automationProperty == AutomationElementIdentifiers.LandmarkTypeProperty &&
			TryGetPeerOwner(peer, out element))
		{
			var landmarkType = newValue is AutomationLandmarkType lt ? lt : AutomationLandmarkType.None;
			var landmarkRole = AriaMapper.GetLandmarkRole(landmarkType);
			NativeUno.uno_accessibility_update_landmark(element.Visual.Handle, landmarkRole);
		}
		else if (automationProperty == RangeValuePatternIdentifiers.MinimumProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (newValue is double min &&
				peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				NativeUno.uno_accessibility_update_range_bounds(element.Visual.Handle, min, rangeProvider.Maximum);
			}
		}
		else if (automationProperty == RangeValuePatternIdentifiers.MaximumProperty &&
			TryGetPeerOwner(peer, out element))
		{
			if (newValue is double max &&
				peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
			{
				NativeUno.uno_accessibility_update_range_bounds(element.Visual.Handle, rangeProvider.Minimum, max);
			}
		}
	}

	public void NotifyAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		if (!IsAccessibilityEnabled || !_accessibilityTreeInitialized)
		{
			return;
		}

		switch (eventId)
		{
			case AutomationEvents.AutomationFocusChanged when TryGetPeerOwner(peer, out var focusedElement):
				NativeUno.uno_accessibility_set_focused(focusedElement.Visual.Handle);
				break;

			case AutomationEvents.InvokePatternOnInvoked when TryGetPeerOwner(peer, out _):
				NativeUno.uno_accessibility_post_layout_changed();
				break;

			case AutomationEvents.SelectionItemPatternOnElementSelected when TryGetPeerOwner(peer, out _):
				NativeUno.uno_accessibility_post_layout_changed();
				break;

			case AutomationEvents.SelectionPatternOnInvalidated:
				NativeUno.uno_accessibility_post_layout_changed();
				break;

			case AutomationEvents.TextPatternOnTextSelectionChanged:
				break;

			case AutomationEvents.LiveRegionChanged when TryGetPeerOwner(peer, out var liveElement):
				// Fire native VoiceOver live region notifications (matching Flutter's
				// AccessibilityBridgeMac LIVE_REGION_CHANGED / ALERT pattern)
				NativeUno.uno_accessibility_post_live_region_changed(liveElement.Visual.Handle);

				// Also use announcement API as a fallback for VoiceOver to speak the text
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

			case AutomationEvents.StructureChanged:
				// Match Flutter's AccessibilityBridgeMac CHILDREN_CHANGED:
				// NSAccessibilityCreatedNotification on the window is the only way
				// to make VoiceOver reliably pick up structural changes.
				NativeUno.uno_accessibility_post_children_changed();
				break;
		}
	}

	public void NotifyNotificationEvent(AutomationPeer peer, AutomationNotificationKind notificationKind, AutomationNotificationProcessing notificationProcessing, string displayString, string activityId)
	{
		if (!IsAccessibilityEnabled || string.IsNullOrEmpty(displayString))
		{
			return;
		}

		var assertive = notificationProcessing == AutomationNotificationProcessing.ImportantAll ||
						notificationProcessing == AutomationNotificationProcessing.ImportantMostRecent;

		NativeUno.uno_accessibility_announce(displayString, assertive);
	}

	public bool ListenerExistsHelper(AutomationEvents eventId)
		=> IsAccessibilityEnabled;

	public void OnAutomationEvent(AutomationPeer peer, AutomationEvents eventId)
	{
		// Forward to NotifyAutomationEvent to keep macOS handling unified
		NotifyAutomationEvent(peer, eventId);
	}

	private static bool TryGetPeerOwner(AutomationPeer peer, [NotNullWhen(true)] out UIElement? owner)
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
}
