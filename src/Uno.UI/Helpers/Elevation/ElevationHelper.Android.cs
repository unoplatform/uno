namespace Uno.UI.Helpers;

internal static class ElevationHelper
{
	internal static void SetElevation(DependencyObject element, double elevation, Color shadowColor)
	{
		if (element is not Android.Views.View view)
		{
			return;
		}

		AndroidX.Core.View.ViewCompat.SetElevation(view, (float)Uno.UI.ViewHelper.LogicalToPhysicalPixels(elevation));
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
		{
			view.SetOutlineAmbientShadowColor(shadowColor);
			view.SetOutlineSpotShadowColor(shadowColor);
		}
	}
}
