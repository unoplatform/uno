#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public  partial class AutoSuggestBoxTextChangedEventArgs : global::Windows.UI.Xaml.DependencyObject
	{
		public AutoSuggestionBoxTextChangeReason Reason
		{
			get => (AutoSuggestionBoxTextChangeReason)this.GetValue(ReasonProperty);
			set => this.SetValue(ReasonProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty ReasonProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Reason", typeof(AutoSuggestionBoxTextChangeReason), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs), 
			new FrameworkPropertyMetadata(default(AutoSuggestionBoxTextChangeReason)));

		public AutoSuggestBoxTextChangedEventArgs() : base()
		{
		}
	}
}
