using System;
using System.Numerics;
using Windows.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;

#if NETCOREAPP && !HAS_UNO
using Microsoft.UI;
#endif

#if __SKIA__
using Uno.UI.Composition.Composition;
#endif

namespace Uno.UI.Xaml
{
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

#if (WINAPPSDK || WINDOWS_UWP || NETCOREAPP) && !HAS_UNO
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor, DependencyObject host = null, CornerRadius cornerRadius = default(CornerRadius))
#else
		internal static void SetElevationInternal(this DependencyObject element, double elevation, Color shadowColor)
#endif
		{
#if __SKIA__
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
	}
}
