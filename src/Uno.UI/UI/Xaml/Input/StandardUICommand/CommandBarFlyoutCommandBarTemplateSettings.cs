using Windows.Foundation;
using Windows.UI.Xaml;

namespace Microsoft.UI.Xaml.Controls.Primitives;

/// <summary>
/// Provides calculated values that can be referenced as TemplatedParent sources when defining
/// templates for a CommandBarFlyout control. Not intended for general use.
/// </summary>
public sealed class CommandBarFlyoutCommandBarTemplateSettings : DependencyObject
{
	internal CommandBarFlyoutCommandBarTemplateSettings()
	{
	}

	/// <summary>
	/// Gets the end position for the close animation.
	/// </summary>
	public double CloseAnimationEndPosition
	{
		get => (double)GetValue(CloseAnimationEndPositionProperty);
		internal set => SetValue(CloseAnimationEndPositionProperty, value);
	}

	internal static DependencyProperty CloseAnimationEndPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(CloseAnimationEndPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the rectangle used to clip the content.
	/// </summary>
	public Rect ContentClipRect
	{
		get => (Rect)GetValue(ContentClipRectProperty);
		internal set => SetValue(ContentClipRectProperty, value);
	}

	internal static DependencyProperty ContentClipRectProperty { get; } =
		DependencyProperty.Register(
			nameof(ContentClipRect),
			typeof(Rect),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(default(Rect)));

	/// <summary>
	/// Gets the current width of the control.
	/// </summary>
	public double CurrentWidth
	{
		get => (double)GetValue(CurrentWidthProperty);
		internal set => SetValue(CurrentWidthProperty, value);
	}

	internal static DependencyProperty CurrentWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(CurrentWidth),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the end position for the expand down animation.
	/// </summary>
	public double ExpandDownAnimationEndPosition
	{
		get => (double)GetValue(ExpandDownAnimationEndPositionProperty);
		internal set => SetValue(ExpandDownAnimationEndPositionProperty, value);
	}

	internal static DependencyProperty ExpandDownAnimationEndPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandDownAnimationEndPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the hold position for the expand down animation.
	/// </summary>
	public double ExpandDownAnimationHoldPosition
	{
		get => (double)GetValue(ExpandDownAnimationHoldPositionProperty);
		internal set => SetValue(ExpandDownAnimationHoldPositionProperty, value);
	}

	internal static DependencyProperty ExpandDownAnimationHoldPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandDownAnimationHoldPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the start position for the expand down animation.
	/// </summary>
	public double ExpandDownAnimationStartPosition
	{
		get => (double)GetValue(ExpandDownAnimationStartPositionProperty);
		internal set => SetValue(ExpandDownAnimationStartPositionProperty, value);
	}

	internal static DependencyProperty ExpandDownAnimationStartPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandDownAnimationStartPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the vertical position of the overflow when expanded down.
	/// </summary>
	public double ExpandDownOverflowVerticalPosition
	{
		get => (double)GetValue(ExpandDownOverflowVerticalPositionProperty);
		internal set => SetValue(ExpandDownOverflowVerticalPositionProperty, value);
	}

	internal static DependencyProperty ExpandDownOverflowVerticalPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandDownOverflowVerticalPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the width of the control when expanded.
	/// </summary>
	public double ExpandedWidth
	{
		get => (double)GetValue(ExpandedWidthProperty);
		internal set => SetValue(ExpandedWidthProperty, value);
	}

	internal static DependencyProperty ExpandedWidthProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandedWidth),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the end position for the expand up animation.
	/// </summary>
	public double ExpandUpAnimationEndPosition
	{
		get => (double)GetValue(ExpandUpAnimationEndPositionProperty);
		internal set => SetValue(ExpandUpAnimationEndPositionProperty, value);
	}

	internal static DependencyProperty ExpandUpAnimationEndPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandUpAnimationEndPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the hold position for the expand up animation.
	/// </summary>
	public double ExpandUpAnimationHoldPosition
	{
		get => (double)GetValue(ExpandUpAnimationHoldPositionProperty);
		internal set => SetValue(ExpandUpAnimationHoldPositionProperty, value);
	}

