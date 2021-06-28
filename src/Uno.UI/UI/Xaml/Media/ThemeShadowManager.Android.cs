using Android.Graphics;
using Windows.UI.Xaml;

namespace Uno.UI.Xaml.Media
{
	internal static partial class ThemeShadowManager
	{
		static partial void UnsetShadow(UIElement uiElement)
		{
			if (uiElement is Android.Views.View view)
			{
				AndroidX.Core.View.ViewCompat.SetElevation(view, (float)0f);
			}
		}

		static partial void SetShadow(UIElement uiElement)
		{
			var translation = uiElement.Translation;

			if (uiElement is Android.Views.View view)
			{
				AndroidX.Core.View.ViewCompat.SetElevation(view, (float)Uno.UI.ViewHelper.LogicalToPhysicalPixels(translation.Z));
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
				{
					view.SetOutlineAmbientShadowColor(Color.Black);
					view.SetOutlineSpotShadowColor(Color.Black);
				}
			}
		}
	}
}
