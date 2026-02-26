#nullable enable

using System;
using System.Globalization;
using System.Runtime.InteropServices.JavaScript;
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
	/// <returns>True if element was created successfully.</returns>
	public static bool CreateElement(
		AutomationPeer peer,
		IntPtr handle,
		IntPtr parentHandle,
		int? index,
		float x,
		float y,
		float width,
		float height)
	{
		var elementType = AriaMapper.GetSemanticElementType(peer);
		var attributes = AriaMapper.GetAriaAttributes(peer);
		var capabilities = AriaMapper.GetPatternCapabilities(peer);

		Console.WriteLine($"[A11y] SemanticElementFactory.CreateElement: type={elementType} handle={handle} parent={parentHandle} index={index?.ToString(CultureInfo.InvariantCulture) ?? "append"} label='{attributes.Label}' pos=({x},{y}) size={width}x{height}");

		var created = elementType switch
		{
			SemanticElementType.Button => CreateButtonElement(peer, handle, parentHandle, index, x, y, width, height, attributes),
			SemanticElementType.Checkbox => CreateCheckboxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, false),
			SemanticElementType.RadioButton => CreateCheckboxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, true),
			SemanticElementType.Slider => CreateSliderElement(peer, handle, parentHandle, index, x, y, width, height, attributes),
			SemanticElementType.TextBox => CreateTextBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, false, false),
			SemanticElementType.TextArea => CreateTextBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, true, false),
			SemanticElementType.Password => CreateTextBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes, false, true),
			SemanticElementType.ComboBox => CreateComboBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes),
			SemanticElementType.ListBox => CreateListBoxElement(peer, handle, parentHandle, index, x, y, width, height, attributes),
			SemanticElementType.ListItem => CreateListItemElement(peer, handle, parentHandle, index, x, y, width, height, attributes),
			SemanticElementType.Link => CreateLinkElement(peer, handle, parentHandle, index, x, y, width, height, attributes),
			SemanticElementType.Heading => CreateHeadingElement(peer, handle, parentHandle, index, x, y, width, height, attributes),
			_ => CreateGenericElement(peer, handle, parentHandle, index, x, y, width, height, attributes)
		};

		// Ensure aria-label is applied for all control types (FR-030, WCAG 4.1.2)
		// Button/Checkbox/Radio already pass label during creation; apply for others
		if (created && !string.IsNullOrEmpty(attributes.Label) &&
			elementType is not (SemanticElementType.Button or SemanticElementType.Checkbox or SemanticElementType.RadioButton))
		{
			NativeMethods.UpdateAriaLabel(handle, attributes.Label);
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
		AriaAttributes attributes)
	{
		Console.WriteLine($"[A11y] CREATE BUTTON: handle={handle} parent={parentHandle} label='{attributes.Label}' disabled={attributes.Disabled} pos=({x},{y}) size={width}x{height}");
		NativeMethods.CreateButtonElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label,
			attributes.Disabled);
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
		bool isRadio)
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

			Console.WriteLine($"[A11y] CREATE RADIO: handle={handle} parent={parentHandle} label='{attributes.Label}' checked={isChecked} group='{groupName}' pos=({x},{y}) size={width}x{height}");
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
				groupName);
		}
		else
		{
			Console.WriteLine($"[A11y] CREATE CHECKBOX: handle={handle} parent={parentHandle} label='{attributes.Label}' checked='{attributes.Checked}' pos=({x},{y}) size={width}x{height}");
			NativeMethods.CreateCheckboxElement(
				parentHandle,
				handle,
				index,
				x,
				y,
				width,
				height,
				attributes.Checked,
				attributes.Label);
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
		AriaAttributes attributes)
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

		Console.WriteLine($"[A11y] CREATE SLIDER: handle={handle} parent={parentHandle} value={value} min={min} max={max} step={step} orientation={orientation} valueText='{attributes.ValueText}' pos=({x},{y}) size={width}x{height}");
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
			attributes.ValueText);
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
		bool password)
	{
		var value = "";
		var isReadOnly = false;

		if (peer.GetPattern(PatternInterface.Value) is IValueProvider valueProvider)
		{
			value = valueProvider.Value ?? "";
			isReadOnly = valueProvider.IsReadOnly;
		}

		Console.WriteLine($"[A11y] CREATE TEXTBOX: handle={handle} parent={parentHandle} multiline={multiline} password={password} readOnly={isReadOnly} valueLen={value?.Length ?? 0} pos=({x},{y}) size={width}x{height}");
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
			isReadOnly);
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
		AriaAttributes attributes)
	{
		var expanded = attributes.Expanded ?? false;
		string? selectedValue = null;

		if (peer is FrameworkElementAutomationPeer frameworkPeer &&
			frameworkPeer.Owner is ComboBox comboBox &&
			comboBox.SelectedItem is { } selected)
		{
			selectedValue = selected.ToString();
		}

		Console.WriteLine($"[A11y] CREATE COMBOBOX: handle={handle} parent={parentHandle} expanded={expanded} selectedValue='{selectedValue}' pos=({x},{y}) size={width}x{height}");
		NativeMethods.CreateComboBoxElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			expanded,
			selectedValue);
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
		AriaAttributes attributes)
	{
		var multiselect = attributes.MultiSelectable ?? false;

		Console.WriteLine($"[A11y] CREATE LISTBOX: handle={handle} parent={parentHandle} multiselect={multiselect} pos=({x},{y}) size={width}x{height}");
		NativeMethods.CreateListBoxElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			multiselect);
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
		AriaAttributes attributes)
	{
		var selected = attributes.Selected ?? false;
		var positionInSet = attributes.PositionInSet ?? 0;
		var sizeOfSet = attributes.SizeOfSet ?? 0;

		Console.WriteLine($"[A11y] CREATE LISTITEM: handle={handle} parent={parentHandle} selected={selected} pos={positionInSet}/{sizeOfSet} at=({x},{y}) size={width}x{height}");
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
			sizeOfSet);
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
		AriaAttributes attributes)
	{
		Console.WriteLine($"[A11y] CREATE LINK: handle={handle} parent={parentHandle} label='{attributes.Label}' pos=({x},{y}) size={width}x{height}");
		NativeMethods.CreateLinkElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			attributes.Label);
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
		AriaAttributes attributes)
	{
		var level = attributes.Level ?? 2; // Default to h2 if no heading level specified

		Console.WriteLine($"[A11y] CREATE HEADING: handle={handle} parent={parentHandle} level=h{level} label='{attributes.Label}' pos=({x},{y}) size={width}x{height}");
		NativeMethods.CreateHeadingElement(
			parentHandle,
			handle,
			index,
			x,
			y,
			width,
			height,
			level,
			attributes.Label);
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
		AriaAttributes attributes)
	{
		var controlType = peer.GetAutomationControlType();
		Console.WriteLine($"[A11y] SemanticElementFactory.CreateGenericElement: no specific semantic element for controlType={controlType} handle={handle} — returning false to use generic AddSemanticElement fallback");

		// Returns false to signal caller should use the existing AddSemanticElement for backward compatibility
		return false;
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createButtonElement")]
		internal static partial void CreateButtonElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label, bool disabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createSliderElement")]
		internal static partial void CreateSliderElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, double value, double min, double max, double step, string orientation, string? valueText);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createCheckboxElement")]
		internal static partial void CreateCheckboxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? checkedState, string? label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createRadioElement")]
		internal static partial void CreateRadioElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool isChecked, string? label, string? groupName);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTextBoxElement")]
		internal static partial void CreateTextBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string value, bool multiline, bool password, bool isReadOnly);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createComboBoxElement")]
		internal static partial void CreateComboBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool expanded, string? selectedValue);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListBoxElement")]
		internal static partial void CreateListBoxElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool multiselect);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListItemElement")]
		internal static partial void CreateListItemElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, bool selected, int positionInSet, int sizeOfSet);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createLinkElement")]
		internal static partial void CreateLinkElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, string? label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createHeadingElement")]
		internal static partial void CreateHeadingElement(IntPtr parentHandle, IntPtr handle, int? index, float x, float y, float width, float height, int level, string? label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaLabel")]
		internal static partial void UpdateAriaLabel(IntPtr handle, string label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaDescription")]
		internal static partial void UpdateAriaDescription(IntPtr handle, string description);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateLandmarkRole")]
		internal static partial void UpdateLandmarkRole(IntPtr handle, string role);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.Accessibility.updateAriaRoleDescription")]
		internal static partial void UpdateAriaRoleDescription(IntPtr handle, string roleDescription);
	}
}
