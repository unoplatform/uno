#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	public partial class AutoSuggestBoxTextChangedEventArgs : global::Windows.UI.Xaml.DependencyObject
	{
		private AutoSuggestBox _owner;
		private string _originalText;

		public AutoSuggestionBoxTextChangeReason Reason
		{
			get => (AutoSuggestionBoxTextChangeReason)this.GetValue(ReasonProperty);
			set => SetValue(ReasonProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty ReasonProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Reason", typeof(AutoSuggestionBoxTextChangeReason),
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs),
			new FrameworkPropertyMetadata(default(AutoSuggestionBoxTextChangeReason)));

		internal AutoSuggestBox Owner
		{
			get => _owner;
			set
			{
				_owner = value;
				_originalText = _owner.Text;
			}
		}

		public AutoSuggestBoxTextChangedEventArgs() : base()
		{
		}

		public bool CheckCurrent() => string.Equals(_originalText, _owner.Text);
	}
}
