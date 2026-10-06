#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class ListViewBaseItem
{
	// Uno reuses detached containers, so a recycled item must not carry interaction
	// state or running chrome animations into its next data item.
	internal override void PrepareForRecycle()
	{
		ClearInteractionState();
		FlushChromeAnimations();

		base.PrepareForRecycle();
	}

	// Stops every running chrome storyboard and runs its completion action synchronously.
	private void FlushChromeAnimations()
	{
		// TODO Uno: forward to ListViewBaseItemPresenter.FlushChromeAnimations once the chrome is linked (C5).
	}
}
