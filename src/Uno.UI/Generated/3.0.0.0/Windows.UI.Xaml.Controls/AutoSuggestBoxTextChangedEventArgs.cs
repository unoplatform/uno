#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class AutoSuggestBoxTextChangedEventArgs : global::Windows.UI.Xaml.DependencyObject
	{
		#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason Reason
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason)this.GetValue(ReasonProperty);
			}
			set
			{
				this.SetValue(ReasonProperty, value);
			}
		}
		#endif
		#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty ReasonProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Reason", typeof(global::Windows.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.AutoSuggestionBoxTextChangeReason)));
		#endif
		#if false
		[global::Uno.NotImplemented]
		public AutoSuggestBoxTextChangedEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs", "AutoSuggestBoxTextChangedEventArgs.AutoSuggestBoxTextChangedEventArgs()");
		} 
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs.AutoSuggestBoxTextChangedEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs.Reason.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs.Reason.set
		#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  bool CheckCurrent()
		{
			throw new global::System.NotImplementedException("The member bool AutoSuggestBoxTextChangedEventArgs.CheckCurrent() is not implemented in Uno.");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBoxTextChangedEventArgs.ReasonProperty.get
	}
}
