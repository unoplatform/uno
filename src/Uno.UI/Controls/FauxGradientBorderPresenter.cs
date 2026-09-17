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
	[GeneratedDependencyProperty(DefaultValue = null)]
	public partial Brush RequestedBorderBrush { get; set; }

	private void OnRequestedBorderBrushChanged() => OnBorderChanged();

	/// <summary>
	/// Gets or sets the thickness of the border that should be displayed.
	/// </summary>
	[GeneratedDependencyProperty]
	public partial Thickness RequestedBorderThickness { get; set; }

	private static Thickness GetRequestedBorderThicknessDefaultValue() => Thickness.Empty;

	private void OnRequestedBorderThicknessChanged() => OnBorderChanged();

	/// <summary>
	/// Gets or sets the corner radius of the border that should be displayed.
	/// </summary>
	[GeneratedDependencyProperty]
	public partial CornerRadius RequestedCornerRadius { get; set; }

	private static CornerRadius GetRequestedCornerRadiusDefaultValue() => CornerRadius.None;

	private void OnRequestedCornerRadiusChanged() => OnBorderChanged();

	private void OnBorderChanged()
	{
	}
}
