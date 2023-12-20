using Windows.UI.Xaml.Documents.TextFormatting;

namespace Windows.UI.Xaml.Documents;

/// <summary>
/// An abstract class that provides a base for all block-level content elements.
/// </summary>
public partial class Block : TextElement
{
	protected Block()
	{
	}

	#region TextAlignment Dependency Property

	/// <summary>
	/// Identifies the TextAlignment dependency property.
	/// </summary>
	/// <returns>
	/// The identifier for the TextAlignment dependency property.
	/// </returns>
	public TextAlignment TextAlignment
	{
		get => (TextAlignment)GetValue(TextAlignmentProperty);
		set => SetValue(TextAlignmentProperty, value);
	}

	public static DependencyProperty TextAlignmentProperty { get; } =
		DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(Block),
			new FrameworkPropertyMetadata(
				defaultValue: TextAlignment.Left,
				propertyChangedCallback: (s, e) => ((Block)s).OnTextAlignmentChanged()
			)
		);

	private void OnTextAlignmentChanged()
	{
		HorizontalTextAlignment = TextAlignment;
		OnTextAlignmentChangedPartial();
		InvalidateSegments();
	}

	partial void OnTextAlignmentChangedPartial();

	#endregion

	#region HorizontalTextAlignment Dependency Property

	/// <summary>
	/// Gets or sets a value that indicates how text is aligned in the Block.
	/// </summary>
	/// <returns>
	/// One of the TextAlignment enumeration values that specifies how text is aligned.
	/// The default is **Left**.
	/// </returns>
	public TextAlignment HorizontalTextAlignment
	{
		get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
		set => SetValue(HorizontalTextAlignmentProperty, value);
	}

	public static DependencyProperty HorizontalTextAlignmentProperty { get; } =
		DependencyProperty.Register(nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(Block),
			new FrameworkPropertyMetadata(
				defaultValue: TextAlignment.Left,
				propertyChangedCallback: (s, e) => ((Block)s).OnHorizontalTextAlignmentChanged()
			)
		);

	// This property provides the same functionality as the TextAlignment property.
	// If both properties are set to conflicting values, the last one set is used.
	// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.textbox.horizontaltextalignment#remarks
	private void OnHorizontalTextAlignmentChanged() => TextAlignment = HorizontalTextAlignment;

	#endregion

	#region LineHeight Dependency Property

	/// <summary>
	/// Gets or sets the height of each line of content.
	/// </summary>
	/// <returns>
	/// The pixel height of each line as modified by LineStackingStrategy. A value of
	/// 0 indicates that the line height is determined automatically from the current
	/// font characteristics. The default is 0.
	/// </returns>
	public double LineHeight
	{
		get { return (double)this.GetValue(LineHeightProperty); }
		set { this.SetValue(LineHeightProperty, value); }
	}

	public static DependencyProperty LineHeightProperty { get; } =
		DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(Block),
			new FrameworkPropertyMetadata(
				defaultValue: 0d,
				propertyChangedCallback: (s, e) => ((Block)s).OnLineHeightChanged()
			)
		);

	private void OnLineHeightChanged()
	{
		OnLineHeightChangedPartial();
		InvalidateSegments();
	}

	partial void OnLineHeightChangedPartial();

	#endregion

	#region LineStackingStrategy Dependency Property

	/// <summary>
	/// Gets or sets a value that indicates how a line box is determined for each line
	/// of text in the Block.
	/// </summary>
	/// <returns>
	/// A value that indicates how a line box is determined for each line of text in
	/// the Block. The default is **MaxHeight**.
	/// </returns>
	public LineStackingStrategy LineStackingStrategy
	{
		get { return (LineStackingStrategy)this.GetValue(LineStackingStrategyProperty); }
		set { this.SetValue(LineStackingStrategyProperty, value); }
	}

	public static DependencyProperty LineStackingStrategyProperty { get; } =
		DependencyProperty.Register(nameof(LineStackingStrategy), typeof(LineStackingStrategy), typeof(Block),
			new FrameworkPropertyMetadata(
				defaultValue: LineStackingStrategy.MaxHeight,
				propertyChangedCallback: (s, e) => ((Block)s).OnLineStackingStrategyChanged()
			)
		);

	private void OnLineStackingStrategyChanged()
	{
		OnLineStackingStrategyChangedPartial();
		InvalidateSegments();
	}

	partial void OnLineStackingStrategyChangedPartial();

	#endregion

	#region Margin Dependency Property

	/// <summary>
	/// Gets or sets the amount of space around a Block element.
	/// </summary>
	/// <returns>
	/// The amount of space around a Block element.
	/// </returns>
	public Thickness Margin
	{
		get => (Thickness)GetValue(MarginProperty);
		set => SetValue(MarginProperty, value);
	}

	public static DependencyProperty MarginProperty { get; } =
		DependencyProperty.Register(nameof(Margin), typeof(Thickness), typeof(Block),
			new FrameworkPropertyMetadata(
				defaultValue: Thickness.Empty,
				options: FrameworkPropertyMetadataOptions.AutoConvert | FrameworkPropertyMetadataOptions.AffectsMeasure,
				propertyChangedCallback: (s, e) => ((Block)s).OnMarginChanged()
			)
		);

	private void OnMarginChanged()
	{
		OnMarginChangedPartial();
		InvalidateSegments();
	}

	partial void OnMarginChangedPartial();

	#endregion

	internal void InvalidateSegments()
	{
#if !IS_UNIT_TESTS
		var parent = this.GetParent();
		if (parent is ISegmentsElement root)
		{
			root.InvalidateSegments();
		}
#endif
	}
}
