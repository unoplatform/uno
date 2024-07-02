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

#if HAS_UNO
using Uno.Extensions;
using Uno.Foundation.Logging;
#endif

#if NETCOREAPP && !HAS_UNO
using Microsoft.UI;
#endif

#if __IOS__ || __MACOS__
using CoreGraphics;
#endif

#if __IOS__ || __MACOS__
using ObjCRuntime;
#endif

#if __SKIA__
using Uno.UI.Composition.Composition;
#endif

namespace Uno.UI.Toolkit
{
#if !NET6_0_OR_GREATER // Moved to the linker definition file
#if __IOS__
	[global::Foundation.PreserveAttribute(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.PreserveAttribute(AllMembers = true)]
#endif
#endif
	public static class UIElementExtensions
	{
		#region Elevation

		public static void SetElevation(this UIElement element, double elevation)
		{
			element.SetValue(ElevationProperty, elevation);
		}

		public static double GetElevation(this UIElement element)
		{
			return (double)element.GetValue(ElevationProperty);
		}

		public static DependencyProperty ElevationProperty { get; } =
			DependencyProperty.RegisterAttached(
				"Elevation",
				typeof(double),
				typeof(UIElementExtensions),
				new PropertyMetadata(0, OnElevationChanged)
			);

		private static readonly Color ElevationColor = Color.FromArgb(64, 0, 0, 0);

		private static void OnElevationChanged(DependencyObject dependencyObject,
			DependencyPropertyChangedEventArgs args)
		{
			if (args.NewValue is double elevation)
			{
				SetElevationInternal(dependencyObject, elevation, ElevationColor);
			}
		}

#if __IOS__ || __MACOS__
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor, CGPath path = null)
#elif (WINAPPSDK || WINDOWS_UWP || NETCOREAPP) && !HAS_UNO
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor, DependencyObject host = null, CornerRadius cornerRadius = default(CornerRadius))
#else
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor)
#endif
		{
#if __ANDROID__
			if (element is Android.Views.View view)
			{
				AndroidX.Core.View.ViewCompat.SetElevation(view, (float)Uno.UI.ViewHelper.LogicalToPhysicalPixels(elevation));
				if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.P)
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
					const float x = 0.28f;
					const float y = 0.92f * 0.5f;
					const float blur = 0.18f;

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
#elif __WASM__
			if (element is UIElement uiElement)
			{
				if (elevation > 0)
				{
					const double x = 0.25d;
					const double y = 0.92f * 0.5f;
					const double blur = 0.3f;

					var str = $"{(x * elevation).ToStringInvariant()}px {(y * elevation).ToStringInvariant()}px {(blur * elevation).ToStringInvariant()}px {shadowColor.ToCssString()}";
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
			if (element is UIElement uiElement)
			{
				var visual = uiElement.Visual;
				const float x = 0.28f;
				const float y = 0.92f * 0.5f;
				const float blur = 0.18f;

				var dx = (float)elevation * x;
				var dy = (float)elevation * y;
				var sigmaX = (float)(blur * elevation);
				var sigmaY = (float)(blur * elevation);
				var shadow = new ShadowState(dx, dy, sigmaX, sigmaY, shadowColor);
				visual.ShadowState = shadow;
			}
#elif (WINAPPSDK || WINDOWS_UWP || NETCOREAPP) && !HAS_UNO
			if (element is UIElement uiElement)
			{
				var compositor = ElementCompositionPreview.GetElementVisual(uiElement).Compositor;
				var spriteVisual = compositor.CreateSpriteVisual();

				var newSize = new Vector2(0, 0);
				if (uiElement is FrameworkElement contentFE)
				{
					newSize = new Vector2((float)contentFE.ActualWidth, (float)contentFE.ActualHeight);
				}

				if (!(host is Canvas uiHost) || newSize == default)
				{
					return;
				}

				spriteVisual.Size = newSize;
				if (elevation > 0)
				{
					const float x = 0.25f;
					const float y = 0.92f * 0.5f;
					const float blur = 0.5f;

					var shadow = compositor.CreateDropShadow();
					shadow.Offset = new Vector3((float)elevation * x, (float)elevation * y, -(float)elevation);
					shadow.BlurRadius = (float)(blur * elevation);

					shadow.Mask = uiElement switch
					{
						// GetAlphaMask is only available for shapes, images, and textblocks
						Shape shape => shape.GetAlphaMask(),
						Image image => image.GetAlphaMask(),
						TextBlock tb => tb.GetAlphaMask(),
						_ => shadow.Mask
					};

					if (!cornerRadius.Equals(default))
					{
						var averageRadius =
							(cornerRadius.TopLeft +
							cornerRadius.TopRight +
							cornerRadius.BottomLeft +
							cornerRadius.BottomRight) / 4f;

						// Create a rectangle with similar corner radius (average for now)
						var rect = new Rectangle()
						{
							Fill = new SolidColorBrush(Colors.White),
							Width = newSize.X,
							Height = newSize.Y,
							RadiusX = averageRadius,
							RadiusY = averageRadius
						};

						uiHost.Children.Add(rect); // The rect need to be in th VisualTree for .GetAlphaMask() to work

						shadow.Mask = rect.GetAlphaMask();

						uiHost.Children.Remove(rect); // No need anymore, we can discard it.
					}

					shadow.Color = shadowColor;
					spriteVisual.Shadow = shadow;
				}

				ElementCompositionPreview.SetElementChildVisual(uiHost, spriteVisual);
			}
#endif
		}

		#endregion

		internal static Thickness GetPadding(this UIElement uiElement)
		{
			if (uiElement is FrameworkElement fe && fe.TryGetPadding(out var padding))
			{
				return padding;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
			return property != null && uiElement.GetValue(property) is Thickness t ? t : default;
		}

		internal static bool SetPadding(this UIElement uiElement, Thickness padding)
		{
			if (uiElement is FrameworkElement fe && fe.TrySetPadding(padding))
			{
				return true;
			}

			var property = uiElement.FindDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
			if (property != null)
			{
				uiElement.SetValue(property, padding);
				return true;
			}

			return false;
		}

		internal static bool TryGetPadding(this FrameworkElement frameworkElement, out Thickness padding)
		{
			switch (frameworkElement)
			{
				case Grid g:
					padding = g.Padding;
					return true;

				case StackPanel sp:
					padding = sp.Padding;
					return true;

				case Control c:
					padding = c.Padding;
					return true;

				case ContentPresenter cp:
					padding = cp.Padding;
					return true;

				case Border b:
					padding = b.Padding;
					return true;
			}

			padding = default;
			return false;
		}

		internal static bool TrySetPadding(this FrameworkElement frameworkElement, Thickness padding)
		{
			switch (frameworkElement)
			{
				case Grid g:
					g.Padding = padding;
					return true;

				case StackPanel sp:
					sp.Padding = padding;
					return true;

				case Control c:
					c.Padding = padding;
					return true;

				case ContentPresenter cp:
					cp.Padding = padding;
					return true;

				case Border b:
					b.Padding = padding;
					return true;
			}

			return false;
		}

		private static Dictionary<(Type type, string property), DependencyProperty> _dependencyPropertyReflectionCache;

		internal static DependencyProperty FindDependencyPropertyUsingReflection<TProperty>(this UIElement uiElement, string propertyName)
		{
			var type = uiElement.GetType();
			var propertyType = typeof(TProperty);
			var key = (ownerType: type, propertyName);

			_dependencyPropertyReflectionCache ??= new Dictionary<(Type, string), DependencyProperty>(2);

			if (_dependencyPropertyReflectionCache.TryGetValue(key, out var property))
			{
				return property;
			}

			property =
				type
					.GetTypeInfo()
					.GetDeclaredProperty(propertyName)
					?.GetValue(null) as DependencyProperty
				?? type
					.GetTypeInfo()
					.GetDeclaredField(propertyName)
					?.GetValue(null) as DependencyProperty;

			if (property == null)
			{
#if HAS_UNO
				uiElement.Log().Warn($"The {propertyName} dependency property does not exist on {type}");
#endif
			}
#if HAS_UNO
			else if (property.Type != propertyType)
			{
				uiElement.Log().Warn($"The {propertyName} dependency property {type} is not of the {propertyType} Type.");
				property = null;
			}
#endif

			_dependencyPropertyReflectionCache[key] = property;

			return property;
		}
	}
}