	internal static DependencyProperty ExpandUpAnimationHoldPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandUpAnimationHoldPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the start position for the expand up animation.
	/// </summary>
	public double ExpandUpAnimationStartPosition
	{
		get => (double)GetValue(ExpandUpAnimationStartPositionProperty);
		internal set => SetValue(ExpandUpAnimationStartPositionProperty, value);
	}

	internal static DependencyProperty ExpandUpAnimationStartPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandUpAnimationStartPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the vertical position of the overflow when expanded up.
	/// </summary>
	public double ExpandUpOverflowVerticalPosition
	{
		get => (double)GetValue(ExpandUpOverflowVerticalPositionProperty);
		internal set => SetValue(ExpandUpOverflowVerticalPositionProperty, value);
	}

	internal static DependencyProperty ExpandUpOverflowVerticalPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(ExpandUpOverflowVerticalPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the end position for the open animation.
	/// </summary>
	public double OpenAnimationEndPosition
	{
		get => (double)GetValue(OpenAnimationEndPositionProperty);
		internal set => SetValue(OpenAnimationEndPositionProperty, value);
	}

	internal static DependencyProperty OpenAnimationEndPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(OpenAnimationEndPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the start position for the open animation.
	/// </summary>
	public double OpenAnimationStartPosition
	{
		get => (double)GetValue(OpenAnimationStartPositionProperty);
		internal set => SetValue(OpenAnimationStartPositionProperty, value);
	}

	internal static DependencyProperty OpenAnimationStartPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(OpenAnimationStartPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the rectangle used to clip the overflow content.
	/// </summary>
	public Rect OverflowContentClipRect
	{
		get => (Rect)GetValue(OverflowContentClipRectProperty);
		internal set => SetValue(OverflowContentClipRectProperty, value);
	}

	internal static DependencyProperty OverflowContentClipRectProperty { get; } =
		DependencyProperty.Register(
			nameof(OverflowContentClipRect),
			typeof(Rect),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(default(Rect)));

	/// <summary>
	/// Gets the end position for the width expansion animation.
	/// </summary>
	public double WidthExpansionAnimationEndPosition
	{
		get => (double)GetValue(WidthExpansionAnimationEndPositionProperty);
		internal set => SetValue(WidthExpansionAnimationEndPositionProperty, value);
	}

	internal static DependencyProperty WidthExpansionAnimationEndPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(WidthExpansionAnimationEndPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the start position for the width expansion animation.
	/// </summary>
	public double WidthExpansionAnimationStartPosition
	{
		get => (double)GetValue(WidthExpansionAnimationStartPositionProperty);
		internal set => SetValue(WidthExpansionAnimationStartPositionProperty, value);
	}

	internal static DependencyProperty WidthExpansionAnimationStartPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(WidthExpansionAnimationStartPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the amount of change for the width expansion.
	/// </summary>
	public double WidthExpansionDelta
	{
		get => (double)GetValue(WidthExpansionDeltaProperty);
		internal set => SetValue(WidthExpansionDeltaProperty, value);
	}

	internal static DependencyProperty WidthExpansionDeltaProperty { get; } =
		DependencyProperty.Register(
			nameof(WidthExpansionDelta),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the end position for the "more" button width expansion animation.
	/// </summary>
	public double WidthExpansionMoreButtonAnimationEndPosition
	{
		get => (double)GetValue(WidthExpansionMoreButtonAnimationEndPositionProperty);
		internal set => SetValue(WidthExpansionMoreButtonAnimationEndPositionProperty, value);
	}

	internal static DependencyProperty WidthExpansionMoreButtonAnimationEndPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(WidthExpansionMoreButtonAnimationEndPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));

	/// <summary>
	/// Gets the start position for the "more" button width expansion animation.
	/// </summary>
	public double WidthExpansionMoreButtonAnimationStartPosition
	{
		get => (double)GetValue(WidthExpansionMoreButtonAnimationStartPositionProperty);
		internal set => SetValue(WidthExpansionMoreButtonAnimationStartPositionProperty, value);
	}

	internal static DependencyProperty WidthExpansionMoreButtonAnimationStartPositionProperty { get; } =
		DependencyProperty.Register(
			nameof(WidthExpansionMoreButtonAnimationStartPosition),
			typeof(double),
			typeof(CommandBarFlyoutCommandBarTemplateSettings),
			new FrameworkPropertyMetadata(0.0));
}
