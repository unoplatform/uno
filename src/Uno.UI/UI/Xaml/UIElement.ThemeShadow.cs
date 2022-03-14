using Uno.Disposables;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
#if __ANDROID__
using Android.Graphics;
#endif

namespace Windows.UI.Xaml;

public partial class UIElement
{
	private void UpdateShadow()
	{
		if (Shadow == null || Translation.Z <= 0)
		{
			UnsetShadow();
		}
		else
		{
			SetShadow();
		}
	}

	partial void SetShadow();

	partial void UnsetShadow();

#if __ANDROID__
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
#endif

#if __IOS__ || __MACOS__
	private readonly SerialDisposable _boundsPathSubscription = new SerialDisposable();

	partial void UnsetShadow()
	{
		_boundsPathSubscription.Disposable = null;
		Layer.ShadowOpacity = 0;
	}

	partial void SetShadow()
	{
		var translation = Translation;

#if __MACOS__
		AppKit.NSView view = this;
#else
		UIKit.UIView view = this;
#endif
		// Values for 1dp elevation according to https://material.io/guidelines/resources/shadows.html#shadows-illustrator
		const float x = 0.25f;
		const float y = 0.92f * 0.5f; // Looks more accurate than the recommended 0.92f.
		const float blur = 0.5f;

#if __MACOS__
		view.WantsLayer = true;
		view.Shadow ??= new AppKit.NSShadow();
#endif
		view.Layer.MasksToBounds = false;
		view.Layer.ShadowOpacity = 1;

#if __MACOS__
		view.Layer.ShadowColor = AppKit.NSColor.Black.CGColor;
#else
		view.Layer.ShadowColor = UIKit.UIColor.Black.CGColor;
#endif

		view.Layer.ShadowRadius = blur * translation.Z;
		view.Layer.ShadowOffset = new CoreGraphics.CGSize(x * translation.Z, y * translation.Z);
		if (view is Border border)
		{
			_boundsPathSubscription.Disposable = null;
			border.BoundsPathUpdated += Border_BoundsPathUpdated;
			_boundsPathSubscription.Disposable = Disposable.Create(() => border.BoundsPathUpdated -= Border_BoundsPathUpdated);
			view.Layer.ShadowPath = border.BoundsPath;
		}
	}

	private void Border_BoundsPathUpdated(object sender, global::System.EventArgs e)
	{
		SetShadow();
	}
#endif

#if __SKIA__
		partial void UnsetShadow(UIElement uiElement) => uiElement.Visual.HasThemeShadow = false;

		partial void SetShadow(UIElement uiElement) => uiElement.Visual.HasThemeShadow = true;
#endif

#if __WASM__
		partial void UnsetShadow(UIElement uiElement)
		{
			uiElement.SetStyle("box-shadow", "unset");
		}

		partial void SetShadow(UIElement uiElement)
		{
			var translation = uiElement.Translation;
			var boxShadowValue = CreateBoxShadow(translation.Z);
			uiElement.SetStyle("box-shadow", boxShadowValue);
		}

		private static string CreateBoxShadow(float translationZ)
		{
			var z = (int)translationZ;
			var halfZ = z / 2;
			var quarterZ = z / 4;
			return $"{quarterZ}px {quarterZ}px {halfZ}px 0px rgba(0,0,0,0.3)";
		} 
#endif
}
