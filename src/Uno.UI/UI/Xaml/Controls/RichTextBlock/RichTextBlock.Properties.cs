#pragma warning disable CS0109

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Windows.UI.Text;
using Windows.Foundation;
using Microsoft.UI.Input;
using Uno;
using Uno.Foundation.Logging;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Microsoft.UI.Xaml.Controls
{
	partial class RichTextBlock
	{
		#region FontStyle Dependency Property

		public FontStyle FontStyle
		{
			get => (FontStyle)GetValue(FontStyleProperty);
			set => SetValue(FontStyleProperty, value);
		}

		public static DependencyProperty FontStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(FontStyle),
				typeof(FontStyle),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontStyle.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region FontStretch Dependency Property

		public FontStretch FontStretch
		{
			get => (FontStretch)GetValue(FontStretchProperty);
			set => SetValue(FontStretchProperty, value);
		}

		public static DependencyProperty FontStretchProperty { get; } =
			DependencyProperty.Register(
				nameof(FontStretch),
				typeof(FontStretch),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontStretch.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region TextWrapping Dependency Property

		public TextWrapping TextWrapping
		{
			get => (TextWrapping)GetValue(TextWrappingProperty);
			set => SetValue(TextWrappingProperty, value);
		}

		public static DependencyProperty TextWrappingProperty { get; } =
			DependencyProperty.Register(
				nameof(TextWrapping),
				typeof(TextWrapping),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextWrapping.Wrap,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region FontWeight Dependency Property

		public FontWeight FontWeight
		{
			get => (FontWeight)GetValue(FontWeightProperty);
			set => SetValue(FontWeightProperty, value);
		}

		public static DependencyProperty FontWeightProperty { get; } =
			DependencyProperty.Register(
				nameof(FontWeight),
				typeof(FontWeight),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontWeights.Normal,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region FontFamily Dependency Property

		public FontFamily FontFamily
		{
			get => (FontFamily)GetValue(FontFamilyProperty);
			set => SetValue(FontFamilyProperty, value);
		}

		public static DependencyProperty FontFamilyProperty { get; } =
			DependencyProperty.Register(
				nameof(FontFamily),
				typeof(FontFamily),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: FontFamily.Default,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region FontSize Dependency Property

		public double FontSize
		{
			get => (double)GetValue(FontSizeProperty);
			set => SetValue(FontSizeProperty, value);
		}

		public static DependencyProperty FontSizeProperty { get; } =
			DependencyProperty.Register(
				nameof(FontSize),
				typeof(double),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: 14.0,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region MaxLines Dependency Property

		public int MaxLines
		{
			get => (int)GetValue(MaxLinesProperty);
			set => SetValue(MaxLinesProperty, value);
		}

		public static DependencyProperty MaxLinesProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxLines),
				typeof(int),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region TextTrimming Dependency Property

		public TextTrimming TextTrimming
		{
			get => (TextTrimming)GetValue(TextTrimmingProperty);
			set => SetValue(TextTrimmingProperty, value);
		}

		public static DependencyProperty TextTrimmingProperty { get; } =
			DependencyProperty.Register(
				nameof(TextTrimming),
				typeof(TextTrimming),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextTrimming.None,
					options: FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region Foreground Dependency Property

		public Brush Foreground
		{
			get => (Brush)GetValue(ForegroundProperty);
			set => SetValue(ForegroundProperty, value);
		}

		public static DependencyProperty ForegroundProperty { get; } =
			DependencyProperty.Register(
				nameof(Foreground),
				typeof(Brush),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: SolidColorBrushHelper.Black,
					options: FrameworkPropertyMetadataOptions.Inherits,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).SubscribeForeground((Brush)e.OldValue, (Brush)e.NewValue)
				)
			);

		private void SubscribeForeground(Brush oldValue, Brush newValue)
		{
			var newOnInvalidateRender = _foregroundChanged ?? (() => OnForegroundChanged());

			_foregroundBrushChangedSubscription?.Dispose();
			_foregroundBrushChangedSubscription = Brush.SetupBrushChanged(newValue, ref _foregroundChanged, newOnInvalidateRender);
		}

		private void OnForegroundChanged()
		{
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

		#region IsTextSelectionEnabled Dependency Property

		public bool IsTextSelectionEnabled
		{
			get => (bool)GetValue(IsTextSelectionEnabledProperty);
			set => SetValue(IsTextSelectionEnabledProperty, value);
		}

		public static DependencyProperty IsTextSelectionEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextSelectionEnabled),
				typeof(bool),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: false,
					propertyChangedCallback: (s, _) => ((RichTextBlock)s).OnIsTextSelectionEnabledChanged()
				)
			);

		private void OnIsTextSelectionEnabledChanged()
		{
			ProtectedCursor = IsTextSelectionEnabled ? InputSystemCursor.Create(InputSystemCursorShape.IBeam) : null;
			RecalculateSubscribeToPointerEvents();
			OnIsTextSelectionEnabledChangedPartial();
		}

		partial void OnIsTextSelectionEnabledChangedPartial();

		#endregion

		#region TextAlignment Dependency Property

		public new TextAlignment TextAlignment
		{
			get => (TextAlignment)GetValue(TextAlignmentProperty);
			set => SetValue(TextAlignmentProperty, value);
		}

		public static DependencyProperty TextAlignmentProperty { get; } =
			DependencyProperty.Register(
				nameof(TextAlignment),
				typeof(TextAlignment),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextAlignment.Left,
					options: FrameworkPropertyMetadataOptions.AffectsArrange,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnTextAlignmentChanged()
				)
			);

		private void OnTextAlignmentChanged()
		{
			HorizontalTextAlignment = TextAlignment;
			InvalidateRichTextBlock();
		}

		#endregion

		#region HorizontalTextAlignment Dependency Property

		public new TextAlignment HorizontalTextAlignment
		{
			get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
			set => SetValue(HorizontalTextAlignmentProperty, value);
		}

		public static DependencyProperty HorizontalTextAlignmentProperty { get; } =
			DependencyProperty.Register(
				nameof(HorizontalTextAlignment),
				typeof(TextAlignment),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextAlignment.Left,
					options: FrameworkPropertyMetadataOptions.AffectsArrange,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnHorizontalTextAlignmentChanged()
				)
			);

		// This property provides the same functionality as the TextAlignment property.
		// If both properties are set to conflicting values, the last one set is used.
		private void OnHorizontalTextAlignmentChanged() => TextAlignment = HorizontalTextAlignment;

		#endregion

		#region LineHeight Dependency Property

		public double LineHeight
		{
			get => (double)GetValue(LineHeightProperty);
			set => SetValue(LineHeightProperty, value);
		}

		public static DependencyProperty LineHeightProperty { get; } =
			DependencyProperty.Register(
				nameof(LineHeight),
				typeof(double),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					0d,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()));

		#endregion

		#region LineStackingStrategy Dependency Property

		public LineStackingStrategy LineStackingStrategy
		{
			get => (LineStackingStrategy)GetValue(LineStackingStrategyProperty);
			set => SetValue(LineStackingStrategyProperty, value);
		}

		public static DependencyProperty LineStackingStrategyProperty { get; } =
			DependencyProperty.Register(
				nameof(LineStackingStrategy),
				typeof(LineStackingStrategy),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					LineStackingStrategy.MaxHeight,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()));

		#endregion

		#region Padding Dependency Property

		public Thickness Padding
		{
			get => (Thickness)GetValue(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		public static DependencyProperty PaddingProperty { get; } =
			DependencyProperty.Register(
				nameof(Padding),
				typeof(Thickness),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					(Thickness)Thickness.Empty,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()));

		#endregion

		#region CharacterSpacing Dependency Property

		public int CharacterSpacing
		{
			get => (int)GetValue(CharacterSpacingProperty);
			set => SetValue(CharacterSpacingProperty, value);
		}

		public static DependencyProperty CharacterSpacingProperty { get; } =
			DependencyProperty.Register(
				nameof(CharacterSpacing),
				typeof(int),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: 0,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region TextDecorations Dependency Property

		public TextDecorations TextDecorations
		{
			get => (TextDecorations)GetValue(TextDecorationsProperty);
			set => SetValue(TextDecorationsProperty, value);
		}

		public static DependencyProperty TextDecorationsProperty { get; } =
			DependencyProperty.Register(
				nameof(TextDecorations),
				typeof(TextDecorations),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: TextDecorations.None,
					options: FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()
				)
			);

		#endregion

		#region TextIndent Dependency Property

		public double TextIndent
		{
			get => (double)GetValue(TextIndentProperty);
			set => SetValue(TextIndentProperty, value);
		}

		public static DependencyProperty TextIndentProperty { get; } =
			DependencyProperty.Register(
				nameof(TextIndent),
				typeof(double),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					0.0,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).InvalidateRichTextBlock()));

		#endregion

		#region TextHighlighters

		public IList<TextHighlighter> TextHighlighters { get; } = new ObservableCollection<TextHighlighter>();

		#endregion

		#region DependencyProperty: IsTextTrimmed

		private TypedEventHandler<RichTextBlock, IsTextTrimmedChangedEventArgs> _isTextTrimmedChanged;

		public event TypedEventHandler<RichTextBlock, IsTextTrimmedChangedEventArgs> IsTextTrimmedChanged
		{
			add => _isTextTrimmedChanged += value;
			remove => _isTextTrimmedChanged -= value;
		}

		public static DependencyProperty IsTextTrimmedProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextTrimmed),
				typeof(bool),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(false, propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnIsTextTrimmedChanged()));

		public bool IsTextTrimmed
		{
			get => (bool)GetValue(IsTextTrimmedProperty);
			private set => SetValue(IsTextTrimmedProperty, value);
		}

		private void OnIsTextTrimmedChanged()
		{
			_isTextTrimmedChanged?.Invoke(this, new());
		}

		#endregion

		#region HasOverflowContent Dependency Property

		public static DependencyProperty HasOverflowContentProperty { get; } =
			DependencyProperty.Register(
				nameof(HasOverflowContent),
				typeof(bool),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(false));

		public bool HasOverflowContent
		{
			get => (bool)GetValue(HasOverflowContentProperty);
			private set => SetValue(HasOverflowContentProperty, value);
		}

		#endregion

		#region OverflowContentTarget Dependency Property

		public RichTextBlockOverflow OverflowContentTarget
		{
			get => (RichTextBlockOverflow)GetValue(OverflowContentTargetProperty);
			set => SetValue(OverflowContentTargetProperty, value);
		}

		public static DependencyProperty OverflowContentTargetProperty { get; } =
			DependencyProperty.Register(
				nameof(OverflowContentTarget),
				typeof(RichTextBlockOverflow),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(null, (s, e) => ((RichTextBlock)s).OnOverflowContentTargetChanged(
					(RichTextBlockOverflow)e.OldValue, (RichTextBlockOverflow)e.NewValue)));

		private void OnOverflowContentTargetChanged(RichTextBlockOverflow oldTarget, RichTextBlockOverflow newTarget)
		{
			// TODO: Implement content overflow support
			InvalidateRichTextBlock();
		}

		#endregion

		#region SelectionChanged

		public event RoutedEventHandler SelectionChanged;

		internal void RaiseSelectionChanged() => SelectionChanged?.Invoke(this, new RoutedEventArgs(this));

		#endregion

		#region ContextMenuOpening

