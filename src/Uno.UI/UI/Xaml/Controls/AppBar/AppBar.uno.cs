#nullable enable

using DirectUI;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class AppBar : IBackButtonListener, IHasLightDismissOverlay
{
	bool IBackButtonListener.OnBackButtonPressed()
	{
		OnBackButtonPressedImpl(out var handled);
		return handled;
	}

	// Implemented explicitly: an implicit implementation would emit the public
	// LightDismissOverlayMode getter as a virtual/final interface slot, and the package API
	// diff reads that change of shape as the public WinUI getter having been removed.
	LightDismissOverlayMode IHasLightDismissOverlay.LightDismissOverlayMode => LightDismissOverlayMode;
}
