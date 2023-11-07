using Windows.Foundation;

namespace Windows.UI.Xaml.Controls;

public partial class AutoSuggestBox : ItemsControl
{
	/// <summary>
	/// Indicates if the suggestion area should be automatically maximized.
	/// </summary>
	public bool AutoMaximizeSuggestionArea
	{
		get => (bool)this.GetValue(AutoMaximizeSuggestionAreaProperty);
		set => this.SetValue(AutoMaximizeSuggestionAreaProperty, value);
	}

	/// <summary>
	/// Identifies the AutoMaximizeSuggestionArea dependency property.
	/// </summary>
	public static DependencyProperty AutoMaximizeSuggestionAreaProperty { get; } =
		DependencyProperty.Register(
			nameof(AutoMaximizeSuggestionArea),
			typeof(bool),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: true)
		);

	/// <summary>
	/// Gets or sets content that is shown below the control. The content should
	/// provide guidance about the input expected by the control.
	/// </summary>
	public
#if __IOS__ || __MACOS__
		new
#endif
		object Description
	{
		get => this.GetValue(DescriptionProperty);
		set => this.SetValue(DescriptionProperty, value);
	}

	/// <summary>
	/// Identifies the Description dependency property.
	/// </summary>
	public static DependencyProperty DescriptionProperty { get; } =
		DependencyProperty.Register(
			nameof(Description),
			typeof(object),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: null, propertyChangedCallback: (s, e) => (s as AutoSuggestBox)?.UpdateDescriptionVisibility(false)));

	/// <summary>
	/// Gets or sets the header object for the text box portion of this control.
	/// </summary>
	public object Header
	{
		get => this.GetValue(HeaderProperty);
		set => this.SetValue(HeaderProperty, value);
	}

	/// <summary>
	/// Identifies the Header dependency property.
	/// </summary>
	public static DependencyProperty HeaderProperty { get; } =
		DependencyProperty.Register(
			nameof(Header),
			typeof(object),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: null)
		);

	/// <summary>
	/// Gets or sets a Boolean value indicating whether the drop-down
	/// portion of the AutoSuggestBox is open.
	/// </summary>
	public bool IsSuggestionListOpen
	{
		get => (bool)this.GetValue(IsSuggestionListOpenProperty);
		set => SetIsSuggestionListOpen(value);
	}

	/// <summary>
	/// Identifies the IsSuggestionListOpen dependency property.
	/// </summary>
	public static DependencyProperty IsSuggestionListOpenProperty { get; } =
		DependencyProperty.Register(
			nameof(IsSuggestionListOpen),
			typeof(bool),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: false)
		);

	/// <summary>
	/// Gets or sets a value that specifies whether the area outside of a light-dismiss UI is darkened.
	/// </summary>
	public LightDismissOverlayMode LightDismissOverlayMode
	{
		get => (LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
		set => this.SetValue(LightDismissOverlayModeProperty, value);
	}

	/// <summary>
	/// Identifies the LightDismissOverlayMode dependency property.
	/// </summary>
	public static DependencyProperty LightDismissOverlayModeProperty { get; } =
		DependencyProperty.Register(
			nameof(LightDismissOverlayMode),
			typeof(LightDismissOverlayMode),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(LightDismissOverlayMode.Auto));

	/// <summary>
	/// Gets or set the maximum height for the drop-down portion of the AutoSuggestBox control.
	/// </summary>
	public double MaxSuggestionListHeight
	{
		get => (double)this.GetValue(MaxSuggestionListHeightProperty);
		set => this.SetValue(MaxSuggestionListHeightProperty, value);
	}

	/// <summary>
	/// Identifies the MaxSuggestionListHeight dependency property.
	/// </summary>
	public static DependencyProperty MaxSuggestionListHeightProperty { get; } =
	DependencyProperty.Register(
		nameof(MaxSuggestionListHeight),
		typeof(double),
		typeof(AutoSuggestBox),
		new FrameworkPropertyMetadata(defaultValue: double.PositiveInfinity)
	);

	/// <summary>
	/// Gets or sets the placeholder text to be displayed in the control.
	/// </summary>
	public string PlaceholderText
	{
		get => (string)this.GetValue(PlaceholderTextProperty);
		set => this.SetValue(PlaceholderTextProperty, value);
	}

	/// <summary>
	/// Identifies the PlaceholderText dependency property.
	/// </summary>
	public static DependencyProperty PlaceholderTextProperty { get; } =
		DependencyProperty.Register(
			nameof(PlaceholderText),
			typeof(string),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: "")
		);

	/// <summary>
	/// Gets or sets the graphic content of the button that is clicked to initiate a query.
	/// </summary>
	public IconElement QueryIcon
	{
		get => (IconElement)this.GetValue(QueryIconProperty);
		set => this.SetValue(QueryIconProperty, value);
	}

	/// <summary>
	/// Gets or sets the graphic content of the button that is clicked to initiate a query.
	/// </summary>
	public static DependencyProperty QueryIconProperty { get; } =
		DependencyProperty.Register(
			nameof(QueryIcon),
			typeof(IconElement),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(default(IconElement)));

	/// <summary>
	/// Gets or sets the text that is shown in the control.
	/// </summary>
	public string Text
	{
		get => (string)this.GetValue(TextProperty);
		set => this.SetValue(TextProperty, value);
	}

	/// <summary>
	/// Identifies the Text dependency property.
	/// </summary>
	public static DependencyProperty TextProperty { get; } =
		DependencyProperty.Register(
			nameof(Text),
			typeof(string),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: "")
		);

	/// <summary>
	/// Gets or sets the style of the auto-suggest text box.
	/// </summary>
	public Style TextBoxStyle
	{
		get => (Style)this.GetValue(TextBoxStyleProperty);
		set => this.SetValue(TextBoxStyleProperty, value);
	}

	/// <summary>
	/// Identifies the TextBoxStyle dependency property.
	/// </summary>
	public static DependencyProperty TextBoxStyleProperty { get; } =
	DependencyProperty.Register(
		nameof(TextBoxStyle),
		typeof(Style),
		typeof(AutoSuggestBox),
		new FrameworkPropertyMetadata(defaultValue: null)
	);

	/// <summary>
	/// Gets or sets the property path that is used to get the value for display
	/// in the text box portion of the AutoSuggestBox control, when an item is selected.
	/// </summary>
	public string TextMemberPath
	{
		get => (string)this.GetValue(TextMemberPathProperty);
		set => this.SetValue(TextMemberPathProperty, value);
	}

	/// <summary>
	/// Identifies the TextMemberPath dependency property.
	/// </summary>
	public static DependencyProperty TextMemberPathProperty { get; } =
		DependencyProperty.Register(
			nameof(TextMemberPath),
			typeof(string),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: "")
		);

	/// <summary>
	/// Used in conjunction with TextMemberPath, gets or sets a value indicating whether items
	/// in the view will trigger an update of the editable text part of the AutoSuggestBox when clicked.
	/// </summary>
	public bool UpdateTextOnSelect
	{
		get => (bool)this.GetValue(UpdateTextOnSelectProperty);
		set => this.SetValue(UpdateTextOnSelectProperty, value);
	}

	/// <summary>
	/// Identifies the UpdateTextOnSelect dependency property.
	/// </summary>
	public static DependencyProperty UpdateTextOnSelectProperty { get; } =
		DependencyProperty.Register(
			nameof(UpdateTextOnSelect), typeof(bool),
			typeof(AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: true)
		);

	/// <summary>
	/// Occurs when the user submits a search query.
	/// </summary>
	public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted;

	/// <summary>
	/// Raised before the text content of the editable control component is updated.
	/// </summary>
	public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;

	/// <summary>
	/// Raised after the text content of the editable control component is updated.
	/// </summary>
	public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> TextChanged;
}
