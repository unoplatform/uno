using Uno.UI.DirectUI.Components;

namespace Windows.UI.Xaml.Controls;

internal static class LightDismissOverlayHelper
{
	internal static bool IsOverlayVisibleForMode(LightDismissOverlayMode mode)
	{
		bool isOverlayVisible;
		if (mode == LightDismissOverlayMode.Auto)
		{
			isOverlayVisible = XboxUtility.IsOnXbox();
		}
		else
		{
			isOverlayVisible = (mode == LightDismissOverlayMode.On);
		}

		return isOverlayVisible;
	}

	/// <summary>
	/// In case of WinUI, this is a template function, so it expects a control
	/// with LightDismissOverlayMode method. In our case, we pass in the mode directly instead.
	/// </summary>
	/// <param name="controlMode">Control light dismiss mode.</param>
	/// <returns>Is overlay visible?</returns>
	internal static bool ResolveIsOverlayVisibleForControl(LightDismissOverlayMode controlMode)
	{
		var overlayMode = controlMode;

		var isOverlayVisible = IsOverlayVisibleForMode(overlayMode);

		return isOverlayVisible;
	}
}
