#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Documents.TextFormatting;
using Windows.UI.Xaml.Markup;
using Windows.UI.Xaml.Media;
using Uno.Foundation.Logging;
using RadialGradientBrush = Microsoft.UI.Xaml.Media.RadialGradientBrush;

namespace Windows.UI.Xaml.Controls;

[ContentProperty(Name = nameof(Blocks))]
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || false || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
#endif
public sealed partial class RichTextBlock : global::Windows.UI.Xaml.FrameworkElement, ISegmentsElement
{
	private BlockCollection _blocks;
	private Action _foregroundChanged;
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || false || __NETSTD_REFERENCE__ || __MACOS__

	[global::Uno.NotImplemented]
#endif

#if !__SKIA__
	public RichTextBlock() : base()
	{
#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || false || __NETSTD_REFERENCE__ || __MACOS__
		global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.RichTextBlock", "RichTextBlock.RichTextBlock()");
#endif
	}
#endif

	#region ITextVisualElement implementation

	void ISegmentsElement.InvalidateSegments()
		=> InvalidateSegments();

	void ISegmentsElement.InvalidateElement()
		=> InvalidateElement();

	internal void InvalidateSegments()
		=> OnBlocksInvalidated();

	internal void InvalidateElement()
		=> OnBlocksInvalidated();

	#endregion

	#region Blocks

	public BlockCollection Blocks
	{
		get => _blocks ??= new BlockCollection(this);
	}

	private void OnBlocksInvalidated()
	{
		OnBlocksChangedPartial();
		InvalidateRichTextBlock();
	}

	partial void OnBlocksChangedPartial();

	#endregion

	#region TextWrapping Dependency Property

	public TextWrapping TextWrapping
	{
		get => (TextWrapping)GetValue(TextWrappingProperty);
		set => SetValue(TextWrappingProperty, value);
	}

	public static DependencyProperty TextWrappingProperty { get; } =
		DependencyProperty.Register(nameof(TextWrapping), typeof(TextWrapping), typeof(RichTextBlock),
			new FrameworkPropertyMetadata(
				defaultValue: TextWrapping.NoWrap,
				propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnTextWrappingChanged()
			)
		);

	private void OnTextWrappingChanged()
	{
		OnTextWrappingChangedPartial();
		InvalidateRichTextBlock();
	}

	partial void OnTextWrappingChangedPartial();

	#endregion

	#region TextAlignment Dependency Property

	public TextAlignment TextAlignment
	{
		get => (TextAlignment)GetValue(TextAlignmentProperty);
		set => SetValue(TextAlignmentProperty, value);
	}

	public static DependencyProperty TextAlignmentProperty { get; } =
		DependencyProperty.Register(nameof(TextAlignment), typeof(TextAlignment), typeof(RichTextBlock),
			new FrameworkPropertyMetadata(
				defaultValue: TextAlignment.Left,
				propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnTextAlignmentChanged()
			)
		);

	private void OnTextAlignmentChanged()
	{
		HorizontalTextAlignment = TextAlignment;
		OnTextAlignmentChangedPartial();
		InvalidateRichTextBlock();
	}

	partial void OnTextAlignmentChangedPartial();

	#endregion

	#region HorizontalTextAlignment Dependency Property

	public TextAlignment HorizontalTextAlignment
	{
		get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
		set => SetValue(HorizontalTextAlignmentProperty, value);
	}

	public static DependencyProperty HorizontalTextAlignmentProperty { get; } =
		DependencyProperty.Register(nameof(HorizontalTextAlignment), typeof(TextAlignment), typeof(RichTextBlock),
			new FrameworkPropertyMetadata(
				defaultValue: TextAlignment.Left,
				propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnHorizontalTextAlignmentChanged()
			)
		);

	// This property provides the same functionality as the TextAlignment property.
	// If both properties are set to conflicting values, the last one set is used.
	// https://docs.microsoft.com/en-us/uwp/api/windows.ui.xaml.controls.textbox.horizontaltextalignment#remarks
	private void OnHorizontalTextAlignmentChanged() => TextAlignment = HorizontalTextAlignment;

	#endregion

	#region LineHeight Dependency Property

	public double LineHeight
	{
		get => (double)GetValue(LineHeightProperty);
		set => SetValue(LineHeightProperty, value);
	}

	public static DependencyProperty LineHeightProperty { get; } =
		DependencyProperty.Register(nameof(LineHeight), typeof(double), typeof(RichTextBlock), 
			new FrameworkPropertyMetadata(
				defaultValue: 0d,
				propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnLineHeightChanged()
			)
		);

