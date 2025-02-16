#nullable enable

using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Xaml;

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
public partial class FauxGradientBorderPresenter : ContentPresenter
{
#if __WASM__ || __IOS__ || __MACOS__
	private readonly Border _displayBorder;
#endif

	public FauxGradientBorderPresenter()
	{
#if __WASM__ || __IOS__ || __MACOS__
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		VerticalContentAlignment = VerticalAlignment.Stretch;
		Content = _displayBorder = new Border();
#else
		Visibility = Visibility.Collapsed;
#endif
	}

	/// <summary>
	/// Gets or sets the border brush that is supposed to be displayed.
	/// </summary>
	public Brush RequestedBorderBrush
	{
		get => GetRequestedBorderBrushValue();
		set => SetRequestedBorderBrushValue(value);
	}

	/// <summary>
	/// Identifies the RequestedBorderBrush dependency property.
	/// </summary>
	[GeneratedDependencyProperty(DefaultValue = null)]
	public static DependencyProperty RequestedBorderBrushProperty { get; } = CreateRequestedBorderBrushProperty();

	private void OnRequestedBorderBrushChanged() => OnBorderChanged();

	/// <summary>
	/// Gets or sets the thickness of the border that should be displayed.
	/// </summary>
	public Thickness RequestedBorderThickness
	{
		get => GetRequestedBorderThicknessValue();
		set => SetRequestedBorderThicknessValue(value);
	}

	/// <summary>
	/// Identifies the RequestedBorderThickness dependency property.
	/// </summary>
	[GeneratedDependencyProperty]
	public static DependencyProperty RequestedBorderThicknessProperty { get; } = CreateRequestedBorderThicknessProperty();

	private static Thickness GetRequestedBorderThicknessDefaultValue() => Thickness.Empty;

	private void OnRequestedBorderThicknessChanged() => OnBorderChanged();

	/// <summary>
	/// Gets or sets the corner radius of the border that should be displayed.
	/// </summary>
	public CornerRadius RequestedCornerRadius
	{
		get => GetRequestedCornerRadiusValue();
		set => SetRequestedCornerRadiusValue(value);
	}

	/// <summary>
	/// Identifies the RequestedCornerRadius dependency property.
	/// </summary>
	[GeneratedDependencyProperty]
	public static DependencyProperty RequestedCornerRadiusProperty { get; } = CreateRequestedCornerRadiusProperty();

	private static CornerRadius GetRequestedCornerRadiusDefaultValue() => CornerRadius.None;

	private void OnRequestedCornerRadiusChanged() => OnBorderChanged();

	private void OnBorderChanged()
	{
#if __WASM__ || __IOS__ || __MACOS__
		var requestedThickness = RequestedBorderThickness;
		var requestedBorderBrush = RequestedBorderBrush;
		var requestedCornerRadius = RequestedCornerRadius;

		if (requestedBorderBrush is not LinearGradientBrush gradientBrush ||
			gradientBrush.CanApplyToBorder(requestedCornerRadius) ||
			!gradientBrush.SupportsFauxGradientBorder)
		{
			_displayBorder.Visibility = Visibility.Collapsed;
			return;
		}

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
