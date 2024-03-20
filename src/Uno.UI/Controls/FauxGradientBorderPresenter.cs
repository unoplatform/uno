using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Uno.UI.Controls;

/// <summary>
/// This presenter provides a way to display "fake" LinearGradientBrush on element border for
/// cases which are unsupported by WebAssembly, iOS and macOS.
/// </summary>
/// <remarks>
/// WASM - the presenter will be used in case the element has CornerRadius applied.
/// iOS and macOS - the presenter will be used in case the element has LinearGradientBrush with a transform applied.
/// All other cases - the presenter is not visible.
/// </remarks>
public partial class FauxGradientBorderPresenter : ContentControl
{
#if __WASM__ || __IOS__ || __MACOS__
	private readonly Border? _displayBorder;
#endif

	public FauxGradientBorderPresenter()
	{
#if __WASM__ || __IOS__ || __MACOS__
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;
		Content = _displayBorder = new Border();
#endif
	}

	/// <summary>
	/// Gets or sets the border brush that is supposed to be displayed.
	/// </summary>
	public Brush RequestedBorderBrush
	{
		get => (Brush)GetValue(RequestedBorderBrushProperty);
		set => SetValue(RequestedBorderBrushProperty, value);
	}

	/// <summary>
	/// Identifies the RequestedBorderBrush dependency property.
	/// </summary>
	public static DependencyProperty RequestedBorderBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(RequestedBorderBrush),
			typeof(Brush),
			typeof(FauxGradientBorderPresenter),
			new FrameworkPropertyMetadata(null, propertyChangedCallback: (s, args) => (s as FauxGradientBorderPresenter)?.OnBorderChanged()));

	/// <summary>
	/// Gets or sets the thickness of the border that is supposed to be displayed.
	/// </summary>
	public Thickness RequestedBorderThickness
	{
		get => (Thickness)GetValue(RequestedBorderThicknessProperty);
		set => SetValue(RequestedBorderThicknessProperty, value);
	}

	/// <summary>
	/// Identifies the RequestedBorderThickness dependency property.
	/// </summary>
	public static DependencyProperty RequestedBorderThicknessProperty { get; } =
		DependencyProperty.Register(
			nameof(RequestedBorderThickness),
			typeof(Thickness),
			typeof(FauxGradientBorderPresenter),
			new FrameworkPropertyMetadata(default(Thickness), propertyChangedCallback: (s, args) => (s as FauxGradientBorderPresenter)?.OnBorderChanged()));

	public CornerRadius RequestedCornerRadius
	{
		get => (CornerRadius)GetValue(RequestedCornerRadiusProperty);
		set => SetValue(RequestedCornerRadiusProperty, value);
	}

	public static DependencyProperty RequestedCornerRadiusProperty { get; } =
		DependencyProperty.Register(
			nameof(RequestedCornerRadius),
			typeof(CornerRadius),
			typeof(FauxGradientBorderPresenter),
			new FrameworkPropertyMetadata(CornerRadius.None, propertyChangedCallback: (s, args) => (s as FauxGradientBorderPresenter)?.OnBorderChanged()));

	private void OnBorderChanged()
	{
#if __WASM__ || __IOS__ || __MACOS__
		if (_displayBorder == null)
		{
			return;
		}

		var requestedThickness = RequestedBorderThickness;
		var requestedBorderBrush = RequestedBorderBrush;
		var requestedCornerRadius = RequestedCornerRadius;

		if (requestedBorderBrush is not LinearGradientBrush gradientBrush ||
			!gradientBrush.CanApplySolidColorRendering())
		{
			_displayBorder.Visibility = Visibility.Collapsed;
			return;
		}

#if __WASM__
		if (requestedCornerRadius == CornerRadius.None)
		{
			// WASM can render linear gradient border unless corner radius is set.
			_displayBorder.Visibility = Visibility.Collapsed;
			return;
		}
#endif

#if __IOS__ || __MACOS__
		if (gradientBrush.RelativeTransform == null)
		{
			// iOS can render linear gradient border unless relative transform is used.
			_displayBorder.Visibility = Visibility.Collapsed;
			return;
		}
#endif

		requestedThickness.Left = 0;
		requestedThickness.Right = 0;
		var minorStopAlignment = gradientBrush.GetMinorStopAlignment();
		if (minorStopAlignment == VerticalAlignment.Top)
		{
			requestedThickness.Bottom = 0;
		}
		else
		{
			requestedThickness.Top = 0;
		}

		if (requestedThickness == Thickness.Empty)
		{
			_displayBorder.Visibility = Visibility.Collapsed;
			return;
		}

		_displayBorder.Visibility = Visibility.Visible;

		_displayBorder.CornerRadius = requestedCornerRadius;
		_displayBorder.BorderThickness = requestedThickness;
		_displayBorder.BorderBrush = gradientBrush.FauxOverlayBrush;
#endif
	}
}
