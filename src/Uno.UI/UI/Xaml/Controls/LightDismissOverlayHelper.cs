using Microsoft.UI.Xaml.Controls;
using Uno.UI.Helpers.WinUI;
namespace Uno.UI.UI.Xaml.Controls;

internal static class LightDismissOverlayHelper
{
	public static bool IsOverlayVisibleForMode(LightDismissOverlayMode mode)
	{
		bool isOverlayVisible = false;

		if (mode == LightDismissOverlayMode.Auto)
		{
			isOverlayVisible = SharedHelpers.IsOnXbox();
		}
		else
		{
			isOverlayVisible = (mode == LightDismissOverlayMode.On);
		}

		return isOverlayVisible;
	}

	// Controls that call this should have a get_LightDismissOverlayMode() method.
	public static bool ResolveIsOverlayVisibleForControl(Control control)
	{
		// Uno Specific: originally, this is done using C++ templates.
		var overlayMode = (LightDismissOverlayMode)control.GetType().GetProperty("LightDismissOverlayMode")!.GetValue(control)!;

		return IsOverlayVisibleForMode(overlayMode);
	}
}
