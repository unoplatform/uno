#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Maps automation peers to ARIA attributes and semantic element types.
/// </summary>
public static class AriaMapper
{
	/// <summary>
	/// Maps AutomationControlType to ARIA role.
	/// </summary>
	private static readonly Dictionary<AutomationControlType, string> ControlTypeToRoleMap = new()
	{
		{ AutomationControlType.Button, "button" },
		{ AutomationControlType.CheckBox, "checkbox" },
		{ AutomationControlType.RadioButton, "radio" },
		{ AutomationControlType.Slider, "slider" },
		{ AutomationControlType.Edit, "textbox" },
		{ AutomationControlType.ComboBox, "combobox" },
		{ AutomationControlType.List, "listbox" },
		{ AutomationControlType.ListItem, "option" },
		{ AutomationControlType.Menu, "menu" },
		{ AutomationControlType.MenuItem, "menuitem" },
		{ AutomationControlType.Tab, "tablist" },
		{ AutomationControlType.TabItem, "tab" },
		{ AutomationControlType.Tree, "tree" },
		{ AutomationControlType.TreeItem, "treeitem" },
		{ AutomationControlType.ProgressBar, "progressbar" },
		{ AutomationControlType.ScrollBar, "scrollbar" },
		// Note: AutomationControlType.Text intentionally omitted.
		// The ARIA "label" role is for labeling form elements, not displaying text.
		// Plain text should have no explicit ARIA role (use aria-label instead).
		{ AutomationControlType.Hyperlink, "link" },
		{ AutomationControlType.Image, "img" },
		{ AutomationControlType.Group, "group" },
		{ AutomationControlType.Header, "heading" },
		{ AutomationControlType.ToolTip, "tooltip" },
		{ AutomationControlType.DataGrid, "grid" },
		{ AutomationControlType.DataItem, "row" },
		{ AutomationControlType.Document, "document" },
		{ AutomationControlType.Window, "dialog" },
		{ AutomationControlType.Pane, "region" },
		{ AutomationControlType.Spinner, "spinbutton" },
		{ AutomationControlType.StatusBar, "status" },
		{ AutomationControlType.Thumb, "slider" },
		{ AutomationControlType.ToolBar, "toolbar" },
		{ AutomationControlType.Custom, "generic" },
	};

	/// <summary>
	/// Gets the ARIA role for an automation control type.
	/// </summary>
	/// <param name="controlType">The automation control type.</param>
	/// <returns>The corresponding ARIA role, or null if no mapping exists.</returns>
	public static string? GetAriaRole(AutomationControlType controlType)
	{
		return ControlTypeToRoleMap.TryGetValue(controlType, out var role) ? role : null;
	}

	/// <summary>
	/// Gets the semantic element type for an automation peer based on its control type and patterns.
	/// </summary>
	/// <param name="peer">The automation peer.</param>
	/// <param name="owner">The optional owner UIElement (used for additional context).</param>
	/// <returns>The semantic element type to create.</returns>
	public static SemanticElementType GetSemanticElementType(AutomationPeer peer, UIElement? owner = null)
	{
		if (peer.GetHeadingLevel() != AutomationHeadingLevel.None)
		{
			return SemanticElementType.Heading;
		}

		var controlType = peer.GetAutomationControlType();

		return controlType switch
		{
			AutomationControlType.Button => GetButtonType(peer),
			AutomationControlType.CheckBox => SemanticElementType.Checkbox,
			AutomationControlType.RadioButton => SemanticElementType.RadioButton,
			AutomationControlType.Slider => SemanticElementType.Slider,
			AutomationControlType.Edit => GetTextBoxType(peer),
			AutomationControlType.ComboBox => SemanticElementType.ComboBox,
			AutomationControlType.List => SemanticElementType.ListBox,
			AutomationControlType.ListItem => SemanticElementType.ListItem,
			AutomationControlType.Hyperlink => SemanticElementType.Link,
			AutomationControlType.Header => SemanticElementType.Heading,
			AutomationControlType.Tab => SemanticElementType.TabList,
			AutomationControlType.TabItem => SemanticElementType.Tab,
			AutomationControlType.Tree => SemanticElementType.Tree,
			AutomationControlType.TreeItem => SemanticElementType.TreeItem,
			AutomationControlType.DataGrid => SemanticElementType.Grid,
			AutomationControlType.DataItem => SemanticElementType.GridRow,
			AutomationControlType.Menu => SemanticElementType.Menu,
			AutomationControlType.MenuItem => SemanticElementType.MenuItem,
			_ => SemanticElementType.Generic
		};
	}

