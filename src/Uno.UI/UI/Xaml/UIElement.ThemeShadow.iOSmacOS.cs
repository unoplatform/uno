using Uno.Disposables;

namespace Windows.UI.Xaml;

public partial class UIElement
{
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
		view.Layer.ShadowOpacity = 0.1f;

#if __MACOS__
		view.Layer.ShadowColor = AppKit.NSColor.Black.CGColor;
#else
		view.Layer.ShadowColor = UIKit.UIColor.Black.CGColor;
#endif

		view.Layer.ShadowRadius = blur * translation.Z;
		view.Layer.ShadowOffset = new CoreGraphics.CGSize(x * translation.Z / 4, y * translation.Z / 4);
		if (view is Windows.UI.Xaml.Controls.Border border)
		{
			_boundsPathSubscription.Disposable = null;
			border.BorderRenderer.BoundsPathUpdated += Border_BoundsPathUpdated;
			_boundsPathSubscription.Disposable = Disposable.Create(() => border.BorderRenderer.BoundsPathUpdated -= Border_BoundsPathUpdated);
			view.Layer.ShadowPath = border.BorderRenderer.BoundsPath;
		}
	}

	private void Border_BoundsPathUpdated(object sender, global::System.EventArgs e)
	{
		SetShadow();
	}
}
