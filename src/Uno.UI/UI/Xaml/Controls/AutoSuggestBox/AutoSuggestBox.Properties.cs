// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference src\dxaml\xcp\dxaml\lib\AutoSuggestBox_Partial.h, tag winui3/release/1.7.1

using Windows.Foundation;

namespace Microsoft.UI.Xaml.Controls;

partial class AutoSuggestBox
{
	#region Properties

	/// <summary>
	/// Gets or sets the placeholder text to be displayed in the control.
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
	/// Gets or sets a value that indicates whether the suggestion list is open.
	/// </summary>
	public bool IsSuggestionListOpen
	{
		get => (bool)GetValue(IsSuggestionListOpenProperty);
		set
		{
			SetValue(IsSuggestionListOpenProperty, value);

			// When we programmatically set IsSuggestionListOpen to true, we want to take focus.
			if (value)
			{
				Focus(FocusState.Programmatic);
			}
		}
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
	/// Gets or sets whether the control automatically maximizes the suggestion area.
	/// </summary>
	public bool AutoMaximizeSuggestionArea
	{
		get => (bool)GetValue(AutoMaximizeSuggestionAreaProperty);
		set => SetValue(AutoMaximizeSuggestionAreaProperty, value);
	}

	/// <summary>
	/// Gets or sets whether the textbox is updated when the user selects a suggestion.
	/// </summary>
	public bool UpdateTextOnSelect
	{
		get => (bool)GetValue(UpdateTextOnSelectProperty);
		set => SetValue(UpdateTextOnSelectProperty, value);
	}

	/// <summary>
	/// Gets or sets the property path that is used to get the value for display in the text box portion
	/// of the AutoSuggestBox control, when an item is selected.
	/// </summary>
	public string TextMemberPath
	{
		get => (string)GetValue(TextMemberPathProperty) ?? "";
		set => SetValue(TextMemberPathProperty, value);
	}

	/// <summary>
	/// Gets or sets the style of the TextBox in the AutoSuggestBox.
	/// </summary>
	public Style TextBoxStyle
	{
		get => (Style)GetValue(TextBoxStyleProperty);
		set => SetValue(TextBoxStyleProperty, value);
	}

	/// <summary>
	/// Gets or sets the text in the AutoSuggestBox.
	/// </summary>
	public string Text
	{
		get => (string)GetValue(TextProperty);
		set => SetValue(TextProperty, value);
	}

	/// <summary>
	/// Gets or sets the graphic content of the button that is clicked to initiate a query.
	/// </summary>
	public IconElement QueryIcon
	{
		get => (IconElement)GetValue(QueryIconProperty);
		set => SetValue(QueryIconProperty, value);
	}

	/// <summary>
	/// Gets or sets content that is shown below the control. The content should provide guidance about the input expected by the control.
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

	#endregion

	#region Dependency Properties

	/// <summary>
	/// Identifies the MaxSuggestionListHeight dependency property.
	/// </summary>
	public static DependencyProperty MaxSuggestionListHeightProperty { get; } =
		DependencyProperty.Register(
			nameof(MaxSuggestionListHeight),
			typeof(double),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(double.PositiveInfinity));

	/// <summary>
	/// Identifies the PlaceholderText dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderTextProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderText),
			typeof(string),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(""));

	/// <summary>
	/// Identifies the TextBoxStyle dependency property.
	/// </summary>
	public static DependencyProperty TextBoxStyleProperty { get; } =
		DependencyProperty.Register(
			nameof(TextBoxStyle),
			typeof(Style),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the TextMemberPath dependency property.
	/// </summary>
	public static DependencyProperty TextMemberPathProperty { get; } =
		DependencyProperty.Register(
			nameof(TextMemberPath),
			typeof(string),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: OnTextMemberPathChanged));

