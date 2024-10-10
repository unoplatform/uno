using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using RadialGradientBrush = Microsoft/* UWP don't rename */.UI.Xaml.Media.RadialGradientBrush;
using Uno;
using Uno.UI.Helpers;
using Uno.UI.Extensions;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Xaml.Controls;

partial class BorderLayerRenderer
{
	private Action _backgroundChanged;
	private Action _borderChanged;

	partial void UpdatePlatform()
	{
		var newState = new BorderLayerState(
			new Size(_owner.RenderSize.Width, _owner.RenderSize.Height),
			_borderInfoProvider.Background,
			_borderInfoProvider.BackgroundSizing,
			_borderInfoProvider.BorderBrush,
			_borderInfoProvider.BorderThickness,
			_borderInfoProvider.CornerRadius);
		var previousLayoutState = _currentState;
		if (!newState.Equals(previousLayoutState))
		{
			if (previousLayoutState.Background != newState.Background && _owner is FrameworkElement fwElt)
			{
				SetAndObserveBackgroundBrush(newState, ref _backgroundChanged);
			}

			if (previousLayoutState.BackgroundSizing != newState.BackgroundSizing)
			{
				_owner.SetStyle("background-clip", newState.BackgroundSizing == BackgroundSizing.InnerBorderEdge ? "padding-box" : "border-box");
			}

			if ((previousLayoutState.BorderBrush, previousLayoutState.BorderThickness, previousLayoutState.CornerRadius, previousLayoutState.ElementSize) !=
				(newState.BorderBrush, newState.BorderThickness, newState.CornerRadius, newState.ElementSize))
			{
				SetAndObserveBorder(newState);
			}

			_currentState = newState;
		}
	}

	private void SetAndObserveBorder(BorderLayerState newState)
	{
		SetBorder(newState);
		Brush.SetupBrushChanged(
			_currentState.BorderBrush,
			newState.BorderBrush,
			ref _borderChanged,
			() =>
			{
				SetBorder(newState);
			});
	}

	private void SetBorder(BorderLayerState newState)
	{
		var thickness = newState.BorderThickness;
		var brush = newState.BorderBrush;
		var cornerRadius = newState.CornerRadius;
		var element = _owner;
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
					ApplySolidColorBorder(element, borderWidth, solidColorBrush.ColorWithOpacity);
					break;
				case GradientBrush gradientBrush when gradientBrush.CanApplyToBorder(cornerRadius):
					var border = gradientBrush.ToCssString(element.RenderSize);
					element.SetStyle(
						("border-style", "solid"),
						("border-color", ""),
						("border-image", border),
						("border-width", borderWidth),
						("border-image-slice", "1"));
					break;
				case GradientBrush unsupportedGradientBrush:
					ApplySolidColorBorder(element, borderWidth, unsupportedGradientBrush.FallbackColorWithOpacity);
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

		SetCornerRadius(_owner, cornerRadius, thickness);
	}

	private static void ApplySolidColorBorder(FrameworkElement element, string borderWidth, Color borderColor)
	{
		element.SetStyle(
			("border", ""),
			("border-style", "solid"),
			("border-color", borderColor.ToHexString()),
			("border-width", borderWidth));
	}

	internal static void SetCornerRadius(
		FrameworkElement element,
		CornerRadius cornerRadius,
		Thickness borderThickness)
	{
		if (cornerRadius == CornerRadius.None)
		{
			element.ResetStyle("border-radius", "overflow");
		}
		else
		{
			var outer = cornerRadius.GetRadii(element.RenderSize, borderThickness).Outer;
			WindowManagerInterop.SetCornerRadius(element.HtmlId, outer.TopLeft.X, outer.TopLeft.Y, outer.TopRight.X, outer.TopRight.Y, outer.BottomRight.X, outer.BottomRight.Y, outer.BottomLeft.X, outer.BottomLeft.Y);
		}
	}

	private void SetAndObserveBackgroundBrush(BorderLayerState newState, ref Action brushChanged)
	{
		var oldValue = _currentState.Background;
		var newValue = newState.Background;
		var element = _owner;
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

	private static void SetBackgroundBrush(FrameworkElement element, Brush brush)
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
}
