#nullable enable

using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Maps automation peers to ARIA attributes and semantic element types.
/// This copy is used for test builds that reference the Uno.UI project.
/// </summary>
public static class AriaMapper
{
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
		{ AutomationControlType.Text, "label" },
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

	public static string? GetAriaRole(AutomationControlType controlType)
	{
		return ControlTypeToRoleMap.TryGetValue(controlType, out var role) ? role : null;
	}

	public static SemanticElementType GetSemanticElementType(AutomationPeer peer)
	{
		var controlType = peer.GetAutomationControlType();
		return controlType switch
		{
			AutomationControlType.Button => SemanticElementType.Button,
			AutomationControlType.CheckBox => SemanticElementType.Checkbox,
			AutomationControlType.RadioButton => SemanticElementType.RadioButton,
			AutomationControlType.Slider => SemanticElementType.Slider,
			AutomationControlType.Edit => GetTextBoxType(peer),
			AutomationControlType.ComboBox => SemanticElementType.ComboBox,
			AutomationControlType.List => SemanticElementType.ListBox,
			AutomationControlType.ListItem => SemanticElementType.ListItem,
			AutomationControlType.Hyperlink => SemanticElementType.Link,
			_ => SemanticElementType.Generic
		};
	}

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

	public static AriaAttributes GetAriaAttributes(AutomationPeer peer)
	{
		var controlType = peer.GetAutomationControlType();
		var attributes = new AriaAttributes
		{
			Role = GetAriaRole(controlType),
			Label = peer.GetName(),
			Disabled = !peer.IsEnabled(),
		};

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

		if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeValueProvider)
		{
			attributes.ValueNow = rangeValueProvider.Value;
			attributes.ValueMin = rangeValueProvider.Minimum;
			attributes.ValueMax = rangeValueProvider.Maximum;
		}

		return attributes;
	}

	private static string ConvertToggleStateToAriaChecked(ToggleState state)
	{
		return state switch
		{
			ToggleState.On => "true",
			ToggleState.Off => "false",
			ToggleState.Indeterminate => "mixed",
			_ => "false"
		};
	}

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

public enum SemanticElementType
{
	Generic,
	Button,
	Checkbox,
	RadioButton,
	Slider,
	TextBox,
	TextArea,
	Password,
	ComboBox,
	ListBox,
	ListItem,
	Link
}

public class AriaAttributes
{
	public string? Role { get; set; }
	public string? Label { get; set; }
	public string? Checked { get; set; }
	public bool? Expanded { get; set; }
	public bool? Selected { get; set; }
	public bool Disabled { get; set; }
	public bool Required { get; set; }
	public double? ValueNow { get; set; }
	public double? ValueMin { get; set; }
	public double? ValueMax { get; set; }
	public int? PositionInSet { get; set; }
	public int? SizeOfSet { get; set; }
	public int? Level { get; set; }
	public bool? MultiSelectable { get; set; }
	public string? HasPopup { get; set; }
	public string? Controls { get; set; }
	public string? DescribedBy { get; set; }
	public string? LabelledBy { get; set; }
}

public class PatternCapabilities
{
	public bool CanInvoke { get; set; }
	public bool CanToggle { get; set; }
	public bool CanRangeValue { get; set; }
	public bool CanValue { get; set; }
	public bool CanExpandCollapse { get; set; }
	public bool CanSelect { get; set; }
	public bool CanScroll { get; set; }
}
