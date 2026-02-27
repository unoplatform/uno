using Windows.Foundation;
using Microsoft.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining templates for an AppBar control.
/// Not intended for general use.
/// </summary>
public sealed partial class AppBarTemplateSettings : DependencyObject
{
	internal AppBarTemplateSettings()
	{
	}

	/// <summary>
	/// Gets the Rect that describes the clipped area of the AppBar.
	/// </summary>
	public Rect ClipRect
	{
		get => (Rect)GetValue(ClipRectProperty);
		internal set => SetValue(ClipRectProperty, value);
	}

	internal static DependencyProperty ClipRectProperty { get; } =
		DependencyProperty.Register(nameof(ClipRect), typeof(Rect), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Rect()));

	/// <summary>
	/// Gets the margin of the AppBar root in the compact state.
	/// </summary>
	public Thickness CompactRootMargin
	{
		get => (Thickness)GetValue(CompactRootMarginProperty);
		internal set => SetValue(CompactRootMarginProperty, value);
	}

	internal static DependencyProperty CompactRootMarginProperty { get; } =
		DependencyProperty.Register(nameof(CompactRootMargin), typeof(Thickness), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Thickness(0)));

	/// <summary>
	/// Gets the vertical delta of the AppBar in the compact state.
	/// </summary>
	public double CompactVerticalDelta
	{
		get => (double)GetValue(CompactVerticalDeltaProperty);
		internal set => SetValue(CompactVerticalDeltaProperty, value);
	}

	internal static DependencyProperty CompactVerticalDeltaProperty { get; } =
		DependencyProperty.Register(nameof(CompactVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the margin of the AppBar root in the hidden state.
	/// </summary>
	public Thickness HiddenRootMargin
	{
		get => (Thickness)GetValue(HiddenRootMarginProperty);
		internal set => SetValue(HiddenRootMarginProperty, value);
	}

	public static DependencyProperty HiddenRootMarginProperty { get; } =
		DependencyProperty.Register(nameof(HiddenRootMargin), typeof(Thickness), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Thickness(0)));

	/// <summary>
	/// Gets the vertical delta of the AppBar in the hidden state.
	/// </summary>
	public double HiddenVerticalDelta
	{
		get => (double)GetValue(HiddenVerticalDeltaProperty);
		internal set => SetValue(HiddenVerticalDeltaProperty, value);
	}

	internal static DependencyProperty HiddenVerticalDeltaProperty { get; } =
		DependencyProperty.Register(nameof(HiddenVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the margin of the AppBar root in the minimal state.
	/// </summary>
	public Thickness MinimalRootMargin
	{
		get => (Thickness)GetValue(MinimalRootMarginProperty);
		internal set => SetValue(MinimalRootMarginProperty, value);
	}

	internal static DependencyProperty MinimalRootMarginProperty { get; } =
		DependencyProperty.Register(nameof(MinimalRootMargin), typeof(Thickness), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(new Thickness(0)));

	/// <summary>
	/// Gets the vertical delta of the AppBar in the minimal state.
	/// </summary>
	public double MinimalVerticalDelta
	{
		get => (double)GetValue(MinimalVerticalDeltaProperty);
		internal set => SetValue(MinimalVerticalDeltaProperty, value);
	}

	internal static DependencyProperty MinimalVerticalDeltaProperty { get; } =
		DependencyProperty.Register(nameof(MinimalVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the negative vertical delta of the AppBar in the compact state.
	/// </summary>
	public double NegativeCompactVerticalDelta
	{
		get => (double)GetValue(NegativeCompactVerticalDeltaProperty);
		internal set => SetValue(NegativeCompactVerticalDeltaProperty, value);
	}

	internal static DependencyProperty NegativeCompactVerticalDeltaProperty { get; } =
		DependencyProperty.Register(nameof(NegativeCompactVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the negative vertical delta of the AppBar in the hidden state.
	/// </summary>
	public double NegativeHiddenVerticalDelta
	{
		get => (double)GetValue(NegativeHiddenVerticalDeltaProperty);
		internal set => SetValue(NegativeHiddenVerticalDeltaProperty, value);
	}

	internal static DependencyProperty NegativeHiddenVerticalDeltaProperty { get; } =
		DependencyProperty.Register(nameof(NegativeHiddenVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the negative vertical delta of the AppBar in the minimal state.
	/// </summary>
	public double NegativeMinimalVerticalDelta
	{
		get => (double)GetValue(NegativeMinimalVerticalDeltaProperty);
		internal set => SetValue(NegativeMinimalVerticalDeltaProperty, value);
	}

	internal static DependencyProperty NegativeMinimalVerticalDeltaProperty { get; } =
		DependencyProperty.Register(nameof(NegativeMinimalVerticalDelta), typeof(double), typeof(AppBarTemplateSettings), new FrameworkPropertyMetadata(0.0));
}
