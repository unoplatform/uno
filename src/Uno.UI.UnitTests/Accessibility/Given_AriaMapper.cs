#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.Runtime.Skia;

namespace Uno.UI.Tests.Accessibility;

/// <summary>
/// Guards the control-type to ARIA role table, which is built once and frozen: the lookup must keep
/// returning the same roles, and control types deliberately left out of the table must keep
/// resolving to no role at all.
/// </summary>
[TestClass]
public class Given_AriaMapper
{
	[TestMethod]
	[DataRow(AutomationControlType.Button, "button")]
	[DataRow(AutomationControlType.CheckBox, "checkbox")]
	[DataRow(AutomationControlType.RadioButton, "radio")]
	[DataRow(AutomationControlType.Edit, "textbox")]
	[DataRow(AutomationControlType.ComboBox, "combobox")]
	[DataRow(AutomationControlType.List, "listbox")]
	[DataRow(AutomationControlType.ListItem, "option")]
	[DataRow(AutomationControlType.Hyperlink, "link")]
	[DataRow(AutomationControlType.Header, "heading")]
	[DataRow(AutomationControlType.DataGrid, "grid")]
	[DataRow(AutomationControlType.Window, "dialog")]
	// Thumb and Slider intentionally share the same role.
	[DataRow(AutomationControlType.Slider, "slider")]
	[DataRow(AutomationControlType.Thumb, "slider")]
	[DataRow(AutomationControlType.Custom, "generic")]
	public void When_Mapped_ControlType(AutomationControlType controlType, string expectedRole)
		=> Assert.AreEqual(expectedRole, AriaMapper.GetAriaRole(controlType));

	[TestMethod]
	// Plain text must carry no explicit role (the ARIA "label" role is for labelling form elements).
	[DataRow(AutomationControlType.Text)]
	[DataRow(AutomationControlType.Calendar)]
	[DataRow(AutomationControlType.Separator)]
	public void When_Unmapped_ControlType(AutomationControlType controlType)
		=> Assert.IsNull(AriaMapper.GetAriaRole(controlType));
}
