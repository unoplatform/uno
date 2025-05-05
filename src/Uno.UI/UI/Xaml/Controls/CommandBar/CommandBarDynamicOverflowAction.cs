using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify whether items were added to or removed from the CommandBar overflow menu.
/// </summary>
public enum CommandBarDynamicOverflowAction
{
	/// <summary>
	/// Items are added to the overflow menu.
	/// </summary>
	AddingToOverflow = 0,

	/// <summary>
	/// Items are removed from the overflow menu.
	/// </summary>
	RemovingFromOverflow = 1,
}
