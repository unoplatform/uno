// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference AutoSuggestBox.idl, commit 5f9e85113

#pragma warning disable CS0067 // Events not yet raised — TextChanged/QuerySubmitted/SuggestionChosen wired in later iters

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls
{
	partial class AutoSuggestBox
	{
		/// <summary>
		/// Gets or sets the text that is displayed in the control until the value is changed by a
		/// user action or some other operation.
		/// </summary>
		public string PlaceholderText
		{
			get => (string)GetValue(PlaceholderTextProperty);
			set => SetValue(PlaceholderTextProperty, value);
		}

		/// <summary>
		/// Gets or sets the maximum height for the drop-down portion of the AutoSuggestBox control.
		/// </summary>
		public double MaxSuggestionListHeight
		{
			get => (double)GetValue(MaxSuggestionListHeightProperty);
			set => SetValue(MaxSuggestionListHeightProperty, value);
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the drop-down portion of the AutoSuggestBox is open.
		/// </summary>
		public bool IsSuggestionListOpen
		{
			get => (bool)GetValue(IsSuggestionListOpenProperty);
			set => SetValue(IsSuggestionListOpenProperty, value);
		}

		/// <summary>
		/// Gets or sets the content for the control's header.
		/// </summary>
		public object Header
		{
			get => GetValue(HeaderProperty);
			set => SetValue(HeaderProperty, value);
		}

		/// <summary>
		/// Gets or sets a value that indicates whether the control will resize and reposition itself
		/// to make room for an on-screen keyboard.
		/// </summary>
		public bool AutoMaximizeSuggestionArea
		{
			get => (bool)GetValue(AutoMaximizeSuggestionAreaProperty);
			set => SetValue(AutoMaximizeSuggestionAreaProperty, value);
		}

		/// <summary>
		/// Gets or sets a value that indicates whether items selected in the drop-down portion of the
		/// AutoSuggestBox update the content of the text box.
		/// </summary>
		public bool UpdateTextOnSelect
		{
			get => (bool)GetValue(UpdateTextOnSelectProperty);
			set => SetValue(UpdateTextOnSelectProperty, value);
		}

		/// <summary>
		/// Gets or sets the name or path of a property that is displayed for each data item.
		/// </summary>
		public string TextMemberPath
		{
			get => (string)GetValue(TextMemberPathProperty) ?? string.Empty;
			set => SetValue(TextMemberPathProperty, value);
		}

		/// <summary>
		/// Gets or sets the style applied to the text box of the AutoSuggestBox.
		/// </summary>
		public Style TextBoxStyle
		{
			get => (Style)GetValue(TextBoxStyleProperty);
			set => SetValue(TextBoxStyleProperty, value);
		}

		/// <summary>
		/// Gets or sets the text that is shown in the control.
		/// </summary>
		public string Text
		{
			get => (string)GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		/// <summary>
		/// Gets or sets the graphic content of the query button.
		/// </summary>
		public IconElement QueryIcon
		{
			get => (IconElement)GetValue(QueryIconProperty);
			set => SetValue(QueryIconProperty, value);
		}

		/// <summary>
		/// Gets or sets content that is shown below the control. The content should provide guidance
		/// about the input expected by the control.
		/// </summary>
		public
#if __APPLE_UIKIT__
		new
#endif
		object Description
		{
			get => GetValue(DescriptionProperty);
			set => SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty MaxSuggestionListHeightProperty { get; } =
			DependencyProperty.Register(
				nameof(MaxSuggestionListHeight),
				typeof(double),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(double.PositiveInfinity));

		public static DependencyProperty PlaceholderTextProperty { get; } =
			DependencyProperty.Register(
				nameof(PlaceholderText),
				typeof(string),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(string.Empty));

		public static DependencyProperty TextBoxStyleProperty { get; } =
			DependencyProperty.Register(
				nameof(TextBoxStyle),
				typeof(Style),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(default(Style)));

		public static DependencyProperty TextMemberPathProperty { get; } =
			DependencyProperty.Register(
				nameof(TextMemberPath),
				typeof(string),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(default(string)));

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				nameof(Text),
				typeof(string),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(string.Empty, (s, e) => ((AutoSuggestBox)s).OnTextPropertyChanged(e)));

		public static DependencyProperty UpdateTextOnSelectProperty { get; } =
			DependencyProperty.Register(
				nameof(UpdateTextOnSelect),
				typeof(bool),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(true));

		public static DependencyProperty AutoMaximizeSuggestionAreaProperty { get; } =
			DependencyProperty.Register(
				nameof(AutoMaximizeSuggestionArea),
				typeof(bool),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(false));

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				nameof(Header),
				typeof(object),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(default(object)));

		public static DependencyProperty IsSuggestionListOpenProperty { get; } =
			DependencyProperty.Register(
				nameof(IsSuggestionListOpen),
				typeof(bool),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(false, (s, e) => ((AutoSuggestBox)s).OnIsSuggestionListOpenPropertyChanged(e)));

		public static DependencyProperty QueryIconProperty { get; } =
			DependencyProperty.Register(
				nameof(QueryIcon),
				typeof(IconElement),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(default(IconElement), (s, e) => ((AutoSuggestBox)s).OnQueryIconPropertyChanged(e)));

		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(
				nameof(Description),
				typeof(object),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(default(object), (s, e) => ((AutoSuggestBox)s).OnDescriptionPropertyChanged(e)));

		/// <summary>
		/// Raised when the user selects an item from the recommended ones in the drop-down list.
		/// </summary>
		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;

		/// <summary>
		/// Raised when the text content of the editable control component of the AutoSuggestBox is updated.
		/// </summary>
		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> TextChanged;

		/// <summary>
		/// Occurs when the user submits a search query.
		/// </summary>
		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted;
	}
}
