// MUX Reference TeachingTip.idl, commit de78834

#nullable enable

namespace Microsoft.UI.Xaml.Controls;

/// <summary>
/// Defines constants that specify whether a teaching tip's Tail is visible or collapsed.
/// </summary>
public enum TeachingTipTailVisibility
{
	/// <summary>
	/// The teaching tip's tail is collapsed when non-targeted and visible when the targeted.
	/// </summary>
	Auto,

	/// <summary>
	/// The teaching tip's tail is visible.
	/// </summary>
	Visible,

	/// <summary>
	/// The teaching tip's tail is collapsed.
	/// </summary>
	Collapsed,
}
