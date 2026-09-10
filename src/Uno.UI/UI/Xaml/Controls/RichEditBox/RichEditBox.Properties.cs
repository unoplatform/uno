// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference dxaml/xcp/tools/XCPTypesAutoGen/Modules/Controls/RichEditBox.cs, commit 3c9c168844f06c6ac000a97977f0bb3f4c90fd75
#nullable enable

using System;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls
{
	partial class RichEditBox
	{
		/// <summary>Gets an object that facilitates programmatic access to the text and formatting properties of the content of the RichEditBox.</summary>
		public global::Microsoft.UI.Text.RichEditTextDocument Document => GetDocumentImpl();

		/// <summary>Gets an object that enables you to access and modify the text in a rich edit control.</summary>
		public global::Microsoft.UI.Text.RichEditTextDocument TextDocument => GetTextDocumentImpl();

		/// <summary>Occurs when content changes in the RichEditBox.</summary>
		public event RoutedEventHandler? TextChanged;

		/// <summary>Occurs when the text selection has changed.</summary>
		public event RoutedEventHandler? SelectionChanged;

		/// <summary>Occurs when the system processes an interaction that displays a context menu.</summary>
		public event ContextMenuOpeningEventHandler? ContextMenuOpening;

		/// <summary>Occurs when text is pasted into the control.</summary>
		public event TextControlPasteEventHandler? Paste;

		/// <summary>Occurs when the Input Method Editor (IME) candidate window opens, updates, or closes.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, CandidateWindowBoundsChangedEventArgs>? CandidateWindowBoundsChanged
		{
			add => AddCandidateWindowBoundsChanged(value);
			remove => _candidateWindowBoundsChanged -= value;
		}

		/// <summary>Occurs synchronously when text in the RichEditBox starts to change, but before it is rendered.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, RichEditBoxTextChangingEventArgs>? TextChanging
		{
			add => AddTextChangingHandler(value);
			remove => _textChanging -= value;
		}

		/// <summary>Occurs when text being composed through an Input Method Editor starts to change.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, TextCompositionStartedEventArgs>? TextCompositionStarted;

		/// <summary>Occurs when text being composed through an Input Method Editor changes.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, TextCompositionChangedEventArgs>? TextCompositionChanged;

		/// <summary>Occurs when text being composed through an Input Method Editor is committed or canceled.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, TextCompositionEndedEventArgs>? TextCompositionEnded;

		/// <summary>Occurs before selected text is copied to the clipboard.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, TextControlCopyingToClipboardEventArgs>? CopyingToClipboard;

		/// <summary>Occurs before selected text is cut to the clipboard.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, TextControlCuttingToClipboardEventArgs>? CuttingToClipboard;

		/// <summary>Occurs before the selection changes. Set Cancel to cancel the change.</summary>
		public event global::Windows.Foundation.TypedEventHandler<RichEditBox, RichEditBoxSelectionChangingEventArgs>? SelectionChanging;

		/// <summary>Identifies the DisabledFormattingAccelerators dependency property.</summary>
		public static DependencyProperty DisabledFormattingAcceleratorsProperty { get; } =
			DependencyProperty.Register(
				nameof(DisabledFormattingAccelerators),
				typeof(DisabledFormattingAccelerators),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(DisabledFormattingAccelerators), OnRichEditBoxPropertyChanged));

		/// <summary>Gets or sets which keyboard shortcuts for formatting are disabled.</summary>
		public DisabledFormattingAccelerators DisabledFormattingAccelerators
		{
			get => (DisabledFormattingAccelerators)GetValue(DisabledFormattingAcceleratorsProperty);
			set => SetValue(DisabledFormattingAcceleratorsProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="AcceptsReturn"/> dependency property.
		/// </summary>
		public static DependencyProperty AcceptsReturnProperty { get; } =
			DependencyProperty.Register(
				nameof(AcceptsReturn),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(defaultValue: true, OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets a value that indicates whether the control accepts newline characters.
		/// </summary>
		public bool AcceptsReturn
		{
			get => (bool)GetValue(AcceptsReturnProperty);
			set => SetAcceptsReturn(value);
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
				new FrameworkPropertyMetadata(RichEditClipboardFormat.AllFormats, OnRichEditBoxPropertyChanged));

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
					TextAlignment.DetectFromContent,
					FrameworkPropertyMetadataOptions.AffectsArrange,
					OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets a value that indicates how text is aligned in the control.
		/// </summary>
		public TextAlignment HorizontalTextAlignment
		{
			get => (TextAlignment)GetValue(HorizontalTextAlignmentProperty);
			set => SetValue(HorizontalTextAlignmentProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="DesiredCandidateWindowAlignment"/> dependency property.
		/// </summary>
		public static DependencyProperty DesiredCandidateWindowAlignmentProperty { get; } =
			DependencyProperty.Register(
				nameof(DesiredCandidateWindowAlignment),
				typeof(CandidateWindowAlignment),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					CandidateWindowAlignment.Default,
					OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets the preferred alignment of the input method candidate window.
		/// </summary>
		public CandidateWindowAlignment DesiredCandidateWindowAlignment
		{
			get => (CandidateWindowAlignment)GetValue(DesiredCandidateWindowAlignmentProperty);
			set => SetValue(DesiredCandidateWindowAlignmentProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsSpellCheckEnabled"/> dependency property.
		/// </summary>
		public static DependencyProperty IsSpellCheckEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSpellCheckEnabled),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(true, OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets a value that indicates whether spell checking is enabled.
		/// </summary>
		public bool IsSpellCheckEnabled
		{
			get => (bool)GetValue(IsSpellCheckEnabledProperty);
			set => SetValue(IsSpellCheckEnabledProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsColorFontEnabled"/> dependency property.
		/// </summary>
		public static DependencyProperty IsColorFontEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsColorFontEnabled),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					true,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets a value that determines whether color font glyphs are enabled.
		/// </summary>
		public bool IsColorFontEnabled
		{
			get => (bool)GetValue(IsColorFontEnabledProperty);
			set => SetValue(IsColorFontEnabledProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsTextPredictionEnabled"/> dependency property.
		/// </summary>
		public static DependencyProperty IsTextPredictionEnabledProperty { get; } =
			DependencyProperty.Register(
				nameof(IsTextPredictionEnabled),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(true, OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets a value that indicates whether text prediction is enabled.
		/// </summary>
		public bool IsTextPredictionEnabled
		{
			get => (bool)GetValue(IsTextPredictionEnabledProperty);
			set => SetValue(IsTextPredictionEnabledProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="InputScope"/> dependency property.
		/// </summary>
		public static DependencyProperty InputScopeProperty { get; } =
			DependencyProperty.Register(
				nameof(InputScope),
				typeof(InputScope),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(
					default(InputScope),
					OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets the input scope used by software keyboards and IME services.
		/// </summary>
		public InputScope InputScope
		{
			get => (InputScope)GetValue(InputScopeProperty);
			set => SetValue(InputScopeProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="IsReadOnly"/> dependency property.
		/// </summary>
		public static DependencyProperty IsReadOnlyProperty { get; } =
			DependencyProperty.Register(
				nameof(IsReadOnly),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(bool), OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets a value that indicates whether the user can change the text.
		/// </summary>
		public bool IsReadOnly
		{
			get => (bool)GetValue(IsReadOnlyProperty);
			set => SetValue(IsReadOnlyProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="PreventKeyboardDisplayOnProgrammaticFocus"/> dependency property.
		/// </summary>
		public static DependencyProperty PreventKeyboardDisplayOnProgrammaticFocusProperty { get; } =
			DependencyProperty.Register(
				nameof(PreventKeyboardDisplayOnProgrammaticFocus),
				typeof(bool),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(false));

		/// <summary>
		/// Gets or sets a value that prevents the software keyboard from displaying when focus is set programmatically.
		/// </summary>
		public bool PreventKeyboardDisplayOnProgrammaticFocus
		{
			get => (bool)GetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty);
			set => SetValue(PreventKeyboardDisplayOnProgrammaticFocusProperty, value);
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
					OnRichEditBoxPropertyChanged,
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
		/// Identifies the <see cref="ProofingMenuFlyout"/> dependency property.
		/// </summary>
		public static DependencyProperty ProofingMenuFlyoutProperty { get; } =
			DependencyProperty.Register(
				nameof(ProofingMenuFlyout),
				typeof(FlyoutBase),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(FlyoutBase), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		/// <summary>
		/// Gets the flyout that displays spelling corrections for the selected text.
		/// </summary>
		public FlyoutBase ProofingMenuFlyout => GetProofingMenuFlyout();

		/// <summary>
		/// Identifies the <see cref="SelectionFlyout"/> dependency property.
		/// </summary>
		public static DependencyProperty SelectionFlyoutProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionFlyout),
				typeof(FlyoutBase),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(default(FlyoutBase), FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext));

		/// <summary>
		/// Gets or sets the flyout shown when text is selected.
		/// </summary>
		public FlyoutBase SelectionFlyout
		{
			get => (FlyoutBase)GetValue(SelectionFlyoutProperty);
			set => SetValue(SelectionFlyoutProperty, value);
		}

		/// <summary>
		/// Identifies the <see cref="SelectionHighlightColor"/> dependency property.
		/// </summary>
		public static DependencyProperty SelectionHighlightColorProperty { get; } =
			DependencyProperty.Register(
				nameof(SelectionHighlightColor),
				typeof(SolidColorBrush),
				typeof(RichEditBox),
				new FrameworkPropertyMetadata(DefaultBrushes.SelectionHighlightColor, OnRichEditBoxPropertyChanged));

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
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.Default,
					propertyChangedCallback: OnRichEditBoxPropertyChanged,
					coerceValueCallback: null,
					backingFieldUpdateCallback: null,
					createDefaultValueCallback: static () => SolidColorBrushHelper.Transparent));

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
					TextAlignment.DetectFromContent,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					OnRichEditBoxPropertyChanged));

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
				new FrameworkPropertyMetadata(
					TextReadingOrder.DetectFromContent,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					OnRichEditBoxPropertyChanged));

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
					TextWrapping.NoWrap,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					OnRichEditBoxPropertyChanged));

		/// <summary>
		/// Gets or sets a value that indicates how text wrapping occurs.
		/// </summary>
		public TextWrapping TextWrapping
		{
			get => (TextWrapping)GetValue(TextWrappingProperty);
			set => SetValue(TextWrappingProperty, value);
		}

	}
}