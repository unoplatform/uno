// MUX Reference TeachingTip.properties.cpp, commit de78834

#nullable enable

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Microsoft/* UWP don't rename */.UI.Xaml.Controls;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for a TeachingTip.
/// </summary>
public partial class TeachingTipTemplateSettings : DependencyObject
{
	/// <summary>
	/// Gets the icon element.
	/// </summary>
	public IconElement IconElement
	{
		get => (IconElement)GetValue(IconElementProperty);
		set => SetValue(IconElementProperty, value);
	}

	/// <summary>
	/// Identifies the IconElement dependency property.
	/// </summary>
	public static DependencyProperty IconElementProperty { get; } =
		DependencyProperty.Register(nameof(IconElement), typeof(IconElement), typeof(TeachingTipTemplateSettings), new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Gets the thickness value of the top left highlight margin.
	/// </summary>
	public Thickness TopLeftHighlightMargin
	{
		get => (Thickness)GetValue(TopLeftHighlightMarginProperty);
		set => SetValue(TopLeftHighlightMarginProperty, value);
	}

	/// <summary>
	/// Identifies the TopLeftHighlightMargin dependency property.
	/// </summary>
	public static DependencyProperty TopLeftHighlightMarginProperty { get; } =
		DependencyProperty.Register(nameof(TopLeftHighlightMargin), typeof(Thickness), typeof(TeachingTipTemplateSettings), new FrameworkPropertyMetadata(default(Thickness)));

	/// <summary>
	/// Gets the thickness value of the top right highlight margin.
	/// </summary>
	public Thickness TopRightHighlightMargin
	{
		get => (Thickness)GetValue(TopRightHighlightMarginProperty);
		set => SetValue(TopRightHighlightMarginProperty, value);
	}

	/// <summary>
	/// Identifies the TopRightHighlightMargin dependency property.
	/// </summary>
	public static DependencyProperty TopRightHighlightMarginProperty { get; } =
		DependencyProperty.Register(nameof(TopRightHighlightMargin), typeof(Thickness), typeof(TeachingTipTemplateSettings), new FrameworkPropertyMetadata(default(Thickness)));
}
