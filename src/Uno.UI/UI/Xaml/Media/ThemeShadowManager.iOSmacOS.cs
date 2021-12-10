using System;
using Windows.UI.Xaml;

#if NET6_0_OR_GREATER
using ObjCRuntime;
#endif

namespace Uno.UI.Xaml.Media
{
	internal static partial class ThemeShadowManager
	{
		static partial void UnsetShadow(UIElement uiElement)
		{
#if __MACOS__
			if (uiElement is AppKit.NSView view)
#else
			if (uiElement is UIKit.UIView view)
#endif
			{
				view.Layer.ShadowOpacity = 0;
			}
		}

		static partial void SetShadow(UIElement uiElement)
		{
			var translation = uiElement.Translation;

#if __MACOS__
			if (uiElement is AppKit.NSView view)
#else
			if (uiElement is UIKit.UIView view)
#endif
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
				view.Layer.ShadowOpacity = 1;
#if __MACOS__
				view.Layer.ShadowColor = AppKit.NSColor.Black.CGColor;
#else
				view.Layer.ShadowColor = UIKit.UIColor.Black.CGColor;
#endif
				view.Layer.ShadowRadius = (nfloat)(blur * translation.Z);
				view.Layer.ShadowOffset = new CoreGraphics.CGSize(x * translation.Z, y * translation.Z);
				//view.Layer.ShadowPath = path; - needed for rounded corners?
			}
		}
	}
}
