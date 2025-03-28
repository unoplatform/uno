
using System;
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

		public global::Windows.UI.Xaml.Style TextBoxStyle
		{
			get => (global::Windows.UI.Xaml.Style)this.GetValue(TextBoxStyleProperty);
			set => this.SetValue(TextBoxStyleProperty, value);
		}

		public string Text
		{
			get => (string)this.GetValue(TextProperty);
			set => this.SetValue(TextProperty, value);
		}

		public global::Windows.UI.Xaml.Controls.IconElement QueryIcon
		{
			get => (global::Windows.UI.Xaml.Controls.IconElement)this.GetValue(QueryIconProperty);
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

		public static global::Windows.UI.Xaml.DependencyProperty MaxSuggestionListHeightProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxSuggestionListHeight", typeof(double),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: double.PositiveInfinity)
		);

		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderTextProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderText", typeof(string),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: "")
		);

		public static global::Windows.UI.Xaml.DependencyProperty TextBoxStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"TextBoxStyle", typeof(global::Windows.UI.Xaml.Style),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: null)
		);

		public static global::Windows.UI.Xaml.DependencyProperty TextMemberPathProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"TextMemberPath", typeof(string),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: null)
		);

		public static DependencyProperty TextProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Text", typeof(string),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: "", propertyChangedCallback: (dO, _) => ((AutoSuggestBox)dO).UpdateTextBox())
		);

		public static global::Windows.UI.Xaml.DependencyProperty UpdateTextOnSelectProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"UpdateTextOnSelect", typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: true)
		);

		public static global::Windows.UI.Xaml.DependencyProperty AutoMaximizeSuggestionAreaProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"AutoMaximizeSuggestionArea", typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: false)
		);

		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: null)
		);

		public static global::Windows.UI.Xaml.DependencyProperty IsSuggestionListOpenProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSuggestionListOpen", typeof(bool),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(defaultValue: false, propertyChangedCallback: (s, e) => (s as AutoSuggestBox)?.OnIsSuggestionListOpenChanged(e))
		);

		public static global::Windows.UI.Xaml.DependencyProperty QueryIconProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"QueryIcon", typeof(global::Windows.UI.Xaml.Controls.IconElement),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconElement), propertyChangedCallback: (s, e) => (s as AutoSuggestBox)?.UpdateQueryButton()));

		public static global::Windows.UI.Xaml.DependencyProperty DescriptionProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				nameof(Description), typeof(object),
				typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox),
				new FrameworkPropertyMetadata(default(object), propertyChangedCallback: (s, e) => (s as AutoSuggestBox)?.UpdateDescriptionVisibility(false)));

		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxSuggestionChosenEventArgs> SuggestionChosen;
		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxTextChangedEventArgs> TextChanged;
		public event TypedEventHandler<AutoSuggestBox, AutoSuggestBoxQuerySubmittedEventArgs> QuerySubmitted;
	}
}
