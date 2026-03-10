using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Documents;

/// <summary>
/// An abstract class that provides a base for all block-level content elements.
/// </summary>
public partial class Block : TextElement
{
	#region TextAlignment

	/// <summary>
	/// Gets or sets the horizontal alignment of the text content.
	/// </summary>
	public TextAlignment TextAlignment
	{
		get => (TextAlignment)GetValue(TextAlignmentProperty);
		set => SetValue(TextAlignmentProperty, value);
	}

	public static DependencyProperty TextAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(TextAlignment),
			typeof(TextAlignment),
			typeof(Block),
			new FrameworkPropertyMetadata(
				TextAlignment.Left,
				propertyChangedCallback: (s, e) => ((Block)s).OnBlockPropertyChanged()));

	#endregion

	#region HorizontalTextAlignment

	/// <summary>
	/// Gets or sets how the text is aligned in the block.
	/// </summary>
	public TextAlignment HorizontalTextAlignment
	{
		get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
		set => SetValue(HorizontalTextAlignmentProperty, value);
	}

	public static DependencyProperty HorizontalTextAlignmentProperty { get; } =
		DependencyProperty.Register(
			nameof(HorizontalTextAlignment),
			typeof(TextAlignment),
			typeof(Block),
			new FrameworkPropertyMetadata(
				TextAlignment.Left,
				propertyChangedCallback: (s, e) => ((Block)s).OnBlockPropertyChanged()));

	#endregion

	#region LineHeight

	/// <summary>
	/// Gets or sets the height of each line of content.
	/// </summary>
	public double LineHeight
	{
		get => (double)GetValue(LineHeightProperty);
		set => SetValue(LineHeightProperty, value);
	}

	public static DependencyProperty LineHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(LineHeight),
			typeof(double),
			typeof(Block),
			new FrameworkPropertyMetadata(
				0.0,
				propertyChangedCallback: (s, e) => ((Block)s).OnBlockPropertyChanged()));

	#endregion

	#region LineStackingStrategy

	/// <summary>
	/// Gets or sets a value that indicates how a line box is determined for each line of text within a block-level text element.
	/// </summary>
	public LineStackingStrategy LineStackingStrategy
	{
		get => (LineStackingStrategy)GetValue(LineStackingStrategyProperty);
		set => SetValue(LineStackingStrategyProperty, value);
	}

	public static DependencyProperty LineStackingStrategyProperty { get; } =
		DependencyProperty.Register(
			nameof(LineStackingStrategy),
			typeof(LineStackingStrategy),
			typeof(Block),
			new FrameworkPropertyMetadata(
				LineStackingStrategy.MaxHeight,
				propertyChangedCallback: (s, e) => ((Block)s).OnBlockPropertyChanged()));

	#endregion

	#region Margin

	/// <summary>
	/// Gets or sets the margin thickness for the block element.
	/// </summary>
	public Thickness Margin
	{
		get => (Thickness)GetValue(MarginProperty);
		set => SetValue(MarginProperty, value);
	}

	public static DependencyProperty MarginProperty { get; } =
		DependencyProperty.Register(
			nameof(Margin),
			typeof(Thickness),
			typeof(Block),
			new FrameworkPropertyMetadata(
				Thickness.Empty,
				propertyChangedCallback: (s, e) => ((Block)s).OnBlockPropertyChanged()));

	#endregion

	protected Block()
	{
	}

	private void OnBlockPropertyChanged()
	{
		var parent = GetContainingFrameworkElement();
		if (parent is RichTextBlock richTextBlock)
		{
			richTextBlock.InvalidateBlockContent();
		}
	}

	internal void InvalidateInlines()
	{
		var parent = GetContainingFrameworkElement();
		if (parent is RichTextBlock richTextBlock)
		{
			richTextBlock.InvalidateBlockContent();
		}
	}
}