	private void OnLineHeightChanged()
	{
		OnLineHeightChangedPartial();
		InvalidateRichTextBlock();
	}

	partial void OnLineHeightChangedPartial();

	#endregion

	#region MaxLines Dependency Property

	public int MaxLines
	{
		get => (int)GetValue(MaxLinesProperty);
		set => SetValue(MaxLinesProperty, value);
	}

	public static DependencyProperty MaxLinesProperty { get; } =
		DependencyProperty.Register(nameof(MaxLines), typeof(int), typeof(RichTextBlock),
			new FrameworkPropertyMetadata(
				defaultValue: 0,
				propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnMaxLinesChanged()
			)
		);

	private void OnMaxLinesChanged()
	{
		OnMaxLinesChangedPartial();
		InvalidateRichTextBlock();
	}

	partial void OnMaxLinesChangedPartial();

	#endregion

	#region LineStackingStrategy Dependency Property

	public LineStackingStrategy LineStackingStrategy
	{
		get => (LineStackingStrategy)GetValue(LineStackingStrategyProperty);
		set => SetValue(LineStackingStrategyProperty, value);
	}

	public static DependencyProperty LineStackingStrategyProperty { get; } =
		DependencyProperty.Register(nameof(LineStackingStrategy), typeof(LineStackingStrategy), typeof(RichTextBlock),
			new FrameworkPropertyMetadata(
				defaultValue: LineStackingStrategy.MaxHeight,
				propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnLineStackingStrategyChanged()
			)
		);

	private void OnLineStackingStrategyChanged()
	{
		OnLineStackingStrategyChangedPartial();
		InvalidateRichTextBlock();
	}

	partial void OnLineStackingStrategyChangedPartial();

	#endregion

	#region Foreground Dependency Property

	public
#if __ANDROID__
	new
#endif
		Brush Foreground
	{
		get => (Brush)GetValue(ForegroundProperty);
		set
		{
#if !__WASM__
			if (value is SolidColorBrush || value is GradientBrush || value is RadialGradientBrush || value is null)
			{
				SetValue(ForegroundProperty, value);
			}
			else
			{
				throw new NotSupportedException("Only SolidColorBrush or GradientBrush's FallbackColor are supported.");
			}
#else
			SetValue(ForegroundProperty, value);
#endif
		}
	}

	public static DependencyProperty ForegroundProperty { get; } =
		DependencyProperty.Register(nameof(Foreground), typeof(Brush), typeof(RichTextBlock),
			new FrameworkPropertyMetadata(
				defaultValue: SolidColorBrushHelper.Black,
				options: FrameworkPropertyMetadataOptions.Inherits,
				propertyChangedCallback: (s, e) => ((RichTextBlock)s).Subscribe((Brush)e.OldValue, (Brush)e.NewValue)
			)
		);

	private void Subscribe(Brush oldValue, Brush newValue)
	{
		var newOnInvalidateRender = _foregroundChanged ?? (() => OnForegroundChanged());
		Brush.SetupBrushChanged(oldValue, newValue, ref _foregroundChanged, newOnInvalidateRender);
	}

	private void OnForegroundChanged()
	{
		// The try-catch here is primarily for the benefit of Android. This callback is raised when (say) the brush color changes,
		// which may happen when the system theme changes from light to dark. For app-level resources, a large number of views may
		// be subscribed to changes on the brush, including potentially some that have been removed from the visual tree, collected
		// on the native side, but not yet collected on the managed side (for Xamarin targets).

		// On Android, in practice this could result in ObjectDisposedExceptions when calling RequestLayout(). The try/catch is to
		// ensure that callbacks are correctly raised for remaining views referencing the brush which *are* still live in the visual tree.
		try
		{
			OnForegroundChangedPartial();
			InvalidateRichTextBlock();
		}
		catch (Exception e)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"Failed to invalidate for brush changed: {e}");
			}
		}
	}

	partial void OnForegroundChangedPartial();

	#endregion

	internal override bool CanHaveChildren() => true;

	public new bool Focus(FocusState value) => base.Focus(value);

	private void InvalidateRichTextBlock()
	{
		InvalidateRichTextBlockPartial();
		InvalidateMeasure();
	}

	partial void InvalidateRichTextBlockPartial();

	internal override bool IsFocusable =>
		/*IsActive() &&*/ //TODO Uno: No concept of IsActive in Uno yet.
		IsVisible() &&
		/*IsEnabled() &&*/ (IsTextSelectionEnabled || IsTabStop) &&
		AreAllAncestorsVisible();
}
