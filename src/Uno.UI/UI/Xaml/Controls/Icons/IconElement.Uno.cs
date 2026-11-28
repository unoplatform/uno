#nullable enable

namespace Microsoft.UI.Xaml.Controls;

partial class IconElement
{
	public IconElement()
	{
		// Lets theme resources set before the icon enters the tree re-resolve if the theme changed meanwhile.
		UpdateLastUsedTheme();
	}

	// Uno's replacement for the IconElement_Foreground cases of the WinUI OnPropertyChanged overrides.
	private protected virtual void OnForegroundChanged(DependencyPropertyChangedEventArgs e) { }

	internal override void UpdateThemeBindings(Data.ResourceUpdateReason updateReason)
	{
		base.UpdateThemeBindings(updateReason);

		UpdateLastUsedTheme();
	}

	internal override bool CanHaveChildren() => true;
}
