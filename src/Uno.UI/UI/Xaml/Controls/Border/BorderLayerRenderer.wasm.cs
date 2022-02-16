using System;
using Uno.Disposables;
using Uno.Extensions;
using Uno.UI.Xaml;
using Uno.UI.Xaml.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls;

internal partial class BorderLayerRenderer
{
	private Brush _currentBackground;
	private (Brush, Thickness) _currentBorder;
	private CornerRadius _currentCornerRadius;

	private SerialDisposable _backgroundSubscription;
	private SerialDisposable _borderSubscription;

	public void UpdateLayer(
		Brush background,
		BackgroundSizing backgroundSizing,
		Thickness borderThickness,
		Brush borderBrush,
		CornerRadius cornerRadius,
		object image)
	{
		UpdateBackground(background, backgroundSizing);
		UpdateBorder(borderBrush, borderThickness, cornerRadius);
	}

	private void UpdateBorder(Brush brush, Thickness thickness, CornerRadius cornerRadius)
	{
		var cornerRadiusChanged = cornerRadius != _currentCornerRadius;

		if (cornerRadiusChanged)
		{
			SetCornerRadius(cornerRadius);
		}

		var borderChanged = _currentBorder != (brush, thickness) ||
			(brush is LinearGradientBrush && cornerRadiusChanged); // Corner radius impacts linear gradient border rendering.

		if (!borderChanged)
		{
			return;
		}

		var subscription = _borderSubscription ??= new SerialDisposable();
		subscription.Disposable = null;
		subscription.Disposable = SetAndObserveBorder(brush, thickness, cornerRadius);
	}

	private void UpdateBackground(Brush background, BackgroundSizing backgroundSizing)
	{
		if (_currentBackground != background && _owner is FrameworkElement fwElt)
		{
			_currentBackground = background;
			var subscription = _backgroundSubscription ??= new SerialDisposable();

			subscription.Disposable = null;
			subscription.Disposable = SetAndObserveBackground(background);
		}
	}

	private IDisposable SetAndObserveBorder(Brush brush, Thickness thickness, CornerRadius cornerRadius)
	{
		SetBorder(brush, thickness, cornerRadius);
		return Brush.AssignAndObserveBrush(brush, _ => SetBorder(brush, thickness, cornerRadius));
	}

	private IDisposable SetAndObserveBackground(Brush brush)
	{
		SetBackground(brush);

		if (brush is ImageBrush imgBrush)
		{
			RecalculateBrushOnSizeChanged(false);
			return imgBrush.Subscribe(img =>
			{
				switch (img.Kind)
				{
					case ImageDataKind.Empty:
					case ImageDataKind.Error:
						_owner.ResetStyle("background-color", "background-image", "background-size");
						break;

					case ImageDataKind.DataUri:
					case ImageDataKind.Url:
					default:
						_owner.SetStyle(
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
			return acrylicBrush.Subscribe(_owner);
		}
		else
		{
			return Brush.AssignAndObserveBrush(brush, _ => SetBackground(brush));
		}
	}

	private void SetCornerRadius(CornerRadius cornerRadius)
	{
		if (cornerRadius == CornerRadius.None)
		{
			_owner.ResetStyle("border-radius", "overflow");
		}
		else
		{
			var borderRadiusCssString = $"{cornerRadius.TopLeft.ToStringInvariant()}px {cornerRadius.TopRight.ToStringInvariant()}px {cornerRadius.BottomRight.ToStringInvariant()}px {cornerRadius.BottomLeft.ToStringInvariant()}px";
			_owner.SetStyle(
				("border-radius", borderRadiusCssString),
				("overflow", "hidden")); // overflow: hidden is required here because the clipping can't do its job when it's non-rectangular.
		}

		_currentCornerRadius = cornerRadius;
	}

	private void SetBackground(Brush brush)
	{
		switch (brush)
		{
			case SolidColorBrush solidColorBrush:
				var color = solidColorBrush.ColorWithOpacity;
				WindowManagerInterop.SetElementBackgroundColor(_owner.HtmlId, color);
				RecalculateBrushOnSizeChanged(false);
				break;
			case GradientBrush gradientBrush:
				WindowManagerInterop.SetElementBackgroundGradient(_owner.HtmlId, gradientBrush.ToCssString(_owner.RenderSize));
				RecalculateBrushOnSizeChanged(true);
				break;
			case XamlCompositionBrushBase unsupportedCompositionBrush:
				var fallbackColor = unsupportedCompositionBrush.FallbackColorWithOpacity;
				WindowManagerInterop.SetElementBackgroundColor(_owner.HtmlId, fallbackColor);
				RecalculateBrushOnSizeChanged(false);
				break;
			default:
				WindowManagerInterop.ResetElementBackground(_owner.HtmlId);
				RecalculateBrushOnSizeChanged(false);
				break;
		}
	}

	private void SetBorder(Brush brush, Thickness thickness, CornerRadius cornerRadius)
	{
		void ApplyGradientBorder(GradientBrush gradient, string borderWidth)
		{
			var border = gradient.ToCssString(_owner.RenderSize); // TODO: Reevaluate when size is changing
			_owner.SetGradientBorder(border, borderWidth);
		}

		void ApplySolidColorBorder(Windows.UI.Color color, string borderWidth)
		{
			_owner.SetSolidColorBorder(color, borderWidth);
		}

		if (thickness == Thickness.Empty)
		{
			_owner.SetStyle(
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
					ApplySolidColorBorder(solidColorBrush.ColorWithOpacity, borderWidth);
					break;
				case LinearGradientBrush linearGradientBrush:
					if (cornerRadius == CornerRadius.None ||
						!BorderGradientBrushHelper.CanApplySolidColorRendering(linearGradientBrush))
					{
						ApplyGradientBorder(linearGradientBrush, borderWidth);
					}
					else
					{
						var majorStop = BorderGradientBrushHelper.GetMajorStop(linearGradientBrush);
						var borderColor = Windows.UI.Color.FromArgb((byte)(linearGradientBrush.Opacity * majorStop.Color.A), majorStop.Color.R, majorStop.Color.G, majorStop.Color.B);

						ApplySolidColorBorder(borderColor, borderWidth);
					}
					break;
				case GradientBrush gradientBrush:
					ApplyGradientBorder(gradientBrush, borderWidth);
					break;
				case AcrylicBrush acrylicBrush:
					if (Brush.TryGetColorWithOpacity(acrylicBrush, out var acrylicFallbackColor))
					{
						ApplySolidColorBorder(acrylicFallbackColor, borderWidth);
					}
					break;
				default:
					_owner.ResetStyle("border-style", "border-color", "border-image", "border-width");
					break;
			}
		}

		_currentBorder = (brush, thickness);
	}

	internal void Clear()
	{
		if (_backgroundSubscription != null)
		{
			_backgroundSubscription.Disposable = null;
		}
		if (_borderSubscription != null)
		{
			_borderSubscription.Disposable = null;
		}
	}	

	private void RecalculateBrushOnSizeChanged(bool shouldRecalculate)
	{
		if (_owner is FrameworkElement fw)
		{
			if (shouldRecalculate)
			{
				fw.SizeChanged -= OnElementSizeChanged;
				fw.SizeChanged += OnElementSizeChanged;
			}
			else
			{
				fw.SizeChanged -= OnElementSizeChanged;
			}
		}

		internal void Clear()
		{
			_background = null;
			_border = default;

		}
	}

	private void OnElementSizeChanged(object sender, SizeChangedEventArgs e)
	{
		var fe = _owner as FrameworkElement;
		SetBackground(fe.Background);
	}
}
