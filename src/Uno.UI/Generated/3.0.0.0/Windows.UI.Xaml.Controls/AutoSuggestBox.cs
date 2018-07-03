#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || NET46 || __WASM__
	[global::Uno.NotImplemented]
	#endif
	public  partial class AutoSuggestBox : global::Windows.UI.Xaml.Controls.ItemsControl
	{
#if false
		[global::Uno.NotImplemented]
		public  string PlaceholderText
		{
			get
			{
				return (string)this.GetValue(PlaceholderTextProperty);
			}
			set
			{
				this.SetValue(PlaceholderTextProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  double MaxSuggestionListHeight
		{
			get
			{
				return (double)this.GetValue(MaxSuggestionListHeightProperty);
			}
			set
			{
				this.SetValue(MaxSuggestionListHeightProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  bool IsSuggestionListOpen
		{
			get
			{
				return (bool)this.GetValue(IsSuggestionListOpenProperty);
			}
			set
			{
				this.SetValue(IsSuggestionListOpenProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  object Header
		{
			get
			{
				return (object)this.GetValue(HeaderProperty);
			}
			set
			{
				this.SetValue(HeaderProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  bool AutoMaximizeSuggestionArea
		{
			get
			{
				return (bool)this.GetValue(AutoMaximizeSuggestionAreaProperty);
			}
			set
			{
				this.SetValue(AutoMaximizeSuggestionAreaProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  bool UpdateTextOnSelect
		{
			get
			{
				return (bool)this.GetValue(UpdateTextOnSelectProperty);
			}
			set
			{
				this.SetValue(UpdateTextOnSelectProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  string TextMemberPath
		{
			get
			{
				return (string)this.GetValue(TextMemberPathProperty);
			}
			set
			{
				this.SetValue(TextMemberPathProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Style TextBoxStyle
		{
			get
			{
				return (global::Windows.UI.Xaml.Style)this.GetValue(TextBoxStyleProperty);
			}
			set
			{
				this.SetValue(TextBoxStyleProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  string Text
		{
			get
			{
				return (string)this.GetValue(TextProperty);
			}
			set
			{
				this.SetValue(TextProperty, value);
			}
		}
#endif
#if false
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.IconElement QueryIcon
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.IconElement)this.GetValue(QueryIconProperty);
			}
			set
			{
				this.SetValue(QueryIconProperty, value);
			}
		}
#endif
#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.LightDismissOverlayMode LightDismissOverlayMode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)this.GetValue(LightDismissOverlayModeProperty);
			}
			set
			{
				this.SetValue(LightDismissOverlayModeProperty, value);
			}
		}
#endif

#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty MaxSuggestionListHeightProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"MaxSuggestionListHeight", typeof(double), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(double)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty PlaceholderTextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"PlaceholderText", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(string)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextBoxStyleProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TextBoxStyle", typeof(global::Windows.UI.Xaml.Style), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Style)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextMemberPathProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"TextMemberPath", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(string)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty TextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Text", typeof(string), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(string)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty UpdateTextOnSelectProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"UpdateTextOnSelect", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(bool)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty AutoMaximizeSuggestionAreaProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"AutoMaximizeSuggestionArea", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(bool)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty HeaderProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Header", typeof(object), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(object)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty IsSuggestionListOpenProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"IsSuggestionListOpen", typeof(bool), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(bool)));
#endif
#if false
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty QueryIconProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"QueryIcon", typeof(global::Windows.UI.Xaml.Controls.IconElement), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.IconElement)));
#endif
	#if __ANDROID__ || __IOS__ || NET46 || __WASM__
		[global::Uno.NotImplemented]
		public static global::Windows.UI.Xaml.DependencyProperty LightDismissOverlayModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"LightDismissOverlayMode", typeof(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode), 
			typeof(global::Windows.UI.Xaml.Controls.AutoSuggestBox), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.LightDismissOverlayMode)));
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.AutoSuggestBox()
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.MaxSuggestionListHeight.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.MaxSuggestionListHeight.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.IsSuggestionListOpen.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.IsSuggestionListOpen.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextMemberPath.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextMemberPath.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.Text.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.Text.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.UpdateTextOnSelect.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.UpdateTextOnSelect.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.PlaceholderText.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.PlaceholderText.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.Header.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.Header.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.AutoMaximizeSuggestionArea.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.AutoMaximizeSuggestionArea.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextBoxStyle.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextBoxStyle.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.SuggestionChosen.add
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.SuggestionChosen.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextChanged.add
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextChanged.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.QueryIcon.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.QueryIcon.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.QuerySubmitted.add
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.QuerySubmitted.remove
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.LightDismissOverlayMode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.LightDismissOverlayMode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.LightDismissOverlayModeProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.QueryIconProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.MaxSuggestionListHeightProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.IsSuggestionListOpenProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextMemberPathProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.UpdateTextOnSelectProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.PlaceholderTextProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.HeaderProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.AutoMaximizeSuggestionAreaProperty.get
		// Forced skipping of method Windows.UI.Xaml.Controls.AutoSuggestBox.TextBoxStyleProperty.get
	}
}
