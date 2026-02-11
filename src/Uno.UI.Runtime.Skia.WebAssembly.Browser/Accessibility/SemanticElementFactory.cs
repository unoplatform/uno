#nullable enable

using System;
using System.Runtime.InteropServices.JavaScript;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Runtime.Skia;

/// <summary>
/// Factory for creating semantic DOM elements based on automation peer type and patterns.
/// Dispatches to type-specific element creation via NativeMethods.
/// </summary>
internal static class SemanticElementFactory
{
	/// <summary>
	/// Creates a semantic element for the given automation peer.
	/// </summary>
	/// <param name="peer">The automation peer.</param>
	/// <param name="handle">The element handle (IntPtr).</param>
	/// <param name="x">X position.</param>
	/// <param name="y">Y position.</param>
	/// <param name="width">Element width.</param>
	/// <param name="height">Element height.</param>
	/// <returns>True if element was created successfully.</returns>
	public static bool CreateElement(
		AutomationPeer peer,
		IntPtr handle,
		float x,
		float y,
		float width,
		float height)
	{
		var elementType = AriaMapper.GetSemanticElementType(peer);
		var attributes = AriaMapper.GetAriaAttributes(peer);
		var capabilities = AriaMapper.GetPatternCapabilities(peer);

		return elementType switch
		{
			SemanticElementType.Button => CreateButtonElement(peer, handle, x, y, width, height, attributes),
			SemanticElementType.Checkbox => CreateCheckboxElement(peer, handle, x, y, width, height, attributes, false),
			SemanticElementType.RadioButton => CreateCheckboxElement(peer, handle, x, y, width, height, attributes, true),
			SemanticElementType.Slider => CreateSliderElement(peer, handle, x, y, width, height, attributes),
			SemanticElementType.TextBox => CreateTextBoxElement(peer, handle, x, y, width, height, attributes, false, false),
			SemanticElementType.TextArea => CreateTextBoxElement(peer, handle, x, y, width, height, attributes, true, false),
			SemanticElementType.Password => CreateTextBoxElement(peer, handle, x, y, width, height, attributes, false, true),
			SemanticElementType.ComboBox => CreateComboBoxElement(peer, handle, x, y, width, height, attributes),
			SemanticElementType.ListBox => CreateListBoxElement(peer, handle, x, y, width, height, attributes),
			SemanticElementType.ListItem => CreateListItemElement(peer, handle, x, y, width, height, attributes),
			SemanticElementType.Link => CreateLinkElement(peer, handle, x, y, width, height, attributes),
			_ => CreateGenericElement(peer, handle, x, y, width, height, attributes)
		};
	}

	/// <summary>
	/// Creates a button semantic element.
	/// </summary>
	private static bool CreateButtonElement(
		AutomationPeer peer,
		IntPtr handle,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes)
	{
		NativeMethods.CreateButtonElement(
			handle,
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

			NativeMethods.CreateRadioElement(
				handle,
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
			NativeMethods.CreateCheckboxElement(
				handle,
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

		NativeMethods.CreateSliderElement(
			handle,
			x,
			y,
			width,
			height,
			value,
			min,
			max,
			step,
			orientation);
		return true;
	}

	/// <summary>
	/// Creates a text input semantic element.
	/// </summary>
	private static bool CreateTextBoxElement(
		AutomationPeer peer,
		IntPtr handle,
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

		NativeMethods.CreateTextBoxElement(
			handle,
			x,
			y,
			width,
			height,
			value,
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

		NativeMethods.CreateComboBoxElement(
			handle,
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
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes)
	{
		var multiselect = attributes.MultiSelectable ?? false;

		NativeMethods.CreateListBoxElement(
			handle,
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
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes)
	{
		var selected = attributes.Selected ?? false;
		var positionInSet = attributes.PositionInSet ?? 0;
		var sizeOfSet = attributes.SizeOfSet ?? 0;

		NativeMethods.CreateListItemElement(
			handle,
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
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes)
	{
		// Stub - will create anchor element
		return true;
	}

	/// <summary>
	/// Creates a generic div semantic element with ARIA role.
	/// Falls back to this for unsupported control types.
	/// </summary>
	private static bool CreateGenericElement(
		AutomationPeer peer,
		IntPtr handle,
		float x,
		float y,
		float width,
		float height,
		AriaAttributes attributes)
	{
		// Returns false to signal caller should use the existing AddSemanticElement for backward compatibility
		return false;
	}

	private static partial class NativeMethods
	{
		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createButtonElement")]
		internal static partial void CreateButtonElement(IntPtr handle, float x, float y, float width, float height, string? label, bool disabled);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createSliderElement")]
		internal static partial void CreateSliderElement(IntPtr handle, float x, float y, float width, float height, double value, double min, double max, double step, string orientation);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createCheckboxElement")]
		internal static partial void CreateCheckboxElement(IntPtr handle, float x, float y, float width, float height, string? checkedState, string? label);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createRadioElement")]
		internal static partial void CreateRadioElement(IntPtr handle, float x, float y, float width, float height, bool isChecked, string? label, string? groupName);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createTextBoxElement")]
		internal static partial void CreateTextBoxElement(IntPtr handle, float x, float y, float width, float height, string value, bool multiline, bool password, bool isReadOnly);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createComboBoxElement")]
		internal static partial void CreateComboBoxElement(IntPtr handle, float x, float y, float width, float height, bool expanded, string? selectedValue);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListBoxElement")]
		internal static partial void CreateListBoxElement(IntPtr handle, float x, float y, float width, float height, bool multiselect);

		[JSImport("globalThis.Uno.UI.Runtime.Skia.SemanticElements.createListItemElement")]
		internal static partial void CreateListItemElement(IntPtr handle, float x, float y, float width, float height, bool selected, int positionInSet, int sizeOfSet);
	}
}
