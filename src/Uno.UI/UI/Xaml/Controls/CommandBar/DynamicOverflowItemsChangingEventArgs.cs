using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Provides data for the CommandBar.DynamicOverflowItemsChanging event.
/// </summary>
public partial class DynamicOverflowItemsChangingEventArgs
{
	/// <summary>
	/// Gets a value that indicates whether items were added to or removed from the CommandBar overflow menu.
	/// </summary>
	public CommandBarDynamicOverflowAction Action { get; internal set; }
}
