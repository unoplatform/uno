using System;
using Microsoft.UI.Xaml.Media;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class BorderLayerRenderer
{
	private readonly SerialDisposable _backgroundSubscription = new();

	private bool _resetBackgroundOnSizeChange;
	private bool _resetBorderOnSizeChange;

	partial void UpdateLayer()
	{
		var newState = new BorderLayerState(
			new Size(_owner.ActualWidth, _owner.ActualHeight),
			_borderInfoProvider);

		var sizeChanged = _currentState.ElementSize != newState.ElementSize;

		var currentBackgroundState = (_currentState.Background, _currentState.BackgroundSizing);
		var newBackgroundState = (newState.Background, newState.BackgroundSizing);

		if ((_resetBackgroundOnSizeChange && sizeChanged) ||
			currentBackgroundState != newBackgroundState)
		{
			_backgroundSubscription.Disposable = null;
			_backgroundSubscription.Disposable = SetAndObserveBackgroundBrush(_owner, _borderInfoProvider.Background);
		}

		var currentBorderState = (_currentState.BorderBrush, _currentState.BorderThickness, _currentState.CornerRadius);
		var newBorderState = (newState.BorderBrush, newState.BorderThickness, newState.CornerRadius);

		if ((_resetBorderOnSizeChange && sizeChanged) ||
			currentBorderState != newBorderState)
		{
			SetBorder(_owner, newBorderState.BorderThickness, newBorderState.BorderBrush, newBorderState.CornerRadius);
		}

		if (_currentState.CornerRadius != newState.CornerRadius)
		{
			SetCornerRadius(_owner, newState.CornerRadius);
		}

		_currentState = newState;
	}

	partial void ClearLayer()
	{
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

	private void SetBorder(UIElement element, Thickness thickness, Brush brush, CornerRadius cornerRadius)
	{
		_resetBorderOnSizeChange = false;
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
					element.SetSolidColorBorder(borderColor, borderWidth);
					break;
				case GradientBrush gradientBrush:
					if (gradientBrush is LinearGradientBrush linearGradientBrush &&
						cornerRadius != CornerRadius.None &&
						linearGradientBrush.CanApplySolidColorRendering())
					{
						var majorStopBrush = linearGradientBrush.MajorStopBrush;
						element.SetSolidColorBorder(majorStopBrush?.Color ?? Colors.Transparent, borderWidth);
					}
					else
					{
						_resetBorderOnSizeChange = true;
						var border = gradientBrush.ToCssString(element.RenderSize);
						element.SetGradientBorder(border, borderWidth);
					}
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

	private IDisposable SetAndObserveBackgroundBrush(FrameworkElement element, Brush brush)
	{
		_resetBackgroundOnSizeChange = false;
		WindowManagerInterop.ResetElementBackground(element.HtmlId);

		if (brush is ImageBrush imgBrush)
		{
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
			acrylicBrush.Apply(element);
		}
		else if (brush is SolidColorBrush solidColorBrush)
		{
			var color = solidColorBrush.ColorWithOpacity;
			WindowManagerInterop.SetElementBackgroundColor(element.HtmlId, color);
		}
		else if (brush is GradientBrush gradientBrush)
		{
			_resetBackgroundOnSizeChange = true;
			WindowManagerInterop.SetElementBackgroundGradient(element.HtmlId, gradientBrush.ToCssString(element.RenderSize));
		}
		else if (brush is XamlCompositionBrushBase unsupportedCompositionBrush)
		{
			var fallbackColor = unsupportedCompositionBrush.FallbackColorWithOpacity;
			WindowManagerInterop.SetElementBackgroundColor(element.HtmlId, fallbackColor);
		}
		else
		{
			WindowManagerInterop.ResetElementBackground(element.HtmlId);
		}

		return Disposable.Empty;
	}
}
