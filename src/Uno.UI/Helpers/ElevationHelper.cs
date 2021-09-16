#if !WINDOWS_UWP && !WINDOWS
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using Uno.Extensions;
using Uno.Logging;
using Uno.UI.Helpers;

#if __IOS__ || __MACOS__
using CoreGraphics;
#endif

namespace Uno.UI.Helpers
{
	internal static class ElevationHelper
	{
#if __IOS__ || __MACOS__
		internal static void SetElevation(this DependencyObject element, double elevation, Color shadowColor, CGPath path = null)
#elif __SKIA__
		internal static void SetElevation(this DependencyObject element, double elevation, Color shadowColor, DependencyObject host = null, CornerRadius cornerRadius = default(CornerRadius))
#else
		internal static void SetElevation(this DependencyObject element, double elevation, Color shadowColor)
#endif
		{
#if __ANDROID__
			if (element is Android.Views.View view)
			{
				AndroidX.Core.View.ViewCompat.SetElevation(view, (float)Uno.UI.ViewHelper.LogicalToPhysicalPixels(elevation));
				if(Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
				{
					view.SetOutlineAmbientShadowColor(shadowColor);
					view.SetOutlineSpotShadowColor(shadowColor);
				}
			}
#elif __IOS__ || __MACOS__
#if __MACOS__
			if (element is AppKit.NSView view)
#else
			if (element is UIKit.UIView view)
#endif
			{
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
				else if(view.Layer != null)
				{
					view.Layer.ShadowOpacity = 0;
				}
			}
#elif __WASM__
			if (element is UIElement uiElement)
			{
				if (elevation > 0)
				{
					// Values for 1dp elevation according to https://material.io/guidelines/resources/shadows.html#shadows-illustrator
					const double x = 0.25d;
					const double y = 0.92f * 0.5f; // Looks more accurate than the recommended 0.92f.
					const double blur = 0.5f;
					var color = Color.FromArgb((byte)(shadowColor.A * .35), shadowColor.R, shadowColor.G, shadowColor.B);

					var str = $"{(x * elevation).ToStringInvariant()}px {(y * elevation).ToStringInvariant()}px {(blur * elevation).ToStringInvariant()}px {color.ToCssString()}";
					uiElement.SetStyle("box-shadow", str);
					uiElement.SetCssClasses("noclip");
				}
				else
				{
					uiElement.ResetStyle("box-shadow");
					uiElement.UnsetCssClasses("noclip");
				}
			}
#elif __SKIA__
			// TODO Uno: Not yet supported, no-op.
#endif
		}
	}
}
#endif
