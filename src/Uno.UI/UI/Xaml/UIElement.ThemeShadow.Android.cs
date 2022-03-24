using Uno.Disposables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

#if __ANDROID__
using Android.Graphics;
#endif

namespace Windows.UI.Xaml;

public partial class UIElement
{
	partial void UnsetShadow()
	{
		AndroidX.Core.View.ViewCompat.SetElevation(this, (float)0f);
	}

	partial void SetShadow()
	{
		var translation = uiElement.Translation;

		AndroidX.Core.View.ViewCompat.SetElevation(this, (float)Uno.UI.ViewHelper.LogicalToPhysicalPixels(translation.Z));
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
		{
			this.SetOutlineAmbientShadowColor(Color.Black);
			this.SetOutlineSpotShadowColor(Color.Black);
		}
	}
}