	/// <summary>
	/// Identifies the Text dependency property.
	/// </summary>
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(
				"",
				propertyChangedCallback: OnTextPropertyChanged));

	/// <summary>
	/// Identifies the UpdateTextOnSelect dependency property.
	/// </summary>
	public static DependencyProperty UpdateTextOnSelectProperty { get; } =
		DependencyProperty.Register(
			nameof(UpdateTextOnSelect),
			typeof(bool),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(true));

	/// <summary>
	/// Identifies the AutoMaximizeSuggestionArea dependency property.
	/// </summary>
	public static DependencyProperty AutoMaximizeSuggestionAreaProperty { get; } =
		DependencyProperty.Register(
			nameof(AutoMaximizeSuggestionArea),
			typeof(bool),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(false));

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(null));

	/// <summary>
	/// Identifies the IsSuggestionListOpen dependency property.
	/// </summary>
	public static DependencyProperty IsSuggestionListOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsSuggestionListOpen),
			typeof(bool),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(
				false,
				propertyChangedCallback: OnIsSuggestionListOpenChanged));

	/// <summary>
	/// Identifies the QueryIcon dependency property.
	/// </summary>
	public static DependencyProperty QueryIconProperty { get; } =
		DependencyProperty.Register(
			nameof(QueryIcon),
			typeof(IconElement),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: OnQueryIconChanged));

	/// <summary>
	/// Identifies the Description dependency property.
	/// </summary>
	public static DependencyProperty DescriptionProperty { get; } =
		DependencyProperty.Register(
			nameof(Description),
			typeof(object),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(
				null,
				propertyChangedCallback: OnDescriptionChanged));

	#endregion

	#region Events

	/// <summary>
	/// Raised before the text content of the editable control component is updated.
	/// </summary>
	public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;

	/// <summary>
	/// Raised after the text content of the editable control component is updated.
	/// </summary>
	public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> TextChanged;

	/// <summary>
	/// Occurs when the user submits a search query.
	/// </summary>
	public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted;

	#endregion

	#region Property Changed Callbacks

#if HAS_UNO
	private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AutoSuggestBox autoSuggestBox)
		{
			autoSuggestBox.OnTextPropertyChanged(e);
		}
	}

	private void OnTextPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		// When Text property changes, update the TextBox if it exists
		if (m_tpTextBoxPart is not null)
		{
			var newText = e.NewValue as string ?? "";
			var currentText = m_tpTextBoxPart.Text;
			if (newText != currentText)
			{
				m_textChangeReason = AutoSuggestionBoxTextChangeReason.ProgrammaticChange;
				m_tpTextBoxPart.Text = newText;
			}
		}
	}

	private static void OnTextMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AutoSuggestBox autoSuggestBox)
		{
			autoSuggestBox.OnTextMemberPathChanged(e);
		}
	}

	private void OnTextMemberPathChanged(DependencyPropertyChangedEventArgs e)
	{
		// Clear the property path listener so it will be recreated with the new path
		m_spPropertyPathListener = null;
	}

	private static void OnIsSuggestionListOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AutoSuggestBox autoSuggestBox)
		{
			autoSuggestBox.OnIsSuggestionListOpenChanged(e);
		}
	}

	private void OnIsSuggestionListOpenChanged(DependencyPropertyChangedEventArgs e)
	{
		var isOpen = (bool)e.NewValue;

		if (m_tpPopupPart is not null)
		{
			m_tpPopupPart.IsOpen = isOpen;

			if (!isOpen)
			{
				// In the desktop window, the focus moves into the AutoSuggestBox when the suggestion
				// popup list is closed so m_suppressSuggestionListVisibility flag ensures the popup
				// close when the AutoSuggestBox got the focus by closing the popup.
				m_suppressSuggestionListVisibility = true;

				// We should ensure that no element in the suggestion list is selected
				// when the popup isn't open, since otherwise that opens up the possibility
				// of interacting with the suggestion list even when it's closed.
				if (m_tpSuggestionsPart is not null)
				{
					m_ignoreSelectionChanges = true;
					m_tpSuggestionsPart.SelectedIndex = -1;
					m_ignoreSelectionChanges = false;
				}
			}
		}

		ReevaluateIsOverlayVisible();

		SetCurrentControlledPeer(isOpen ? ControlledPeer.SuggestionsList : ControlledPeer.None);
	}

	private static void OnQueryIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AutoSuggestBox autoSuggestBox)
		{
			autoSuggestBox.OnQueryIconChanged(e);
		}
	}

	private void OnQueryIconChanged(DependencyPropertyChangedEventArgs e)
	{
		SetTextBoxQueryButtonIcon();
	}

	private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is AutoSuggestBox autoSuggestBox)
		{
			autoSuggestBox.UpdateDescriptionVisibility(false);
		}
	}
#else
	private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }
	private static void OnTextMemberPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }
	private static void OnIsSuggestionListOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }
	private static void OnQueryIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }
	private static void OnDescriptionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) { }
#endif

	#endregion
}
