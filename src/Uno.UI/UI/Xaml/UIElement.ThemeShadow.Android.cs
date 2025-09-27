using Android.Graphics;
using Uno.Disposables;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml;

public partial class UIElement
{
	partial void UnsetShadow()
	{
		AndroidX.Core.View.ViewCompat.SetElevation(this, (float)0f);
	}

	partial void SetShadow()
	{
		AndroidX.Core.View.ViewCompat.SetElevation(this, Uno.UI.ViewHelper.LogicalToPhysicalPixels(Translation.Z) / 1.8f);
		if (ABuild.VERSION.SdkInt >= ABuildVersionCodes.P)
		{
			this.SetOutlineSpotShadowColor(Colors.Black.WithOpacity(0.5));
			this.SetOutlineAmbientShadowColor(Colors.Transparent);
		}
	}
}
