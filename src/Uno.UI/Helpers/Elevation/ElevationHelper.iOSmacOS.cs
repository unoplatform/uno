namespace Uno.UI.Helpers;

#if __IOS__
using _View = UIKit.UIView;
#else
using _View = AppKit.NSView;
#endif

internal static class ElevationHelper
{
	internal static void SetElevation(DependencyObject element, double elevation, Color shadowColor, CGPath path = null)
	{
		if (element is not _View view)
		{
			return;
		}

		if (elevation > 0)
		{
			// Values for 1dp elevation according to https://material.io/guidelines/resources/shadows.html#shadows-illustrator
			const float x = 0.25f;
			const float y = 0.92f * 0.5f; // Looks more accurate than the recommended 0.92f.
			const float blur = 0.5f;

#if __MACOS__
			view.WantsLayer = true;
			view.Shadow ??= new AppKit.NSShadow();
#endif
			view.Layer.MasksToBounds = false;
			view.Layer.ShadowOpacity = shadowColor.A / 255f;
#if __MACOS__
			view.Layer.ShadowColor = AppKit.NSColor.FromRgb(shadowColor.R, shadowColor.G, shadowColor.B).CGColor;
#else
			view.Layer.ShadowColor = UIKit.UIColor.FromRGB(shadowColor.R, shadowColor.G, shadowColor.B).CGColor;
#endif
			view.Layer.ShadowRadius = (nfloat)(blur * elevation);
			view.Layer.ShadowOffset = new CoreGraphics.CGSize(x * elevation, y * elevation);
			view.Layer.ShadowPath = path;
		}
		else if (view.Layer != null)
		{
			view.Layer.ShadowOpacity = 0;
		}
	}
}
