using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify the placement and visibility of an app bar button's label.
/// </summary>
public enum CommandBarLabelPosition
{
	/// <summary>
	/// The placement and visibility of the app bar button's label is determined by the value of the CommandBar.DefaultLabelPosition property.
	/// </summary>
	Default,

	/// <summary>
	/// The app bar button's label is always hidden whether the command bar is open or closed.
	/// </summary>
	Collapsed,
}
