using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Hosting;
using Windows.UI.Xaml.Shapes;
using Uno.Extensions;
using Uno.Logging;

#if __IOS__ || __MACOS__
using CoreGraphics;
#endif

namespace Uno.UI.Toolkit
{
#if __IOS__
	[global::Foundation.PreserveAttribute(AllMembers = true)]
#elif __ANDROID__
	[Android.Runtime.PreserveAttribute(AllMembers = true)]
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
				new FrameworkPropertyMetadata(0, OnElevationChanged)
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

#if __IOS__
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor, CGPath path = null)
#elif NETFX_CORE
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor, DependencyObject host = null, CornerRadius cornerRadius = default(CornerRadius))
#else
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor)
#endif
		{
#if __ANDROID__
			if (element is Android.Views.View view)
			{
				AndroidX.Core.View.ViewCompat.SetElevation(view, (float)Uno.UI.ViewHelper.LogicalToPhysicalPixels(elevation));
			}
#elif __IOS__
			if (element is UIKit.UIView view)
			{
				if (elevation > 0)
				{
					// Values for 1dp elevation according to https://material.io/guidelines/resources/shadows.html#shadows-illustrator
					const float x = 0.25f;
					const float y = 0.92f * 0.5f; // Looks more accurate than the recommended 0.92f.
					const float blur = 0.5f;

					view.Layer.MasksToBounds = false;
					view.Layer.ShadowOpacity = shadowColor.A / 255f;
					view.Layer.ShadowColor = UIKit.UIColor.FromRGB(shadowColor.R, shadowColor.G, shadowColor.B).CGColor;
					view.Layer.ShadowRadius = (nfloat)(blur * elevation);
					view.Layer.ShadowOffset = new CoreGraphics.CGSize(x * elevation, y * elevation);
					view.Layer.ShadowPath = path;
				}
				else
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

					var str = $"{x * elevation}px {y * elevation}px {blur * elevation}px {shadowColor.ToCssString()}";
					uiElement.SetStyle("box-shadow", str);
				}
				else
				{
					uiElement.ResetStyle("box-shadow");
				}
			}
#elif NETFX_CORE
			if (element is UIElement uiElement)
			{
				SpriteVisual spriteVisual;
				Compositor compositor = ElementCompositionPreview.GetElementVisual(uiElement).Compositor;
				spriteVisual = compositor.CreateSpriteVisual();

				Vector2 newSize = new Vector2(0, 0);
				if (uiElement is FrameworkElement contentFE)
				{
					newSize = new Vector2((float)contentFE.ActualWidth, (float)contentFE.ActualHeight);
				}

				spriteVisual.Size = newSize;
				DropShadow shadow = compositor.CreateDropShadow();
				shadow.Offset = new Vector3(0, (float)elevation, -(float)elevation);

				// GetAlphaMask is only available for shapes, images, and textblocks
				if (uiElement is Shape shape)
				{
					shadow.Mask = shape.GetAlphaMask();
				}
				else if (uiElement is Image image)
				{
					shadow.Mask = image.GetAlphaMask();
				}
				else if (uiElement is TextBlock tb)
				{
					shadow.Mask = tb.GetAlphaMask();
				}

				if (!cornerRadius.Equals(default(CornerRadius)))
				{
					// Only one value for radius cnan be specified, using BottomLeft
					shadow.BlurRadius = (float)cornerRadius.BottomLeft;
				}

				shadow.Color = shadowColor;
				spriteVisual.Shadow = shadow;

				if (host != null && host is UIElement uiHost)
				{
					ElementCompositionPreview.SetElementChildVisual(uiHost, spriteVisual);
				}
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

			var property = uiElement.GetDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
			return property != null && uiElement.GetValue(property) is Thickness t ? t : default;
		}

		internal static bool SetPadding(this UIElement uiElement, Thickness padding)
		{
			if (uiElement is FrameworkElement fe && fe.TrySetPadding(padding))
			{
				return true;
			}

			var property = uiElement.GetDependencyPropertyUsingReflection<Thickness>("PaddingProperty");
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

		internal static DependencyProperty GetDependencyPropertyUsingReflection<TProperty>(this UIElement uiElement, string propertyName)
		{
			var type = uiElement.GetType();
			var propertyType = typeof(TProperty);
			var key = (ownerType: type, propertyName);

			_dependencyPropertyReflectionCache =
				_dependencyPropertyReflectionCache
				?? new Dictionary<(Type, string), DependencyProperty>(2);

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
				uiElement.Log().Warn($"The {propertyName} dependency property does not exist on {type}");
			}
#if !NETFX_CORE
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
