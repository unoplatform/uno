#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Factory for creating semantic DOM elements based on automation peer type and patterns.
/// Dispatches to type-specific element creation via NativeMethods.
/// </summary>
internal static partial class SemanticElementFactory
{
	/// <summary>
	/// Creates a semantic element for the given automation peer.
	/// </summary>
	/// <param name="peer">The automation peer.</param>
	/// <param name="handle">The element handle (IntPtr).</param>
	/// <param name="parentHandle">The parent element handle (IntPtr).</param>
	/// <param name="index">The insertion index within the parent, or null to append.</param>
	/// <param name="x">X position.</param>
	/// <param name="y">Y position.</param>
	/// <param name="width">Element width.</param>
	/// <param name="height">Element height.</param>
	/// <param name="owner">The owner UIElement, when available.</param>
	/// <param name="isFocusable">Whether the element should be keyboard-focusable (drives tabindex).</param>
	/// <returns>True if element was created successfully.</returns>
	public static bool CreateElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		UIElement? owner,
		bool isFocusable)
	{
		var elementType = AriaMapper.GetSemanticElementType(peer, owner);
		var attributes = AriaMapper.GetAriaAttributes(peer);
		var capabilities = AriaMapper.GetPatternCapabilities(peer);

		var created = elementType switch
		{
			SemanticElementType.Button => CreateButtonElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.ToggleButton => CreateToggleButtonElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Switch => CreateSwitchElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Checkbox => CreateCheckboxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, false, isFocusable),
			SemanticElementType.RadioButton => CreateCheckboxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, true, isFocusable),
			SemanticElementType.Slider => CreateSliderElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.TextBox => CreateTextBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, false, false, isFocusable),
			SemanticElementType.TextArea => CreateTextBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, true, false, isFocusable),
			SemanticElementType.Password => CreateTextBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, false, true, isFocusable),
			SemanticElementType.ComboBox => CreateComboBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.ListBox => CreateListBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.ListItem => CreateListItemElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Link => CreateLinkElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Heading => CreateHeadingElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.TabList => CreateTabListElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Tab => CreateTabElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Tree => CreateTreeElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.TreeItem => CreateTreeItemElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Grid => CreateGridElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.GridRow => CreateGridRowElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.GridCell => CreateGridCellElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.ColumnHeader => CreateColumnHeaderElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Menu => CreateMenuElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.MenuItem => CreateMenuItemElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable),
			SemanticElementType.Text => CreateTextElement(peer, handle, parentHandle, index, x, y, width, height, attributes, owner, isFocusable),
			_ => CreateGenericElement(peer, handle, parentHandle, index, x, y, width, height, attributes, isFocusable)
		};

		// Standalone body text carries only its textContent (set at creation) — it must NOT receive
		// aria-label/description/posinset/etc., which would duplicate the announced text.
		if (elementType == SemanticElementType.Text)
		{
			return created;
		}

		// Ensure aria-label is applied for all control types (FR-030, WCAG 4.1.2)
		// Button/Checkbox/Radio already pass label during creation; apply for others
		if (created && !string.IsNullOrEmpty(attributes.Label) &&
			elementType is not (SemanticElementType.Button or SemanticElementType.Checkbox or SemanticElementType.RadioButton))
		{
			NativeMethods.UpdateAriaLabel(handle, attributes.Label);
		}

		// Surface AutomationProperties.AutomationId as the xamlautomationid attribute on
		// factory-path elements (a stable test/automation id, not the accessible name).
		if (created && owner is not null)
		{
			var xamlAutomationId = AutomationProperties.GetAutomationId(owner);
			if (!string.IsNullOrEmpty(xamlAutomationId))
			{
				NativeMethods.SetXamlAutomationId(handle, xamlAutomationId);
			}
		}

		// Apply aria-description from HelpText for VoiceOver secondary context
		if (created && !string.IsNullOrEmpty(attributes.Description))
		{
			NativeMethods.UpdateAriaDescription(handle, attributes.Description);
		}

		// Apply landmark role if set (VoiceOver rotor landmark navigation)
		if (created && !string.IsNullOrEmpty(attributes.LandmarkRole))
		{
			NativeMethods.UpdateLandmarkRole(handle, attributes.LandmarkRole);
		}

		// Apply aria-roledescription for custom role descriptions
		if (created && !string.IsNullOrEmpty(attributes.RoleDescription))
		{
			NativeMethods.UpdateAriaRoleDescription(handle, attributes.RoleDescription);
		}

		// Apply aria-posinset/aria-setsize ONLY for element types whose ARIA role supports them
		// (option, listitem, menuitem, tab, treeitem, row, radio). For other roles (e.g. button)
		// these are dropped — position MUST NOT be concatenated into aria-label (FR-028); doing so
		// corrupts the accessible name and is not what posinset/setsize mean.
		if (created && attributes.PositionInSet is > 0 && attributes.SizeOfSet is > 0 &&
			SupportsAriaPositionInSet(elementType))
		{
			NativeMethods.UpdatePositionInSet(handle, attributes.PositionInSet.Value, attributes.SizeOfSet.Value);
		}

		// Apply aria-required for form fields (WCAG 3.3.2, matches WinUI3 IsRequiredForForm)
		if (created && attributes.Required)
		{
			NativeMethods.UpdateAriaRequired(handle, true);
		}

		// Apply aria-invalid for form fields with invalid values (WCAG 3.3.1, matches
		// WinUI3 IsDataValidForForm with inverted polarity). Omitted when the field is valid.
		if (created && attributes.Invalid)
		{
			NativeMethods.UpdateAriaInvalid(handle, true);
		}

		// Apply aria-live for live region elements (screen readers monitor content changes)
		if (created && owner is not null)
		{
			var liveSetting = AutomationProperties.GetLiveSetting(owner);
			if (liveSetting != AutomationLiveSetting.Off)
			{
				var ariaLive = liveSetting == AutomationLiveSetting.Assertive ? "assertive" : "polite";
				NativeMethods.UpdateAriaLive(handle, ariaLive);
			}
		}

		// Apply aria-labelledby when AutomationProperties.LabeledBy resolves to a labeller that has
		// its own semantic node. The IDREF is computed here (not in AriaMapper) because only the
		// WASM layer can map the labeller UIElement → its uno-semantics-{handle} id and verify the
		// node is actually present (no dangling IDREF — FR-019/FR-022). aria-labelledby is independent
		// of aria-label: both can be present.
		if (created)
		{
			var labelledById = ResolveLabelledByIdRef(peer);
			if (labelledById is not null)
			{
				NativeMethods.UpdateAriaLabelledBy(handle, labelledById);
			}
		}

		// Apply relationship attributes (aria-describedby, aria-controls, aria-flowto)
		if (created)
		{
			ApplyRelationshipAttributes(peer, handle);
		}

		// Apply aria-expanded for ExpandCollapse-capable elements not handled by their
		// own factory (Expander, NavigationViewItem, SplitButton, MenuFlyoutSubItem).
		// ComboBox and TreeItem already pass `expanded` at creation time.
		if (created && attributes.Expanded.HasValue &&
			elementType is not (SemanticElementType.ComboBox or SemanticElementType.TreeItem))
		{
			NativeMethods.UpdateExpandCollapseState(handle, attributes.Expanded.Value);
		}

		// Apply aria-keyshortcuts from AcceleratorKey only (WinUI3 parity, ARIA 1.2).
		if (created && !string.IsNullOrEmpty(attributes.KeyShortcuts))
		{
			NativeMethods.UpdateAriaKeyShortcuts(handle, attributes.KeyShortcuts);
		}

		// Apply aria-haspopup from the C# value (popup kind decided by AriaMapper from the
		// ExpandCollapse pattern / control type), not hardcoded in TS (FR-028).
		if (created && !string.IsNullOrEmpty(attributes.HasPopup))
		{
			NativeMethods.UpdateAriaHasPopup(handle, attributes.HasPopup);
		}

		// Apply the HTML accesskey attribute from AccessKey (mnemonic), kept separate from
		// aria-keyshortcuts (FR-028).
		if (created && !string.IsNullOrEmpty(attributes.AccessKey))
		{
			NativeMethods.SetAccessKey(handle, attributes.AccessKey);
		}

		// Apply aria-modal for IsDialog peers (the FocusTrap subsystem also sets this
		// when a modal scope activates; this covers static/declarative dialogs).
		if (created && attributes.Modal == true)
		{
			NativeMethods.UpdateAriaModal(handle, true);
		}

		// Apply owner-scoped attributes sourced from AutomationProperties attached properties
		// (aria-level, aria-busy, lang). These are independent of the resolved element type.
		if (created && owner is not null)
		{
			ApplyOwnerScopedAriaAttributes(owner, handle);
		}

		return created;
	}

	/// <summary>
	/// Creates a button semantic element.
	/// </summary>
	private static bool CreateButtonElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		NativeMethods.CreateButtonElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			attributes.Disabled,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a checkbox or radio button semantic element.
	/// </summary>
	private static bool CreateCheckboxElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isRadio,
		bool isFocusable)
	{
		if (isRadio)
		{
			var isChecked = attributes.Checked == "true";

			// Get group name for radio button
			string? groupName = null;
			if (peer is FrameworkElementAutomationPeer frameworkPeer &&
				frameworkPeer.Owner is RadioButton radioButton)
			{
				groupName = radioButton.GroupName;
			}

			NativeMethods.CreateRadioElement(
				parentHandle,
				handle,
				index,
				x,
				y,
				width,
				height,
				isChecked,
				attributes.Label,
				groupName,
				isFocusable);
		}
		else
		{
			NativeMethods.CreateCheckboxElement(
				parentHandle,
				handle,
				index,
				x,
				y,
				width,
				height,
				attributes.Checked,
				attributes.Label,
				isFocusable);
		}

		return true;
	}

	/// <summary>
	/// Creates a slider (range input) semantic element.
	/// </summary>
	private static bool CreateSliderElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var value = attributes.ValueNow ?? 0;
		var min = attributes.ValueMin ?? 0;
		var max = attributes.ValueMax ?? 100;

		// Determine step from SmallChange (used for arrow key increments)
		double step = 1;
		if (peer.GetPattern(PatternInterface.RangeValue) is IRangeValueProvider rangeProvider)
		{
			step = rangeProvider.SmallChange;
		}

		// Determine orientation from the Slider control
		var orientation = "horizontal";
		if (peer is FrameworkElementAutomationPeer frameworkPeer &&
			frameworkPeer.Owner is Microsoft.UI.Xaml.Controls.Slider slider &&
			slider.Orientation == Microsoft.UI.Xaml.Controls.Orientation.Vertical)
		{
			orientation = "vertical";
		}

		NativeMethods.CreateSliderElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			value,
			min,
			max,
			step,
			orientation,
			attributes.ValueText,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a text input semantic element.
	/// </summary>
	private static bool CreateTextBoxElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool multiline,
		bool password,
		bool isFocusable)
	{
		var value = "";
		var isReadOnly = false;
		string? placeholder = null;
		var selectionStart = 0;
		var selectionEnd = 0;

		if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
		{
			value = valueProvider.Value ?? "";
			isReadOnly = valueProvider.IsReadOnly;
		}

		// Extract placeholder text from the control
		if (peer is FrameworkElementAutomationPeer feap)
		{
			if (feap.Owner is TextBox textBox)
			{
				placeholder = textBox.PlaceholderText;
				selectionStart = Math.Max(0, Math.Min(textBox.SelectionStart, value.Length));
				selectionEnd = Math.Max(selectionStart, Math.Min(textBox.SelectionStart + textBox.SelectionLength, value.Length));
			}
		}

		NativeMethods.CreateTextBoxElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			value ?? "",
			multiline,
			password,
			isReadOnly,
			selectionStart,
			selectionEnd,
			isFocusable);

		// Set native placeholder on the input element
		if (!string.IsNullOrEmpty(placeholder))
		{
			NativeMethods.UpdateTextBoxPlaceholder(handle, placeholder);
		}

		return true;
	}

	/// <summary>
	/// Creates a combobox semantic element.
	/// </summary>
	private static bool CreateComboBoxElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var expanded = attributes.Expanded ?? false;
		string? selectedValue = null;

		if (peer is FrameworkElementAutomationPeer frameworkPeer &&
			frameworkPeer.Owner is ComboBox comboBox &&
			comboBox.SelectedItem is { } selected)
		{
			// Mirror WinUI selection-text resolution: prefer the value pattern (which honors
			// DisplayMemberPath / item-template-bound value providers), fall back to ToString
			// only when no value provider is exposed. This avoids announcing a type name for
			// non-string item view models.
			selectedValue = (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
				? valueProvider.Value
				: selected.ToString();
		}

		NativeMethods.CreateComboBoxElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			expanded,
			selectedValue,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a listbox semantic element.
	/// </summary>
	private static bool CreateListBoxElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var multiselect = attributes.MultiSelectable ?? false;

		NativeMethods.CreateListBoxElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			multiselect,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a list item semantic element.
	/// </summary>
	private static bool CreateListItemElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var selected = attributes.Selected ?? false;
		var positionInSet = attributes.PositionInSet ?? 0;
		var sizeOfSet = attributes.SizeOfSet ?? 0;

		NativeMethods.CreateListItemElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			selected,
			positionInSet,
			sizeOfSet,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a link semantic element.
	/// </summary>
	private static bool CreateLinkElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		NativeMethods.CreateLinkElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a heading semantic element. VoiceOver uses headings for rotor navigation.
	/// </summary>
	private static bool CreateHeadingElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var level = attributes.Level ?? 2; // Default to h2 if no heading level specified

		NativeMethods.CreateHeadingElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			level,
			attributes.Label,
			isFocusable);
		return true;
	}

	/// <summary>
	/// FR-015: creates a non-interactive standalone body-text element (&lt;p&gt; block / &lt;span&gt; inline).
	/// Only its textContent is exposed — no role, no aria-label, no tabindex.
	/// </summary>
	private static bool CreateTextElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		UIElement? owner,
		bool isFocusable)
	{
		var text = !string.IsNullOrEmpty(attributes.Label) ? attributes.Label : (owner as TextBlock)?.Text;
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}

		// TextBlock/RichTextBlock are leaf, block-level controls in the AOM — emit <p>.
		NativeMethods.CreateTextElement(parentHandle, handle, index, x, y, width, height, text, true, isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a toggle button semantic element (button with aria-pressed).
	/// Used for ToggleButton, AppBarToggleButton, etc.
	/// </summary>
	private static bool CreateToggleButtonElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var pressed = attributes.Checked ?? "false";
		NativeMethods.CreateToggleButtonElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			pressed,
			attributes.Disabled,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a switch semantic element (role="switch" with aria-checked).
	/// Used for ToggleSwitch which maps to the ARIA switch pattern.
	/// </summary>
	private static bool CreateSwitchElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var isOn = attributes.Checked ?? "false";
		NativeMethods.CreateSwitchElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			isOn,
			attributes.Disabled,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a tablist container element.
	/// </summary>
	private static bool CreateTabListElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		NativeMethods.CreateTabListElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a tab element with selection and position.
	/// </summary>
	private static bool CreateTabElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var selected = attributes.Selected ?? false;
		var positionInSet = attributes.PositionInSet ?? 0;
		var sizeOfSet = attributes.SizeOfSet ?? 0;

		NativeMethods.CreateTabElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			selected,
			positionInSet,
			sizeOfSet,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a tree container element.
	/// </summary>
	private static bool CreateTreeElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var multiselectable = attributes.MultiSelectable ?? false;

		NativeMethods.CreateTreeElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			multiselectable,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a treeitem element with level, expanded state, and selection.
	/// </summary>
	private static bool CreateTreeItemElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var level = attributes.Level ?? 1;
		var selected = attributes.Selected ?? false;
		var positionInSet = attributes.PositionInSet ?? 0;
		var sizeOfSet = attributes.SizeOfSet ?? 0;

		// Determine expanded state: "true", "false", or "none" (leaf node)
		var expanded = "none";
		if (attributes.Expanded.HasValue)
		{
			expanded = attributes.Expanded.Value ? "true" : "false";
		}

		NativeMethods.CreateTreeItemElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			level,
			expanded,
			selected,
			positionInSet,
			sizeOfSet,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a grid/table container element with row/column count.
	/// </summary>
	private static bool CreateGridElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var rowCount = 0;
		var colCount = 0;

		if (peer.GetPattern(PatternInterface.Grid) is IGridProvider gridProvider)
		{
			rowCount = gridProvider.RowCount;
			colCount = gridProvider.ColumnCount;
		}

		NativeMethods.CreateGridElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			rowCount,
			colCount,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a grid row element.
	/// </summary>
	private static bool CreateGridRowElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		// ARIA aria-rowindex is 1-based; ensure at least 1
		var rowIndex = Math.Max(attributes.PositionInSet ?? 1, 1);

		NativeMethods.CreateGridRowElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			rowIndex,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a grid cell element.
	/// </summary>
	private static bool CreateGridCellElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var rowIndex = 0;
		var colIndex = 0;

		if (peer.GetPattern(PatternInterface.GridItem) is IGridItemProvider gridItemProvider)
		{
			// ARIA aria-rowindex/aria-colindex are 1-based; GridItemProvider is 0-based
			rowIndex = gridItemProvider.Row + 1;
			colIndex = gridItemProvider.Column + 1;
		}

		NativeMethods.CreateGridCellElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			rowIndex,
			colIndex,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a column header element.
	/// </summary>
	private static bool CreateColumnHeaderElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var colIndex = 0;
		if (peer.GetPattern(PatternInterface.GridItem) is IGridItemProvider gridItemProvider)
		{
			colIndex = gridItemProvider.Column;
		}

		NativeMethods.CreateColumnHeaderElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			colIndex,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a menu container element.
	/// </summary>
	private static bool CreateMenuElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		NativeMethods.CreateMenuElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a menuitem element.
	/// </summary>
	private static bool CreateMenuItemElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		var hasSubmenu = attributes.Expanded.HasValue; // If it can expand, it has a submenu

		NativeMethods.CreateMenuItemElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			attributes.Disabled,
			hasSubmenu,
			isFocusable);
		return true;
	}

	/// <summary>
	/// Creates a generic div semantic element with ARIA role.
	/// Falls back to this for unsupported control types.
	/// </summary>
	private static bool CreateGenericElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes,
		bool isFocusable)
	{
		// Returns false to signal caller should use the existing AddSemanticElement for backward compatibility.
		// Focusability for this fallback path is applied by AddSemanticElement, not here.
		return false;
	}

	/// <summary>
	/// Returns true if the given element type maps to an ARIA role that supports aria-posinset/aria-setsize.
	/// Per WAI-ARIA, these are: option, listitem, menuitem, menuitemcheckbox, menuitemradio, radio, row, tab, treeitem.
	/// </summary>
	private static bool SupportsAriaPositionInSet(SemanticElementType elementType)
	{
		return elementType is SemanticElementType.ListItem or SemanticElementType.RadioButton
			or SemanticElementType.Tab or SemanticElementType.TreeItem
			or SemanticElementType.MenuItem or SemanticElementType.GridRow;
	}

	/// <summary>
	/// Applies ARIA relationship attributes (describedby, controls, flowto) to a semantic element.
	/// Resolves AutomationPeer collections to space-separated DOM element IDs.
	/// </summary>
	private static void ApplyRelationshipAttributes(AutomationPeer peer, IntPtr handle)
	{
		var describedByIds = ResolvePeerCollectionToIdList(peer.GetDescribedBy());
		if (describedByIds is not null)
		{
			NativeMethods.UpdateAriaDescribedBy(handle, describedByIds);
		}

		var controlledIds = ResolvePeerCollectionToIdList(peer.GetControlledPeers());
		if (controlledIds is not null)
		{
			NativeMethods.UpdateAriaControls(handle, controlledIds);
		}

		var flowsToIds = ResolvePeerCollectionToIdList(peer.GetFlowsTo());
		if (flowsToIds is not null)
		{
			NativeMethods.UpdateAriaFlowTo(handle, flowsToIds);
		}
	}

	/// <summary>
	/// Applies the owner-scoped ARIA attributes that are derived from <see cref="AutomationProperties"/>
	/// attached properties rather than from automation patterns: <c>aria-level</c>
	/// (<see cref="AutomationProperties.LevelProperty"/>), <c>aria-busy</c>
	/// (<see cref="AutomationProperties.ItemStatusProperty"/>) and <c>lang</c>
	/// (<see cref="AutomationProperties.CultureProperty"/>).
	/// </summary>
	/// <remarks>
	/// Shared between the factory path and the generic <c>AddSemanticElement</c> path so both
	/// emit the same attributes regardless of the resolved element type.
	/// </remarks>
	internal static void ApplyOwnerScopedAriaAttributes(UIElement owner, IntPtr handle)
	{
		// aria-level from AutomationProperties.Level (distinct from heading level, which is
		// emitted by the heading factory). Level is a 1-based hierarchical position; 0 = unset.
		var level = AutomationProperties.GetLevel(owner);
		if (level > 0)
		{
			NativeMethods.UpdateAriaLevel(handle, level);
		}

		// aria-busy from AutomationProperties.ItemStatus. ItemStatus is a free-form status
		// string ("New", "Busy", "Loading", ...); we map it conservatively to aria-busy only
		// when it clearly conveys a busy/in-progress state.
		var itemStatus = AutomationProperties.GetItemStatus(owner);
		if (IsBusyStatus(itemStatus))
		{
			NativeMethods.UpdateAriaBusy(handle, true);
		}

		// lang from AutomationProperties.Culture (an LCID). The XAML FrameworkElement.Language
		// property is not implemented on Skia/WASM, so Culture is the reliable source here.
		var lang = ResolveLang(owner);
		if (!string.IsNullOrEmpty(lang))
		{
			NativeMethods.UpdateLang(handle, lang);
		}
	}

	/// <summary>
	/// Determines whether an <see cref="AutomationProperties.ItemStatusProperty"/> value
	/// represents a busy / in-progress state that should surface as <c>aria-busy="true"</c>.
	/// </summary>
	private static bool IsBusyStatus(string? itemStatus)
	{
		if (string.IsNullOrWhiteSpace(itemStatus))
		{
			return false;
		}

		var trimmed = itemStatus.Trim();
		return trimmed.Equals("busy", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("loading", StringComparison.OrdinalIgnoreCase)
			|| trimmed.Equals("updating", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Resolves the BCP-47 language tag for the <c>lang</c> attribute from
	/// <see cref="AutomationProperties.CultureProperty"/> (an LCID). Returns <c>null</c> when
	/// no culture is set or the LCID cannot be resolved.
	/// </summary>
	private static string? ResolveLang(UIElement owner)
	{
		var lcid = AutomationProperties.GetCulture(owner);
		if (lcid <= 0)
		{
			return null;
		}

		try
		{
			var name = CultureInfo.GetCultureInfo(lcid).Name;
			return string.IsNullOrEmpty(name) ? null : name;
		}
		catch (CultureNotFoundException)
		{
			// Unknown/invalid LCID — skip rather than emitting a bogus lang attribute.
			return null;
		}
	}

	/// <summary>
	/// Resolves the <c>aria-labelledby</c> IDREF for a peer from its owner's
	/// <see cref="AutomationProperties.LabeledByProperty"/> (FR-019). Returns the labeller's
	/// <c>uno-semantics-{handle}</c> id only when the labeller has a real semantic node in the AOM
	/// (<see cref="WebAssemblyAccessibility.HasSemanticElement"/>), so a dangling IDREF is never
	/// emitted (FR-022). Returns <c>null</c> when there is no labeller or the labeller is not semantic.
	/// </summary>
	/// <param name="peer">The automation peer whose owner may carry <c>AutomationProperties.LabeledBy</c>.</param>
	/// <returns>The <c>uno-semantics-{handle}</c> id of the labeller, or <c>null</c>.</returns>
	internal static string? ResolveLabelledByIdRef(AutomationPeer peer)
	{
		var labeller = AriaMapper.ResolveLabelledByElement(peer);
		if (labeller is null)
		{
			return null;
		}

		var labellerHandle = labeller.Visual.Handle;
		if (labellerHandle == IntPtr.Zero || !WebAssemblyAccessibility.Instance.HasSemanticElement(labellerHandle))
		{
			return null;
		}

		return "uno-semantics-" + labellerHandle;
	}

	/// <summary>
	/// Resolves a collection of AutomationPeers to a space-separated list of DOM element IDs
	/// using the uno-semantics-{handle} convention.
	/// </summary>
	internal static string? ResolvePeerCollectionToIdList(IEnumerable<AutomationPeer>? peers)
	{
		if (peers is null)
		{
			return null;
		}

		StringBuilder? sb = null;
		foreach (var relatedPeer in peers)
		{
			if (relatedPeer is FrameworkElementAutomationPeer { Owner: { } relatedOwner })
			{
				var relatedHandle = relatedOwner.Visual.Handle;
				if (relatedHandle != IntPtr.Zero)
				{
					sb ??= new StringBuilder();
					if (sb.Length > 0)
					{
						sb.Append(' ');
					}
					sb.Append("uno-semantics-");
					sb.Append(relatedHandle);
				}
			}
		}

		return sb?.ToString();
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createButtonElement")]
		internal static partial void CreateButtonElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool disabled, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createSliderElement")]
		internal static partial void CreateSliderElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, double value, double min, double max, double step, string orientation, string? valueText, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createCheckboxElement")]
		internal static partial void CreateCheckboxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? checkedState, string? label, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createRadioElement")]
		internal static partial void CreateRadioElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool isChecked, string? label, string? groupName, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTextBoxElement")]
		internal static partial void CreateTextBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string value, bool multiline, bool password, bool isReadOnly, int selectionStart, int selectionEnd, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createComboBoxElement")]
		internal static partial void CreateComboBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool expanded, string? selectedValue, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListBoxElement")]
		internal static partial void CreateListBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool multiselect, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListItemElement")]
		internal static partial void CreateListItemElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool selected, int positionInSet, int sizeOfSet, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createLinkElement")]
		internal static partial void CreateLinkElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createHeadingElement")]
		internal static partial void CreateHeadingElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, int level, string? label, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTextElement")]
		internal static partial void CreateTextElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string text, bool isBlock, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createToggleButtonElement")]
		internal static partial void CreateToggleButtonElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, string pressed, bool disabled, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createSwitchElement")]
		internal static partial void CreateSwitchElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, string isOn, bool disabled, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabel")]
		internal static partial void UpdateAriaLabel(IntPtr handle, string label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.setXamlAutomationId")]
		internal static partial void SetXamlAutomationId(IntPtr handle, string automationId);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescription")]
		internal static partial void UpdateAriaDescription(IntPtr handle, string description);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateLandmarkRole")]
		internal static partial void UpdateLandmarkRole(IntPtr handle, string role);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaRoleDescription")]
		internal static partial void UpdateAriaRoleDescription(IntPtr handle, string roleDescription);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updatePositionInSet")]
		internal static partial void UpdatePositionInSet(IntPtr handle, int positionInSet, int sizeOfSet);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaRequired")]
		internal static partial void UpdateAriaRequired(IntPtr handle, bool required);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaInvalid")]
		internal static partial void UpdateAriaInvalid(IntPtr handle, bool invalid);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaPressed")]
		internal static partial void UpdateAriaPressed(IntPtr handle, string pressed);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLive")]
		internal static partial void UpdateAriaLive(IntPtr handle, string ariaLive);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescribedBy")]
		internal static partial void UpdateAriaDescribedBy(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaControls")]
		internal static partial void UpdateAriaControls(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaFlowTo")]
		internal static partial void UpdateAriaFlowTo(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateTextBoxPlaceholder")]
		internal static partial void UpdateTextBoxPlaceholder(IntPtr handle, string placeholder);

		// ===== Tab / Tree / Grid / Menu Element Creation =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTabListElement")]
		internal static partial void CreateTabListElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTabElement")]
		internal static partial void CreateTabElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool selected, int positionInSet, int sizeOfSet, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTreeElement")]
		internal static partial void CreateTreeElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool multiselectable, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTreeItemElement")]
		internal static partial void CreateTreeItemElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, int level, string expanded, bool selected, int positionInSet, int sizeOfSet, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createGridElement")]
		internal static partial void CreateGridElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, int rowCount, int colCount, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createGridRowElement")]
		internal static partial void CreateGridRowElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, int rowIndex, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createGridCellElement")]
		internal static partial void CreateGridCellElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, int rowIndex, int colIndex, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createColumnHeaderElement")]
		internal static partial void CreateColumnHeaderElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, int colIndex, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createMenuElement")]
		internal static partial void CreateMenuElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool isFocusable);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createMenuItemElement")]
		internal static partial void CreateMenuItemElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool disabled, bool hasSubmenu, bool isFocusable);

		// ===== Relationship Updates =====

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabelledBy")]
		internal static partial void UpdateAriaLabelledBy(IntPtr handle, string idList);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.updateExpandCollapseState")]
		internal static partial void UpdateExpandCollapseState(IntPtr handle, bool expanded);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaKeyShortcuts")]
		internal static partial void UpdateAriaKeyShortcuts(IntPtr handle, string keyShortcuts);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaHasPopup")]
		internal static partial void UpdateAriaHasPopup(IntPtr handle, string hasPopup);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.setAccessKey")]
		internal static partial void SetAccessKey(IntPtr handle, string accessKey);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaModal")]
		internal static partial void UpdateAriaModal(IntPtr handle, bool modal);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLevel")]
		internal static partial void UpdateAriaLevel(IntPtr handle, int level);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaBusy")]
		internal static partial void UpdateAriaBusy(IntPtr handle, bool busy);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateLang")]
		internal static partial void UpdateLang(IntPtr handle, string lang);
	}
}
