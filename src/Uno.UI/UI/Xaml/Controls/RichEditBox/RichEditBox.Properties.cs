using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RichEditBox
	{
		/// <summary>
		/// Gets or sets a value that indicates how text wrapping occurs.
		/// </summary>
		public TextWrapping TextWrapping
		{
			get => (TextWrapping)GetValue(TextWrappingProperty);
			set => SetValue(TextWrappingProperty, value);
		}

		public static DependencyProperty TextWrappingProperty { get; } =
			DependencyProperty.Register(
				nameof(TextWrapping),
				typeof(TextWrapping),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(TextWrapping.Wrap, OnTextWrappingChanged));

		private static void OnTextWrappingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is RichEditBox richEditBox)
			{
				richEditBox.OnTextWrappingChangedPartial();
			}
		}

		partial void OnTextWrappingChangedPartial();

		/// <summary>
		/// Gets or sets a value that indicates whether the user can interact with the control to edit text.
		/// </summary>
		public bool IsReadOnly
		{
			get => (bool)GetValue(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		public static DependencyProperty IsReadOnlyProperty { get; } =
			DependencyProperty.Register(
				nameof(IsReadOnly),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Gets or sets the text alignment.
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
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(TextAlignment.Left, OnTextAlignmentChanged));

		private static void OnTextAlignmentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is RichEditBox richEditBox)
			{
				richEditBox.OnTextAlignmentChangedPartial();
			}
		}

		partial void OnTextAlignmentChangedPartial();

		/// <summary>
		/// Gets or sets the brush used to highlight the selected text.
		/// </summary>
		public SolidColorBrush SelectionHighlightColor
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
			set => SetValue(SelectionHighlightColorProperty, value);
		}

		public static DependencyProperty SelectionHighlightColorProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColor),
				typeof(SolidColorBrush),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Gets or sets the text shown when the control has no content.
		/// </summary>
		public string PlaceholderText
		{
			get => (string)GetValue(PlaceholderTextProperty);
			set => SetValue(PlaceholderTextProperty, value);
		}

		public static DependencyProperty PlaceholderTextProperty { get; } =
			DependencyProperty.Register(
				nameof(PlaceholderText),
				typeof(string),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(string.Empty));

		/// <summary>
		/// Gets or sets the header content.
		/// </summary>
		public object Header
		{
			get => GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header),
				typeof(object),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null, OnHeaderChanged));

		private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is RichEditBox richEditBox)
			{
				richEditBox.UpdateHeaderVisibility();
			}
		}

		/// <summary>
		/// Gets or sets the DataTemplate used to display the header content.
		/// </summary>
		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplate),
				typeof(DataTemplate),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null, OnHeaderChanged));

		/// <summary>
		/// Gets or sets the maximum number of characters allowed for input.
		/// </summary>
		public int MaxLength
		{
			get => (int)GetValue(MaxLengthProperty);
			set => SetValue(MaxLengthProperty, value);
		}

		public static DependencyProperty MaxLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxLength),
				typeof(int),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(0));

		/// <summary>
		/// Gets or sets a value that indicates whether the text editing control
		/// accepts and displays the return/enter key.
		/// </summary>
		public bool AcceptsReturn
		{
			get => (bool)GetValue(AcceptsReturnProperty);
			set => SetValue(AcceptsReturnProperty, value);
		}

		public static DependencyProperty AcceptsReturnProperty { get; } =
			DependencyProperty.Register(
				nameof(AcceptsReturn),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(true));

		/// <summary>
		/// Gets or sets content that is shown below the control.
		/// </summary>
		public object Description
		{
			get => GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(
				nameof(Description),
				typeof(object),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Gets or sets a value that specifies whether text is rendered with all uppercase characters.
		/// </summary>
		public CharacterCasing CharacterCasing
		{
			get => (CharacterCasing)GetValue(CharacterCasingProperty);
			set => SetValue(CharacterCasingProperty, value);
		}

		public static DependencyProperty CharacterCasingProperty { get; } =
			DependencyProperty.Register(
				nameof(CharacterCasing),
				typeof(CharacterCasing),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(CharacterCasing.Normal));

		/// <summary>
		/// Gets or sets a flyout shown when text is selected.
		/// </summary>
		public FlyoutBase SelectionFlyout
		{
			get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
			set => SetValue(SelectionFlyoutProperty, value);
		}

		public static DependencyProperty SelectionFlyoutProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionFlyout),
				typeof(FlyoutBase),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Gets or sets a value that specifies the clipboard copy format.
		/// </summary>
		public RichEditClipboardFormat ClipboardCopyFormat
		{
			get => (RichEditClipboardFormat)GetValue(ClipboardCopyFormatProperty);
			set => SetValue(ClipboardCopyFormatProperty, value);
		}

		public static DependencyProperty ClipboardCopyFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(ClipboardCopyFormat),
				typeof(RichEditClipboardFormat),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(RichEditClipboardFormat.AllFormats));

		/// <summary>
		/// Gets or sets a value that indicates whether spell checking is enabled.
		/// </summary>
		public bool IsSpellCheckEnabled
		{
			get => (bool)GetValue(IsSpellCheckEnabledProperty);
			set => SetValue(IsSpellCheckEnabledProperty, value);
		}

		public static DependencyProperty IsSpellCheckEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSpellCheckEnabled),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(true));

		/// <summary>
		/// Gets or sets a value that indicates whether text prediction is enabled.
		/// </summary>
		public bool IsTextPredictionEnabled
		{
			get => (bool)GetValue(IsTextPredictionEnabledProperty);
			set => SetValue(IsTextPredictionEnabledProperty, value);
		}

		public static DependencyProperty IsTextPredictionEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextPredictionEnabled),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(true));

		/// <summary>
		/// Gets or sets a value that indicates which keyboard shortcuts for formatting are disabled.
		/// </summary>
		public DisabledFormattingAccelerators DisabledFormattingAccelerators
		{
			get => (DisabledFormattingAccelerators)GetValue(DisabledFormattingAcceleratorsProperty);
			set => SetValue(DisabledFormattingAcceleratorsProperty, value);
		}

		public static DependencyProperty DisabledFormattingAcceleratorsProperty { get; } =
			DependencyProperty.Register(
				nameof(DisabledFormattingAccelerators),
				typeof(DisabledFormattingAccelerators),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(DisabledFormattingAccelerators.None));

		/// <summary>
		/// Gets or sets the Input Method Editor (IME) input scope.
		/// </summary>
		public InputScope InputScope
		{
			get => (InputScope)GetValue(InputScopeProperty);
			set => SetValue(InputScopeProperty, value);
		}

		public static DependencyProperty InputScopeProperty { get; } =
			DependencyProperty.Register(
				nameof(InputScope),
				typeof(InputScope),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Gets or sets the brush used to highlight selected text when the control doesn't have focus.
		/// </summary>
		public SolidColorBrush SelectionHighlightColorWhenNotFocused
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorWhenNotFocusedProperty);
			set => SetValue(SelectionHighlightColorWhenNotFocusedProperty, value);
		}

		public static DependencyProperty SelectionHighlightColorWhenNotFocusedProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColorWhenNotFocused),
				typeof(SolidColorBrush),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null));

		/// <summary>
		/// Gets or sets the proofing menu flyout.
		/// </summary>
		public FlyoutBase ProofingMenuFlyout
		{
			get => (FlyoutBase)GetValue(ProofingMenuFlyoutProperty);
			set => SetValue(ProofingMenuFlyoutProperty, value);
		}

		public static DependencyProperty ProofingMenuFlyoutProperty { get; } =
			DependencyProperty.Register(
				nameof(ProofingMenuFlyout),
				typeof(FlyoutBase),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(null));
	}
}
