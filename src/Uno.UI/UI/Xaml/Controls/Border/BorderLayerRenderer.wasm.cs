using System;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;

using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;
using Uno;
using Uno.UI.Helpers;
using Uno.UI.Extensions;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Shapes
{
	partial class BorderLayerRenderer
	{
		private Brush _background;
		private (Brush, Thickness, CornerRadius, Size) _border;

		private Action _backgroundChanged;

		public void UpdateLayer(
			UIElement element,
			Brush background,
			BackgroundSizing backgroundSizing,
			Thickness borderThickness,
			Brush borderBrush,
			CornerRadius cornerRadius,
			object image)
		{
			if (_background != background && element is FrameworkElement fwElt)
			{
				var oldValue = _background;
				_background = background;

				SetAndObserveBackgroundBrush(fwElt, oldValue, background, ref _backgroundChanged);
			}

			var renderSize = element.RenderSize;
			if (_border != (borderBrush, borderThickness, cornerRadius, renderSize))
			{
				_border = (borderBrush, borderThickness, cornerRadius, renderSize);
				SetBorder(element, borderThickness, borderBrush, cornerRadius);
			}
		}

		public static void SetBorder(UIElement element, Thickness thickness, Brush brush, CornerRadius cornerRadius)
		{
			if (thickness == Thickness.Empty)
			{
				element.SetStyle(
					("border-style", "none"),
					("border-color", ""),
					("border-width", ""));
			}
			else
			{
				var borderWidth = $"{thickness.Top.ToStringInvariant()}px {thickness.Right.ToStringInvariant()}px {thickness.Bottom.ToStringInvariant()}px {thickness.Left.ToStringInvariant()}px";
				switch (brush)
				{
					case SolidColorBrush solidColorBrush:
						var borderColor = solidColorBrush.ColorWithOpacity;
						element.SetStyle(
							("border", ""),
							("border-style", "solid"),
							("border-color", borderColor.ToHexString()),
							("border-width", borderWidth));
						break;
					case GradientBrush gradientBrush:
						var border = gradientBrush.ToCssString(element.RenderSize);
						element.SetStyle(
							("border-style", "solid"),
							("border-color", ""),
							("border-image", border),
							("border-width", borderWidth),
							("border-image-slice", "1"));
						break;
					case RadialGradientBrush radialGradientBrush:
						var radialBorder = radialGradientBrush.ToCssString(element.RenderSize);
						element.SetStyle(
							("border-style", "solid"),
							("border-color", ""),
							("border-image", radialBorder),
							("border-width", borderWidth),
							("border-image-slice", "1"));
						break;
					case AcrylicBrush acrylicBrush:
						var acrylicFallbackColor = acrylicBrush.FallbackColorWithOpacity;
						element.SetStyle(
							("border", ""),
							("border-style", "solid"),
							("border-color", acrylicFallbackColor.ToHexString()),
							("border-width", borderWidth));
						break;
					default:
						element.ResetStyle("border-style", "border-color", "border-image", "border-width");
						break;
				}
			}

			if (cornerRadius == CornerRadius.None)
			{
				element.ResetStyle("border-radius", "overflow");
			}
			else
			{
				var outer = cornerRadius.GetRadii(element.RenderSize, thickness).Outer;
				WindowManagerInterop.SetCornerRadius(element.HtmlId, outer.TopLeft.X, outer.TopLeft.Y, outer.TopRight.X, outer.TopRight.Y, outer.BottomRight.X, outer.BottomRight.Y, outer.BottomLeft.X, outer.BottomLeft.Y);
			}
		}

		public static void SetAndObserveBackgroundBrush(FrameworkElement element, Brush oldValue, Brush newValue, ref Action brushChanged)
		{
			if (oldValue is AcrylicBrush oldAcrylic)
			{
				AcrylicBrush.ResetStyle(element);
			}

			Action newOnInvalidateRender;

			if (newValue is ImageBrush imgBrush)
			{
				SetBackgroundBrush(element, newValue);

				RecalculateBrushOnSizeChanged(element, false);
				newOnInvalidateRender = () =>
				{
					if (imgBrush.ImageDataCache is not { } img)
					{
						return;
					}

					switch (img.Kind)
					{
						case ImageDataKind.Empty:
						case ImageDataKind.Error:
							element.ResetStyle("background-color", "background-image", "background-size");
							break;

						case ImageDataKind.DataUri:
						case ImageDataKind.Url:
						default:
							element.SetStyle(
								("background-color", ""),
								("background-origin", "content-box"),
								("background-position", imgBrush.ToCssPosition()),
								("background-size", imgBrush.ToCssBackgroundSize()),
								("background-image", "url(" + img.Value + ")"),
								("background-repeat", "no-repeat")
							);
							break;
					}
				};
			}
			else if (newValue is AcrylicBrush acrylicBrush)
			{
				SetBackgroundBrush(element, newValue);

				newOnInvalidateRender = () => acrylicBrush.Apply(element);
			}
			else
			{
				SetBackgroundBrush(element, newValue);
				if (newValue is not null)
				{
					newOnInvalidateRender = () => SetBackgroundBrush(element, newValue);
				}
				else
				{
					newOnInvalidateRender = null;
				}
			}

			if (newOnInvalidateRender is not null)
			{
				Brush.SetupBrushChanged(oldValue, newValue, ref brushChanged, newOnInvalidateRender);
			}
		}

		public static void SetBackgroundBrush(FrameworkElement element, Brush brush)
		{
			switch (brush)
			{
				case SolidColorBrush solidColorBrush:
					var color = solidColorBrush.ColorWithOpacity;
					WindowManagerInterop.SetElementBackgroundColor(element.HtmlId, color);
					RecalculateBrushOnSizeChanged(element, false);
					break;
				case GradientBrush gradientBrush:
					WindowManagerInterop.SetElementBackgroundGradient(element.HtmlId, gradientBrush.ToCssString(element.RenderSize));
					RecalculateBrushOnSizeChanged(element, true);
					break;
				case RadialGradientBrush radialGradientBrush:
					WindowManagerInterop.SetElementBackgroundGradient(element.HtmlId, radialGradientBrush.ToCssString(element.RenderSize));
					RecalculateBrushOnSizeChanged(element, true);
					break;
				case XamlCompositionBrushBase unsupportedCompositionBrush:
					var fallbackColor = unsupportedCompositionBrush.FallbackColorWithOpacity;
					WindowManagerInterop.SetElementBackgroundColor(element.HtmlId, fallbackColor);
					RecalculateBrushOnSizeChanged(element, false);
					break;
				default:
					WindowManagerInterop.ResetElementBackground(element.HtmlId);
					RecalculateBrushOnSizeChanged(element, false);
					break;
			}
		}

		private static readonly SizeChangedEventHandler _onSizeChangedForBrushCalculation = (sender, args) =>
		{
			var fe = sender as FrameworkElement;
			SetBackgroundBrush(fe, fe.Background);
		};

		private static void RecalculateBrushOnSizeChanged(FrameworkElement element, bool shouldRecalculate)
		{
			if (shouldRecalculate)
			{
				element.SizeChanged -= _onSizeChangedForBrushCalculation;
				element.SizeChanged += _onSizeChangedForBrushCalculation;
			}
			else
			{
				element.SizeChanged -= _onSizeChangedForBrushCalculation;
			}
		}

		internal void Clear()
		{
			_background = null;
			_border = default;

		}
	}
}
