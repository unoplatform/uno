using System;
using System.Linq;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

partial class BorderLayerRenderer
{
	private Brush _background;
	private (Brush, Thickness) _border;
	private CornerRadius _cornerRadius;

	private SerialDisposable _backgroundSubscription;

	partial void UpdateLayer()
	{
		if (_background != _borderInfoProvider.Background)
		{
			_background = _borderInfoProvider.Background;
			var subscription = _backgroundSubscription ??= new SerialDisposable();

			subscription.Disposable = null;
			subscription.Disposable = SetAndObserveBackgroundBrush(fwElt, _borderInfoProvider.Background);
		}

		if (_border != (_borderInfoProvider.BorderBrush, _borderInfoProvider.BorderThickness))
		{
			_border = (_borderInfoProvider.BorderBrush, _borderInfoProvider.BorderThickness);
			SetBorder(_owner, _borderInfoProvider.BorderThickness, _borderInfoProvider.BorderBrush);
		}

		if (_cornerRadius != _borderInfoProvider.CornerRadius)
		{
			_cornerRadius = _borderInfoProvider.CornerRadius;
			SetCornerRadius(_owner, _borderInfoProvider.CornerRadius);
		}
	}

	partial void ClearLayer()
	{
		//TODO:MZ
	}

	public static void SetCornerRadius(UIElement element, CornerRadius cornerRadius)
	{
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
	}

	public static void SetBorder(UIElement element, Thickness thickness, Brush brush)
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
					var border = gradientBrush.ToCssString(element.RenderSize); // TODO: Reevaluate when size is changing
					element.SetStyle(
						("border-style", "solid"),
						("border-color", ""),
						("border-image", border),
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
	}

	public static IDisposable SetAndObserveBackgroundBrush(FrameworkElement element, Brush brush)
	{
		SetBackgroundBrush(element, brush);

		if (brush is ImageBrush imgBrush)
		{
			RecalculateBrushOnSizeChanged(element, false);
			return imgBrush.Subscribe(img =>
			{
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
			});
		}
		else if (brush is AcrylicBrush acrylicBrush)
		{
			return acrylicBrush.Subscribe(element);
		}
		else
		{
			return Brush.AssignAndObserveBrush(brush, _ => SetBackgroundBrush(element, brush));
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
}
