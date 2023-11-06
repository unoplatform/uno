using Windows.Foundation;

namespace Windows.UI.Xaml.Controls
{
	public partial class AutoSuggestBox : ItemsControl
	{
		public string PlaceholderText
		{
			get => (string)this.GetValue(PlaceholderTextProperty);
			set => this.SetValue(PlaceholderTextProperty, value);
		}

		public double MaxSuggestionListHeight
		{
			get => (double)this.GetValue(MaxSuggestionListHeightProperty);
			set => this.SetValue(MaxSuggestionListHeightProperty, value);
		}

		public bool IsSuggestionListOpen
		{
			get => (bool)this.GetValue(IsSuggestionListOpenProperty);
			set => this.SetValue(IsSuggestionListOpenProperty, value);
		}

		public object Header
		{
			get => (object)this.GetValue(HeaderProperty);
			set => this.SetValue(HeaderProperty, value);
		}

		public bool AutoMaximizeSuggestionArea
		{
			get => (bool)this.GetValue(AutoMaximizeSuggestionAreaProperty);
			set => this.SetValue(AutoMaximizeSuggestionAreaProperty, value);
		}

		public bool UpdateTextOnSelect
		{
			get => (bool)this.GetValue(UpdateTextOnSelectProperty);
			set => this.SetValue(UpdateTextOnSelectProperty, value);
		}

		public string TextMemberPath
		{
			get => (string)this.GetValue(TextMemberPathProperty) ?? "";
			set => this.SetValue(TextMemberPathProperty, value);
		}

		public Style TextBoxStyle
		{
			get => (Style)this.GetValue(TextBoxStyleProperty);
			set => this.SetValue(TextBoxStyleProperty, value);
		}

		public string Text
		{
			get => (string)this.GetValue(TextProperty);
			set => this.SetValue(TextProperty, value);
		}

		public IconElement QueryIcon
		{
			get => (IconElement)this.GetValue(QueryIconProperty);
			set => this.SetValue(QueryIconProperty, value);
		}

		public
#if __IOS__ || __MACOS__
		new
#endif
		object Description
		{
			get => this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		public static DependencyProperty MaxSuggestionListHeightProperty { get; } =
			DependencyProperty.Register(
				"MaxSuggestionListHeight", typeof(double),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: double.PositiveInfinity)
			);

		public static DependencyProperty PlaceholderTextProperty { get; } =
			DependencyProperty.Register(
				"PlaceholderText", typeof(string),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: "")
			);

		public static DependencyProperty TextBoxStyleProperty { get; } =
			DependencyProperty.Register(
				"TextBoxStyle", typeof(Style),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: null)
			);

		public static DependencyProperty TextMemberPathProperty { get; } =
			DependencyProperty.Register(
				"TextMemberPath", typeof(string),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: null)
			);

		public static DependencyProperty TextProperty { get; } =
			DependencyProperty.Register(
				"Text", typeof(string),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: "")
			);

		public static DependencyProperty UpdateTextOnSelectProperty { get; } =
			DependencyProperty.Register(
				"UpdateTextOnSelect", typeof(bool),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: true)
			);

		public static DependencyProperty AutoMaximizeSuggestionAreaProperty { get; } =
			DependencyProperty.Register(
				"AutoMaximizeSuggestionArea", typeof(bool),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: false)
			);

		public static DependencyProperty HeaderProperty { get; } =
			DependencyProperty.Register(
				"Header", typeof(object),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: null)
			);

		public static DependencyProperty IsSuggestionListOpenProperty { get; } =
			DependencyProperty.Register(
				"IsSuggestionListOpen", typeof(bool),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => (s as AutoSuggestBox)?.OnIsSuggestionListOpenChanged(e))
			);

		public static DependencyProperty QueryIconProperty { get; } =
			DependencyProperty.Register(
				"QueryIcon", typeof(IconElement),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(default(IconElement)));

		public static DependencyProperty DescriptionProperty { get; } =
			DependencyProperty.Register(
				nameof(Description), typeof(object),
				typeof(AutoSuggestBox),
				new FrameworkPropertyMetadata(default(object)));

		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;
		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> TextChanged;
		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted;
	}
}