#pragma warning disable CS0067 // Event is never used (subscribed to on Skia only)
		public event ContextMenuOpeningEventHandler ContextMenuOpening;
#pragma warning restore CS0067

		#endregion

		#region SelectionHighlightColor (DP)

		public SolidColorBrush SelectionHighlightColor
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
			set => SetValue(SelectionHighlightColorProperty, value);
		}

		public static DependencyProperty SelectionHighlightColorProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColor),
				typeof(SolidColorBrush),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => ((RichTextBlock)s).OnSelectionHighlightColorChangedPartial((SolidColorBrush)e.OldValue, (SolidColorBrush)e.NewValue)));

		partial void OnSelectionHighlightColorChangedPartial(SolidColorBrush oldBrush, SolidColorBrush newBrush);

		#endregion

		#region SelectionFlyout (DP)

		public static DependencyProperty SelectionFlyoutProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionFlyout), typeof(FlyoutBase), typeof(RichTextBlock),
				new FrameworkPropertyMetadata(default(FlyoutBase), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		public FlyoutBase SelectionFlyout
		{
			get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
			set => SetValue(SelectionFlyoutProperty, value);
		}

		#endregion

		#region SelectedText (DP - readonly)

		public static DependencyProperty SelectedTextProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectedText), typeof(string),
				typeof(RichTextBlock),
				new FrameworkPropertyMetadata(string.Empty));

		public string SelectedText
		{
			get => (string)GetValue(SelectedTextProperty);
			private set => SetValue(SelectedTextProperty, value);
		}

		#endregion

#pragma warning disable IDE0051 // Implemented in platform-specific partial
		partial void UpdateIsTextTrimmed();
#pragma warning restore IDE0051

		internal void OnFontLoaded() => InvalidateRichTextBlock();

#if __SKIA__
		private bool IsTextTrimmable =>
			TextTrimming != TextTrimming.None ||
			MaxLines != 0;
#endif
	}
}
