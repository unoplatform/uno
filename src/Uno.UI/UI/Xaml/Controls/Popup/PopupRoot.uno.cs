namespace Microsoft.UI.Xaml.Controls.Primitives;

internal partial class PopupRoot
{
	public PopupRoot()
	{
		KeyDown += OnKeyDown;
		PointerPressed += OnPointerPressed;

		// See https://github.com/unoplatform/uno/issues/16358#issuecomment-2115276460
		// InputManager's unfocus-on-release isn't a WinUI thing, and must not run for a press that dismissed a popup.
		PointerReleased += (_, e) => e.Handled = true;
	}

	internal void CloseLightDismissablePopups()
	{
		foreach (var popup in GetPopupsInSafeClosingOrder())
		{
			if (popup.IsLightDismissEnabled)
			{
				if (popup.AssociatedFlyout is { } flyout)
				{
					flyout.Hide();
				}
				else
				{
					popup.IsOpen = false;
				}
			}
		}
	}
}
