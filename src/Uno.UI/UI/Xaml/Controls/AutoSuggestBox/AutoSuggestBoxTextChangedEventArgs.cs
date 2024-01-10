#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	public partial class AutoSuggestBoxTextChangedEventArgs : global::Microsoft.UI.Xaml.DependencyObject
	{
		private AutoSuggestBox _owner;
		private string _originalText;

		public AutoSuggestionBoxTextChangeReason Reason
		{
			get => (AutoSuggestionBoxTextChangeReason)this.GetValue(ReasonProperty);
			set => SetValue(ReasonProperty, value);
		}

		public static global::Microsoft.UI.Xaml.DependencyProperty ReasonProperty { get; } =
		Microsoft.UI.Xaml.DependencyProperty.Register(
			"Reason", typeof(AutoSuggestionBoxTextChangeReason),
			typeof(global::Microsoft.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs),
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
