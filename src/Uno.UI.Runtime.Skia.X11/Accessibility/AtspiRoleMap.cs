#nullable enable

using Microsoft.UI.Xaml.Automation.Peers;

namespace Uno.WinUI.Runtime.Skia.X11;

/// <summary>
/// Maps WinUI <see cref="AutomationControlType"/> values to the numeric AT-SPI role
/// ids defined by at-spi2-core's atspi-constants.h. libatspi derives the displayed
/// role name from the numeric <c>GetRole</c> value, so the ids must match the real
/// <c>AtspiRole</c> enum exactly. Unknown or unmapped control types fall back to
/// <c>ATSPI_ROLE_PANEL</c>, which is a safe generic container role.
/// </summary>
internal static class AtspiRoleMap
{
	/// <summary>
	/// Resolves the AT-SPI role id and name for a WinUI automation control type.
	/// </summary>
	public static (uint Id, string Name) GetRole(AutomationControlType controlType) => controlType switch
	{
		AutomationControlType.Button => (43u, "push button"),      // ATSPI_ROLE_PUSH_BUTTON
		AutomationControlType.Edit => (79u, "entry"),              // ATSPI_ROLE_ENTRY
		AutomationControlType.CheckBox => (7u, "check box"),       // ATSPI_ROLE_CHECK_BOX
		AutomationControlType.RadioButton => (44u, "radio button"),// ATSPI_ROLE_RADIO_BUTTON
		AutomationControlType.Slider => (51u, "slider"),           // ATSPI_ROLE_SLIDER
		AutomationControlType.ComboBox => (11u, "combo box"),      // ATSPI_ROLE_COMBO_BOX
		AutomationControlType.Text => (29u, "label"),              // ATSPI_ROLE_LABEL
		AutomationControlType.List => (31u, "list"),               // ATSPI_ROLE_LIST
		AutomationControlType.ListItem => (32u, "list item"),      // ATSPI_ROLE_LIST_ITEM
		AutomationControlType.Image => (27u, "image"),             // ATSPI_ROLE_IMAGE
		AutomationControlType.Hyperlink => (88u, "link"),          // ATSPI_ROLE_LINK
		AutomationControlType.ScrollBar => (48u, "scroll bar"),    // ATSPI_ROLE_SCROLL_BAR
		AutomationControlType.ProgressBar => (42u, "progress bar"),// ATSPI_ROLE_PROGRESS_BAR
		AutomationControlType.TabItem => (37u, "page tab"),        // ATSPI_ROLE_PAGE_TAB
		AutomationControlType.Tab => (38u, "page tab list"),       // ATSPI_ROLE_PAGE_TAB_LIST
		_ => (39u, "panel"),                                       // ATSPI_ROLE_PANEL
	};
}
