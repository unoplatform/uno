#nullable enable

using DirectUI;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

internal class LightDismissOverlayHelper
{
	static bool IsOverlayVisibleForMode(LightDismissOverlayMode mode)
	{
		bool isOverlayVisible = false;

		if (mode == LightDismissOverlayMode.Auto)
		{
			isOverlayVisible = XboxUtility.IsOnXbox;
		}
		else
		{
			isOverlayVisible = (mode == LightDismissOverlayMode.On);
		}

		return isOverlayVisible;
	}

	internal static bool ResolveIsOverlayVisibleForControl<T>(T control)
		where T : class, IHasLightDismissOverlay
	{
		var overlayMode = control.LightDismissOverlayMode;

		return IsOverlayVisibleForMode(overlayMode);
	}
}