	private static SemanticElementType GetButtonType(AutomationPeer peer)
	{
		if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider)
		{
			// ToggleSwitch maps to the ARIA switch pattern (role="switch" + aria-checked)
			// rather than toggle button (aria-pressed), so VoiceOver announces "On"/"Off".
			if (peer is ToggleSwitchAutomationPeer)
			{
				return SemanticElementType.Switch;
			}

			return SemanticElementType.ToggleButton;
		}

		return SemanticElementType.Button;
	}

	/// <summary>
	/// Determines the appropriate textbox type (text, textarea, or password).
	/// </summary>
	private static SemanticElementType GetTextBoxType(AutomationPeer peer)
	{
		if (peer.IsPassword())
		{
			return SemanticElementType.Password;
		}

		if (peer is FrameworkElementAutomationPeer frameworkPeer &&
			frameworkPeer.Owner is Microsoft.UI.Xaml.Controls.TextBox textBox)
		{
			if (textBox.AcceptsReturn)
			{
				return SemanticElementType.TextArea;
			}
		}

		return SemanticElementType.TextBox;
	}

	/// <summary>
	/// Gets the ARIA attributes for an automation peer.
	/// </summary>
	/// <param name="peer">The automation peer.</param>
	/// <returns>A dictionary of ARIA attribute names to values.</returns>
	public static AriaAttributes GetAriaAttributes(AutomationPeer peer)
	{
		var controlType = peer.GetAutomationControlType();
		var attributes = new AriaAttributes
		{
			Role = GetAriaRole(controlType),
			Label = ResolveLabel(peer),
			Disabled = !peer.IsEnabled(),
		};

		var fullDescription = peer.GetFullDescription();
		// HelpText is used as a fallback because VoiceOver reads it as secondary context
		var helpText = peer.GetHelpText();
		if (!string.IsNullOrEmpty(fullDescription))
		{
			attributes.Description = fullDescription;
		}
		else if (!string.IsNullOrEmpty(helpText))
		{
			attributes.Description = helpText;
		}

		if (peer.IsRequiredForForm())
		{
			attributes.Required = true;
		}

		// VoiceOver uses landmark roles for rotor navigation
		var landmarkType = peer.GetLandmarkType();
		if (landmarkType != AutomationLandmarkType.None)
		{
			attributes.LandmarkRole = GetLandmarkRole(landmarkType);

			if (landmarkType == AutomationLandmarkType.Custom)
			{
				var localizedLandmarkType = peer.GetLocalizedLandmarkType();
				if (!string.IsNullOrEmpty(localizedLandmarkType))
				{
					attributes.RoleDescription = localizedLandmarkType;
				}
			}
		}

		var positionInSet = peer.GetPositionInSet();
		if (positionInSet > 0)
		{
			attributes.PositionInSet = positionInSet;
		}

		var sizeOfSet = peer.GetSizeOfSet();
		if (sizeOfSet > 0)
		{
			attributes.SizeOfSet = sizeOfSet;
		}

		var headingLevel = peer.GetHeadingLevel();
		if (headingLevel != AutomationHeadingLevel.None)
		{
			attributes.Level = (int)headingLevel;
		}

		// Pattern queries are wrapped in try-catch because some peers (e.g.,
		// CalendarViewBaseItemAutomationPeer) may throw NRE if queried before
		// they are fully initialized in the visual tree.
		try
		{
			if (peer.GetPattern(PatternInterface.Toggle) is IToggleProvider toggleProvider)
			{
				attributes.Checked = ConvertToggleStateToAriaChecked(toggleProvider.ToggleState);
			}

			if (peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider expandCollapseProvider)
			{
				attributes.Expanded = expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.Expanded ||
									  expandCollapseProvider.ExpandCollapseState == ExpandCollapseState.PartiallyExpanded;

				if (controlType == AutomationControlType.ComboBox)
				{
					attributes.HasPopup = "listbox";
				}
				else if (controlType == AutomationControlType.Menu || controlType == AutomationControlType.MenuItem)
				{
					attributes.HasPopup = "menu";
				}
			}

			if (peer.GetPattern(PatternInterface.Selection) is ISelectionProvider selectionProvider)
			{
				attributes.MultiSelectable = selectionProvider.CanSelectMultiple;
			}

			if (peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider selectionItemProvider)
			{
				attributes.Selected = selectionItemProvider.IsSelected;
			}
			else if (peer is FrameworkElementAutomationPeer { Owner: SelectorItem selectorItem })
			{
				// ListViewItemAutomationPeer and ListBoxItemAutomationPeer are container
				// peers that don't implement ISelectionItemProvider (that's on the data
				// peer SelectorItemAutomationPeer). Fall back to SelectorItem.IsSelected
				// so the initial aria-selected state is correct for list items.
				attributes.Selected = selectorItem.IsSelected;
			}

			if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValueProvider)
			{
				attributes.ValueNow = rangeValueProvider.Value;
				attributes.ValueMin = rangeValueProvider.Minimum;
				attributes.ValueMax = rangeValueProvider.Maximum;

				// aria-valuetext for VoiceOver: read human-friendly text instead of raw number
				// Use the automation name if it contains the value context (e.g., "Volume: 50%")
				if (peer is FrameworkElementAutomationPeer frameworkPeerRange &&
					frameworkPeerRange.Owner is Slider slider)
				{
					var headerText = slider.Header?.ToString();
					if (!string.IsNullOrEmpty(headerText))
					{
						attributes.ValueText = $"{headerText}: {rangeValueProvider.Value}";
					}
				}
			}
		}
		catch
		{
			// Some peers may throw if queried before fully initialized.
			// Attributes will be updated later when properties change.
		}

		return attributes;
	}

	/// <summary>
	/// Converts a ToggleState to an ARIA checked value.
	/// </summary>
	/// <param name="state">The toggle state.</param>
	/// <returns>The ARIA checked value ("true", "false", or "mixed").</returns>
	public static string ConvertToggleStateToAriaChecked(ToggleState state)
	{
		return state switch
		{
			ToggleState.On => "true",
			ToggleState.Off => "false",
			ToggleState.Indeterminate => "mixed",
			_ => "false"
		};
	}

	/// <summary>
	/// Resolves a label for an automation peer using a fallback chain aligned with WinUI's
	/// <c>FrameworkElementAutomationPeer::GetNameCore()</c> while preserving Skia-specific fallbacks:
	/// 1. <c>peer.GetName()</c>
	/// 2. Owner-side <c>AutomationProperties.LabeledBy</c> / <c>AutomationProperties.Name</c>
	/// 3. <c>Content.ToString()</c> for content controls
	/// 4. First child <see cref="TextBlock"/> text
	/// </summary>
	public static string? ResolveLabel(AutomationPeer peer)
	{
		try
		{
			// 1. Standard automation peer name — this already resolves:
			//    - AutomationProperties.Name
			//    - LabeledBy → peer.GetName()
			//    - GetPlainText / GetAccessibilityInnerText
			// Matches WinUI's GetNameCore resolution chain.
			var name = peer.GetName();
			if (!string.IsNullOrEmpty(name))
			{
				return name;
			}

			// Try to get the owner element for additional fallbacks.
			if (peer is not FrameworkElementAutomationPeer frameworkPeer)
			{
				return name;
			}

			var owner = frameworkPeer.Owner;

			// 2. Explicit owner-side fallback for LabeledBy / Name.
			// In most cases peer.GetName() already covers these, but some peers can still
			// surface an empty name while the owner metadata is available.
			var labeledBy = AutomationProperties.GetLabeledBy(owner);
			if (labeledBy is TextBlock labelTextBlock && !string.IsNullOrEmpty(labelTextBlock.Text))
			{
				return labelTextBlock.Text;
			}
			else if (labeledBy is UIElement labelElement)
			{
				var labelPeer = labelElement.GetOrCreateAutomationPeer();
				var labelName = labelPeer?.GetName();
				if (!string.IsNullOrEmpty(labelName))
				{
					return labelName;
				}
			}

			var automationName = AutomationProperties.GetName(owner);
			if (!string.IsNullOrEmpty(automationName))
			{
				return automationName;
			}

			// 3. For ContentControls, try Content.ToString()
			// This is a Skia-specific enhancement for buttons with non-text content
			if (owner is ContentControl contentControl && contentControl.Content is not null)
			{
				var contentString = contentControl.Content switch
				{
					string s => s,
					// Avoid calling ToString() on UIElement / complex objects
					UIElement => null,
					_ => contentControl.Content.ToString()
				};
				if (!string.IsNullOrEmpty(contentString))
				{
					return contentString;
				}
			}

			// 4. Walk immediate children looking for a TextBlock with text
			// This is a Skia-specific enhancement for controls with text children
			if (owner is UIElement uiElement)
			{
				foreach (var child in uiElement.GetChildren())
				{
					if (child is TextBlock textBlock && !string.IsNullOrEmpty(textBlock.Text))
					{
						return textBlock.Text;
					}
				}
			}

			return name; // May still be empty, but we tried
		}
		catch (Exception ex)
		{
			// Some peers may throw if queried before fully initialized.
			// Return empty rather than crashing the accessibility system.
			System.Diagnostics.Debug.WriteLine($"[A11y] AriaMapper.ResolveLabel failed for {peer.GetType().Name}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Maps AutomationLandmarkType to ARIA landmark role.
	/// VoiceOver uses landmarks for rotor navigation.
	/// </summary>
	public static string? GetLandmarkRole(AutomationLandmarkType landmarkType)
	{
		return landmarkType switch
		{
			AutomationLandmarkType.Main => "main",
			AutomationLandmarkType.Navigation => "navigation",
			AutomationLandmarkType.Search => "search",
			AutomationLandmarkType.Form => "form",
			AutomationLandmarkType.Custom => "region",
			_ => null
		};
	}

	/// <summary>
	/// Gets the pattern capabilities for an automation peer.
	/// </summary>
	/// <param name="peer">The automation peer.</param>
	/// <returns>The pattern capabilities.</returns>
	public static PatternCapabilities GetPatternCapabilities(AutomationPeer peer)
	{
		return new PatternCapabilities
		{
			CanInvoke = peer.GetPattern(PatternInterface.Invoke) is IInvokeProvider,
			CanToggle = peer.GetPattern(PatternInterface.Toggle) is IToggleProvider,
			CanRangeValue = peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider,
			CanValue = peer.GetPattern(PatternInterface.Value) is IValueProvider,
			CanExpandCollapse = peer.GetPattern(PatternInterface.ExpandCollapse) is IExpandCollapseProvider,
			CanSelect = peer.GetPattern(PatternInterface.SelectionItem) is ISelectionItemProvider,
			CanScroll = peer.GetPattern(PatternInterface.Scroll) is IScrollProvider
		};
	}
}

/// <summary>
/// Type of semantic HTML element to create.
/// </summary>
public enum SemanticElementType
{
	/// <summary>div with ARIA role</summary>
	Generic,
	/// <summary>button element</summary>
	Button,
	/// <summary>heading element (h1-h6)</summary>
	Heading,
	/// <summary>input type="checkbox"</summary>
	Checkbox,
	/// <summary>input type="radio"</summary>
	RadioButton,
	/// <summary>input type="range"</summary>
	Slider,
	/// <summary>input type="text"</summary>
	TextBox,
	/// <summary>textarea element</summary>
	TextArea,
	/// <summary>input type="password"</summary>
	Password,
	/// <summary>div with role="combobox"</summary>
	ComboBox,
	/// <summary>div with role="listbox"</summary>
	ListBox,
	/// <summary>div with role="option"</summary>
	ListItem,
	/// <summary>anchor element</summary>
	Link,
	Switch,
	ToggleButton,
	/// <summary>div with role="tablist"</summary>
	TabList,
	/// <summary>div with role="tab"</summary>
	Tab,
	/// <summary>div with role="tree"</summary>
	Tree,
	/// <summary>div with role="treeitem"</summary>
	TreeItem,
	/// <summary>div with role="grid"</summary>
	Grid,
	/// <summary>div with role="row"</summary>
	GridRow,
	/// <summary>div with role="gridcell"</summary>
	GridCell,
	/// <summary>div with role="columnheader"</summary>
	ColumnHeader,
	/// <summary>div with role="menu"</summary>
	Menu,
	/// <summary>div with role="menuitem"</summary>
	MenuItem
}

/// <summary>
/// Collection of ARIA attributes for a semantic element.
/// </summary>
public class AriaAttributes
{
	/// <summary>ARIA role</summary>
	public string? Role { get; set; }

	/// <summary>aria-label</summary>
	public string? Label { get; set; }

	/// <summary>aria-checked (true/false/mixed)</summary>
	public string? Checked { get; set; }

	/// <summary>aria-expanded</summary>
	public bool? Expanded { get; set; }

	/// <summary>aria-selected</summary>
	public bool? Selected { get; set; }

	/// <summary>aria-disabled</summary>
	public bool Disabled { get; set; }

	/// <summary>aria-required</summary>
	public bool Required { get; set; }

	/// <summary>aria-valuenow</summary>
	public double? ValueNow { get; set; }

	/// <summary>aria-valuemin</summary>
	public double? ValueMin { get; set; }

	/// <summary>aria-valuemax</summary>
	public double? ValueMax { get; set; }

	/// <summary>aria-posinset</summary>
	public int? PositionInSet { get; set; }

	/// <summary>aria-setsize</summary>
	public int? SizeOfSet { get; set; }

	/// <summary>aria-level</summary>
	public int? Level { get; set; }

	/// <summary>aria-multiselectable</summary>
	public bool? MultiSelectable { get; set; }

	/// <summary>aria-haspopup</summary>
	public string? HasPopup { get; set; }

	/// <summary>aria-controls</summary>
	public string? Controls { get; set; }

	/// <summary>aria-describedby</summary>
	public string? DescribedBy { get; set; }

	/// <summary>aria-labelledby</summary>
	public string? LabelledBy { get; set; }

	/// <summary>aria-description (VoiceOver secondary text from HelpText)</summary>
	public string? Description { get; set; }

	/// <summary>aria-valuetext (human-readable slider value)</summary>
	public string? ValueText { get; set; }

	/// <summary>ARIA landmark role (main, navigation, search, form, region)</summary>
	public string? LandmarkRole { get; set; }

	/// <summary>aria-roledescription (custom role description for VoiceOver)</summary>
	public string? RoleDescription { get; set; }
}

/// <summary>
/// Describes which automation patterns a UIElement supports.
/// </summary>
public class PatternCapabilities
{
	/// <summary>Has IInvokeProvider</summary>
	public bool CanInvoke { get; set; }

	/// <summary>Has IToggleProvider</summary>
	public bool CanToggle { get; set; }

	/// <summary>Has IRangeValueProvider</summary>
	public bool CanRangeValue { get; set; }

	/// <summary>Has IValueProvider</summary>
	public bool CanValue { get; set; }

	/// <summary>Has IExpandCollapseProvider</summary>
	public bool CanExpandCollapse { get; set; }

	/// <summary>Has ISelectionItemProvider</summary>
	public bool CanSelect { get; set; }

	/// <summary>Has IScrollProvider</summary>
	public bool CanScroll { get; set; }
}
