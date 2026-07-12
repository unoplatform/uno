#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	public partial class RichEditBox
	{
		/// <summary>
		/// Identifies the <see cref="AcceptsReturn"/> dependency property.
		/// </summary>
		public static DependencyProperty AcceptsReturnProperty { get; } =
			DependencyProperty.Register(
				nameof(AcceptsReturn),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(defaultValue: true));

		/// <summary>
		/// Gets or sets a value that indicates whether the control accepts newline characters.
		/// </summary>
		public bool AcceptsReturn
		{
			get => (bool)GetValue(AcceptsReturnProperty);
			set => SetValue(AcceptsReturnProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="CharacterCasing"/> dependency property.
		/// </summary>
		public static DependencyProperty CharacterCasingProperty { get; } =
			DependencyProperty.Register(
				nameof(CharacterCasing),
				typeof(CharacterCasing),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(CharacterCasing.Normal));

		/// <summary>
		/// Gets or sets how characters are cased as they are entered.
		/// </summary>
		public CharacterCasing CharacterCasing
		{
			get => (CharacterCasing)GetValue(CharacterCasingProperty);
			set => SetValue(CharacterCasingProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="ClipboardCopyFormat"/> dependency property.
		/// </summary>
		public static DependencyProperty ClipboardCopyFormatProperty { get; } =
			DependencyProperty.Register(
				nameof(ClipboardCopyFormat),
				typeof(RichEditClipboardFormat),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(RichEditClipboardFormat.AllFormats));

		/// <summary>
		/// Gets or sets whether copied content includes rich formatting or plain text only.
		/// </summary>
		public RichEditClipboardFormat ClipboardCopyFormat
		{
			get => (RichEditClipboardFormat)GetValue(ClipboardCopyFormatProperty);
			set => SetValue(ClipboardCopyFormatProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="Description"/> dependency property.
		/// </summary>
		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(
				nameof(Description),
				typeof(object),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(object)));

		/// <summary>
		/// Gets or sets content displayed below the control.
		/// </summary>
		public object Description
		{
			get => GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="Header"/> dependency property.
		/// </summary>
		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header),
				typeof(object),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(object), FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		/// Gets or sets the content displayed as the control header.
		/// </summary>
		public object Header
		{
			get => GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="HeaderTemplate"/> dependency property.
		/// </summary>
		public static DependencyProperty HeaderTemplateProperty { get; } =
			DependencyProperty.Register(
				nameof(HeaderTemplate),
				typeof(DataTemplate),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					default(DataTemplate),
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext | FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		/// Gets or sets the template used to display the control header.
		/// </summary>
		public DataTemplate HeaderTemplate
		{
			get => (DataTemplate)GetValue(HeaderTemplateProperty);
			set => SetValue(HeaderTemplateProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="HorizontalTextAlignment"/> dependency property.
		/// </summary>
		public static DependencyProperty HorizontalTextAlignmentProperty { get; } =
			DependencyProperty.Register(
				nameof(HorizontalTextAlignment),
				typeof(TextAlignment),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					TextAlignment.Left,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					OnHorizontalTextAlignmentChanged));

		/// <summary>
		/// Gets or sets a value that indicates how text is aligned in the control.
		/// </summary>
		public TextAlignment HorizontalTextAlignment
		{
			get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
			set => SetValue(HorizontalTextAlignmentProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsSpellCheckEnabled"/> dependency property.
		/// </summary>
		public static DependencyProperty IsSpellCheckEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSpellCheckEnabled),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(true, OnIsSpellCheckEnabledChanged));

		/// <summary>
		/// Gets or sets a value that indicates whether spell checking is enabled.
		/// </summary>
		public bool IsSpellCheckEnabled
		{
			get => (bool)GetValue(IsSpellCheckEnabledProperty);
			set => SetValue(IsSpellCheckEnabledProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsTextPredictionEnabled"/> dependency property.
		/// </summary>
		public static DependencyProperty IsTextPredictionEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextPredictionEnabled),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(true, OnIsTextPredictionEnabledChanged));

		/// <summary>
		/// Gets or sets a value that indicates whether text prediction is enabled.
		/// </summary>
		public bool IsTextPredictionEnabled
		{
			get => (bool)GetValue(IsTextPredictionEnabledProperty);
			set => SetValue(IsTextPredictionEnabledProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsReadOnly"/> dependency property.
		/// </summary>
		public static DependencyProperty IsReadOnlyProperty { get; } =
			DependencyProperty.Register(
				nameof(IsReadOnly),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(bool), OnIsReadOnlyChanged));

		/// <summary>
		/// Gets or sets a value that indicates whether the user can change the text.
		/// </summary>
		public bool IsReadOnly
		{
			get => (bool)GetValue(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="MaxLength"/> dependency property.
		/// </summary>
		public static DependencyProperty MaxLengthProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxLength),
				typeof(int),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					default(int),
					OnMaxLengthChanged,
					CoerceMaxLength));

		/// <summary>
		/// Gets or sets the maximum number of characters allowed for user input.
		/// </summary>
		public int MaxLength
		{
			get => (int)GetValue(MaxLengthProperty);
			set => SetValue(MaxLengthProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="PlaceholderText"/> dependency property.
		/// </summary>
		public static DependencyProperty PlaceholderTextProperty { get; } =
			DependencyProperty.Register(
				nameof(PlaceholderText),
				typeof(string),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		/// Gets or sets the text displayed when the control is empty.
		/// </summary>
		public string PlaceholderText
		{
			get => (string)GetValue(PlaceholderTextProperty);
			set => SetValue(PlaceholderTextProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="SelectionHighlightColor"/> dependency property.
		/// </summary>
		public static DependencyProperty SelectionHighlightColorProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColor),
				typeof(SolidColorBrush),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(DefaultBrushes.SelectionHighlightColor, OnSelectionHighlightColorChanged));

		/// <summary>
		/// Gets or sets the brush used to highlight selected text.
		/// </summary>
		public SolidColorBrush SelectionHighlightColor
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorProperty);
			set => SetValue(SelectionHighlightColorProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="SelectionHighlightColorWhenNotFocused"/> dependency property.
		/// </summary>
		public static DependencyProperty SelectionHighlightColorWhenNotFocusedProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColorWhenNotFocused),
				typeof(SolidColorBrush),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(SolidColorBrush), OnSelectionHighlightColorChanged));

		/// <summary>
		/// Gets or sets the brush used to highlight selected text when the control is not focused.
		/// </summary>
		public SolidColorBrush SelectionHighlightColorWhenNotFocused
		{
			get => (SolidColorBrush)GetValue(SelectionHighlightColorWhenNotFocusedProperty);
			set => SetValue(SelectionHighlightColorWhenNotFocusedProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="TextAlignment"/> dependency property.
		/// </summary>
		public static DependencyProperty TextAlignmentProperty { get; } =
			DependencyProperty.Register(
				nameof(TextAlignment),
				typeof(TextAlignment),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					TextAlignment.Left,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					OnTextAlignmentChanged));

		/// <summary>
		/// Gets or sets a value that indicates how text is aligned in the control.
		/// </summary>
		public TextAlignment TextAlignment
		{
			get => (TextAlignment)GetValue(TextAlignmentProperty);
			set => SetValue(TextAlignmentProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="TextReadingOrder"/> dependency property.
		/// </summary>
		public static DependencyProperty TextReadingOrderProperty { get; } =
			DependencyProperty.Register(
				nameof(TextReadingOrder),
				typeof(TextReadingOrder),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(TextReadingOrder.DetectFromContent, FrameworkPropertyMetadataOptions.AffectsMeasure));

		/// <summary>
		/// Gets or sets a value that indicates how the reading order is determined.
		/// </summary>
		public TextReadingOrder TextReadingOrder
		{
			get => (TextReadingOrder)GetValue(TextReadingOrderProperty);
			set => SetValue(TextReadingOrderProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="TextWrapping"/> dependency property.
		/// </summary>
		public static DependencyProperty TextWrappingProperty { get; } =
			DependencyProperty.Register(
				nameof(TextWrapping),
				typeof(TextWrapping),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					TextWrapping.Wrap,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					OnTextWrappingChanged,
					CoerceTextWrapping));

		/// <summary>
		/// Gets or sets a value that indicates how text wrapping occurs.
		/// </summary>
		public TextWrapping TextWrapping
		{
			get => (TextWrapping)GetValue(TextWrappingProperty);
			set => SetValue(TextWrappingProperty, value);
		}

		private static void OnHorizontalTextAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (RichEditBox)sender;
			var value = (TextAlignment)args.NewValue;
			if (owner.TextAlignment != value)
			{
				owner.SetValue(TextAlignmentProperty, value);
			}

			owner._textBoxView?.SetTextAlignment();
		}

		private static void OnTextAlignmentChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (RichEditBox)sender;
			var value = (TextAlignment)args.NewValue;
			if (owner.HorizontalTextAlignment != value)
			{
				owner.SetValue(HorizontalTextAlignmentProperty, value);
			}

			owner._textBoxView?.SetTextAlignment();
		}

		private static void OnIsSpellCheckEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			if (((RichEditBox)sender)._textBoxView is { } view)
			{
				view.DisplayBlock.IsSpellCheckEnabled = (bool)args.NewValue;
				view.UpdateProperties();
			}
		}

		private static void OnIsTextPredictionEnabledChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
			=> ((RichEditBox)sender)._textBoxView?.UpdateProperties();

		private static void OnIsReadOnlyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (RichEditBox)sender;
			var oldValue = (bool)args.OldValue;
			var newValue = (bool)args.NewValue;

			if (newValue)
			{
				owner.EndImeSession();
				owner.StopCaret();
			}
			else if (owner.FocusState != FocusState.Unfocused)
			{
				owner.ResumeCaret();
				owner.StartImeSession();
			}
			else
			{
				owner.UpdateDisplaySelection();
			}

			owner._textBoxView?.UpdateProperties();
			if (AutomationPeer.ListenerExistsHelper(AutomationEvents.PropertyChanged)
				&& owner.GetOrCreateAutomationPeer() is RichEditBoxAutomationPeer peer)
			{
				peer.RaiseIsReadOnlyPropertyChangedEvent(oldValue, newValue);
			}
		}

		private static void OnMaxLengthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
			=> ((RichEditBox)sender)._textBoxView?.UpdateMaxLength();

		private static object CoerceMaxLength(DependencyObject sender, object baseValue, DependencyPropertyValuePrecedences precedence)
		{
			var value = (int)baseValue;
			if (value < 0)
			{
				throw new ArgumentException("MaxLength cannot be negative.", nameof(baseValue));
			}

			return value;
		}

		private static void OnSelectionHighlightColorChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
			=> ((RichEditBox)sender).UpdateSelectionHighlightColor();

		private static void OnTextWrappingChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
		{
			var owner = (RichEditBox)sender;
			owner._textBoxView?.SetWrapping();
			if (owner._contentElement is ScrollViewer scrollViewer)
			{
				scrollViewer.HorizontalScrollBarVisibility = owner.TextWrapping == TextWrapping.NoWrap
					? ScrollBarVisibility.Auto
					: ScrollBarVisibility.Disabled;
			}
		}

		private static object CoerceTextWrapping(DependencyObject sender, object baseValue, DependencyPropertyValuePrecedences precedence)
		{
			var value = (TextWrapping)baseValue;
			if (value == TextWrapping.WrapWholeWords)
			{
				throw new ArgumentException("RichEditBox does not support TextWrapping.WrapWholeWords.", nameof(baseValue));
			}

			return value;
		}
	}
}