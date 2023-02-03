#if !HAS_UNO && !HAS_UNO_WINUI
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

namespace Uno.UI.Helpers
{
	internal static class ElevationHelper
	{
		internal static void SetElevation(this DependencyObject element, double elevation, Color shadowColor, DependencyObject host = null, CornerRadius cornerRadius = default(CornerRadius))
		{
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
					// Values for 1dp elevation according to https://material.io/guidelines/resources/shadows.html#shadows-illustrator
					const float x = 0.25f;
					const float y = 0.92f * 0.5f; // Looks more accurate than the recommended 0.92f.
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
					shadow.Opacity = shadowColor.A / 255f;
					spriteVisual.Shadow = shadow;
				}

				ElementCompositionPreview.SetElementChildVisual(uiHost, spriteVisual);
			}
		}
	}
}
#endif
