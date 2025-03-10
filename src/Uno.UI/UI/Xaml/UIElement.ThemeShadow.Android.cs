using Android.Graphics;
using Uno.Disposables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml;

public partial class UIElement
{
	partial void UnsetShadow()
	{
		AndroidX.Core.View.ViewCompat.SetElevation(this, (float)0f);
	}

	partial void SetShadow()
	{
		AndroidX.Core.View.ViewCompat.SetElevation(this, Uno.UI.ViewHelper.LogicalToPhysicalPixels(Translation.Z) / 1.8f);
		if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
		{
			this.SetOutlineSpotShadowColor(Colors.Black.WithOpacity(0.5));
			this.SetOutlineAmbientShadowColor(Colors.Transparent);
		}
	}
}
