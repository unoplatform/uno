using Microsoft.UI.Xaml.Media;
using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls.Primitives;

public sealed partial class SplitViewTemplateSettings : DependencyObject
{
	public SplitViewTemplateSettings()
	{
		InitializeBinder();
	}

	#region DependencyProperty: OpenPaneLength

	public static DependencyProperty OpenPaneLengthProperty { get; } = DependencyProperty.Register(
		nameof(OpenPaneLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(320.0));

	public double OpenPaneLength
	{
		get => (double)GetValue(OpenPaneLengthProperty);
		internal set => SetValue(OpenPaneLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: CompactPaneLength

	// Uno specific: This property is not in WinUI's SplitViewTemplateSettings,
	// but is needed for backward compatibility with existing Uno XAML templates.
	public static DependencyProperty CompactPaneLengthProperty { get; } = DependencyProperty.Register(
		nameof(CompactPaneLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(48.0));

	public double CompactPaneLength
	{
		get => (double)GetValue(CompactPaneLengthProperty);
		internal set => SetValue(CompactPaneLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: CompactPaneGridLength

	public static DependencyProperty CompactPaneGridLengthProperty { get; } = DependencyProperty.Register(
		nameof(CompactPaneGridLength),
		typeof(GridLength),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(GridLength)));

	public GridLength CompactPaneGridLength
	{
		get => (GridLength)GetValue(CompactPaneGridLengthProperty);
		internal set => SetValue(CompactPaneGridLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: OpenPaneGridLength

	public static DependencyProperty OpenPaneGridLengthProperty { get; } = DependencyProperty.Register(
		nameof(OpenPaneGridLength),
		typeof(GridLength),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(GridLength)));

	public GridLength OpenPaneGridLength
	{
		get => (GridLength)GetValue(OpenPaneGridLengthProperty);
		internal set => SetValue(OpenPaneGridLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: NegativeOpenPaneLength

	public static DependencyProperty NegativeOpenPaneLengthProperty { get; } = DependencyProperty.Register(
		nameof(NegativeOpenPaneLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(double)));

	public double NegativeOpenPaneLength
	{
		get => (double)GetValue(NegativeOpenPaneLengthProperty);
		internal set => SetValue(NegativeOpenPaneLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: NegativeOpenPaneLengthMinusCompactLength

	public static DependencyProperty NegativeOpenPaneLengthMinusCompactLengthProperty { get; } = DependencyProperty.Register(
		nameof(NegativeOpenPaneLengthMinusCompactLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(double)));

	public double NegativeOpenPaneLengthMinusCompactLength
	{
		get => (double)GetValue(NegativeOpenPaneLengthMinusCompactLengthProperty);
		internal set => SetValue(NegativeOpenPaneLengthMinusCompactLengthProperty, value);
	}

	#endregion
	#region DependencyProperty: OpenPaneLengthMinusCompactLength

	public static DependencyProperty OpenPaneLengthMinusCompactLengthProperty { get; } = DependencyProperty.Register(
		nameof(OpenPaneLengthMinusCompactLength),
		typeof(double),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(double)));

	public double OpenPaneLengthMinusCompactLength
	{
		get => (double)GetValue(OpenPaneLengthMinusCompactLengthProperty);
		internal set => SetValue(OpenPaneLengthMinusCompactLengthProperty, value);
	}

	#endregion

#if !__SKIA__
	// Uno specific: These properties were added to facilitate clipping on non-Skia targets
	// where Geometry.Transform is not supported (issue #3747).
	// On Skia, PaneClipRectangleTransform (TranslateTransform) in XAML handles pane clipping.

	#region DependencyProperty: LeftClip

	public static DependencyProperty LeftClipProperty { get; } = DependencyProperty.Register(
		nameof(LeftClip),
		typeof(RectangleGeometry),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(RectangleGeometry)));

	public RectangleGeometry LeftClip
	{
		get => (RectangleGeometry)GetValue(LeftClipProperty);
		internal set => SetValue(LeftClipProperty, value);
	}

	#endregion
	#region DependencyProperty: RightClip

	public static DependencyProperty RightClipProperty { get; } = DependencyProperty.Register(
		nameof(RightClip),
		typeof(RectangleGeometry),
		typeof(SplitViewTemplateSettings),
		new FrameworkPropertyMetadata(default(RectangleGeometry)));

	public RectangleGeometry RightClip
	{
		get => (RectangleGeometry)GetValue(RightClipProperty);
		internal set => SetValue(RightClipProperty, value);
	}

	#endregion
#endif
}
