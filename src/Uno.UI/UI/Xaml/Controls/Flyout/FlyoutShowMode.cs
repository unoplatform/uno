namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Defines constants that specify how a flyout behaves when shown.
/// </summary>
public enum FlyoutShowMode
{
	/// <summary>
	/// The show mode is determined automatically based on the method used to show the flyout.	
	/// </summary>
	Auto = 0,

	/// <summary>
	/// Behavior is typical of a flyout shown reactively, like a context menu. The open flyout takes focus. For a CommandBarFlyout, it opens in its expanded state.
	/// </summary>
	Standard = 1,

	/// <summary>
	/// Behavior is typical of a flyout shown proactively. The open flyout does not take focus. For a CommandBarFlyout, it opens in its collapsed state.
	/// </summary>
	Transient = 2,

	/// <summary>
	/// The flyout exhibits Transient behavior while the cursor is close to it, but is dismissed when the cursor moves away.
	/// </summary>
	TransientWithDismissOnPointerMoveAway = 3,
}
