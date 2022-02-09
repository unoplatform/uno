using System;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Color = Windows.UI.Color;

using RadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;
using Uno;
using Uno.UI.Helpers;

namespace Windows.UI.Xaml.Shapes
{
	partial class BorderLayerRenderer
	{
		private Brush _background;
		private (Brush, Thickness, CornerRadius, Size) _border;
		private (Brush, Thickness) _border;
		private CornerRadius _cornerRadius;

		private SerialDisposable _backgroundSubscription;
		private SerialDisposable _borderSubscription;

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

			SetBorder(element, borderBrush, borderThickness, cornerRadius);
		}

		public void SetBorder(UIElement element, Brush brush, Thickness thickness, CornerRadius cornerRadius)
		{
			var cornerRadiusChanged = cornerRadius != _cornerRadius;
			if (cornerRadius == CornerRadius.None)
			{
				element.ResetStyle("border-radius", "overflow");
			}
			else
			{
				var borderRadiusCssString = $"min(50%,{cornerRadius.TopLeft.ToStringInvariant()}px) min(50%,{cornerRadius.TopRight.ToStringInvariant()}px) min(50%,{cornerRadius.BottomRight.ToStringInvariant()}px) min(50%,{cornerRadius.BottomLeft.ToStringInvariant()}px)";
				element.SetStyle(
					("border-radius", borderRadiusCssString),
					("overflow", "hidden")); // overflow: hidden is required here because the clipping can't do its job when it's non-rectangular.
			}
			_cornerRadius = cornerRadius;

			var borderChanged = _border != (brush, thickness) ||
				(brush is LinearGradientBrush && cornerRadiusChanged); // Corner radius impacts linear gradient border rendering.
			if (!borderChanged)
			{
				return;
			}

			var subscription = _borderSubscription ??= new SerialDisposable();
			subscription.Disposable = null;
			subscription.Disposable = SetAndObserveBorderBrush(element, brush, thickness, cornerRadius);
		}

		private static void ApplySolidColor(UIElement element, Color color, string borderWidth)
		{
			element.SetSolidColorBorder(color.ToHexString(), borderWidth);
		}

		private static void ApplyGradient(UIElement element, GradientBrush gradient, string borderWidth)
		{
			var border = gradient.ToCssString(element.RenderSize); // TODO: Reevaluate when size is changing
			element.SetGradientBorder(border, borderWidth);
		}

		public IDisposable SetAndObserveBorderBrush(UIElement element, Brush brush, Thickness thickness, CornerRadius cornerRadius)
		{
			SetBorderBrush(element, brush, thickness, cornerRadius);
			return Brush.AssignAndObserveBrush(brush, _ => SetBorderBrush(element, brush, thickness, cornerRadius));
		}

		public static IDisposable SetAndObserveBackgroundBrush(FrameworkElement element, Brush brush)
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

		private void SetBorderBrush(UIElement element, Brush brush, Thickness thickness, CornerRadius cornerRadius)
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
						ApplySolidColor(element, solidColorBrush.ColorWithOpacity, borderWidth);
						break;
					case LinearGradientBrush linearGradientBrush:
						if (cornerRadius == CornerRadius.None ||
							!BorderGradientBrushHelper.CanApplySolidColorRendering(linearGradientBrush))
						{
							ApplyGradient(element, linearGradientBrush, borderWidth);
						}
						else
						{
							var majorStop = BorderGradientBrushHelper.GetMajorStop(linearGradientBrush);
							var borderColor = Color.FromArgb((byte)(linearGradientBrush.Opacity * majorStop.Color.A), majorStop.Color.R, majorStop.Color.G, majorStop.Color.B);

							ApplySolidColor(element, borderColor, borderWidth);
						}
						break;
					case GradientBrush gradientBrush:
						ApplyGradient(element, gradientBrush, borderWidth);
						break;
					case AcrylicBrush acrylicBrush:
						if (Brush.TryGetColorWithOpacity(acrylicBrush, out var acrylicFallbackColor))
						{
							ApplySolidColor(element, acrylicFallbackColor, borderWidth);
						}
						break;
					default:
						element.ResetStyle("border-style", "border-color", "border-image", "border-width");
						break;
				}
			}

			_border = (brush, thickness);
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

		public void SetCornerRadius(UIElement element, CornerRadius cornerRadius)
		{
			// Apply corner radius while reusing previous border properties.
			SetBorder(element, _border.Item1, _border.Item2, cornerRadius);
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
