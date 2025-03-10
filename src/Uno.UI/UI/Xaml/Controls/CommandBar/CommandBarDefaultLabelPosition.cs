using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the placement and visibility of AppBarButton labels in a CommandBar.
/// </summary>
public enum CommandBarDefaultLabelPosition
{
	/// <summary>
	/// App bar button labels are shown below the icon. Labels are visible only when the command bar is open.
	/// </summary>
	Bottom = 0,

	/// <summary>
	/// App bar button labels are shown to the right of the icon. Labels are visible even when the command bar is closed.
	/// </summary>
	Right = 1,

	/// <summary>
	/// App bar button labels are always hidden whether the command bar is open or closed.
	/// </summary>
	Collapsed = 2,
}
