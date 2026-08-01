#nullable enable

using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
	public FauxGradientBorderPresenter()
	{
		Visibility = Visibility.Collapsed;
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
	}
}
